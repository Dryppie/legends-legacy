using Application.Interfaces.Services.LL.Items;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Equipments.Queries.GetMyEquipment;
public record GetMyEquipmentQuery(Guid EntityId) : IRequest<List<EquipmentSlotDto>>;

public class GetEquipmentQueryHandler : IRequestHandler<GetMyEquipmentQuery, List<EquipmentSlotDto>>
{
    private readonly IEquipmentSlotService _equipmentService;
    private readonly IMapper _mapper;

    public GetEquipmentQueryHandler(IEquipmentSlotService equipmentService, IMapper mapper)
    {
        _equipmentService = equipmentService;
        _mapper = mapper;
    }

    public async Task<List<EquipmentSlotDto>> Handle(GetMyEquipmentQuery request, CancellationToken cancellationToken)
    {
        var equipment = await _equipmentService.GetEquipmentSlotsByEntityIdAsync(request.EntityId, cancellationToken);

        return _mapper.Map<List<EquipmentSlotDto>>(equipment);
    }
}
