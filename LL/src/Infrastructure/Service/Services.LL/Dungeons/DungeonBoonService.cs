using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Dungeons.Definitions.Boons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Essences.Definitions;

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

    private static DungeonBoonChoiceOption ToChoiceOption(DungeonBoonDefinition definition) => new()
    {
        Id = definition.Id,
        Name = definition.Name,
        Description = definition.Description,
        Rarity = definition.Rarity.ToString()
    };

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
}
