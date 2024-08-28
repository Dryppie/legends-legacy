using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.CharacterActions;

namespace Application.UseCases.CharacterActions.Dtos;
public class CharacterActionDto : IMapFrom<CharacterAction>
{
    public DateTimeOffset UpdatedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CharacterAction, CharacterActionDto>();
    }
}