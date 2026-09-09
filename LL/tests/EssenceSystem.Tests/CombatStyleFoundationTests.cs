using Application.Interfaces.Services.LL.CombatStyles;
using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.CombatStyles.Dtos;
using AutoMapper;
using BalanceHarness;
using Domain.Models.CombatStyles;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Services.LL.CombatStyles;
using Services.LL.Combat.Engine;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.CombatStyles;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class CombatStyleFoundationTests
{
    private static readonly long[] Requirements = [100, 200, 400, 700, 1100, 1600, 2300, 3200, 4400, 6000];

    [Fact]
    public async Task Same_service_reloads_progress_after_persistence_clears_its_tracked_state()
    {
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var id = Guid.NewGuid();
        var original = new CharacterCombatStyle { CharacterId = id, CombatStyleId = CombatStyleIds.Bastion };
        db.CharacterCombatStyles.Add(original);
        await db.SaveChangesAsync();
        var service = new CombatStyleService(new CombatStyleRepository(db), new TestCatalog(), null!, null!, null!, null!);
        await service.GrantCapturedCombatXpAsync(id, CombatStyleIds.Bastion, 150, default);
        await db.SaveChangesAsync();
        db.ClearTrackedEntities();

        await service.GrantCapturedCombatXpAsync(id, CombatStyleIds.Bastion, 100, default);
        await db.SaveChangesAsync();
        var fresh = await db.CharacterCombatStyles.SingleAsync();

        Assert.NotSame(original, fresh);
        Assert.Equal(1, fresh.Level);
        Assert.Equal(150, fresh.CurrentXp);
        Assert.Equal(50, original.CurrentXp);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(4, 0)]
    [InlineData(5, 1)]
    [InlineData(7, 1)]
    [InlineData(8, 2)]
    [InlineData(10, 2)]
    public void Upgrade_slots_are_derived_from_individual_level(int level, int slots)
    {
        Assert.Equal(slots, CombatStyleProgression.UpgradeSlots(level));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, .01)]
    [InlineData(2, .02)]
    [InlineData(3, .03)]
    [InlineData(9, .09)]
    [InlineData(10, .10)]
    public void Each_mastery_level_adds_the_catalog_bonus_without_a_separate_rank(int level, double expected)
    {
        var catalog = LoadCatalog();
        foreach (var definition in catalog.Styles)
        {
            var snapshot = CombatStyleRules.Snapshot(catalog, definition,
                new() { CombatStyleId = definition.Id, Level = level }, new(definition.Id, null, [], null), null);
            Assert.Equal(expected, snapshot.BarrierMasteryBonus, 8);
            Assert.Equal(expected, snapshot.FocusMasteryBonus, 8);
            var json = JsonSerializer.Serialize(snapshot);
            Assert.DoesNotContain("\"CoreRank\"", json);
            Assert.DoesNotContain("\"BarrierMasteryBonus\"", json);
            Assert.DoesNotContain("\"FocusMasteryBonus\"", json);
        }
    }

    [Fact]
    public void Large_award_crosses_all_milestones_and_discards_overflow_without_training_other_style()
    {
        var bastion = new CharacterCombatStyle { CombatStyleId = "bastion" };
        var conduit = new CharacterCombatStyle { CombatStyleId = "conduit" };
        var result = CombatStyleProgression.Grant(bastion, long.MaxValue, Requirements);
        Assert.Equal(20000, result.XpGained);
        Assert.Equal(10, result.LevelsGained);
        Assert.Equal(10, bastion.Level);
        Assert.Equal(0, bastion.CurrentXp);
        Assert.Equal(0, conduit.Level);
        Assert.Equal(0, CombatStyleProgression.Grant(bastion, 100, Requirements).XpGained);
    }

    [Fact]
    public void Split_and_single_awards_have_identical_progress()
    {
        var single = new CharacterCombatStyle();
        var split = new CharacterCombatStyle();
        CombatStyleProgression.Grant(single, 7199, Requirements);
        foreach (var xp in new[] { 99, 301, 2400, 4399 }) CombatStyleProgression.Grant(split, xp, Requirements);
        Assert.Equal(single.Level, split.Level);
        Assert.Equal(single.CurrentXp, split.CurrentXp);
    }

    [Fact]
    public void Catalog_rejects_duplicate_missing_and_unsupported_content()
    {
        var valid = LoadCatalog();
        CombatStyleRules.ValidateCatalog(valid);
        Assert.Throws<InvalidOperationException>(() => CombatStyleRules.ValidateCatalog(valid with { XpRequirements = [0] }));
        Assert.Throws<InvalidOperationException>(() => CombatStyleRules.ValidateCatalog(valid with { Styles = [valid.Styles[0], valid.Styles[0]] }));
        Assert.Throws<InvalidOperationException>(() => CombatStyleRules.ValidateCatalog(valid with { Styles = [valid.Styles[0]] }));
        Assert.Throws<InvalidOperationException>(() => CombatStyleRules.ValidateCatalog(valid with {
            Styles = [valid.Styles[0] with { Upgrades = [] }, valid.Styles[1]] }));
    }

    [Fact]
    public void Current_catalog_requires_finite_nonnegative_per_mastery_level_tuning()
    {
        var catalog = LoadCatalog();
        var definition = catalog.Styles[0];
        foreach (var tuning in new[]
        {
            definition.Tuning with { BarrierPerMasteryLevel = null },
            definition.Tuning with { FocusPerMasteryLevel = null },
            definition.Tuning with { BarrierPerMasteryLevel = double.NaN },
            definition.Tuning with { FocusPerMasteryLevel = double.PositiveInfinity },
            definition.Tuning with { BarrierPerMasteryLevel = -.01 },
            definition.Tuning with { FocusPerMasteryLevel = -.01 }
        })
            Assert.Throws<InvalidOperationException>(() => CombatStyleRules.ValidateCatalog(catalog with
            {
                Styles = [definition with { Tuning = tuning }, catalog.Styles[1]]
            }));
    }

    [Fact]
    public void Legacy_snapshot_retains_captured_rank_and_tuning_through_serialization()
    {
        var snapshot = JsonSerializer.Deserialize<CombatStyleSnapshot>("""
            {"CombatStyleId":"conduit","Kind":2,"ContentVersion":"combat-styles.v2","Level":9,"CoreRank":3,
             "Tuning":{"BarrierPerCoreRank":0.025,"FocusPerCoreRank":0.015}}
            """)!;
        Assert.Null(snapshot.Tuning.BarrierPerMasteryLevel);
        Assert.Null(snapshot.Tuning.FocusPerMasteryLevel);
        Assert.Equal(.075, snapshot.BarrierMasteryBonus, 8);
        Assert.Equal(.045, snapshot.FocusMasteryBonus, 8);

        var json = JsonSerializer.Serialize(snapshot);
        Assert.DoesNotContain("PerMasteryLevel", json);
        var restored = JsonSerializer.Deserialize<CombatStyleSnapshot>(json)!;
        Assert.Equal(snapshot.BarrierMasteryBonus, restored.BarrierMasteryBonus);
        Assert.Equal(snapshot.FocusMasteryBonus, restored.FocusMasteryBonus);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Explicit_legacy_zero_rank_preserves_json_identity_while_new_snapshots_omit_rank(bool harnessJson)
    {
        var options = harnessJson ? HarnessJson.Options : JsonSerializerOptions.Default;
        var rankProperty = harnessJson ? "coreRank" : "CoreRank";
        var legacy = JsonSerializer.Deserialize<CombatStyleSnapshot>(harnessJson
            ? """{"combatStyleId":"bastion","kind":"Bastion","level":1,"coreRank":0}"""
            : """{"CombatStyleId":"bastion","Kind":1,"Level":1,"CoreRank":0}""", options)!;

        var serialized = JsonSerializer.Serialize(legacy, options);
        using var document = JsonDocument.Parse(serialized);
        Assert.Equal(0, document.RootElement.GetProperty(rankProperty).GetInt32());
        var restored = JsonSerializer.Deserialize<CombatStyleSnapshot>(serialized, options)!;
        Assert.Equal(0, restored.CoreRank);
        Assert.Equal(0, restored.BarrierMasteryBonus);
        Assert.Equal(HarnessJson.Hash(legacy), HarnessJson.Hash(restored));

        var catalog = LoadCatalog();
        var current = CombatStyleRules.Snapshot(catalog, catalog.Styles[0], new() { Level = 1 },
            new(catalog.Styles[0].Id, null, [], null), null);
        using var currentDocument = JsonDocument.Parse(JsonSerializer.Serialize(current, options));
        Assert.False(currentDocument.RootElement.TryGetProperty(rankProperty, out _));
        Assert.Null(current.CoreRank);
    }

    [Fact]
    public void Explicit_per_level_tuning_takes_precedence_over_legacy_ranks_and_clamps_at_maximum_level()
    {
        var snapshot = new CombatStyleSnapshot
        {
            Level = 11, CoreRank = 5,
            Tuning = new() { BarrierPerMasteryLevel = .007, FocusPerMasteryLevel = .009,
                BarrierPerCoreRank = .50, FocusPerCoreRank = .50 }
        };
        Assert.Equal(.07, snapshot.BarrierMasteryBonus, 8);
        Assert.Equal(.09, snapshot.FocusMasteryBonus, 8);
        var disabled = snapshot with { Tuning = snapshot.Tuning with { BarrierPerMasteryLevel = 0, FocusPerMasteryLevel = 0 } };
        Assert.Equal(0, disabled.BarrierMasteryBonus);
        Assert.Equal(0, disabled.FocusMasteryBonus);
    }

    [Fact]
    public void Selection_requires_individual_milestones_and_distinct_compatible_upgrades()
    {
        var catalog = LoadCatalog();
        var bastion = catalog.Styles.Single(x => x.Id == "bastion");
        var owned = new CharacterCombatStyle { CombatStyleId = "bastion" };
        CombatStyleSelectionRequest select = new("bastion", "rebuild", [], null);
        Assert.NotNull(CombatStyleRules.ValidateSelection(null, bastion, select, []));
        Assert.NotNull(CombatStyleRules.ValidateSelection(owned, bastion, select, []));
        owned.Level = 3;
        Assert.Null(CombatStyleRules.ValidateSelection(owned, bastion, select, []));
        owned.Level = 10;
        Assert.NotNull(CombatStyleRules.ValidateSelection(owned, bastion,
            select with { UpgradeIds = ["prepared-wall", "prepared-wall"] }, []));
        Assert.NotNull(CombatStyleRules.ValidateSelection(owned, bastion,
            select with { UpgradeIds = ["full-circuit"] }, []));
        Assert.Null(CombatStyleRules.ValidateSelection(owned, bastion,
            select with { UpgradeIds = ["prepared-wall", "hold-the-breach"] }, []));
    }

    [Theory]
    [InlineData(8, CombatStyleIds.PreparedWall, false)]
    [InlineData(9, CombatStyleIds.PreparedWall, true)]
    [InlineData(10, CombatStyleIds.PreparedWall, true)]
    [InlineData(9, CombatStyleIds.HoldTheBreach, false)]
    [InlineData(9, CombatStyleIds.FullCircuit, false)]
    [InlineData(9, null, true)]
    public void Mastery_requires_level_nine_and_one_equipped_upgrade(int level, string? mastery, bool valid)
    {
        var definition = LoadCatalog().Styles.Single(x => x.Id == CombatStyleIds.Bastion);
        var owned = new CharacterCombatStyle { CombatStyleId = definition.Id, Level = level };
        var selection = new CombatStyleSelectionRequest(definition.Id, null, [CombatStyleIds.PreparedWall], null,
            MasteredUpgradeId: mastery);
        Assert.Equal(valid, CombatStyleRules.ValidateSelection(owned, definition, selection, []) is null);
        var snapshot = CombatStyleRules.Snapshot(LoadCatalog(), definition, owned, selection, null);
        Assert.Equal(valid && mastery is not null, snapshot.HasMasteredUpgrade(CombatStyleIds.PreparedWall));
    }

    [Fact]
    public void Empty_style_cannot_hold_a_mastery_choice()
    {
        Assert.NotNull(CombatStyleRules.ValidateSelection(null, null,
            new(null, null, [], null, MasteredUpgradeId: CombatStyleIds.PreparedWall), []));
    }

    [Fact]
    public void Old_committed_snapshot_keeps_milestones_inert_and_new_snapshot_keeps_independent_tuning()
    {
        var old = JsonSerializer.Deserialize<CombatStyleSnapshot>("""
            {"CombatStyleId":"conduit","Kind":2,"ContentVersion":"combat-styles.v1","Level":10,"CoreRank":5,"UpgradeIds":["full-circuit"]}
            """)!;
        Assert.Equal(new CombatStyleMilestoneTuning(), old.MilestoneTuning);
        Assert.Null(old.MasteredUpgradeId);
        Assert.False(old.HasMasteredUpgrade(CombatStyleIds.FullCircuit));

        var catalog = LoadCatalog();
        var definition = catalog.Styles.Single(x => x.Id == CombatStyleIds.Conduit);
        var snapshot = CombatStyleRules.Snapshot(catalog, definition,
            new() { CombatStyleId = definition.Id, Level = 9 },
            new(definition.Id, CombatStyleIds.DeepReservoir, [CombatStyleIds.FullCircuit], Guid.NewGuid(),
                MasteredUpgradeId: CombatStyleIds.FullCircuit), "essence.focus");
        Assert.Equal(4, snapshot.Tuning.ChargeCap);
        Assert.Equal(1, snapshot.MilestoneTuning.OpeningCharge);
        Assert.Equal(2, snapshot.MilestoneTuning.FullCircuitMinimumCharge);
        Assert.True(snapshot.HasMasteredUpgrade(CombatStyleIds.FullCircuit));
        Assert.Equal(snapshot, JsonSerializer.Deserialize<CombatStyleSnapshot>(JsonSerializer.Serialize(snapshot))! with
        {
            UpgradeIds = snapshot.UpgradeIds
        });
    }

    [Fact]
    public void Catalog_rejects_invalid_milestone_tuning()
    {
        var valid = LoadCatalog();
        Assert.All(valid.Styles, style =>
        {
            Assert.NotNull(style.OpeningTechnique);
            Assert.All(style.Upgrades, upgrade => Assert.False(string.IsNullOrWhiteSpace(upgrade.MasteryDescription)));
        });
        foreach (var tuning in new[] {
            new CombatStyleMilestoneTuning { OpeningBarrierFraction = double.NaN },
            new CombatStyleMilestoneTuning { MeasuredRecoveryOverhealBarrierFraction = -1 },
            new CombatStyleMilestoneTuning { OpeningCharge = 3 },
            new CombatStyleMilestoneTuning { FullCircuitMinimumCharge = 3 } })
            Assert.Throws<InvalidOperationException>(() => CombatStyleRules.ValidateCatalog(valid with
            {
                Styles = [valid.Styles[0], valid.Styles[1] with { MilestoneTuning = tuning }]
            }));
    }

    [Fact]
    public void Missing_or_ineligible_focus_is_not_silently_replaced()
    {
        var conduit = LoadCatalog().Styles.Single(x => x.Id == "conduit");
        var owned = new CharacterCombatStyle { CombatStyleId = "conduit" };
        var focusId = Guid.NewGuid();
        CombatStyleSelectionRequest selection = new("conduit", null, [], focusId);
        CombatStyleFocusOption option = new(focusId, "essence.test", "Test", "ability.test", 10, false, []);
        Assert.NotNull(CombatStyleRules.ValidateSelection(owned, conduit, selection, []));
        Assert.NotNull(CombatStyleRules.ValidateSelection(owned, conduit, selection, [option]));
        Assert.Null(CombatStyleRules.ValidateSelection(owned, conduit, selection, [option with { IsEligible = true }]));
    }

    [Fact]
    public void Snapshot_copies_choices_and_keeps_level_bonus_and_tuning_after_progress_changes()
    {
        var catalog = LoadCatalog();
        var owned = new CharacterCombatStyle { CombatStyleId = "conduit", Level = 8 };
        var upgrades = new List<string> { "full-circuit" };
        var snapshot = CombatStyleRules.Snapshot(catalog, catalog.Styles.Single(x => x.Id == "conduit"), owned,
            new("conduit", "deep-reservoir", upgrades, Guid.NewGuid()), "essence.focus");
        owned.Level = 10;
        upgrades.Clear();
        Assert.Equal(8, snapshot.Level);
        Assert.Equal(.08, snapshot.FocusMasteryBonus, 8);
        Assert.Equal(4, snapshot.Tuning.ChargeCap);
        Assert.Equal(.6, snapshot.Tuning.FocusBaseMultiplier);
        Assert.True(snapshot.HasUpgrade("full-circuit"));
        Assert.Equal("essence.focus", snapshot.FocusEssenceDefinitionId);
    }

    [Fact]
    public async Task Reward_uses_captured_style_and_loads_owned_rows_once_per_scope()
    {
        var repository = new TestRepository();
        repository.Styles.Add(new() { CombatStyleId = "bastion" });
        repository.Styles.Add(new() { CombatStyleId = "conduit" });
        var service = new CombatStyleService(repository, new TestCatalog(), null!, null!, null!, null!);
        await service.GrantCapturedCombatXpAsync(Guid.Empty, "bastion", 150, default);
        await service.GrantCapturedCombatXpAsync(Guid.Empty, "bastion", 150, default);
        await service.GrantCapturedCombatXpAsync(Guid.Empty, null, 9000, default);
        Assert.Equal(2, repository.Styles[0].Level);
        Assert.Equal(0, repository.Styles[1].Level);
        Assert.Equal(1, repository.LoadCount);
    }

    [Fact]
    public void Mapping_preserves_nullable_global_selection_and_normalizes_omitted_upgrade_array()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<CombatStyleMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();
        var result = mapper.Map<CombatStyleSelectionRequest>(new CombatStyleSelectionDto());
        Assert.Null(result.CombatStyleId);
        Assert.Empty(result.UpgradeIds);
        Assert.Null(result.MasteredUpgradeId);
        var mastered = mapper.Map<CombatStyleSelectionRequest>(new CombatStyleSelectionDto(
            CombatStyleIds.Bastion, UpgradeIds: [CombatStyleIds.PreparedWall], MasteredUpgradeId: CombatStyleIds.PreparedWall));
        Assert.Equal(CombatStyleIds.PreparedWall, mastered.MasteredUpgradeId);
    }

    [Fact]
    public async Task Overview_contract_keeps_focus_eligibility_and_effect_amounts_without_retired_display_fields()
    {
        var focus = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.focus" };
        var service = CreateService(new TestRepository(), new TestLoadouts([focus]));
        var preview = await service.PreviewAsync(Guid.Empty, new(CombatStyleIds.Conduit, null, [], focus.Id), default);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<CombatStyleMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();
        var dto = mapper.Map<CombatStyleOverviewDto>(preview);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var option = json.RootElement.GetProperty("focusOptions")[0];
        Assert.False(option.TryGetProperty("castOrder", out _));
        Assert.Equal(focus.Id, option.GetProperty("playerEssenceId").GetGuid());
        Assert.True(option.GetProperty("isEligible").GetBoolean());
        Assert.Equal(10, option.GetProperty("cooldownTicks").GetInt32());
        Assert.All(json.RootElement.GetProperty("styles").EnumerateArray(),
            style => Assert.False(style.GetProperty("definition").TryGetProperty("tradeoff", out _)));
        Assert.Equal("80% of normal strength", dto.PreviewFacts[0].Value);
        Assert.Null(dto.PreviewFacts[0].Condition);
        Assert.Equal(.8, preview.EffectiveStyle!.Tuning.FocusBaseMultiplier);
    }

    [Fact]
    public async Task Initial_overview_and_preview_make_both_styles_available_without_creating_persistence_rows()
    {
        var repository = new TestRepository();
        var service = CreateService(repository);
        var overview = await service.GetOverviewAsync(Guid.Empty, default);
        Assert.Equal(new[] { CombatStyleIds.Bastion, CombatStyleIds.Conduit }, overview.Styles.Select(x => x.Definition.Id));
        Assert.All(overview.Styles, style => { Assert.Equal(0, style.Level); Assert.Equal(0, style.CurrentXp); Assert.Equal(Requirements[0], style.XpRequired); });
        Assert.Null(overview.Selection.CombatStyleId);

        var bastion = await service.PreviewAsync(Guid.Empty, new(CombatStyleIds.Bastion, null, [], null), default);
        Assert.Null(bastion.ValidationIssue);
        Assert.Equal(CombatStyleIds.Bastion, bastion.EffectiveStyle!.CombatStyleId);
        var conduit = await service.PreviewAsync(Guid.Empty, new(CombatStyleIds.Conduit, null, [], null), default);
        Assert.Contains("Focus", conduit.ValidationIssue);
        Assert.Empty(repository.Styles);
        Assert.Null(repository.Selection);
        Assert.Equal(0, repository.SelectionAddCount);
    }

    [Fact]
    public async Task Saving_a_new_style_twice_persists_one_progress_row_and_one_global_selection()
    {
        var repository = new TestRepository();
        var service = CreateService(repository);
        var selection = new CombatStyleSelectionRequest(CombatStyleIds.Bastion, null, [], null);
        Assert.True((await service.SelectAsync(Guid.Empty, selection, default)).Succeeded);
        Assert.True((await service.SelectAsync(Guid.Empty, selection, default)).Succeeded);
        Assert.Equal(CombatStyleIds.Bastion, Assert.Single(repository.Styles).CombatStyleId);
        Assert.Equal(1, repository.SelectionAddCount);
        Assert.Equal(Guid.Empty, repository.Selection!.CharacterId);
        Assert.Equal(0, repository.Styles[0].Level);
        Assert.Equal(0, (await service.GetOverviewAsync(Guid.Empty, default)).Styles.Single(x => x.Definition.Id == CombatStyleIds.Conduit).Level);
    }

    [Theory]
    [InlineData(EssenceCombatActivity.None)]
    [InlineData(EssenceCombatActivity.IdleCombat)]
    [InlineData(EssenceCombatActivity.Dungeon)]
    [InlineData(EssenceCombatActivity.Raid)]
    [InlineData(EssenceCombatActivity.WorldTower)]
    [InlineData(EssenceCombatActivity.Arena)]
    [InlineData(EssenceCombatActivity.Tournament)]
    [InlineData(EssenceCombatActivity.RegionBoss)]
    public async Task Global_selection_and_empty_slot_apply_to_every_battle_activity(EssenceCombatActivity activity)
    {
        var repository = new TestRepository();
        var service = CreateService(repository);
        await service.SelectAsync(Guid.Empty, new(CombatStyleIds.Bastion, null, [], null), default);
        Assert.Equal(CombatStyleIds.Bastion, (await service.ResolveAsync(Guid.Empty, activity, default))!.CombatStyleId);

        await service.SelectAsync(Guid.Empty, new(null, null, [], null), default);
        Assert.Null(await service.ResolveAsync(Guid.Empty, activity, default));
        Assert.Equal(CombatStyleIds.Bastion, Assert.Single(repository.Styles).CombatStyleId);
        Assert.Equal(1, repository.SelectionAddCount);
    }

    [Fact]
    public async Task Conduit_uses_one_global_focus_and_rejects_battles_where_that_essence_is_not_equipped()
    {
        var repository = new TestRepository();
        var focus = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.focus" };
        var loadouts = new TestLoadouts([focus]);
        var service = CreateService(repository, loadouts);
        Assert.True((await service.SelectAsync(Guid.Empty, new(CombatStyleIds.Conduit, null, [], focus.Id), default)).Succeeded);

        foreach (var activity in Enum.GetValues<EssenceCombatActivity>())
        {
            var snapshot = await service.ResolveAsync(Guid.Empty, activity, default);
            Assert.Equal(CombatStyleIds.Conduit, snapshot!.CombatStyleId);
            Assert.Equal(focus.Id, snapshot.FocusPlayerEssenceId);
        }
        Assert.Equal(Enum.GetValues<EssenceCombatActivity>(), loadouts.Activities.TakeLast(Enum.GetValues<EssenceCombatActivity>().Length));
        await Assert.ThrowsAsync<CombatStyleConfigurationException>(() => service.ResolveAsync(Guid.Empty, EssenceCombatActivity.Dungeon, default, []));
        Assert.Equal(focus.Id, repository.Selection!.FocusPlayerEssenceId);
    }

    [Fact]
    public async Task Switching_styles_restores_individual_choices_and_keeps_captured_xp_with_its_style()
    {
        var repository = new TestRepository();
        repository.Styles.Add(new() { CombatStyleId = CombatStyleIds.Bastion, Level = 5, CurrentXp = 123,
            RefinementId = CombatStyleIds.Rebuild, UpgradeIds = [CombatStyleIds.PreparedWall] });
        var focus = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.focus" };
        var service = CreateService(repository, new TestLoadouts([focus]));
        await service.SelectAsync(Guid.Empty, new(CombatStyleIds.Conduit, null, [], focus.Id), default);
        await service.GrantCapturedCombatXpAsync(Guid.Empty, CombatStyleIds.Bastion, 100, default);
        var result = await service.SelectAsync(Guid.Empty, new(CombatStyleIds.Bastion, null, [], null, true), default);

        Assert.True(result.Succeeded);
        Assert.Equal(CombatStyleIds.Rebuild, repository.Selection!.RefinementId);
        Assert.Equal(new[] { CombatStyleIds.PreparedWall }, repository.Selection.UpgradeIds);
        var overview = await service.GetOverviewAsync(Guid.Empty, default);
        Assert.Equal(223, overview.Styles.Single(x => x.Definition.Id == CombatStyleIds.Bastion).CurrentXp);
        Assert.Equal(5, overview.EffectiveStyle!.Level);
        Assert.Equal(0, overview.Styles.Single(x => x.Definition.Id == CombatStyleIds.Conduit).Level);
        Assert.Equal(0, overview.Styles.Single(x => x.Definition.Id == CombatStyleIds.Conduit).CurrentXp);
    }

    [Fact]
    public async Task Mastery_is_remembered_per_style_cleared_explicitly_and_never_selected_automatically()
    {
        var repository = new TestRepository();
        repository.Styles.Add(new() { CombatStyleId = CombatStyleIds.Bastion, Level = 9 });
        repository.Styles.Add(new() { CombatStyleId = CombatStyleIds.Conduit, Level = 9 });
        var focus = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.focus" };
        var service = CreateService(repository, new TestLoadouts([focus]));
        var bastion = new CombatStyleSelectionRequest(CombatStyleIds.Bastion, CombatStyleIds.Shelter,
            [CombatStyleIds.PreparedWall, CombatStyleIds.MeasuredRecovery], null, MasteredUpgradeId: CombatStyleIds.MeasuredRecovery);
        Assert.True((await service.SelectAsync(Guid.Empty, bastion, default)).Succeeded);
        Assert.True((await service.SelectAsync(Guid.Empty,
            new(CombatStyleIds.Conduit, null, [CombatStyleIds.FullCircuit], focus.Id, MasteredUpgradeId: CombatStyleIds.FullCircuit), default)).Succeeded);
        Assert.True((await service.SelectAsync(Guid.Empty, new(null, null, [], null), default)).Succeeded);
        Assert.Null(repository.Selection!.MasteredUpgradeId);
        Assert.True((await service.SelectAsync(Guid.Empty, new(CombatStyleIds.Bastion, null, [], null, true), default)).Succeeded);
        var overview = await service.GetOverviewAsync(Guid.Empty, default);
        Assert.Equal(CombatStyleIds.MeasuredRecovery, overview.Selection.MasteredUpgradeId);
        Assert.Equal(CombatStyleIds.MeasuredRecovery, overview.EffectiveStyle!.MasteredUpgradeId);
        Assert.Equal(CombatStyleIds.FullCircuit, overview.Styles.Single(x => x.Definition.Id == CombatStyleIds.Conduit).MasteredUpgradeId);

        Assert.False((await service.SelectAsync(Guid.Empty, bastion with { UpgradeIds = [CombatStyleIds.PreparedWall] }, default)).Succeeded);
        Assert.Equal(CombatStyleIds.MeasuredRecovery, repository.Selection!.MasteredUpgradeId);
        Assert.True((await service.SelectAsync(Guid.Empty, bastion with { MasteredUpgradeId = null }, default)).Succeeded);
        Assert.Null(repository.Selection.MasteredUpgradeId);
        Assert.Null(repository.Styles.Single(x => x.CombatStyleId == CombatStyleIds.Bastion).MasteredUpgradeId);
        Assert.True((await service.SelectAsync(Guid.Empty, new(CombatStyleIds.Bastion, null, [], null, true), default)).Succeeded);
        Assert.Null((await service.GetOverviewAsync(Guid.Empty, default)).Selection.MasteredUpgradeId);
    }

    [Fact]
    public async Task Reaching_mastery_level_does_not_assign_a_mastered_upgrade()
    {
        var repository = new TestRepository();
        repository.Styles.Add(new() { CombatStyleId = CombatStyleIds.Bastion, Level = 8,
            UpgradeIds = [CombatStyleIds.PreparedWall] });
        var service = CreateService(repository);
        await service.GrantCapturedCombatXpAsync(Guid.Empty, CombatStyleIds.Bastion,
            CombatStyleProgression.XpRequired(8, Requirements), default);
        var entry = (await service.GetOverviewAsync(Guid.Empty, default)).Styles.Single(x => x.Definition.Id == CombatStyleIds.Bastion);
        Assert.Equal(9, entry.Level);
        Assert.Null(entry.MasteredUpgradeId);
    }

    [Theory]
    [InlineData(6, false)]
    [InlineData(7, true)]
    public async Task Opening_preview_unlocks_at_level_seven(int level, bool unlocked)
    {
        var repository = new TestRepository();
        repository.Styles.Add(new() { CombatStyleId = CombatStyleIds.Bastion, Level = level });
        var overview = await CreateService(repository).PreviewAsync(Guid.Empty, new(CombatStyleIds.Bastion, null, [], null), default);
        Assert.Equal(unlocked, overview.PreviewFacts.Any(x => x.Label == "Opening Technique"));
        if (unlocked) Assert.Equal("5% Max Health as starting Barrier", overview.PreviewFacts.Single(x => x.Label == "Opening Technique").Value);
    }

    [Theory]
    [InlineData(CombatStyleIds.Bastion, 1, "50 Health + 151.5 Barrier")]
    [InlineData(CombatStyleIds.Bastion, 3, "50 Health + 154.5 Barrier")]
    [InlineData(CombatStyleIds.Conduit, 1, "101% of normal strength")]
    [InlineData(CombatStyleIds.Conduit, 3, "103% of normal strength")]
    public async Task Odd_mastery_levels_update_service_previews(string styleId, int level, string expected)
    {
        var repository = new TestRepository();
        repository.Styles.Add(new() { CombatStyleId = styleId, Level = level });
        var focus = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.focus" };
        var service = CreateService(repository, new TestLoadouts([focus]));
        var conduit = styleId == CombatStyleIds.Conduit;
        var preview = await service.PreviewAsync(Guid.Empty,
            new(styleId, null, [], conduit ? focus.Id : null), default);

        Assert.Equal(expected, preview.PreviewFacts.Single(x => x.Label == (conduit ? "1 Charge" : "200 healing received")).Value);
        if (conduit)
            Assert.Equal("80% of normal strength", preview.PreviewFacts.Single(x => x.Label == "0 Charge").Value);
    }

    [Fact]
    public async Task Bastion_mastery_previews_follow_allocated_healing_and_refinements()
    {
        var repository = new TestRepository();
        var progress = new CharacterCombatStyle { CombatStyleId = CombatStyleIds.Bastion, Level = 5 };
        repository.Styles.Add(progress);
        var service = CreateService(repository);
        var preparedRequest = new CombatStyleSelectionRequest(CombatStyleIds.Bastion, null, [CombatStyleIds.PreparedWall], null);
        var preview = await service.PreviewAsync(Guid.Empty, preparedRequest, default);
        Assert.Equal("50 Health + 157.5 Barrier", preview.PreviewFacts.Single(x => x.Label == "200 healing received").Value);
        var preparedBonus = preview.PreviewFacts.Single(x => x.Label == "Prepared Wall");
        Assert.Equal("+7.5% flat increase · +15 Barrier", preparedBonus.Value);
        Assert.Equal("Gain extra Barrier when you receive healing at 80% Health or higher.", preparedBonus.Condition);

        progress.Level = 9;
        var request = new CombatStyleSelectionRequest(CombatStyleIds.Bastion, CombatStyleIds.Rebuild,
            [CombatStyleIds.HoldTheBreach, CombatStyleIds.MeasuredRecovery], null, MasteredUpgradeId: CombatStyleIds.HoldTheBreach);
        preview = await service.PreviewAsync(Guid.Empty, request, default);
        Assert.Equal("60 Health + 163.5 Barrier", preview.PreviewFacts.Single(x => x.Label == "200 healing received").Value);
        Assert.Equal("+7.5% flat increase · +15 Barrier", preview.PreviewFacts.Single(x => x.Label == "Hold the Breach").Value);
        Assert.Equal("Gain extra Barrier when you receive healing with no Barrier.", preview.PreviewFacts.Single(x => x.Label == "Hold the Breach").Condition);
        Assert.Equal("+20% Health recovery (×1.2) · 60 Health", preview.PreviewFacts.Single(x => x.Label == "Measured Recovery").Value);
        Assert.Equal("+20% Health recovery (×1.2) · 72 Health", preview.PreviewFacts.Single(x => x.Label == "Hold the Breach mastery").Value);
        Assert.Equal("+20% Health recovery (×1.2) · 240 Health", preview.PreviewFacts.Single(x => x.Label == "Rebuild with mastery").Value);
        preview = await service.PreviewAsync(Guid.Empty, request with { UpgradeIds = [CombatStyleIds.HoldTheBreach] }, default);
        Assert.Equal("+20% Health recovery (×1.2) · 60 Health", preview.PreviewFacts.Single(x => x.Label == "Hold the Breach mastery").Value);
        Assert.Equal("+20% Health recovery (×1.2) · 240 Health", preview.PreviewFacts.Single(x => x.Label == "Rebuild with mastery").Value);
        preview = await service.PreviewAsync(Guid.Empty, request with { MasteredUpgradeId = CombatStyleIds.MeasuredRecovery }, default);
        Assert.Contains("100% of excess Health healing", preview.PreviewFacts.Single(x => x.Label == "Measured Recovery mastery").Value);
        Assert.DoesNotContain(preview.PreviewFacts, x => x.Label == "Hold the Breach mastery");
        preview = await service.PreviewAsync(Guid.Empty, preparedRequest with { MasteredUpgradeId = CombatStyleIds.PreparedWall }, default);
        Assert.Equal("50 Health + 163.5 Barrier", preview.PreviewFacts.Single(x => x.Label == "200 healing received").Value);
        var masteredPrepared = preview.PreviewFacts.Single(x => x.Label == "Prepared Wall");
        Assert.Equal(preparedBonus.Value, masteredPrepared.Value);
        Assert.Contains("80% Health or higher", masteredPrepared.Condition);
        Assert.Contains("no Barrier", masteredPrepared.Condition);
    }

    [Theory]
    [InlineData(CombatStyleIds.FullCircuit, "109% of normal strength", "134% of normal strength", "154% of normal strength")]
    [InlineData(CombatStyleIds.PartialFlow, "114% of normal strength", "134% of normal strength", "149% of normal strength")]
    [InlineData(CombatStyleIds.EmergencyChannel, "109% of normal strength", "129% of normal strength", "149% of normal strength")]
    public async Task Conduit_mastery_previews_match_conditional_focus_bonuses(string mastery, string oneCharge, string twoCharge, string threeCharge)
    {
        var repository = new TestRepository();
        repository.Styles.Add(new() { CombatStyleId = CombatStyleIds.Conduit, Level = 9 });
        var focus = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.focus" };
        var service = CreateService(repository, new TestLoadouts([focus]));
        var preview = await service.PreviewAsync(Guid.Empty,
            new(CombatStyleIds.Conduit, null, [mastery], focus.Id, MasteredUpgradeId: mastery), default);
        Assert.Equal("80% of normal strength", preview.PreviewFacts.Single(x => x.Label == "0 Charge").Value);
        Assert.Equal(oneCharge, preview.PreviewFacts.Single(x => x.Label == "1 Charge").Value);
        Assert.Equal(twoCharge, preview.PreviewFacts.Single(x => x.Label == "2 Charge").Value);
        Assert.Equal(threeCharge, preview.PreviewFacts.Single(x => x.Label == "3 Charge").Value);
        Assert.Equal("1 starting Charge", preview.PreviewFacts.Single(x => x.Label == "Opening Technique").Value);
        if (mastery == CombatStyleIds.EmergencyChannel)
        {
            Assert.Equal("+5% flat increase to healing and Barrier on yourself", preview.PreviewFacts.Single(x => x.Label == "Emergency Channel").Value);
            Assert.Contains("any Health", preview.PreviewFacts.Single(x => x.Label == "Emergency Channel").Condition);
        }
        else
        {
            var label = mastery == CombatStyleIds.FullCircuit ? "Full Circuit" : "Partial Flow";
            var bonus = preview.PreviewFacts.Single(x => x.Label == label);
            Assert.Equal("+5% flat increase", bonus.Value);
            Assert.Contains(mastery == CombatStyleIds.FullCircuit ? "2 or more Charge" : "1 to 2 Charge", bonus.Condition);
        }
    }

    [Fact]
    public async Task Reward_initializes_only_its_captured_style_and_ignores_empty_or_unknown_snapshots()
    {
        var repository = new TestRepository();
        var service = CreateService(repository);
        await service.GrantCapturedCombatXpAsync(Guid.Empty, null, 100, default);
        await service.GrantCapturedCombatXpAsync(Guid.Empty, "unknown", 100, default);
        await service.GrantCapturedCombatXpAsync(Guid.Empty, CombatStyleIds.Bastion, 0, default);
        Assert.Empty(repository.Styles);

        await service.GrantCapturedCombatXpAsync(Guid.Empty, CombatStyleIds.Bastion, 150, default);
        var progress = Assert.Single(repository.Styles);
        Assert.Equal(CombatStyleIds.Bastion, progress.CombatStyleId);
        Assert.Equal(1, progress.Level);
        Assert.Equal(50, progress.CurrentXp);
        Assert.Null(repository.Selection);
    }

    [Fact]
    public async Task Invalid_or_blocked_selections_do_not_create_progress_or_change_global_style()
    {
        var repository = new TestRepository();
        var boundary = new TestBoundary();
        var service = CreateService(repository, boundary: boundary);
        Assert.False((await service.SelectAsync(Guid.Empty, new(CombatStyleIds.Bastion, CombatStyleIds.Rebuild, [], null), default)).Succeeded);
        Assert.Equal(0, boundary.Calls);
        boundary.Blocked = "Finish the committed battle first.";
        Assert.False((await service.SelectAsync(Guid.Empty, new(CombatStyleIds.Bastion, null, [], null), default)).Succeeded);
        Assert.Empty(repository.Styles);
        Assert.Null(repository.Selection);
    }

    [Fact]
    public async Task Legacy_counterweight_choices_read_as_reprisal_without_mutating_saved_rows_then_save_current_id()
    {
        var repository = new TestRepository();
        var progress = new CharacterCombatStyle
        {
            CombatStyleId = CombatStyleIds.Bastion, Level = 9,
            RefinementId = CombatStyleIds.Counterweight,
            UpgradeIds = [CombatStyleIds.MeasuredRecovery], MasteredUpgradeId = CombatStyleIds.MeasuredRecovery
        };
        var saved = new CharacterCombatStyleSelection
        {
            CombatStyleId = CombatStyleIds.Bastion, RefinementId = CombatStyleIds.Counterweight,
            UpgradeIds = [CombatStyleIds.MeasuredRecovery], MasteredUpgradeId = CombatStyleIds.MeasuredRecovery
        };
        repository.Add(progress); repository.Add(saved);
        var service = CreateService(repository);

        var overview = await service.GetOverviewAsync(Guid.Empty, default);
        var restored = await service.PreviewAsync(Guid.Empty, new(CombatStyleIds.Bastion, null, [], null, true), default);
        var captured = await service.ResolveAsync(Guid.Empty, EssenceCombatActivity.Dungeon, default);
        Assert.Equal(CombatStyleIds.Reprisal, overview.Selection.RefinementId);
        Assert.Equal(CombatStyleIds.Reprisal, overview.Styles.Single(x => x.Definition.Id == CombatStyleIds.Bastion).RefinementId);
        Assert.Equal(CombatStyleIds.Reprisal, overview.EffectiveStyle!.RefinementId);
        Assert.Equal(CombatStyleIds.Reprisal, restored.Selection.RefinementId);
        Assert.Equal(CombatStyleIds.MeasuredRecovery, restored.Selection.MasteredUpgradeId);
        Assert.Equal(CombatStyleIds.Reprisal, captured!.RefinementId);
        Assert.Equal(CombatStyleIds.Counterweight, progress.RefinementId);
        Assert.Equal(CombatStyleIds.Counterweight, saved.RefinementId);

        Assert.True((await service.SelectAsync(Guid.Empty,
            new(CombatStyleIds.Bastion, CombatStyleIds.Counterweight, [CombatStyleIds.MeasuredRecovery], null,
                MasteredUpgradeId: CombatStyleIds.MeasuredRecovery), default)).Succeeded);
        Assert.Equal(CombatStyleIds.Reprisal, saved.RefinementId);
        Assert.Equal(CombatStyleIds.Reprisal, progress.RefinementId);
    }

    [Fact]
    public async Task Reprisal_alias_keeps_refinement_unlock_and_preview_matches_absorption_rules()
    {
        var repository = new TestRepository();
        var progress = new CharacterCombatStyle { CombatStyleId = CombatStyleIds.Bastion, Level = 2 };
        repository.Add(progress);
        var service = CreateService(repository);
        var request = new CombatStyleSelectionRequest(CombatStyleIds.Bastion, CombatStyleIds.Counterweight, [], null);
        Assert.False((await service.SelectAsync(Guid.Empty, request, default)).Succeeded);
        Assert.Null(repository.Selection);
        progress.Level = 3;

        var preview = await service.PreviewAsync(Guid.Empty, request, default);

        Assert.Null(preview.ValidationIssue);
        Assert.Equal(CombatStyleIds.Reprisal, preview.Selection.RefinementId);
        Assert.Equal("25% of absorbed damage stored · cap 10% Max Health", preview.PreviewFacts.Single(x => x.Label == "Reprisal").Value);
        Assert.Equal("200 absorbed → 50 bonus damage", preview.PreviewFacts.Single(x => x.Label == "Reprisal example").Value);
        Assert.DoesNotContain(preview.PreviewFacts, x => x.Label == "Counterweight");
        Assert.Equal(.25, preview.EffectiveStyle!.Tuning.ReprisalAbsorbedDamageFraction);
        Assert.Equal(.10, preview.EffectiveStyle.Tuning.ReprisalMaxHealthCapFraction);
        Assert.Contains(preview.Styles[0].Definition.Refinements, x => x.Id == CombatStyleIds.Reprisal);
        Assert.DoesNotContain(preview.Styles[0].Definition.Refinements, x => x.Id == CombatStyleIds.Counterweight);
    }

    [Fact]
    public void Current_catalog_validates_reprisal_tuning_while_old_committed_counterweight_snapshot_stays_unchanged()
    {
        var catalog = LoadCatalog();
        Assert.Equal("combat-styles.v4", catalog.ContentVersion);
        foreach (var invalid in new double?[] { null, double.NaN, double.PositiveInfinity, -.01, 1.01 })
        {
            var bastion = catalog.Styles.Single(x => x.Id == CombatStyleIds.Bastion);
            foreach (var tuning in new[] {
                bastion.Tuning with { ReprisalAbsorbedDamageFraction = invalid },
                bastion.Tuning with { ReprisalMaxHealthCapFraction = invalid } })
                Assert.Throws<InvalidOperationException>(() => CombatStyleRules.ValidateCatalog(catalog with
                {
                    Styles = [bastion with { Tuning = tuning }, catalog.Styles.Single(x => x.Id == CombatStyleIds.Conduit)]
                }));
        }

        var snapshot = JsonSerializer.Deserialize<CombatStyleSnapshot>("""
            {"CombatStyleId":"bastion","Kind":1,"ContentVersion":"combat-styles.v2","Level":10,"CoreRank":5,"RefinementId":"counterweight"}
            """)!;
        var roundTrip = JsonSerializer.Deserialize<CombatStyleSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal(CombatStyleIds.Counterweight, roundTrip.RefinementId);
        Assert.Null(roundTrip.Tuning.ReprisalAbsorbedDamageFraction);
        Assert.Null(roundTrip.Tuning.ReprisalMaxHealthCapFraction);
        Assert.Equal(.20, roundTrip.Tuning.CounterweightBarrierThreshold);
        Assert.Equal(.10, roundTrip.Tuning.CounterweightBarrierCost);
    }

    internal static CombatStyleCatalog LoadCatalog()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "LL", "src", "API")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return new JsonCombatStyleCatalogProvider(Path.Combine(directory.FullName,
            "LL", "src", "API", "API.LL", "Data", "combat-styles", "combat-styles.v1.json")).Catalog;
    }

    private sealed class TestCatalog : ICombatStyleCatalogProvider { public CombatStyleCatalog Catalog { get; } = LoadCatalog() with { XpRequirements = Requirements }; }

    private static CombatStyleService CreateService(TestRepository repository, TestLoadouts? loadouts = null, TestBoundary? boundary = null) =>
        new(repository, new TestCatalog(), new TestDefinitions(), loadouts ?? new([]), new TestAbilities(), boundary ?? new());

    private sealed class TestLoadouts(IReadOnlyList<PlayerEssence> equipped) : IEssenceCombatLoadoutResolver
    {
        public List<EssenceCombatActivity> Activities { get; } = [];
        public Task<EssenceCombatLoadout> ResolveAsync(Guid id, CancellationToken ct) => ResolveAsync(id, EssenceCombatActivity.None, ct);
        public Task<EssenceCombatLoadout> ResolveAsync(Guid id, EssenceCombatActivity activity, CancellationToken ct)
        { Activities.Add(activity); return Task.FromResult(Resolve(id, equipped)); }
        public EssenceCombatLoadout Resolve(Guid id, IEnumerable<PlayerEssence> essences) => new(id, essences.ToArray(), [], new HashSet<string>());
    }

    private sealed class TestDefinitions : IEssenceDefinitionRepository
    {
        private static readonly EssenceDefinition Focus = new()
        {
            Id = "essence.focus", Name = "Focus", ActiveAbility = new()
            {
                Id = "ability.focus", Name = "Focus", Kind = AbilitySpecKind.Active, CooldownTicks = 10,
                Effects = [new() { Id = "heal", Operation = AbilityEffectOperation.Heal, Target = AbilityTargetSelector.Self, ScalingCoefficient = 1 }]
            }
        };
        public IReadOnlyList<EssenceDefinition> GetAll() => [Focus];
        public IReadOnlyList<AbilitySpec> GetAllAbilities() => [Focus.ActiveAbility];
        public EssenceDefinition? GetById(string id) => id == Focus.Id ? Focus : null;
        public AbilitySpec? GetAbilityById(string id) => id == Focus.ActiveAbility.Id ? Focus.ActiveAbility : null;
    }

    private sealed class TestAbilities : IAbilityCatalogProvider
    {
        public AbilityCatalog GetCatalog() => new(new TestDefinitions().GetAllAbilities(), [], [], new Dictionary<string, string>());
    }

    private sealed class TestBoundary : ICombatStyleMutationBoundary
    {
        public int Calls { get; private set; }
        public string? Blocked { get; set; }
        public Task<string?> PrepareMutationAsync(Guid id, CancellationToken ct) { Calls++; return Task.FromResult(Blocked); }
    }

    private sealed class TestRepository : ICombatStyleRepository
    {
        public List<CharacterCombatStyle> Styles { get; } = [];
        public int LoadCount { get; private set; }
        public CharacterCombatStyleSelection? Selection { get; private set; }
        public int SelectionAddCount { get; private set; }
        public Task<IReadOnlyList<CharacterCombatStyle>> GetOwnedAsync(Guid id, CancellationToken ct)
        {
            LoadCount++;
            return Task.FromResult<IReadOnlyList<CharacterCombatStyle>>(Styles);
        }
        public Task<CharacterCombatStyleSelection?> GetSelectionAsync(Guid id, CancellationToken ct) => Task.FromResult(Selection);
        public void Add(CharacterCombatStyle style) => Styles.Add(style);
        public void Add(CharacterCombatStyleSelection selection) { Selection = selection; SelectionAddCount++; }
    }
}
