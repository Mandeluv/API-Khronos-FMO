namespace API_Khronos_FMO.DTOs
{
    public class RecursoEspacioDTO
    {
        public int IdEspacioRecurso { get; set; }

        public int IdEspacio { get; set; }

        public string? NombreEspacio { get; set; } = string.Empty;

        public int IdRecurso { get; set; }

        public string? Recurso { get; set; } = string.Empty;

        public string? TipoRecurso { get; set; }

        public int? IdCategoria { get; set; }

        public string? CategoriaRecurso { get; set; }

        public int? Cantidad { get; set; }
    }
}
