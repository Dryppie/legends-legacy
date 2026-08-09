using Application.Interfaces.Services.LL.Quests;
using Application.MediatR.Markers;
using Application.UseCases.Quests.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Quests.Commands.AcknowledgeQuestWelcome;

public sealed record AcknowledgeQuestWelcomeCommand(Guid CharacterId)
    : ICommand<Response<QuestJournalDto>>;

public sealed class AcknowledgeQuestWelcomeCommandHandler(
    IQuestService questService,
    IMapper mapper) : IRequestHandler<AcknowledgeQuestWelcomeCommand, Response<QuestJournalDto>>
{
    public async Task<Response<QuestJournalDto>> Handle(
        AcknowledgeQuestWelcomeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var journal = await questService.AcknowledgeWelcomeAsync(
                request.CharacterId,
                cancellationToken);
            return Response<QuestJournalDto>.Success(mapper.Map<QuestJournalDto>(journal));
        }
        catch (InvalidOperationException ex)
        {
            return Response<QuestJournalDto>.Fail(ex.Message);
        }
    }
}
