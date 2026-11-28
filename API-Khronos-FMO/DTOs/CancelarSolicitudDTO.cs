using System.ComponentModel.DataAnnotations;

namespace API_Khronos_FMO.DTOs
{
    public class CancelarSolicitudDTO
    {
        [Required]
        [StringLength(15, MinimumLength = 1)]
        public string CarnetUsuario { get; set; } = string.Empty;
    }
}
