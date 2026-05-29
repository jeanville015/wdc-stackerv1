using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WDC_STACKER.API.Swagger
{
    public class AddRequiredHeaderParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var path = context.ApiDescription.RelativePath ?? string.Empty;

            if (!path.StartsWith("api/user-privileges", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("api/feats/query", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            operation.Parameters ??= new List<IOpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Feats-Username",
                In = ParameterLocation.Header,
                Required = true,
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            });

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Feats-Password",
                In = ParameterLocation.Header,
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Format = "password"
                }
            });
        }
    }
}
