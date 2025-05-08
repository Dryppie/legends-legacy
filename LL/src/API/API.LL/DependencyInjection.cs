using API.LL.Common;
using API.LL.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

namespace API.LL;

/// <summary>
/// Dependency Injection extensions
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection SetupApi(this IServiceCollection services)
    {
        services.AddControllers(o => o.Filters.Add<ResponseResultFilter>());

        return services;
    }

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
