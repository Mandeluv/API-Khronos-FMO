using System.ComponentModel.DataAnnotations;
using API_Khronos_FMO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
namespace API_Khronos_FMO.Controllers;
[ApiController, Authorize(Roles="Administrador"), Route("api/Academico")]
public class AcademicoController(EscrituraService escritura, ConsultasService consultas) : ControllerBase
{
    [HttpGet("estudiantes")] public async Task<IActionResult> Estudiantes(CancellationToken ct) => Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM tbl_estudiantes x",ct));
    public record Estudiante([Required,StringLength(15)] string Carnet,[Required,StringLength(100)] string Nombre,[Required,StringLength(100)] string Apellido,[Required,EmailAddress,StringLength(150)] string Correo,[Required,StringLength(100)] string CodigoCarrera);
    [HttpPost("estudiantes")]
    public async Task<IActionResult> CrearEstudiante(Estudiante d, CancellationToken ct)
    {
        var resultado = await escritura.EjecutarAsync("INSERT INTO tbl_estudiantes(id_carnet_estudiante,nombre,apellido,correo,codigo_carrera) VALUES(@id,@n,@a,@c,@ca) RETURNING id_carnet_estudiante", ct, new NpgsqlParameter("id", d.Carnet), new NpgsqlParameter("n", d.Nombre), new NpgsqlParameter("a", d.Apellido), new NpgsqlParameter("c", d.Correo), new NpgsqlParameter("ca", d.CodigoCarrera));
        return StatusCode(201,new { id = resultado });
    }
    public record Grupo([Required,StringLength(100)] string Nombre,[Range(0,int.MaxValue)] int Cantidad,[Required,StringLength(100)] string CodigoAsignatura,[Required,StringLength(15)] string CarnetDocente);
    [HttpPost("grupos")]
    public async Task<IActionResult> CrearGrupo(Grupo d, CancellationToken ct)
    {
        var resultado = await escritura.EjecutarAsync("INSERT INTO tbl_grupos(grupo,cantidad_estudiantes,codigo_asignatura,carnet_usuario) VALUES(@n,@c,@a,@u) RETURNING id_grupo", ct, new NpgsqlParameter("n", d.Nombre), new NpgsqlParameter("c", d.Cantidad), new NpgsqlParameter("a", d.CodigoAsignatura), new NpgsqlParameter("u", d.CarnetDocente));
        return StatusCode(201,new { id = resultado });
    }
    public record Inscripcion([Required,StringLength(15)] string Carnet);
    [HttpPost("grupos/{id:int}/estudiantes")]
    public async Task<IActionResult> Inscribir(int id, Inscripcion d, CancellationToken ct)
    {
        if (id <= 0) return BadRequest();
        var resultado = await escritura.EjecutarAsync("INSERT INTO tbl_estudiante_grupo(id_grupo,id_carnet_estudiante) VALUES(@id,@c) RETURNING id_estudiante_grupo", ct, new NpgsqlParameter("id", id), new NpgsqlParameter("c", d.Carnet));
        return StatusCode(201,new { id = resultado });
    }
    public record Asignatura([Required,StringLength(100)] string Codigo,[Required,StringLength(100)] string Nombre,[Required,StringLength(100)] string CodigoCarrera);
    [HttpPost("asignaturas")]
    public async Task<IActionResult> CrearAsignatura(Asignatura d, CancellationToken ct)
    {
        var resultado = await escritura.EjecutarAsync("INSERT INTO tbl_asignatura(codigo_asignatura,nombre,codigo_carrera) VALUES(@id,@n,@c) RETURNING codigo_asignatura", ct, new NpgsqlParameter("id", d.Codigo), new NpgsqlParameter("n", d.Nombre), new NpgsqlParameter("c", d.CodigoCarrera));
        return StatusCode(201,new { id = resultado });
    }
    public record Carrera([Required,StringLength(100)] string Codigo,[Required,StringLength(200)] string Nombre);
    [HttpPost("carreras")]
    public async Task<IActionResult> CrearCarrera(Carrera d, CancellationToken ct)
    {
        var resultado = await escritura.EjecutarAsync("INSERT INTO tbl_carrera(codigo_carrera,nombre) VALUES(@c,@n) RETURNING id_carrera", ct, new NpgsqlParameter("c", d.Codigo), new NpgsqlParameter("n", d.Nombre));
        return StatusCode(201,new { id = resultado });
    }
}
