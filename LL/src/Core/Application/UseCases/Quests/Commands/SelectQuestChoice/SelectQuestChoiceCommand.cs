using Application.Interfaces.Services.LL.Quests;
using Application.MediatR.Markers;
using Application.UseCases.Quests.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Quests.Commands.SelectQuestChoice;

public sealed record SelectQuestChoiceCommand(
    Guid CharacterId,
    string QuestId,
    string OptionKey) : ICommand<Response<QuestJournalDto>>;

public sealed class SelectQuestChoiceCommandHandler(
    IQuestService questService,
    IMapper mapper) : IRequestHandler<SelectQuestChoiceCommand, Response<QuestJournalDto>>
{
    public async Task<Response<QuestJournalDto>> Handle(
        SelectQuestChoiceCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var journal = await questService.SelectChoiceAsync(
                request.CharacterId,
                request.QuestId,
                request.OptionKey,
                cancellationToken);
            return Response<QuestJournalDto>.Success(mapper.Map<QuestJournalDto>(journal));
        }
        catch (InvalidOperationException ex)
        {
            return Response<QuestJournalDto>.Fail(ex.Message);
        }
    }
}

public sealed record SelectQuestChoiceRequest(string OptionKey);
