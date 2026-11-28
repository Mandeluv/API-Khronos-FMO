using Microsoft.AspNetCore.Authorization;
using API_Khronos_FMO.Services;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace API_Khronos_FMO.Controllers;

[Authorize(Roles="Administrador")]
[ApiController]
[Route("api/[controller]")]
public class IncidenciasController(ConsultasService consultas) : ControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> ObtenerIncidencias(CancellationToken ct)
    {
        return Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM (SELECT * FROM v_reportes_incidencias) AS x;", ct));
    }

}
