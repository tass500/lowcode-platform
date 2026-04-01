using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LowCodePlatform.Backend.Auth.Bff;

public static class BffAuthFeature
{
    public static bool IsEnabled(BffAuthOptions options, IHostEnvironment env, IConfiguration configuration) =>
        options.Enabled
        && (env.IsDevelopment() || env.IsEnvironment("Testing") || configuration.GetValue("Auth:Bff:AllowNonDevelopment", false));
}
