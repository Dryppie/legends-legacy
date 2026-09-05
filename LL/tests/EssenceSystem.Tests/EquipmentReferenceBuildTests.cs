using System.Text.Json;
using Domain.Models.Combat;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Snapshots;
using LegendsLegacy.Balance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence.LL;
using Services.LL.Combat;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Essences;
using Services.LL.Items;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.PowerRatings;

namespace EssenceSystem.Tests;

public sealed class EquipmentReferenceBuildTests
{
    private static string ContentRoot => BalancePathLocator.FindApiContentRoot(null);
    private static IReadOnlyList<EquipmentReferenceBuildDefinition> Profiles =>
        JsonSerializer.Deserialize<EquipmentReferenceBuildDefinition[]>(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", EquipmentReferenceReportRunner.FixtureFileName)),
            EquipmentReferenceCommand.JsonOptions)!;

    private static (EquipmentReferenceBuildFactory Factory, StarterEquipmentCatalog Catalog, CombatSetupService Setup) Services()
    {
        var config = new ConfigurationBuilder().Build();
        var json = EquipmentReferenceCommand.JsonOptions;
        var catalog = JsonStarterEquipmentCatalog.Load(Path.Combine(ContentRoot, "Data", "equipment", "equipment-starters.v1.json"));
        var essences = new JsonEssenceDefinitionRepository(config, ContentRoot, json, new EssenceDefinitionValidator());
        var loadouts = new CatalogEssenceLoadoutResolver(essences);
        return (new(catalog, essences, loadouts), catalog,
            new CombatSetupService(null!, loadouts, essences, null!, equipmentCatalog: catalog));
    }

    [Fact]
    public void Complete_matrix_uses_live_canonical_stats_without_legacy_progression_or_paid_investments()
    {
        var (factory, catalog, _) = Services();
        Assert.Equal(12, Profiles.Count);
        foreach (var profile in Profiles)
        foreach (var rank in Enumerable.Range(0, 6))
        {
            var build = factory.Create(profile with { Rank = rank });
            Assert.Equal(8, build.Character.EquipmentSlots.Count);
            Assert.Equal(profile.EssenceIds.Count, build.EquippedEssences.Count);
            Assert.True(build.Rating.Overall > 0);
            Assert.All(build.Equipment, item =>
            {
                var data = Assert.IsType<EquipmentData>(item.ProgressionData);
                Assert.Equal(rank, data.State.Rank);
                Assert.Equal(1, data.State.Tier);
                Assert.Empty(item.BaseModifiers);
                Assert.Equal(build.Character.Id, data.State.Ownership.OwnerId);
                Assert.Equal(catalog.Evaluator.Evaluate(data.EquipmentState).Stats.OrderBy(x => x.Key), data.Stats.OrderBy(x => x.Key));
            });
        }
    }

    [Fact]
    public void Named_identity_survives_restyle_and_clear_without_changing_native_style_or_rarity()
    {
        var (factory, _, _) = Services();
        var weapons = new[] { "named-native", "named-restyled", "named-cleared" }.Select(id =>
            factory.Create(Profiles.Single(x => x.Id == id)).Character.EquipmentSlots
                .Single(x => x.EquipmentSlotType == EquipmentSlotType.MainHand).EquipmentInstance!.ProgressionData!).ToArray();
        Assert.All(weapons, x =>
        {
            Assert.Equal(weapons[0].State.DefinitionId, x.State.DefinitionId);
            Assert.Equal("blueprint_fury", x.State.NativeStyleId);
            Assert.Equal(weapons[0].Rarity, x.Rarity);
        });
        Assert.Equal("blueprint_fury", weapons[0].State.ActiveStyleId);
        Assert.Equal("blueprint_arcane", weapons[1].State.ActiveStyleId);
        Assert.Null(weapons[2].State.ActiveStyleId);
        Assert.NotEqual(weapons[0].Serialize(), weapons[1].Serialize());
    }

    [Theory]
    [InlineData("balanced-plain")]
    [InlineData("defensive-shield")]
    [InlineData("dual-wield")]
    [InlineData("area-styled")]
    public async Task Live_and_frozen_snapshot_combatants_have_identical_slots_stats_and_behavior(string profileId)
    {
        var (factory, _, setup) = Services();
        var build = factory.Create(Profiles.Single(x => x.Id == profileId) with { Rank = 3 });
        var snapshotId = Guid.NewGuid();
        var snapshot = new CharacterSnapshot
        {
            Id = snapshotId, CharacterId = build.Character.Id, Name = build.Character.Name, Level = build.Character.Level,
            BaseAttributes = build.Character.BaseAttributes.Select(x => new EntityAttributeSnapshot
            {
                CharacterSnapshotId = snapshotId, AttributeType = x.AttributeType, Value = x.Value
            }).ToArray(),
            Equipment = build.Character.EquipmentSlots.Select(x => EquipmentSnapshot.From(x.EquipmentSlotType, x.EquipmentInstance!)).ToArray(),
            EquippedEssences = build.EquippedEssences.Select((x, index) => EquippedEssenceSnapshot.From(snapshotId, index, x)).ToArray()
        };
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.ItemBases.AddRange(build.Equipment.Select(x => x.ItemBase).DistinctBy(x => x.Id));
        db.CharacterSnapshots.Add(snapshot);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var stored = await db.CharacterSnapshots.Include(x => x.BaseAttributes).Include(x => x.Equipment)
            .Include(x => x.EquippedEssences).SingleAsync();
        var frozen = Assert.Single(await new SnapshotCombatantBuilder(db, setup).BuildAsync(
            [new SnapshotCombatantRequest(stored, new CombatParticipantSlot("reference", build.Character.Id, CombatSide.Friendly))],
            CancellationToken.None)).Combatant;
        var direct = new CombatEntity(build.Character) { EquippedEssences = [.. build.EquippedEssences], HasEquippedEssenceSnapshot = true };
        await setup.PrepareEntitiesForCombat([direct, frozen], EssenceCombatActivity.Arena);
        Assert.Equal(build.Equipment.Count, direct.Equipment.Count);
        Assert.Equal(direct.Equipment.Count, frozen.Equipment.Count);
        Assert.Equal(direct.CombatAttributes.OrderBy(x => x.Key), frozen.CombatAttributes.OrderBy(x => x.Key));
        Assert.Equal(direct.MainHandEquipment!.ProgressionData!.Serialize(), frozen.MainHandEquipment!.ProgressionData!.Serialize());
        Assert.Equal(direct.OffHandEquipment!.ProgressionData!.Serialize(), frozen.OffHandEquipment!.ProgressionData!.Serialize());
        Assert.Equal(direct.Tags.Order(), frozen.Tags.Order());
    }

