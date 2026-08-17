using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Administration.Queries.SearchPlayers;

public sealed record SearchPlayersQuery(string Query, int Limit = 20)
    : IQuery<Response<IReadOnlyList<PlayerAdministrationDto>>>;

public sealed class SearchPlayersQueryHandler(
    ILiveOpsService liveOps,
    IMapper mapper)
    : IRequestHandler<SearchPlayersQuery, Response<IReadOnlyList<PlayerAdministrationDto>>>
{
    public async Task<Response<IReadOnlyList<PlayerAdministrationDto>>> Handle(
        SearchPlayersQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Response<IReadOnlyList<PlayerAdministrationDto>>.Fail(
                "Enter at least two characters, an account ID, or a character ID.");
        }

        var players = await liveOps.SearchPlayersAsync(
            request.Query,
            request.Limit,
            cancellationToken);
        return Response<IReadOnlyList<PlayerAdministrationDto>>.Success(
            mapper.Map<IReadOnlyList<PlayerAdministrationDto>>(players));
    }
}
