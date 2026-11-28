using System.ComponentModel.DataAnnotations;

namespace API_Khronos_FMO.DTOs
{
    public class BloquearEspacioDTO
    {
        [Required]
        [StringLength(15, MinimumLength = 1)]
        public string CarnetUsuario { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Motivo { get; set; } = string.Empty;
    }
}
