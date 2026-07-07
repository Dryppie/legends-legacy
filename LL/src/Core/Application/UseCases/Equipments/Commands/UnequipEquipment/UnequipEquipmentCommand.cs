using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Outbox;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Outbox;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Items.Equipments.Slots;
using MediatR;

namespace Application.UseCases.Equipments.Commands.UnequipEquipment;

public record UnequipEquipmentCommand(Guid EntityId, EquipmentSlotType SlotType) : ICommand<Response<EquipmentChangeResponseDto>>;

public class UnequipEquipmentCommandHandler : IRequestHandler<UnequipEquipmentCommand, Response<EquipmentChangeResponseDto>>
{
    private readonly IEquipmentSlotService _equipmentService;
    private readonly IInventoryService _inventoryService;
    private readonly IGameEventOutbox _outbox;
    private readonly IMapper _mapper;

    public UnequipEquipmentCommandHandler(
        IEquipmentSlotService equipmentService,
        IInventoryService inventoryService,
        IMapper mapper,
        IGameEventOutbox outbox)
    {
        _equipmentService = equipmentService;
        _inventoryService = inventoryService;
        _mapper = mapper;
        _outbox = outbox;
    }

    public async Task<Response<EquipmentChangeResponseDto>> Handle(UnequipEquipmentCommand request, CancellationToken cancellationToken)
    {
        var success = await _equipmentService.UnequipEquipmentAsync(request.EntityId, request.SlotType, cancellationToken);
        if (!success)
            return Response<EquipmentChangeResponseDto>.Fail("Failed to unequip item.");

        await _outbox.EnqueueAsync(
            GameEventTypes.EquipmentChanged,
            new EquipmentChangedPayload(request.EntityId),
            request.EntityId,
            null,
            cancellationToken);

        var equipment = await _equipmentService.GetEquipmentSlotsByEntityIdAsync(request.EntityId, cancellationToken);
        var inventory = await _inventoryService.GetInventoryByIdAsync(request.EntityId, cancellationToken);
        if (inventory == null)
            return Response<EquipmentChangeResponseDto>.Fail("Failed to unequip item.");

        return Response<EquipmentChangeResponseDto>.Success(new EquipmentChangeResponseDto
        {
            EquipmentSlots = _mapper.Map<List<EquipmentSlotDto>>(equipment),
            InventoryItems = _mapper.Map<List<InventoryItemDto>>(inventory.InventoryItems)
        });
    }
}
