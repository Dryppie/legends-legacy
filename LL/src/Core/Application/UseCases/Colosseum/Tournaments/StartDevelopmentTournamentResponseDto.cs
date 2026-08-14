using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Colosseum.Tournaments;

namespace Application.UseCases.Colosseum.Tournaments;

public sealed record StartDevelopmentTournamentResponseDto(
    bool Started,
    Guid? TournamentId,
    int RegisteredParticipantCount,
    int TeamCount) : IMapFrom<StartDevelopmentTournamentResult>
{
    public StartDevelopmentTournamentResponseDto()
        : this(false, null, 0, 0)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<StartDevelopmentTournamentResult, StartDevelopmentTournamentResponseDto>();
    }
}
