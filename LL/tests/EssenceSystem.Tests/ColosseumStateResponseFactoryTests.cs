using System.Reflection;
using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Colosseum;
using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Colosseum;
using AutoMapper;
using Domain.Models.Colosseum;
using Domain.Models.Entities.Characters;
using Domain.Models.Leaderboards;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class ColosseumStateResponseFactoryTests
{
    [Fact]
    public async Task Arena_response_includes_the_next_level_requirement()
    {
        var character = new Character
        {
            Id = Guid.NewGuid(),
            Level = 5,
            Experience = 42,
            ArenaProfile = new CharacterArenaProfile()
        };
        var colosseum = DispatchProxy.Create<IColosseumService, ColosseumProxy>();
        ((ColosseumProxy)colosseum).Character = character;
        var mapper = new MapperConfiguration(
            config => config.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var factory = new ColosseumStateResponseFactory(
            colosseum,
            mapper,
            new FixedProgression());

        var response = await factory.CreateAsync(character.Id, CancellationToken.None);

        Assert.Equal(42, response.Character.Experience);
        Assert.Equal(2_425, response.Character.ExperienceUntilNextLevel);
    }

    private sealed class FixedProgression : ICharacterExperienceProgressionProvider
    {
        public long GetRequiredExperience(int level) => level == 5 ? 2_425 : throw new InvalidOperationException();
    }

    public class ColosseumProxy : DispatchProxy
    {
        public Character Character { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            nameof(IColosseumService.GetArenaCharacterAsync) => Task.FromResult<Character?>(Character),
            nameof(IColosseumService.GetArenaTicketStatusAsync) => Task.FromResult(new ArenaTicketStatus()),
            nameof(IColosseumService.GetArenaDefenseSnapshotAsync) => Task.FromResult<ArenaDefenseSnapshot?>(null),
            nameof(IColosseumService.GetArenaOpponents) => Task.FromResult<IReadOnlyList<ArenaOpponentPreview>>([]),
            nameof(IColosseumService.GetRankings) => Task.FromResult(new List<LeaderboardEntry>()),
            nameof(IColosseumService.GetColosseumMatchResults) => Task.FromResult(new List<ColosseumMatchResult>()),
            _ => throw new NotSupportedException(targetMethod?.Name)
        };
    }
}
