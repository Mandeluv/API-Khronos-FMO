using API_Khronos_FMO.Services;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;

namespace API_Khronos_FMO.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisponibilidadController(ConsultasService consultas) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Consultar([FromQuery] DateOnly? fecha, [FromQuery] TimeOnly? horaInicio, [FromQuery] TimeOnly? horaFin, CancellationToken ct, int capacidad = 1)
    {
        if (fecha is null || fecha == default(DateOnly) || fecha.Value.DayOfWeek == DayOfWeek.Sunday ||
            horaInicio is null || horaFin is null || horaFin.Value <= horaInicio.Value || capacidad <= 0)
            return BadRequest(new { mensaje = "Fecha, horario o capacidad inválidos." });
        const string sql = """
            SELECT row_to_json(e)::text
            FROM v_espacios_detalle e
            WHERE e.estado::text = 'Disponible' AND e.capacidad >= @capacidad
              AND @fecha + @inicio > (CURRENT_TIMESTAMP AT TIME ZONE 'America/El_Salvador')
              AND NOT EXISTS (SELECT 1 FROM tbl_solicitud_reserva r
                WHERE r.id_espacio = e.id_espacio AND r.fecha = @fecha
                AND r.estado::text IN ('PENDIENTE','APROBADA')
                AND r.hora_inicio < @fin AND r.hora_fin > @inicio)
              AND NOT EXISTS (SELECT 1 FROM tbl_bloqueo_temporal b
                WHERE b.id_espacio = e.id_espacio AND b.fecha = @fecha
                AND b.hora_inicio < @fin AND b.hora_fin > @inicio)
              AND NOT EXISTS (SELECT 1 FROM tbl_horario_ordinario h
                WHERE h.id_espacio = e.id_espacio
                AND @fecha BETWEEN h.fecha_inicio_vigencia AND h.fecha_fin_vigencia
                AND h.dia_semana::text = CASE EXTRACT(ISODOW FROM @fecha)
                  WHEN 1 THEN 'Lunes' WHEN 2 THEN 'Martes' WHEN 3 THEN 'Miercoles'
                  WHEN 4 THEN 'Jueves' WHEN 5 THEN 'Viernes' WHEN 6 THEN 'Sabado' END
                AND h.hora_inicio < @fin AND h.hora_finalizacion > @inicio)
            ORDER BY e.nombre_espacio;
            """;
        return Ok(await consultas.ConsultarAsync(sql, ct,
            new NpgsqlParameter("fecha", NpgsqlDbType.Date) { Value = fecha.Value },
            new NpgsqlParameter("inicio", NpgsqlDbType.Time) { Value = horaInicio.Value },
            new NpgsqlParameter("fin", NpgsqlDbType.Time) { Value = horaFin.Value },
            new NpgsqlParameter("capacidad", capacidad)));
    }
}
