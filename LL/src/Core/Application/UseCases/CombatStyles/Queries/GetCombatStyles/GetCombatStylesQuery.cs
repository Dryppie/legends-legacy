using Application.Interfaces.Services.LL.CombatStyles;
using Application.MediatR.Markers;
using Application.UseCases.CombatStyles.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.CombatStyles.Queries.GetCombatStyles;

public sealed record GetCombatStylesQuery(Guid CharacterId) : IQuery<CombatStyleOverviewDto>;

public sealed class GetCombatStylesQueryHandler(ICombatStyleService service, IMapper mapper)
    : IRequestHandler<GetCombatStylesQuery, CombatStyleOverviewDto>
{
    public async Task<CombatStyleOverviewDto> Handle(GetCombatStylesQuery request, CancellationToken ct) =>
        mapper.Map<CombatStyleOverviewDto>(await service.GetOverviewAsync(request.CharacterId, ct));
}
