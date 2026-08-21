using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Inventories.Commands.MarkInventoryItemSeen;

/// <summary>
/// Records that the owning character has inspected an inventory item, clearing its "new" marker.
/// </summary>
public sealed record MarkInventoryItemSeenCommand(
    Guid CharacterId,
    Guid ItemInstanceId) : ICommand<Response<MarkInventoryItemSeenResponseDto>>;

public sealed class MarkInventoryItemSeenCommandHandler
    : IRequestHandler<MarkInventoryItemSeenCommand, Response<MarkInventoryItemSeenResponseDto>>
{
    private readonly IInventoryService _inventory;
    private readonly IMapper _mapper;

    public MarkInventoryItemSeenCommandHandler(
        IInventoryService inventory,
        IMapper mapper)
    {
        _inventory = inventory;
        _mapper = mapper;
    }

    public async Task<Response<MarkInventoryItemSeenResponseDto>> Handle(
        MarkInventoryItemSeenCommand request,
        CancellationToken cancellationToken)
    {
        var marked = await _inventory.MarkItemSeenAsync(
            request.CharacterId,
            request.ItemInstanceId,
            cancellationToken);

        if (!marked)
            return Response<MarkInventoryItemSeenResponseDto>.Fail(
                "The item is no longer in your inventory.");

        var inventory = await _inventory.GetInventoryByIdAsync(
            request.CharacterId,
            cancellationToken);
        if (inventory is null)
            return Response<MarkInventoryItemSeenResponseDto>.Fail(
                "The inventory could not be loaded.");

        return Response<MarkInventoryItemSeenResponseDto>.Success(new MarkInventoryItemSeenResponseDto
        {
            ItemInstanceId = request.ItemInstanceId,
            InventoryItems = _mapper.Map<List<InventoryItemDto>>(inventory.InventoryItems)
        });
    }
}
