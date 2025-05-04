using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.CharacterActions.Sessions;

namespace Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
public class CombatSessionDto : IMapFrom<CombatSession>
{
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public CombatResultDto CombatResult { get; set; } = null!;
    public CombatSummary CombatSummary { get; set; } = null!;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatSession,  CombatSessionDto>();
    }
}