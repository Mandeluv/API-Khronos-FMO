using Microsoft.AspNetCore.Authorization;
using API_Khronos_FMO.Services;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace API_Khronos_FMO.Controllers;

[Authorize(Roles="Administrador")]
[ApiController]
[Route("api/[controller]")]
public class GruposController(ConsultasService consultas) : ControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> ObtenerGrupos(CancellationToken ct)
    {
        return Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM (SELECT * FROM v_grupos_detalle) AS x;", ct));
    }

    [HttpGet("{id:int}/estudiantes")]
    public async Task<IActionResult> ObtenerEstudiantes(int id, CancellationToken ct)
    {
        if (id <= 0) return BadRequest(new { mensaje = "Identificador inválido." });
        return Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM (SELECT * FROM v_estudiantes_por_grupo WHERE id_grupo = @id) AS x;", ct, new NpgsqlParameter("id", id)));
    }

}
