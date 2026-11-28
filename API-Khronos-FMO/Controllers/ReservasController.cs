using Microsoft.AspNetCore.Authorization;
using API_Khronos_FMO.Services;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace API_Khronos_FMO.Controllers;

[Authorize(Roles="Administrador")]
[ApiController]
[Route("api/[controller]")]
public class ReservasController(ConsultasService consultas) : ControllerBase
{
    [HttpGet("aprobadas")]
    public async Task<IActionResult> ObtenerAprobadas(CancellationToken ct)
    {
        return Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM (SELECT * FROM v_reservas_aprobadas) AS x;", ct));
    }

    [HttpGet("rechazadas")]
    public async Task<IActionResult> ObtenerRechazadas(CancellationToken ct)
    {
        return Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM (SELECT * FROM v_reservas_rechazadas) AS x;", ct));
    }

    [HttpGet("canceladas")]
    public async Task<IActionResult> ObtenerCanceladas(CancellationToken ct)
    {
        return Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM (SELECT * FROM v_solicitudes_detalle WHERE estado = 'CANCELADA') AS x;", ct));
    }

}
