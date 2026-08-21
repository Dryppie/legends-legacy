using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Combat;

namespace Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
public class SimpleCombatEntityDto : IMapFrom<SimpleCombatEntity>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Barrier { get; set; }
    public int Level { get; set; } = 1;
    public int? PartyNumber { get; set; }
    public int CurrentStagger { get; set; }
    public int MaxStagger { get; set; }
    public bool IsStaggered { get; set; }
    public bool IsStaggerRecovering { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<SimpleCombatEntity, SimpleCombatEntityDto>();
    }
}
