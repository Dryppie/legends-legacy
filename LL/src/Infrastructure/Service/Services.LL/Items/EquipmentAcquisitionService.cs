using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Items;
using Common.Randomness;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.Extensions.Options;

namespace Services.LL.Items;

public sealed class EquipmentAcquisitionService(
    CombatAcquisitionCatalog catalog,
    IDungeonDefinitions dungeons,
    IDungeonRunRepository runs,
    IOptions<EquipmentProgressionOptions> options,
    EquipmentBlueprintCatalog? blueprints = null,
    IEquipmentBlueprintRepository? blueprintRepository = null) : IEquipmentAcquisitionService
{
    public async Task CompleteAsync(DungeonRun run, bool firstCompletion, CancellationToken ct)
    {
        _ = firstCompletion;
        if (!options.Value.ProtectedAcquisitionEnabled || run.Status != DungeonRunStatus.Completed)
            return;

        var dungeon = dungeons.GetByKey(run.DungeonDefinitionId);
        var rules = catalog.FindRegion(dungeon.Region);
        if (rules is null || rules.EquipmentTier != dungeon.Region)
            return;

        var identity = new[]
        {
            EquipmentKeys.DungeonCompletionSource,
            run.CharacterId.ToString("N"),
            run.Id.ToString("N"),
            run.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        var rewardId = StableRandom.Guid(identity);
        var source = blueprints?.FindSource(dungeon.SigilItemId);
        if (source is not null && run.State?.EquipmentBlueprintProcessed != true)
        {
            var progress = await (blueprintRepository
                ?? throw new InvalidOperationException("Blueprint persistence is required.")).LoadForCompletionAsync(
                    run.CharacterId, source.FamilyId, ct);
            if (progress.Complete(run.Id, new Random(StableRandom.Seed([.. identity, "blueprint"])).NextDouble(), blueprints!))
            {
                var drops = blueprints!.DropsFor(source);
                var blueprint = drops[new Random(StableRandom.Seed([.. identity, "blueprint-type"])).Next(drops.Count)];
                await runs.AddPendingRewardAsync(run, new RunReward
                {
                    Id = StableRandom.Guid([.. identity, "blueprint"]),
                    ItemId = blueprint.ItemId,
                    Name = $"Blueprint: {blueprint.Name}",
                    ItemType = ItemType.Resource,
                    Quantity = 1,
                    Source = "equipment-blueprint"
                }, ct);
            }
            run.State ??= new DungeonRunState();
            run.State.EquipmentBlueprintProcessed = true;
        }
        if (run.PendingRewards.Any(reward => reward.Id == rewardId))
            return;

        var random = new Random(StableRandom.Seed(identity));
        if (random.NextDouble() >= rules.DungeonEquipment.DropChanceAtMastery(run.State?.MasteryLevelAtStart ?? 0))
            return;

        await runs.AddPendingRewardAsync(run,
            RollEquipmentReward(run, dungeon, rules, identity, random, EquipmentKeys.DungeonCompletionSource), ct);
    }

    public RunReward RollTreasuryReward(DungeonRun run, int roomIndex)
    {
        var dungeon = dungeons.GetByKey(run.DungeonDefinitionId);
        var rules = catalog.FindRegion(dungeon.Region)
            ?? throw new InvalidOperationException($"No equipment pool for dungeon '{dungeon.Id}'.");
        var source = blueprints?.FindSource(dungeon.SigilItemId)
            ?? throw new InvalidOperationException($"No blueprint pool for dungeon '{dungeon.Id}'.");
        var identity = new[]
        {
            "dungeon-treasury", run.CharacterId.ToString("N"), run.Id.ToString("N"),
            run.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture),
            roomIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        var random = new Random(StableRandom.Seed(identity));
        if (random.Next(2) == 0)
        {
            var drops = blueprints!.DropsFor(source);
            var blueprint = drops[random.Next(drops.Count)];
            return new RunReward
            {
                Id = StableRandom.Guid(identity), ItemId = blueprint.ItemId,
                Name = $"Blueprint: {blueprint.Name}", ItemType = ItemType.Resource,
                Quantity = 1, Source = "dungeon-treasury"
            };
        }

        return RollEquipmentReward(run, dungeon, rules, identity, random, "dungeon-treasury", requireDungeonStyle: true);
    }

    private RunReward RollEquipmentReward(
        DungeonRun run, DungeonDefinition dungeon, CombatAcquisitionRules rules,
        string[] identity, Random random, string rewardSource, bool requireDungeonStyle = false)
    {
        var rewardId = StableRandom.Guid(identity);
        var source = blueprints?.FindSource(dungeon.SigilItemId);
        var rarity = rules.DungeonEquipment.Rarities.ForGrade(dungeon.Grade).Roll(random.NextDouble());
        var definitions = blueprints is null ? catalog.DropDefinitions(rarity) : catalog.BaseDropDefinitions(rarity);
        if (requireDungeonStyle)
        {
            var compatibleArchetypes = catalog.Equipment.Styles.Where(style => source!.StyleIds.Contains(style.Id))
                .SelectMany(style => style.CompatibleArchetypeIds).ToHashSet();
            definitions = definitions.Where(definition => compatibleArchetypes.Contains(definition.ArchetypeId)).ToArray();
        }
        if (definitions.Count == 0)
            throw new InvalidOperationException($"No compatible {rarity} equipment for dungeon '{dungeon.Id}'.");
        var definition = rules.SelectionWeights.Roll(definitions, catalog.Equipment.Evaluator, random);
        var quality = rules.DungeonEquipment.Qualities.Roll(random.NextDouble());
        var attributeRollMultiplier = 0.95d + random.NextDouble() * 0.10d;
        var state = EquipmentState.Award(
            rewardId,
            catalog.Equipment.Evaluator,
            definition.Id,
            rules.EquipmentTier,
            rules.DungeonEquipment.Rank,
            new(EquipmentAwardKind.RandomDiscovery, dungeon.Id, run.Id.ToString("N")),
            new(EquipmentOwnershipKind.UnboundPersonal, run.CharacterId),
            quality,
            attributeRollMultiplier);
        if (source is not null)
            state = blueprints!.RollVariant(state, catalog.Equipment, source.StyleIds.ToArray(),
                requireDungeonStyle ? 1d : blueprints.DungeonVariantChance,
                new Random(StableRandom.Seed([.. identity, "variant"])));
        var equipment = EquipmentData.Create(state, catalog.Equipment.Evaluator);

        return new RunReward
        {
            Id = equipment.State.Id,
            ItemId = equipment.ItemBaseId,
            Name = equipment.DisplayName,
            ItemType = ItemType.Equipment,
            Quantity = 1,
            Source = rewardSource,
            ProgressionData = equipment
        };
    }
}
