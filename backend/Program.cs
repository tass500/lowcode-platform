using LowCodePlatform.Backend.Data;
using LowCodePlatform.Backend.Middleware;
using LowCodePlatform.Backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var cs = builder.Configuration.GetConnectionString("Platform")
         ?? builder.Configuration["ConnectionStrings:Platform"]
         ?? "Data Source=platform.db";

builder.Services.AddDbContext<PlatformDbContext>(o => o.UseSqlite(cs));

builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<InstallationService>();
builder.Services.AddSingleton<VersionEnforcementService>();
builder.Services.AddSingleton<DevUpgradeFaults>();

builder.Services.AddHostedService<UpgradeOrchestratorHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseMiddleware<TraceIdMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    await db.Database.EnsureCreatedAsync();
    var inst = scope.ServiceProvider.GetRequiredService<InstallationService>();
    await inst.EnsureDefaultInstallationAsync(CancellationToken.None);
}

app.Run();

public partial class Program { }
