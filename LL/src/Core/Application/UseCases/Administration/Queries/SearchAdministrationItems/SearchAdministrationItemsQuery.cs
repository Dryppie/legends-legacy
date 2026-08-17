using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Administration.Queries.SearchAdministrationItems;

public sealed record SearchAdministrationItemsQuery(string Query, int Limit = 20)
    : IQuery<Response<IReadOnlyList<AdministrationItemDto>>>;

public sealed class SearchAdministrationItemsQueryHandler(
    ILiveOpsService liveOps,
    IMapper mapper)
    : IRequestHandler<SearchAdministrationItemsQuery, Response<IReadOnlyList<AdministrationItemDto>>>
{
    public async Task<Response<IReadOnlyList<AdministrationItemDto>>> Handle(
        SearchAdministrationItemsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query) || request.Query.Trim().Length < 2)
        {
            return Response<IReadOnlyList<AdministrationItemDto>>.Fail(
                "Enter at least two characters of an item name or ID.");
        }

        var items = await liveOps.SearchItemsAsync(
            request.Query,
            request.Limit,
            cancellationToken);
        return Response<IReadOnlyList<AdministrationItemDto>>.Success(
            mapper.Map<IReadOnlyList<AdministrationItemDto>>(items));
    }
}
