using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.CharacterActions.Sessions;

namespace Application.UseCases.CharacterActions.Dtos.Responses.GatheringDtos;
public class GatheringSessionDto : IMapFrom<GatheringSession>
{
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public GatheringSummaryDto GatheringSummary { get; set; } = null!;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<GatheringSession, GatheringSessionDto>();
    }
}
