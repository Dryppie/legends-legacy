using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.PurchaseGuildShopItem;

public record PurchaseGuildShopItemCommand(Guid CharacterId, string ItemKey) : ICommand<Response<GuildShopOverviewDto>>;

public class PurchaseGuildShopItemCommandHandler : IRequestHandler<PurchaseGuildShopItemCommand, Response<GuildShopOverviewDto>>
{
    private readonly IGuildShopService _guildShopService;

    public PurchaseGuildShopItemCommandHandler(IGuildShopService guildShopService)
    {
        _guildShopService = guildShopService;
    }

    public async Task<Response<GuildShopOverviewDto>> Handle(PurchaseGuildShopItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _guildShopService.PurchaseAsync(request.CharacterId, request.ItemKey, DateTimeOffset.UtcNow, cancellationToken);
        return result.Succeeded && result.Value is not null
            ? Response<GuildShopOverviewDto>.Success(result.Value)
            : Response<GuildShopOverviewDto>.Fail(result.Error ?? "Failed to purchase guild shop item.");
    }
}
