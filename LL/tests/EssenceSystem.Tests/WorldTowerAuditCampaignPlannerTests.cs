using Services.AdminDashboard.Combat;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class WorldTowerAuditCampaignPlannerTests
{
    [Fact]
    public void Planner_reuses_five_member_discovery_across_roster_and_equipment_scenarios()
    {
        var requirements = new[]
        {
            Requirement("five-standard", "Standard", essenceCount: 7, teamSize: 5, equipmentTier: 1),
            Requirement("fifteen-fine", "Fine", essenceCount: 7, teamSize: 15, equipmentTier: 8),
            Requirement("eight-essences", "Fine", essenceCount: 8)
        };

        var plan = WorldTowerAuditCampaignPlanner.Create(
            requirements,
            new WorldTowerAuditCampaignOptions(FinalistBattleCount: 50, RandomSeeds: [17, 29]));

        Assert.Equal(3, plan.Scenarios.Count);
        Assert.Equal(2, plan.Audits.Count);
        var sharedWorkIds = plan.Scenarios.Take(2)
            .Select(scenario => scenario.AuditWorkId)
            .Distinct()
            .ToArray();
        Assert.Single(sharedWorkIds);
        Assert.NotEqual(sharedWorkIds[0], plan.Scenarios[2].AuditWorkId);
        var sharedAudit = plan.Audits.Single(audit => audit.Id == sharedWorkIds[0]);
        Assert.Equal(2, sharedAudit.ScenarioIds.Count);
        Assert.Equal([17, 29], sharedAudit.Request.RandomSeeds);
        Assert.Equal(5, sharedAudit.Request.TeamSize);
        Assert.Equal(7, sharedAudit.Request.EssencesPerParticipant);
        Assert.Equal(1, sharedAudit.Request.EquipmentTier);
        Assert.Equal("Epic", sharedAudit.Request.EquipmentRarity);
        Assert.Equal("Balanced", sharedAudit.Request.EquipmentProfile);
    }

    [Fact]
    public void Planner_creates_one_party_audit_per_distinct_essence_slot_count()
    {
        var requirements = Enumerable.Range(5, 5)
            .SelectMany(essenceCount => new[]
            {
                Requirement($"{essenceCount}-five", "Standard", essenceCount, teamSize: 5),
                Requirement($"{essenceCount}-ten", "Fine", essenceCount, teamSize: 10),
                Requirement($"{essenceCount}-fifteen", "Exceptional", essenceCount, teamSize: 15)
            })
            .ToArray();

        var plan = WorldTowerAuditCampaignPlanner.Create(
            requirements,
            new WorldTowerAuditCampaignOptions());

        Assert.Equal(15, plan.Scenarios.Count);
        Assert.Equal(5, plan.Audits.Count);
        Assert.All(plan.Audits, audit => Assert.Equal(5, audit.Request.TeamSize));
        Assert.Equal([5, 6, 7, 8, 9], plan.Audits
            .Select(audit => audit.Request.EssencesPerParticipant)
            .Order()
            .ToArray());
        Assert.All(plan.Audits, audit => Assert.Equal(34, audit.Request.FinalistBattleCount));
    }

    [Fact]
    public void Planner_rejects_campaigns_that_cannot_meet_matchup_evidence_threshold()
    {
        var exception = Assert.Throws<ArgumentException>(() => WorldTowerAuditCampaignPlanner.Create(
            [Requirement("insufficient-matchups", "Standard", essenceCount: 5, teamSize: 5)],
            new WorldTowerAuditCampaignOptions(
                FinalistBattleCount: 10,
                RandomSeeds: [1337, 2027, 9001],
                MinimumMatchupBattles: 100)));

        Assert.Contains("only 30 battles per finalist matchup", exception.Message);
        Assert.Contains("requires 100", exception.Message);
    }

    [Fact]
    public void Planner_is_deterministic_regardless_of_requirement_order()
    {
        var requirements = new[]
        {
            Requirement("standard", "Standard", essenceCount: 7),
            Requirement("fine", "Fine", essenceCount: 7),
            Requirement("eight-essences", "Fine", essenceCount: 8)
        };
        var options = new WorldTowerAuditCampaignOptions();

        var first = WorldTowerAuditCampaignPlanner.Create(requirements, options);
        var reversed = WorldTowerAuditCampaignPlanner.Create(requirements.Reverse().ToArray(), options);

        Assert.Equal(
            first.Audits.Select(audit => audit.Id),
            reversed.Audits.Select(audit => audit.Id));
        Assert.Equal(
            first.Scenarios.OrderBy(scenario => scenario.Requirement.ScenarioId)
                .Select(scenario => (scenario.Requirement.ScenarioId, scenario.AuditWorkId)),
            reversed.Scenarios.OrderBy(scenario => scenario.Requirement.ScenarioId)
                .Select(scenario => (scenario.Requirement.ScenarioId, scenario.AuditWorkId)));
    }

    [Theory]
    [InlineData("old-discovery", "materialization", "new-discovery", "materialization", WorldTowerBalancingReuseMode.RunDiscovery)]
    [InlineData("discovery", "old-materialization", "discovery", "new-materialization", WorldTowerBalancingReuseMode.RebuildProfiles)]
    [InlineData("discovery", "materialization", "discovery", "materialization", WorldTowerBalancingReuseMode.ReuseProfiles)]
    public void Dependency_planner_selects_the_minimum_safe_rerun(
        string previousDiscovery,
        string previousMaterialization,
        string currentDiscovery,
        string currentMaterialization,
        WorldTowerBalancingReuseMode expected)
    {
        Assert.Equal(expected, WorldTowerBalancingDependencyPlanner.Decide(
            previousDiscovery,
            previousMaterialization,
            currentDiscovery,
            currentMaterialization));
    }

    private static WorldTowerProfileScenarioRequirement Requirement(
        string suffix,
        string quality,
        int essenceCount,
        int teamSize = 10,
        int equipmentTier = 2) => new(
        $"scenario-{suffix}",
        [11],
        teamSize,
        equipmentTier,
        "Epic",
        quality,
        "Balanced",
        essenceCount,
        220d,
        215,
        225);
}
