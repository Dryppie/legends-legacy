using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Combat;
using Domain.Models.Inventories;

namespace Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
public class CombatResultDto : IMapFrom<CombatResult>
{
    public List<SimpleCombatEntityDto> PlayerTeam { get; set; } = [];
    public List<SimpleCombatEntityDto> EnemyTeam { get; set; } = [];
    public List<CombatEvent> EventLog { get; set; } = [];
    public BattleOutcome Outcome { get; set; }
    public List<InventoryItem> Loot { get; set; } = [];
    public int ExperienceGained { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public int Duration { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatResult, CombatResultDto>();
    }
}