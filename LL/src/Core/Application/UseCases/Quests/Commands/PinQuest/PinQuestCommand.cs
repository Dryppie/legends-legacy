using Application.Interfaces.Services.LL.Quests;
using Application.MediatR.Markers;
using Application.UseCases.Quests.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Quests.Commands.PinQuest;

public sealed record PinQuestCommand(Guid CharacterId, string? QuestId)
    : ICommand<Response<QuestJournalDto>>;

public sealed class PinQuestCommandHandler(
    IQuestService questService,
    IMapper mapper) : IRequestHandler<PinQuestCommand, Response<QuestJournalDto>>
{
    public async Task<Response<QuestJournalDto>> Handle(
        PinQuestCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var journal = await questService.PinAsync(
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

public sealed record PinQuestRequest(string? QuestId);
