using System.ComponentModel.DataAnnotations;

namespace API_Khronos_FMO.DTOs
{
    public class ResolverSolicitudDTO
    {
        [Required]
        [StringLength(15, MinimumLength = 1)]
        public string CarnetUsuario { get; set; } = string.Empty;

        [StringLength(200)]
        public string? MotivoRechazo { get; set; }
    }

}

