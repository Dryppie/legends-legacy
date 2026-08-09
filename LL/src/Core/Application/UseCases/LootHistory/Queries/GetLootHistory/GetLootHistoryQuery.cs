using Application.Interfaces.Services.LL.Inventories;
using Application.MediatR.Markers;
using Application.UseCases.LootHistory.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.LootHistory.Queries.GetLootHistory;

public sealed record GetLootHistoryQuery(Guid CharacterId)
    : IQuery<Response<IReadOnlyList<LootHistoryEntryDto>>>;

public sealed class GetLootHistoryQueryHandler
    : IRequestHandler<GetLootHistoryQuery, Response<IReadOnlyList<LootHistoryEntryDto>>>
{
    private readonly ILootHistoryService _lootHistory;

    public GetLootHistoryQueryHandler(ILootHistoryService lootHistory)
    {
        _lootHistory = lootHistory;
    }

    public async Task<Response<IReadOnlyList<LootHistoryEntryDto>>> Handle(
        GetLootHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var entries = await _lootHistory.GetRecentAsync(request.CharacterId, cancellationToken);
        return Response<IReadOnlyList<LootHistoryEntryDto>>.Success(entries);
    }
}
