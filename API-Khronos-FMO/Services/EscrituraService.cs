using API_Khronos_FMO.Data;
using Npgsql;
using System.Data;

namespace API_Khronos_FMO.Services;

public class EscrituraService(ConexionBD bd)
{
    public async Task<object?> EjecutarAsync(string sql, CancellationToken ct, params NpgsqlParameter[] args)
    {
        await using var c = await bd.ObtenerConexionAsync(ct);
        await using var tx = await c.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            // Orden común para las operaciones que alteran la planificación.
            await using (var bloqueo = new NpgsqlCommand("SELECT pg_advisory_xact_lock(4102026)", c, tx))
                await bloqueo.ExecuteNonQueryAsync(ct);

            await using var cmd = new NpgsqlCommand(sql, c, tx);
            cmd.Parameters.AddRange(args);
            var resultado = await cmd.ExecuteScalarAsync(ct);
            await tx.CommitAsync(ct);
            return resultado is DBNull ? null : resultado;
        }
        catch (PostgresException ex)
        {
            await tx.RollbackAsync(CancellationToken.None);

            // 23505: Violación de restricción UNIQUE (ej. correo duplicado)
            if (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                var mensaje = ex.ConstraintName switch
                {
                    "tbl_perfil_correo_key" => "El correo electrónico ya se encuentra registrado.",
                    "tbl_perfil_telefono_key" => "El número de teléfono ya se encuentra registrado.",
                    _ => "Ya existe un registro con esos datos únicos."
                };

                throw new InvalidOperationException(mensaje, ex);
            }


            // 23503: Violación de llave foránea (ej. ID relacionado no existe)
            if (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                throw new ArgumentException("Uno de los registros relacionados no existe.", ex);
            }

            // Otros errores lanzados desde PostgreSQL (ej. RAISE EXCEPTION)
            throw new InvalidOperationException(ex.MessageText, ex);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
