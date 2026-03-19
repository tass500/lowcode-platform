using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LowCodePlatform.Backend.Swagger;

public sealed class AddTenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        if (operation.Parameters.Any(p => string.Equals(p.Name, "X-Tenant-Id", StringComparison.OrdinalIgnoreCase)))
            return;

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Tenant-Id",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Development helper: overrides resolved tenant (use host-based tenancy in prod).",
            Schema = new OpenApiSchema { Type = "string" },
        });
    }
}
