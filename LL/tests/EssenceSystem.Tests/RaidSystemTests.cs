using System.IO.Compression;
using System.Text.Json;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Raids;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;
using Application.UseCases.Raids.Dtos;
using RaidUpdated = Application.WebSockets.Contracts.RaidUpdated;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Raids;
using Domain.Models.Snapshots;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Services.LL.Raids;
using Services.LL.Interfaces;

namespace EssenceSystem.Tests;

public sealed class RaidSystemTests
{
    [Fact]
    public void Raid_playback_bundle_preserves_entity_and_ability_threat()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var combatant = new SimpleCombatEntity(
            "friendly-1",
            "Friendly One",
            string.Empty,
            maxHealth: 100,
            barrier: 0);
        var stats = new EntityStats(
            combatant.Id,
            combatant.Name,
            [new AbilityStats("Basic Attack", Uses: 2, TotalThreat: 34)],
            ThreatGenerated: 34);
        var result = new CombatResult
        {
            Duration = 10,
            Outcome = BattleOutcome.Victory,
            PlayerTeam = [combatant],
            EntityStats = [stats]
        };
        var capture = new RaidLanePlaybackCapture(
            RaidLane.Rearguard,
            result,
            [new CombatCheckpoint(
                Sequence: 1,
                Tick: 10,
                Friendly: [combatant],
                Hostile: [],
                EntityStats: [stats],
                Events: [],
                IsFinal: true)]);

        var playback = new RaidPlaybackBundleBuilder(jsonOptions, TimeProvider.System)
            .Build(Guid.NewGuid(), capture);
        using var compressed = new MemoryStream(playback.Artifact.BundleBytes);
        using var decompressed = new BrotliStream(compressed, CompressionMode.Decompress);
        var bundle = JsonSerializer.Deserialize<RaidPlaybackBundleDto>(
            decompressed,
            jsonOptions);

