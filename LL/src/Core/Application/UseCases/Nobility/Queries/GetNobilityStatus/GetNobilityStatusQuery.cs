using Application.Interfaces.Services.LL.Nobility;
using Application.MediatR.Markers;
using Application.UseCases.Nobility.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Nobility.Queries.GetNobilityStatus;

public sealed record GetNobilityStatusQuery(Guid AccountId, Guid CharacterId) : IQuery<NobilityStatusDto>;

public sealed class GetNobilityStatusQueryHandler(INobilityService service, IMapper mapper)
    : IRequestHandler<GetNobilityStatusQuery, NobilityStatusDto>
{
    public async Task<NobilityStatusDto> Handle(GetNobilityStatusQuery request, CancellationToken ct) =>
        mapper.Map<NobilityStatusDto>(await service.GetStatusAsync(request.AccountId, request.CharacterId, ct));
}
