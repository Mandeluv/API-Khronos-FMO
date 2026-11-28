namespace API_Khronos_FMO.Data
{
    using Npgsql;

    public class ConexionBD
    {
        private readonly NpgsqlDataSource _dataSource;

        public ConexionBD(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public ValueTask<NpgsqlConnection> ObtenerConexionAsync(
            CancellationToken cancellationToken = default)
        {
            return _dataSource.OpenConnectionAsync(cancellationToken);
        }
    }

}
