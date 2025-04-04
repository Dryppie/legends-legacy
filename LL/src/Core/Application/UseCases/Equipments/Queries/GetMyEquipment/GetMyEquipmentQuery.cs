using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Equipments.Queries.GetMyEquipment;
public record GetEquipmentQuery(Guid EquipmentId) : IRequest<EquipmentDto>;

public class GetEquipmentQueryHandler : IRequestHandler<GetEquipmentQuery, EquipmentDto>
{
    private readonly IEquipmentService _equipmentService;
    private readonly IMapper _mapper;

    public GetEquipmentQueryHandler(IEquipmentService equipmentService, IMapper mapper)
    {
        _equipmentService = equipmentService;
        _mapper = mapper;
    }

    public async Task<EquipmentDto> Handle(GetEquipmentQuery request, CancellationToken cancellationToken)
    {
        var equipment = await _equipmentService.GetEquipmentByIdAsync(request.EquipmentId, cancellationToken);

        return _mapper.Map<EquipmentDto>(equipment);
    }
}
