using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Equipments.Queries.GetEquipmentAccess;

public sealed record GetEquipmentAccessQuery(Guid CharacterId) : IQuery<EquipmentAccessDto>;

public sealed class GetEquipmentAccessQueryHandler(IStarterEquipmentService service, IMapper mapper)
    : IRequestHandler<GetEquipmentAccessQuery, EquipmentAccessDto>
{
    public async Task<EquipmentAccessDto> Handle(GetEquipmentAccessQuery request, CancellationToken cancellationToken) =>
        mapper.Map<EquipmentAccessDto>(await service.GetAccessAsync(request.CharacterId, cancellationToken));
}
