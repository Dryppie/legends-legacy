using Application.Interfaces.Services.LL.PowerRatings;
using Application.MediatR.Markers;
using Domain.Models.Dungeons.Definitions;
using MediatR;

namespace Application.UseCases.Dungeons.Queries.GetDungeonReadiness;

public sealed record GetDungeonReadinessQuery(
    Guid CharacterId,
    string DungeonId,
    DungeonTier Tier,
    IReadOnlyList<Guid> CompanionIds) : IQuery<DungeonReadinessResult>;

public sealed class GetDungeonReadinessQueryHandler
    : IRequestHandler<GetDungeonReadinessQuery, DungeonReadinessResult>
{
    private readonly IDungeonReadinessService _readiness;

    public GetDungeonReadinessQueryHandler(IDungeonReadinessService readiness)
    {
        _readiness = readiness;
    }

    public Task<DungeonReadinessResult> Handle(
        GetDungeonReadinessQuery request,
        CancellationToken cancellationToken) =>
        _readiness.AnalyzeAsync(
            request.CharacterId,
            request.DungeonId,
            request.Tier,
            new DungeonPartySelection(request.CompanionIds),
            cancellationToken);
}
