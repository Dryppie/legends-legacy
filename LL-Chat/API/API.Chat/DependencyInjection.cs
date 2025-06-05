using API.Chat.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

namespace API.Chat;

public static class DependencyInjection
{
    /// <summary>
    /// util method for setting up swagger support with api versioning support
    /// </summary>
    public static IServiceCollection SetupSwagger(this IServiceCollection services, string projectName, IConfiguration configuration)
    {
        var autoAuthorize = configuration.GetValue<bool>("SwaggerOptions:AutoAuthorize");

        services.AddSwaggerGen(options =>
        {

            options.SwaggerDoc("v1", new OpenApiInfo { Title = projectName, Version = "v1" });

            var jwtSecurityScheme = new OpenApiSecurityScheme
            {
                Scheme = "bearer",
                BearerFormat = "JWT",
                Name = "JWT Authentication",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Description = "Enter your token in the field below and click authorize (without bearer)",

                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };

            options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement {{ jwtSecurityScheme, Array.Empty<string>() }
            });

            if (autoAuthorize)
            {
                options.OperationFilter<SwaggerAutoAuthFilter>();
            }
        });

        return services;
    }
}
