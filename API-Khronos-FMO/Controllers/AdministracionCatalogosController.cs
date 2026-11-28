using System.ComponentModel.DataAnnotations;
using API_Khronos_FMO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
namespace API_Khronos_FMO.Controllers;
[ApiController, Authorize(Roles="Administrador"), Route("api/Catalogos")]
public class AdministracionCatalogosController(EscrituraService escritura, ConsultasService consultas) : ControllerBase
{
    public record Ubicacion([Required,StringLength(200)] string Nombre);
    [HttpPost("ubicaciones")]
    public async Task<IActionResult> CrearUbicacion(Ubicacion d, CancellationToken ct)
    {
        var resultado = await escritura.EjecutarAsync("INSERT INTO tbl_ubicacion(nombre) VALUES(@n) RETURNING id_ubicacion", ct, new NpgsqlParameter("n", d.Nombre));
        return StatusCode(201,new { id = resultado });
    }
    public record Categoria([Required,StringLength(150)] string Nombre,[StringLength(300)] string? Descripcion);
    [HttpPost("categorias")]
    public async Task<IActionResult> CrearCategoria(Categoria d, CancellationToken ct)
    {
        var resultado = await escritura.EjecutarAsync("INSERT INTO tbl_categoria(nombre,descripcion) VALUES(@n,@d) RETURNING id_categoria", ct, new NpgsqlParameter("n", d.Nombre), new NpgsqlParameter("d", NpgsqlTypes.NpgsqlDbType.Varchar) { Value=(object?)d.Descripcion ?? DBNull.Value });
        return StatusCode(201,new { id = resultado });
    }
    public record NuevoRecurso([Required,StringLength(200)] string Nombre,[Range(1,int.MaxValue)] int IdCategoria,[Required,StringLength(200)] string Tipo);
    [HttpPost("recursos")]
    public async Task<IActionResult> CrearRecurso(NuevoRecurso d, CancellationToken ct)
    {
        var resultado = await escritura.EjecutarAsync("INSERT INTO tbl_recursos(nombre,id_categoria,tipo_recurso) VALUES(@n,@c,@t) RETURNING id_recurso", ct, new NpgsqlParameter("n", d.Nombre), new NpgsqlParameter("c", d.IdCategoria), new NpgsqlParameter("t", d.Tipo));
        return StatusCode(201,new { id = resultado });
    }
}
