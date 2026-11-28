using API_Khronos_FMO.Data;
using API_Khronos_FMO.DTOs;
using NpgsqlTypes;
using Npgsql;

namespace API_Khronos_FMO.Services
{
    public class SolicitudService
    {
        private readonly ConexionBD _conexionBD;

        public SolicitudService(ConexionBD conexionBD)
        {
            _conexionBD = conexionBD;
        }

        public async Task<List<SolicitudDTO>> ObtenerPendientesAsync(
            CancellationToken cancellationToken = default)
        {
            var solicitudes = new List<SolicitudDTO>();

            await using var conexion =
                await _conexionBD.ObtenerConexionAsync(cancellationToken);

            const string sql = """
            SELECT
                id_solicitud,
                carnet_usuario,
                solicitante,
                fecha_solicitud,
                fecha,
                hora_inicio,
                hora_fin,
                duracion,
                estado::text,
                id_espacio,
                nombre_espacio,
                capacidad,
                id_grupo,
                grupo,
                codigo_asignatura,
                asignatura,
                id_motivo,
                motivo,
                prioridad,
                nivel_de_prioridad,
                tiene_conflicto
            FROM v_solicitudes_pendientes
            ORDER BY fecha, hora_inicio;
            """;

            await using var comando = new NpgsqlCommand(sql, conexion);

            await using var reader =
                await comando.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                solicitudes.Add(new SolicitudDTO
                {
                    IdSolicitud = reader.GetInt32(0),
                    CarnetUsuario = reader.GetString(1),
                    Solicitante = reader.GetString(2),
                    FechaSolicitud = reader.GetDateTime(3),
                    Fecha = reader.GetFieldValue<DateOnly>(4),
                    HoraInicio = reader.GetFieldValue<TimeSpan>(5),
                    HoraFin = reader.GetFieldValue<TimeSpan>(6),
                    Duracion = reader.GetFieldValue<TimeSpan>(7),
                    Estado = reader.GetString(8),
                    IdEspacio = reader.GetInt32(9),
                    NombreEspacio = reader.GetString(10),
                    Capacidad = reader.IsDBNull(11)
                        ? null : reader.GetInt32(11),
                    IdGrupo = reader.GetInt32(12),
                    Grupo = reader.GetString(13),
                    CodigoAsignatura = reader.GetString(14),
                    Asignatura = reader.GetString(15),
                    IdMotivo = reader.GetInt32(16),
                    Motivo = reader.GetString(17),
                    Prioridad = reader.GetString(18),
                    NivelDePrioridad = reader.GetString(19),
                    TieneConflicto = reader.GetBoolean(20)
                });
            }

            return solicitudes;
        }


