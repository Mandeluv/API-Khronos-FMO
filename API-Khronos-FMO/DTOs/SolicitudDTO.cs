namespace API_Khronos_FMO.DTOs
{
    public class SolicitudDTO
    {
        public int IdSolicitud { get; set; }

        public string CarnetUsuario { get; set; } = string.Empty;

        public string Solicitante { get; set; } = string.Empty;

        public DateTime FechaSolicitud { get; set; }

        public DateOnly Fecha { get; set; }

        public TimeSpan HoraInicio { get; set; }

        public TimeSpan HoraFin { get; set; }

        public TimeSpan Duracion { get; set; }

        public string Estado { get; set; } = string.Empty;

        public int IdEspacio { get; set; }

        public string NombreEspacio { get; set; } = string.Empty;

        public int? Capacidad { get; set; }

        public int IdGrupo { get; set; }

        public string Grupo { get; set; } = string.Empty;

        public string CodigoAsignatura { get; set; } = string.Empty;

        public string Asignatura { get; set; } = string.Empty;

        public int IdMotivo { get; set; }

        public string Motivo { get; set; } = string.Empty;

        public string Prioridad { get; set; } = string.Empty;

        public string NivelDePrioridad { get; set; } = string.Empty;

        public bool TieneConflicto { get; set; }
    }

}

