using Application.Interfaces.Services.LL.Quests;
using Application.MediatR.Markers;
using Application.UseCases.Quests.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Quests.Commands.TurnInQuest;

public sealed record TurnInQuestCommand(
    Guid CharacterId,
    string QuestId) : ICommand<Response<QuestJournalDto>>;

public sealed class TurnInQuestCommandHandler(
    IQuestService questService,
    IMapper mapper) : IRequestHandler<TurnInQuestCommand, Response<QuestJournalDto>>
{
    public async Task<Response<QuestJournalDto>> Handle(
        TurnInQuestCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var journal = await questService.TurnInAsync(
                request.CharacterId,
                request.QuestId,
                cancellationToken);
            return Response<QuestJournalDto>.Success(mapper.Map<QuestJournalDto>(journal));
        }
        catch (ArgumentException ex)
        {
            return Response<QuestJournalDto>.Fail(ex.Message);
        }
    }
}
