
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using API_Khronos_FMO.Data;
using Npgsql;
using API_Khronos_FMO.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuración de PostgreSQL
var connectionString = builder.Configuration
    .GetConnectionString("Postgres");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No se encontró la cadena de conexión a PostgreSQL.");
}

builder.Services.AddSingleton<NpgsqlDataSource>(
    _ => NpgsqlDataSource.Create(
        new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timezone = "America/El_Salvador"
        }.ConnectionString));

builder.Services.AddScoped<ConexionBD>();
builder.Services.AddScoped<ConsultasService>();
builder.Services.AddScoped<EspacioService>();
builder.Services.AddScoped<SolicitudService>();
builder.Services.AddScoped<HorarioOrdinarioService>();

// Servicios de la API
builder.Services.AddScoped<EscrituraService>();
builder.Services.AddScoped<IdentidadFilter>();

builder.Services.AddScoped<IPasswordHasher<string>,
    PasswordHasher<string>>();

builder.Services.AddAuthentication(
    BearerTokenDefaults.AuthenticationScheme)
    .AddBearerToken();

builder.Services.AddAuthorization(o =>
    o.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());

builder.Services.AddControllers(o =>
    o.Filters.AddService<IdentidadFilter>());

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<PostgresExceptionHandler>();
// OpenAPI con autenticación Bearer
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes =
            new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    Description = "Token de acceso de KhronosAPI"
                }
            };

        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var esPublico = context.Description.ActionDescriptor
            .EndpointMetadata
            .OfType<AllowAnonymousAttribute>()
            .Any();

        if (!esPublico)
        {
            operation.Security ??= [];

            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(
                    "Bearer", context.Document)] = []
            });
        }

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Restablecimiento local de contraseña
if (args.Contains("--restablecer-clave"))
{
    var carnet = Environment.GetEnvironmentVariable("KHRONOS_CARNET")
        ?? throw new InvalidOperationException("Falta KHRONOS_CARNET");

    var clave = Environment.GetEnvironmentVariable("KHRONOS_CLAVE")
        ?? throw new InvalidOperationException("Falta KHRONOS_CLAVE");

    if (clave.Length < 12)
        throw new InvalidOperationException(
            "La clave debe tener al menos 12 caracteres.");

    using var scope = app.Services.CreateScope();

    var h = scope.ServiceProvider
        .GetRequiredService<IPasswordHasher<string>>();

    var e = scope.ServiceProvider
        .GetRequiredService<EscrituraService>();

    var id = await e.EjecutarAsync(
        "UPDATE tbl_usuarios SET \"contraseña\"=@h WHERE carnet_usuario=@id RETURNING carnet_usuario",
        CancellationToken.None,
        new NpgsqlParameter("h", h.HashPassword(carnet, clave)),
        new NpgsqlParameter("id", carnet));

    Console.WriteLine(id is null
        ? "Usuario no encontrado"
        : "Clave restablecida");

    return;
}

app.UseExceptionHandler();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

    app.UseSwaggerUi(options =>
    {
        options.DocumentPath = "/openapi/v1.json";
    });
}

// Middleware HTTP y seguridad
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
