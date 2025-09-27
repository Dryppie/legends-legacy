using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Items.Equipments.Slots;
using MediatR;

namespace Application.UseCases.Equipments.Commands.UnequipEquipment;
public record UnequipEquipmentCommand(Guid EntityId, EquipmentSlotType SlotType) : ICommand<Response<bool>>;
public class UnequipEquipmentCommandHandler : IRequestHandler<UnequipEquipmentCommand, Response<bool>>
{
    private readonly IEquipmentSlotService _equipmentService;

    public UnequipEquipmentCommandHandler(IEquipmentSlotService equipmentService)
    {
        _equipmentService = equipmentService;
    }
    public async Task<Response<bool>> Handle(UnequipEquipmentCommand request, CancellationToken cancellationToken)
    {
        return await _equipmentService.UnequipEquipmentAsync(request.EntityId, request.SlotType, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to unequip item.");
    }
}