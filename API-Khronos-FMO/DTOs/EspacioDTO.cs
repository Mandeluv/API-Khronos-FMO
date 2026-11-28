namespace API_Khronos_FMO.DTOs
{
    public class EspacioDTO
    {
        public int IdEspacio { get; set; }

        public string? NombreEspacio { get; set; }

        public int? Capacidad { get; set; }

        public string? Estado { get; set; }

        public int? IdUbicacion { get; set; }

        public string? Ubicacion { get; set; }

        public string? CarnetResponsable { get; set; }

        public string? Responsable { get; set; }

        public string Categorias { get; set; } = string.Empty;
    }
}
