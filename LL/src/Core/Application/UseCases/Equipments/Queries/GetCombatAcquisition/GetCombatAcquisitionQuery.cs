using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Equipments.Queries.GetCombatAcquisition;
public sealed record GetCombatAcquisitionQuery(Guid CharacterId) : IQuery<IReadOnlyList<CombatAcquisitionDto>>;
public sealed class GetCombatAcquisitionQueryHandler(ICombatAcquisitionService service, IMapper mapper)
    : IRequestHandler<GetCombatAcquisitionQuery, IReadOnlyList<CombatAcquisitionDto>>
{
    public async Task<IReadOnlyList<CombatAcquisitionDto>> Handle(GetCombatAcquisitionQuery request, CancellationToken ct) =>
        mapper.Map<IReadOnlyList<CombatAcquisitionDto>>(await service.GetAsync(request.CharacterId, ct));
}
