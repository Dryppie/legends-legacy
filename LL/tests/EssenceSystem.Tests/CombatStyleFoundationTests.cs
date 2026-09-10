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
        var service = new CombatStyleService(new CombatStyleRepository(db), new TestCatalog(), null!, null!, null!);
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
            var bonus = snapshot.Kind switch
            {
                CombatStyleKind.Bastion => snapshot.BarrierMasteryBonus,
                CombatStyleKind.Conduit => snapshot.ChanneledMasteryBonus,
                CombatStyleKind.Reaper => snapshot.Tuning.Reaper!.Multiplier(level) - snapshot.Tuning.Reaper.BaseMultiplier,
                CombatStyleKind.Duelist => snapshot.Tuning.Duelist!.Multiplier(level) - snapshot.Tuning.Duelist.OpeningMultiplier,
                _ => throw new InvalidOperationException()
            };
            Assert.Equal(expected, bonus, 8);
            var json = JsonSerializer.Serialize(snapshot);
            Assert.DoesNotContain("\"CoreRank\"", json);
            Assert.DoesNotContain("\"BarrierMasteryBonus\"", json);
            Assert.DoesNotContain("\"ChanneledMasteryBonus\"", json);
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
            Styles = [valid.Styles[0] with { Upgrades = [] }, .. valid.Styles.Skip(1)] }));
    }

    [Fact]
    public void Current_catalog_requires_finite_nonnegative_per_mastery_level_tuning()
    {
        var catalog = LoadCatalog();
        var definition = catalog.Styles[0];
        foreach (var tuning in new[]
        {
            definition.Tuning with { BarrierPerMasteryLevel = null },
            definition.Tuning with { ChanneledPerMasteryLevel = null },
            definition.Tuning with { BarrierPerMasteryLevel = double.NaN },
            definition.Tuning with { ChanneledPerMasteryLevel = double.PositiveInfinity },
            definition.Tuning with { BarrierPerMasteryLevel = -.01 },
            definition.Tuning with { ChanneledPerMasteryLevel = -.01 }
        })
            Assert.Throws<InvalidOperationException>(() => CombatStyleRules.ValidateCatalog(catalog with
            {
                Styles = [definition with { Tuning = tuning }, .. catalog.Styles.Skip(1)]
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
        Assert.Null(snapshot.Tuning.ChanneledPerMasteryLevel);
        Assert.Equal(.075, snapshot.BarrierMasteryBonus, 8);
        Assert.Equal(.045, snapshot.ChanneledMasteryBonus, 8);

        var json = JsonSerializer.Serialize(snapshot);
        Assert.DoesNotContain("PerMasteryLevel", json);
        var restored = JsonSerializer.Deserialize<CombatStyleSnapshot>(json)!;
        Assert.Equal(snapshot.BarrierMasteryBonus, restored.BarrierMasteryBonus);
        Assert.Equal(snapshot.ChanneledMasteryBonus, restored.ChanneledMasteryBonus);
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
            Tuning = new() { BarrierPerMasteryLevel = .007, ChanneledPerMasteryLevel = .009,
                BarrierPerCoreRank = .50, ChanneledPerCoreRank = .50 }
        };
        Assert.Equal(.07, snapshot.BarrierMasteryBonus, 8);
        Assert.Equal(.09, snapshot.ChanneledMasteryBonus, 8);
        var disabled = snapshot with { Tuning = snapshot.Tuning with { BarrierPerMasteryLevel = 0, ChanneledPerMasteryLevel = 0 } };
        Assert.Equal(0, disabled.BarrierMasteryBonus);
        Assert.Equal(0, disabled.ChanneledMasteryBonus);
    }

    [Fact]
    public void Selection_requires_individual_milestones_and_distinct_compatible_upgrades()
    {
        var catalog = LoadCatalog();
        var bastion = catalog.Styles.Single(x => x.Id == "bastion");
        var owned = new CharacterCombatStyle { CombatStyleId = "bastion" };
        CombatStyleSelectionRequest select = new("bastion", "rebuild", [], null);
        Assert.NotNull(CombatStyleRules.ValidateSelection(null, bastion, select));
        Assert.NotNull(CombatStyleRules.ValidateSelection(owned, bastion, select));
        owned.Level = 3;
        Assert.Null(CombatStyleRules.ValidateSelection(owned, bastion, select));
        owned.Level = 10;
        Assert.NotNull(CombatStyleRules.ValidateSelection(owned, bastion,
            select with { UpgradeIds = ["prepared-wall", "prepared-wall"] }));
        Assert.NotNull(CombatStyleRules.ValidateSelection(owned, bastion,
            select with { UpgradeIds = ["full-circuit"] }));
        Assert.Null(CombatStyleRules.ValidateSelection(owned, bastion,
            select with { UpgradeIds = ["prepared-wall", "hold-the-breach"] }));
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
        Assert.Equal(valid, CombatStyleRules.ValidateSelection(owned, definition, selection) is null);
        var snapshot = CombatStyleRules.Snapshot(LoadCatalog(), definition, owned, selection, null);
        Assert.Equal(valid && mastery is not null, snapshot.HasMasteredUpgrade(CombatStyleIds.PreparedWall));
    }

    [Fact]
    public void Empty_style_cannot_hold_a_mastery_choice()
    {
        Assert.NotNull(CombatStyleRules.ValidateSelection(null, null,
            new(null, null, [], null, MasteredUpgradeId: CombatStyleIds.PreparedWall)));
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
                MasteredUpgradeId: CombatStyleIds.FullCircuit),
            new(Guid.NewGuid(), "essence.channeled", "Channeled", "ability.channeled", 10, true, ["heal"]));
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
                Styles = [valid.Styles[0], valid.Styles[1] with { MilestoneTuning = tuning }, valid.Styles[2]]
            }));
    }

    [Fact]
    public void Conduit_global_choices_are_valid_independently_of_battle_channeled_eligibility()
    {
        var conduit = LoadCatalog().Styles.Single(x => x.Id == "conduit");
        var owned = new CharacterCombatStyle { CombatStyleId = "conduit" };
        var channeledId = Guid.NewGuid();
        CombatStyleSelectionRequest selection = new("conduit", null, [], channeledId);
        ChanneledEssenceOption option = new(channeledId, "essence.test", "Test", "ability.test", 10, false, []);
        Assert.Null(CombatStyleRules.ValidateSelection(owned, conduit, selection));
        Assert.Null(CombatStyleRules.NormalizeSelection(selection).ChanneledPlayerEssenceId);
        Assert.NotNull(CombatStyleRules.ValidateChanneledEssence(null));
        Assert.NotNull(CombatStyleRules.ValidateChanneledEssence(option));
        Assert.Null(CombatStyleRules.ValidateChanneledEssence(option with { IsEligible = true }));
    }

    [Fact]
    public void Snapshot_copies_choices_and_keeps_level_bonus_and_tuning_after_progress_changes()
    {
        var catalog = LoadCatalog();
        var owned = new CharacterCombatStyle { CombatStyleId = "conduit", Level = 8 };
        var upgrades = new List<string> { "full-circuit" };
        var channeled = new ChanneledEssenceOption(Guid.NewGuid(), "essence.channeled", "Channeled", "ability.channeled", 10, true, ["heal"]);
        var snapshot = CombatStyleRules.Snapshot(catalog, catalog.Styles.Single(x => x.Id == "conduit"), owned,
            new("conduit", "deep-reservoir", upgrades, Guid.NewGuid()), channeled);
        owned.Level = 10;
        upgrades.Clear();
        Assert.Equal(8, snapshot.Level);
        Assert.Equal(.08, snapshot.ChanneledMasteryBonus, 8);
        Assert.Equal(4, snapshot.Tuning.ChargeCap);
        Assert.Equal(.6, snapshot.Tuning.ChanneledBaseMultiplier);
        Assert.True(snapshot.HasUpgrade("full-circuit"));
        Assert.Equal("essence.channeled", snapshot.ChanneledEssenceDefinitionId);
        Assert.Equal(channeled.PlayerEssenceId, snapshot.ChanneledPlayerEssenceId);
    }

    [Fact]
    public async Task Reward_uses_captured_style_and_loads_owned_rows_once_per_scope()
    {
        var repository = new TestRepository();
        repository.Styles.Add(new() { CombatStyleId = "bastion" });
        repository.Styles.Add(new() { CombatStyleId = "conduit" });
        var service = new CombatStyleService(repository, new TestCatalog(), null!, null!, null!);
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
        var legacyInput = JsonSerializer.Deserialize<CombatStyleSelectionDto>(
            $$"""{"combatStyleId":"conduit","focusPlayerEssenceId":"{{Guid.NewGuid()}}"}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var normalized = mapper.Map<CombatStyleSelectionRequest>(legacyInput);
        Assert.Equal(CombatStyleIds.Conduit, normalized.CombatStyleId);
        Assert.Null(normalized.ChanneledPlayerEssenceId);
    }

    [Fact]
    public async Task Overview_contract_keeps_numeric_previews_without_a_separate_channeled_selection()
    {
        var channeled = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.channeled" };
        var service = CreateService(new TestRepository(), new TestLoadouts([channeled]));
        var preview = await service.PreviewAsync(Guid.Empty, new(CombatStyleIds.Conduit, null, [], channeled.Id), default);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<CombatStyleMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();
        var dto = mapper.Map<CombatStyleOverviewDto>(preview);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.False(json.RootElement.TryGetProperty("focusOptions", out _));
        Assert.False(json.RootElement.GetProperty("selection").TryGetProperty("focusPlayerEssenceId", out _));
        Assert.All(json.RootElement.GetProperty("styles").EnumerateArray(),
            style =>
            {
                Assert.False(style.TryGetProperty("focusPlayerEssenceId", out _));
                Assert.False(style.GetProperty("definition").TryGetProperty("tradeoff", out _));
            });
        Assert.Equal("80% of normal strength", dto.PreviewFacts[0].Value);
        Assert.Null(dto.PreviewFacts[0].Condition);
        Assert.Equal(.8, preview.EffectiveStyle!.Tuning.ChanneledBaseMultiplier);
        var effective = json.RootElement.GetProperty("effectiveStyle");
        Assert.True(effective.TryGetProperty("channeledPlayerEssenceId", out _));
        Assert.True(effective.TryGetProperty("channeledEssenceDefinitionId", out _));
        Assert.False(effective.TryGetProperty("focusPlayerEssenceId", out _));
        var tuning = effective.GetProperty("tuning");
        Assert.Equal(.8, tuning.GetProperty("channeledBaseMultiplier").GetDouble());
        Assert.Equal(.2, tuning.GetProperty("channeledPerCharge").GetDouble());
        Assert.Equal(.01, tuning.GetProperty("channeledPerMasteryLevel").GetDouble());
        Assert.False(tuning.TryGetProperty("focusBaseMultiplier", out _));
        Assert.All(json.RootElement.GetProperty("styles").EnumerateArray(), style =>
        {
            var definition = style.GetProperty("definition");
            Assert.True(definition.GetProperty("tuning").TryGetProperty("channeledBaseMultiplier", out _));
            Assert.False(definition.GetProperty("tuning").TryGetProperty("focusBaseMultiplier", out _));
        });
    }

    [Fact]
    public async Task Initial_overview_and_preview_make_all_styles_available_without_creating_persistence_rows()
    {
        var repository = new TestRepository();
        var service = CreateService(repository);
        var overview = await service.GetOverviewAsync(Guid.Empty, default);
        Assert.Equal(new[] { CombatStyleIds.Bastion, CombatStyleIds.Conduit, CombatStyleIds.Reaper, CombatStyleIds.Duelist }, overview.Styles.Select(x => x.Definition.Id));
        Assert.All(overview.Styles, style => { Assert.Equal(0, style.Level); Assert.Equal(0, style.CurrentXp); Assert.Equal(Requirements[0], style.XpRequired); });
        Assert.Null(overview.Selection.CombatStyleId);

        var bastion = await service.PreviewAsync(Guid.Empty, new(CombatStyleIds.Bastion, null, [], null), default);
        Assert.Null(bastion.ValidationIssue);
        Assert.Equal(CombatStyleIds.Bastion, bastion.EffectiveStyle!.CombatStyleId);
        var conduit = await service.PreviewAsync(Guid.Empty, new(CombatStyleIds.Conduit, null, [], null), default);
        Assert.Null(conduit.ValidationIssue);
        Assert.Equal(CombatStyleIds.Conduit, conduit.EffectiveStyle!.CombatStyleId);
        Assert.Equal("80% of normal strength", conduit.PreviewFacts.Single(x => x.Label == "0 Charge").Value);
        var reaper = await service.PreviewAsync(Guid.Empty, new(CombatStyleIds.Reaper, null, [], null), default);
        Assert.Null(reaper.ValidationIssue);
        Assert.Equal(CombatStyleKind.Reaper, reaper.EffectiveStyle!.Kind);
        Assert.Equal("110 damage dealt now", reaper.PreviewFacts.Single(x => x.Label == "100 damage harvested").Value);
        Assert.DoesNotContain(reaper.PreviewFacts, x => x.Label.Contains("Charge"));
        Assert.Empty(repository.Styles);
        Assert.Null(repository.Selection);
        Assert.Equal(0, repository.SelectionAddCount);
    }

    [Theory]
    [InlineData(null, "120 damage dealt now")]
    [InlineData(CombatStyleIds.SoulSiphon, "120 Health restored")]
    [InlineData(CombatStyleIds.LastRites, "120 damage dealt now")]
    [InlineData(CombatStyleIds.DeathSentence, "130 Magical Damage after 15 seconds")]
    public async Task Reaper_preview_and_dto_use_current_form_bonus_and_opening(string? refinement, string expected)
    {
        var repository = new TestRepository();
        repository.Styles.Add(new() { CombatStyleId = CombatStyleIds.Reaper, Level = 10 });
        var preview = await CreateService(repository).PreviewAsync(Guid.Empty,
            new(CombatStyleIds.Reaper, refinement, [CombatStyleIds.ClosingHand], null), default);
        Assert.Null(preview.ValidationIssue);
        Assert.Equal(expected, preview.PreviewFacts.Single(x => x.Label == "100 damage harvested").Value);
        Assert.Equal("Grave Seed: 5 Poison stacks", preview.PreviewFacts.Single(x => x.Label == "Opening Technique").Value);
        Assert.Equal("Your opponent must be at or below 35% Health.",
            preview.PreviewFacts.Single(x => x.Label == "Closing Hand").Condition);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<CombatStyleMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();
        var dto = mapper.Map<CombatStyleOverviewDto>(preview);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var tuning = json.RootElement.GetProperty("effectiveStyle").GetProperty("tuning").GetProperty("reaper");
        Assert.Equal(.1, tuning.GetProperty("deathSentenceBonus").GetDouble());
        Assert.Equal(5, tuning.GetProperty("openingPoisonStacks").GetInt32());
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
    public async Task Conduit_derives_channeled_from_each_battle_loadout_and_supplied_snapshot_order()
    {
        var repository = new TestRepository();
        var channeled = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.channeled" };
        var loadouts = new TestLoadouts([channeled]);
        foreach (var activity in Enum.GetValues<EssenceCombatActivity>())
            loadouts.ByActivity[activity] = [new() { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.channeled" }, channeled];
        var service = CreateService(repository, loadouts);
        Assert.True((await service.SelectAsync(Guid.Empty, new(CombatStyleIds.Conduit, null, [], channeled.Id), default)).Succeeded);
        Assert.Empty(loadouts.Activities);

        foreach (var activity in Enum.GetValues<EssenceCombatActivity>())
        {
            var snapshot = await service.ResolveAsync(Guid.Empty, activity, default);
            Assert.Equal(CombatStyleIds.Conduit, snapshot!.CombatStyleId);
            Assert.Equal(loadouts.ByActivity[activity][0].Id, snapshot.ChanneledPlayerEssenceId);
        }
        Assert.Equal(Enum.GetValues<EssenceCombatActivity>(), loadouts.Activities.TakeLast(Enum.GetValues<EssenceCombatActivity>().Length));
        var supplied = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.channeled" };
        var suppliedSnapshot = await service.ResolveAsync(Guid.Empty, EssenceCombatActivity.Dungeon, default, [supplied, channeled]);
        Assert.Equal(supplied.Id, suppliedSnapshot!.ChanneledPlayerEssenceId);
        Assert.Equal(Enum.GetValues<EssenceCombatActivity>().Length, loadouts.Activities.Count);
        Assert.Null(repository.Selection!.ChanneledPlayerEssenceId);
    }

    [Fact]
    public async Task Global_conduit_choices_preview_and_save_without_loading_essences_and_clear_legacy_channeled_only_on_save()
    {
        var repository = new TestRepository();
        var legacyChanneled = Guid.NewGuid();
        var progress = new CharacterCombatStyle
        {
            CombatStyleId = CombatStyleIds.Conduit, Level = 9, RefinementId = CombatStyleIds.DeepReservoir,
            UpgradeIds = [CombatStyleIds.FullCircuit], MasteredUpgradeId = CombatStyleIds.FullCircuit,
            ChanneledPlayerEssenceId = legacyChanneled
        };
        repository.Styles.Add(progress);
        repository.Add(new CharacterCombatStyleSelection
        {
            CombatStyleId = progress.CombatStyleId, RefinementId = progress.RefinementId,
            UpgradeIds = progress.UpgradeIds, MasteredUpgradeId = progress.MasteredUpgradeId,
            ChanneledPlayerEssenceId = legacyChanneled
        });
        var loadouts = new TestLoadouts([]) { ThrowOnResolve = true };
        var service = CreateService(repository, loadouts);

        var overview = await service.GetOverviewAsync(Guid.Empty, default);
        var remembered = await service.PreviewAsync(Guid.Empty,
            new(CombatStyleIds.Conduit, null, [], Guid.NewGuid(), RestoreRememberedChoices: true), default);
        Assert.Null(overview.ValidationIssue);
        Assert.Null(remembered.ValidationIssue);
        Assert.Null(overview.Selection.ChanneledPlayerEssenceId);
        Assert.Null(remembered.Selection.ChanneledPlayerEssenceId);
        Assert.Null(remembered.EffectiveStyle!.ChanneledPlayerEssenceId);
        Assert.Equal(CombatStyleIds.DeepReservoir, remembered.Selection.RefinementId);
        Assert.Equal(CombatStyleIds.FullCircuit, remembered.Selection.MasteredUpgradeId);
        Assert.Equal("60% of normal strength", remembered.PreviewFacts.Single(x => x.Label == "0 Charge").Value);
        Assert.Equal("174% of normal strength", remembered.PreviewFacts.Single(x => x.Label == "4 Charge").Value);
        Assert.Equal(legacyChanneled, repository.Selection!.ChanneledPlayerEssenceId);
        Assert.Equal(legacyChanneled, progress.ChanneledPlayerEssenceId);

        var saved = await service.SelectAsync(Guid.Empty,
            new(CombatStyleIds.Conduit, CombatStyleIds.Relay, [CombatStyleIds.PartialFlow], Guid.NewGuid(),
                MasteredUpgradeId: CombatStyleIds.PartialFlow), default);
        Assert.True(saved.Succeeded);
        Assert.Equal(CombatStyleIds.Relay, repository.Selection.RefinementId);
        Assert.Equal(CombatStyleIds.PartialFlow, progress.MasteredUpgradeId);
        Assert.Null(repository.Selection.ChanneledPlayerEssenceId);
        Assert.Null(progress.ChanneledPlayerEssenceId);
        Assert.Empty(loadouts.Activities);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("essence.support")]
    [InlineData("essence.missing")]
    public async Task Conduit_is_inactive_for_empty_or_ineligible_loadouts_in_every_activity(string? firstDefinition)
    {
        var repository = new TestRepository();
        var eligible = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.channeled" };
        var loadouts = new TestLoadouts([eligible]);
        var service = CreateService(repository, loadouts);
        Assert.True((await service.SelectAsync(Guid.Empty, new(CombatStyleIds.Conduit, null, [], eligible.Id), default)).Succeeded);
        PlayerEssence[] equipped = firstDefinition is null ? [] :
            [new() { Id = Guid.NewGuid(), EssenceDefinitionId = firstDefinition }, eligible];

        foreach (var activity in Enum.GetValues<EssenceCombatActivity>())
        {
            loadouts.ByActivity[activity] = equipped;
            Assert.Null(await service.ResolveAsync(Guid.Empty, activity, default));

            // Supplied snapshot Essences are authoritative, even when the live loadout is eligible.
            loadouts.ByActivity[activity] = [eligible];
            loadouts.ThrowOnResolve = true;
            Assert.Null(await service.ResolveAsync(Guid.Empty, activity, default, equipped));
            loadouts.ThrowOnResolve = false;

            // Fixing the loadout reactivates Conduit without selecting the style again.
            var active = await service.ResolveAsync(Guid.Empty, activity, default);
            Assert.Equal(CombatStyleIds.Conduit, active!.CombatStyleId);
            Assert.Equal(eligible.Id, active.ChanneledPlayerEssenceId);
        }

        Assert.Equal(CombatStyleIds.Conduit, repository.Selection!.CombatStyleId);
        Assert.Null(repository.Selection!.ChanneledPlayerEssenceId);
    }

    [Fact]
    public async Task Reordering_a_loadout_changes_only_new_captures_and_historical_channeled_ids_round_trip_unchanged()
    {
        var first = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.channeled" };
        var second = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.channeled" };
        var loadouts = new TestLoadouts([first, second]);
        var service = CreateService(new TestRepository(), loadouts);
        await service.SelectAsync(Guid.Empty, new(CombatStyleIds.Conduit, null, [], second.Id), default);
        var captured = (await service.ResolveAsync(Guid.Empty, EssenceCombatActivity.Dungeon, default))!;
        var historical = captured with { ContentVersion = "combat-styles.v4", ChanneledPlayerEssenceId = second.Id };
        var committedJson = JsonSerializer.Serialize(historical);

        loadouts.ByActivity[EssenceCombatActivity.Dungeon] = [second, first];
        var next = await service.ResolveAsync(Guid.Empty, EssenceCombatActivity.Dungeon, default);

        Assert.Equal(first.Id, captured.ChanneledPlayerEssenceId);
        Assert.Equal(second.Id, next!.ChanneledPlayerEssenceId);
        var restored = JsonSerializer.Deserialize<CombatStyleSnapshot>(committedJson)!;
        Assert.Equal("combat-styles.v4", restored.ContentVersion);
        Assert.Equal(second.Id, restored.ChanneledPlayerEssenceId);
        Assert.Equal("essence.channeled", restored.ChanneledEssenceDefinitionId);
        Assert.Equal(HarnessJson.Hash(historical), HarnessJson.Hash(restored));
    }

    [Fact]
    public async Task Switching_styles_restores_individual_choices_and_keeps_captured_xp_with_its_style()
    {
        var repository = new TestRepository();
        repository.Styles.Add(new() { CombatStyleId = CombatStyleIds.Bastion, Level = 5, CurrentXp = 123,
            RefinementId = CombatStyleIds.Rebuild, UpgradeIds = [CombatStyleIds.PreparedWall] });
        var channeled = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.channeled" };
        var service = CreateService(repository, new TestLoadouts([channeled]));
        await service.SelectAsync(Guid.Empty, new(CombatStyleIds.Conduit, null, [], channeled.Id), default);
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
        var channeled = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.channeled" };
        var service = CreateService(repository, new TestLoadouts([channeled]));
        var bastion = new CombatStyleSelectionRequest(CombatStyleIds.Bastion, CombatStyleIds.Shelter,
            [CombatStyleIds.PreparedWall, CombatStyleIds.MeasuredRecovery], null, MasteredUpgradeId: CombatStyleIds.MeasuredRecovery);
        Assert.True((await service.SelectAsync(Guid.Empty, bastion, default)).Succeeded);
        Assert.True((await service.SelectAsync(Guid.Empty,
            new(CombatStyleIds.Conduit, null, [CombatStyleIds.FullCircuit], channeled.Id, MasteredUpgradeId: CombatStyleIds.FullCircuit), default)).Succeeded);
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
        var channeled = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.channeled" };
        var service = CreateService(repository, new TestLoadouts([channeled]));
        var conduit = styleId == CombatStyleIds.Conduit;
        var preview = await service.PreviewAsync(Guid.Empty,
            new(styleId, null, [], conduit ? channeled.Id : null), default);

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
    public async Task Conduit_mastery_previews_match_conditional_channeled_bonuses(string mastery, string oneCharge, string twoCharge, string threeCharge)
    {
        var repository = new TestRepository();
        repository.Styles.Add(new() { CombatStyleId = CombatStyleIds.Conduit, Level = 9 });
        var channeled = new PlayerEssence { Id = Guid.NewGuid(), EssenceDefinitionId = "essence.channeled" };
        var service = CreateService(repository, new TestLoadouts([channeled]));
        var preview = await service.PreviewAsync(Guid.Empty,
            new(CombatStyleIds.Conduit, null, [mastery], channeled.Id, MasteredUpgradeId: mastery), default);
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
        Assert.Equal("combat-styles.v9", catalog.ContentVersion);
        foreach (var invalid in new double?[] { null, double.NaN, double.PositiveInfinity, -.01, 1.01 })
        {
            var bastion = catalog.Styles.Single(x => x.Id == CombatStyleIds.Bastion);
            foreach (var tuning in new[] {
                bastion.Tuning with { ReprisalAbsorbedDamageFraction = invalid },
                bastion.Tuning with { ReprisalMaxHealthCapFraction = invalid } })
                Assert.Throws<InvalidOperationException>(() => CombatStyleRules.ValidateCatalog(catalog with
                {
                    Styles = [bastion with { Tuning = tuning }, .. catalog.Styles.Where(x => x.Id != CombatStyleIds.Bastion)]
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

    [Theory]
    [InlineData(null, 3, "155% of normal damage")]
    [InlineData(CombatStyleIds.Flurry, 3, "140% of normal damage")]
    [InlineData(CombatStyleIds.PatientBlade, 5, "190% of normal damage")]
    [InlineData(CombatStyleIds.GuardedThrust, 3, "135% of normal damage")]
    public async Task Duelist_is_available_and_previews_and_serializes_its_selected_form(string? form, int read, string damage)
    {
        var repository = new TestRepository();
        var service = CreateService(repository);
        var initial = await service.PreviewAsync(Guid.Empty, new(CombatStyleIds.Duelist, null, [], null), default);
        Assert.Null(initial.ValidationIssue);
        Assert.Equal("145% of normal damage", initial.PreviewFacts.Single(x => x.Label == "Opening damage").Value);
        repository.Styles.Add(new() { CombatStyleId = CombatStyleIds.Duelist, Level = 10 });
        var selection = new CombatStyleSelectionRequest(CombatStyleIds.Duelist, form,
            [CombatStyleIds.MeasuredStrikes, CombatStyleIds.FinishingTouch], null, MasteredUpgradeId: CombatStyleIds.MeasuredStrikes);
        Assert.True((await service.SelectAsync(Guid.Empty, selection, default)).Succeeded);
        var preview = await service.PreviewAsync(Guid.Empty, selection, default);
        Assert.Null(preview.ValidationIssue);
        Assert.Equal(damage, preview.PreviewFacts.Single(x => x.Label == "Opening damage").Value);
        Assert.Equal(read.ToString(), preview.PreviewFacts.Single(x => x.Label == "Read needed").Value);
        Assert.Contains(preview.PreviewFacts, x => x.Label == "Opening Technique");
        if (form == CombatStyleIds.GuardedThrust)
            Assert.Equal("Guard(1)", preview.PreviewFacts.Single(x => x.Label == "Guarded Thrust").Value);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<CombatStyleMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();
        var dto = mapper.Map<CombatStyleOverviewDto>(preview);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.Equal(read, json.RootElement.GetProperty("effectiveStyle").GetProperty("tuning")
            .GetProperty("duelist").GetProperty("readRequired").GetInt32());
        var resolved = await service.ResolveAsync(Guid.Empty, EssenceCombatActivity.IdleCombat, default);
        Assert.Equal(form, resolved!.RefinementId);
        Assert.Equal(read, resolved.Tuning.Duelist!.ReadRequired);
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
        new(repository, new TestCatalog(), loadouts ?? new([]), new ChanneledEssenceResolver(new TestDefinitions(), new TestAbilities()), boundary ?? new());

    private sealed class TestLoadouts(IReadOnlyList<PlayerEssence> equipped) : IEssenceCombatLoadoutResolver
    {
        public List<EssenceCombatActivity> Activities { get; } = [];
        public Dictionary<EssenceCombatActivity, IReadOnlyList<PlayerEssence>> ByActivity { get; } = [];
        public bool ThrowOnResolve { get; set; }
        public Task<EssenceCombatLoadout> ResolveAsync(Guid id, CancellationToken ct) => ResolveAsync(id, EssenceCombatActivity.None, ct);
        public Task<EssenceCombatLoadout> ResolveAsync(Guid id, EssenceCombatActivity activity, CancellationToken ct)
        {
            if (ThrowOnResolve) throw new InvalidOperationException("This operation must not load Essence loadouts.");
            Activities.Add(activity);
            return Task.FromResult(Resolve(id, ByActivity.GetValueOrDefault(activity, equipped)));
        }
        public EssenceCombatLoadout Resolve(Guid id, IEnumerable<PlayerEssence> essences) => new(id, essences.ToArray(), [], new HashSet<string>());
    }

    private sealed class TestDefinitions : IEssenceDefinitionRepository
    {
        private static readonly EssenceDefinition Channeled = new()
        {
            Id = "essence.channeled", Name = "Channeled", ActiveAbility = new()
            {
                Id = "ability.channeled", Name = "Channeled", Kind = AbilitySpecKind.Active, CooldownTicks = 10,
                Effects = [new() { Id = "heal", Operation = AbilityEffectOperation.Heal, Target = AbilityTargetSelector.Self, ScalingCoefficient = 1 }]
            }
        };
        private static readonly EssenceDefinition Support = new()
        {
            Id = "essence.support", Name = "Support", ActiveAbility = new()
            {
                Id = "ability.support", Name = "Support", Kind = AbilitySpecKind.Active, CooldownTicks = 10,
                Effects = [new() { Id = "threat", Operation = AbilityEffectOperation.ModifyThreat, Target = AbilityTargetSelector.Self, BaseValue = 10 }]
            }
        };
        public IReadOnlyList<EssenceDefinition> GetAll() => [Channeled, Support];
        public IReadOnlyList<AbilitySpec> GetAllAbilities() => GetAll().Select(x => x.ActiveAbility).ToArray();
        public EssenceDefinition? GetById(string id) => GetAll().FirstOrDefault(x => x.Id == id);
        public AbilitySpec? GetAbilityById(string id) => GetAllAbilities().FirstOrDefault(x => x.Id == id);
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
