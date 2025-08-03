using Application.Interfaces.Services.LL.Items;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Items.Equipments.Slots;
using MediatR;

namespace Application.UseCases.Equipments.Commands.EquipEquipment;
public record EquipEquipmentCommand(Guid EntityId, string EquipmentId, EquipmentSlotType SlotType) : IRequest<Response<bool>>;
public class EquipEquipmentCommandHandler : IRequestHandler<EquipEquipmentCommand, Response<bool>>
{
    private readonly IEquipmentSlotService _equipmentService;

    public EquipEquipmentCommandHandler(IEquipmentSlotService equipmentService, IMapper mapper)
    {
        _equipmentService = equipmentService;
    }

    public async Task<Response<bool>> Handle(EquipEquipmentCommand request, CancellationToken cancellationToken)
    {
        return await _equipmentService.EquipEquipmentAsync(request.EntityId, Guid.Parse(request.EquipmentId), request.SlotType, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to equip item.");
    }
}