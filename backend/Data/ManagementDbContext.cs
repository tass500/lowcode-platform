using LowCodePlatform.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Data;

public sealed class ManagementDbContext : DbContext
{
    public ManagementDbContext(DbContextOptions<ManagementDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("tenant");
            e.HasKey(x => x.TenantId);
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.Slug).HasColumnName("slug");
            e.Property(x => x.ConnectionStringSecretRef).HasColumnName("connection_string_secret_ref");
            e.Property(x => x.ConnectionString).HasColumnName("connection_string");
            e.Property(x => x.TenantApiKeySha256Hex).HasColumnName("tenant_api_key_sha256_hex");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.HasIndex(x => x.Slug).IsUnique();
        });
    }
}
