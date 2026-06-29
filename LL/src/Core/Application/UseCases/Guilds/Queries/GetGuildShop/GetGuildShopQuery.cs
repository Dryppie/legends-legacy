using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Markers;
using MediatR;

namespace Application.UseCases.Guilds.Queries.GetGuildShop;

public record GetGuildShopQuery(Guid CharacterId) : IQuery<GuildShopOverviewDto?>;

public class GetGuildShopQueryHandler : IRequestHandler<GetGuildShopQuery, GuildShopOverviewDto?>
{
    private readonly IGuildShopService _guildShopService;

    public GetGuildShopQueryHandler(IGuildShopService guildShopService)
    {
        _guildShopService = guildShopService;
    }

    public async Task<GuildShopOverviewDto?> Handle(GetGuildShopQuery request, CancellationToken cancellationToken) =>
        await _guildShopService.GetOverviewAsync(request.CharacterId, DateTimeOffset.UtcNow, cancellationToken);
}
