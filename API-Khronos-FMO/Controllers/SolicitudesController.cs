using Microsoft.AspNetCore.Authorization;
using API_Khronos_FMO.DTOs;
using API_Khronos_FMO.Services;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace API_Khronos_FMO.Controllers
{
  [ApiController]
        [Route("api/[controller]")]
        public class SolicitudesController : ControllerBase
        {
            private readonly SolicitudService _solicitudService;

            public SolicitudesController(SolicitudService solicitudService)
            {
                _solicitudService = solicitudService;
            }

            // GET: api/Solicitudes/pendientes
            [Authorize(Roles="Administrador,Encargado de espacio")]
            [HttpGet("pendientes")]
            public async Task<ActionResult<List<SolicitudDTO>>> ObtenerPendientes(
                CancellationToken cancellationToken)
            {
                var solicitudes = await _solicitudService.ObtenerPendientesAsync(
                    cancellationToken);

                return Ok(solicitudes);
            }


        // POST: api/Solicitudes
        [Authorize(Roles="Docente")]
        [HttpPost]
        public async Task<IActionResult> RegistrarSolicitud(
            [FromBody] RegistrarSolicitudDTO solicitud,
            CancellationToken cancellationToken)
        {
            if (solicitud.Fecha == default) //puede que esto deba moverlo 
            {
                return BadRequest(new
                {
                    mensaje = "Debe proporcionar una fecha válida."
                });
            }

            if (solicitud.HoraInicio < TimeSpan.Zero ||
                solicitud.HoraInicio >= TimeSpan.FromDays(1) ||
                solicitud.HoraFin <= TimeSpan.Zero ||
                solicitud.HoraFin >= TimeSpan.FromDays(1))
            {
                return BadRequest(new
                {
                    mensaje = "Las horas deben estar dentro del día."
                });
            }

            if (solicitud.HoraFin <= solicitud.HoraInicio)
            {
                return BadRequest(new
                {
                    mensaje = "La hora final debe ser mayor que la inicial."
                });
            }

            try
            {
                var resultado =
                    await _solicitudService.RegistrarSolicitudAsync(
                        solicitud, cancellationToken);

                return Ok(new
                {
                    mensaje = "Solicitud registrada correctamente.",
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


        // POST: api/Solicitudes/5/aprobar
        [Authorize(Roles="Encargado de espacio")]
        [HttpPost("{id:int}/aprobar")]
        public async Task<IActionResult> AprobarSolicitud(
            int id,
            [FromBody] ResolverSolicitudDTO solicitud,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "El identificador debe ser mayor que cero."
                });
            }

            try
            {
                var resultado =
                    await _solicitudService.ResolverSolicitudAsync(
                        id,
                        solicitud.CarnetUsuario,
                        "APROBADA",
                        null,
                        cancellationToken);

                return Ok(new
                {
                    mensaje = "Solicitud aprobada correctamente.",
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

        // POST: api/Solicitudes/5/rechazar
        [Authorize(Roles="Encargado de espacio")]
        [HttpPost("{id:int}/rechazar")]
        public async Task<IActionResult> RechazarSolicitud(
            int id,
            [FromBody] ResolverSolicitudDTO solicitud,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "El identificador debe ser mayor que cero."
                });
            }

            if (string.IsNullOrWhiteSpace(solicitud.MotivoRechazo))
            {
                return BadRequest(new
                {
                    mensaje = "Debe indicar el motivo del rechazo."
                });
            }

            try
            {
                var resultado =
                    await _solicitudService.ResolverSolicitudAsync(
                        id,
                        solicitud.CarnetUsuario,
                        "RECHAZADA",
                        solicitud.MotivoRechazo.Trim(),
                        cancellationToken);

                return Ok(new
                {
                    mensaje = "Solicitud rechazada correctamente.",
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


        // POST: api/Solicitudes/5/cancelar
        [HttpPost("{id:int}/cancelar")]
        public async Task<IActionResult> CancelarSolicitud(
            int id,
            [FromBody] CancelarSolicitudDTO solicitud,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "El identificador debe ser mayor que cero."
                });
            }

            try
            {
                var resultado =
                    await _solicitudService.CancelarSolicitudAsync(
                        id,
                        solicitud.CarnetUsuario,
                        cancellationToken);

                return Ok(new
                {
                    mensaje = "Solicitud cancelada correctamente.",
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



        // GET: api/Solicitudes/5/historial
        [Authorize(Roles="Administrador,Encargado de espacio")]
        [HttpGet("{id:int}/historial")]
        public async Task<IActionResult> ObtenerHistorial(
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

            var historial =
                await _solicitudService.ObtenerHistorialAsync(
                    id, cancellationToken);

            return Ok(historial);
        }



    }
}
