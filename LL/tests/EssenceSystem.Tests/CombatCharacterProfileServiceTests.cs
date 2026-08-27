using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services.LL.CombatProfiles;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Profiles;
using Services.LL.Essences;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class CombatCharacterProfileServiceTests
{
    [Fact]
    public void Role_aware_discovery_uses_canonical_roles_and_preserves_legacy_signatures()
    {
        var services = CreateServices();
        var simulator = new AbilityBalanceSimulator(
            services.AbilityCatalog,
            services.EssenceDefinitions,
            services.Factory);
        AbilityBalanceTeamLoadout[] candidates =
        [
            new(Enumerable.Range(0, 5)
                .Select(_ => new AbilityBalanceParticipantLoadout(["essence.goblin"]))
                .ToArray()),
            new(Enumerable.Range(0, 5)
                .Select(_ => new AbilityBalanceParticipantLoadout(["essence.raven"]))
                .ToArray())
        ];

        var roleAware = simulator.Run(new AbilityBalanceSimulationRequest(
            BattleCount: 1,
            TeamSize: 5,
            EssencesPerParticipant: 1,
            RandomSeed: 8471,
            TopResults: 2,
            CandidatePoolSize: 2,
            CandidateTeams: candidates,
            EquipmentTier: 1,
            EquipmentRarity: "Epic",
            EquipmentProfile: "Balanced",
            UseCanonicalRoles: true));
        var legacy = simulator.Run(new AbilityBalanceSimulationRequest(
            BattleCount: 1,
            TeamSize: 5,
            EssencesPerParticipant: 1,
            RandomSeed: 8471,
            TopResults: 2,
            CandidatePoolSize: 2,
            CandidateTeams: candidates,
            EquipmentTier: 1,
            EquipmentRarity: "Epic",
            EquipmentProfile: "Balanced"));

        Assert.All(roleAware.RankedCombinations, result =>
            Assert.Equal(
                ["Guardian", "Restorer", "Striker", "Striker", "Controller"],
                result.Participants.Select(participant => participant.Role).ToArray()));
        Assert.All(roleAware.RankedCombinations, result =>
        {
            Assert.Contains("Guardian=", result.Signature);
            Assert.Contains("Controller=", result.Signature);
        });
        Assert.NotNull(roleAware.ParticipantAttributesByRole);
        Assert.Equal(
            ["Controller", "Guardian", "Restorer", "Striker"],
            roleAware.ParticipantAttributesByRole!.Keys.Order().ToArray());
        Assert.NotEqual(
            roleAware.ParticipantAttributesByRole["Guardian"]["MaxHealth"],
            roleAware.ParticipantAttributesByRole["Striker"]["MaxHealth"]);
        Assert.All(legacy.RankedCombinations, result =>
        {
            Assert.DoesNotContain("Balance=", result.Signature);
            Assert.All(result.Participants, participant => Assert.Equal("Balance", participant.Role));
        });
    }

    [Fact]
    public async Task Generator_selects_distinct_families_and_prepares_complete_production_profiles()
    {
        var services = CreateServices();
        await using var db = CreateDbContext();
        db.ItemBases.AddRange(services.CraftingDefinitions.GetEquipmentBases().Values);
        await db.SaveChangesAsync();
        var setup = new CombatSetupService(
            null!,
            services.EssenceResolver,
            services.EssenceDefinitions,
            services.CreatureEssences,
            craftingDefinitions: services.CraftingDefinitions);
        var pipeline = new CombatPreparationPipeline(
            new SnapshotCombatantBuilder(db, setup),
            setup);
        var materializer = new CombatCharacterProfileMaterializer(
            services.Factory,
            pipeline,
            services.EssenceDefinitions);
        var profileService = new CombatCharacterProfileService(
            services.Factory,
            materializer,
            services.EssenceDefinitions,
            services.AbilityCatalog);
        var request = new CombatCharacterProfileGenerationRequest(
            "audit-1",
            CreateAudit(AbilityBalanceContentFingerprint.Create(
                services.AbilityCatalog,
                services.EssenceDefinitions)),
            ContentType: "Dungeon",
            EquipmentQuality: "Standard",
            TeamsPerFamily: 1,
            RandomSeed: 8471,
            PortfolioMode: "Core",
            MinimumSourceBattles: 1,
            MaximumConfidenceWidth95: 1,
            MaximumSeedScoreSpread: 1,
            MaximumEssenceOverlap: 1,
            RequireMultiSeedStability: false);

        var first = await profileService.GenerateAsync(request, CancellationToken.None);
        var second = await profileService.GenerateAsync(request, CancellationToken.None);

        Assert.Equal(CombatCharacterProfileService.SchemaVersion, first.SchemaVersion);
        Assert.Equal(CombatCharacterProfileService.GeneratorVersion, first.GeneratorVersion);
        Assert.NotNull(first.Scenario);
        Assert.Equal(1, first.Scenario.TeamSize);
        Assert.Equal(1, first.Scenario.EssencesPerParticipant);
        Assert.Equal("Standard", first.Scenario.EquipmentQuality);
        Assert.Equal("Balanced", first.Scenario.AuditEquipmentProfile);
        Assert.Equal(
            "scenario.dungeon.team-1.tier-1.epic.standard.balanced.essences-1",
            first.Scenario.Id);
        Assert.Equal(["Meta", "Typical", "WeakButLegal"], first.Teams.Select(team => team.Family));
        Assert.Equal(first.Teams.Select(team => team.Id), second.Teams.Select(team => team.Id));
        Assert.Equal(
            first.Teams.SelectMany(team => team.Profiles).Select(profile => profile.Id),
            second.Teams.SelectMany(team => team.Profiles).Select(profile => profile.Id));
        Assert.All(first.Teams.SelectMany(team => team.Profiles), profile =>
        {
            Assert.True(profile.Prepared.IsProductionReady);
            Assert.Equal("Dungeon", profile.ContentType);
            Assert.Equal("Defensive", profile.EquipmentProfile);
            Assert.Equal("Guardian", profile.Role);
            Assert.Equal(7, profile.Prepared.Equipment.Count);
            Assert.NotEmpty(profile.Prepared.AbilityIds);
            Assert.Equal(profile.EssenceIds, profile.Prepared.EssenceIds);
            Assert.True(profile.DisplayPowerRating > 0);
            Assert.Equal(profile.Prepared.MaxHealth, profile.Prepared.CurrentHealth);
        });
        Assert.Equal("essence.goblin", first.Teams[0].Profiles[0].EssenceIds.Single());
        Assert.Equal("essence.raven", first.Teams[1].Profiles[0].EssenceIds.Single());
        Assert.Equal("essence.green_slime", first.Teams[2].Profiles[0].EssenceIds.Single());
        var snapshotRequest = materializer.CreateSnapshotRequest(first.Teams[0].Profiles[0], 1);
        Assert.Equal(first.Teams[0].Profiles[0].Id, snapshotRequest.Slot.SlotId);
        Assert.Equal(1, snapshotRequest.Slot.PartyNumber);
        Assert.Equal(7, snapshotRequest.Snapshot.Equipment.Count);
        Assert.Equal(
            first.Teams[0].Profiles[0].EssenceIds,
            snapshotRequest.Snapshot.EquippedEssences
                .OrderBy(essence => essence.SlotIndex)
                .Select(essence => essence.EssenceDefinitionId));

        var staleRequest = request with
        {
            Audit = request.Audit with { ContentHash = "stale-content" }
        };
        var stale = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            profileService.GenerateAsync(staleRequest, CancellationToken.None));
        Assert.Contains("stale", stale.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task World_tower_generator_expands_a_five_member_party_into_an_exact_fifteen_slot_expedition()
    {
        var services = CreateServices();
        await using var db = CreateDbContext();
        db.ItemBases.AddRange(services.CraftingDefinitions.GetEquipmentBases().Values);
        await db.SaveChangesAsync();
        var setup = new CombatSetupService(
            null!,
            services.EssenceResolver,
            services.EssenceDefinitions,
            services.CreatureEssences,
            craftingDefinitions: services.CraftingDefinitions);
        var materializer = new CombatCharacterProfileMaterializer(
            services.Factory,
            new CombatPreparationPipeline(new SnapshotCombatantBuilder(db, setup), setup),
            services.EssenceDefinitions);
        var generator = new CombatCharacterProfileService(
            services.Factory,
            materializer,
            services.EssenceDefinitions,
            services.AbilityCatalog);
        var contentHash = AbilityBalanceContentFingerprint.Create(
            services.AbilityCatalog,
            services.EssenceDefinitions);

        var report = await generator.GenerateAsync(
            new CombatCharacterProfileGenerationRequest(
                "tower-party-audit",
                CreatePartyAudit(contentHash),
                ContentType: "WorldTower",
                EquipmentQuality: "Fine",
                TeamsPerFamily: 1,
                RandomSeed: 8471,
                PortfolioMode: "Core",
                MinimumSourceBattles: 1,
                MaximumConfidenceWidth95: 1,
                MaximumSeedScoreSpread: 1,
                MaximumEssenceOverlap: 1,
                RequireMultiSeedStability: false,
                TargetTeamSize: 15,
                TargetEquipmentTier: 2,
                TargetEquipmentRarity: "Legendary"),
            CancellationToken.None);

        Assert.Equal(CombatCharacterProfileService.SchemaVersion, report.SchemaVersion);
        Assert.Equal(CombatCharacterProfileService.GeneratorVersion, report.GeneratorVersion);
        Assert.NotNull(report.Scenario);
        Assert.Equal(15, report.Scenario.TeamSize);
        Assert.Equal(5, report.Scenario.PartySize);
        Assert.Equal(3, report.Scenario.PartyCount);
        Assert.Equal(5, report.Scenario.DiscoveryTeamSize);
        Assert.Equal(2, report.Scenario.EquipmentTier);
        Assert.Equal("Legendary", report.Scenario.EquipmentRarity);
        Assert.Equal("Fine", report.Scenario.EquipmentQuality);

        Assert.All(report.Teams, team =>
        {
            Assert.Equal(15, team.Profiles.Count);
            Assert.Equal(3, team.Parties?.Count);
            Assert.True(team.IsComposedExpedition);
            Assert.Equal(0, team.SourceBattles);
            Assert.Equal([1, 2, 3], team.Profiles
                .Select(profile => profile.PartyNumber)
                .Distinct()
                .ToArray());
            foreach (var party in team.Parties!)
            {
                Assert.Equal(5, party.ProfileIds.Count);
                Assert.False(string.IsNullOrWhiteSpace(party.Evidence.SourceSignature));
                Assert.False(string.IsNullOrWhiteSpace(party.Evidence.SelectionReason));
                var profiles = team.Profiles
                    .Where(profile => profile.PartyNumber == party.PartyNumber)
                    .OrderBy(profile => profile.PartySlotIndex)
                    .ToArray();
                Assert.Equal([0, 1, 2, 3, 4], profiles
                    .Select(profile => profile.PartySlotIndex)
                    .ToArray());
                Assert.Equal(party.ProfileIds, profiles.Select(profile => profile.Id));
                Assert.All(profiles, profile =>
                    Assert.Equal(party.SourcePartyProfileId, profile.SourcePartyProfileId));
            }
            Assert.All(team.Profiles.SelectMany(profile => profile.Prepared.Equipment), equipment =>
            {
                Assert.Equal(2, equipment.Tier);
                Assert.Equal("Legendary", equipment.Rarity);
                Assert.Equal("Fine", equipment.Quality);
            });
        });

        var thirdPartyProfile = report.Teams[0].Profiles.Single(profile =>
            profile.PartyNumber == 3 && profile.PartySlotIndex == 0);
        var snapshotRequest = materializer.CreateSnapshotRequest(thirdPartyProfile);
        Assert.Equal(3, snapshotRequest.Slot.PartyNumber);

        var catalogService = new JsonCombatCharacterProfileCatalogService(
            Path.Combine(FindApiContentRoot(), "Data", "combat", "combat-character-profiles.json"),
            services.JsonOptions,
            services.Factory,
            materializer,
            services.EssenceDefinitions,
            services.AbilityCatalog);
        var validation = await catalogService.ValidateAsync(
            new CombatCharacterProfileCatalogDocument(1, 1, [report]),
            CancellationToken.None);
        Assert.True(
            validation.IsValid,
            string.Join(Environment.NewLine, validation.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));

    }

    [Fact]
    public async Task World_tower_generation_selects_profiles_from_production_context_evidence()
    {
        var services = CreateServices();
        await using var db = CreateDbContext();
        db.ItemBases.AddRange(services.CraftingDefinitions.GetEquipmentBases().Values);
        await db.SaveChangesAsync();
        var setup = new CombatSetupService(
            null!,
            services.EssenceResolver,
            services.EssenceDefinitions,
            services.CreatureEssences,
            craftingDefinitions: services.CraftingDefinitions);
        var materializer = new CombatCharacterProfileMaterializer(
            services.Factory,
            new CombatPreparationPipeline(new SnapshotCombatantBuilder(db, setup), setup),
            services.EssenceDefinitions);
        var qualifier = new StubWorldTowerProfileCandidateQualifier(new Dictionary<string, double>
        {
            ["party-meta"] = 0.10,
            ["party-typical"] = 0.50,
            ["party-weak"] = 1.00
        });
        var generator = new CombatCharacterProfileService(
            services.Factory,
            materializer,
            services.EssenceDefinitions,
            services.AbilityCatalog,
            qualifier);
        var contentHash = AbilityBalanceContentFingerprint.Create(
            services.AbilityCatalog,
            services.EssenceDefinitions);

        var report = await generator.GenerateAsync(
            new CombatCharacterProfileGenerationRequest(
                "tower-qualified-audit",
                CreatePartyAudit(contentHash),
                ContentType: "WorldTower",
                EquipmentQuality: "Standard",
                TeamsPerFamily: 1,
                RandomSeed: 8471,
                PortfolioMode: "Core",
                MinimumSourceBattles: 1,
                MaximumConfidenceWidth95: 1,
                MaximumSeedScoreSpread: 1,
                MaximumEssenceOverlap: 1,
                RequireMultiSeedStability: false,
                TargetTeamSize: 5,
                TargetEquipmentTier: 1,
                TargetEquipmentRarity: "Epic",
                TargetFloorNumbers: [1],
                ContextQualificationSampleCount: 7),
            CancellationToken.None);

        Assert.Equal([1], report.Scenario!.FloorNumbers);
        Assert.Equal(7, qualifier.SampleCount);
        Assert.Equal([1], qualifier.Scenario!.FloorNumbers);
        var meta = Assert.Single(report.Teams, team => team.Family == "Meta");
        Assert.Equal("party-weak", meta.SourceSignature);
        var evidence = Assert.Single(Assert.Single(meta.Parties!).Evidence.ContextEvidence!);
        Assert.Equal(1, evidence.FloorNumber);
        Assert.Equal(7, evidence.SampleCount);
        Assert.Equal(1d, evidence.WinRate);
        Assert.True(evidence.UsesProductionRuntime);
        Assert.True(evidence.AbilitiesStartOnCooldown);

        var catalogService = new JsonCombatCharacterProfileCatalogService(
            Path.Combine(FindApiContentRoot(), "Data", "combat", "combat-character-profiles.json"),
            services.JsonOptions,
            services.Factory,
            materializer,
            services.EssenceDefinitions,
            services.AbilityCatalog);
        var valid = await catalogService.ValidateAsync(
            new CombatCharacterProfileCatalogDocument(1, 1, [report]),
            CancellationToken.None);
        Assert.True(
            valid.IsValid,
            string.Join(Environment.NewLine, valid.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));

        var party = Assert.Single(meta.Parties!);
        var invalidEvidence = evidence with { SeedManifestHash = "not-a-sha256" };
        var invalidParty = party with
        {
            Evidence = party.Evidence with { ContextEvidence = [invalidEvidence] }
        };
        var invalidTeam = meta with { Parties = [invalidParty] };
        var invalidReport = report with
        {
            Teams = [invalidTeam, .. report.Teams.Where(team => team.Id != meta.Id)]
        };
        var invalid = await catalogService.ValidateAsync(
            new CombatCharacterProfileCatalogDocument(1, 1, [invalidReport]),
            CancellationToken.None);
        Assert.False(invalid.IsValid);
        Assert.Contains(invalid.Issues, issue => issue.Code == "PartyContextEvidenceInvalid");
    }

    [Fact]
    public async Task Expanded_world_tower_generation_composes_ten_bounded_mixed_expeditions_with_party_evidence()
    {
        var services = CreateServices();
        await using var db = CreateDbContext();
        db.ItemBases.AddRange(services.CraftingDefinitions.GetEquipmentBases().Values);
        await db.SaveChangesAsync();
        var setup = new CombatSetupService(
            null!,
            services.EssenceResolver,
            services.EssenceDefinitions,
            services.CreatureEssences,
            craftingDefinitions: services.CraftingDefinitions);
        var materializer = new CombatCharacterProfileMaterializer(
            services.Factory,
            new CombatPreparationPipeline(new SnapshotCombatantBuilder(db, setup), setup),
            services.EssenceDefinitions);
        var generator = new CombatCharacterProfileService(
            services.Factory,
            materializer,
            services.EssenceDefinitions,
            services.AbilityCatalog);
        var contentHash = AbilityBalanceContentFingerprint.Create(
            services.AbilityCatalog,
            services.EssenceDefinitions);

        var generationRequest = new CombatCharacterProfileGenerationRequest(
                "tower-expanded-party-audit",
                CreateExpandedPartyAudit(contentHash, services.EssenceDefinitions),
                ContentType: "WorldTower",
                EquipmentQuality: "Standard",
                TeamsPerFamily: 1,
                RandomSeed: 8471,
                PortfolioMode: "Expanded",
                MinimumSourceBattles: 100,
                MaximumConfidenceWidth95: 0.25,
                MaximumSeedScoreSpread: 0.15,
                MaximumEssenceOverlap: 0.20,
                RequireMultiSeedStability: true,
                TargetTeamSize: 15,
                TargetEquipmentTier: 1,
                TargetEquipmentRarity: "Epic");
        var report = await generator.GenerateAsync(
            generationRequest,
            CancellationToken.None);

        Assert.Equal(10, report.Teams.Count);
        Assert.All(report.Teams, team =>
        {
            Assert.True(team.IsComposedExpedition);
            Assert.Equal(15, team.Profiles.Count);
            Assert.Equal(3, team.Parties?.Count);
            Assert.Equal(0, team.SourceBattles);
            Assert.All(team.Parties!, party =>
            {
                Assert.Equal(5, party.ProfileIds.Count);
                Assert.True(party.Evidence.IsSyntheticControl || party.Evidence.SourceBattles >= 100);
                Assert.True(party.Evidence.IsSyntheticControl
                            || party.Evidence.ConfidenceUpper95 - party.Evidence.ConfidenceLower95 <= 0.25);
            });
        });
        Assert.Equal(
            ["Meta", "Typical", "Meta"],
            Assert.Single(report.Teams, team => team.Family == "Mixed.MetaTypical")
                .Parties!
                .OrderBy(party => party.PartyNumber)
                .Select(party => party.Evidence.Family)
                .ToArray());
        Assert.Equal(
            3,
            Assert.Single(report.Teams, team => team.Family == "Mixed.RoleSpecialist")
                .Parties!
                .Select(party => party.Evidence.Family)
                .Distinct()
                .Count());
        var adversarial = Assert.Single(report.Teams, team => team.Family == "EqualPowerAdversarial");
        Assert.Equal(2, adversarial.Parties!
            .Select(party => party.Evidence.SourceSignature)
            .Distinct()
            .Count());

        var singlePartyReport = await generator.GenerateAsync(
            generationRequest with
            {
                AuditId = "tower-expanded-single-party-audit",
                TargetTeamSize = 5
            },
            CancellationToken.None);
        Assert.Equal(10, singlePartyReport.Teams.Count);
        Assert.All(singlePartyReport.Teams, team =>
        {
            Assert.False(team.IsComposedExpedition);
            Assert.Single(team.Parties!);
        });
        Assert.Contains(singlePartyReport.Teams, team =>
            team.Family == "RoleSpecialist.Controller");

        var catalogService = new JsonCombatCharacterProfileCatalogService(
            Path.Combine(FindApiContentRoot(), "Data", "combat", "combat-character-profiles.json"),
            services.JsonOptions,
            services.Factory,
            materializer,
            services.EssenceDefinitions,
            services.AbilityCatalog);
        var validation = await catalogService.ValidateAsync(
            new CombatCharacterProfileCatalogDocument(1, 1, [singlePartyReport, report]),
            CancellationToken.None);
        Assert.True(
            validation.IsValid,
            string.Join(Environment.NewLine, validation.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));

        var underSampledAudit = generationRequest.Audit with
        {
            FinalistMatchups = generationRequest.Audit.FinalistMatchups!
                .Select(matchup => matchup with
                {
                    Battles = 30,
                    FirstWins = (int)Math.Round(matchup.FirstScore * 30),
                    SecondWins = 30 - (int)Math.Round(matchup.FirstScore * 30),
                    Draws = 0
                })
                .ToArray()
        };
        var underSampled = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            generator.GenerateAsync(
                generationRequest with
                {
                    AuditId = "tower-expanded-under-sampled-matchups",
                    Audit = underSampledAudit,
                    MinimumSourceBattles = 100,
                    MinimumMatchupBattles = 100
                },
                CancellationToken.None));
        Assert.Contains("head-to-head matchups", underSampled.Message);

        var mixed = Assert.Single(report.Teams, team => team.Family == "Mixed.MetaTypical");
        var tamperedParty = mixed.Parties![0] with
        {
            Evidence = mixed.Parties[0].Evidence with { SourceBattles = 1 }
        };
        var tamperedTeam = mixed with
        {
            SourceBattles = 1,
            Parties = [tamperedParty, .. mixed.Parties.Skip(1)]
        };
        var tamperedReport = report with
        {
            Teams = [tamperedTeam, .. report.Teams.Where(team => team.Id != mixed.Id)]
        };
        var rejected = await catalogService.ValidateAsync(
            new CombatCharacterProfileCatalogDocument(1, 1, [tamperedReport]),
            CancellationToken.None);
        Assert.False(rejected.IsValid);
        Assert.Contains(rejected.Issues, issue =>
            issue.Code == "ComposedExpeditionClaimsDirectEvidence");
        Assert.Contains(rejected.Issues, issue =>
            issue.Code == "PartySourceBattleCountsInvalid");
    }

    [Fact]
    public async Task Expanded_generator_uses_stable_evidence_and_role_aware_controls()
    {
        var services = CreateServices();
        await using var db = CreateDbContext();
        db.ItemBases.AddRange(services.CraftingDefinitions.GetEquipmentBases().Values);
        await db.SaveChangesAsync();
        var setup = new CombatSetupService(
            null!,
            services.EssenceResolver,
            services.EssenceDefinitions,
            services.CreatureEssences,
            craftingDefinitions: services.CraftingDefinitions);
        var generator = new CombatCharacterProfileService(
            services.Factory,
            new CombatCharacterProfileMaterializer(
                services.Factory,
                new CombatPreparationPipeline(new SnapshotCombatantBuilder(db, setup), setup),
                services.EssenceDefinitions),
            services.EssenceDefinitions,
            services.AbilityCatalog);
        var contentHash = AbilityBalanceContentFingerprint.Create(
            services.AbilityCatalog,
            services.EssenceDefinitions);

        var report = await generator.GenerateAsync(
            new CombatCharacterProfileGenerationRequest(
                "audit-expanded",
                CreateExpandedAudit(contentHash, services.EssenceDefinitions),
                ContentType: "Dungeon",
                TeamsPerFamily: 1,
                RandomSeed: 8471,
                PortfolioMode: "Expanded",
                MinimumSourceBattles: 100,
                MaximumConfidenceWidth95: 0.25,
                MaximumSeedScoreSpread: 0.15,
                MaximumEssenceOverlap: 0.20,
                RequireMultiSeedStability: true),
            CancellationToken.None);

        Assert.Equal("Expanded", report.PortfolioMode);
        Assert.Equal(15, report.Teams.Count);
        Assert.Equal(15, report.Teams.Select(team => team.SourceSignature).Distinct().Count());
        Assert.Contains(report.Teams, team => team.Family == "Budget");
        Assert.Contains(report.Teams, team => team.Family == "Counter");
        Assert.Contains(report.Teams, team => team.Family == "Countered");
        Assert.Equal(2, report.Teams.Count(team => team.Family == "EqualPowerAdversarial"));

        var meta = Assert.Single(report.Teams, team => team.Family == "Meta");
        Assert.Equal(["Guardian", "Restorer", "Striker"], meta.Profiles.Select(profile => profile.Role));
        Assert.Equal(["Defensive", "Sustain", "Offense"], meta.Profiles.Select(profile => profile.EquipmentProfile));

        foreach (var role in Enum.GetNames<CanonicalCooperativeRole>())
        {
            var specialist = Assert.Single(report.Teams, team => team.Family == $"RoleSpecialist.{role}");
            Assert.All(specialist.Profiles, profile => Assert.Equal(role, profile.Role));
        }

        var counter = Assert.Single(report.Teams, team => team.Family == "Counter");
        var countered = Assert.Single(report.Teams, team => team.Family == "Countered");
        Assert.Equal(countered.SourceSignature, counter.AdversarySourceSignature);
        Assert.Equal(counter.SourceSignature, countered.AdversarySourceSignature);
        Assert.Equal(300, counter.AdversaryBattles);
        Assert.Equal(1, counter.AdversaryScore!.Value, precision: 10);
        Assert.Equal(0, countered.AdversaryScore!.Value, precision: 10);
        Assert.True(counter.AdversaryConfidenceLower95 > 0.5);
        Assert.True(countered.AdversaryConfidenceUpper95 < 0.5);
        Assert.InRange(
            counter.AdversaryScore.Value,
            counter.AdversaryConfidenceLower95!.Value,
            counter.AdversaryConfidenceUpper95!.Value);
        Assert.InRange(
            countered.AdversaryScore.Value,
            countered.AdversaryConfidenceLower95!.Value,
            countered.AdversaryConfidenceUpper95!.Value);
        Assert.Equal(100, report.MinimumMatchupBattles);

        var noEssence = Assert.Single(report.Teams, team => team.Family == "NoEssence");
        Assert.True(noEssence.IsSyntheticControl);
        Assert.Equal(0, noEssence.SourceBattles);
        Assert.All(noEssence.Profiles, profile =>
        {
            Assert.Empty(profile.EssenceIds);
            Assert.Empty(profile.Prepared.EssenceIds);
            Assert.True(profile.Prepared.IsProductionReady);
        });
        Assert.All(report.Teams.Where(team => !team.IsSyntheticControl), team =>
        {
            Assert.True(team.SourceBattles >= report.MinimumSourceBattles);
            Assert.True(team.ConfidenceUpper95 - team.ConfidenceLower95 <= report.MaximumConfidenceWidth95);
            Assert.True(team.SeedScoreMaximum - team.SeedScoreMinimum <= report.MaximumSeedScoreSpread);
            Assert.True(team.NearestSelectedEssenceOverlap <= report.MaximumEssenceOverlap);
            Assert.False(string.IsNullOrWhiteSpace(team.SelectionReason));
        });

        var nonCommonEssences = services.EssenceDefinitions.GetAll()
            .Where(definition => definition.Rarity != Rarity.Common)
            .Select(definition => definition.Id)
            .ToArray();
        Assert.NotEmpty(nonCommonEssences);
        var singleBudgetAudit = CreateExpandedAudit(contentHash, services.EssenceDefinitions);
        singleBudgetAudit = singleBudgetAudit with
        {
            Finalists = singleBudgetAudit.Finalists.Select((candidate, index) =>
                index == singleBudgetAudit.Finalists.Count - 1
                    ? candidate
                    : candidate with
                    {
                        Participants = candidate.Participants.Select((participant, participantIndex) =>
                            participantIndex == 0
                                ? participant with
                                {
                                    EssenceIds = [nonCommonEssences[index % nonCommonEssences.Length]]
                                }
                                : participant).ToArray()
                    }).ToArray()
        };
        var singleBudgetReport = await generator.GenerateAsync(
            new CombatCharacterProfileGenerationRequest(
                "audit-expanded-single-budget",
                singleBudgetAudit,
                ContentType: "Dungeon",
                TeamsPerFamily: 1,
                RandomSeed: 8471,
                PortfolioMode: "Expanded",
                MinimumSourceBattles: 100,
                MinimumMatchupBattles: 100,
                MaximumConfidenceWidth95: 0.25,
                MaximumSeedScoreSpread: 0.15,
                MaximumEssenceOverlap: 0.20,
                RequireMultiSeedStability: true),
            CancellationToken.None);
        var singleBudget = Assert.Single(singleBudgetReport.Teams, team => team.Family == "Budget");
        var alternateWeak = Assert.Single(singleBudgetReport.Teams, team => team.Family == "WeakButLegal");
        Assert.Equal("expanded-17", singleBudget.SourceSignature);
        Assert.NotEqual(singleBudget.SourceSignature, alternateWeak.SourceSignature);

        var catalogService = new JsonCombatCharacterProfileCatalogService(
            Path.Combine(FindApiContentRoot(), "Data", "combat", "combat-character-profiles.json"),
            services.JsonOptions,
            services.Factory,
            new CombatCharacterProfileMaterializer(
                services.Factory,
                new CombatPreparationPipeline(new SnapshotCombatantBuilder(db, setup), setup),
                services.EssenceDefinitions),
            services.EssenceDefinitions,
            services.AbilityCatalog);
        var validation = await catalogService.ValidateAsync(
            new CombatCharacterProfileCatalogDocument(1, 1, [report]),
            CancellationToken.None);
        Assert.True(
            validation.IsValid,
            string.Join(Environment.NewLine, validation.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
    }

    [Fact]
    public async Task Catalog_rebuilds_profiles_and_rejects_stale_or_drifted_artifacts()
    {
        var services = CreateServices();
        await using var db = CreateDbContext();
        db.ItemBases.AddRange(services.CraftingDefinitions.GetEquipmentBases().Values);
        await db.SaveChangesAsync();
        var setup = new CombatSetupService(
            null!,
            services.EssenceResolver,
            services.EssenceDefinitions,
            services.CreatureEssences,
            craftingDefinitions: services.CraftingDefinitions);
        var pipeline = new CombatPreparationPipeline(
            new SnapshotCombatantBuilder(db, setup),
            setup);
        var materializer = new CombatCharacterProfileMaterializer(
            services.Factory,
            pipeline,
            services.EssenceDefinitions);
        var generator = new CombatCharacterProfileService(
            services.Factory,
            materializer,
            services.EssenceDefinitions,
            services.AbilityCatalog);
        var currentHash = AbilityBalanceContentFingerprint.Create(
            services.AbilityCatalog,
            services.EssenceDefinitions);
        var generationRequest = new CombatCharacterProfileGenerationRequest(
            "audit-catalog",
            CreateAudit(currentHash),
            ContentType: "Dungeon",
            PortfolioMode: "Core",
            MinimumSourceBattles: 1,
            MaximumConfidenceWidth95: 1,
            MaximumSeedScoreSpread: 1,
            MaximumEssenceOverlap: 1,
            RequireMultiSeedStability: false);
        var generated = await generator.GenerateAsync(generationRequest, CancellationToken.None);
        var catalogService = new JsonCombatCharacterProfileCatalogService(
            Path.Combine(FindApiContentRoot(), "Data", "combat", "combat-character-profiles.json"),
            services.JsonOptions,
            services.Factory,
            materializer,
            services.EssenceDefinitions,
            services.AbilityCatalog);
        var document = new CombatCharacterProfileCatalogDocument(1, 1, [generated]);

        var valid = await catalogService.ValidateAsync(document, CancellationToken.None);
        var approved = await catalogService.GetApprovedAsync(CancellationToken.None);

        Assert.True(valid.IsValid);
        Assert.Empty(valid.Issues);
        Assert.True(approved.IsValid);
        Assert.Contains(approved.Issues, issue => issue.Code == "CatalogHasNoProfiles");
        var batch = await new CombatCharacterProfileBatchService(generator, catalogService)
            .GenerateCatalogAsync(
                new CombatCharacterProfileBatchGenerationRequest([
                    generationRequest,
                    generationRequest with
                    {
                        AuditId = "audit-catalog-fine",
                        EquipmentQuality = "Fine"
                    }
                ]),
                CancellationToken.None);
        Assert.Equal(2, batch.RequestedScenarioCount);
        Assert.True(batch.CatalogValidation.IsValid);
        Assert.Contains(
            batch.CatalogValidation.NormalizedCatalog.ProfileSets,
            set => set.Scenario == generated.Scenario);
        Assert.Equal(
            2,
            batch.CatalogValidation.NormalizedCatalog.ProfileSets
                .Select(set => set.Scenario!.Id)
                .Distinct()
                .Count());
        Assert.Equal(
            batch.CatalogValidation.NormalizedCatalog.ProfileSets.Sum(set => set.Teams.Count),
            batch.CatalogValidation.NormalizedCatalog.ProfileSets
                .SelectMany(set => set.Teams)
                .Select(team => team.Id)
                .Distinct()
                .Count());

        var originalTeam = generated.Teams[0];
        var originalProfile = originalTeam.Profiles[0];
        var driftedProfile = originalProfile with
        {
            RawPowerRating = originalProfile.RawPowerRating + 100,
            Prepared = originalProfile.Prepared with
            {
                MaxHealth = originalProfile.Prepared.MaxHealth + 1
            }
        };
        var driftedTeam = originalTeam with
        {
            Profiles = [driftedProfile, .. originalTeam.Profiles.Skip(1)]
        };
        var driftedSet = generated with
        {
            Teams = [driftedTeam, .. generated.Teams.Skip(1)]
        };
        var drifted = await catalogService.ValidateAsync(
            new CombatCharacterProfileCatalogDocument(1, 1, [driftedSet]),
            CancellationToken.None);

        Assert.False(drifted.IsValid);
        Assert.Contains(drifted.Issues, issue => issue.Code == "PowerRatingDrift");
        Assert.Contains(drifted.Issues, issue => issue.Code == "PreparedCombatantDrift");
        Assert.Equal(
            originalProfile.RawPowerRating,
            drifted.NormalizedCatalog.ProfileSets[0].Teams[0].Profiles[0].RawPowerRating);

        var stale = await catalogService.ValidateAsync(
            new CombatCharacterProfileCatalogDocument(
                1,
                1,
                [generated with { SourceContentHash = "stale-content" }]),
            CancellationToken.None);

        Assert.False(stale.IsValid);
        Assert.Contains(stale.Issues, issue => issue.Code == "ContentHashStale");

        var duplicateScenario = await catalogService.ValidateAsync(
            new CombatCharacterProfileCatalogDocument(
                1,
                1,
                [generated, generated with { AuditId = "same-scenario-second-audit" }]),
            CancellationToken.None);
        Assert.False(duplicateScenario.IsValid);
        Assert.Contains(duplicateScenario.Issues, issue => issue.Code == "DuplicateProfileScenario");

        var mismatchedEssenceScenario = generated with
        {
            Scenario = generated.Scenario! with
            {
                Id = CombatCharacterProfileScenario.CreateId(
                    generated.ContentType,
                    generated.Scenario!.TeamSize,
                    generated.Scenario.EquipmentTier,
                    generated.Scenario.EquipmentRarity,
                    generated.Scenario.EquipmentQuality,
                    generated.Scenario.AuditEquipmentProfile,
                    generated.Scenario.EssencesPerParticipant + 1),
                EssencesPerParticipant = generated.Scenario.EssencesPerParticipant + 1
            }
        };
        var mismatched = await catalogService.ValidateAsync(
            new CombatCharacterProfileCatalogDocument(1, 1, [mismatchedEssenceScenario]),
            CancellationToken.None);
        Assert.False(mismatched.IsValid);
        Assert.Contains(mismatched.Issues, issue => issue.Code == "ScenarioEssenceCountMismatch");
    }

    private static AbilityBalanceAuditReport CreateAudit(string contentHash)
    {
        var finalists = new[]
        {
            Combination("meta", "essence.goblin", wins: 90, losses: 10),
            Combination("typical", "essence.raven", wins: 50, losses: 50),
            Combination("weak", "essence.green_slime", wins: 10, losses: 90)
        };
        return new AbilityBalanceAuditReport(
            contentHash,
            ScreeningBattlesRun: 300,
            ValidationBattlesRun: 0,
            FinalistBattlesRun: 300,
            TotalBattlesRun: 600,
            CandidateTeamsTested: 3,
            FinalistTeamCount: 3,
            EquipmentTier: 1,
            EquipmentRarity: "Epic",
            EquipmentProfile: "Balanced",
            ParticipantAttributes: new Dictionary<string, float>(),
            EssenceResults: [],
            FinalistEssenceResults: [],
            ValidationResults: [],
            Finalists: finalists);
    }

    private static AbilityBalanceAuditReport CreatePartyAudit(string contentHash)
    {
        var finalists = new[]
        {
            Combination("party-meta", "essence.goblin", wins: 90, losses: 10, teamSize: 5),
            Combination("party-typical", "essence.raven", wins: 50, losses: 50, teamSize: 5),
            Combination("party-weak", "essence.green_slime", wins: 10, losses: 90, teamSize: 5)
        };
        return new AbilityBalanceAuditReport(
            contentHash,
            ScreeningBattlesRun: 300,
            ValidationBattlesRun: 0,
            FinalistBattlesRun: 300,
            TotalBattlesRun: 600,
            CandidateTeamsTested: 3,
            FinalistTeamCount: 3,
            EquipmentTier: 1,
            EquipmentRarity: "Epic",
            EquipmentProfile: "Balanced",
            ParticipantAttributes: new Dictionary<string, float>(),
            EssenceResults: [],
            FinalistEssenceResults: [],
            ValidationResults: [],
            Finalists: finalists);
    }

    private static AbilityBalanceAuditReport CreateExpandedAudit(
        string contentHash,
        IEssenceDefinitionRepository essenceDefinitions)
    {
        int[] wins = [270, 255, 240, 225, 210, 195, 180, 168, 162, 159, 156, 153, 150, 147, 135, 120, 90, 30];
        var commonEssences = essenceDefinitions.GetAll()
            .Where(definition => definition.Rarity == Rarity.Common)
            .Select(definition => definition.Id)
            .Take(wins.Length * 3)
            .ToArray();
        Assert.Equal(wins.Length * 3, commonEssences.Length);
        var finalists = wins.Select((winCount, index) =>
        {
            var score = winCount / 300d;
            return new AbilityBalanceCombinationResult(
                $"expanded-{index:D2}",
                $"Expanded {index:D2}",
                Enumerable.Range(0, 3)
                    .Select(slot => new AbilityBalanceParticipantLoadout([commonEssences[index * 3 + slot]]))
                    .ToArray(),
                Battles: 300,
                Wins: winCount,
                Losses: 300 - winCount,
                Draws: 0,
                WinRate: score,
                LossRate: 1d - score,
                DrawRate: 0,
                AverageDuration: 100,
                AverageDamageDone: 100,
                AverageDamageTaken: 100,
                SeedResults:
                [
                    new AbilityBalanceSeedResult(1337, 100, Math.Clamp(score - 0.01, 0, 1)),
                    new AbilityBalanceSeedResult(2027, 100, score),
                    new AbilityBalanceSeedResult(9001, 100, Math.Clamp(score + 0.01, 0, 1))
                ]);
        }).ToArray();
        AbilityBalanceMatchupResult[] matchups =
        [
            new("expanded-02", "expanded-03", 300, 300, 0, 0, 1.00),
            new("expanded-04", "expanded-05", 300, 270, 30, 0, 0.90)
        ];

        return new AbilityBalanceAuditReport(
            contentHash,
            ScreeningBattlesRun: 5400,
            ValidationBattlesRun: 0,
            FinalistBattlesRun: 5400,
            TotalBattlesRun: 10800,
            CandidateTeamsTested: finalists.Length,
            FinalistTeamCount: finalists.Length,
            EquipmentTier: 1,
            EquipmentRarity: "Epic",
            EquipmentProfile: "Balanced",
            ParticipantAttributes: new Dictionary<string, float>(),
            EssenceResults: [],
            FinalistEssenceResults: [],
            ValidationResults: [],
            Finalists: finalists,
            RandomSeeds: [1337, 2027, 9001],
            FinalistMatchups: matchups);
    }

    private static AbilityBalanceAuditReport CreateExpandedPartyAudit(
        string contentHash,
        IEssenceDefinitionRepository essenceDefinitions)
    {
        int[] wins = [270, 255, 240, 225, 210, 195, 180, 168, 162, 159, 156, 153, 150, 147, 135, 120, 90, 30];
        var commonEssences = essenceDefinitions.GetAll()
            .Where(definition => definition.Rarity == Rarity.Common)
            .Select(definition => definition.Id)
            .Take(wins.Length)
            .ToArray();
        Assert.Equal(wins.Length, commonEssences.Length);
        var finalists = wins.Select((winCount, index) =>
        {
            var score = winCount / 300d;
            return new AbilityBalanceCombinationResult(
                $"expanded-party-{index:D2}",
                $"Expanded Party {index:D2}",
                Enumerable.Range(0, 5)
                    .Select(_ => new AbilityBalanceParticipantLoadout([commonEssences[index]]))
                    .ToArray(),
                Battles: 300,
                Wins: winCount,
                Losses: 300 - winCount,
                Draws: 0,
                WinRate: score,
                LossRate: 1d - score,
                DrawRate: 0,
                AverageDuration: 100,
                AverageDamageDone: 100,
                AverageDamageTaken: 100,
                SeedResults:
                [
                    new AbilityBalanceSeedResult(1337, 100, Math.Clamp(score - 0.01, 0, 1)),
                    new AbilityBalanceSeedResult(2027, 100, score),
                    new AbilityBalanceSeedResult(9001, 100, Math.Clamp(score + 0.01, 0, 1))
                ]);
        }).ToArray();
        AbilityBalanceMatchupResult[] matchups =
        [
            new("expanded-party-02", "expanded-party-03", 300, 300, 0, 0, 1.00),
            new("expanded-party-04", "expanded-party-05", 300, 270, 30, 0, 0.90)
        ];

        return new AbilityBalanceAuditReport(
            contentHash,
            ScreeningBattlesRun: 5400,
            ValidationBattlesRun: 0,
            FinalistBattlesRun: 5400,
            TotalBattlesRun: 10800,
            CandidateTeamsTested: finalists.Length,
            FinalistTeamCount: finalists.Length,
            EquipmentTier: 1,
            EquipmentRarity: "Epic",
            EquipmentProfile: "Balanced",
            ParticipantAttributes: new Dictionary<string, float>(),
            EssenceResults: [],
            FinalistEssenceResults: [],
            ValidationResults: [],
            Finalists: finalists,
            RandomSeeds: [1337, 2027, 9001],
            FinalistMatchups: matchups);
    }

    private static AbilityBalanceCombinationResult Combination(
        string id,
        string essenceId,
        int wins,
        int losses,
        int teamSize = 1) =>
        new(
            id,
            id,
            Enumerable.Range(0, teamSize)
                .Select(_ => new AbilityBalanceParticipantLoadout([essenceId]))
                .ToArray(),
            Battles: wins + losses,
            Wins: wins,
            Losses: losses,
            Draws: 0,
            WinRate: wins / (double)(wins + losses),
            LossRate: losses / (double)(wins + losses),
            DrawRate: 0,
            AverageDuration: 100,
            AverageDamageDone: 100,
            AverageDamageTaken: 100);

    private static TestServices CreateServices()
    {
        var contentRoot = FindApiContentRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        var essenceDefinitions = new JsonEssenceDefinitionRepository(
            configuration,
            contentRoot,
            jsonOptions,
            new EssenceDefinitionValidator());
        var creatureEssences = new JsonCreatureEssenceLootTableRepository(
            configuration,
            contentRoot,
            jsonOptions,
            essenceDefinitions);
        var essenceResolver = new EssenceSystemService(
            null!, null!, null!, essenceDefinitions, creatureEssences,
            null!, null!, null!, null!, null!, null!);
        var balance = Options.Create(new CraftingBalanceOptions());
        var craftingDefinitions = new JsonCraftingDefinitionProvider(
            configuration,
            contentRoot,
            jsonOptions);
        var factory = new CanonicalEquipmentBuildFactory(
            craftingDefinitions,
            new ItemStatRollService(balance),
            new TemperingMechanicsService(balance),
            new ItemPotentialService(balance),
            essenceResolver,
            essenceDefinitions);
        var abilityCatalog = new JsonAbilityCatalogProvider(
            configuration,
            contentRoot,
            jsonOptions);
        return new TestServices(
            jsonOptions,
            craftingDefinitions,
            essenceDefinitions,
            creatureEssences,
            essenceResolver,
            factory,
            abilityCatalog);
    }

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }

    private static string FindApiContentRoot()
    {
        var configured = Environment.GetEnvironmentVariable("LL_TEST_CONTENT_ROOT");
        if (!string.IsNullOrWhiteSpace(configured)
            && Directory.Exists(Path.Combine(configured, "Data")))
            return configured;

        var workingTreeCandidate = Path.Combine(
            Directory.GetCurrentDirectory(),
            "LL",
            "src",
            "API",
            "API.LL");
        if (Directory.Exists(Path.Combine(workingTreeCandidate, "Data")))
            return workingTreeCandidate;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "API", "API.LL");
            if (Directory.Exists(Path.Combine(candidate, "Data")))
                return candidate;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate API.LL content root.");
    }

    private sealed record TestServices(
        JsonSerializerOptions JsonOptions,
        JsonCraftingDefinitionProvider CraftingDefinitions,
        JsonEssenceDefinitionRepository EssenceDefinitions,
        JsonCreatureEssenceLootTableRepository CreatureEssences,
        EssenceSystemService EssenceResolver,
        CanonicalEquipmentBuildFactory Factory,
        JsonAbilityCatalogProvider AbilityCatalog);

    private sealed class StubWorldTowerProfileCandidateQualifier(
        IReadOnlyDictionary<string, double> scores) : IWorldTowerProfileCandidateQualifier
    {
        public CombatCharacterProfileScenario? Scenario { get; private set; }
        public int SampleCount { get; private set; }

        public Task<IReadOnlyDictionary<string, IReadOnlyList<CombatCharacterProfileContextEvidence>>>
            QualifyAsync(
                IReadOnlyList<AbilityBalanceCombinationResult> candidates,
                CombatCharacterProfileScenario scenario,
                int sampleCount,
                int baseRandomSeed,
                CancellationToken cancellationToken)
        {
            Scenario = scenario;
            SampleCount = sampleCount;
            var result = candidates.ToDictionary(
                candidate => candidate.Signature,
                candidate => (IReadOnlyList<CombatCharacterProfileContextEvidence>)
                [
                    CreateEvidence(candidate)
                ],
                StringComparer.Ordinal);
            return Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<CombatCharacterProfileContextEvidence>>>(
                result);

            CombatCharacterProfileContextEvidence CreateEvidence(
                AbilityBalanceCombinationResult candidate)
            {
                var wins = (int)Math.Round(scores[candidate.Signature] * sampleCount);
                return new(
                        scenario.Id,
                        scenario.FloorNumbers!.Single(),
                        scenario.TeamSize,
                        sampleCount,
                        Wins: wins,
                        Losses: sampleCount - wins,
                        Draws: 0,
                        WinRate: wins / (double)sampleCount,
                        TimeoutRate: 0,
                        AverageDurationTicks: 100,
                        SeedManifestId: $"test:{baseRandomSeed}",
                        SeedManifestHash: new string('a', 64),
                        UsesProductionRuntime: true,
                        AbilitiesStartOnCooldown: true);
            }
        }
    }
}
