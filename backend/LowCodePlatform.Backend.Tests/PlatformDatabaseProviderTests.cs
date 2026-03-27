using LowCodePlatform.Backend.Data;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class PlatformDatabaseProviderTests
{
    [Theory]
    [InlineData("Data Source=tenant.db", false)]
    [InlineData("Data Source=:memory:", false)]
    [InlineData(@"Data Source=C:\data\my.db", false)]
    [InlineData("Server=localhost;Database=app;Trusted_Connection=True;", true)]
    [InlineData("Server=tcp:myserver.database.windows.net,1433;Initial Catalog=app;User ID=u;Password=p;", true)]
    [InlineData("Data Source=sql.host.com,1433;Initial Catalog=app;User ID=u;Password=p;", true)]
    public void IsSqlServerConnectionString_classifies_expected_inputs(string connectionString, bool expectedSqlServer)
    {
        Assert.Equal(expectedSqlServer, PlatformDatabaseProvider.IsSqlServerConnectionString(connectionString));
    }

    [Fact]
    public void IsSqlServerConnectionString_null_or_whitespace_is_false()
    {
        Assert.False(PlatformDatabaseProvider.IsSqlServerConnectionString(null));
        Assert.False(PlatformDatabaseProvider.IsSqlServerConnectionString(""));
        Assert.False(PlatformDatabaseProvider.IsSqlServerConnectionString("   "));
    }
}
