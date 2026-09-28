using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Mappings;
using AutoMapper;
using Common.Primitives;
using MediatR;
namespace Application.UseCases.Administration.Queries.GetSupportCase;
public sealed record GetSupportCaseQuery(Guid CaseId, int? BeforeSequence) : IQuery<Response<SupportCaseDetailsDto>>;
public sealed class GetSupportCaseQueryHandler(ISupportCaseService service, IMapper mapper) : IRequestHandler<GetSupportCaseQuery, Response<SupportCaseDetailsDto>>
{
    public async Task<Response<SupportCaseDetailsDto>> Handle(GetSupportCaseQuery request, CancellationToken ct) =>
        SupportCaseMappingProfile.Map(await service.GetAsync(request.CaseId, request.BeforeSequence, ct), mapper);
}
