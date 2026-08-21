using Application.Common.Mappings;
using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Quests.Dtos;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Administration;

namespace Application.UseCases.GameBootstrap.Dtos;

public sealed class GameBootstrapDto : IMapFrom<GameBootstrapSnapshot>
{
    public required CharacterDto Character { get; init; }
    public required QuestJournalDto QuestJournal { get; init; }
    public CharacterActionDto? CurrentAction { get; init; }
    public DateTimeOffset ServerTimeUtc { get; init; }
    public IReadOnlyCollection<AttributeDefinition> AttributeDefinitions { get; init; } = [];
    public required AccountAccessDto AccountAccess { get; init; }
    public IReadOnlyDictionary<string, long> StateVersions { get; init; } =
        new Dictionary<string, long>(StringComparer.Ordinal);

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
    public required AccountAccessDto AccountAccess { get; init; }
    public IReadOnlyDictionary<string, long> StateVersions { get; init; } =
        new Dictionary<string, long>(StringComparer.Ordinal);
}

public sealed record AccountAccessDto(
    bool CanParticipate,
    bool IsPubliclyEligible,
    string? RestrictionCode,
    DateTimeOffset? ExpiresAt)
{
    public static AccountAccessDto From(AccountAccessSnapshot access) => new(
        access.CanParticipate,
        access.IsPubliclyEligible,
        access.EffectiveRestriction?.RestrictionType ==
            AccountRestrictionType.MultiplayerRestriction
            ? "multiplayer_restricted"
            : null,
        access.EffectiveRestriction?.ExpiresAt);
}
