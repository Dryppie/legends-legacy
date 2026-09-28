using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Equipments.Queries.GetMigratedSpecializationChoice;

public sealed record GetMigratedSpecializationChoiceQuery(Guid CharacterId, Guid ItemId) : IQuery<Response<EquipmentMigrationChoiceDto?>>;
public sealed class GetMigratedSpecializationChoiceQueryHandler(IEquipmentMigrationService service, IMapper mapper)
    : IRequestHandler<GetMigratedSpecializationChoiceQuery, Response<EquipmentMigrationChoiceDto?>>
{
    public async Task<Response<EquipmentMigrationChoiceDto?>> Handle(GetMigratedSpecializationChoiceQuery request, CancellationToken ct) =>
        Response<EquipmentMigrationChoiceDto?>.Success(mapper.Map<EquipmentMigrationChoiceDto?>(await service.GetChoiceAsync(request.CharacterId, request.ItemId, ct)));
}
