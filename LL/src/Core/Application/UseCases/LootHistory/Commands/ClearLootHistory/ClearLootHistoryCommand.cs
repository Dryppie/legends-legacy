using Application.Interfaces.Services.LL.Inventories;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.LootHistory.Commands.ClearLootHistory;

public sealed record ClearLootHistoryCommand(Guid CharacterId) : ICommand<Response<int>>;

public sealed class ClearLootHistoryCommandHandler
    : IRequestHandler<ClearLootHistoryCommand, Response<int>>
{
    private readonly ILootHistoryService _lootHistory;

    public ClearLootHistoryCommandHandler(ILootHistoryService lootHistory)
    {
        _lootHistory = lootHistory;
    }

    public async Task<Response<int>> Handle(
        ClearLootHistoryCommand request,
        CancellationToken cancellationToken)
    {
        var deleted = await _lootHistory.ClearAsync(request.CharacterId, cancellationToken);
        return Response<int>.Success(deleted);
    }
}
