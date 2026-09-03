using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Equipments.Queries.GetBaselineEquipmentRecovery;
public sealed record GetBaselineEquipmentRecoveryQuery(Guid CharacterId) : IQuery<IReadOnlyList<BaselineEquipmentRecoveryOptionDto>>;
public sealed class GetBaselineEquipmentRecoveryQueryHandler(IEquipmentAcquisitionService service, IMapper mapper)
    : IRequestHandler<GetBaselineEquipmentRecoveryQuery, IReadOnlyList<BaselineEquipmentRecoveryOptionDto>>
{
    public async Task<IReadOnlyList<BaselineEquipmentRecoveryOptionDto>> Handle(GetBaselineEquipmentRecoveryQuery request, CancellationToken ct) =>
        mapper.Map<List<BaselineEquipmentRecoveryOptionDto>>(await service.GetRecoveryOptionsAsync(request.CharacterId, ct));
}
