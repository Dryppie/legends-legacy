using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Combat;

namespace Application.UseCases.CharacterActions.Dtos.CombatDtos;
public class SimpleCombatEntityDto : IMapFrom<SimpleCombatEntity>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<SimpleCombatEntity, SimpleCombatEntityDto>();
    }
}