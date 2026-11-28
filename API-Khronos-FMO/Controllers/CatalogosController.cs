using API_Khronos_FMO.Services;
using Microsoft.AspNetCore.Mvc;
namespace API_Khronos_FMO.Controllers;
[ApiController, Route("api/[controller]")]
public class CatalogosController(ConsultasService consultas) : ControllerBase
{
    [HttpGet("ubicaciones")] public async Task<IActionResult> Ubicaciones(CancellationToken ct) => Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM tbl_ubicacion x",ct));
    [HttpGet("asignaturas")] public async Task<IActionResult> Asignaturas(CancellationToken ct) => Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM tbl_asignatura x",ct));
    [HttpGet("recursos")] public async Task<IActionResult> Recursos(CancellationToken ct) => Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM tbl_recursos x",ct));
    [HttpGet("categorias")] public async Task<IActionResult> Categorias(CancellationToken ct) => Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM tbl_categoria x",ct));
    [HttpGet("carreras")] public async Task<IActionResult> Carreras(CancellationToken ct) => Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM tbl_carrera x",ct));
    [HttpGet("motivos")] public async Task<IActionResult> Motivos(CancellationToken ct) => Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM tbl_motivo x",ct));
    [HttpGet("prioridades")] public async Task<IActionResult> Prioridades(CancellationToken ct) => Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM tbl_prioridades x",ct));
    [HttpGet("roles")] public async Task<IActionResult> Roles(CancellationToken ct) => Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM tbl_rol x",ct));
}
