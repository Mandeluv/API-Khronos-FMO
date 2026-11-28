
using API_Khronos_FMO.Data;
using API_Khronos_FMO.DTOs;
using Npgsql;
using NpgsqlTypes;

namespace API_Khronos_FMO.Services
{
    public class HorarioOrdinarioService
    {
        private readonly ConexionBD _conexionBD;

        public HorarioOrdinarioService(ConexionBD conexionBD)
        {
            _conexionBD = conexionBD;
        }

        public async Task<string> RegistrarHorarioAsync(
            HorarioOrdinarioDTO horario,
            CancellationToken cancellationToken = default)
        {
            string[] diasValidos =
            {
                "Lunes", "Martes", "Miercoles",
                "Jueves", "Viernes", "Sabado"
            };

            if (!diasValidos.Contains(horario.DiaSemana))
                throw new ArgumentException("El día de la semana no es válido.");

            if (horario.HoraInicio < TimeSpan.Zero || horario.HoraFinalizacion >= TimeSpan.FromDays(1) ||
                horario.HoraFinalizacion <= horario.HoraInicio)
                throw new ArgumentException(
                    "La hora de finalización debe ser mayor que la hora de inicio.");

            if (horario.FechaInicioVigencia == default || horario.FechaFinVigencia == default ||
                horario.FechaFinVigencia < horario.FechaInicioVigencia)
                throw new ArgumentException(
                    "La fecha final debe ser mayor o igual a la fecha inicial.");

            await using var conexion =
                await _conexionBD.ObtenerConexionAsync(cancellationToken);

            await using var transaccion = await conexion.BeginTransactionAsync(cancellationToken);
            try
            {
                await using (var lockCmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(4102026)", conexion, transaccion))
                    await lockCmd.ExecuteNonQueryAsync(cancellationToken);
                await using var comando = new NpgsqlCommand(

                    @"CALL cargar_horario_ordinario(
                        @id_espacio,
                        @id_grupo,
                        @dia_semana::tipo_dia_semana,
                        @hora_inicio,
                        @hora_finalizacion,
                        @fecha_inicio,
                        @fecha_fin,
                        NULL
                    )",
                    conexion, transaccion);

                comando.Parameters.AddWithValue(
                    "id_espacio", horario.IdEspacio);

                comando.Parameters.AddWithValue(
                    "id_grupo", horario.IdGrupo);


                comando.Parameters.AddWithValue(
                    "dia_semana",
                    horario.DiaSemana);


                comando.Parameters.AddWithValue(
                    "hora_inicio", horario.HoraInicio);

                comando.Parameters.AddWithValue(
                    "hora_finalizacion", horario.HoraFinalizacion);

                comando.Parameters.AddWithValue(
                    "fecha_inicio", horario.FechaInicioVigencia);

                comando.Parameters.AddWithValue(
                    "fecha_fin", horario.FechaFinVigencia);

                string resultado;
                await using (var reader = await comando.ExecuteReaderAsync(cancellationToken))
                {
                    if (!await reader.ReadAsync(cancellationToken) || reader.IsDBNull(0))
                        throw new InvalidOperationException("El procedimiento no devolvió un resultado.");
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

        public async Task<List<HorarioOrdinarioRespuestaDTO>>
            ObtenerTodosAsync(CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT
                    id_horario,
                    id_espacio,
                    id_grupo,
                    dia_semana::TEXT,
                    hora_inicio,
                    hora_finalizacion,
                    fecha_inicio_vigencia,
                    fecha_fin_vigencia
                FROM tbl_horario_ordinario
                ORDER BY id_horario;";

            return await ConsultarHorariosAsync(sql, null, cancellationToken);
        }

        public async Task<List<HorarioOrdinarioRespuestaDTO>>
            ObtenerPorEspacioAsync(
                int idEspacio,
                CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT
                    id_horario,
                    id_espacio,
                    id_grupo,
                    dia_semana::TEXT,
                    hora_inicio,
                    hora_finalizacion,
                    fecha_inicio_vigencia,
                    fecha_fin_vigencia
                FROM tbl_horario_ordinario
                WHERE id_espacio = @id
                ORDER BY id_horario;";

            return await ConsultarHorariosAsync(
                sql, idEspacio, cancellationToken);
        }

        public async Task<List<HorarioOrdinarioRespuestaDTO>>
            ObtenerPorGrupoAsync(
                int idGrupo,
                CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT
                    id_horario,
                    id_espacio,
                    id_grupo,
                    dia_semana::TEXT,
                    hora_inicio,
                    hora_finalizacion,
                    fecha_inicio_vigencia,
                    fecha_fin_vigencia
                FROM tbl_horario_ordinario
                WHERE id_grupo = @id
                ORDER BY id_horario;";

            return await ConsultarHorariosAsync(
                sql, idGrupo, cancellationToken);
        }

        private async Task<List<HorarioOrdinarioRespuestaDTO>>
            ConsultarHorariosAsync(
                string sql,
                int? id,
                CancellationToken cancellationToken)
        {
            var horarios = new List<HorarioOrdinarioRespuestaDTO>();

            await using var conexion =
                await _conexionBD.ObtenerConexionAsync(cancellationToken);

            await using var comando = new NpgsqlCommand(sql, conexion);

            if (id.HasValue)
                comando.Parameters.AddWithValue("id", id.Value);

            await using var reader =
                await comando.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                horarios.Add(new HorarioOrdinarioRespuestaDTO
                {
                    IdHorario = reader.GetInt32(0),
                    IdEspacio = reader.GetInt32(1),
                    IdGrupo = reader.GetInt32(2),
                    DiaSemana = reader.GetString(3),
                    HoraInicio = reader.GetTimeSpan(4),
                    HoraFinalizacion = reader.GetTimeSpan(5),
                    FechaInicioVigencia = reader.GetFieldValue<DateOnly>(6),
                    FechaFinVigencia = reader.GetFieldValue<DateOnly>(7)
                });
            }

            return horarios;
        }
    }
}
