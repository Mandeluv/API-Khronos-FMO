
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace API_Khronos_FMO.Services;

public sealed class PostgresExceptionHandler : IExceptionHandler
{
    private readonly ILogger<PostgresExceptionHandler> _logger;

    public PostgresExceptionHandler(
        ILogger<PostgresExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Comprobar si el error proviene de PostgreSQL.
        if (exception is not PostgresException pgException)
            return false;

        // 23505 = Violación de una restricción UNIQUE.
        if (pgException.SqlState != PostgresErrorCodes.UniqueViolation)
            return false;

        _logger.LogWarning(
            "Registro duplicado. Restricción: {Constraint}",
            pgException.ConstraintName);

        string mensaje = pgException.ConstraintName switch
        {
            "tbl_perfil_correo_key" =>
                "Ya existe un usuario registrado con ese correo electrónico.",

            "tbl_usuarios_pkey" =>
                "Ya existe un usuario con ese carnet.",

            _ =>
                "El registro que intenta guardar ya existe."
        };

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflicto de datos",
            Detail = mensaje,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode =
            StatusCodes.Status409Conflict;

        httpContext.Response.ContentType =
            "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken);

        return true;
    }
}
