using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Equipments.Queries.GetForgeStyles;

public sealed record GetForgeStylesQuery(Guid CharacterId, Guid ItemInstanceId) : IQuery<IReadOnlyList<ForgeStyleOptionDto>>;
public sealed class GetForgeStylesQueryHandler(IForgeService service, IMapper mapper)
    : IRequestHandler<GetForgeStylesQuery, IReadOnlyList<ForgeStyleOptionDto>>
{
    public async Task<IReadOnlyList<ForgeStyleOptionDto>> Handle(GetForgeStylesQuery request, CancellationToken ct) =>
        mapper.Map<IReadOnlyList<ForgeStyleOptionDto>>(await service.GetStylesAsync(request.CharacterId, request.ItemInstanceId, ct));
}
