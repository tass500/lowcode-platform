using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LowCodePlatform.Backend.Data;

public sealed class DesignTimeManagementDbContextFactory : IDesignTimeDbContextFactory<ManagementDbContext>
{
    public ManagementDbContext CreateDbContext(string[] args)
    {
        var cs = "Data Source=management.db";

        var options = new DbContextOptionsBuilder<ManagementDbContext>()
            .UseSqlite(cs)
            .Options;

        return new ManagementDbContext(options);
    }
}