        public async Task<string> RegistrarSolicitudAsync(
      RegistrarSolicitudDTO solicitud,
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
                await using (var verificar = new NpgsqlCommand("SELECT 1 FROM tbl_grupos WHERE id_grupo=@g AND carnet_usuario=@u", conexion, transaccion))
                {
                    verificar.Parameters.AddWithValue("g", solicitud.IdGrupo);
                    verificar.Parameters.AddWithValue("u", solicitud.CarnetUsuario);
                    if (await verificar.ExecuteScalarAsync(cancellationToken) is null) throw new PostgresException("El grupo no pertenece al solicitante.", "ERROR", "ERROR", "P0001");
                }
                const string sql = """
                CALL registrar_solicitud_reserva(
                    @carnetUsuario::varchar(15),
                    @fecha::date,
                    @horaInicio::time,
                    @horaFin::time,
                    @idEspacio::integer,
                    @idMotivo::integer,
                    @idGrupo::integer,
                    @codigoAsignatura::varchar(100),   
                    NULL::text
                );
                """;

                await using var comando = new NpgsqlCommand(
                    sql,
                    conexion,
                    transaccion);

                comando.Parameters.Add(
                    new NpgsqlParameter("carnetUsuario", NpgsqlDbType.Varchar)
                    {
                        Value = solicitud.CarnetUsuario
                    });

                comando.Parameters.Add(
                    new NpgsqlParameter("fecha", NpgsqlDbType.Date)
                    {
                        Value = solicitud.Fecha
                    });

                comando.Parameters.Add(
                    new NpgsqlParameter("horaInicio", NpgsqlDbType.Time)
                    {
                        Value = solicitud.HoraInicio
                    });

                comando.Parameters.Add(
                    new NpgsqlParameter("horaFin", NpgsqlDbType.Time)
                    {
                        Value = solicitud.HoraFin
                    });

                comando.Parameters.Add(
                    new NpgsqlParameter("idEspacio", NpgsqlDbType.Integer)
                    {
                        Value = solicitud.IdEspacio
                    });

                comando.Parameters.Add(
                    new NpgsqlParameter("idMotivo", NpgsqlDbType.Integer)
                    {
                        Value = solicitud.IdMotivo
                    });

                comando.Parameters.Add(
                    new NpgsqlParameter("idGrupo", NpgsqlDbType.Integer)
                    {
                        Value = solicitud.IdGrupo
                    });
                comando.Parameters.Add(
                    new NpgsqlParameter("codigoAsignatura", NpgsqlDbType.Varchar)
                    {
                        Value = solicitud.CodigoAsignatura
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


        public async Task<string> ResolverSolicitudAsync(
            int idSolicitud,
            string carnetUsuario,
            string decision,
            string? motivoRechazo,
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
            CALL aprobar_rechazar_solicitud(
                @carnetUsuario::varchar(15),
                @idSolicitud::integer,
                @decision::tipo_estado_solicitud,
                @motivoRechazo::text,
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
                        Value = carnetUsuario
                    });

                comando.Parameters.Add(
                    new NpgsqlParameter(
                        "idSolicitud", NpgsqlDbType.Integer)
                    {
                        Value = idSolicitud
                    });

                comando.Parameters.Add(
                    new NpgsqlParameter(
                        "decision", NpgsqlDbType.Text)
                    {
                        Value = decision
                    });

                comando.Parameters.Add(
                    new NpgsqlParameter(
                        "motivoRechazo", NpgsqlDbType.Text)
                    {
                        Value = (object?)motivoRechazo ?? DBNull.Value
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


        public async Task<string> CancelarSolicitudAsync(
            int idSolicitud,
            string carnetUsuario,
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
            CALL cancelar_solicitud(
                @carnetUsuario::varchar(15),
                @idSolicitud::integer,
                NULL::text
            );
            """;

                await using var comando = new NpgsqlCommand(
                    sql,
                    conexion,
                    transaccion);

                comando.Parameters.Add(
                    new NpgsqlParameter(
                        "carnetUsuario",
                        NpgsqlDbType.Varchar)
                    {
                        Value = carnetUsuario
                    });

                comando.Parameters.Add(
                    new NpgsqlParameter(
                        "idSolicitud",
                        NpgsqlDbType.Integer)
                    {
                        Value = idSolicitud
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


        public async Task<List<HistorialSolicitudDTO>>
            ObtenerHistorialAsync(
                int idSolicitud,
                CancellationToken cancellationToken = default)
        {
            var historial = new List<HistorialSolicitudDTO>();

            await using var conexion =
                await _conexionBD.ObtenerConexionAsync(cancellationToken);

            const string sql = """
        SELECT
            id_historial,
            fecha,
            fecha_hora,
            donde,
            modificacion,
            motivo,
            carnet_usuario,
            id_solicitud
        FROM tbl_historial
        WHERE id_solicitud = @idSolicitud
        ORDER BY fecha_hora ASC;
        """;

            await using var comando = new NpgsqlCommand(sql, conexion);

            comando.Parameters.Add(
                new NpgsqlParameter(
                    "idSolicitud",
                    NpgsqlDbType.Integer)
                {
                    Value = idSolicitud
                });

            await using var reader =
                await comando.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                historial.Add(new HistorialSolicitudDTO
                {
                    IdHistorial = reader.GetInt32(0),

                    Fecha = reader.IsDBNull(1)
                        ? null
                        : reader.GetFieldValue<DateOnly>(1),

                    FechaHora = reader.GetDateTime(2),

                    Donde = reader.IsDBNull(3)
                        ? null : reader.GetString(3),

                    Modificacion = reader.IsDBNull(4)
                        ? null : reader.GetString(4),

                    Motivo = reader.IsDBNull(5)
                        ? null : reader.GetString(5),

                    CarnetUsuario = reader.IsDBNull(6)
                        ? null : reader.GetString(6),

                    IdSolicitud = reader.IsDBNull(7)
                        ? null : reader.GetInt32(7)
                });
            }

            return historial;
        }



    }
}
