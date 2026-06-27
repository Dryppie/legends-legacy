using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.ClaimGuildOrderReward;

public record ClaimGuildOrderRewardCommand(Guid CharacterId, Guid OrderId) : ICommand<Response<GuildMissionOverviewDto>>;

public class ClaimGuildOrderRewardCommandHandler : IRequestHandler<ClaimGuildOrderRewardCommand, Response<GuildMissionOverviewDto>>
{
    private readonly IGuildMissionService _guildMissionService;

    public ClaimGuildOrderRewardCommandHandler(IGuildMissionService guildMissionService)
    {
        _guildMissionService = guildMissionService;
    }

    public async Task<Response<GuildMissionOverviewDto>> Handle(ClaimGuildOrderRewardCommand request, CancellationToken cancellationToken)
    {
        var result = await _guildMissionService.ClaimPersonalOrderRewardAsync(request.CharacterId, request.OrderId, DateTimeOffset.UtcNow, cancellationToken);
        return result.Succeeded && result.Value is not null
            ? Response<GuildMissionOverviewDto>.Success(result.Value)
            : Response<GuildMissionOverviewDto>.Fail(result.Error ?? "Failed to claim guild order reward.");
    }
}