    [Theory]
    [InlineData(3, 0, 100)]
    [InlineData(2, 0, 10)]
    [InlineData(1, -1, 10)]
    [InlineData(1, 6, 10)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 0, 101)]
    [InlineData(1, 0, 1)]
    public void Unsupported_tiers_ranks_levels_and_locked_essence_slots_fail(int tier, int rank, int level)
    {
        var (factory, _, _) = Services();
        Assert.ThrowsAny<ArgumentException>(() => factory.Create(Profiles[0] with { Tier = tier, Rank = rank, CharacterLevel = level }));
    }

    [Fact]
    public void Illegal_slots_styles_and_essence_families_fail()
    {
        var (factory, _, _) = Services();
        var profile = Profiles[0];
        Assert.Throws<ArgumentException>(() => factory.Create(profile with { Equipment = profile.Equipment.Skip(1).ToArray() }));
        Assert.Throws<ArgumentException>(() => factory.Create(profile with { Equipment = [.. profile.Equipment, profile.Equipment[0]] }));
        Assert.Throws<ArgumentException>(() => factory.Create(profile with
        {
            Equipment = [.. profile.Equipment, new(EquipmentSlotType.OffHand, "plain.towershield")]
        }));
        Assert.Throws<ArgumentException>(() => factory.Create(profile with
        {
            Equipment = profile.Equipment.Select(x => x.Slot == EquipmentSlotType.Head ? x with { ActiveStyleId = "blueprint_fury" } : x).ToArray()
        }));
        Assert.Throws<ArgumentException>(() => factory.Create(profile with { EssenceIds = ["essence.goblin", "essence.goblin"] }));
        Assert.Throws<ArgumentException>(() => factory.Create(profile with { EssenceIds = ["essence.missing"] }));
    }

    [Fact]
    public async Task Reference_report_runs_production_combat_and_repeats_exactly_for_the_same_seed()
    {
        var runner = ProductionBalanceComposition.CreateEquipmentReferences(ContentRoot);
        var profiles = Profiles;
        var first = await runner.RunAsync(profiles, 1337, "fixture-test");
        var second = await runner.RunAsync(profiles, 1337, "fixture-test");
        Assert.Equal(72, first.Builds.Count);
        Assert.All(first.Builds, x =>
        {
            Assert.InRange(x.Combat.DurationTicks, 1, EquipmentReferenceReportRunner.MaximumCombatTicks);
            Assert.True(x.Combat.DamageDealt + x.Combat.DamageTaken > 0);
            Assert.NotEmpty(x.PreparedAttributes);
        });
        Assert.Equal(JsonSerializer.Serialize(first, EquipmentReferenceCommand.JsonOptions), JsonSerializer.Serialize(second, EquipmentReferenceCommand.JsonOptions));
        var changedSeed = await runner.RunAsync([profiles[0]], 77, "fixture-test");
        Assert.NotEqual(first.Builds[0].Combat.Seed, changedSeed.Builds[0].Combat.Seed);
    }

    [Fact]
    public async Task Tier_two_transition_evaluates_all_builds_against_invested_tier_one()
    {
        var profiles = Profiles.SelectMany(p => new[] {
            p with { CharacterLevel = 50 }, p with { Id = "tier2-" + p.Id, CharacterLevel = 50, Tier = 2 } }).ToArray();
        var report = await ProductionBalanceComposition.CreateEquipmentReferences(ContentRoot)
            .RunAsync(profiles, 1337, "transition-fixtures", regionTwoTransition: true);
        Assert.Equal(144, report.Builds.Count);
        Assert.All(report.Builds, b => {
            Assert.Equal("fixed-tier1-rank5-opponent", b.Combat.OpponentId);
            Assert.InRange(b.Combat.DurationTicks, 1, EquipmentReferenceReportRunner.MaximumCombatTicks);
            Assert.True(b.Combat.DamageDealt + b.Combat.DamageTaken > 0);
        });
        var (_, catalog, _) = Services();
        foreach (var plain in catalog.Options)
        {
            var invested = catalog.Evaluator.Evaluate(plain.DefinitionId, 1, 5, null);
            var replacement = catalog.Evaluator.Evaluate(plain.DefinitionId, 2, 0, null);
            Assert.InRange(replacement.TargetBudget / invested.TargetBudget, 1.12, 1.14);
        }
    }

    [Theory]
    [InlineData("--full")]
    [InlineData("--floor-progression-calibration")]
    [InlineData("--seed")]
    public void Reference_command_rejects_incompatible_or_incomplete_options(string option)
    {
        Assert.Equal(2, BalanceCli.Run([EquipmentReferenceCommand.Switch, option]));
    }
}
