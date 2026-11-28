-- Ejecutar en la base Khronos, no modifica datos.
SELECT nombre, to_regclass(nombre) IS NOT NULL AS existe
FROM unnest(ARRAY['v_grupos_detalle','v_estudiantes_por_grupo',
 'v_espacios_detalle','v_recursos_por_espacio','v_solicitudes_detalle',
 'v_reservas_aprobadas','v_reservas_rechazadas','v_solicitudes_pendientes',
 'v_historial_reservas_detalle','v_uso_por_solicitante','v_reportes_incidencias',
 'tbl_solicitud_reserva','tbl_bloqueo_temporal','tbl_horario_ordinario']) AS t(nombre);
SELECT table_schema, table_name, column_name, data_type
FROM information_schema.columns
WHERE table_name IN ('tbl_usuarios','tbl_rol','tbl_perfil','tbl_reporte',
 'tbl_espacios','tbl_bloqueo_temporal','tbl_horario_ordinario','tbl_solicitud_reserva',
 'v_estudiantes_por_grupo','v_solicitudes_detalle','v_recursos_por_espacio')
ORDER BY table_schema, table_name, ordinal_position;
SELECT n.nspname AS esquema, p.proname AS procedimiento,
 pg_get_function_arguments(p.oid) AS argumentos, pg_get_functiondef(p.oid) AS definicion
FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE p.prokind = 'p' AND n.nspname NOT IN ('pg_catalog','information_schema');
