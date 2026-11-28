using System.ComponentModel.DataAnnotations;

namespace API_Khronos_FMO.DTOs
{
    public class RegistrarSolicitudDTO
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

        [Range(1, int.MaxValue)]
        public int IdEspacio { get; set; }

        [Range(1, int.MaxValue)]
        public int IdMotivo { get; set; }

        [Range(1, int.MaxValue)]
        public int IdGrupo { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string CodigoAsignatura { get; set; } = string.Empty;
    }
}