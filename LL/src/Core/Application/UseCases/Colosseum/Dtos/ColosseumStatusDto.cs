using Application.Common.Mappings;
using Application.UseCases.Colosseum.Models;
using AutoMapper;
using Domain.Models.Colosseum;

namespace Application.UseCases.Colosseum.Dtos;

public sealed class ColosseumStatusDto : IMapFrom<ColosseumStatusModel>
{
    public int Rating { get; set; }
    public int LifetimeHighestRating { get; set; }
    public ArenaRankProgressDto RankProgress { get; set; } = default!;
    public int Glory { get; set; }
    public int Tickets { get; set; }
    public int MaxTickets { get; set; }
    public DateTimeOffset? NextTicketAt { get; set; }
    public int CurrentAttackWinStreak { get; set; }
    public int BestAttackWinStreak { get; set; }
    public bool DailyFirstWinAvailable { get; set; }
    public int DailyFirstWinBonusGlory { get; set; }
    public ArenaRecordDto AttackRecord { get; set; } = default!;
    public ArenaRecordDto DefenseRecord { get; set; } = default!;
    public ArenaDefenseStatusDto DefenseStatus { get; set; } = default!;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ColosseumStatusModel, ColosseumStatusDto>();
    }
}

public sealed class ArenaRecordDto : IMapFrom<ArenaRecordModel>
{
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ArenaRecordModel, ArenaRecordDto>();
    }
}

public sealed class ArenaDefenseStatusDto : IMapFrom<ArenaDefenseStatusModel>, IMapFrom<ArenaDefenseSnapshot>
{
    public bool HasSnapshot { get; set; }
    public bool IsValid { get; set; }
    public bool IsOutdated { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? LoadoutHash { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ArenaDefenseStatusModel, ArenaDefenseStatusDto>();
        profile.CreateMap<ArenaDefenseSnapshot, ArenaDefenseStatusDto>()
            .ForMember(dest => dest.HasSnapshot, opt => opt.MapFrom(_ => true));
    }
}
