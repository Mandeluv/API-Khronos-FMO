using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using API_Khronos_FMO.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
namespace API_Khronos_FMO.Controllers;
[ApiController, Route("api/[controller]")]
public class AuthController(ConexionBD bd, IPasswordHasher<string> hasher) : ControllerBase
{
    public record Credenciales([Required, StringLength(15)] string Carnet, [Required, StringLength(200)] string Clave);
    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login(Credenciales datos, CancellationToken ct)
    {
        await using var c = await bd.ObtenerConexionAsync(ct);
        await using var cmd = new NpgsqlCommand("SELECT u.\"contraseña\", r.rol FROM tbl_usuarios u JOIN tbl_rol r USING(id_rol) WHERE carnet_usuario=@carnet", c);
        cmd.Parameters.AddWithValue("carnet", datos.Carnet);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return Unauthorized();
        var hash = r.GetString(0);
        var rol = r.GetString(1);
        try { if (hasher.VerifyHashedPassword(datos.Carnet, hash, datos.Clave) == PasswordVerificationResult.Failed) return Unauthorized(); }
        catch (FormatException) { return Unauthorized(); }
        var identidad = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, datos.Carnet), new Claim(ClaimTypes.Name, datos.Carnet), new Claim(ClaimTypes.Role, rol) }, BearerTokenDefaults.AuthenticationScheme);
        return SignIn(new ClaimsPrincipal(identidad), BearerTokenDefaults.AuthenticationScheme);
    }
    [HttpGet("yo")]
    public IActionResult Yo() => Ok(new { carnet = User.FindFirstValue(ClaimTypes.NameIdentifier), rol = User.FindFirstValue(ClaimTypes.Role) });
}
