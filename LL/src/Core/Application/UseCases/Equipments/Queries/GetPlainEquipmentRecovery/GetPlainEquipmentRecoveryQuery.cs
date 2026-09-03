using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using MediatR;
namespace Application.UseCases.Equipments.Queries.GetPlainEquipmentRecovery;

public sealed record GetPlainEquipmentRecoveryQuery(Guid CharacterId) : IQuery<IReadOnlyList<PlainEquipmentRecoveryOptionDto>>;
public sealed class GetPlainEquipmentRecoveryQueryHandler(IPlainEquipmentRecoveryService service, IMapper mapper)
    : IRequestHandler<GetPlainEquipmentRecoveryQuery, IReadOnlyList<PlainEquipmentRecoveryOptionDto>>
{
    public async Task<IReadOnlyList<PlainEquipmentRecoveryOptionDto>> Handle(GetPlainEquipmentRecoveryQuery request, CancellationToken ct) =>
        mapper.Map<IReadOnlyList<PlainEquipmentRecoveryOptionDto>>(await service.GetOptionsAsync(request.CharacterId, ct));
}
