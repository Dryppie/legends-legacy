using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Combat;

namespace Application.UseCases.CharacterActions.Dtos.CombatDtos;
public class CombatEntityDto : IMapFrom<CombatEntity>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatEntity, CombatEntityDto>();
    }
}