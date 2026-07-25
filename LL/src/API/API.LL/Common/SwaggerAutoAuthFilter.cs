using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL.Entities;
using Common.Authorization.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Persistence.LL;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.LL.Common;

public class SwaggerAutoAuthFilter : IDocumentFilter
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public SwaggerAutoAuthFilter(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var userEmail = _configuration["SwaggerOptions:UserEmail"];
        if (string.IsNullOrWhiteSpace(userEmail))
            return;

        var token = GenerateTokenForUserByEmail(userEmail, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        if (token is null)
            return;

        foreach (var operation in swaggerDoc.Paths.Values.SelectMany(path => path.Operations.Values))
        {
            operation.Parameters ??= [];
            if (operation.Parameters.Any(parameter =>
                    parameter.In == ParameterLocation.Header
                    && parameter.Name.Equals("DevAuth", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            operation.Parameters.Add(new()
            {
                Name = "DevAuth",
                In = ParameterLocation.Header,
                Description = "Auto-set JWT token",
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Default = new OpenApiString(token.AccessToken)
                }
            });
        }
    }

    private async Task<Tokens?> GenerateTokenForUserByEmail(string email, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LLDbContext>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtGenerator>();
        var characterService = scope.ServiceProvider.GetRequiredService<ICharacterService>();

        var user = await context.Users.FirstOrDefaultAsync(
            user => user.Email == email,
            cancellationToken);
        if (user is null)
            return null;

        var character = await characterService.GetMyCharacterAsync(user.Id, cancellationToken);
        if (character is null)
            return null;

        return await jwtTokenService.IssueTokens(user, character);
    }
}
