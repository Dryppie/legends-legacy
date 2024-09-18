using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.CharacterActions;

namespace Application.UseCases.CharacterActions.Dtos;
public class CharacterActionDto : IMapFrom<CharacterAction>
{
    public CharacterActionType CharacterActionType { get; set; }
    public Guid LootTableId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CharacterAction, CharacterActionDto>();
    }
}