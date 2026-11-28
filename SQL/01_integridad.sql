-- Ejecutar una sola vez en una copia de la base y verificar antes de producción.
BEGIN;
SET LOCAL search_path=public,pg_catalog;
-- Los estados terminales no deben revalidar la disponibilidad histórica.
DO $$ DECLARE t text; BEGIN
FOREACH t IN ARRAY ARRAY['trg_validar_bloqueo_reserva','trg_validar_capacidad','trg_validar_estado_espacio','trg_validar_fecha_reserva','trg_validar_horario_grupo','trg_validar_horario_ordinario','trg_validar_reservas'] LOOP
 EXECUTE format('DROP TRIGGER %I ON public.tbl_solicitud_reserva',t);
 EXECUTE format('CREATE TRIGGER %I BEFORE INSERT OR UPDATE ON public.tbl_solicitud_reserva FOR EACH ROW WHEN (NEW.estado IN (''PENDIENTE'',''APROBADA'')) EXECUTE FUNCTION public.%I()',t,replace(t,'trg_','fn_'));
END LOOP; END $$;
CREATE OR REPLACE FUNCTION public.fn_control_estado_khronos() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF TG_OP='INSERT' THEN
  IF NEW.estado <> 'PENDIENTE' THEN RAISE EXCEPTION 'Una solicitud nueva debe ser pendiente'; END IF;
 ELSIF NEW.estado IS DISTINCT FROM OLD.estado THEN
  IF NOT ((OLD.estado='PENDIENTE' AND NEW.estado IN ('APROBADA','RECHAZADA','CANCELADA')) OR (OLD.estado='APROBADA' AND NEW.estado='CANCELADA')) THEN RAISE EXCEPTION 'Transición no permitida'; END IF;
  IF NEW.estado='CANCELADA' AND OLD.fecha+OLD.hora_inicio <= LOCALTIMESTAMP THEN RAISE EXCEPTION 'La reserva ya comenzó'; END IF;
 END IF;
 RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS trg_control_estado_khronos ON tbl_solicitud_reserva;
CREATE TRIGGER trg_control_estado_khronos BEFORE INSERT OR UPDATE ON tbl_solicitud_reserva FOR EACH ROW EXECUTE FUNCTION fn_control_estado_khronos();
CREATE OR REPLACE FUNCTION public.fn_historial_khronos() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF NEW.estado <> 'PENDIENTE' AND NEW.estado IS DISTINCT FROM OLD.estado THEN
 INSERT INTO tbl_historial_reserva(id_solicitud,carnet_usuario,id_grupo,fecha,hora_inicio,hora_fin,id_espacio,estado)
 VALUES(NEW.id_solicitud,NEW.carnet_usuario,NEW.id_grupo,NEW.fecha,NEW.hora_inicio,NEW.hora_fin,NEW.id_espacio,NEW.estado::text::tipo_estado_historial)
 ON CONFLICT(id_solicitud) DO UPDATE SET estado=EXCLUDED.estado,asistencia_solicitante=NULL,fecha_resolucion=CURRENT_TIMESTAMP;
 END IF;
 RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS trg_historial_khronos ON tbl_solicitud_reserva;
CREATE TRIGGER trg_historial_khronos AFTER UPDATE OF estado ON tbl_solicitud_reserva FOR EACH ROW EXECUTE FUNCTION fn_historial_khronos();
INSERT INTO tbl_historial_reserva(id_solicitud,carnet_usuario,id_grupo,fecha,hora_inicio,hora_fin,id_espacio,estado)
SELECT id_solicitud,carnet_usuario,id_grupo,fecha,hora_inicio,hora_fin,id_espacio,estado::text::tipo_estado_historial FROM tbl_solicitud_reserva WHERE estado <> 'PENDIENTE' ON CONFLICT(id_solicitud) DO NOTHING;
-- Mantenimiento: no dejar reservas activas futuras sin resolver.
CREATE OR REPLACE FUNCTION public.fn_mantenimiento_khronos() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF NEW.estado='En_mantenimiento' AND OLD.estado IS DISTINCT FROM NEW.estado AND EXISTS(SELECT 1 FROM tbl_solicitud_reserva s WHERE s.id_espacio=NEW.id_espacio AND s.estado IN ('PENDIENTE','APROBADA') AND s.fecha+s.hora_fin > LOCALTIMESTAMP) THEN RAISE EXCEPTION 'Existen solicitudes o reservas activas futuras'; END IF;
 IF NEW.capacidad IS NULL OR NEW.capacidad<=0 THEN RAISE EXCEPTION 'Capacidad inválida'; END IF;
 IF EXISTS(SELECT 1 FROM tbl_solicitud_reserva s JOIN tbl_grupos g USING(id_grupo) WHERE s.id_espacio=NEW.id_espacio AND s.estado IN ('PENDIENTE','APROBADA') AND s.fecha+s.hora_fin>LOCALTIMESTAMP AND greatest(coalesce(g.cantidad_estudiantes,0),(SELECT count(*) FROM tbl_estudiante_grupo eg WHERE eg.id_grupo=g.id_grupo))>NEW.capacidad) THEN RAISE EXCEPTION 'La capacidad afecta reservas activas'; END IF;
 RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS trg_mantenimiento_khronos ON tbl_espacios;
