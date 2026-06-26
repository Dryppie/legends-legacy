using Application.Common.Mappings;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Application.UseCases.Colosseum.Models;
using AutoMapper;
using Domain.Models.Colosseum;

namespace Application.UseCases.Colosseum.Dtos;

public sealed class StartArenaBattleResponseDto : IMapFrom<StartArenaBattleResponseModel>
{
    public Guid BattleId { get; set; }
    public required CombatResultDto Battle { get; init; }
    public required CombatResultDto Combat { get; init; }
    public required ArenaBattleOutcomeDto Outcome { get; init; }
    public required ArenaTicketStatusDto ArenaTicketStatus { get; init; }
    public required ArenaRewardDto Rewards { get; init; }
    public required ArenaRatingChangeDto AttackerRating { get; init; }
    public required ArenaRatingChangeDto DefenderRating { get; init; }
    public required ArenaRankChangeDto AttackerRank { get; init; }
    public required ArenaStreakChangeDto Streak { get; init; }
    public required ArenaOpponentPreviewDto Opponent { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<StartArenaBattleResponseModel, StartArenaBattleResponseDto>();
    }
}

public sealed class StartArenaBattleRequestDto : IMapFrom<StartArenaBattleRequestModel>
{
    public Guid OpponentId { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<StartArenaBattleRequestModel, StartArenaBattleRequestDto>();
    }
}

public sealed class ArenaBattleOutcomeDto : IMapFrom<ArenaBattleOutcomeModel>
{
    public string Result { get; set; } = string.Empty;
    public Guid AttackerCharacterId { get; set; }
    public Guid DefenderCharacterId { get; set; }
    public Guid? WinnerCharacterId { get; set; }
    public DateTimeOffset CompletedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ArenaBattleOutcomeModel, ArenaBattleOutcomeDto>();
    }
}

public sealed class ArenaRatingChangeDto : IMapFrom<ArenaRatingChangeModel>
{
    public int RatingBefore { get; set; }
    public int RatingAfter { get; set; }
    public int Delta { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ArenaRatingChangeModel, ArenaRatingChangeDto>();
    }
}

public sealed class ArenaRewardDto : IMapFrom<ArenaRewardModel>
{
    public int GloryEarned { get; set; }
    public int BaseReward { get; set; }
    public int DailyFirstWinBonus { get; set; }
    public int StreakBonus { get; set; }
    public int DefensiveBonus { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ArenaRewardModel, ArenaRewardDto>();
    }
}

public sealed class ArenaRankTierDto : IMapFrom<ArenaRankTier>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MinRating { get; set; }
    public int? MaxRating { get; set; }
    public int SortOrder { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ArenaRankTier, ArenaRankTierDto>();
    }
}

public sealed class ArenaRankProgressDto : IMapFrom<ArenaRankProgress>
{
    public string CurrentTierId { get; set; } = string.Empty;
    public string CurrentTierName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public int CurrentTierMinRating { get; set; }
    public int? NextTierMinRating { get; set; }
    public string? NextTierName { get; set; }
    public int? RatingUntilNextTier { get; set; }
    public decimal ProgressPercent { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ArenaRankProgress, ArenaRankProgressDto>();
    }
}

public sealed class ArenaRankChangeDto : IMapFrom<ArenaRankChangeModel>
{
    public required ArenaRankProgressDto Before { get; init; }
    public required ArenaRankProgressDto After { get; init; }
    public bool TierChanged { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ArenaRankChangeModel, ArenaRankChangeDto>();
    }
}

public sealed class ArenaStreakChangeDto : IMapFrom<ArenaStreakChangeModel>
{
    public int Before { get; set; }
    public int After { get; set; }
    public int BonusGlory { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ArenaStreakChangeModel, ArenaStreakChangeDto>();
    }
}
