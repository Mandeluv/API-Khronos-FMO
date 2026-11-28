using Microsoft.AspNetCore.Authorization;
using API_Khronos_FMO.Services;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace API_Khronos_FMO.Controllers;

[Authorize(Roles="Administrador")]
[ApiController]
[Route("api/[controller]")]
public class EstadisticasController(ConsultasService consultas) : ControllerBase
{
    [HttpGet("uso-por-solicitante")]
    public async Task<IActionResult> ObtenerUsoPorSolicitante(CancellationToken ct)
    {
        return Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM (SELECT * FROM v_uso_por_solicitante) AS x;", ct));
    }

}
