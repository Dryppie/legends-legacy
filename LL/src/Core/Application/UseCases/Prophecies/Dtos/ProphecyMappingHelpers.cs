using Application.Interfaces.Services.LL.Prophecies;
using AutoMapper;
using Domain.Models.Prophecies;
using System.Text.Json;

namespace Application.UseCases.Prophecies.Dtos;

internal static class ProphecyMappingHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ProphecyDefinition Definition(PlayerProphecyInstance instance) =>
        instance.ProphecyDefinition ?? new ProphecyDefinition
        {
            Id = instance.ProphecyDefinitionId,
            Title = instance.ProphecyDefinitionId,
            FlavorText = string.Empty,
            ObjectiveText = string.Empty,
            ObjectiveType = string.Empty
        };

    public static ProphecyRewardSnapshot ReadReward(string json) =>
        JsonSerializer.Deserialize<ProphecyRewardSnapshot>(json, JsonOptions) ?? new ProphecyRewardSnapshot();

    public static ProphecyGuidanceDto Guidance(PlayerProphecyInstance instance)
    {
        var definition = Definition(instance);
        ProphecyObjectiveParameters.TryParse(instance.ObjectiveParameterSnapshotJson, out var parameters);

        return definition.ObjectiveType switch
        {
            ProphecyObjectiveType.ClearDungeonRooms => CreateGuidance(
                ProphecyGuidanceDestination.Dungeons,
                "Run Dungeons",
                "Enter dungeons and clear rooms to progress this prophecy."),
            ProphecyObjectiveType.CompleteDungeons => CreateGuidance(
                ProphecyGuidanceDestination.Dungeons,
                "Run Dungeons",
                "Complete full dungeon runs to progress this prophecy."),
            ProphecyObjectiveType.GainEssenceXp => CreateGuidance(
                ProphecyGuidanceDestination.Essences,
                "Train Essences",
                "Equip or absorb Essences, then earn Essence XP."),
            ProphecyObjectiveType.AbsorbEssence => CreateGuidance(
                ProphecyGuidanceDestination.SoulArchive,
                "Open Archive",
                "Absorb one new Unbound Essence into the Soul Archive."),
            ProphecyObjectiveType.GatherResources => CreateGatheringGuidance(parameters.RequiredProfession),
            ProphecyObjectiveType.TemperItems => CreateGuidance(
                ProphecyGuidanceDestination.Crafting,
                "Temper Gear",
                "Complete tempering actions to progress this prophecy."),
            ProphecyObjectiveType.SpendPotential => CreateGuidance(
                ProphecyGuidanceDestination.Crafting,
                "Temper Gear",
                "Spend item Potential through tempering to progress this prophecy."),
            ProphecyObjectiveType.TreasureProgress => CreateGuidance(
                ProphecyGuidanceDestination.Dungeons,
                "Seek Treasure",
                "Items found during world and dungeon combat progress this prophecy."),
            ProphecyObjectiveType.MeaningfulDefeatThenWins => CreateGuidance(
                ProphecyGuidanceDestination.WorldCombat,
                "Return To Battle",
                "After a meaningful defeat, win qualifying encounters."),
            ProphecyObjectiveType.KillDifferentCreatureTypes => CreateGuidance(
                ProphecyGuidanceDestination.WorldCombat,
                "Fight Encounters",
                "Defeat different creature types in world encounters."),
            ProphecyObjectiveType.WinEncounters => CreateWinEncounterGuidance(parameters.MinimumEnemyCount),
            ProphecyObjectiveType.KillCreatures => CreateGuidance(
                ProphecyGuidanceDestination.WorldCombat,
                "Fight Encounters",
                "Every creature you defeat in combat counts toward this prophecy."),
            _ => new ProphecyGuidanceDto()
        };
    }

    private static ProphecyGuidanceDto CreateGatheringGuidance(string? requiredProfession)
    {
        var profession = requiredProfession?.Trim();
        return string.IsNullOrWhiteSpace(profession)
            ? CreateGuidance(
                ProphecyGuidanceDestination.Gathering,
                "Gather Resources",
                "Gather resources from world activity to progress this prophecy.")
            : CreateGuidance(
                ProphecyGuidanceDestination.Gathering,
                $"Go {profession}",
                $"Only {profession} gathering rewards count toward this prophecy.");
    }

    private static ProphecyGuidanceDto CreateWinEncounterGuidance(int? minimumEnemyCount) =>
        minimumEnemyCount is > 1
            ? CreateGuidance(
                ProphecyGuidanceDestination.WorldCombat,
                "Fight Groups",
                $"Win encounters against groups of at least {minimumEnemyCount.Value} enemies.")
            : CreateGuidance(
                ProphecyGuidanceDestination.WorldCombat,
                "Fight Encounters",
                "Win qualifying combat encounters in the world.");

    private static ProphecyGuidanceDto CreateGuidance(string destination, string actionLabel, string hint) =>
        new()
        {
            Destination = destination,
            ActionLabel = actionLabel,
            Hint = hint
        };

    public static WeeklyRevelationProgressDto MapWeeklyRevelation(
        WeeklyRevelationProgress progress,
        IReadOnlyList<WeeklyRevelationMilestone> milestones,
        ResolutionContext context)
    {
        var dto = context.Mapper.Map<WeeklyRevelationProgressDto>(progress);
        dto.Milestones = milestones
            .Select(context.Mapper.Map<WeeklyRevelationMilestoneDto>)
            .ToList();

        return dto;
    }
}
