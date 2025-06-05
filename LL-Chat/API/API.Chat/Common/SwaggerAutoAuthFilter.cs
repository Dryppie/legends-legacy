using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Chat.Common;

public class SwaggerAutoAuthFilter : IOperationFilter
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public SwaggerAutoAuthFilter(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Ensure that the security property is initialized
        if (operation.Security == null)
        {
            operation.Security = new List<OpenApiSecurityRequirement>();
        }

        // Create the security requirement for Bearer Auth
        var bearerAuth = new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer" // Must match the ID used in Swagger configuration
                    }
                },
                new string[] { }
            }
        };

        // Add the Bearer Auth requirement if it's not already present
        if (!operation.Security.Any(sr => sr.ContainsKey(new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        })))
        {
            operation.Security.Add(bearerAuth);
        }
    }
}
