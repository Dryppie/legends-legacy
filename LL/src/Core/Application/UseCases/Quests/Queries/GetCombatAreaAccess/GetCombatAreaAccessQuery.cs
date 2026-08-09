using Application.Interfaces.Services.LL.Quests;
using Application.MediatR.Markers;
using Application.UseCases.Quests.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Quests.Queries.GetCombatAreaAccess;

public sealed record GetCombatAreaAccessQuery(Guid CharacterId)
    : IQuery<IReadOnlyList<CombatAreaAccessDto>>;

public sealed class GetCombatAreaAccessQueryHandler(
    ICombatAreaAccessService accessService,
    IMapper mapper) : IRequestHandler<GetCombatAreaAccessQuery, IReadOnlyList<CombatAreaAccessDto>>
{
    public async Task<IReadOnlyList<CombatAreaAccessDto>> Handle(
        GetCombatAreaAccessQuery request,
        CancellationToken cancellationToken) =>
        mapper.Map<IReadOnlyList<CombatAreaAccessDto>>(
            await accessService.GetAllAccessAsync(request.CharacterId, cancellationToken));
}
