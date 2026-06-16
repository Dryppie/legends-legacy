using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;

namespace Application.UseCases.Colosseum.Dtos;

public sealed class StartArenaBattleResponseDto
{
    public required CombatResultDto Battle { get; init; }
    public required ArenaTicketStatusDto ArenaTicketStatus { get; init; }
}
