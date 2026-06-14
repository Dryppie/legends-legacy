using Application.Interfaces.Services.LL;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.LootTables;
using Services.LL.Interfaces;

namespace Services.LL.Loots;
public class LootService : ILootService
{
    private static readonly Random RandomGenerator = new();
    private readonly IInventoryItemFactory _inventoryItemFactory;

    public LootService(IInventoryItemFactory inventoryItemFactory)
    {
        _inventoryItemFactory = inventoryItemFactory;
    }

    public int GenerateSoulstoneLoot(int seconds, double dropRate, double doubleChance)
    {
        double baseChance = 0.000278; // every 1 hour
                                      // 1/21600 - 0.0000463 // every 6 hour
                                      // 1/43200 - 0.0000232 // every 12 hour
        double effectiveRate = baseChance * (1 + (dropRate / 100.0));
        double expectedDrops = seconds * effectiveRate;

        int earned = SamplePoisson(expectedDrops);
        if (earned < 1) return 0;

        var rng = Random.Shared;
        if (earned > 0 && rng.NextDouble() <= doubleChance)
            earned *= 2;

        return earned;
    }

    private static int SamplePoisson(double lambda)
    {
        var rng = Random.Shared;
        int k = 0;
        double p = 1.0;
        double L = Math.Exp(-lambda);

        while (p > L)
        {
            k++;
            p *= rng.NextDouble();
        }

        return k - 1;
    }

    public int GenerateCinderLoot(Dictionary<Guid, int> creatureKills, Dictionary<Guid, int> baseCinderValues, double dropChance = 0.2)
    {
        int totalCinders = 0;

        foreach (var (creatureGuid, kills) in creatureKills)
        {
            if (kills <= 0 || !baseCinderValues.TryGetValue(creatureGuid, out int baseValue) || baseValue <= 0)
                continue;

            int drops = SampleBinomial(kills, dropChance);

            for (int i = 0; i < drops; i++)
            {
                double variation = RandomGenerator.NextDouble() * 0.4 - 0.2; // ±20%
                double cinderValue = baseValue * (1 + variation);
                totalCinders += (int)Math.Round(cinderValue);
            }
        }

        return totalCinders;
    }

    private static int SampleBinomial(int trials, double probability)
    {
        if (trials < 1000)
        {
            int success = 0;
            for (int i = 0; i < trials; i++)
                if (RandomGenerator.NextDouble() <= probability)
                    success++;
            return success;
        }

        // Normal approximation for performance
        double mean = trials * probability;
        double stdDev = Math.Sqrt(trials * probability * (1 - probability));
        return Math.Max(0, (int)Math.Round(mean + stdDev * SampleStandardNormal()));
    }

    private static double SampleStandardNormal()
    {
        double u1 = 1.0 - RandomGenerator.NextDouble();
        double u2 = 1.0 - RandomGenerator.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    public List<InventoryItem> GenerateGatheringLootAsync(LootTable lootTable, CancellationToken cancellationToken)
    {
        var ctx = new LootContext
        {
            Source = LootSource.Gathering
            //  ⟹ leave multipliers empty (1×) unless you have special rules
        };
        return GetRandomLoot(lootTable, ctx);
    }

    public List<InventoryItem> GenerateDungeonLoot(LootTable lootTable, Dictionary<ItemType, double>? multipliers = null)
    {
        var ctx = new LootContext
        {
            Source = LootSource.Combat,
            TypeMultipliers = multipliers ?? []
        };

        return GetRandomLoot(lootTable, ctx);
    }

    public List<InventoryItem> GenerateIdleCombatLootAsync(List<Entity> entities, Dictionary<ItemType, double> multipliers)
    {
        var ctx = new LootContext
        {
            Source = LootSource.Combat,
            TypeMultipliers = multipliers
        };

        var total = new List<InventoryItem>();
        foreach (var creature in entities.OfType<Creature>())
        {
            if (creature.LootTable?.Entries is null || creature.LootTable.Entries.Count == 0)
            {
                continue;
            }

            total.AddRange(GetRandomLoot(creature.LootTable, ctx));
        }

        return total;
    }

    // TODO: Redo Loot Generation
    public List<InventoryItem> GetRandomLoot(LootTable lootTable, LootContext ctx, int numberOfRolls = 1)
    {
        var generatedLoot = new List<InventoryItem>();

        for (int i = 0; i < numberOfRolls; i++)
        {

            var selectedEntry = GetRandomEntryBasedOnWeight([.. lootTable.Entries], ctx);

            if (selectedEntry is LootTableItem lootTableItem)
            {
                if (lootTableItem.Item is null)
                {
                    continue;
                }

                generatedLoot.Add(ConvertItemIntoInventoryItem(lootTableItem.Item));
            }
            else if (selectedEntry is LootTable table)
            {
                generatedLoot.AddRange(GetRandomLoot(table, ctx, 1));
            }
        }

        return generatedLoot;
    }

    private LootTableEntry? GetRandomEntryBasedOnWeight(List<LootTableEntry> entries, LootContext ctx)
    {
        var weighted = entries
            .Select(e =>
            {
                double mult = 0.0;

                if (e is LootTableItem li
                    && li.Item is not null
                    && ctx.TypeMultipliers.TryGetValue(li.Item.ItemType, out var m))
                    mult = m;

                return (Entry: e, Weight: e.Weight * (1 + (mult / 100)));
            })
            .Where(w => w.Weight > 0)
            .ToList();

        if (weighted.Count == 0) return null;

        double effectiveTotal = weighted.Sum(w => w.Weight);
        double roll = RandomGenerator.NextDouble() * 100;

        if (roll > effectiveTotal)
            return null;

        double accum = 0;
        foreach (var (entry, weight) in weighted)
        {
            accum += weight;
            if (roll <= accum)
                return entry;
        }

        return null;
    }

    private InventoryItem ConvertItemIntoInventoryItem(ItemBase item)
    {
        return _inventoryItemFactory.Create(item, 1);
    }
}
