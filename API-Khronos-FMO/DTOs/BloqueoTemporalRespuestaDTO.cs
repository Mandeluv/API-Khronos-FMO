namespace API_Khronos_FMO.DTOs
{
    public class BloqueoTemporalRespuestaDTO
    {
        public int IdBloqueo { get; set; }
        public int IdEspacio { get; set; }
        public string? NombreEspacio { get; set; }
        public string? CarnetUsuario { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public string? Motivo { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
