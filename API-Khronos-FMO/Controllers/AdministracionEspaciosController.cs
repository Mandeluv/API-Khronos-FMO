using System.ComponentModel.DataAnnotations;
using API_Khronos_FMO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
namespace API_Khronos_FMO.Controllers;
[ApiController, Authorize(Roles="Administrador"), Route("api/Espacios")]
public class AdministracionEspaciosController(EscrituraService escritura, ConsultasService consultas) : ControllerBase
{
    public record NuevoEspacio([Required,StringLength(200)] string Nombre, [Range(1,int.MaxValue)] int Capacidad, [Range(1,int.MaxValue)] int IdUbicacion);
    [HttpPost("")]
    public async Task<IActionResult> Crear(NuevoEspacio d, CancellationToken ct)
    {
        var resultado = await escritura.EjecutarAsync("INSERT INTO tbl_espacios(nombre_espacio,capacidad,id_ubicacion,estado) VALUES(@n,@c,@u,'Disponible') RETURNING id_espacio", ct, new NpgsqlParameter("n", d.Nombre), new NpgsqlParameter("c", d.Capacidad), new NpgsqlParameter("u", d.IdUbicacion));
        return StatusCode(201,new { id = resultado });
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Modificar(int id, NuevoEspacio d, CancellationToken ct)
    {
        if (id <= 0) return BadRequest();
        var resultado = await escritura.EjecutarAsync("UPDATE tbl_espacios SET nombre_espacio=@n,capacidad=@c,id_ubicacion=@u WHERE id_espacio=@id RETURNING id_espacio", ct, new NpgsqlParameter("n", d.Nombre), new NpgsqlParameter("c", d.Capacidad), new NpgsqlParameter("u", d.IdUbicacion), new NpgsqlParameter("id", id));
        return resultado is null ? NotFound() : Ok(new { id = resultado });
    }
    public record AsignarEncargado([Required,StringLength(15)] string Carnet);
    [HttpPut("{id:int}/encargado")]
    public async Task<IActionResult> Encargado(int id, AsignarEncargado d, CancellationToken ct)
    {
        if (id <= 0) return BadRequest();
        var resultado = await escritura.EjecutarAsync("SELECT fn_asignar_encargado(@id,@c::varchar)", ct, new NpgsqlParameter("id", id), new NpgsqlParameter("c", d.Carnet));
        return NoContent();
    }
    public record Recurso([Range(1,int.MaxValue)] int IdRecurso,[Range(1,int.MaxValue)] int Cantidad);
    [HttpPost("{id:int}/recursos")]
    public async Task<IActionResult> AsignarRecurso(int id, Recurso d, CancellationToken ct)
    {
        if (id <= 0) return BadRequest();
        var resultado = await escritura.EjecutarAsync("INSERT INTO tbl_espacio_recurso(id_espacio,id_recurso,cantidad) VALUES(@id,@r,@c) RETURNING id_espacio_recurso", ct, new NpgsqlParameter("id", id), new NpgsqlParameter("r", d.IdRecurso), new NpgsqlParameter("c", d.Cantidad));
        return StatusCode(201,new { id = resultado });
    }
    public record CategoriaEspacio([Range(1,int.MaxValue)] int IdCategoria,[Required] string Categoria);
    [HttpPost("{id:int}/categorias")]
    public async Task<IActionResult> AsignarCategoria(int id, CategoriaEspacio d, CancellationToken ct)
    {
        if (id <= 0) return BadRequest();
        var resultado = await escritura.EjecutarAsync("INSERT INTO tbl_categoria_espacio(id_espacio,id_categoria,categoria) VALUES(@id,@c,@t::espacio_categoria) RETURNING id_categoria_espacio", ct, new NpgsqlParameter("id", id), new NpgsqlParameter("c", d.IdCategoria), new NpgsqlParameter("t", d.Categoria));
        return StatusCode(201,new { id = resultado });
    }
}
