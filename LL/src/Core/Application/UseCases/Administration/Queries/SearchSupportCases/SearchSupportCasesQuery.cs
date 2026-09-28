using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;
namespace Application.UseCases.Administration.Queries.SearchSupportCases;
public sealed record SearchSupportCasesQuery(Guid? CharacterId, SupportCaseStatus? Status, string? Search, int Page) : IQuery<Response<SupportCasePageDto>>;
public sealed class SearchSupportCasesQueryHandler(ISupportCaseService service, IMapper mapper) : IRequestHandler<SearchSupportCasesQuery, Response<SupportCasePageDto>>
{
    public async Task<Response<SupportCasePageDto>> Handle(SearchSupportCasesQuery request, CancellationToken ct) =>
        Response<SupportCasePageDto>.Success(mapper.Map<SupportCasePageDto>(await service.SearchAsync(request.CharacterId, request.Status, request.Search, request.Page, ct)));
}
