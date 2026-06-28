using Application.Interfaces.Services.LL.CombatStyles;
using Application.MediatR.Markers;
using Application.UseCases.CombatStyles.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.CombatStyles.Queries.GetCombatStyles;

public sealed record GetCombatStylesQuery(Guid CharacterId) : IQuery<CombatStylesOverviewDto>;

public sealed class GetCombatStylesQueryHandler : IRequestHandler<GetCombatStylesQuery, CombatStylesOverviewDto>
{
    private readonly ICombatStyleService _service;
    private readonly IMapper _mapper;

    public GetCombatStylesQueryHandler(ICombatStyleService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    public async Task<CombatStylesOverviewDto> Handle(GetCombatStylesQuery request, CancellationToken cancellationToken)
    {
        var overview = await _service.GetOverviewAsync(request.CharacterId, cancellationToken);
        return _mapper.Map<CombatStylesOverviewDto>(overview);
    }
}
