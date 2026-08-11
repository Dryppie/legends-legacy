using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Bonuses;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Regions.Areas;
using Services.LL.Extensions;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class IdleDungeonSigilDropCalculator : IIdleDungeonSigilDropCalculator
{
    private readonly IDungeonDefinitions _dungeons;
    private readonly IItemBaseRepository _itemBases;
    private readonly IRandomSource _randomSource;
    private readonly IInventoryItemFactory _inventoryItemFactory;
    private readonly IBonusService _bonusService;

    private const int IdleActionsPerDay = 24 * 60 * 60 / 10;
    private const double TargetSigilDropsPerDay = 2d;
    private const double SigilDropChancePerIdleAction = TargetSigilDropsPerDay / IdleActionsPerDay;
    public IdleDungeonSigilDropCalculator(
        IDungeonDefinitions dungeons,
        IItemBaseRepository itemBases,
        IRandomSource randomSource,
        IInventoryItemFactory inventoryItemFactory,
        IBonusService bonusService)
    {
        _dungeons = dungeons;
        _itemBases = itemBases;
        _randomSource = randomSource;
        _inventoryItemFactory = inventoryItemFactory;
        _bonusService = bonusService;
    }

    public async Task<IReadOnlyList<InventoryItem>> RollAsync(
        Guid characterId,
        Area area,
        int eligibleVictories,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<BonusKind, double>? bonusFactors = null)
    {
        if (eligibleVictories <= 0)
        {
            return [];
        }

        var sigilIds = GetSigilIdsForArea(area);
        if (sigilIds.Count == 0)
        {
            return [];
        }

        var factors = bonusFactors ?? await _bonusService.GetAggregatedAsync(characterId, DateTimeOffset.UtcNow, cancellationToken);
        var sigilDropRateBps = factors.Get(BonusKind.DungeonSigilDropRateRelativeBps);
        var dropCount = SamplePoisson(
            (eligibleVictories * SigilDropChancePerIdleAction).ApplyPositiveBps(sigilDropRateBps));
        if (dropCount <= 0)
        {
            return [];
        }

        var itemBases = await _itemBases.GetItemBasesByIdsAsync(sigilIds, cancellationToken);
        var quantitiesBySigilId = RollQuantities(sigilIds, dropCount);
        var drops = new List<InventoryItem>();

        foreach (var (sigilId, quantity) in quantitiesBySigilId)
        {
            if (!itemBases.TryGetValue(sigilId, out var itemBase))
            {
                continue;
            }

            drops.Add(_inventoryItemFactory.Create(itemBase, quantity));
        }

        return drops;
    }

    private IReadOnlyList<string> GetSigilIdsForArea(Area area)
    {
        var region = ResolveRegion(area.Id);
        if (region is null)
        {
            return [];
        }

        return _dungeons.GetAll()
            .Where(dungeon => dungeon.Region == region)
            .Select(dungeon => dungeon.SigilItemId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private Dictionary<string, int> RollQuantities(
        IReadOnlyList<string> sigilIds,
        int dropCount)
    {
        var quantitiesBySigilId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < dropCount; i++)
        {
            var sigilId = PickRandomSigilId(sigilIds);
            quantitiesBySigilId[sigilId] = quantitiesBySigilId.GetValueOrDefault(sigilId) + 1;
        }

        return quantitiesBySigilId;
    }

    private int SamplePoisson(double lambda)
    {
        if (lambda <= 0)
        {
            return 0;
        }

        var drops = 0;
        var probability = 1.0;
        var threshold = Math.Exp(-lambda);

        while (probability > threshold)
        {
            drops++;
            probability *= _randomSource.NextDouble();
        }

        return drops - 1;
    }

    private string PickRandomSigilId(IReadOnlyList<string> sigilIds)
    {
        var index = Math.Min((int)(_randomSource.NextDouble() * sigilIds.Count), sigilIds.Count - 1);
        return sigilIds[index];
    }

    private static int? ResolveRegion(string? areaId)
    {
        if (string.IsNullOrWhiteSpace(areaId))
        {
            return null;
        }

        const string areaMarker = "_area_";
        var markerIndex = areaId.IndexOf(areaMarker, StringComparison.OrdinalIgnoreCase);

        const string regionPrefix = "region_";
        if (markerIndex <= regionPrefix.Length ||
            !areaId.StartsWith(regionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return int.TryParse(
            areaId.AsSpan(regionPrefix.Length, markerIndex - regionPrefix.Length),
            out var region)
            ? region
            : null;
    }
}
