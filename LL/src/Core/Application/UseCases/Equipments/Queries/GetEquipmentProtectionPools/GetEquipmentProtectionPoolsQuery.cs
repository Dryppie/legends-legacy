using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Equipments.Queries.GetEquipmentProtectionPools;
public sealed record GetEquipmentProtectionPoolsQuery(Guid CharacterId) : IQuery<IReadOnlyList<EquipmentProtectionPoolDto>>;
public sealed class GetEquipmentProtectionPoolsQueryHandler(IEquipmentAcquisitionService service, IMapper mapper)
    : IRequestHandler<GetEquipmentProtectionPoolsQuery, IReadOnlyList<EquipmentProtectionPoolDto>>
{
    public async Task<IReadOnlyList<EquipmentProtectionPoolDto>> Handle(GetEquipmentProtectionPoolsQuery request, CancellationToken ct) =>
        mapper.Map<List<EquipmentProtectionPoolDto>>(await service.GetPoolsAsync(request.CharacterId, ct));
}
