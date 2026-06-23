using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Dungeons.Definitions.Boons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Essences.Definitions;
using System.Globalization;

namespace Services.LL.Dungeons;

public sealed class DungeonBoonService : IDungeonBoonService
{
    private readonly IDungeonBoonDefinitionProvider _definitions;

    public DungeonBoonService(IDungeonBoonDefinitionProvider definitions)
    {
        _definitions = definitions;
    }

    public IReadOnlyList<DungeonBoonDefinition> GetAllDefinitions() => _definitions.GetAll();

    public DungeonBoonDefinition? GetDefinition(string boonId) => _definitions.GetById(boonId);

    public IReadOnlyList<DungeonBoonChoiceOption> GenerateBoonChoices(DungeonRun run, int count = 3)
    {
        ArgumentNullException.ThrowIfNull(run);
        run.State ??= new DungeonRunState { RunId = run.Id };

        var active = run.State.ActiveBoonIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var available = _definitions.GetAll()
            .Where(x => !active.Contains(x.Id))
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToList();

        var random = new Random(CreateRunSeed(run.Seed, run.CurrentRoomIndex, active.Count));
        var choices = PickWeighted(available, Math.Max(1, count), random)
            .Select(ToChoiceOption)
            .ToList();

        run.State.CurrentBoonChoices = choices;
        return choices;
    }

    public void ChooseBoon(DungeonRun run, string boonId)
    {
        ArgumentNullException.ThrowIfNull(run);

        var choice = run.State.CurrentBoonChoices
            .FirstOrDefault(x => string.Equals(x.Id, boonId, StringComparison.OrdinalIgnoreCase));

        if (choice is null || _definitions.GetById(choice.Id) is null)
        {
            throw new InvalidOperationException("The selected boon is no longer available.");
        }

        if (!run.State.ActiveBoonIds.Contains(choice.Id, StringComparer.OrdinalIgnoreCase))
        {
            run.State.ActiveBoonIds.Add(choice.Id);
        }

        run.State.CurrentBoonChoices.Clear();
        SyncActiveBoonState(run);
    }

    public void SyncActiveBoonState(DungeonRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        run.State ??= new DungeonRunState { RunId = run.Id };

        var activeStacks = GetActiveBoonStacks(run).ToList();
        run.State.ActiveBoonSummaries = activeStacks
            .Select(stack => new DungeonActiveBoonSummary
            {
                Id = stack.Definition.Id,
                Name = stack.Definition.Name,
                Description = stack.Definition.Description,
                Rarity = stack.Definition.Rarity.ToString(),
                Count = stack.Count,
                EffectSummaries = CreateEffectSummaries(stack.Definition)
            })
            .ToList();

        run.State.ActiveBoonEffectSummaries = CreateAggregateEffectSummaries(activeStacks);
    }

    public IReadOnlyList<AttributeModifierBase> GetActiveAttributeModifiers(DungeonRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        run.State ??= new DungeonRunState { RunId = run.Id };

        return run.State.ActiveBoonIds
            .Select(_definitions.GetById)
            .Where(x => x is not null)
            .SelectMany(x => x!.AttributeModifiers)
            .Cast<AttributeModifierBase>()
            .ToList();
    }

    public IReadOnlyList<EssenceAbilityModifierDefinition> GetActiveAbilityModifiers(DungeonRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        run.State ??= new DungeonRunState { RunId = run.Id };

        return run.State.ActiveBoonIds
            .Select(_definitions.GetById)
            .Where(x => x is not null)
            .SelectMany(x => x!.AbilityModifiers)
            .ToList();
    }

    private static IEnumerable<DungeonBoonDefinition> PickWeighted(
        List<DungeonBoonDefinition> available,
        int count,
        Random random)
    {
        var pool = available.ToList();
        while (pool.Count > 0 && count > 0)
        {
            var totalWeight = pool.Sum(x => GetRarityWeight(x.Rarity));
            var roll = random.Next(1, totalWeight + 1);
            var cursor = 0;

            for (var i = 0; i < pool.Count; i++)
            {
                cursor += GetRarityWeight(pool[i].Rarity);
                if (roll > cursor)
                {
                    continue;
                }

                var selected = pool[i];
                pool.RemoveAt(i);
                count--;
                yield return selected;
                break;
            }
        }
    }

    private static int GetRarityWeight(DungeonBoonRarity rarity) =>
        rarity switch
        {
            DungeonBoonRarity.Common => 60,
            DungeonBoonRarity.Uncommon => 30,
            DungeonBoonRarity.Rare => 12,
            DungeonBoonRarity.Epic => 4,
            DungeonBoonRarity.Legendary => 2,
            DungeonBoonRarity.Legacy => 1,
            _ => 1
        };

    private IEnumerable<ActiveBoonStack> GetActiveBoonStacks(DungeonRun run)
    {
        foreach (var group in run.State.ActiveBoonIds
                     .Where(id => !string.IsNullOrWhiteSpace(id))
                     .GroupBy(id => id, StringComparer.OrdinalIgnoreCase))
        {
            var definition = _definitions.GetById(group.Key);
            if (definition is null)
            {
                continue;
            }

            yield return new ActiveBoonStack(definition, group.Count());
        }
    }