CREATE TRIGGER trg_mantenimiento_khronos BEFORE UPDATE ON tbl_espacios FOR EACH ROW EXECUTE FUNCTION fn_mantenimiento_khronos();
-- Control simétrico de horarios nuevos o modificados contra solicitudes y bloqueos.
CREATE OR REPLACE FUNCTION public.fn_horario_khronos() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF EXISTS(SELECT 1 FROM tbl_espacios WHERE id_espacio=NEW.id_espacio AND estado='En_mantenimiento') THEN RAISE EXCEPTION 'Espacio en mantenimiento'; END IF;
 IF EXISTS(SELECT 1 FROM tbl_horario_ordinario h WHERE h.id_grupo=NEW.id_grupo AND h.id_horario IS DISTINCT FROM NEW.id_horario AND h.dia_semana=NEW.dia_semana AND h.fecha_inicio_vigencia<=NEW.fecha_fin_vigencia AND h.fecha_fin_vigencia>=NEW.fecha_inicio_vigencia AND h.hora_inicio<NEW.hora_finalizacion AND h.hora_finalizacion>NEW.hora_inicio) THEN RAISE EXCEPTION 'Grupo con horario simultáneo'; END IF;
 IF EXISTS(SELECT 1 FROM tbl_solicitud_reserva s WHERE (s.id_espacio=NEW.id_espacio OR s.id_grupo=NEW.id_grupo) AND s.estado IN ('PENDIENTE','APROBADA') AND s.fecha BETWEEN NEW.fecha_inicio_vigencia AND NEW.fecha_fin_vigencia AND extract(isodow FROM s.fecha)=array_position(ARRAY['Lunes','Martes','Miercoles','Jueves','Viernes','Sabado'],NEW.dia_semana::text) AND s.hora_inicio<NEW.hora_finalizacion AND s.hora_fin>NEW.hora_inicio) THEN RAISE EXCEPTION 'Horario coincide con reserva activa'; END IF;
 IF EXISTS(SELECT 1 FROM tbl_bloqueo_temporal b WHERE b.id_espacio=NEW.id_espacio AND b.fecha BETWEEN NEW.fecha_inicio_vigencia AND NEW.fecha_fin_vigencia AND extract(isodow FROM b.fecha)=array_position(ARRAY['Lunes','Martes','Miercoles','Jueves','Viernes','Sabado'],NEW.dia_semana::text) AND b.hora_inicio<NEW.hora_finalizacion AND b.hora_fin>NEW.hora_inicio) THEN RAISE EXCEPTION 'Horario coincide con bloqueo'; END IF;
 RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS trg_horario_khronos ON tbl_horario_ordinario;
CREATE TRIGGER trg_horario_khronos BEFORE INSERT OR UPDATE ON tbl_horario_ordinario FOR EACH ROW EXECUTE FUNCTION fn_horario_khronos();
ALTER TABLE tbl_reporte ADD COLUMN IF NOT EXISTS estado varchar(20) NOT NULL DEFAULT 'PENDIENTE';
ALTER TABLE tbl_reporte ADD COLUMN IF NOT EXISTS fecha_resolucion timestamptz;
ALTER TABLE tbl_reporte ADD COLUMN IF NOT EXISTS resolucion text;
ALTER TABLE tbl_reporte ADD COLUMN IF NOT EXISTS carnet_resolutor varchar(15) REFERENCES tbl_usuarios(carnet_usuario);
DO $$ BEGIN IF NOT EXISTS(SELECT 1 FROM pg_constraint WHERE conname='chk_reporte_estado_khronos' AND conrelid='public.tbl_reporte'::regclass) THEN ALTER TABLE tbl_reporte ADD CONSTRAINT chk_reporte_estado_khronos CHECK(estado IN ('PENDIENTE','EN_PROCESO','RESUELTA')); END IF; END $$;
COMMIT;
