using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Inventories.Commands.SetInventoryItemFavorite;

public sealed record SetInventoryItemFavoriteCommand(
    Guid CharacterId,
    Guid ItemInstanceId,
    bool IsFavorite) : ICommand<Response<SetInventoryItemFavoriteResponseDto>>;

public sealed class SetInventoryItemFavoriteCommandHandler
    : IRequestHandler<SetInventoryItemFavoriteCommand, Response<SetInventoryItemFavoriteResponseDto>>
{
    private readonly IInventoryService _inventory;
    private readonly IMapper _mapper;

    public SetInventoryItemFavoriteCommandHandler(
        IInventoryService inventory,
        IMapper mapper)
    {
        _inventory = inventory;
        _mapper = mapper;
    }

    public async Task<Response<SetInventoryItemFavoriteResponseDto>> Handle(
        SetInventoryItemFavoriteCommand request,
        CancellationToken cancellationToken)
    {
        var updated = await _inventory.SetItemFavoriteAsync(
            request.CharacterId,
            request.ItemInstanceId,
            request.IsFavorite,
            cancellationToken);

        if (!updated)
            return Response<SetInventoryItemFavoriteResponseDto>.Fail(
                "The item is no longer in your inventory.");

        var inventory = await _inventory.GetInventoryByIdAsync(
            request.CharacterId,
            cancellationToken);
        if (inventory is null)
            return Response<SetInventoryItemFavoriteResponseDto>.Fail(
                "The inventory could not be loaded.");

        return Response<SetInventoryItemFavoriteResponseDto>.Success(
            new SetInventoryItemFavoriteResponseDto
            {
                ItemInstanceId = request.ItemInstanceId,
                IsFavorite = request.IsFavorite,
                InventoryItems = _mapper.Map<List<InventoryItemDto>>(inventory.InventoryItems)
            });
    }
}
