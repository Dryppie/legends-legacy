using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
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

    public SetInventoryItemFavoriteCommandHandler(IInventoryService inventory)
    {
        _inventory = inventory;
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

        return Response<SetInventoryItemFavoriteResponseDto>.Success(
            new SetInventoryItemFavoriteResponseDto
            {
                ItemInstanceId = request.ItemInstanceId,
                IsFavorite = request.IsFavorite
            });
    }
}
