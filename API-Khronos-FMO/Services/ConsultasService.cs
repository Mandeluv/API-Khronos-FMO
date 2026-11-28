using System.Text.Json;
using API_Khronos_FMO.Data;
using Npgsql;

namespace API_Khronos_FMO.Services;

// Consultas de solo lectura. El SQL lo define exclusivamente el servidor.
public class ConsultasService(ConexionBD conexionBD)
{
    public async Task<List<JsonElement>> ConsultarAsync(string sql, CancellationToken ct, params NpgsqlParameter[] parametros)
    {
        await using var conexion = await conexionBD.ObtenerConexionAsync(ct);
        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddRange(parametros);
        await using var reader = await comando.ExecuteReaderAsync(ct);
        var filas = new List<JsonElement>();
        while (await reader.ReadAsync(ct))
        {
            using var documento = JsonDocument.Parse(reader.GetString(0));
            filas.Add(documento.RootElement.Clone());
        }
        return filas;
    }
}
