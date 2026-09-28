using Application;
using Application.UseCases.Equipments.Queries.GetMigratedSpecializationChoice;
using AutoMapper;
using Domain.Models.Analytics;
using Domain.Models.Attributes;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.Extensions.DependencyInjection;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class EquipmentMigrationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Choice_query_maps_migration_options_and_preserves_no_choice_response(bool migrated)
    {
        var fixture = ServiceFixture();
        var itemId = fixture.Store.Item!.State.Id;
        var migrationId = Guid.NewGuid();
        if (migrated)
        {
            var preview = await fixture.Service.PreviewAsync(new(itemId), null, default);
            await fixture.Service.ApplyAsync(migrationId, preview.Target, preview.SourceHash,
                preview.After.State.DefinitionId, "test", default);
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        using var provider = services.BuildServiceProvider();
        var handler = new GetMigratedSpecializationChoiceQueryHandler(
            fixture.Service, provider.GetRequiredService<IMapper>());

        var response = await handler.Handle(new(fixture.Owner, itemId), default);

        Assert.True(response.IsSuccess);
        if (!migrated)
        {
            Assert.Null(response.Data);
            return;
        }

        Assert.NotNull(response.Data);
        Assert.Equal(migrationId, response.Data.MigrationId);
        var choice = await fixture.Service.GetChoiceAsync(fixture.Owner, itemId, default);
        Assert.NotNull(choice);
        Assert.NotEmpty(choice.Options);
        Assert.Equal(choice.Options.Select(option => option.State.DefinitionId),
            response.Data.Options.Select(option => option.DefinitionId));
        foreach (var (expected, actual) in choice.Options.Zip(response.Data.Options))
        {
            Assert.Equal(itemId, actual.Id);
            Assert.Equal(expected.DisplayName, actual.DisplayName);
            Assert.Equal(expected.State.Tier, actual.Tier);
            Assert.Equal(expected.State.Rank, actual.Rank);
            Assert.Equal(expected.State.BalanceVersion, actual.BalanceVersion);
            Assert.Equal(expected.State.Quality, actual.Quality);
            Assert.Equal(expected.State.Ownership.Kind, actual.Ownership);
            Assert.Equal(expected.Stats, actual.Stats);
        }
    }

    [Fact]
    public async Task Startup_conversion_keeps_scheduled_combat_and_retries_without_a_second_receipt()
    {
        var fixture = ServiceFixture();
        var releases = Releases();
        fixture.Store.ScheduledCombat = true;
        var before = fixture.Store.Item!;
        var service = new EquipmentMigrationService(fixture.Store, new(releases.Get(4), releases),
            new RecordingItemizationOutbox(), TimeProvider.System, fixture.Store, startup: fixture.Store);
        var preview = await service.PreviewAsync(new(before.State.Id), null, default, 4);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyAsync(Guid.NewGuid(), preview.Target,
            preview.SourceHash, preview.After.State.DefinitionId, "operator", default, 4, preview.ResultHash));

        Assert.True(await service.ConvertOnStartupAsync(Guid.NewGuid(), preview.Target, 4, default));
        Assert.False(await service.ConvertOnStartupAsync(Guid.NewGuid(), preview.Target, 4, default));
        Assert.True(fixture.Store.ScheduledCombat);
        Assert.Equal(2, fixture.Store.StartupLocks);
        Assert.Equal(1, fixture.Store.Saves);
        Assert.Equal(4, fixture.Store.Item!.State.BalanceVersion);
        Assert.Equal(before.State.Ownership, fixture.Store.Item.State.Ownership);
        Assert.Equal(before.State.Id, fixture.Store.Item.State.Id);
    }

    [Fact]
    public async Task Startup_conversion_skips_an_item_that_lost_its_live_reference()
    {
        var fixture = ServiceFixture();
        var releases = Releases();
        fixture.Store.Referenced = false;
        var original = fixture.Store.Item!;
        var service = new EquipmentMigrationService(fixture.Store, new(releases.Get(4), releases),
            new RecordingItemizationOutbox(), TimeProvider.System, fixture.Store, startup: fixture.Store);
        Assert.False(await service.ConvertOnStartupAsync(Guid.NewGuid(), new(original.State.Id), 4, default));
        Assert.Equal(0, fixture.Store.Saves);
        Assert.Same(original, fixture.Store.Item);
    }

    [Fact]
    public void Referenced_readiness_keeps_unreferenced_records_visible_and_pending_rewards_blocking()
    {
        var audit = new EquipmentMigrationAudit(0, 100, 558, 0, 408, 0, 1082, [])
            { UnreferencedUnversionedInstances = 408 };
        Assert.True(audit.ReferencedItemConversionComplete);
        Assert.False(audit.ItemConversionComplete);
        Assert.Equal(408, audit.UnversionedInstances);
        Assert.False((audit with { UnversionedInstances = 409 }).ReferencedItemConversionComplete);
        Assert.False((audit with { UnversionedPendingRewards = 1 }).ReferencedItemConversionComplete);
    }
    [Theory]
    [InlineData("cloth_cowl", "plain.light_hood", Domain.Models.Items.Equipments.EquipmentType.Head, false)]
    [InlineData("cloth_pants", "plain.light_leggings", Domain.Models.Items.Equipments.EquipmentType.Legs, false)]
    [InlineData("mace", "plain.mace", Domain.Models.Items.Equipments.EquipmentType.OneHanded, true)]
    public async Task Retired_identity_survives_import_rebalance_choice_and_rollback(string itemBaseId, string archetype,
        Domain.Models.Items.Equipments.EquipmentType slot, bool empty)
    {
        var fixture = ServiceFixture();
        var versions = Releases();
        var id = fixture.Store.Item!.State.Id;
        var legacy = new LegacyEquipmentSnapshot(id, itemBaseId, "Original cloth name", slot,
            Domain.Models.Items.Rarity.Uncommon, Domain.Models.Items.ItemQuality.Fine, 2, false,
            new(EquipmentOwnershipKind.UnboundPersonal, fixture.Owner), DateTimeOffset.UnixEpoch, "crafting", true, ["old-affinity"],
            empty ? [] : [new(Guid.NewGuid(), AttributeType.Power, 30, Domain.Models.Attributes.Modifiers.ModifierType.Flat)],
            empty ? [] : [new(Guid.NewGuid(), AttributeType.HealingPowerPercent, 12.5f, Domain.Models.Attributes.Modifiers.ModifierType.Flat, 1.5f)]);
        fixture.Store.Item = null;
        fixture.Store.Legacy = legacy;
        var service = new EquipmentMigrationService(fixture.Store, new(versions.Get(4), versions),
            new RecordingItemizationOutbox(), TimeProvider.System, fixture.Store);
        var target = new EquipmentMigrationTarget(id);
        var preview = await service.PreviewAsync(target, null, default, 4);
        Assert.Equal(0, preview.SourceBalanceVersion);
        Assert.Equal(legacy.Hash(), preview.SourceHash);
        Assert.Equal(archetype, preview.After.State.ArchetypeId);
        Assert.Equal(legacy.Ownership, preview.After.State.Ownership);
        Assert.Equal(legacy.Quality, preview.After.Quality);
        Assert.Equal(2, preview.After.State.Tier);
        Assert.Equal(0, preview.After.State.Rank);
        if (empty)
        {
            Assert.Null(preview.Before);
            Assert.Equal(0, preview.OldBudget);
            Assert.Equal("plain.mace.rarity.uncommon", preview.After.State.DefinitionId);
            Assert.Contains("No positive attributes", preview.MappingReason);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyAsync(Guid.NewGuid(), target, preview.SourceHash,
            preview.After.State.DefinitionId, "test", default, 4));
        fixture.Store.Legacy = legacy with { IsFavorite = false };
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyAsync(Guid.NewGuid(), target, preview.SourceHash,
            preview.After.State.DefinitionId, "test", default, 4, preview.ResultHash));
        fixture.Store.Legacy = legacy;
        var receipt = await service.ApplyAsync(Guid.NewGuid(), target, preview.SourceHash, preview.After.State.DefinitionId, "test", default, 4, preview.ResultHash);
        Assert.Equal(legacy.Hash(), LegacyEquipmentMigration.ReadBefore(receipt.BeforeJson)!.Hash());
        await service.RollbackAsync(receipt.OperationId, "test", default);
        Assert.Null(fixture.Store.Item);
        Assert.Equal(legacy.Hash(), fixture.Store.Legacy!.Hash());
        Assert.Equal(preview.SourceHash, (await service.PreviewAsync(target, null, default, 4)).SourceHash);
        var again = await service.ApplyAsync(Guid.NewGuid(), target, preview.SourceHash, preview.After.State.DefinitionId, "test", default, 4, preview.ResultHash);
        var choices = (await service.GetChoiceAsync(fixture.Owner, id, default))!;
        Assert.All(choices.Options, x => Assert.Equal(itemBaseId, x.ItemBaseId));
        var chosen = await service.ChooseAsync(fixture.Owner, again.OperationId, Guid.NewGuid(), choices.Options.Last().State.DefinitionId, default);
        Assert.Equal(itemBaseId, chosen.ItemBaseId);
        Assert.Equal(legacy.DisplayName, chosen.DisplayName);
        Assert.Equal(legacy.Ownership, chosen.State.Ownership);
        Assert.Equal(itemBaseId, chosen.ReinforceFrozen(versions.Get(4).Evaluator.Balance).ItemBaseId);
    }

    [Fact]
    public void Retired_versioned_descriptor_preserves_frozen_state_and_behavior()
    {
        var fixture = ServiceFixture();
        var old = fixture.Store.Item!;
        var before = new EquipmentData(old.State with { ArchetypeId = "plain.cloth_cowl", DefinitionId = "plain.cloth_cowl", Rank = 2 },
            "cloth_cowl", "Cloth Cowl", old.Rarity, Domain.Models.Items.Equipments.EquipmentType.Head,
            new() { Role = "Cloth" }, old.Stats, null);
        var result = EquipmentMigrationPolicy.Preview(new(before.State.Id), before, Releases().Get(4).Evaluator);
        Assert.Equal(2, result.After.State.Rank);
        Assert.Equal(before.ItemBaseId, result.After.ItemBaseId);
        Assert.Equal(before.Behavior, result.After.Behavior);
        Assert.Equal("plain.light_hood", result.After.State.ArchetypeId);
        Assert.Null(LegacyEquipmentMigration.ReadBefore(before.Serialize()));
    }

    private static JsonEquipmentCatalogProvider Releases() => new(Path.Combine(TestContentPaths.FindApiRoot(), "Data", "equipment", "equipment-starters.v1.json"));

    [Theory]
    [InlineData(AttributeType.Threat, 0, Domain.Models.Attributes.Modifiers.ModifierType.Flat)]
    [InlineData(AttributeType.Power, 1, Domain.Models.Attributes.Modifiers.ModifierType.Multiplicative)]
    [InlineData(AttributeType.Power, -1, Domain.Models.Attributes.Modifiers.ModifierType.Flat)]
    public void Unsupported_legacy_modifiers_are_not_discarded_or_treated_as_an_empty_item(AttributeType attribute,
        float amount, Domain.Models.Attributes.Modifiers.ModifierType modifierType)
    {
        var legacy = new LegacyEquipmentSnapshot(Guid.NewGuid(), "mace", "Mace", Domain.Models.Items.Equipments.EquipmentType.OneHanded,
            Domain.Models.Items.Rarity.Common, Domain.Models.Items.ItemQuality.Standard, 1, true,
            new(EquipmentOwnershipKind.BoundPersonal, Guid.NewGuid()), DateTimeOffset.UnixEpoch, "legacy-backfill", false, [], [],
            [new(Guid.NewGuid(), attribute, amount, modifierType)]);
        var exception = Assert.Throws<InvalidOperationException>(() => legacy.Preview(new(legacy.Id), Releases().Get(4).Evaluator,
            new EquipmentBalance(1), null));
        Assert.Contains("unsupported modifiers", exception.Message);
    }

    [Fact]
    public void Release_prices_and_allocation_shares_are_validated_and_materialized()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EquipmentBalance(3, settings: new(18, AttributeCosts: new Dictionary<AttributeType, double> { [AttributeType.Power] = 0 })));
        Assert.Throws<ArgumentException>(() => new EquipmentBalance(3, settings: new(18, CoreShare: 1)));
        Assert.Throws<ArgumentException>(() => new EquipmentBalance(3, settings: new(18, IdentityShare: .2)));
        var catalog = Releases().Get(3);
        var original = catalog.Evaluator;
        var balance = new EquipmentBalance(4, settings: new(18, CoreShare: .6, AttributeCosts: original.Balance.AttributeCosts));
        var evaluator = new EquipmentEvaluator(balance, original.Definitions.Select(x => x.ArchetypeId).Distinct().Select(original.GetArchetype), catalog.Styles, original.Definitions);
        var evaluation = evaluator.Evaluate("plain.shortsword", 1, 0, null);
        Assert.Equal(60, evaluation.Allocation!.Core);
        Assert.Equal(40, evaluation.Allocation.Specialization);
        Assert.Equal(100, evaluation.TargetBudget);
        Assert.Equal(4 * EquipmentTierBudgetCurve.GetScale(2), balance.GetMaterializedCostPerPoint(AttributeType.MagicPenetration, 2), 8);
        Assert.Equal(22.5, balance.GetMaterializedCostPerPoint(AttributeType.Power, 2));
    }

    [Fact]
    public async Task Successive_rebalances_preserve_specialization_credit_and_require_reverse_rollback()
    {
        var versions = Releases();
        var fixture = ServiceFixture();
        var service = new EquipmentMigrationService(fixture.Store, new(versions.Get(2), versions),
            new RecordingItemizationOutbox(), TimeProvider.System, fixture.Store);
        var original = EquipmentMigrationPolicy.Hash(fixture.Store.Item!);
        var target = new EquipmentMigrationTarget(fixture.Store.Item!.State.Id);
        var first = await service.PreviewAsync(target, null, default, 2);
        var receipt1 = await service.ApplyAsync(Guid.NewGuid(), target, first.SourceHash, first.After.State.DefinitionId, "test", default, 2, first.ResultHash);
        var second = await service.PreviewAsync(target, null, default, 3);
        Assert.Equal(first.After.State.DefinitionId, second.After.State.DefinitionId);
        Assert.Equal(1, second.RespecializationAllowance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyAsync(Guid.NewGuid(), target, second.SourceHash, second.After.State.DefinitionId, "test", default, 3));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyAsync(Guid.NewGuid(), target, second.SourceHash, second.After.State.DefinitionId, "test", default, 3, "stale-result"));
        var receipt2 = await service.ApplyAsync(Guid.NewGuid(), target, second.SourceHash, second.After.State.DefinitionId, "test", default, 3, second.ResultHash);
        Assert.Equal(receipt1.Revision + 1, receipt2.Revision);
        Assert.Same(receipt2, await service.ApplyAsync(receipt2.OperationId, target, second.SourceHash, second.After.State.DefinitionId, "test", default, 3, second.ResultHash));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RollbackAsync(receipt1.OperationId, "test", default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChooseAsync(fixture.Owner, receipt1.OperationId, Guid.NewGuid(), first.After.State.DefinitionId, default));
        Assert.Equal(receipt2.OperationId, (await service.GetChoiceAsync(fixture.Owner, target.ItemId, default))!.MigrationId);
        await service.RollbackAsync(receipt2.OperationId, "test", default);
        Assert.Equal(first.ResultHash, EquipmentMigrationPolicy.Hash(fixture.Store.Item!));
        await service.RollbackAsync(receipt1.OperationId, "test", default);
        Assert.Equal(original, EquipmentMigrationPolicy.Hash(fixture.Store.Item!));
        // Reapplying after rollback reuses neither an operation nor a consumed credit.
        await service.ApplyAsync(Guid.NewGuid(), target, first.SourceHash, first.After.State.DefinitionId, "test", default, 2, first.ResultHash);
        var again = await service.PreviewAsync(target, null, default, 3);
        var receipt3 = await service.ApplyAsync(Guid.NewGuid(), target, again.SourceHash, again.After.State.DefinitionId, "test", default, 3, again.ResultHash);
        var choice = (await service.GetChoiceAsync(fixture.Owner, target.ItemId, default))!;
        var chosen = await service.ChooseAsync(fixture.Owner, receipt3.OperationId, Guid.NewGuid(), choice.Options.Last().State.DefinitionId, default);
        Assert.Equal(3, chosen.State.BalanceVersion);
        Assert.Null(await service.GetChoiceAsync(fixture.Owner, target.ItemId, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RollbackAsync(receipt3.OperationId, "test", default));
    }

    [Theory]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    public async Task Ordinary_rebalance_grants_no_new_specialization_choice(int sourceVersion, int targetVersion)
    {
        var versions = Releases();
        var fixture = ServiceFixture();
        fixture.Store.Item = EquipmentMigrationPolicy.Preview(new(fixture.Store.Item!.State.Id), fixture.Store.Item, versions.Get(sourceVersion).Evaluator).After;
        var service = new EquipmentMigrationService(fixture.Store, new(versions.Get(targetVersion), versions),
            new RecordingItemizationOutbox(), TimeProvider.System, fixture.Store);
        var preview = await service.PreviewAsync(new(fixture.Store.Item.State.Id), null, default, targetVersion);
        Assert.Equal(0, preview.RespecializationAllowance);
        var receipt = await service.ApplyAsync(Guid.NewGuid(), preview.Target, preview.SourceHash, preview.After.State.DefinitionId, "test", default, targetVersion, preview.ResultHash);
        Assert.Equal(0, receipt.RespecializationAllowance);
        Assert.Null(await service.GetChoiceAsync(fixture.Owner, preview.Target.ItemId, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChooseAsync(fixture.Owner, receipt.OperationId, Guid.NewGuid(), preview.After.State.DefinitionId, default));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rebalance_does_not_restore_a_spent_or_style_invalidated_choice(bool changeStyle)
    {
        var versions = Releases();
        var fixture = ServiceFixture();
        var service = new EquipmentMigrationService(fixture.Store, new(versions.Get(2), versions),
            new RecordingItemizationOutbox(), TimeProvider.System, fixture.Store);
        var target = new EquipmentMigrationTarget(fixture.Store.Item!.State.Id);
        var first = await service.PreviewAsync(target, null, default);
        var receipt = await service.ApplyAsync(Guid.NewGuid(), target, first.SourceHash, first.After.State.DefinitionId, "test", default);
        if (changeStyle) fixture.Store.Item = fixture.Store.Item.ApplyVariant(versions.Get(2).Evaluator, "blueprint_fury");
        else await service.ChooseAsync(fixture.Owner, receipt.OperationId, Guid.NewGuid(), first.After.State.DefinitionId, default);
        var next = await service.PreviewAsync(target, null, default, 3);
        Assert.Equal(0, next.RespecializationAllowance);
        await service.ApplyAsync(Guid.NewGuid(), target, next.SourceHash, next.After.State.DefinitionId, "test", default, 3, next.ResultHash);
        Assert.Null(await service.GetChoiceAsync(fixture.Owner, target.ItemId, default));
    }

    [Fact]
    public void Releases_keep_historical_prices_and_combat_versions_independent()
    {
        var versions = Releases();
        Assert.Equal(new[] { 1, 2, 3, 4 }, versions.Versions);
        Assert.Equal(1.5, versions.Get(2).Evaluator.Balance.GetMaterializedCostPerPoint(AttributeType.ArmorPenetration, 1));
        Assert.Equal(4, versions.Get(3).Evaluator.Balance.GetMaterializedCostPerPoint(AttributeType.ArmorPenetration, 1));
        Assert.Equal(18, versions.Get(3).Evaluator.Balance.AttributeVersion);
        Assert.Equal(3, new AttributeRulesSelection(18, 3).EquipmentBalanceVersion);
        Assert.Throws<ArgumentException>(() => new AttributeRulesSelection(17, 3));
        Assert.Throws<InvalidOperationException>(() => versions.Get(999));
        foreach (var definition in versions.Get(2).Evaluator.Definitions)
        {
            var before = EquipmentData.Create(EquipmentState.Award(Guid.NewGuid(), versions.Get(2).Evaluator, definition.Id, 1, 2,
                new(EquipmentAwardKind.RandomDiscovery, "test", "release"), new(EquipmentOwnershipKind.UnboundPersonal, Guid.NewGuid())), versions.Get(2).Evaluator);
            var proposal = EquipmentMigrationPolicy.Preview(new(before.State.Id), before, versions.Get(3).Evaluator, sourceBalance: versions.Get(2).Evaluator.Balance);
            Assert.Equal(before.State with { BalanceVersion = 3 }, proposal.After.State);
            Assert.Equal(before.Allocation, proposal.After.Allocation);
            Assert.Equal(proposal.OldBudget, proposal.NewBudget);
            Assert.Equal(0, proposal.RespecializationAllowance);
            Assert.Equal(before.Stats.OrderBy(x => x.Key), EquipmentData.Deserialize(before.Serialize()).Stats.OrderBy(x => x.Key));
        }
    }

    [Fact]
    public void Daily_report_retains_ordered_essence_and_attribute_outcomes_without_treating_repeated_fights_as_players()
    {
        var time = DateTimeOffset.UtcNow;
        var build = new ItemizationBuildSnapshot("build", 90, [], [new("essence.pixie", 1, 0)], null,
            [new(AttributeType.CritChance, 45, 45, 0)]);
        var observations = Enumerable.Range(0, 30).Select(i => new ItemizationObservation(i.ToString(), "battle",
            Guid.NewGuid(), time, "Arena", 18, Build: build,
            Battle: new(Guid.NewGuid(), "Pvp", "fixture", "Win", 100,
                new Domain.Models.Combat.EntityStats("actor", "", [])))).ToArray();
        var cohort = Assert.Single(ItemizationCohortReport.Create(DateOnly.FromDateTime(time.UtcDateTime), observations).Cohorts);
        var band = Assert.Single(cohort.AttributeOutcomes);
        Assert.Equal(40, band.MinimumInclusive);
        Assert.Equal(60, band.MaximumExclusive);
        Assert.Equal(30, band.DistinctCharacters);
        Assert.True(band.Lower95 < 1); // Even 30/30 players winning cannot establish certainty.
        Assert.Equal(0, Assert.Single(cohort.EssenceUsage).SlotOrder);
        var repeats = Enumerable.Range(0, 100).Select(i => observations[0] with { Id = i.ToString() });
        var repeated = Assert.Single(ItemizationCohortReport.Create(DateOnly.FromDateTime(time.UtcDateTime), repeats).Cohorts);
        Assert.Null(Assert.Single(repeated.AttributeOutcomes).Lower95);
        Assert.Equal(1, Assert.Single(repeated.EffectiveAttributes).Count);
    }

    private static (StarterEquipmentCatalog Legacy, StarterEquipmentCatalog Current) Catalogs()
    {
        var path = Path.Combine(TestContentPaths.FindApiRoot(), "Data", "equipment", "equipment-starters.v1.json");
        return (JsonStarterEquipmentCatalog.Load(path, balanceVersion: 1), JsonStarterEquipmentCatalog.Load(path));
    }

    [Fact]
    public void Every_legacy_definition_has_a_repeatable_conversion_preserving_identity_and_progression()
    {
        var (legacy, current) = Catalogs();
        foreach (var definition in legacy.Evaluator.Definitions)
        {
            var state = EquipmentState.Award(Guid.NewGuid(), legacy.Evaluator, definition.Id, 2, 2,
                new(EquipmentAwardKind.RandomDiscovery, "combat", "award"), new(EquipmentOwnershipKind.UnboundPersonal, Guid.NewGuid()),
                Domain.Models.Items.ItemQuality.Standard, 1.03);
            var before = EquipmentData.Create(state, legacy.Evaluator);
            var target = new EquipmentMigrationTarget(state.Id);
            var proposal = EquipmentMigrationPolicy.Preview(target, before, current.Evaluator);
            var repeated = EquipmentMigrationPolicy.Preview(target, EquipmentData.Deserialize(before.Serialize()), current.Evaluator);
            Assert.Equal(EquipmentMigrationPolicy.Hash(proposal.After), EquipmentMigrationPolicy.Hash(repeated.After));
            Assert.Equal(before.State.Id, proposal.After.State.Id);
            Assert.Equal(before.State.Ownership, proposal.After.State.Ownership);
            Assert.Equal(before.State.Provenance, proposal.After.State.Provenance);
            Assert.Equal(before.State.Rank, proposal.After.State.Rank);
            Assert.Equal(before.State.Tier, proposal.After.State.Tier);
            Assert.Equal(before.State.AttributeRollMultiplier, proposal.After.State.AttributeRollMultiplier);
            Assert.Equal(before.State.ActiveStyleId, proposal.After.State.ActiveStyleId);
            Assert.Equal(before.Rarity, proposal.After.Rarity);
            Assert.Equal(AttributeRules.CurrentVersion, proposal.After.StatVersion);
            Assert.All(proposal.After.Stats.Keys, x => Assert.True(AttributeRules.IsOrdinaryEquipmentAttribute(x)));
            Assert.Equal(1, proposal.RespecializationAllowance);
            // Integer Power is the largest quantization unit. No unexplained nominal budget reroll.
            Assert.InRange(Math.Abs(proposal.OldBudget - proposal.NewBudget), 0, 30);
        }
    }

    [Fact]
    public void Source_hash_is_independent_of_json_dictionary_key_order()
    {
        var (legacy, current) = Catalogs();
        var state = EquipmentState.Award(Guid.NewGuid(), legacy.Evaluator, "plain.heavy_helm", 1, 0,
            new(EquipmentAwardKind.RandomDiscovery, "combat", "award"), new(EquipmentOwnershipKind.UnboundPersonal, Guid.NewGuid()));
        var data = EquipmentData.Create(state, legacy.Evaluator);
        var reordered = new EquipmentData(data.State, data.ItemBaseId, data.DisplayName, data.Rarity,
            data.EquipmentType, data.Behavior, data.Stats.Reverse().ToDictionary(x => x.Key, x => x.Value), data.EquipmentSetId, data.BaseStats, data.Allocation);
        Assert.Equal(EquipmentMigrationPolicy.Hash(data), EquipmentMigrationPolicy.Hash(reordered));
        Assert.Throws<InvalidOperationException>(() => EquipmentMigrationPolicy.Preview(new(state.Id), data, current.Evaluator, "plain.dagger"));
    }

    [Theory]
    [InlineData("plain.grimoire.rarity.uncommon", "blueprint_arcane", true)]
    [InlineData("plain.heavy_helm.rarity.epic", "blueprint_endurance", true)]
    [InlineData("plain.band.rarity.unique", "blueprint_phoenix", true)]
    [InlineData("plain.amulet.rarity.epic", "blueprint_execution", true)]
    [InlineData("plain.grimoire.rarity.uncommon", "blueprint_arcane", false)]
    public async Task Rolled_blueprint_styles_survive_conversion_choice_and_rollback(
        string definition, string nativeStyle, bool additiveVariantBonus)
    {
        var (legacy, current) = Catalogs();
        var owner = Guid.NewGuid();
        var awarded = EquipmentState.Award(Guid.NewGuid(), legacy.Evaluator, definition, 2, 2,
            new(EquipmentAwardKind.RandomDiscovery, "combat", "styled-award"),
            new(EquipmentOwnershipKind.UnboundPersonal, owner), Domain.Models.Items.ItemQuality.Standard, 1.03);
        Assert.Null(legacy.Evaluator.GetDefinition(definition).NativeStyleId);
        var rolled = new EquipmentBlueprintCatalog().RollVariant(awarded, legacy, [nativeStyle], 1, new Random(1));
        // A later blueprint change must not replace the original native style during migration.
        var replacement = legacy.Styles.First(x => x.Id != nativeStyle && x.CompatibleArchetypeIds.Contains(rolled.ArchetypeId));
        var styled = EquipmentState.Restore(rolled.ApplyVariant(legacy.Evaluator, replacement.Id).ToSnapshot()
            with { AdditiveVariantBonus = additiveVariantBonus });
        var before = EquipmentData.Create(styled, legacy.Evaluator);
        var store = new MigrationStore(before, current);
        var service = new EquipmentMigrationService(store, new(current), new RecordingItemizationOutbox(), TimeProvider.System, store);
        var preview = await service.PreviewAsync(new(styled.Id), null, default);
        Assert.NotEmpty(preview.Choices);
        Assert.Equal(before.State with { ModelVersion = EquipmentBalance.ModelVersion,
            BalanceVersion = current.Evaluator.Balance.Version, DefinitionId = preview.After.State.DefinitionId }, preview.After.State);
        Assert.Equal(current.Evaluator.Evaluate(EquipmentState.Restore(preview.After.State)).EquipmentSetId, preview.After.EquipmentSetId);
        var operation = Guid.NewGuid();
        await service.ApplyAsync(operation, preview.Target, preview.SourceHash, preview.After.State.DefinitionId, "test", default);
        await service.RollbackAsync(operation, "test", default);
        Assert.Equal(EquipmentMigrationPolicy.Hash(before), EquipmentMigrationPolicy.Hash(store.Item!));
        var choiceOperation = Guid.NewGuid();
        await service.ApplyAsync(choiceOperation, preview.Target, preview.SourceHash, preview.After.State.DefinitionId, "test", default);
        var choices = (await service.GetChoiceAsync(owner, styled.Id, default))!;
        var choice = await service.ChooseAsync(owner, choiceOperation, Guid.NewGuid(), choices.Options.Last().State.DefinitionId, default);
        Assert.Equal(nativeStyle, choice.State.NativeStyleId);
        Assert.Equal(replacement.Id, choice.State.ActiveStyleId);
        Assert.Equal(additiveVariantBonus, choice.State.AdditiveVariantBonus);
        Assert.Equal(before.State.Ownership, choice.State.Ownership);
    }

    [Fact]
    public async Task Retired_archetype_preview_returns_an_explicit_blocker_without_mutation()
    {
        var fixture = ServiceFixture();
        var original = fixture.Store.Item!;
        fixture.Store.Item = new EquipmentData(original.State with { ArchetypeId = "retired.fixture", DefinitionId = "retired.fixture" },
            original.ItemBaseId, original.DisplayName, original.Rarity, original.EquipmentType,
            original.Behavior, original.Stats, original.EquipmentSetId, original.BaseStats);
        var beforeHash = EquipmentMigrationPolicy.Hash(fixture.Store.Item);
        var handler = new Application.UseCases.Equipments.Queries.PreviewEquipmentMigration.PreviewEquipmentMigrationQueryHandler(fixture.Service);
        var result = await handler.Handle(new(new(original.State.Id)), default);
        Assert.False(result.IsSuccess);
        Assert.Equal("equipment_migration_preview_blocked", result.ErrorCode);
        Assert.Contains("No compatible authored conversion", result.ErrorMessage);
        Assert.Equal(0, fixture.Store.Saves);
        Assert.Equal(beforeHash, EquipmentMigrationPolicy.Hash(fixture.Store.Item));
    }

    [Fact]
    public void Legacy_reinforcement_keeps_its_model_version_and_wrong_evaluator_is_rejected()
    {
        var (legacy, current) = Catalogs();
        var state = EquipmentState.Award(Guid.NewGuid(), legacy.Evaluator, "plain.dagger", 1, 0,
            new(EquipmentAwardKind.RandomDiscovery, "combat", "award"), new(EquipmentOwnershipKind.UnboundPersonal, Guid.NewGuid()));
        state = EquipmentState.Restore(state.ToSnapshot() with { ModelVersion = 2 });
        Assert.Equal(2, state.Reinforce(legacy.Evaluator).ModelVersion);
        Assert.Throws<InvalidOperationException>(() => state.Reinforce(current.Evaluator));
    }

    [Fact]
    public void Observation_retries_do_not_inflate_cohorts_and_small_cohorts_do_not_claim_precision()
    {
        var character = Guid.NewGuid();
        var observation = new ItemizationObservation(ItemizationObservation.StableId("equipped", "operation", character),
            "equipped", character, DateTimeOffset.UnixEpoch, "test", AttributeRules.CurrentVersion);
        var report = ItemizationCohortReport.Create(new(2026, 9, 25), [observation, observation]);
        var cohort = Assert.Single(report.Cohorts);
        Assert.Equal(1, cohort.Equipped);
        Assert.Equal(1, cohort.DistinctCharacters);
        Assert.Null(cohort.WinRateLower95);
        Assert.Null(cohort.WinRateUpper95);
    }
    [Fact]
    public async Task Conversion_and_rollback_retries_do_not_depend_on_the_item_still_existing()
    {
        var fixture = ServiceFixture();
        var preview = await fixture.Service.PreviewAsync(new(fixture.Store.Item!.State.Id), null, default);
        var operation = Guid.NewGuid();
        var receipt = await fixture.Service.ApplyAsync(operation, preview.Target, preview.SourceHash, preview.After.State.DefinitionId, "operator", default);
        Assert.Equal(1, fixture.Store.Saves);
        var migrated = fixture.Store.Item;
        fixture.Store.Item = null;
        Assert.Same(receipt, await fixture.Service.ApplyAsync(operation, preview.Target, preview.SourceHash, preview.After.State.DefinitionId, "operator", default));
        Assert.Equal(1, fixture.Store.Saves);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ApplyAsync(operation, preview.Target, "changed", preview.After.State.DefinitionId, "operator", default));
        fixture.Store.Item = migrated;
        await fixture.Service.RollbackAsync(operation, "operator", default);
        Assert.Equal(preview.SourceHash, EquipmentMigrationPolicy.Hash(fixture.Store.Item!));
        Assert.Equal(2, fixture.Store.Saves);
        fixture.Store.Item = null;
        await fixture.Service.RollbackAsync(operation, "operator", default);
        Assert.Equal(2, fixture.Store.Saves);
    }

    [Fact]
    public async Task Stale_previews_changed_items_and_reused_choices_cannot_overwrite_progression()
    {
        var fixture = ServiceFixture();
        var preview = await fixture.Service.PreviewAsync(new(fixture.Store.Item!.State.Id), null, default);
        var operation = Guid.NewGuid();
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ApplyAsync(operation, preview.Target, "stale", preview.After.State.DefinitionId, "operator", default));
        Assert.Equal(0, fixture.Store.Saves);
        await fixture.Service.ApplyAsync(operation, preview.Target, preview.SourceHash, preview.After.State.DefinitionId, "operator", default);
        var choice = (await fixture.Service.GetChoiceAsync(fixture.Owner, preview.Target.ItemId, default))!;
        var selected = choice.Options.Last().State.DefinitionId;
        var choiceId = Guid.NewGuid();
        var after = await fixture.Service.ChooseAsync(fixture.Owner, operation, choiceId, selected, default);
        Assert.Equal(selected, after.State.DefinitionId);
        Assert.Equal(EquipmentMigrationPolicy.Hash(after), EquipmentMigrationPolicy.Hash(await fixture.Service.ChooseAsync(fixture.Owner, operation, choiceId, selected, default)));
        Assert.Equal(2, fixture.Store.Saves);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ChooseAsync(fixture.Owner, operation, Guid.NewGuid(), selected, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.RollbackAsync(operation, "operator", default));
        Assert.Null(await fixture.Service.GetChoiceAsync(fixture.Owner, preview.Target.ItemId, default));
    }

    [Fact]
    public void Seven_day_decision_cohort_deduplicates_and_closes_replacement_intervals()
    {
        var fixture = ServiceFixture();
        var start = new DateTimeOffset(2026, 9, 18, 0, 0, 0, TimeSpan.Zero);
        ItemizationObservation Event(string kind, int minutes) => new(kind, kind, fixture.Owner, start.AddMinutes(minutes), "idle", 17,
            fixture.Store.Item!.State.Id, fixture.Store.Item);
        var award = Event("awarded", 0);
        var reports = ItemizationOpportunity.Create(new(2026, 9, 25), [award, award, Event("equipped", 30), Event("unequipped", 150), Event("Dismantle", 200)]);
        var report = Assert.Single(reports);
        Assert.Equal(1, report.Awarded);
        Assert.Equal(1, report.SustainedUse);
        Assert.Equal(1, report.DismantledWithinSevenDays);
        Assert.Equal(.5, report.MedianHoursToFirstEquip);
        Assert.Equal(2, report.EquippedHours);
    }

    private static (EquipmentMigrationService Service, MigrationStore Store, Guid Owner) ServiceFixture()
    {
        var (legacy, current) = Catalogs();
        var owner = Guid.NewGuid();
        var state = EquipmentState.Award(Guid.NewGuid(), legacy.Evaluator, "plain.shortsword", 1, 0,
            new(EquipmentAwardKind.RandomDiscovery, "test", "award"), new(EquipmentOwnershipKind.UnboundPersonal, owner));
        var store = new MigrationStore(EquipmentData.Create(state, legacy.Evaluator), current);
        return (new EquipmentMigrationService(store, new(current), new RecordingItemizationOutbox(), TimeProvider.System, store), store, owner);
    }

    private sealed class MigrationStore(EquipmentData item, StarterEquipmentCatalog catalog) : IEquipmentMigrationRepository, IEquipmentUpgradeRepository, IEquipmentStartupConversionRepository
    {
        public EquipmentData? Item = item;
        public LegacyEquipmentSnapshot? Legacy;
        public int Saves;
        public bool ScheduledCombat;
        public bool Referenced = true;
        public int StartupLocks;
        private readonly Dictionary<Guid, EquipmentMigrationReceipt> _receipts = [];
        public Task AssertNoScheduledCombatAsync(IReadOnlyList<Guid> ids, CancellationToken ct) => ScheduledCombat
            ? throw new InvalidOperationException("Scheduled combat") : Task.CompletedTask;
        public Task LockCharactersAsync(EquipmentMigrationTarget target, CancellationToken ct) { StartupLocks++; return Task.CompletedTask; }
        public Task<bool> IsCandidateAsync(EquipmentMigrationTarget target, int version, CancellationToken ct) => Task.FromResult(Referenced);
        public Task<IAsyncDisposable> AcquireRunnerLockAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<EquipmentMigrationTarget>> GetTargetsAsync(int version, int limit, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Guid>> GetArenaDefensesAsync(int version, int limit, CancellationToken ct) => throw new NotSupportedException();
        public Task<EquipmentStartupConversionAudit> AuditAsync(int version, CancellationToken ct) => throw new NotSupportedException();
        public Task<EquipmentMigrationAudit> AuditAsync(int page, int pageSize, CancellationToken ct, int sourceBalanceVersion = 1) => throw new NotSupportedException();
        public Task<EquipmentData?> LoadAsync(EquipmentMigrationTarget target, bool mutation, CancellationToken ct) => Task.FromResult(Item);
        public Task<LegacyEquipmentSnapshot?> LoadLegacyAsync(EquipmentMigrationTarget target, CancellationToken ct) => Task.FromResult(Legacy);
        public Task RestoreLegacyAsync(EquipmentMigrationTarget target, LegacyEquipmentSnapshot original, EquipmentMigrationReceipt receipt, CancellationToken ct)
        { Item = null; Legacy = original; Saves++; return Task.CompletedTask; }
        public Task<IReadOnlyList<Guid>> GetAffectedCharactersAsync(EquipmentMigrationTarget target, CancellationToken ct) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<EquipmentMigrationReceipt?> GetReceiptAsync(Guid id, CancellationToken ct) => Task.FromResult(_receipts.GetValueOrDefault(id));
        public Task<EquipmentMigrationReceipt?> GetActiveReceiptForItemAsync(Guid id, CancellationToken ct) => Task.FromResult(_receipts.Values.Where(x => x.ItemId == id && x.RolledBackAtUtc is null).OrderByDescending(x => x.Revision).FirstOrDefault());
        public Task SaveAsync(EquipmentMigrationTarget target, EquipmentData data, EquipmentMigrationReceipt receipt, bool isNew, CancellationToken ct)
        { Item = data; _receipts[receipt.OperationId] = receipt; Saves++; return Task.CompletedTask; }
        public Task<EquipmentUpgradeReceipt?> GetReceiptAsync(Guid character, Guid id, CancellationToken ct) => Task.FromResult<EquipmentUpgradeReceipt?>(null);
        public Task<EquipmentUpgradeContext?> LoadAsync(Guid character, Guid id, bool mutation, CancellationToken ct)
        {
            if (Item is null) return Task.FromResult<EquipmentUpgradeContext?>(null);
            var equipment = new Domain.Models.Items.Equipments.EquipmentInstance { Id = id, ItemBaseId = Item.ItemBaseId,
                ItemBase = catalog.EquipmentBases.GetValueOrDefault(Item.ItemBaseId)
                    ?? new Domain.Models.Items.Equipments.EquipmentBase { Id = Item.ItemBaseId, EquipmentType = Item.EquipmentType } };
            equipment.ApplyProgressionData(Item);
            return Task.FromResult<EquipmentUpgradeContext?>(new(new() { Id = character }, new(), equipment, false, null, []));
        }
        public Task ApplyAsync(EquipmentUpgradeContext context, EquipmentUpgradeQuote quote, EquipmentUpgradeReceipt receipt, CancellationToken ct) => throw new NotSupportedException();
    }

}
