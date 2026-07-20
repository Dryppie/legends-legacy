using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Definitions.Events;
using Domain.Models.Dungeons.Runs;

namespace Services.LL.Dungeons;

public sealed class DungeonEventChoiceService : IDungeonEventChoiceService
{
    private readonly IDungeonEventDefinitionProvider _definitions;
    private readonly IDungeonVigorService _vigor;

    public DungeonEventChoiceService(
        IDungeonEventDefinitionProvider definitions,
        IDungeonVigorService vigor)
    {
        _definitions = definitions;
        _vigor = vigor;
    }

    public IReadOnlyList<DungeonEventChoiceOption> EnsureChoices(
        DungeonRun run,
        EventOutcomeType eventOutcome)
    {
        return EnsureChoices(run, run.DungeonDefinitionId, eventOutcome);
    }

    public IReadOnlyList<DungeonEventChoiceOption> EnsureChoices(
        DungeonRun run,
        string dungeonDefinitionId,
        EventOutcomeType eventOutcome)
    {
        ArgumentNullException.ThrowIfNull(run);
        run.State ??= new DungeonRunState { RunId = run.Id };

        if (run.State.CurrentEventChoices.Count > 0)
        {
            return run.State.CurrentEventChoices;
        }

        run.State.CurrentEventChoices = _definitions
            .GetDefinition(dungeonDefinitionId, eventOutcome)
            .Choices
            .Select(choice => ToOption(run, choice))
            .ToList();

        return run.State.CurrentEventChoices;
    }

    public DungeonEventChoiceOption ApplyChoiceState(DungeonRun run, string choiceId)
    {
        ArgumentNullException.ThrowIfNull(run);
        var choice = run.State.CurrentEventChoices
            .FirstOrDefault(x => string.Equals(x.Id, choiceId, StringComparison.OrdinalIgnoreCase));

        if (choice is null)
        {
            throw new InvalidOperationException("The selected event choice is not available.");
        }

        if (choice.MissingRequirements.Count > 0)
        {
            throw new InvalidOperationException("The selected event choice requirements are not met.");
        }

        foreach (var flag in choice.AddFlags)
        {
            AddFlag(run, flag, 1);
        }

        foreach (var flag in choice.RemoveFlags)
        {
            run.State.Flags.Remove(flag);
        }

        if (choice.VigorDelta != 0)
        {
            var room = run.Rooms.FirstOrDefault(candidate => candidate.RoomIndex == run.CurrentRoomIndex)
                ?? throw new InvalidOperationException("The current event room could not be found.");
            _vigor.ApplyEventChange(run, room, choice.VigorDelta, choice.Label);
        }

        if (choice.AmbushChancePercent > 0 && RollsAmbush(run, choice))
        {
            AddFlag(run, "event_ambush_triggered", 1);
        }

        if (choice.RevealsHiddenRoute)
        {
            AddFlag(run, "hidden_route_revealed", 1);
        }

        return choice;
    }

    private static DungeonEventChoiceOption ToOption(
        DungeonRun run,
        DungeonEventChoiceDefinition choice)
    {
        var missingRequirements = GetMissingRequirements(run, choice);

        return new DungeonEventChoiceOption
        {
            Id = choice.Id,
            Label = choice.Label,
            Description = choice.Description,
            VigorDelta = choice.VigorDelta,
            AddFlags = choice.AddFlags.ToList(),
            RemoveFlags = choice.RemoveFlags.ToList(),
            MissingRequirements = missingRequirements,
            GrantsLoot = choice.GrantsLoot,
            AmbushChancePercent = Math.Clamp(choice.AmbushChancePercent, 0, 100),
            RevealsHiddenRoute = choice.RevealsHiddenRoute
        };
    }

    private static List<string> GetMissingRequirements(
        DungeonRun run,
        DungeonEventChoiceDefinition choice)
    {
        var missing = new List<string>();

        foreach (var flag in choice.RequiredFlags)
        {
            if (run.State.Flags.GetValueOrDefault(flag) <= 0)
            {
                missing.Add($"Requires: {FormatFlagRequirement(flag)}");
            }
        }

        foreach (var flag in choice.RequiredMissingFlags)
        {
            if (run.State.Flags.GetValueOrDefault(flag) > 0)
            {
                missing.Add($"Blocked by: {FormatFlagRequirement(flag)}");
            }
        }

        return missing;
    }

    private static string FormatFlagRequirement(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
        {
            return "Unknown requirement";
        }

        return flag switch
        {
            "saved_miner" => "Save Miner",
            "cleansed_shrine" => "Cleanse Shrine",
            "cleansed_tomb" => "Cleanse Tomb",
            "searched_deep_treasure" => "Search Deeper",
            "revealed_hidden_route" => "Reveal Hidden Route",
            "hidden_route_revealed" => "Reveal Hidden Route",
            "collapsed_tunnel" => "Collapse Tunnel",
            "goblin_powder_looted" => "Loot Powder Cache",
            "saved_explosives" => "Save Explosives",
            "opened_reliquary" => "Open Reliquary",
            "sealed_reliquary" => "Seal Reliquary",
            "bound_spirit_power" => "Bind Spirit Power",
            "boss_reinforcements_reduced" => "Reduce Boss Reinforcements",
            _ => string.Join(
                ' ',
                flag.Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => char.ToUpperInvariant(part[0]) + part[1..]))
        };
    }

    private static bool RollsAmbush(DungeonRun run, DungeonEventChoiceOption choice)
    {
        var seed = HashCode.Combine(run.Seed, run.CurrentRoomIndex, choice.Id);
        var rng = new Random(seed);
        return rng.Next(1, 101) <= choice.AmbushChancePercent;
    }

    private static void AddFlag(DungeonRun run, string flag, int amount)
    {
        if (string.IsNullOrWhiteSpace(flag))
        {
            return;
        }

        run.State.Flags[flag] = run.State.Flags.GetValueOrDefault(flag) + amount;
    }
}
