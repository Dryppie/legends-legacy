using Application.Interfaces.Services.LL.Quests;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Quests.Commands.StartQuestEncounter;

public sealed record StartQuestEncounterCommand(
    Guid CharacterId,
    string QuestId,
    string EncounterKey) : ICommand<Response<CombatResultDto>>;

public sealed class StartQuestEncounterCommandHandler(
    IQuestEncounterService encounterService,
    IMapper mapper) : IRequestHandler<StartQuestEncounterCommand, Response<CombatResultDto>>
{
    public async Task<Response<CombatResultDto>> Handle(
        StartQuestEncounterCommand request,
        CancellationToken cancellationToken)
    {
        var result = await encounterService.StartAsync(
            request.CharacterId,
            request.QuestId,
            request.EncounterKey,
            cancellationToken);

        return result is null
            ? Response<CombatResultDto>.Fail("The quest encounter is not available.")
            : Response<CombatResultDto>.Success(mapper.Map<CombatResultDto>(result));
    }
}