        Assert.NotNull(bundle);
        Assert.Equal(RaidPlayback.CompactBundleSchemaVersion, bundle.SchemaVersion);
        var frame = Assert.Single(bundle.Frames);
        Assert.Equal(34, Assert.Single(frame.EntityTotals).ThreatGenerated);
        Assert.Equal(34, Assert.Single(frame.AbilityTotals).TotalThreat);
    }

    [Fact]
    public void Raid_assignments_have_three_parties_and_exclude_the_final_assault()
    {
        Assert.Equal(
            [RaidLane.Rearguard, RaidLane.Vanguard, RaidLane.MainGuard],
            RaidParties.All);
        Assert.All(RaidParties.All, party => Assert.True(RaidParties.IsAssignable(party)));
        Assert.False(RaidParties.IsAssignable(RaidLane.FinalAssault));
        Assert.Equal(1, RaidParties.FormationNumber(RaidLane.Rearguard));
        Assert.Equal(2, RaidParties.FormationNumber(RaidLane.Vanguard));
        Assert.Equal(3, RaidParties.FormationNumber(RaidLane.MainGuard));
        Assert.Equal(1, RaidParties.EncounterOrder(RaidLane.Rearguard));
        Assert.Equal(2, RaidParties.EncounterOrder(RaidLane.Vanguard));
        Assert.Equal(3, RaidParties.EncounterOrder(RaidLane.MainGuard));
        Assert.Equal(4, RaidParties.EncounterOrder(RaidLane.FinalAssault));
    }

    [Fact]
    public void Raid_plus_difficulty_scales_from_regular_without_changing_roster_or_timers()
    {
        var regular = new RaidBossTierDefinition
        {
            Tier = 7,
            LaneSlots = 3,
            MinimumRoster = 6,
            SignupWindowHours = 24,
            RecommendedWingPower = new RaidRecommendedWingPowerDefinition
            {
                Rearguard = 100,
                Vanguard = 200,
                MainGuard = 300
            },
            TickBudget = new RaidTickBudgetDefinition
            {
                Rearguard = 1000,
                Vanguard = 2000,
                MainGuard = 3000,
                FinalAssault = 4000
            },
            Boss = new RaidBossCombatDefinition
            {
                Scaling = new RaidAttributeScalingDefinition
                {
                    Health = 100,
                    Offense = 100,
                    Defense = 100,
                    Resistance = 100,
                    Penetration = 100,
                    Regeneration = 100
                },
                OvertimeStartsAtTick = 2000,
                OvertimePowerIncreasePercent = 6
            },
            Rearguard = new RaidRearguardDefinition
            {
                Adds =
                [
                    new RaidCreatureGroupEntry
                    {
                        Count = 2,
                        Scaling = new RaidAttributeScalingDefinition()
                    }
                ]
            },
            Vanguard = new RaidVanguardDefinition
            {
                GuardianScaling = new RaidAttributeScalingDefinition(),
                Escorts =
                [
                    new RaidCreatureGroupEntry
                    {
                        Count = 1,
                        Scaling = new RaidAttributeScalingDefinition()
                    }
                ]
            },
            MainGuard = new RaidMainGuardDefinition
            {
                ProjectionScaling = new RaidAttributeScalingDefinition(),
                SurvivalThresholdsPercent = [33m, 67m, 100m]
            },
            Rewards = new RaidRewardDefinition
            {
                SlainTrophies = 100,
                BrokenTrophies = 80,
                WoundedTrophies = 60,
                RepelledTrophies = 40,
                GuaranteedItems = [new RaidPendingItem("raid-material", 20)]
            }
        };
        var boss = new RaidBossDefinition { Id = "raid-boss.plus-test", Tiers = [regular] };

        var plusThree = RaidPlusDifficulty.Create(boss, 3);

        Assert.Equal(3, plusThree.Tier);
        Assert.Equal((3, 6, 24), (plusThree.LaneSlots, plusThree.MinimumRoster, plusThree.SignupWindowHours));
        Assert.Equal((1000, 2000, 3000, 4000), (
            plusThree.TickBudget.Rearguard,
            plusThree.TickBudget.Vanguard,
            plusThree.TickBudget.MainGuard,
            plusThree.TickBudget.FinalAssault));
        Assert.Equal(128, plusThree.RecommendedWingPower.Rearguard);
        Assert.Equal(133.1f, plusThree.Boss.Scaling.Health, 1);
        Assert.Equal(124.2f, plusThree.Boss.Scaling.Offense, 1);
        Assert.Equal(113.5f, plusThree.Boss.Scaling.Defense, 1);
        Assert.Equal(109f, plusThree.Boss.Scaling.Penetration, 1);
        Assert.Equal(115f, plusThree.Boss.Scaling.Regeneration, 1);
        Assert.Equal(124, plusThree.Rewards.SlainTrophies);
        Assert.Equal(23, Assert.Single(plusThree.Rewards.GuaranteedItems).Quantity);
        Assert.Equal(3, Assert.Single(plusThree.Rearguard.Adds).Count);
        Assert.Equal(1850, plusThree.Boss.OvertimeStartsAtTick);
        Assert.Equal(7, plusThree.Boss.OvertimePowerIncreasePercent);
        Assert.Equal([35m, 69m, 100m], plusThree.MainGuard.SurvivalThresholdsPercent);
        Assert.Equal("Regular", RaidPlusDifficulty.Label(0));
        Assert.Equal("+12", RaidPlusDifficulty.Label(12));
    }

    [Fact]
    public void Raid_boss_and_vendor_content_is_complete_and_references_existing_content()
    {
        var apiRoot = TestContentPaths.FindApiRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" })
            .Build();
        var provider = new JsonRaidBossDefinitionProvider(
            configuration,
            apiRoot,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var bosses = provider.GetAll();
        Assert.Equal(2, bosses.Count);
        var hive = Assert.Single(bosses, x => x.Id == "raid-boss.hives-abyss");
        Assert.Equal("The Hive's Abyss", hive.Name);
        Assert.Equal([1, 2, 3], hive.Tiers.Select(x => x.Tier));
        Assert.All(hive.Tiers, tier =>
        {
            var antKing = Assert.Single(tier.Boss.Variants);
            Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000083"), antKing.CreatureId);
            Assert.Equal(8m, antKing.SpawnChancePercent);
        });

        var sanguine = Assert.Single(bosses, x => x.Id == "raid-boss.sanguine-horror");
        Assert.Equal(2, sanguine.Region);
        Assert.Equal([2, 3], sanguine.Tiers.Select(x => x.Tier));
        Assert.All(sanguine.Tiers, tier =>
        {
            var bloodthorn = Assert.Single(tier.Vanguard.Escorts);
            Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000084"), bloodthorn.CreatureId);
            Assert.Equal(22m, bloodthorn.SpawnChancePercent);
        });

        using var creatureDocument = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(apiRoot, "Data", "world", "creatures.json")));
        var creatures = creatureDocument.RootElement.GetProperty("creatures")
            .EnumerateArray()
            .ToArray();
        var creatureIds = creatures.Select(x => x.GetProperty("id").GetGuid()).ToHashSet();
        var creatureNames = creatures.Select(x => x.GetProperty("name").GetString()).ToHashSet();
        Assert.Subset(
            creatureNames!,
            new HashSet<string?>
            {
                "Ant Worker",
                "Fire Ant",
                "Queen's Guard Ant",
                "Ant Queen",
                "Ant King",
                "Bloodthorn Vine",
                "Wendigo",
                "Corpse Golem"
            });
        var tiers = bosses.SelectMany(x => x.Tiers).ToArray();
        Assert.All(tiers, tier =>
        {
            Assert.Equal(10, tier.Rearguard.WaveCount);
            Assert.Equal(600, tier.TickBudget.MainGuard);
            Assert.Equal([33m, 67m, 100m], tier.MainGuard.SurvivalThresholdsPercent);
            Assert.True(tier.TickBudget.FinalAssault > 0);
            Assert.True(tier.Boss.MaxGuardianBreakPercent > 0);
            Assert.True(tier.Boss.MaxSignaturePowerReductionPercent > 0);
            Assert.True(tier.Boss.MaxSignatureCooldownDelayPercent > 0);
        });
        var referencedCreatures = tiers.SelectMany(tier => tier.Rearguard.Adds
            .Concat(tier.Vanguard.Escorts)
            .Select(x => x.CreatureId)
            .Append(tier.Boss.CreatureId)
            .Append(tier.Vanguard.GuardianCreatureId)
            .Append(tier.MainGuard.ProjectionCreatureId)
            .Concat(tier.Boss.Variants.Select(x => x.CreatureId)));
        Assert.All(referencedCreatures, id => Assert.Contains(id, creatureIds));

        using var itemDocument = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(apiRoot, "Data", "items", "items.json")));
        var itemIds = itemDocument.RootElement.EnumerateArray()
            .Select(x => x.GetProperty("id").GetString())
            .Where(x => x is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.All(tiers, tier =>
        {
            Assert.All(tier.Rewards.GuaranteedItems, reward => Assert.Contains(reward.ItemId, itemIds!));
            Assert.DoesNotContain(
                tier.Rewards.GuaranteedItems,
                reward => reward.ItemId.Contains("raid_seal", StringComparison.OrdinalIgnoreCase));
        });

        Assert.DoesNotContain(itemIds!, itemId =>
            itemId!.Contains("raid_seal", StringComparison.OrdinalIgnoreCase));

        var vendor = new JsonRaidTrophyVendorCatalog(
            configuration,
            apiRoot,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var vendorItems = bosses.SelectMany(boss => vendor.GetForBoss(boss.Id)).ToArray();
        Assert.Equal(4, vendorItems.Length);
        Assert.All(vendorItems, item =>
        {
            Assert.Contains(item.RaidBossId, bosses.Select(x => x.Id));
            Assert.Contains(item.RewardItemId, itemIds!);
        });

        using var blueprintDocument = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(apiRoot, "Data", "crafting", "blueprints.json")));
        var blueprintIds = blueprintDocument.RootElement.EnumerateArray()
            .Select(x => x.GetProperty("id").GetString())
            .Where(x => x is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("blueprint_raidforged", blueprintIds!);
        Assert.Contains("blueprint_gravebound", blueprintIds!);
    }

    [Fact]
    public void Raid_scaling_uses_explicit_multiplicative_modifiers()
    {
        var combatant = new CombatEntity(new Creature
        {
            Id = Guid.NewGuid(),
            Name = "Raid target",
            Level = 25
        });

        RaidCombatScaling.Apply(combatant, new RaidAttributeScalingDefinition
        {
            Health = 2f,
            Offense = 1.5f,
            Defense = 0.75f,
            Resistance = 1.25f,
            Penetration = 3f,
            Regeneration = 4f
        });

        AssertModifier(combatant, AttributeType.MaxHealth, 100f);
        AssertModifier(combatant, AttributeType.Power, 50f);
        AssertModifier(combatant, AttributeType.Armor, -25f);
        AssertModifier(combatant, AttributeType.Resistance, 25f);
        AssertModifier(combatant, AttributeType.ArmorPenetration, 200f);
        AssertModifier(combatant, AttributeType.MagicPenetration, 200f);
        AssertModifier(combatant, AttributeType.HealthRegeneration, 300f);
    }

    [Fact]
    public void Raid_persistence_model_enforces_roster_and_weekly_reward_invariants()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        using var db = new LLDbContext(options);

        var run = db.Model.FindEntityType(typeof(RaidRun));
        Assert.NotNull(run);
        Assert.Null(run.FindProperty("RaidSealOwnerCharacterId"));
        Assert.Null(run.FindProperty("RaidSealRefunded"));
        Assert.Contains(run.GetIndexes(), index =>
            PropertyNames(index).SequenceEqual(["Status", "PlaybackEndsAt"]));

        var character = db.Model.FindEntityType(typeof(Domain.Models.Entities.Characters.Character));
        Assert.NotNull(character);
        Assert.Equal(0L, character.FindProperty("RaidTrophies")!.GetDefaultValue());

        var signup = db.Model.FindEntityType(typeof(RaidSignup));
        Assert.NotNull(signup);
        Assert.Contains(signup.GetIndexes(), index =>
            index.IsUnique && PropertyNames(index).SequenceEqual(["RaidRunId", "CharacterId"]));
        Assert.Contains(signup.GetIndexes(), index =>
            index.IsUnique && PropertyNames(index).SequenceEqual(["RaidRunId", "AccountId"]));
        Assert.Contains(signup.GetIndexes(), index =>
            index.IsUnique && PropertyNames(index).SequenceEqual(["RaidRunId", "Lane", "WingSlotIndex"]));

        var claim = db.Model.FindEntityType(typeof(RaidRewardClaim));
        Assert.NotNull(claim);
        var weeklyRewardIndex = Assert.Single(claim.GetIndexes(), index =>
            PropertyNames(index).SequenceEqual(["RaidBossId", "CharacterId", "WeekKey"]));
        Assert.False(weeklyRewardIndex.IsUnique);
        Assert.NotNull(claim.FindProperty(nameof(RaidRewardClaim.Kind)));

        var participantResult = db.Model.FindEntityType(typeof(RaidParticipantResult));
        Assert.NotNull(participantResult);
        Assert.Null(participantResult.FindProperty("PayoutMultiplier"));

        var playback = db.Model.FindEntityType(typeof(RaidPlayback));
        Assert.NotNull(playback);
        Assert.Contains(playback.GetIndexes(), index =>
            index.IsUnique && PropertyNames(index).SequenceEqual(["RaidRunId", "Lane"]));

        var purchase = db.Model.FindEntityType(typeof(RaidTrophyPurchase));
        Assert.NotNull(purchase);
        Assert.Contains(purchase.GetIndexes(), index =>
            PropertyNames(index).SequenceEqual(["CharacterId", "VendorItemId", "WeekKey"]));

    }

    [Fact]
    public void Weekly_reward_upgrades_pay_only_the_unearned_outcome_difference()
    {
        var rewards = new RaidRewardDefinition
        {
            SlainTrophies = 100,
            BrokenTrophies = 65,
            WoundedTrophies = 40,
            RepelledTrophies = 20,
            GuaranteedItems =
            [
                new RaidPendingItem("soul_dust", 25),
                new RaidPendingItem("monster_core", 2)
            ]
        };
        var wounded = RaidRewardCalculator.CalculateGrant(
            RaidRewardCalculator.FullPackage(rewards, RaidOutcome.Wounded),
            []);
        var slain = RaidRewardCalculator.CalculateGrant(
            RaidRewardCalculator.FullPackage(rewards, RaidOutcome.Slain),
            [wounded.Package]);

        Assert.Equal(RaidRewardKind.WeeklyBase, wounded.Kind);
        Assert.Equal(40, wounded.Package.Trophies);
        Assert.Equal(RaidRewardKind.WeeklyUpgrade, slain.Kind);
        Assert.Equal(60, slain.Package.Trophies);
        Assert.Equal(15, slain.Package.Items.Single(x => x.ItemId == "soul_dust").Quantity);
        Assert.Equal(1, slain.Package.Items.Single(x => x.ItemId == "monster_core").Quantity);
        Assert.Equal(100, wounded.Package.Trophies + slain.Package.Trophies);
    }

    [Fact]
    public void Weekly_reward_can_upgrade_by_tier_and_repeat_clears_keep_quarter_rewards()
    {
        var tierOne = new RaidRewardDefinition
        {
            SlainTrophies = 100,
            GuaranteedItems = [new RaidPendingItem("soul_dust", 25)]
        };
        var tierTwo = new RaidRewardDefinition
        {
            SlainTrophies = 180,
            GuaranteedItems =
            [
                new RaidPendingItem("soul_dust", 45),
                new RaidPendingItem("greater_core", 2)
            ]
        };
        var first = RaidRewardCalculator.CalculateGrant(
            RaidRewardCalculator.FullPackage(tierOne, RaidOutcome.Slain),
            []);
        var upgrade = RaidRewardCalculator.CalculateGrant(
            RaidRewardCalculator.FullPackage(tierTwo, RaidOutcome.Slain),
            [first.Package]);
        var repeat = RaidRewardCalculator.CalculateGrant(
            RaidRewardCalculator.FullPackage(tierTwo, RaidOutcome.Slain),
            [first.Package, upgrade.Package]);

        Assert.Equal(RaidRewardKind.WeeklyUpgrade, upgrade.Kind);
        Assert.Equal(80, upgrade.Package.Trophies);
        Assert.Equal(20, upgrade.Package.Items.Single(x => x.ItemId == "soul_dust").Quantity);
        Assert.Equal(2, upgrade.Package.Items.Single(x => x.ItemId == "greater_core").Quantity);
        Assert.Equal(RaidRewardKind.Repeat, repeat.Kind);
        Assert.Equal(45, repeat.Package.Trophies);
        Assert.Equal(11, repeat.Package.Items.Single(x => x.ItemId == "soul_dust").Quantity);
        Assert.Equal(1, repeat.Package.Items.Single(x => x.ItemId == "greater_core").Quantity);
    }

    [Fact]
    public void Equal_outcome_rewards_do_not_accept_or_apply_contribution_scaling()
    {
        var rewards = new RaidRewardDefinition
        {
            BrokenTrophies = 65,
            GuaranteedItems = [new RaidPendingItem("soul_dust", 25)]
        };
        var package = RaidRewardCalculator.FullPackage(rewards, RaidOutcome.Broken);
        var firstParticipant = RaidRewardCalculator.CalculateGrant(package, []);
        var secondParticipant = RaidRewardCalculator.CalculateGrant(package, []);

        Assert.Equal(firstParticipant.Kind, secondParticipant.Kind);
        Assert.Equal(firstParticipant.Package.Trophies, secondParticipant.Package.Trophies);
        Assert.Equal(
            firstParticipant.Package.Items.Select(x => (x.ItemId, x.Quantity)),
            secondParticipant.Package.Items.Select(x => (x.ItemId, x.Quantity)));
    }

    [Fact]
    public async Task Leader_can_atomically_arrange_raid_parties_and_bench_participants()
    {
        await using var db = CreateDbContext();
        var users = Enumerable.Range(1, 3).Select(number =>
        {
            var user = AppUser.Guest();
            user.Username = $"RaidPartyUser{number}";
            return user;
        }).ToArray();
        var characters = users.Select((user, index) => new Character
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Name = $"Raid Party Character {index + 1}",
            Level = 30
        }).ToArray();
        db.Users.AddRange(users);
        db.Characters.AddRange(characters);

        var tier = new RaidBossTierDefinition
        {
            Tier = 0,
            LaneSlots = 3,
            MinimumRoster = 3
        };
        var boss = new RaidBossDefinition
        {
            Id = "raid-boss.party-test",
            Name = "Party Test Boss",
            Region = 1,
            LevelRequirement = 30,
            Tiers = [tier]
        };
        var run = new RaidRun
        {
            Id = Guid.NewGuid(),
            RaidBossId = boss.Id,
            Tier = tier.Tier,
            DefinitionSnapshotJson = JsonSerializer.Serialize(
                tier,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            LeaderCharacterId = characters[0].Id,
            Status = RaidRunStatus.Mustering,
            CreatedAt = DateTimeOffset.UtcNow,
            SignupClosesAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        var originalPositions = new[]
        {
            (RaidLane.Rearguard, 0),
            (RaidLane.Vanguard, 0),
            (RaidLane.MainGuard, 0)
        };
        for (var index = 0; index < characters.Length; index++)
        {
            run.Signups.Add(new RaidSignup
            {
                RaidRun = run,
                RaidRunId = run.Id,
                CharacterId = characters[index].Id,
                AccountId = users[index].Id,
                CharacterName = characters[index].Name,
                LoadoutHash = $"party-test-{index}",
                PowerRating = 100 + index,
                Lane = originalPositions[index].Item1,
                WingSlotIndex = originalPositions[index].Item2,
                SignedUpAt = DateTimeOffset.UtcNow.AddTicks(index)
            });
        }
        db.RaidRuns.Add(run);
        await db.SaveChangesAsync();

        var service = CreateRaidService(
            db,
            boss,
            developmentToolsEnabled: false);
        var result = await service.UpdatePartiesAsync(
            characters[0].Id,
            run.Id,
            [
                new RaidPartyAssignment(characters[0].Id, RaidLane.MainGuard, 1),
                new RaidPartyAssignment(characters[1].Id, RaidLane.Vanguard, 0),
                new RaidPartyAssignment(characters[2].Id, null, null)
            ],
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        var signups = result.Value!.Signups.ToDictionary(signup => signup.CharacterId);
        Assert.Equal((RaidLane.MainGuard, 1), (signups[characters[0].Id].Lane, signups[characters[0].Id].WingSlotIndex));
        Assert.Equal((RaidLane.Vanguard, 0), (signups[characters[1].Id].Lane, signups[characters[1].Id].WingSlotIndex));
        Assert.Equal((null, null), (signups[characters[2].Id].Lane, signups[characters[2].Id].WingSlotIndex));
        Assert.False(result.Value.CanCommence);
    }

    [Fact]
    public async Task Development_raid_creation_bypasses_prior_tier_requirement()
    {
        await using var db = CreateDbContext();
        var user = AppUser.Guest();
        user.Username = "RaidDeveloper";
        user.IsGuest = false;
        var character = new Character
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Name = "Raid Developer",
            Level = 30
        };
        db.Users.Add(user);
        db.Characters.Add(character);
        await db.SaveChangesAsync();

        var tierOne = new RaidBossTierDefinition
        {
            Tier = 1,
            LaneSlots = 3,
            MinimumRoster = 3
        };
        var tierTwo = new RaidBossTierDefinition
        {
            Tier = 2,
            LaneSlots = 4,
            MinimumRoster = 6
        };
        var boss = new RaidBossDefinition
        {
            Id = "raid-boss.test",
            Name = "Test Raid Boss",
            Region = 1,
            LevelRequirement = 25,
            Tiers = [tierOne, tierTwo]
        };
        var outbox = new RecordingGameEventOutbox();
        var service = CreateRaidService(
            db,
            boss,
            developmentToolsEnabled: true,
            powerRatings: new FixedPowerRatingService(),
            snapshots: new FixedCharacterSnapshotService(db),
            outbox: outbox);

        var result = await service.CreateDevelopmentAsync(
            character.Id,
            boss.Id,
            tierTwo.Tier,
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(tierTwo.Tier, result.Value!.Tier);
        Assert.True(result.Value.DevelopmentToolsEnabled);
        var run = await db.RaidRuns.AsNoTracking().SingleAsync();
        var channelSnapshot = Assert.IsType<RaidChatChannelSnapshotPayload>(
            Assert.Single(outbox.Payloads.OfType<RaidChatChannelSnapshotPayload>()));
        Assert.Equal([character.Id], channelSnapshot.MemberCharacterIds);
        Assert.Equal(
            "Raid channel opened. Raid Developer is leading the raid.",
            channelSnapshot.LifecycleMessage?.Body);
    }

    [Fact]
    public async Task Raid_leader_approves_join_request_and_can_remove_accepted_member()
    {
        await using var db = CreateDbContext();
        var leaderUser = AppUser.Guest();
        leaderUser.Username = "RaidLeader";
        leaderUser.IsGuest = false;
        var memberUser = AppUser.Guest();
        memberUser.Username = "RaidMember";
        memberUser.IsGuest = false;
        var leader = new Character
        {
            Id = Guid.NewGuid(),
            UserId = leaderUser.Id,
            User = leaderUser,
            Name = "Raid Leader",
            Level = 30
        };
        var member = new Character
        {
            Id = Guid.NewGuid(),
            UserId = memberUser.Id,
            User = memberUser,
            Name = "Raid Member",
            Level = 30
        };
        db.Users.AddRange(leaderUser, memberUser);
        db.Characters.AddRange(leader, member);
        await db.SaveChangesAsync();

        var boss = new RaidBossDefinition
        {
            Id = "raid-boss.test",
            Name = "Test Raid Boss",
            Region = 1,
            LevelRequirement = 25,
            Tiers =
            [
                new RaidBossTierDefinition
                {
                    Tier = 0,
                    LaneSlots = 3,
                    MinimumRoster = 3
                }
            ]
        };
        var outbox = new RecordingGameEventOutbox();
        var service = CreateRaidService(
            db,
            boss,
            developmentToolsEnabled: true,
            powerRatings: new FixedPowerRatingService(),
            snapshots: new FixedCharacterSnapshotService(db),
            outbox: outbox);

        var created = await service.CreateDevelopmentAsync(
            leader.Id,
            boss.Id,
            0,
            CancellationToken.None);
        Assert.True(created.Succeeded, created.Error);
        db.ChangeTracker.Clear();
        var joined = await service.JoinAsync(
            member.Id,
            created.Value!.Id,
            CancellationToken.None);
        Assert.True(joined.Succeeded, joined.Error);
        Assert.DoesNotContain(joined.Value!.Signups, signup => signup.CharacterId == member.Id);
        Assert.Equal(member.Id, Assert.Single(joined.Value.JoinRequests).CharacterId);
        var selfApproval = await service.ApproveSignupAsync(
            member.Id,
            created.Value.Id,
            member.Id,
            CancellationToken.None);
        Assert.False(selfApproval.Succeeded);
        Assert.Equal("Only the raid leader can approve join requests.", selfApproval.Error);
        db.ChangeTracker.Clear();
        var approved = await service.ApproveSignupAsync(
            leader.Id,
            created.Value.Id,
            member.Id,
            CancellationToken.None);
        Assert.True(approved.Succeeded, approved.Error);
        Assert.Empty(approved.Value!.JoinRequests);
        Assert.Contains(approved.Value.Signups, signup => signup.CharacterId == member.Id);
        db.ChangeTracker.Clear();
        var removed = await service.RemoveSignupAsync(
            leader.Id,
            created.Value.Id,
            member.Id,
            CancellationToken.None);
        Assert.True(removed.Succeeded, removed.Error);
        Assert.DoesNotContain(removed.Value!.Signups, signup => signup.CharacterId == member.Id);

        var snapshots = outbox.Payloads
            .OfType<RaidChatChannelSnapshotPayload>()
            .ToArray();
        Assert.Equal(4, snapshots.Length);
        Assert.Equal("Raid Member requested to join the raid.", snapshots[1].LifecycleMessage?.Body);
        Assert.DoesNotContain(member.Id, snapshots[1].MemberCharacterIds);
        Assert.Equal("Raid Member was approved to join the raid.", snapshots[2].LifecycleMessage?.Body);
        Assert.Contains(member.Id, snapshots[2].MemberCharacterIds);
        Assert.Equal("Raid Member was removed from the raid.", snapshots[3].LifecycleMessage?.Body);
        Assert.DoesNotContain(member.Id, snapshots[3].MemberCharacterIds);
        Assert.Equal(
            ["Created", "SignupRequested", "SignupApproved", "ParticipantRemoved"],
            outbox.Payloads.OfType<RaidUpdated>().Select(update => update.Event));
    }

    [Theory]
    [InlineData(RaidRunStatus.Playback)]
    [InlineData(RaidRunStatus.Settled)]
    public async Task Raid_participant_can_view_every_lane_during_and_after_playback(
        RaidRunStatus status)
    {
        await using var db = CreateDbContext();
        var participantId = Guid.NewGuid();
        var run = new RaidRun
        {
            Id = Guid.NewGuid(),
            RaidBossId = "raid-boss.test",
            Tier = 1,
            LeaderCharacterId = participantId,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            SignupClosesAt = DateTimeOffset.UtcNow,
            Signups =
            [
                new RaidSignup
                {
                    CharacterId = participantId,
                    AccountId = Guid.NewGuid(),
                    CharacterName = "Rearguard Participant",
                    Lane = RaidLane.Rearguard,
                    WingSlotIndex = 0,
                    SignedUpAt = DateTimeOffset.UtcNow
                }
            ]
        };
        foreach (var lane in Enum.GetValues<RaidLane>())
        {
            run.Playbacks.Add(new RaidPlayback
            {
                RaidRun = run,
                RaidRunId = run.Id,
                Lane = lane,
                TotalTicks = 10,
                FrameCount = 2,
                BundleHash = $"bundle-{lane}",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        db.RaidRuns.Add(run);
        await db.SaveChangesAsync();

        var service = CreateRaidService(
            db,
            new RaidBossDefinition { Id = run.RaidBossId },
            developmentToolsEnabled: false);

        foreach (var lane in Enum.GetValues<RaidLane>())
        {
            var playback = await service.GetPlaybackAsync(
                participantId,
                run.Id,
                lane,
                CancellationToken.None);

            Assert.NotNull(playback);
            Assert.Equal(lane, playback.Lane);
        }
        Assert.Null(await service.GetPlaybackAsync(
            Guid.NewGuid(),
            run.Id,
            RaidLane.Rearguard,
            CancellationToken.None));
    }

    [Fact]
    public void Playback_duration_runs_preparation_battles_concurrently()
    {
        RaidPlayback[] playbacks =
        [
            new() { Lane = RaidLane.Rearguard, TotalTicks = 100, TicksPerSecond = 10 },
            new() { Lane = RaidLane.Vanguard, TotalTicks = 200, TicksPerSecond = 10 },
            new() { Lane = RaidLane.MainGuard, TotalTicks = 150, TicksPerSecond = 10 },
            new() { Lane = RaidLane.FinalAssault, TotalTicks = 50, TicksPerSecond = 10 }
        ];
        var method = typeof(RaidService).GetMethod(
            "GetPlaybackDuration",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var duration = Assert.IsType<TimeSpan>(method!.Invoke(null, [playbacks, 2]));

        // Vanguard is the longest preparation at 20s. It overlaps Rearguard (12s including holds)
        // and Main Guard (15s), then Final Assault takes 5s with one 1.5s transition per phase.
        Assert.Equal(TimeSpan.FromSeconds(28), duration);
    }

    [Fact]
    public async Task Personal_raid_history_prioritizes_unclaimed_rewards_and_filters_by_boss()
    {
        await using var db = CreateDbContext();
        var characterId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var boss = new RaidBossDefinition
        {
            Id = "raid-boss.test",
            Name = "Test Raid Boss"
        };

        var unclaimedRun = CreateCompletedRun(boss.Id, 2, RaidOutcome.Broken, now.AddDays(-2));
        var claimedRun = CreateCompletedRun(boss.Id, 1, RaidOutcome.Slain, now.AddDays(-1));
        var otherBossRun = CreateCompletedRun("raid-boss.other", 3, RaidOutcome.Wounded, now);
        unclaimedRun.RewardClaims.Add(CreateRewardClaim(unclaimedRun, characterId, 65));
        claimedRun.RewardClaims.Add(CreateRewardClaim(claimedRun, characterId, 100, now));
        otherBossRun.RewardClaims.Add(CreateRewardClaim(otherBossRun, characterId, 40));
        db.RaidRuns.AddRange(unclaimedRun, claimedRun, otherBossRun);
        await db.SaveChangesAsync();

        var service = CreateRaidService(
            db,
            boss,
            developmentToolsEnabled: false,
            rewardsEnabled: true);

        var history = await service.GetHistoryAsync(characterId, boss.Id, 10, CancellationToken.None);

        Assert.Collection(
            history,
            entry =>
            {
                Assert.Equal(unclaimedRun.Id, entry.RaidRunId);
                Assert.Equal("Test Raid Boss", entry.RaidBossName);
                Assert.True(entry.CanClaim);
                Assert.Null(entry.ClaimedAt);
                Assert.Equal(65, entry.Trophies);
            },
            entry =>
            {
                Assert.Equal(claimedRun.Id, entry.RaidRunId);
                Assert.False(entry.CanClaim);
                Assert.NotNull(entry.ClaimedAt);
            });
    }

    [Fact]
    public async Task Disabled_raid_rewards_cannot_be_claimed_or_presented_as_claimable()
    {
        await using var db = CreateDbContext();
        var characterId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var boss = new RaidBossDefinition
        {
            Id = "raid-boss.rewards-disabled",
            Name = "Rewards Disabled Boss"
        };
        var run = CreateCompletedRun(boss.Id, 1, RaidOutcome.Slain, now);
        run.RewardClaims.Add(CreateRewardClaim(run, characterId, 100));
        db.RaidRuns.Add(run);
        await db.SaveChangesAsync();

        var service = CreateRaidService(
            db,
            boss,
            developmentToolsEnabled: false);

        var history = await service.GetHistoryAsync(characterId, boss.Id, 10, CancellationToken.None);
        var claim = await service.ClaimAsync(characterId, run.Id, CancellationToken.None);

        Assert.False(Assert.Single(history).CanClaim);
        Assert.False(claim.Succeeded);
        Assert.Equal("Raid rewards are currently disabled.", claim.Error);
        Assert.False(run.RewardClaims.Single().ClaimedAt.HasValue);
    }

    [Fact]
    public async Task Slain_announcement_waits_until_visual_playback_has_ended()
    {
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var leaderId = Guid.NewGuid();
        var boss = new RaidBossDefinition
        {
            Id = "raid-boss.playback-test",
            Name = "Playback Test Boss",
            Tiers = [new RaidBossTierDefinition { Tier = 1 }]
        };
        db.RaidRuns.Add(new RaidRun
        {
            Id = Guid.NewGuid(),
            RaidBossId = boss.Id,
            Tier = 1,
            LeaderCharacterId = Guid.NewGuid(),
            Status = RaidRunStatus.Settled,
            Outcome = RaidOutcome.Slain,
            CreatedAt = now.AddDays(-1),
            SignupClosesAt = now.AddDays(-1),
            ResolvedAt = now.AddDays(-1),
            SettledAt = now.AddDays(-1)
        });
        var run = new RaidRun
        {
            Id = Guid.NewGuid(),
            RaidBossId = boss.Id,
            Tier = 1,
            LeaderCharacterId = leaderId,
            Status = RaidRunStatus.Playback,
            Outcome = RaidOutcome.Slain,
            CreatedAt = now,
            SignupClosesAt = now,
            PlaybackStartedAt = now,
            PlaybackEndsAt = now.AddMinutes(1),
            Signups =
            [
                new RaidSignup
                {
                    CharacterId = leaderId,
                    AccountId = Guid.NewGuid(),
                    CharacterName = "Raid Leader",
                    Lane = RaidLane.Vanguard,
                    WingSlotIndex = 0,
                    SignedUpAt = now
                }
            ]
        };
        db.RaidRuns.Add(run);
        await db.SaveChangesAsync();

        var outbox = new RecordingGameEventOutbox();
        var service = CreateRaidService(
            db,
            boss,
            developmentToolsEnabled: false,
            outbox: outbox,
            stateSync: new NoopStateSyncService());

        await service.ProcessDueRaidsAsync("test-worker", 10, CancellationToken.None);

        Assert.Equal(RaidRunStatus.Playback, run.Status);
        Assert.DoesNotContain(GameEventTypes.RaidChatAnnouncement, outbox.EventTypes);

        run.PlaybackEndsAt = now.AddSeconds(-1);
        await db.SaveChangesAsync();
        await service.ProcessDueRaidsAsync("test-worker", 10, CancellationToken.None);

        Assert.Equal(RaidRunStatus.Settled, run.Status);
        Assert.NotNull(run.ResolvedAt);
        Assert.Contains(GameEventTypes.RaidChatAnnouncement, outbox.EventTypes);
        Assert.Contains(GameEventTypes.RaidChatChannelSnapshot, outbox.EventTypes);
    }

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new LLDbContext(options);
    }

    private static RaidRun CreateCompletedRun(
        string raidBossId,
        int tier,
        RaidOutcome outcome,
        DateTimeOffset resolvedAt) =>
        new()
        {
            RaidBossId = raidBossId,
            Tier = tier,
            Status = RaidRunStatus.Settled,
            Outcome = outcome,
            CreatedAt = resolvedAt.AddHours(-1),
            SignupClosesAt = resolvedAt.AddHours(-1),
            ResolvedAt = resolvedAt,
            SettledAt = resolvedAt
        };

    private static RaidRewardClaim CreateRewardClaim(
        RaidRun run,
        Guid characterId,
        int trophies,
        DateTimeOffset? claimedAt = null) =>
        new()
        {
            RaidRun = run,
            RaidRunId = run.Id,
            RaidBossId = run.RaidBossId,
            CharacterId = characterId,
            Trophies = trophies,
            CreatedAt = run.ResolvedAt!.Value,
            ClaimedAt = claimedAt
        };

    private static RaidService CreateRaidService(
        LLDbContext db,
        RaidBossDefinition boss,
        bool developmentToolsEnabled,
        IPowerRatingService? powerRatings = null,
        ICharacterSnapshotService? snapshots = null,
        IGameEventOutbox? outbox = null,
        IStateSyncService? stateSync = null,
        bool rewardsEnabled = false) =>
        new(
            db: db,
            definitions: new FixedRaidBossDefinitionProvider(boss),
            trophyVendor: null!,
            snapshots: snapshots!,
            powerRatings: powerRatings!,
            inventory: null!,
            inventoryItemFactory: null!,
            itemBases: null!,
            combatResolver: null!,
            playbackBundles: null!,
            achievements: null!,
            outbox: outbox ?? new NoopGameEventOutbox(),
            stateSync: stateSync!,
            memoryCache: new MemoryCache(new MemoryCacheOptions()),
            timeProvider: TimeProvider.System,
            jsonOptions: new JsonSerializerOptions(JsonSerializerDefaults.Web),
            options: Options.Create(new RaidOptions
            {
                RewardsEnabled = rewardsEnabled,
                DevelopmentToolsEnabled = developmentToolsEnabled
            }),
            logger: NullLogger<RaidService>.Instance);

    private sealed class NoopGameEventOutbox : IGameEventOutbox
    {
        public Task EnqueueAsync<TPayload>(
            string eventType,
            TPayload payload,
            Guid? characterId,
            Guid? accountId,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class RecordingGameEventOutbox : IGameEventOutbox
    {
        public List<string> EventTypes { get; } = [];
        public List<object> Payloads { get; } = [];

        public Task EnqueueAsync<TPayload>(
            string eventType,
            TPayload payload,
            Guid? characterId,
            Guid? accountId,
            CancellationToken cancellationToken)
        {
            EventTypes.Add(eventType);
            Payloads.Add(payload!);
            return Task.CompletedTask;
        }
    }

    private sealed class NoopStateSyncService : IStateSyncService
    {
        public IReadOnlyDictionary<string, long> GetChangedRevisions(Guid? characterId) =>
            new Dictionary<string, long>();

        public Task InvalidateCharacterAsync(
            Guid characterId,
            string reason,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task InvalidateCharacterScopeAsync(
            Guid characterId,
            string scope,
            string reason,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task InvalidateWorldScopeAsync(
            string scope,
            string reason,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Application.WebSockets.Contracts.StateSyncCheckpoint> GetCheckpointAsync(
            Guid characterId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Application.WebSockets.Contracts.StateSyncCheckpoint(
                characterId,
                new Dictionary<string, long>(),
                DateTimeOffset.UtcNow));
    }

    private sealed class FixedPowerRatingService : IPowerRatingService
    {
        public Task<OverallPowerRating> GetCharacterOverallRatingAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new OverallPowerRating(1_000, PowerAnalysisState.Available));

        public Task<OverallPowerRating> GetCharacterOverallRatingAsync(
            Character character,
            CancellationToken cancellationToken) =>
            GetCharacterOverallRatingAsync(character.Id, cancellationToken);

        public Task<PowerRatingSnapshot> GetCharacterRatingAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PowerRatingSnapshot(
                PowerRatingAlgorithm.Version,
                "development-build",
                1_000,
                1_000,
                1_000,
                1_000,
                1_000,
                1_000,
                1_000,
                DateTimeOffset.UtcNow,
                PowerRatingConfidence.High,
                PowerAnalysisState.Available));

        public Task<PowerRatingSnapshot> GetPartyRatingAsync(
            Guid characterId,
            DungeonPartySelection partySelection,
            CancellationToken cancellationToken) =>
            GetCharacterRatingAsync(characterId, cancellationToken);
    }

    private sealed class FixedCharacterSnapshotService(LLDbContext? db = null) : ICharacterSnapshotService
    {
        public async Task<CharacterSnapshot> CreateAsync(Guid characterId, CancellationToken ct)
        {
            var snapshot = new CharacterSnapshot
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                Name = "Raid Developer",
                Level = 30
            };
            if (db is not null)
            {
                db.CharacterSnapshots.Add(snapshot);
                await db.SaveChangesAsync(ct);
            }

            return snapshot;
        }

        public Task<CharacterSnapshot?> GetSnapshotByCharacterIdAsync(
            Guid characterId,
            CancellationToken ct) =>
            Task.FromResult<CharacterSnapshot?>(null);

        public Task<CharacterSnapshot?> GetSnapshotByIdAsync(
            Guid snapshotId,
            CancellationToken ct) =>
            Task.FromResult<CharacterSnapshot?>(null);
    }

    private sealed class FixedRaidBossDefinitionProvider(RaidBossDefinition boss)
        : IRaidBossDefinitionProvider
    {
        public IReadOnlyList<RaidBossDefinition> GetAll() => [boss];

        public RaidBossDefinition? Get(string raidBossId) =>
            string.Equals(raidBossId, boss.Id, StringComparison.OrdinalIgnoreCase)
                ? boss
                : null;
    }

    private static string[] PropertyNames(Microsoft.EntityFrameworkCore.Metadata.IReadOnlyIndex index) =>
        index.Properties.Select(x => x.Name).ToArray();

    private static void AssertModifier(CombatEntity combatant, AttributeType attribute, float amount)
    {
        var modifier = Assert.Single(combatant.TemporaryModifiers, x => x.AttributeType == attribute);
        Assert.Equal(ModifierType.Multiplicative, modifier.ModifierType);
        Assert.Equal(amount, modifier.Amount, 3);
    }
}
