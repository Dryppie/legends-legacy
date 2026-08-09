using Application.Interfaces.Services.LL.Quests;
using Application.MediatR.Markers;
using Application.UseCases.Quests.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Quests.Commands.AcceptQuest;

public sealed record AcceptQuestCommand(Guid CharacterId, string QuestId)
    : ICommand<Response<QuestJournalDto>>;

public sealed class AcceptQuestCommandHandler(
    IQuestService questService,
    IMapper mapper) : IRequestHandler<AcceptQuestCommand, Response<QuestJournalDto>>
{
    public async Task<Response<QuestJournalDto>> Handle(
        AcceptQuestCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var journal = await questService.AcceptAsync(
                request.CharacterId,
                request.QuestId,
                cancellationToken);
            return Response<QuestJournalDto>.Success(mapper.Map<QuestJournalDto>(journal));
        }
        catch (InvalidOperationException ex)
        {
            return Response<QuestJournalDto>.Fail(ex.Message);
        }
    }
}
