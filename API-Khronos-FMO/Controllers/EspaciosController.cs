using Microsoft.AspNetCore.Authorization;
using API_Khronos_FMO.DTOs;
using API_Khronos_FMO.Services;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace API_Khronos_FMO.Controllers
{
    using API_Khronos_FMO.DTOs;
    using API_Khronos_FMO.Services;
    using Microsoft.AspNetCore.Mvc;


    [ApiController]
    [Route("api/[controller]")]
    public class EspaciosController : ControllerBase
    {
        private readonly EspacioService _espacioService;

        public EspaciosController(EspacioService espacioService)
        {
            _espacioService = espacioService;
        }

        // GET: api/Espacios
        [HttpGet]
        public async Task<ActionResult<List<EspacioDTO>>> ObtenerEspacios(
            CancellationToken cancellationToken)
        {
            var espacios = await _espacioService.ObtenerEspaciosAsync(
                cancellationToken);

            return Ok(espacios);
        }

        // GET: api/Espacios/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<EspacioDTO>> ObtenerEspacio(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "El identificador debe ser mayor que cero."
                });
            }

            var espacio = await _espacioService.ObtenerEspacioPorIdAsync(
                id, cancellationToken);

            if (espacio == null)
            {
                return NotFound(new
                {
                    mensaje = "No se encontró el espacio solicitado."
                });
            }

            return Ok(espacio);
        }

        // GET: api/Espacios/5/recursos
        [HttpGet("{id:int}/recursos")]
        public async Task<ActionResult<List<RecursoEspacioDTO>>> ObtenerRecursos(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "El identificador debe ser mayor que cero."
                });
            }

            var espacio = await _espacioService.ObtenerEspacioPorIdAsync(
                id, cancellationToken);

            if (espacio == null)
            {
                return NotFound(new
                {
                    mensaje = "No se encontró el espacio solicitado."
                });
            }

            var recursos = await _espacioService.ObtenerRecursosAsync(
                id, cancellationToken);

            return Ok(recursos);
        }


        // POST: api/Espacios/1/bloquear
        [Authorize(Roles="Encargado de espacio")]
        [HttpPost("{id:int}/bloquear")]
        public async Task<IActionResult> BloquearEspacio(
            int id,
            [FromBody] BloquearEspacioDTO solicitud,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "El identificador debe ser mayor que cero."
                });
            }

            if (string.IsNullOrWhiteSpace(solicitud.Motivo))
            {
                return BadRequest(new
                {
                    mensaje = "Debe indicar el motivo del mantenimiento."
                });
            }

            try
            {
                var resultado =
                    await _espacioService.BloquearEspacioAsync(
                        id,
                        solicitud,
                        cancellationToken);

                return Ok(new
                {
                    mensaje = "Espacio bloqueado correctamente.",
                    resultado
                });
            }
            catch (PostgresException ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.MessageText
                });
            }
        }


        // POST: api/Espacios/1/bloqueos
        [Authorize(Roles="Encargado de espacio")]
        [HttpPost("{id:int}/bloqueos")]
        public async Task<IActionResult> RegistrarBloqueoTemporal(
            int id,
            [FromBody] BloqueoTemporalDTO solicitud,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "El identificador debe ser mayor que cero."
                });
            }

            if (solicitud.HoraInicio < TimeSpan.Zero ||
                solicitud.HoraInicio >= TimeSpan.FromDays(1) ||
                solicitud.HoraFin <= TimeSpan.Zero ||
                solicitud.HoraFin >= TimeSpan.FromDays(1) ||
                solicitud.HoraFin <= solicitud.HoraInicio)
            {
                return BadRequest(new
                {
                    mensaje = "El horario proporcionado no es válido."
                });
            }

            if (solicitud.Fecha == default)
            {
                return BadRequest(new
                {
                    mensaje = "Debe proporcionar una fecha válida."
                });
            }

            if (string.IsNullOrWhiteSpace(solicitud.Motivo))
            {
                return BadRequest(new
                {
                    mensaje = "Debe indicar el motivo del bloqueo."
                });
            }

            try
            {
                var idBloqueo =
                    await _espacioService.RegistrarBloqueoTemporalAsync(
                        id, solicitud, cancellationToken);

                return Ok(new
                {
                    mensaje = "Bloqueo temporal registrado correctamente.",
                    idBloqueo
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
            catch (PostgresException ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.MessageText
                });
            }
        }

        // GET: api/Espacios/1/bloqueos
        [HttpGet("{id:int}/bloqueos")]
        public async Task<IActionResult> ObtenerBloqueosPorEspacio(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "El identificador debe ser mayor que cero."
                });
            }

            var bloqueos =
                await _espacioService.ObtenerBloqueosPorEspacioAsync(
                    id, cancellationToken);

            return Ok(bloqueos);
        }



    }

}
