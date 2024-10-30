using Application.Common.Mappings;
using Application.UseCases.CharacterActions.Dtos.CombatDtos;
using AutoMapper;
using Domain.Models.CharacterActions;

namespace Application.UseCases.CharacterActions.Dtos;
public class CharacterActionDto : IMapFrom<CharacterAction>
{
    public CharacterActionType CharacterActionType { get; set; }
    public Guid LootTableId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public CombatResultDto? CombatResult { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CharacterAction, CharacterActionDto>();
    }
}