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
        catch { await tx.RollbackAsync(CancellationToken.None); throw; }
    }
}
