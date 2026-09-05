using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Equipments.Queries.GetEquipmentBlueprints;

public sealed record GetEquipmentBlueprintsQuery(Guid CharacterId, Guid ItemInstanceId)
    : IQuery<IReadOnlyList<EquipmentBlueprintOptionDto>>;

public sealed class GetEquipmentBlueprintsQueryHandler(IEquipmentUpgradeService service, IMapper mapper)
    : IRequestHandler<GetEquipmentBlueprintsQuery, IReadOnlyList<EquipmentBlueprintOptionDto>>
{
    public async Task<IReadOnlyList<EquipmentBlueprintOptionDto>> Handle(GetEquipmentBlueprintsQuery request, CancellationToken ct) =>
        mapper.Map<IReadOnlyList<EquipmentBlueprintOptionDto>>(
            await service.GetBlueprintsAsync(request.CharacterId, request.ItemInstanceId, ct));
}
