using API.LL.Controllers.V1;
using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Characters.Queries.GetCharacterIdByName;
using Domain.Models.Entities.Characters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace EssenceSystem.Tests;

public sealed class CharacterNameResolutionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task ResolveName_returns_not_found_when_lookup_has_no_character(string? storedId)
    {
        await using var provider = CreateProvider(storedId is null ? null : Guid.Parse(storedId));
        var controller = CreateController(provider);

        var result = await controller.ResolveName("MissingHero");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task ResolveName_returns_existing_character_id()
    {
        var characterId = Guid.NewGuid();
        await using var provider = CreateProvider(characterId);
        var controller = CreateController(provider);

        var result = await controller.ResolveName("ExistingHero");

        var response = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(characterId, Assert.IsType<Guid>(response.Value));
    }

    private static ServiceProvider CreateProvider(Guid? characterId)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICharacterService>(new StubCharacterService(characterId));
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<GetCharacterIdByNameQueryHandler>());
        return services.BuildServiceProvider();
    }

    private static CharacterController CreateController(IServiceProvider provider) => new()
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = provider }
        }
    };

    private sealed class StubCharacterService(Guid? characterId) : ICharacterService
    {
        public Task<Guid?> GetCharacterIdByNameAsync(string name, CancellationToken cancellationToken) => Task.FromResult(characterId);
        public Task<Character> CreateCharacterAsync(Guid userId, string username, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetMyCharacterAsync(Guid currentUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetCharacterByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetMyCharacterOverviewAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetCharacterOverviewByNameAsync(string characterName, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> UpdateCharacterNameAsync(Guid userId, string username, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsCharacterNameTakenAsync(string name, Guid? excludedCharacterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetBaseCharacterByIdAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetCharacterWithSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
