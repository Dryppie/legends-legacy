using Application.Common.Interfaces;
using Domain.Models.Prophecies;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Prophecies;

public sealed class ProphecyRepository : IProphecyRepository
{
    private readonly IDbContext _context;

    public ProphecyRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProphecyDefinition>> GetEnabledDefinitionsAsync(CancellationToken cancellationToken) =>
        await _context.ProphecyDefinitions
            .AsNoTracking()
            .Where(x => x.IsEnabled)
            .ToListAsync(cancellationToken);

    public async Task AddMissingDefinitionsAsync(IReadOnlyCollection<ProphecyDefinition> definitions, CancellationToken cancellationToken)
    {
        var ids = definitions.Select(x => x.Id).ToArray();
        var existing = await _context.ProphecyDefinitions
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = definitions.Where(x => !existingSet.Contains(x.Id)).ToList();

        if (missing.Count > 0)
        {
            await _context.ProphecyDefinitions.AddRangeAsync(missing, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<PlayerProphecyInstance>> GetInstancesForPeriodAsync(
        Guid playerId,
        Guid characterId,
        ProphecyScope scope,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken) =>
        await _context.PlayerProphecyInstances
            .Include(x => x.ProphecyDefinition)
            .Where(x =>
                x.PlayerId == playerId &&
                x.CharacterId == characterId &&
                x.Scope == scope &&
                x.PeriodStart == periodStart &&
                x.PeriodEnd == periodEnd)
            .OrderBy(x => x.SlotType)
            .ToListAsync(cancellationToken);

    public async Task<PlayerProphecyInstance?> GetInstanceAsync(Guid instanceId, Guid playerId, Guid characterId, CancellationToken cancellationToken) =>
        await _context.PlayerProphecyInstances
            .Include(x => x.ProphecyDefinition)
            .FirstOrDefaultAsync(x => x.Id == instanceId && x.PlayerId == playerId && x.CharacterId == characterId, cancellationToken);

    public async Task AddInstancesAsync(IReadOnlyCollection<PlayerProphecyInstance> instances, CancellationToken cancellationToken)
    {
        if (instances.Count == 0)
        {
            return;
        }

        await _context.PlayerProphecyInstances.AddRangeAsync(instances, cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerProphecyInstance>> GetAcceptedInstancesForProgressAsync(
        Guid characterId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken) =>
        await _context.PlayerProphecyInstances
            .Include(x => x.ProphecyDefinition)
            .Where(x =>
                x.CharacterId == characterId &&
                x.Status == ProphecyStatus.Accepted &&
                x.AcceptedAt <= occurredAt &&
                x.PeriodStart <= occurredAt &&
                x.PeriodEnd > occurredAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PlayerProphecyInstance>> GetRecentInstancesAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset since,
        int limit,
        CancellationToken cancellationToken) =>
        await _context.PlayerProphecyInstances
            .Include(x => x.ProphecyDefinition)
            .Where(x =>
                x.PlayerId == playerId &&
                x.CharacterId == characterId &&
                x.GeneratedAt >= since &&
                x.Status != ProphecyStatus.Offered &&
                x.Status != ProphecyStatus.Accepted)
            .OrderByDescending(x => x.ClaimedAt ?? x.CompletedAt ?? x.AcceptedAt ?? x.GeneratedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<WeeklyRevelationProgress?> GetWeeklyProgressAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset periodStart,
        CancellationToken cancellationToken) =>
        await _context.WeeklyRevelationProgress
            .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.CharacterId == characterId && x.PeriodStart == periodStart, cancellationToken);

    public async Task AddWeeklyProgressAsync(WeeklyRevelationProgress progress, CancellationToken cancellationToken) =>
        await _context.WeeklyRevelationProgress.AddAsync(progress, cancellationToken);
}
