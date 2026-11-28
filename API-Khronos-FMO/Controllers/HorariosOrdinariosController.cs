using Microsoft.AspNetCore.Authorization;

using API_Khronos_FMO.DTOs;
using API_Khronos_FMO.Services;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace API_Khronos_FMO.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HorariosOrdinariosController : ControllerBase
    {
        private readonly HorarioOrdinarioService _horarioService;

        public HorariosOrdinariosController(
            HorarioOrdinarioService horarioService)
        {
            _horarioService = horarioService;
        }

        // POST: api/HorariosOrdinarios
        [Authorize(Roles="Administrador")]
        [HttpPost]
        public async Task<IActionResult> Registrar(
            [FromBody] HorarioOrdinarioDTO horario,
            CancellationToken cancellationToken)
        {
            try
            {
                var resultado = await _horarioService.RegistrarHorarioAsync(
                    horario, cancellationToken);

                return Ok(new
                {
                    mensaje = resultado
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (PostgresException ex)
            {
                return BadRequest(new { mensaje = ex.MessageText });
            }
        }

        // GET: api/HorariosOrdinarios
        [HttpGet]
        public async Task<IActionResult> ObtenerTodos(
            CancellationToken cancellationToken)
        {
            var horarios = await _horarioService.ObtenerTodosAsync(
                cancellationToken);

            return Ok(horarios);
        }

        // GET: api/HorariosOrdinarios/espacio/1
        [HttpGet("espacio/{idEspacio:int}")]
        public async Task<IActionResult> ObtenerPorEspacio(
            int idEspacio,
            CancellationToken cancellationToken)
        {
            if (idEspacio <= 0)
                return BadRequest("El ID del espacio no es válido.");

            var horarios = await _horarioService.ObtenerPorEspacioAsync(
                idEspacio, cancellationToken);

            return Ok(horarios);
        }

        // GET: api/HorariosOrdinarios/grupo/1
        [HttpGet("grupo/{idGrupo:int}")]
        public async Task<IActionResult> ObtenerPorGrupo(
            int idGrupo,
            CancellationToken cancellationToken)
        {
            if (idGrupo <= 0)
                return BadRequest("El ID del grupo no es válido.");

            var horarios = await _horarioService.ObtenerPorGrupoAsync(
                idGrupo, cancellationToken);

            return Ok(horarios);
        }
    }
}
