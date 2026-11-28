using System.ComponentModel.DataAnnotations;

namespace API_Khronos_FMO.DTOs
{
    public class BloqueoTemporalDTO
    {
        [Required]
        [StringLength(15, MinimumLength = 1)]
        public string CarnetUsuario { get; set; } = string.Empty;

        [Required]
        public DateOnly Fecha { get; set; }

        [Required]
        public TimeSpan HoraInicio { get; set; }

        [Required]
        public TimeSpan HoraFin { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Motivo { get; set; } = string.Empty;
    }
}
