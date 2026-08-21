using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Prophecies;
using Application.MediatR.Markers;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Dungeons.Queries.GetAvailableDungeons;
using Application.UseCases.Prophecies.Events;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Dungeons.Runs;
using MediatR;

namespace Application.UseCases.Dungeons.Commands.ExecuteDungeonAction;

public record ExecuteDungeonActionCommand(Guid CharacterId, Guid RunId, string ActionId, object? Payload) : ICommand<Response<ExecuteDungeonActionResponseDto>>;
public class ExecuteDungeonActionCommandHandler : IRequestHandler<ExecuteDungeonActionCommand, Response<ExecuteDungeonActionResponseDto>>
{
    private readonly IDungeonRunService _dungeonRunService;
    private readonly IMapper _mapper;
    private readonly IPublisher _publisher;
    private readonly DungeonHubFactory _dungeonHub;

    public ExecuteDungeonActionCommandHandler(
        IDungeonRunService dungeonRunService,
        IMapper mapper,
        IPublisher publisher,
        DungeonHubFactory dungeonHub)
    {
        _dungeonRunService = dungeonRunService;
        _mapper = mapper;
        _publisher = publisher;
        _dungeonHub = dungeonHub;
    }

    public async Task<Response<ExecuteDungeonActionResponseDto>> Handle(
        ExecuteDungeonActionCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _dungeonRunService.ExecuteActionAsync(
            request.CharacterId,
            request.RunId,
            request.ActionId,
            request.Payload,
            cancellationToken);

        if (result == null)
            return Response<ExecuteDungeonActionResponseDto>.Fail("Failed to execute action on dungeon run.");

        await PublishProphecyProgressAsync(request.CharacterId, result.Outcome, cancellationToken);

        var response = _mapper.Map<ExecuteDungeonActionResponseDto>(result);
        response.Hub = await _dungeonHub.CreateAsync(request.CharacterId, cancellationToken);

        return Response<ExecuteDungeonActionResponseDto>.Success(response);
    }

    private async Task PublishProphecyProgressAsync(
        Guid characterId,
        DungeonActionOutcome outcome,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var progressEvents = new List<ProphecyProgressEvent>();

        if (outcome is DungeonActionOutcome.CombatVictory
            or DungeonActionOutcome.RestSiteResolved
            or DungeonActionOutcome.RunCompleted)
        {
            progressEvents.Add(new ProphecyProgressEvent(
                characterId,
                now,
                ProphecyProgressKind.DungeonRoomCleared));
        }

        if (outcome == DungeonActionOutcome.RunCompleted)
        {
            progressEvents.Add(new ProphecyProgressEvent(
                characterId,
                now,
                ProphecyProgressKind.DungeonCompleted));
        }

        if (progressEvents.Count > 0)
        {
            await _publisher.Publish(new ProphecyProgressBatchNotification(progressEvents), cancellationToken);
        }
    }
}
