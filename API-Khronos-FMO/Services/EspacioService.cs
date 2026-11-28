namespace API_Khronos_FMO.Services
{
    using API_Khronos_FMO.Data;
    using API_Khronos_FMO.DTOs;
    using Npgsql;
    using NpgsqlTypes;

    public class EspacioService
    {
        private readonly ConexionBD _conexionBD;

        public EspacioService(ConexionBD conexionBD)
        {
            _conexionBD = conexionBD;
        }

        // Obtener todos los espacios
        public async Task<List<EspacioDTO>> ObtenerEspaciosAsync(
            CancellationToken cancellationToken = default)
        {
            var espacios = new List<EspacioDTO>();

            await using var conexion =
                await _conexionBD.ObtenerConexionAsync(cancellationToken);

            const string sql = """
            SELECT
                id_espacio,
                nombre_espacio,
                capacidad,
                estado::text AS estado,
                id_ubicacion,
                ubicacion,
                carnet_responsable,
                responsable,
                categorias
            FROM v_espacios_detalle
            ORDER BY nombre_espacio;
            """;

            await using var comando = new NpgsqlCommand(sql, conexion);

            await using var reader =
                await comando.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                espacios.Add(MapearEspacio(reader));
            }

            return espacios;
        }

        // Obtener un espacio por su ID
        public async Task<EspacioDTO?> ObtenerEspacioPorIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            await using var conexion =
                await _conexionBD.ObtenerConexionAsync(cancellationToken);

            const string sql = """
            SELECT
                id_espacio,
                nombre_espacio,
                capacidad,
                estado::text AS estado,
                id_ubicacion,
                ubicacion,
                carnet_responsable,
                responsable,
                categorias
            FROM v_espacios_detalle
            WHERE id_espacio = @id;
            """;

            await using var comando = new NpgsqlCommand(sql, conexion);

            comando.Parameters.AddWithValue("id", id);

            await using var reader =
                await comando.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return MapearEspacio(reader);
            }

            return null;
        }

        // Obtener los recursos de un espacio
        public async Task<List<RecursoEspacioDTO>> ObtenerRecursosAsync(
            int idEspacio,
            CancellationToken cancellationToken = default)
        {
            var recursos = new List<RecursoEspacioDTO>();

            await using var conexion =
                await _conexionBD.ObtenerConexionAsync(cancellationToken);

            const string sql = """
            SELECT
                id_espacio_recurso,
                id_espacio,
                nombre_espacio,
                id_recurso,
                recurso,
                tipo_recurso,
                id_categoria,
                categoria_recurso,
                cantidad
            FROM v_recursos_por_espacio
            WHERE id_espacio = @idEspacio
            ORDER BY recurso;
            """;

            await using var comando = new NpgsqlCommand(sql, conexion);

            comando.Parameters.AddWithValue("idEspacio", idEspacio);

            await using var reader =
                await comando.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                recursos.Add(new RecursoEspacioDTO
                {
                    IdEspacioRecurso = reader.GetInt32(0),
                    IdEspacio = reader.GetInt32(1),
                    NombreEspacio = reader.IsDBNull(2)
                        ? null : reader.GetString(2),
                    IdRecurso = reader.GetInt32(3),
                    Recurso = reader.IsDBNull(4)
                        ? null : reader.GetString(4),
                    TipoRecurso = reader.IsDBNull(5)
                        ? null : reader.GetString(5),
                    IdCategoria = reader.IsDBNull(6)
                        ? null : reader.GetInt32(6),
                    CategoriaRecurso = reader.IsDBNull(7)
                        ? null : reader.GetString(7),
                    Cantidad = reader.IsDBNull(8)
                        ? null : reader.GetInt32(8)
                });
            }

            return recursos;
        }

        // Convertir el resultado de PostgreSQL en un DTO
        private static EspacioDTO MapearEspacio(NpgsqlDataReader reader)
        {
            return new EspacioDTO
            {
                IdEspacio = reader.GetInt32(0),
                NombreEspacio = reader.IsDBNull(1)
                    ? null : reader.GetString(1),
                Capacidad = reader.IsDBNull(2)
                    ? null : reader.GetInt32(2),
                Estado = reader.IsDBNull(3)
                    ? null : reader.GetString(3),
                IdUbicacion = reader.IsDBNull(4)
                    ? null : reader.GetInt32(4),
                Ubicacion = reader.IsDBNull(5)
                    ? null : reader.GetString(5),
                CarnetResponsable = reader.IsDBNull(6)
                    ? null : reader.GetString(6),
                Responsable = reader.IsDBNull(7)
                    ? null : reader.GetString(7),
                Categorias = reader.GetString(8)
            };
        }


        public async Task<string> BloquearEspacioAsync(
            int idEspacio,
            BloquearEspacioDTO solicitud,
            CancellationToken cancellationToken = default)
        {
            await using var conexion =
                await _conexionBD.ObtenerConexionAsync(cancellationToken);

            await using var transaccion =
                await conexion.BeginTransactionAsync(cancellationToken);

            try
            {
                await using (var lockCmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(4102026)", conexion, transaccion))
                    await lockCmd.ExecuteNonQueryAsync(cancellationToken);
                const string sql = """
            CALL bloquear_espacio(
                @carnetUsuario::varchar(15),
                @idEspacio::integer,
                @motivo::varchar(200),
                NULL::text
            );
            """;

                await using var comando = new NpgsqlCommand(
                    sql,
                    conexion,
                    transaccion);

                comando.Parameters.Add(
                    new NpgsqlParameter(
                        "carnetUsuario", NpgsqlDbType.Varchar)
                    {
                        Value = solicitud.CarnetUsuario
                    });

                comando.Parameters.Add(
                    new NpgsqlParameter(
                        "idEspacio", NpgsqlDbType.Integer)
                    {
                        Value = idEspacio
                    });

                comando.Parameters.Add(
                    new NpgsqlParameter(
                        "motivo", NpgsqlDbType.Varchar)
                    {
                        Value = solicitud.Motivo.Trim()
                    });

                string resultado;

                await using (var reader =
                    await comando.ExecuteReaderAsync(cancellationToken))
                {
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        throw new InvalidOperationException(
                            "El procedimiento no devolvió un resultado.");
                    }

                    resultado = reader.GetString(0);
                }

                await transaccion.CommitAsync(cancellationToken);

                return resultado;
            }
            catch
            {
                await transaccion.RollbackAsync(CancellationToken.None);
                throw;
            }
        }


        public async Task<int> RegistrarBloqueoTemporalAsync(
            int idEspacio,
            BloqueoTemporalDTO solicitud,
            CancellationToken cancellationToken = default)
        {
            await using var conexion =
                await _conexionBD.ObtenerConexionAsync(cancellationToken);

            await using var transaccion =
                await conexion.BeginTransactionAsync(cancellationToken);

            try
            {
                await using (var lockCmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(4102026)", conexion, transaccion))
                    await lockCmd.ExecuteNonQueryAsync(cancellationToken);
                const string sqlValidar = """
            SELECT 1
            FROM tbl_espacios
            WHERE id_espacio = @idEspacio
              AND carnet_usuario = @carnetUsuario
            FOR UPDATE;
            """;

                await using var comandoValidar = new NpgsqlCommand(
                    sqlValidar, conexion, transaccion);

                comandoValidar.Parameters.Add(
                    new NpgsqlParameter("idEspacio", NpgsqlDbType.Integer)
                    {
                        Value = idEspacio
                    });

                comandoValidar.Parameters.Add(
                    new NpgsqlParameter("carnetUsuario", NpgsqlDbType.Varchar)
                    {
                        Value = solicitud.CarnetUsuario
                    });

                var existeEncargado =
                    await comandoValidar.ExecuteScalarAsync(cancellationToken);

                if (existeEncargado == null)
                {
                    throw new InvalidOperationException(
                        "El espacio no existe o el usuario no es su encargado.");
                }

                const string sqlInsertar = """
            INSERT INTO tbl_bloqueo_temporal
            (
                id_espacio,
                carnet_usuario,
                fecha,
                hora_inicio,
                hora_fin,
                motivo
            )
            VALUES
            (
                @idEspacio,
                @carnetUsuario,
                @fecha,
                @horaInicio,
                @horaFin,
                @motivo
            )
            RETURNING id_bloqueo;
            """;

                await using var comandoInsertar = new NpgsqlCommand(
                    sqlInsertar, conexion, transaccion);

                comandoInsertar.Parameters.AddWithValue(
                    "idEspacio", NpgsqlDbType.Integer, idEspacio);

                comandoInsertar.Parameters.AddWithValue(
                    "carnetUsuario", NpgsqlDbType.Varchar,
                    solicitud.CarnetUsuario);

                comandoInsertar.Parameters.AddWithValue(
                    "fecha", NpgsqlDbType.Date, solicitud.Fecha);

                comandoInsertar.Parameters.AddWithValue(
                    "horaInicio", NpgsqlDbType.Time, solicitud.HoraInicio);

                comandoInsertar.Parameters.AddWithValue(
                    "horaFin", NpgsqlDbType.Time, solicitud.HoraFin);

                comandoInsertar.Parameters.AddWithValue(
                    "motivo", NpgsqlDbType.Varchar,
                    solicitud.Motivo.Trim());

                var idBloqueo = (int)(await comandoInsertar.ExecuteScalarAsync(
                    cancellationToken))!;

                await transaccion.CommitAsync(cancellationToken);

                return idBloqueo;
            }
            catch
            {
                await transaccion.RollbackAsync(CancellationToken.None);
                throw;
            }
        }


        public async Task<List<BloqueoTemporalRespuestaDTO>>
            ObtenerBloqueosPorEspacioAsync(
                int idEspacio,
                CancellationToken cancellationToken = default)
        {
            var bloqueos = new List<BloqueoTemporalRespuestaDTO>();

            await using var conexion =
                await _conexionBD.ObtenerConexionAsync(cancellationToken);

            const string sql = """
        SELECT
            b.id_bloqueo,
            b.id_espacio,
            e.nombre_espacio,
            b.carnet_usuario,
            b.fecha,
            b.hora_inicio,
            b.hora_fin,
            b.motivo,
            b.fecha_registro
        FROM tbl_bloqueo_temporal b
        INNER JOIN tbl_espacios e
            ON e.id_espacio = b.id_espacio
        WHERE b.id_espacio = @idEspacio
        ORDER BY b.fecha DESC, b.hora_inicio;
        """;

            await using var comando = new NpgsqlCommand(sql, conexion);

            comando.Parameters.AddWithValue(
                "idEspacio", NpgsqlDbType.Integer, idEspacio);

            await using var reader =
                await comando.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                bloqueos.Add(new BloqueoTemporalRespuestaDTO
                {
                    IdBloqueo = reader.GetInt32(0),
                    IdEspacio = reader.GetInt32(1),
                    NombreEspacio = reader.IsDBNull(2) ? null : reader.GetString(2),
                    CarnetUsuario = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Fecha = reader.GetFieldValue<DateOnly>(4),
                    HoraInicio = reader.GetTimeSpan(5),
                    HoraFin = reader.GetTimeSpan(6),
                    Motivo = reader.IsDBNull(7) ? null : reader.GetString(7),
                    FechaRegistro = reader.GetDateTime(8)
                });
            }

            return bloqueos;
        }

        public async Task<List<BloqueoTemporalRespuestaDTO>>
            ObtenerTodosLosBloqueosAsync(
                CancellationToken cancellationToken = default)
        {
            var bloqueos = new List<BloqueoTemporalRespuestaDTO>();

            await using var conexion =
                await _conexionBD.ObtenerConexionAsync(cancellationToken);

            const string sql = """
        SELECT
            b.id_bloqueo,
            b.id_espacio,
            e.nombre_espacio,
            b.carnet_usuario,
            b.fecha,
            b.hora_inicio,
            b.hora_fin,
            b.motivo,
            b.fecha_registro
        FROM tbl_bloqueo_temporal b
        INNER JOIN tbl_espacios e
            ON e.id_espacio = b.id_espacio
        ORDER BY b.fecha DESC, b.hora_inicio;
        """;

            await using var comando = new NpgsqlCommand(sql, conexion);

            await using var reader =
                await comando.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                bloqueos.Add(new BloqueoTemporalRespuestaDTO
                {
                    IdBloqueo = reader.GetInt32(0),
                    IdEspacio = reader.GetInt32(1),
                    NombreEspacio = reader.IsDBNull(2) ? null : reader.GetString(2),
                    CarnetUsuario = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Fecha = reader.GetFieldValue<DateOnly>(4),
                    HoraInicio = reader.GetTimeSpan(5),
                    HoraFin = reader.GetTimeSpan(6),
                    Motivo = reader.IsDBNull(7) ? null : reader.GetString(7),
                    FechaRegistro = reader.GetDateTime(8)
                });
            }

            return bloqueos;
        }



    }

}
