using Application.Common.Mappings;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Domain.Models.Combat;

namespace Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;

public sealed class AbilityStatsDto : IMapFrom<AbilityStats>
{
    public string Name { get; set; } = string.Empty;
    public int TotalDamage { get; set; }
    public IReadOnlyList<AbilityDamageTypeStats> DamageByType { get; set; } = [];
    public int TotalHealing { get; set; }
    public int Uses { get; set; }
    public int Hits { get; set; }
    public int Crits { get; set; }
    public int Summons { get; set; }
    public int Stuns { get; set; }
    public int SelfDamage { get; set; }
    public int AlliedDamage { get; set; }
    public int TotalBarrier { get; set; }
    public int TotalThreat { get; set; }
    public int TotalStagger { get; set; }
    public int StaggerBreaks { get; set; }
    public EssenceAbilityDto? Definition { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<AbilityStats, AbilityStatsDto>()
            .ForMember(
                destination => destination.Definition,
                options =>
                {
                    options.PreCondition(source => source.Definition is not null);
                    options.MapFrom(source => source.Definition);
                });
    }
}
