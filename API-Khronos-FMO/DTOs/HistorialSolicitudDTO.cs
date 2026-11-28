namespace API_Khronos_FMO.DTOs
{
    public class HistorialSolicitudDTO
    {
        public int IdHistorial { get; set; }

        public DateOnly? Fecha { get; set; }

        public DateTime FechaHora { get; set; }

        public string? Donde { get; set; }

        public string? Modificacion { get; set; }

        public string? Motivo { get; set; }

        public string? CarnetUsuario { get; set; }

        public int? IdSolicitud { get; set; }
    }
}
