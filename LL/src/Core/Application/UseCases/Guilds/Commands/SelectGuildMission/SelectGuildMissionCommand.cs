using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Markers;
using Application.UseCases.Outbox;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.SelectGuildMission;

public record SelectGuildMissionCommand(Guid CharacterId, Guid MissionOptionId) : ICommand<Response<GuildMissionOverviewDto>>;

public class SelectGuildMissionCommandHandler : IRequestHandler<SelectGuildMissionCommand, Response<GuildMissionOverviewDto>>
{
    private readonly IGuildMissionService _guildMissionService;
    private readonly IGameEventOutbox _outbox;

    public SelectGuildMissionCommandHandler(
        IGuildMissionService guildMissionService,
        IGameEventOutbox outbox)
    {
        _guildMissionService = guildMissionService;
        _outbox = outbox;
    }

    public async Task<Response<GuildMissionOverviewDto>> Handle(SelectGuildMissionCommand request, CancellationToken cancellationToken)
    {
        var result = await _guildMissionService.SelectMissionAsync(request.CharacterId, request.MissionOptionId, DateTimeOffset.UtcNow, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return Response<GuildMissionOverviewDto>.Fail(result.Error ?? "Failed to select guild mission.");
        }

        await _outbox.EnqueueAsync(
            GameEventTypes.GuildMissionSelected,
            new GuildMissionSelectedPayload(
                result.Value.GuildId,
                request.CharacterId,
                InitiatorHandled: true),
            request.CharacterId,
            null,
            cancellationToken);

        return Response<GuildMissionOverviewDto>.Success(result.Value);
    }
}
