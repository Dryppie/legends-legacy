using Application.Interfaces.Services.LL.CombatStyles;
using Application.MediatR.Markers;
using Application.UseCases.CombatStyles.Dtos;
using AutoMapper;
using Domain.Models.CombatStyles;
using MediatR;

namespace Application.UseCases.CombatStyles.Queries.PreviewCombatStyle;

public sealed record PreviewCombatStyleQuery(Guid CharacterId, CombatStyleSelectionDto Selection) : IQuery<CombatStyleOverviewDto>;

public sealed class PreviewCombatStyleQueryHandler(ICombatStyleService service, IMapper mapper)
    : IRequestHandler<PreviewCombatStyleQuery, CombatStyleOverviewDto>
{
    public async Task<CombatStyleOverviewDto> Handle(PreviewCombatStyleQuery request, CancellationToken ct) =>
        mapper.Map<CombatStyleOverviewDto>(await service.PreviewAsync(request.CharacterId,
            mapper.Map<CombatStyleSelectionRequest>(request.Selection), ct));
}
