using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class OidcJwtClaimMappingTests
{
    [Fact]
    public void Maps_first_tenant_claim_source_when_tenant_missing()
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim("tid", "acme"),
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
            },
            authenticationType: "Bearer");

        var principal = new ClaimsPrincipal(identity);
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:Oidc:TenantClaimSource"] = "tid,tenant",
        }).Build();

        OidcJwtClaimMapping.Apply(principal, cfg);

        var tenant = principal.FindFirst(OidcJwtClaimMapping.TenantClaimType)?.Value;
        Assert.Equal("acme", tenant);
    }

    [Fact]
    public void Does_not_override_existing_tenant_claim()
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(OidcJwtClaimMapping.TenantClaimType, "existing"),
                new Claim("tid", "ignored"),
            },
            authenticationType: "Bearer");

        var principal = new ClaimsPrincipal(identity);
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:Oidc:TenantClaimSource"] = "tid",
        }).Build();

        OidcJwtClaimMapping.Apply(principal, cfg);

        Assert.Equal("existing", principal.FindFirst(OidcJwtClaimMapping.TenantClaimType)?.Value);
    }

    [Fact]
    public void GrantAdminIfRoleContains_adds_admin_role()
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Role, "LowCode.Admins"),
            },
            authenticationType: "Bearer");

        var principal = new ClaimsPrincipal(identity);
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:Oidc:GrantAdminIfRoleContains"] = "Admins",
        }).Build();

        OidcJwtClaimMapping.Apply(principal, cfg);

        Assert.Contains(principal.FindAll(ClaimTypes.Role), c => c.Value == "admin");
    }
}
