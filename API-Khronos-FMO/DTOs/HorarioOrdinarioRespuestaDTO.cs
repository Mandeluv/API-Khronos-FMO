namespace API_Khronos_FMO.DTOs
{
    public class HorarioOrdinarioRespuestaDTO
    {
        public int IdHorario { get; set; }

        public int IdEspacio { get; set; }

        public int IdGrupo { get; set; }

        public string DiaSemana { get; set; } = string.Empty;

        public TimeSpan HoraInicio { get; set; }

        public TimeSpan HoraFinalizacion { get; set; }

        public DateOnly FechaInicioVigencia { get; set; }

        public DateOnly FechaFinVigencia { get; set; }
    }
}
