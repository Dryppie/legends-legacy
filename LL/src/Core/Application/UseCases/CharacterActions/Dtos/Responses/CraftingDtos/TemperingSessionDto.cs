using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.CharacterActions.Sessions;

namespace Application.UseCases.CharacterActions.Dtos.Responses.CraftingDtos;
public class TemperingSessionDto : IMapFrom<TemperingSession>
{
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public TemperingSummary TemperingSummary { get; set; } = null!;
    public void Mapping(Profile profile)
    {
        profile.CreateMap<TemperingSession, TemperingSessionDto>();
    }
}