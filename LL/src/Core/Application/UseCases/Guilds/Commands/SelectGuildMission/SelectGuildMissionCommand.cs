using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.SelectGuildMission;

public record SelectGuildMissionCommand(Guid CharacterId, Guid MissionOptionId) : ICommand<Response<GuildMissionOverviewDto>>;

public class SelectGuildMissionCommandHandler : IRequestHandler<SelectGuildMissionCommand, Response<GuildMissionOverviewDto>>
{
    private readonly IGuildMissionService _guildMissionService;

    public SelectGuildMissionCommandHandler(IGuildMissionService guildMissionService)
    {
        _guildMissionService = guildMissionService;
    }

    public async Task<Response<GuildMissionOverviewDto>> Handle(SelectGuildMissionCommand request, CancellationToken cancellationToken)
    {
        var result = await _guildMissionService.SelectMissionAsync(request.CharacterId, request.MissionOptionId, DateTimeOffset.UtcNow, cancellationToken);
        return result.Succeeded && result.Value is not null
            ? Response<GuildMissionOverviewDto>.Success(result.Value)
            : Response<GuildMissionOverviewDto>.Fail(result.Error ?? "Failed to select guild mission.");
    }
}
