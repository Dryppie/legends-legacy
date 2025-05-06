using Application.Interfaces.Services.LL.Items;
using Common.Primitives;
using Domain.Models.Items.Equipments.Slots;
using MediatR;

namespace Application.UseCases.Equipments.Commands.UnequipEquipment;
public record UnequipEquipmentCommand(Guid EntityId, EquipmentType EquipmentType) : IRequest<Response<bool>>;
public class UnequipEquipmentCommandHandler : IRequestHandler<UnequipEquipmentCommand, Response<bool>>
{
    private readonly IEquipmentSlotService _equipmentService;

    public UnequipEquipmentCommandHandler(IEquipmentSlotService equipmentService)
    {
        _equipmentService = equipmentService;
    }
    public async Task<Response<bool>> Handle(UnequipEquipmentCommand request, CancellationToken cancellationToken)
    {
        return await _equipmentService.UnequipEquipmentAsync(request.EntityId, request.EquipmentType, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to unequip item.");
    }
}