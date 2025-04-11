using Application.Interfaces.Services.LL.Items;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Equipments.Commands.EquipEquipment;
public record EquipEquipmentCommand(Guid EntityId, string EquipmentId) : IRequest<bool>;
public class EquipEquipmentCommandHandler : IRequestHandler<EquipEquipmentCommand, bool>
{
    private readonly IEquipmentSlotService _equipmentService;
    private readonly IMapper _mapper;

    public EquipEquipmentCommandHandler(IEquipmentSlotService equipmentService, IMapper mapper)
    {
        _equipmentService = equipmentService;
        _mapper = mapper;
    }
    public Task<bool> Handle(EquipEquipmentCommand request, CancellationToken cancellationToken)
    {
        return _equipmentService.EquipEquipmentAsync(request.EntityId, Guid.Parse(request.EquipmentId), cancellationToken);
    }
}