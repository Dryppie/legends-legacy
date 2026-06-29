using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.ClaimGuildWeeklyMissionReward;

public record ClaimGuildWeeklyMissionRewardCommand(Guid CharacterId) : ICommand<Response<GuildMissionOverviewDto>>;

public class ClaimGuildWeeklyMissionRewardCommandHandler : IRequestHandler<ClaimGuildWeeklyMissionRewardCommand, Response<GuildMissionOverviewDto>>
{
    private readonly IGuildMissionService _guildMissionService;

    public ClaimGuildWeeklyMissionRewardCommandHandler(IGuildMissionService guildMissionService)
    {
        _guildMissionService = guildMissionService;
    }

    public async Task<Response<GuildMissionOverviewDto>> Handle(ClaimGuildWeeklyMissionRewardCommand request, CancellationToken cancellationToken)
    {
        var result = await _guildMissionService.ClaimWeeklyRewardAsync(request.CharacterId, DateTimeOffset.UtcNow, cancellationToken);
        return result.Succeeded && result.Value is not null
            ? Response<GuildMissionOverviewDto>.Success(result.Value)
            : Response<GuildMissionOverviewDto>.Fail(result.Error ?? "Failed to claim weekly guild mission reward.");
    }
}
