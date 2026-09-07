using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;

namespace Application.UseCases.Equipments.Queries.GetEquipmentLoadouts;

public sealed record GetEquipmentLoadoutsQuery(Guid CharacterId) : IQuery<List<EquipmentLoadoutDto>>;

public sealed class GetEquipmentLoadoutsQueryHandler(IEquipmentLoadoutService service, IMapper mapper) : IRequestHandler<GetEquipmentLoadoutsQuery, List<EquipmentLoadoutDto>>
{
    public async Task<List<EquipmentLoadoutDto>> Handle(GetEquipmentLoadoutsQuery request, CancellationToken ct) =>
        mapper.Map<List<EquipmentLoadoutDto>>(await service.GetAsync(request.CharacterId, ct));
}
