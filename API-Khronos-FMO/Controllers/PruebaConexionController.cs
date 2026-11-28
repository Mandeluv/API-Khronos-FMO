namespace API_Khronos_FMO.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using API_Khronos_FMO.Data;
    using Microsoft.AspNetCore.Mvc;
    using Npgsql;

    [ApiController]
    [Route("api/[controller]")]
    public class PruebaConexionController : ControllerBase
    {
        private readonly ConexionBD _conexionBD;

        public PruebaConexionController(ConexionBD conexionBD)
        {
            _conexionBD = conexionBD;
        }


        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ProbarConexion()
        {
            await using var conexion =
                await _conexionBD.ObtenerConexionAsync();

            await using var comando = new NpgsqlCommand(
                "SELECT current_database(), current_user;",
                conexion);

            await using var reader = await comando.ExecuteReaderAsync();

            await reader.ReadAsync();

            return Ok(new
            {
                mensaje = "Conexión exitosa con PostgreSQL",
                baseDatos = reader.GetString(0),
                usuario = reader.GetString(1)
            });
        }
    }

}
