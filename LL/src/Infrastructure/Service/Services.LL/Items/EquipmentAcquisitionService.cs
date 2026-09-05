using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Items;
using Common.Randomness;
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
                await runs.AddPendingRewardAsync(run, new RunReward
                {
                    Id = StableRandom.Guid([.. identity, "blueprint"]),
                    ItemId = source.SelectionItemId,
                    Name = $"{source.Name} Blueprint Choice",
                    ItemType = ItemType.Resource,
                    Quantity = 1,
                    Source = "equipment-blueprint"
                }, ct);
            run.State ??= new DungeonRunState();
            run.State.EquipmentBlueprintProcessed = true;
        }
        if (run.PendingRewards.Any(reward => reward.Id == rewardId))
            return;

        var random = new Random(StableRandom.Seed(identity));
        if (random.NextDouble() >= rules.DungeonEquipment.DropChance)
            return;

        var rarity = rules.DungeonEquipment.Rarities.Roll(random.NextDouble());
        var definitions = blueprints is null ? catalog.DropDefinitions(rarity) : catalog.BaseDropDefinitions(rarity);
        var definition = definitions[random.Next(definitions.Count)];
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
                blueprints.DungeonVariantChance, new Random(StableRandom.Seed([.. identity, "variant"])));
        var equipment = EquipmentData.Create(state, catalog.Equipment.Evaluator);

        await runs.AddPendingRewardAsync(run, new RunReward
        {
            Id = equipment.State.Id,
            ItemId = equipment.ItemBaseId,
            Name = equipment.DisplayName,
            ItemType = ItemType.Equipment,
            Quantity = 1,
            Source = EquipmentKeys.DungeonCompletionSource,
            ProgressionData = equipment
        }, ct);
    }
}
