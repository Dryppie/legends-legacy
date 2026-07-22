using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;

using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Dungeons.Runs;

namespace Application.UseCases.Dungeons.Dtos;

public sealed class ExecuteDungeonActionResponseDto : IMapFrom<ExecuteDungeonActionResult>
{
    public required DungeonRunDto Run { get; init; }
    public required DungeonActionOutcomeDto Outcome { get; init; }
    public CombatSessionDto? CombatSession { get; init; }
    public string? Message { get; init; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<ExecuteDungeonActionResult, ExecuteDungeonActionResponseDto>();
}
