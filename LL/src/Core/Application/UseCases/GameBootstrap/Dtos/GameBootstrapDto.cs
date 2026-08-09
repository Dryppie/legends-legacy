using Application.Common.Mappings;
using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Quests.Dtos;
using AutoMapper;
using Domain.Models.Attributes;

namespace Application.UseCases.GameBootstrap.Dtos;

public sealed class GameBootstrapDto : IMapFrom<GameBootstrapSnapshot>
{
    public required CharacterDto Character { get; init; }
    public required QuestJournalDto QuestJournal { get; init; }
    public CharacterActionDto? CurrentAction { get; init; }
    public DateTimeOffset ServerTimeUtc { get; init; }
    public IReadOnlyCollection<AttributeDefinition> AttributeDefinitions { get; init; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<GameBootstrapSnapshot, GameBootstrapDto>();
    }
}

public sealed class GameBootstrapSnapshot
{
    public required CharacterDto Character { get; init; }
    public required QuestJournalDto QuestJournal { get; init; }
    public CharacterActionDto? CurrentAction { get; init; }
    public DateTimeOffset ServerTimeUtc { get; init; }
    public IReadOnlyCollection<AttributeDefinition> AttributeDefinitions { get; init; } = [];
}
