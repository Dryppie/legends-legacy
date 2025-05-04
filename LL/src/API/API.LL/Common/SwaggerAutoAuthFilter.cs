using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Common.Authorization.Security;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.LL.Common;

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

        var userEmail = _configuration["SwaggerOptions:UserEmail"];
        if (!string.IsNullOrEmpty(userEmail))
        {
            var token = GenerateTokenForUserByEmail(userEmail, CancellationToken.None).Result;
            if (token != null)
            {
                // Add a default value to swagger's Authorization here
                operation.Parameters.Add(new()
                {
                    Name = "DevAuth",
                    In = ParameterLocation.Header,
                    Description = "Auto-set JWT token",
                    Required = true,
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Default = new OpenApiString($"{token.AccessToken}")
                    }
                });
            }
        }
    }

    private async Task<Tokens?> GenerateTokenForUserByEmail(string email, CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtGenerator>();
            var characterService = scope.ServiceProvider.GetRequiredService<ICharacterService>();

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return null;
            }

            var character = await characterService.GetMyCharacterAsync(Guid.Parse(user.Id), cancellationToken);

            var authInfo = new AuthInfo
            {
                IsValid = true,
                Id = user.Id,
                Name = user.UserName!,
                CharacterId = character.Id.ToString(),
            };

            var token = jwtTokenService.GenerateTokens(authInfo);
            return token;
        }
    }
}
