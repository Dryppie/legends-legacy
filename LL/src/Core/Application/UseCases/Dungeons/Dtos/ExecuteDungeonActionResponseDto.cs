using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;

namespace Application.UseCases.Dungeons.Dtos;

public sealed class ExecuteDungeonActionResponseDto
{
    public required DungeonRunDto Run { get; init; }
    public required DungeonActionOutcomeDto Outcome { get; init; }
    public CombatSessionDto? CombatSession { get; init; }
    public string? Message { get; init; }
}