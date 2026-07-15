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

    public async Task<IReadOnlyList<ProphecyDefinition>> SyncDefinitionsAsync(IReadOnlyCollection<ProphecyDefinition> definitions, CancellationToken cancellationToken)
    {
        var existing = await _context.ProphecyDefinitions
            .ToListAsync(cancellationToken);

        var existingById = existing.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var authoredIds = definitions.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var staleDefinition in existing.Where(x => !authoredIds.Contains(x.Id)))
        {
            staleDefinition.IsEnabled = false;
        }

        foreach (var definition in definitions)
        {
            if (existingById.TryGetValue(definition.Id, out var existingDefinition))
            {
                CopyDefinition(definition, existingDefinition);
                continue;
            }

            var newDefinition = CloneDefinition(definition);
            await _context.ProphecyDefinitions.AddAsync(newDefinition, cancellationToken);
            existingById.Add(newDefinition.Id, newDefinition);
        }

        return definitions.Select(x => existingById[x.Id]).ToList();
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

    public async Task<IReadOnlyList<PlayerProphecyInstance>> GetAcceptedInstancesForProgressWindowAsync(
        Guid characterId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken) =>
        await _context.PlayerProphecyInstances
            .Include(x => x.ProphecyDefinition)
            .Where(x =>
                x.CharacterId == characterId &&
                x.Status == ProphecyStatus.Accepted &&
                x.AcceptedAt <= to &&
                x.PeriodStart <= to &&
                x.PeriodEnd > from)
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

    public async Task<IReadOnlySet<string>> GetRecentDefinitionIdsAsync(
        Guid playerId,
        Guid characterId,
        ProphecyScope scope,
        DateTimeOffset since,
        DateTimeOffset before,
        CancellationToken cancellationToken)
    {
        var instances = await _context.PlayerProphecyInstances
            .AsNoTracking()
            .Where(x =>
                x.PlayerId == playerId &&
                x.CharacterId == characterId &&
                x.Scope == scope &&
                x.GeneratedAt >= since &&
                x.GeneratedAt < before)
            .Select(x => new { x.ProphecyDefinitionId, x.RerolledFromDefinitionId })
            .ToListAsync(cancellationToken);

        return instances
            .SelectMany(x => new[] { x.ProphecyDefinitionId, x.RerolledFromDefinitionId })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> TryConsumeDailyRerollAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset periodStart,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken)
    {
        var updated = await _context.PlayerProphecyInstances
            .Where(x =>
                x.PlayerId == playerId &&
                x.CharacterId == characterId &&
                x.Scope == ProphecyScope.Daily &&
                x.PeriodStart == periodStart &&
                x.SlotType == ProphecySlotType.Steady &&
                x.DailyRerollUsedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.DailyRerollUsedAt, usedAt),
                cancellationToken);

        return updated == 1;
    }

    public async Task<DailyProphecyRerollState?> GetDailyRerollStateAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset periodStart,
        CancellationToken cancellationToken)
    {
        var local = _context.DailyProphecyRerollStates.Local.FirstOrDefault(x =>
            x.PlayerId == playerId &&
            x.CharacterId == characterId &&
            x.PeriodStart == periodStart);
        return local ?? await _context.DailyProphecyRerollStates.FirstOrDefaultAsync(x =>
            x.PlayerId == playerId &&
            x.CharacterId == characterId &&
            x.PeriodStart == periodStart,
            cancellationToken);
    }

    public async Task AddDailyRerollStateAsync(
        DailyProphecyRerollState state,
        CancellationToken cancellationToken) =>
        await _context.DailyProphecyRerollStates.AddAsync(state, cancellationToken);

    public async Task<bool> TrySpendFateEchoAsync(
        Guid characterId,
        long amount,
        CancellationToken cancellationToken)
    {
        return await _context.Characters
            .Where(x => x.Id == characterId && x.FateEcho >= amount)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.FateEcho, x => x.FateEcho - amount),
                cancellationToken) == 1;
    }

    public async Task<WeeklyRevelationProgress?> GetWeeklyProgressAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset periodStart,
        CancellationToken cancellationToken) =>
        await _context.WeeklyRevelationProgress
            .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.CharacterId == characterId && x.PeriodStart == periodStart, cancellationToken);

    public async Task AddWeeklyProgressAsync(WeeklyRevelationProgress progress, CancellationToken cancellationToken) =>
        await _context.WeeklyRevelationProgress.AddAsync(progress, cancellationToken);

    private static ProphecyDefinition CloneDefinition(ProphecyDefinition source)
    {
        var target = new ProphecyDefinition
        {
            Id = source.Id
        };

        CopyDefinition(source, target);
        return target;
    }

    private static void CopyDefinition(ProphecyDefinition source, ProphecyDefinition target)
    {
        target.Title = source.Title;
        target.FlavorText = source.FlavorText;
        target.ObjectiveText = source.ObjectiveText;
        target.Scope = source.Scope;
        target.Category = source.Category;
        target.Difficulty = source.Difficulty;
        target.ObjectiveType = source.ObjectiveType;
        target.ObjectiveParameterJson = source.ObjectiveParameterJson;
        target.RewardProfileId = source.RewardProfileId;
        target.Weight = source.Weight;
        target.IsEnabled = source.IsEnabled;
        target.AllowedSlots = source.AllowedSlots.ToList();
        target.RequiredFeatures = source.RequiredFeatures.ToList();
        target.RequiredTags = source.RequiredTags.ToList();
        target.ExcludedTags = source.ExcludedTags.ToList();
        target.MinPlayerLevel = source.MinPlayerLevel;
        target.MaxPlayerLevel = source.MaxPlayerLevel;
    }
}