    private static List<DungeonBoonEffectSummary> CreateAggregateEffectSummaries(
        IReadOnlyList<ActiveBoonStack> activeStacks)
    {
        var attributeEffects = activeStacks
            .SelectMany(stack => stack.Definition.AttributeModifiers.Select(modifier => new
            {
                modifier.AttributeType,
                modifier.ModifierType,
                Amount = modifier.Amount * stack.Count
            }))
            .GroupBy(
                modifier => new { modifier.AttributeType, modifier.ModifierType },
                modifier => modifier.Amount)
            .Select(group => new DungeonBoonEffectSummary
            {
                Id = $"attribute:{group.Key.AttributeType}:{group.Key.ModifierType}",
                Label = FormatIdentifier(group.Key.AttributeType.ToString()),
                Value = FormatSignedAmount(group.Sum(), group.Key.ModifierType != ModifierType.Flat),
                Category = "Stats"
            });

        var abilityEffects = activeStacks
            .SelectMany(stack => stack.Definition.AbilityModifiers.Select(modifier => new
            {
                modifier.Target,
                modifier.Operation,
                Value = modifier.Value * stack.Count
            }))
            .GroupBy(
                modifier => new
                {
                    Target = modifier.Target ?? string.Empty,
                    Operation = modifier.Operation ?? string.Empty
                },
                modifier => modifier.Value)
            .Select(group => new DungeonBoonEffectSummary
            {
                Id = $"ability:{group.Key.Target}:{group.Key.Operation}",
                Label = FormatAbilityTarget(group.Key.Target),
                Value = FormatAggregateAbilityValue(group.Key.Operation, group.Sum()),
                Category = "Ability Effects"
            });

        return attributeEffects
            .Concat(abilityEffects)
            .OrderBy(effect => effect.Category)
            .ThenBy(effect => effect.Label)
            .ToList();
    }

    private static DungeonBoonChoiceOption ToChoiceOption(DungeonBoonDefinition definition) => new()
    {
        Id = definition.Id,
        Name = definition.Name,
        Description = definition.Description,
        Rarity = definition.Rarity.ToString(),
        EffectSummaries = CreateEffectSummaries(definition)
    };

    private static List<string> CreateEffectSummaries(DungeonBoonDefinition definition)
    {
        var summaries = definition.AttributeModifiers
            .Select(FormatAttributeModifier)
            .Concat(definition.AbilityModifiers.Select(FormatAbilityModifier))
            .Where(summary => !string.IsNullOrWhiteSpace(summary))
            .ToList();

        return summaries.Count > 0
            ? summaries
            : ["No direct combat effect."];
    }

    private static string FormatAttributeModifier(EssenceAttributeModifier modifier)
    {
        var amount = FormatSignedAmount(modifier.Amount, modifier.ModifierType != ModifierType.Flat);
        var attribute = FormatIdentifier(modifier.AttributeType.ToString());
        var suffix = modifier.ModifierType switch
        {
            ModifierType.Multiplicative => " multiplier",
            _ => string.Empty
        };

        return $"{amount} {attribute}{suffix}";
    }

    private static string FormatAbilityModifier(EssenceAbilityModifierDefinition modifier)
    {
        var target = FormatAbilityTarget(modifier.Target);

        if (modifier.Operation.Equals("AddMultiplier", StringComparison.OrdinalIgnoreCase))
        {
            var percent = FormatSignedDouble(modifier.Value * 100);
            return $"{percent}% {target}";
        }

        if (modifier.Operation.Equals("AddEffect", StringComparison.OrdinalIgnoreCase))
        {
            return $"Adds {target}";
        }

        var value = FormatSignedDouble(modifier.Value);
        return $"{FormatIdentifier(modifier.Operation)} {value} to {target}";
    }

    private static string FormatAggregateAbilityValue(string operation, double value)
    {
        if (operation.Equals("AddMultiplier", StringComparison.OrdinalIgnoreCase))
        {
            return $"{FormatSignedDouble(value * 100)}%";
        }

        if (operation.Equals("AddEffect", StringComparison.OrdinalIgnoreCase))
        {
            return "Added";
        }

        return $"{FormatIdentifier(operation)} {FormatSignedDouble(value)}";
    }

    private static string FormatSignedAmount(float value, bool asPercent)
    {
        var formatted = value.ToString("0.##", CultureInfo.InvariantCulture);
        var prefix = value > 0 ? "+" : string.Empty;
        return asPercent ? $"{prefix}{formatted}%" : $"{prefix}{formatted}";
    }

    private static string FormatSignedDouble(double value)
    {
        var formatted = value.ToString("0.##", CultureInfo.InvariantCulture);
        var prefix = value > 0 ? "+" : string.Empty;
        return $"{prefix}{formatted}";
    }

    private static string FormatAbilityTarget(string target)
    {
        if (target.Equals("effect.damage.main", StringComparison.OrdinalIgnoreCase))
        {
            return "main damage effects";
        }

        return FormatIdentifier(target);
    }

    private static string FormatIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "effect";
        }

        return string.Join(
            ' ',
            value
                .Replace('.', ' ')
                .Replace('_', ' ')
                .Replace('-', ' ')
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(SplitIdentifierPart))
            .Trim();
    }

    private static string SplitIdentifierPart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = new List<char>(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (i > 0 && char.IsUpper(current) && !char.IsUpper(value[i - 1]))
            {
                chars.Add(' ');
            }

            chars.Add(i == 0 ? char.ToUpperInvariant(current) : current);
        }

        return new string(chars.ToArray());
    }

    private static int CreateRunSeed(int runSeed, int roomIndex, int activeBoonCount)
    {
        unchecked
        {
            var seed = runSeed;
            seed = (seed * 397) ^ roomIndex;
            seed = (seed * 397) ^ activeBoonCount;
            seed = (seed * 397) ^ 113;
            return seed;
        }
    }

    private sealed record ActiveBoonStack(DungeonBoonDefinition Definition, int Count);
}
