using Microsoft.AspNetCore.Authorization;
namespace API_Khronos_FMO.Controllers
{
    using API_Khronos_FMO.Services;
    using Microsoft.AspNetCore.Mvc;


    [Authorize(Roles="Administrador")]
[ApiController]
    [Route("api/[controller]")]
    public class BloqueosController : ControllerBase
    {
        private readonly EspacioService _espacioService;

        public BloqueosController(EspacioService espacioService)
        {
            _espacioService = espacioService;
        }

        // GET: api/Bloqueos
        [HttpGet]
        public async Task<IActionResult> ObtenerTodos(
            CancellationToken cancellationToken)
        {
            var bloqueos =
                await _espacioService.ObtenerTodosLosBloqueosAsync(
                    cancellationToken);

            return Ok(bloqueos);
        }
    }

}
