using System.ComponentModel.DataAnnotations;
using API_Khronos_FMO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
namespace API_Khronos_FMO.Controllers;
[ApiController, Authorize(Roles="Administrador"), Route("api/[controller]")]
public class UsuariosController(ConsultasService consultas, EscrituraService escritura, IPasswordHasher<string> hasher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await consultas.ConsultarAsync("SELECT row_to_json(x)::text FROM (SELECT u.carnet_usuario, u.id_rol, r.rol, p.id_perfil, p.nombre, p.apellido, p.correo, p.telefono FROM tbl_usuarios u JOIN tbl_perfil p USING(id_perfil) JOIN tbl_rol r USING(id_rol)) x", ct));
    public record NuevoUsuario([Required,StringLength(15)] string Carnet, [Required,StringLength(100)] string Nombre, [Required,StringLength(100)] string Apellido, [Required,EmailAddress,StringLength(150)] string Correo, [Required,RegularExpression(@"^[0-9]{8}$")] string Telefono, [Required,StringLength(200,MinimumLength=12)] string Clave, [Range(1,int.MaxValue)] int IdRol);
    [HttpPost]
    public async Task<IActionResult> Crear(NuevoUsuario d, CancellationToken ct)
    {
        var hash = hasher.HashPassword(d.Carnet,d.Clave);
        var id = await escritura.EjecutarAsync("WITH p AS (INSERT INTO tbl_perfil(nombre,apellido,correo,telefono) VALUES(@n,@a,@c,@t) RETURNING id_perfil) INSERT INTO tbl_usuarios(carnet_usuario,id_perfil,\"contraseña\",id_rol) SELECT @id,id_perfil,@h,@r FROM p RETURNING carnet_usuario", ct, new NpgsqlParameter("n",d.Nombre),new NpgsqlParameter("a",d.Apellido),new NpgsqlParameter("c",d.Correo),new NpgsqlParameter("t",d.Telefono),new NpgsqlParameter("id",d.Carnet),new NpgsqlParameter("h",hash),new NpgsqlParameter("r",d.IdRol));
        return StatusCode(201,new { carnet=id });
    }
    public record ClaveNueva([Required,StringLength(200,MinimumLength=12)] string Clave);
    [HttpPut("{carnet}/clave")]
    public async Task<IActionResult> CambiarClave(string carnet, ClaveNueva d, CancellationToken ct)
    {
        var id=await escritura.EjecutarAsync("UPDATE tbl_usuarios SET \"contraseña\"=@h WHERE carnet_usuario=@id RETURNING carnet_usuario",ct,new NpgsqlParameter("h",hasher.HashPassword(carnet,d.Clave)),new NpgsqlParameter("id",carnet));
        return id is null ? NotFound() : NoContent();
    }
}
