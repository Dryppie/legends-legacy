using Application.Interfaces.Services.LL.Items;
using AutoMapper;
using Domain.Models.Items.Equipments.Slots;
using MediatR;

namespace Application.UseCases.Equipments.Commands.UnequipEquipment;
public record UnequipEquipmentCommand(Guid EntityId, EquipmentType EquipmentType) : IRequest<bool>;
public class UnequipEquipmentCommandHandler : IRequestHandler<UnequipEquipmentCommand, bool>
{
    private readonly IEquipmentSlotService _equipmentService;
    private readonly IMapper _mapper;

    public UnequipEquipmentCommandHandler(IEquipmentSlotService equipmentService, IMapper mapper)
    {
        _equipmentService = equipmentService;
        _mapper = mapper;
    }
    public Task<bool> Handle(UnequipEquipmentCommand request, CancellationToken cancellationToken)
    {
        return _equipmentService.UnequipEquipmentAsync(request.EntityId, request.EquipmentType, cancellationToken);
    }
}