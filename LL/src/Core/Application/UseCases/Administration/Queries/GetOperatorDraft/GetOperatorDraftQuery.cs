using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Mappings;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Administration.Queries.GetOperatorDraft;

public sealed record GetOperatorDraftQuery(string Actor, string Key) : IQuery<Response<OperatorDraftDto>>;
public sealed class GetOperatorDraftQueryHandler(IOperatorDraftService service, IMapper mapper)
    : IRequestHandler<GetOperatorDraftQuery, Response<OperatorDraftDto>>
{
    public async Task<Response<OperatorDraftDto>> Handle(GetOperatorDraftQuery request, CancellationToken ct) =>
        OperatorDraftMappingProfile.Map(await service.GetAsync(request.Actor, request.Key, ct), mapper);
}
