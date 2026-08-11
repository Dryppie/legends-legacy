using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Outbox;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.PurchaseGuildShopItem;

public sealed record PurchaseGuildShopItemResponseDto(
    Guid GuildId,
    long GuildFavor,
    string WeeklyPeriodKey,
    DateTimeOffset NextWeeklyResetAt,
    IReadOnlyList<GuildShopItemDto> Items,
    Guid? InventoryGrantId,
    IReadOnlyList<InventoryItemDto> InventoryItemsGranted);

public record PurchaseGuildShopItemCommand(Guid CharacterId, string ItemKey)
    : ICommand<Response<PurchaseGuildShopItemResponseDto>>;

public class PurchaseGuildShopItemCommandHandler
    : IRequestHandler<PurchaseGuildShopItemCommand, Response<PurchaseGuildShopItemResponseDto>>
{
    private readonly IGuildShopService _guildShopService;
    private readonly IGameEventOutbox _outbox;
    private readonly IMapper _mapper;

    public PurchaseGuildShopItemCommandHandler(
        IGuildShopService guildShopService,
        IGameEventOutbox outbox,
        IMapper mapper)
    {
        _guildShopService = guildShopService;
        _outbox = outbox;
        _mapper = mapper;
    }

    public async Task<Response<PurchaseGuildShopItemResponseDto>> Handle(
        PurchaseGuildShopItemCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _guildShopService.PurchaseAsync(request.CharacterId, request.ItemKey, DateTimeOffset.UtcNow, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return Response<PurchaseGuildShopItemResponseDto>.Fail(
                result.Error ?? "Failed to purchase guild shop item.");
        }

        var inventoryItems = _mapper.Map<List<InventoryItemDto>>(result.Value.InventoryItemsGranted);
        Guid? grantId = inventoryItems.Count > 0 ? Guid.NewGuid() : null;
        if (grantId.HasValue)
        {
            await _outbox.EnqueueAsync(
                GameEventTypes.InventoryItemsGranted,
                new InventoryItemsGrantedPayload(
                    grantId.Value,
                    request.CharacterId,
                    inventoryItems,
                    "guild-shop",
                    "Guild Shop"),
                request.CharacterId,
                null,
                cancellationToken);
        }

        var shop = result.Value.Shop;
        return Response<PurchaseGuildShopItemResponseDto>.Success(
            new PurchaseGuildShopItemResponseDto(
                shop.GuildId,
                shop.GuildFavor,
                shop.WeeklyPeriodKey,
                shop.NextWeeklyResetAt,
                shop.Items,
                grantId,
                inventoryItems));
    }
}
