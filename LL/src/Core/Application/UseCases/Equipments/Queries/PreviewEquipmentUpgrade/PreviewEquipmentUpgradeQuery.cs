using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Queries.PreviewEquipmentUpgrade;

public sealed record PreviewEquipmentUpgradeQuery(
    Guid CharacterId,
    EquipmentUpgradeRequest Operation) : IQuery<EquipmentUpgradeQuoteDto>;

public sealed class PreviewEquipmentUpgradeQueryHandler(
    IEquipmentUpgradeService service,
    IMapper mapper)
    : IRequestHandler<PreviewEquipmentUpgradeQuery, EquipmentUpgradeQuoteDto>
{
    public async Task<EquipmentUpgradeQuoteDto> Handle(
        PreviewEquipmentUpgradeQuery request,
        CancellationToken cancellationToken) =>
        mapper.Map<EquipmentUpgradeQuoteDto>(await service.PreviewAsync(
            request.CharacterId,
            request.Operation,
            cancellationToken));
}
