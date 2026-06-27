using Application.Interfaces.Services.LL.CombatStyles;
using Application.MediatR.Markers;
using Application.UseCases.CombatStyles.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.CombatStyles.Queries.GetCombatBuildPreview;

public sealed record GetCombatBuildPreviewQuery(Guid CharacterId) : IQuery<CombatBuildPreviewDto>;

public sealed class GetCombatBuildPreviewQueryHandler : IRequestHandler<GetCombatBuildPreviewQuery, CombatBuildPreviewDto>
{
    private readonly ICombatStyleService _service;
    private readonly IMapper _mapper;

    public GetCombatBuildPreviewQueryHandler(ICombatStyleService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    public async Task<CombatBuildPreviewDto> Handle(GetCombatBuildPreviewQuery request, CancellationToken cancellationToken)
    {
        var preview = await _service.GetBuildPreviewAsync(request.CharacterId, cancellationToken);
        return _mapper.Map<CombatBuildPreviewDto>(preview);
    }
}
