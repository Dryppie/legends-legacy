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

namespace Application.UseCases.Equipments.Commands.EquipEquipment;

public record EquipEquipmentCommand(Guid EntityId, string EquipmentId, EquipmentSlotType? SlotType) : ICommand<Response<EquipmentChangeResponseDto>>;

public class EquipEquipmentCommandHandler : IRequestHandler<EquipEquipmentCommand, Response<EquipmentChangeResponseDto>>
{
    private readonly IEquipmentSlotService _equipmentService;
    private readonly IInventoryService _inventoryService;
    private readonly IGameEventOutbox _outbox;
    private readonly IMapper _mapper;

    public EquipEquipmentCommandHandler(
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

    public async Task<Response<EquipmentChangeResponseDto>> Handle(EquipEquipmentCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.EquipmentId, out var equipmentId))
            return Response<EquipmentChangeResponseDto>.Fail("Failed to equip item.");

        var equipResult = await _equipmentService.EquipEquipmentAsync(request.EntityId, equipmentId, request.SlotType, cancellationToken);
        if (!equipResult.Succeeded)
            return Response<EquipmentChangeResponseDto>.Fail(
                equipResult.ErrorMessage ?? "Failed to equip item.");

        await _outbox.EnqueueAsync(
            GameEventTypes.EquipmentChanged,
            new EquipmentChangedPayload(request.EntityId),
            request.EntityId,
            null,
            cancellationToken);

        var equipment = await _equipmentService.GetEquipmentSlotsByEntityIdAsync(request.EntityId, cancellationToken);
        var inventory = await _inventoryService.GetInventoryByIdAsync(request.EntityId, cancellationToken);
        if (inventory == null)
            return Response<EquipmentChangeResponseDto>.Fail("Failed to equip item.");

        return Response<EquipmentChangeResponseDto>.Success(new EquipmentChangeResponseDto
        {
            EquipmentSlots = _mapper.Map<List<EquipmentSlotDto>>(equipment),
            InventoryItems = _mapper.Map<List<InventoryItemDto>>(inventory.InventoryItems)
        });
    }
}
