using LowCodePlatform.Backend.Middleware;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LowCodePlatform.Backend.Swagger;

public sealed class AddTenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        if (!operation.Parameters.Any(p => string.Equals(p.Name, "X-Tenant-Id", StringComparison.OrdinalIgnoreCase)))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Tenant-Id",
                In = ParameterLocation.Header,
                Required = false,
                Description = "Development helper: overrides resolved tenant (use host-based tenancy in prod).",
                Schema = new OpenApiSchema { Type = "string" },
            });
        }

        if (!operation.Parameters.Any(p => string.Equals(p.Name, TenantApiKeyAuthenticationMiddleware.HeaderName, StringComparison.OrdinalIgnoreCase)))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = TenantApiKeyAuthenticationMiddleware.HeaderName,
                In = ParameterLocation.Header,
                Required = false,
                Description = "Optional: tenant-scoped API key (SHA-256 stored server-side). Use instead of Bearer JWT for automation when provisioned via admin API.",
                Schema = new OpenApiSchema { Type = "string" },
            });
        }
    }
}
