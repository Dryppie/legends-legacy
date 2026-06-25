using Application.Interfaces.Services.LL.Prophecies;
using Domain.Models.Prophecies;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Prophecies;

public sealed class JsonProphecyDefinitionProvider : IProphecyDefinitionProvider
{
    private static readonly IReadOnlySet<string> KnownObjectiveTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        ProphecyObjectiveType.KillCreatures,
        ProphecyObjectiveType.KillDifferentCreatureTypes,
        ProphecyObjectiveType.WinEncounters,
        ProphecyObjectiveType.ClearDungeonRooms,
        ProphecyObjectiveType.CompleteDungeons,
        ProphecyObjectiveType.ResolveDungeonEvents,
        ProphecyObjectiveType.GainEssenceXp,
        ProphecyObjectiveType.EssenceArchivedOrFed,
        ProphecyObjectiveType.GatherResources,
        ProphecyObjectiveType.TemperItems,
        ProphecyObjectiveType.SpendPotential,
        ProphecyObjectiveType.TreasureProgress,
        ProphecyObjectiveType.MeaningfulDefeatThenWins
    };

    private readonly IReadOnlyList<ProphecyDefinition> _definitions;

    public JsonProphecyDefinitionProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "prophecies.json");
        var document = JsonSerializer.Deserialize<ProphecyDefinitionDocument>(
            File.ReadAllText(path),
            options) ?? new();

        ThrowIfInvalid(document.Definitions);
        _definitions = document.Definitions;
    }

    public IReadOnlyList<ProphecyDefinition> GetAll() => _definitions;

    private static void ThrowIfInvalid(IReadOnlyList<ProphecyDefinition> definitions)
    {
        if (definitions.Count == 0)
        {
            throw new InvalidOperationException("Prophecy definitions file must contain at least one definition.");
        }

        var duplicates = definitions
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException("Duplicate prophecy definition ids: " + string.Join(", ", duplicates));
        }

        var missingRequiredFields = definitions
            .Where(x =>
                string.IsNullOrWhiteSpace(x.Id) ||
                string.IsNullOrWhiteSpace(x.Title) ||
                string.IsNullOrWhiteSpace(x.FlavorText) ||
                string.IsNullOrWhiteSpace(x.ObjectiveText) ||
                string.IsNullOrWhiteSpace(x.ObjectiveType) ||
                string.IsNullOrWhiteSpace(x.RewardProfileId))
            .Select(x => string.IsNullOrWhiteSpace(x.Id) ? "<missing id>" : x.Id)
            .ToList();

        if (missingRequiredFields.Count > 0)
        {
            throw new InvalidOperationException("Prophecy definitions require id, title, flavor text, objective text, objective type, and reward profile id: " + string.Join(", ", missingRequiredFields));
        }

        var missingSlots = definitions
            .Where(x => x.AllowedSlots.Count == 0)
            .Select(x => x.Id)
            .ToList();

        if (missingSlots.Count > 0)
        {
            throw new InvalidOperationException("Prophecy definitions require at least one allowed slot: " + string.Join(", ", missingSlots));
        }

        var invalidSlots = definitions
            .SelectMany(definition => definition.AllowedSlots
                .Where(slot => !Enum.TryParse<ProphecySlotType>(slot, ignoreCase: false, out _))
                .Select(slot => $"{definition.Id}:{slot}"))
            .ToList();

        if (invalidSlots.Count > 0)
        {
            throw new InvalidOperationException("Prophecy definitions contain invalid allowed slots: " + string.Join(", ", invalidSlots));
        }

        var invalidObjectiveTypes = definitions
            .Where(x => !KnownObjectiveTypes.Contains(x.ObjectiveType))
            .Select(x => $"{x.Id}:{x.ObjectiveType}")
            .ToList();

        if (invalidObjectiveTypes.Count > 0)
        {
            throw new InvalidOperationException("Prophecy definitions contain invalid objective types: " + string.Join(", ", invalidObjectiveTypes));
        }

        var invalidWeights = definitions
            .Where(x => x.Weight <= 0)
            .Select(x => x.Id)
            .ToList();

        if (invalidWeights.Count > 0)
        {
            throw new InvalidOperationException("Prophecy definition weights must be greater than zero: " + string.Join(", ", invalidWeights));
        }
    }

    private sealed class ProphecyDefinitionDocument
    {
        public List<ProphecyDefinition> Definitions { get; set; } = [];
    }
}
