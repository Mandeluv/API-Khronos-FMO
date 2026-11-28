using System.ComponentModel.DataAnnotations;

namespace API_Khronos_FMO.DTOs
{
    public class HorarioOrdinarioDTO
    {
        [Range(1, int.MaxValue)]
        public int IdEspacio { get; set; }

        [Range(1, int.MaxValue)]
        public int IdGrupo { get; set; }

        [Required]
        public string DiaSemana { get; set; } = string.Empty;

        public TimeSpan HoraInicio { get; set; }

        public TimeSpan HoraFinalizacion { get; set; }

        public DateOnly FechaInicioVigencia { get; set; }

        public DateOnly FechaFinVigencia { get; set; }
    }
}

