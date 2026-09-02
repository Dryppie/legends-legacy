using Application.Common.Mappings;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Domain.Models.Combat;

namespace Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
public class CombatResultDto : IMapFrom<CombatResult>
{
    public List<SimpleCombatEntityDto> PlayerTeam { get; set; } = [];
    public List<SimpleCombatEntityDto> EnemyTeam { get; set; } = [];
    public List<EntityStatsDto> EntityStats { get; set; } = [];
    public BattleOutcome Outcome { get; set; }
    public List<InventoryItemDto> Loot { get; set; } = [];
    public List<GatheringRewardResult> GatheringRewards { get; set; } = [];
    public int ExperienceGained { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public int Duration { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatResult, CombatResultDto>();
    }
}
