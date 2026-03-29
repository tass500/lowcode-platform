using LowCodePlatform.Backend;
using LowCodePlatform.Backend.Data;
using LowCodePlatform.Backend.Middleware;
using LowCodePlatform.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using LowCodePlatform.Backend.Swagger;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
    builder.Logging.AddFilter((_, level) => level >= LogLevel.Warning);

    var enableEfConnectionLogs = string.Equals(Environment.GetEnvironmentVariable("LCP_TEST_EF_CONNECTION_LOGS"), "1", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(builder.Configuration["Testing:EnableEfConnectionLogs"], "true", StringComparison.OrdinalIgnoreCase);
    if (!enableEfConnectionLogs)
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Connection", LogLevel.None);
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "LowCodePlatform Backend API",
        Version = "v1",
        Description = "Tenant routing: prefer host-based tenancy (e.g. http://t1.localhost:PORT). In Development you can also use the X-Tenant-Id header. "
                      + "Bearer JWT: symmetric signing key (dev-token) and/or, when Auth:Oidc:Authority is set, tokens validated via OIDC metadata. "
                      + "`tenant_user` requires a `tenant` claim.",
    });

    if (builder.Environment.IsDevelopment())
        o.OperationFilter<AddTenantHeaderOperationFilter>();

    o.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.",
    });

    o.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer",
                }
            },
            Array.Empty<string>()
        }
    });
});

Action<JwtBearerOptions> configureSymmetricJwt = o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2),
    };
};

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = LcpAuthenticationSchemeNames.JwtForwarder;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddPolicyScheme(LcpAuthenticationSchemeNames.JwtForwarder, "JWT forwarder", opt =>
    {
        opt.ForwardDefaultSelector = ctx =>
        {
            var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
            var auth = cfg["Auth:Oidc:Authority"]?.Trim();
            string[]? extra = null;
            var rawIssuers = cfg["Auth:Oidc:ValidIssuers"];
            if (!string.IsNullOrWhiteSpace(rawIssuers))
            {
                var split = rawIssuers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                extra = split.Length == 0 ? null : split;
            }

            return LcpJwtForwardSelector.SelectScheme(ctx, auth, extra);
        };
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, configureSymmetricJwt)
    .AddJwtBearer(LcpAuthenticationSchemeNames.OidcJwt, o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        };
    });

builder.Services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerIssuerAudiencePostConfigure>();
builder.Services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, OidcJwtBearerPostConfigure>();

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("tenant_user", p => p
        .RequireAuthenticatedUser()
        .RequireClaim("tenant"));
    o.AddPolicy("admin", p => p.RequireRole("admin"));
});

builder.Services.AddDbContext<ManagementDbContext>((sp, o) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var managementCs = cfg.GetConnectionString("Management")
                       ?? cfg["ConnectionStrings:Management"]
                       ?? "Data Source=management.db";
    o.UseSqlite(managementCs);
});

builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<TenantRegistryService>();
builder.Services.AddScoped<TenantDbConnectionStringProvider>();
builder.Services.AddScoped<TenantMigrationService>();
builder.Services.AddSingleton<ITenantSecretResolver, ConfigurationTenantSecretResolver>();

var isEfDesignTime = string.Equals(Environment.GetEnvironmentVariable("LCP_EF_DESIGN_TIME"), "1", StringComparison.OrdinalIgnoreCase)
                     || Process.GetCurrentProcess().ProcessName.Contains("dotnet-ef", StringComparison.OrdinalIgnoreCase)
                     || Environment.GetCommandLineArgs().Any(a => a.Contains("dotnet-ef", StringComparison.OrdinalIgnoreCase));

// Integration tests use environment "Testing" and must keep tenant-resolved PlatformDbContext even if
// LCP_EF_DESIGN_TIME=1 leaked into the process from a prior `dotnet ef` invocation in the same shell.
if (isEfDesignTime && !builder.Environment.IsEnvironment("Testing"))
{
    var designTimeTenantCs = builder.Configuration["Tenancy:DesignTimeTenantConnectionString"]
                             ?? "Data Source=tenant-default.db";

    builder.Services.AddDbContext<PlatformDbContext>(o =>
        PlatformDatabaseProvider.ConfigurePlatformDbContext(o, designTimeTenantCs));
}
else
{
    builder.Services.AddScoped<PlatformDbContext>(sp =>
    {
        var tenantCs = sp.GetRequiredService<TenantDbConnectionStringProvider>().Get();
        return PlatformDatabaseProvider.CreatePlatformDbContext(sp, tenantCs);
    });
}

builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<InstallationService>();
builder.Services.AddSingleton<WorkflowRunCancellationRegistry>();
builder.Services.AddScoped<WorkflowRunnerService>();
builder.Services.AddSingleton<VersionEnforcementService>();
builder.Services.AddSingleton<DevUpgradeFaults>();

if (!builder.Environment.IsEnvironment("Testing") && !isEfDesignTime)
{
    builder.Services.AddHostedService<UpgradeOrchestratorHostedService>();
    builder.Services.AddHostedService<WorkflowScheduleHostedService>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();

app.UseMiddleware<TraceIdMiddleware>();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();

app.UseMiddleware<AdminApiKeyMiddleware>();

app.UseMiddleware<TenantResolutionMiddleware>();

app.UseMiddleware<TenantApiKeyAuthenticationMiddleware>();

app.UseMiddleware<TenantClaimEnforcementMiddleware>();

app.UseAuthorization();

app.UseMiddleware<AccessLogMiddleware>();

app.MapGet("/health", () => Results.Ok(HealthPayloadBuilder.Live()));
app.MapGet("/api/health", () => Results.Ok(HealthPayloadBuilder.Live()));

app.MapGet("/health/live", () => Results.Ok(HealthPayloadBuilder.Live()));
app.MapGet("/health/ready", async (ManagementDbContext db) =>
{
    var ok = await db.Database.CanConnectAsync();
    return ok
        ? Results.Ok(HealthPayloadBuilder.Ready())
        : Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapControllers();

static bool IsEfDesignTime()
    => (Process.GetCurrentProcess().ProcessName.Contains("dotnet-ef", StringComparison.OrdinalIgnoreCase))
       || Environment.GetCommandLineArgs().Any(a => a.Contains("dotnet-ef", StringComparison.OrdinalIgnoreCase))
       || Environment.GetCommandLineArgs().Any(a => string.Equals(a, "ef", StringComparison.OrdinalIgnoreCase));

static bool ShouldSkipStartupMigrationsAndSeeding()
    => string.Equals(Environment.GetEnvironmentVariable("LCP_SKIP_STARTUP_SEED"), "1", StringComparison.OrdinalIgnoreCase);

if (!app.Environment.IsEnvironment("Testing") && !IsEfDesignTime() && !ShouldSkipStartupMigrationsAndSeeding())
{
    using var scope = app.Services.CreateScope();

    var managementDb = scope.ServiceProvider.GetRequiredService<ManagementDbContext>();
    try
    {
        await managementDb.Database.MigrateAsync();
    }
    catch (Microsoft.Data.Sqlite.SqliteException ex)
        when ((app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
              && ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
    {
        // Dev/Test safety valve: tolerate stale SQLite DBs created outside migrations history.
        // Prod should fail-fast to avoid running with an unknown schema state.
        await RepairStaleManagementSqliteSchemaAsync(managementDb, CancellationToken.None);
    }

    var defaultTenantSlug = builder.Configuration["Tenancy:DefaultTenantSlug"]
                            ?? "default";
    var defaultTenantSecretRef = builder.Configuration["Tenancy:DefaultTenantConnectionStringSecretRef"]
                                 ?? "default";
    var defaultTenantCs = builder.Configuration["Tenancy:DefaultTenantConnectionString"]
                          ?? "Data Source=tenant-default.db";

    LowCodePlatform.Backend.Models.Tenant? existing;
    try
    {
        existing = await managementDb.Tenants.FirstOrDefaultAsync(x => x.Slug == defaultTenantSlug);
    }
    catch (Microsoft.Data.Sqlite.SqliteException ex)
        when ((app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
              && ex.Message.Contains("no such column", StringComparison.OrdinalIgnoreCase)
              && ex.Message.Contains("connection_string_secret_ref", StringComparison.OrdinalIgnoreCase))
    {
        // Dev/Test safety valve: tolerate stale SQLite DBs missing the newer column.
        // Prod should fail-fast to avoid running with an unknown schema state.
        await RepairStaleManagementSqliteSchemaAsync(managementDb, CancellationToken.None);
        existing = await managementDb.Tenants.FirstOrDefaultAsync(x => x.Slug == defaultTenantSlug);
    }

    if (existing is null)
    {
        managementDb.Tenants.Add(new LowCodePlatform.Backend.Models.Tenant
        {
            TenantId = Guid.NewGuid(),
            Slug = defaultTenantSlug,
            ConnectionStringSecretRef = defaultTenantSecretRef,
            ConnectionString = defaultTenantCs,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await managementDb.SaveChangesAsync();
    }

    var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
    tenant.Slug = defaultTenantSlug;

    var tenantDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var tenantCsForBootstrap = tenantDb.Database.GetConnectionString() ?? defaultTenantCs;
    try
    {
        await PlatformTenantDatabaseBootstrap.MigrateOrEnsureCreatedAsync(tenantDb, tenantCsForBootstrap, CancellationToken.None);
    }
    catch (Microsoft.Data.Sqlite.SqliteException ex)
        when ((app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
              && ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
    {
        // Dev/Test safety valve: tolerate stale SQLite DBs created outside migrations history.
        // Prod should fail-fast to avoid running with an unknown schema state.
        startupLogger.LogWarning(ex, "tenant_sqlite_migrate_skipped_already_exists; will try upgrade table repair");
    }

    await PlatformSqliteSchemaRepair.EnsureUpgradeTablesExistAsync(tenantDb, CancellationToken.None);

    var inst = scope.ServiceProvider.GetRequiredService<InstallationService>();
    await inst.EnsureDefaultInstallationAsync(CancellationToken.None);
}

app.Run();

static async Task RepairStaleManagementSqliteSchemaAsync(ManagementDbContext db, CancellationToken ct)
{
    // Best-effort, idempotent schema repair for dev/test only.
    // This is specifically to handle old SQLite files where the "tenant" table exists
    // but migrations history is missing, causing MigrateAsync() to fail on CREATE TABLE.

    var conn = db.Database.GetDbConnection();
    await conn.OpenAsync(ct);

    var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    await using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "PRAGMA table_info('tenant');";
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            // PRAGMA table_info columns: cid, name, type, notnull, dflt_value, pk
            var name = reader.GetString(1);
            existingColumns.Add(name);
        }
    }

    if (existingColumns.Count == 0)
        return;

    if (!existingColumns.Contains("connection_string_secret_ref"))
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE tenant ADD COLUMN connection_string_secret_ref TEXT NULL;", ct);

    if (!existingColumns.Contains("tenant_api_key_sha256_hex"))
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE tenant ADD COLUMN tenant_api_key_sha256_hex TEXT NULL;", ct);

    // If the old schema has connection_string NOT NULL, make it nullable by table rebuild.
    // SQLite doesn't support ALTER COLUMN, so we do a safe no-op if it's already nullable.
    bool connectionStringNotNull;
    await using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "SELECT 1 FROM pragma_table_info('tenant') WHERE name='connection_string' AND \"notnull\"=1;";
        var scalar = await cmd.ExecuteScalarAsync(ct);
        connectionStringNotNull = scalar is not null;
    }

    if (connectionStringNotNull)
    {
        // Rebuild tenant table with the new nullable column definition.
        // Keep data, preserve primary key and unique index on slug.
        await db.Database.ExecuteSqlRawAsync(@"
BEGIN TRANSACTION;
CREATE TABLE IF NOT EXISTS tenant__tmp (
    tenant_id TEXT NOT NULL CONSTRAINT PK_tenant__tmp PRIMARY KEY,
    slug TEXT NOT NULL,
    connection_string_secret_ref TEXT NULL,
    connection_string TEXT NULL,
    created_at_utc TEXT NOT NULL
);
INSERT INTO tenant__tmp (tenant_id, slug, connection_string_secret_ref, connection_string, created_at_utc)
SELECT tenant_id, slug,
       COALESCE(connection_string_secret_ref, NULL),
       connection_string,
       created_at_utc
FROM tenant;
DROP TABLE tenant;
ALTER TABLE tenant__tmp RENAME TO tenant;
CREATE UNIQUE INDEX IF NOT EXISTS IX_tenant_slug ON tenant(slug);
COMMIT;
", ct);
    }
}

public partial class Program { }
