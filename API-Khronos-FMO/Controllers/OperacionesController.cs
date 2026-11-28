using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using API_Khronos_FMO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
namespace API_Khronos_FMO.Controllers;
[ApiController, Route("api")]
public class OperacionesController(EscrituraService escritura, ConsultasService consultas) : ControllerBase
{
    private string Carnet => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public record Incidencia([Range(1,int.MaxValue)] int IdEspacio,[Required] string Descripcion,[StringLength(255)] string? Evidencia);
    [HttpPost("Incidencias")]
    public async Task<IActionResult> Reportar(Incidencia d,CancellationToken ct)
    {
        var id=await escritura.EjecutarAsync("INSERT INTO tbl_reporte(id_espacio,carnet_usuario,descripcion,evidencia) VALUES(@e,@u,@d,@v) RETURNING id_reporte",ct,new NpgsqlParameter("e",d.IdEspacio),new NpgsqlParameter("u",Carnet),new NpgsqlParameter("d",d.Descripcion),new NpgsqlParameter("v", NpgsqlTypes.NpgsqlDbType.Varchar) { Value=(object?)d.Evidencia ?? DBNull.Value });
        return StatusCode(201,new { id });
    }
    [HttpGet("Solicitudes/mias")]
    public async Task<IActionResult> MisSolicitudes(CancellationToken ct) => Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM v_solicitudes_detalle x WHERE carnet_usuario=@u",ct,new NpgsqlParameter("u",Carnet)));
    [HttpGet("Grupos/mios"),Authorize(Roles="Docente")]
    public async Task<IActionResult> MisGrupos(CancellationToken ct) => Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM v_grupos_detalle x WHERE carnet_docente=@u",ct,new NpgsqlParameter("u",Carnet)));
    public record Motivo([Required,StringLength(200)] string Descripcion);
    [HttpPost("Espacios/{id:int}/desbloquear"),Authorize(Roles="Encargado de espacio")]
    public async Task<IActionResult> Desbloquear(int id,Motivo d,CancellationToken ct)
    {
        var r=await escritura.EjecutarAsync("WITH e AS (UPDATE tbl_espacios SET estado='Disponible' WHERE id_espacio=@id AND carnet_usuario=@u AND estado='En_mantenimiento' RETURNING id_espacio) INSERT INTO tbl_historial(fecha,donde,modificacion,motivo,carnet_usuario) SELECT CURRENT_DATE,'tbl_espacios','DESBLOQUEO',@m,@u FROM e RETURNING id_historial",ct,new NpgsqlParameter("id",id),new NpgsqlParameter("u",Carnet),new NpgsqlParameter("m",d.Descripcion));
        return r is null ? NotFound() : Ok(new { idHistorial=r });
    }
    [HttpDelete("Bloqueos/{id:int}"),Authorize(Roles="Encargado de espacio")]
    public async Task<IActionResult> QuitarBloqueo(int id,CancellationToken ct)
    {
        var r=await escritura.EjecutarAsync("WITH b AS (DELETE FROM tbl_bloqueo_temporal b USING tbl_espacios e WHERE b.id_bloqueo=@id AND b.id_espacio=e.id_espacio AND e.carnet_usuario=@u RETURNING b.id_bloqueo) INSERT INTO tbl_historial(fecha,donde,modificacion,carnet_usuario) SELECT CURRENT_DATE,'tbl_bloqueo_temporal','ELIMINACION',@u FROM b RETURNING id_historial",ct,new NpgsqlParameter("id",id),new NpgsqlParameter("u",Carnet));
        return r is null ? NotFound() : NoContent();
    }
    public record Asistencia(bool Asistio);
    [HttpPost("Reservas/{id:int}/asistencia"),Authorize(Roles="Encargado de espacio")]
    public async Task<IActionResult> RegistrarAsistencia(int id,Asistencia d,CancellationToken ct)
    {
        var r=await escritura.EjecutarAsync("UPDATE tbl_historial_reserva h SET asistencia_solicitante=@a, estado=(CASE WHEN @a THEN 'APROBADA' ELSE 'NO_ASISTIO' END)::tipo_estado_historial FROM tbl_solicitud_reserva s JOIN tbl_espacios e USING(id_espacio) WHERE h.id_solicitud=s.id_solicitud AND s.id_solicitud=@id AND s.estado='APROBADA' AND e.carnet_usuario=@u AND s.fecha+s.hora_fin <= LOCALTIMESTAMP AND h.asistencia_solicitante IS NULL RETURNING h.id_historial",ct,new NpgsqlParameter("a",d.Asistio),new NpgsqlParameter("id",id),new NpgsqlParameter("u",Carnet));
        return r is null ? Conflict(new { mensaje="Reserva no elegible para asistencia." }) : Ok(new { idHistorial=r });
    }
    [HttpDelete("HorariosOrdinarios/{id:int}"),Authorize(Roles="Administrador")]
    public async Task<IActionResult> QuitarHorario(int id,CancellationToken ct)
    {
        var r=await escritura.EjecutarAsync("DELETE FROM tbl_horario_ordinario WHERE id_horario=@id RETURNING id_horario",ct,new NpgsqlParameter("id",id));
        return r is null ? NotFound() : NoContent();
    }
    public record Resolucion([Required] string Descripcion);
    [HttpPut("Incidencias/{id:int}/resolver"),Authorize(Roles="Encargado de espacio")]
    public async Task<IActionResult> Resolver(int id,Resolucion d,CancellationToken ct)
    {
        var r=await escritura.EjecutarAsync("UPDATE tbl_reporte r SET estado='RESUELTA',fecha_resolucion=CURRENT_TIMESTAMP,resolucion=@d,carnet_resolutor=@u FROM tbl_espacios e WHERE r.id_espacio=e.id_espacio AND r.id_reporte=@id AND e.carnet_usuario=@u AND r.estado<>'RESUELTA' RETURNING r.id_reporte",ct,new NpgsqlParameter("id",id),new NpgsqlParameter("u",Carnet),new NpgsqlParameter("d",d.Descripcion));
        return r is null ? NotFound() : NoContent();
    }
}
