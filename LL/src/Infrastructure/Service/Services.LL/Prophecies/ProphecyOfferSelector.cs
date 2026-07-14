using Domain.Models.Prophecies;
using System.Security.Cryptography;
using System.Text;

namespace Services.LL.Prophecies;

public static class ProphecyOfferSelector
{
    public static ProphecyDefinition? Pick(
        IReadOnlyList<ProphecyDefinition> definitions,
        ProphecyScope scope,
        ProphecySlotType slot,
        Guid characterId,
        DateTimeOffset periodStart,
        string selectionSalt,
        IReadOnlySet<string>? excludedDefinitionIds = null,
        IReadOnlySet<ProphecyCategory>? excludedCategories = null,
        IReadOnlySet<string>? recentDefinitionIds = null)
    {
        var slotName = slot.ToString();
        var candidates = definitions
            .Where(x => x.IsEnabled && x.Scope == scope && x.AllowedSlots.Contains(slotName))
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = definitions
                .Where(x => x.IsEnabled && x.Scope == scope && x.Category == ProphecyCategory.Combat)
                .ToList();
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        excludedDefinitionIds ??= EmptyDefinitionIds;
        excludedCategories ??= EmptyCategories;
        recentDefinitionIds ??= EmptyDefinitionIds;

        var uniqueCandidates = candidates
            .Where(x => !excludedDefinitionIds.Contains(x.Id))
            .ToList();

        var selectionPool = FirstNonEmpty(
            uniqueCandidates.Where(x =>
                !excludedCategories.Contains(x.Category) &&
                !recentDefinitionIds.Contains(x.Id)),
            uniqueCandidates.Where(x => !excludedCategories.Contains(x.Category)),
            uniqueCandidates.Where(x => !recentDefinitionIds.Contains(x.Id)),
            uniqueCandidates,
            candidates.Where(x => !recentDefinitionIds.Contains(x.Id)),
            candidates);

        return PickWeighted(
            selectionPool,
            $"{characterId:N}:{periodStart:O}:{slotName}:{scope}:{selectionSalt}");
    }

    private static IReadOnlyList<ProphecyDefinition> FirstNonEmpty(
        params IEnumerable<ProphecyDefinition>[] pools)
    {
        foreach (var pool in pools)
        {
            var values = pool.ToList();
            if (values.Count > 0)
            {
                return values;
            }
        }

        return [];
    }

    private static ProphecyDefinition PickWeighted(
        IReadOnlyList<ProphecyDefinition> candidates,
        string seed)
    {
        var totalWeight = candidates.Sum(x => Math.Max(1, x.Weight));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var roll = (int)(BitConverter.ToUInt32(hash, 0) % (uint)totalWeight);

        foreach (var candidate in candidates)
        {
            roll -= Math.Max(1, candidate.Weight);
            if (roll < 0)
            {
                return candidate;
            }
        }

        return candidates[^1];
    }

    private static readonly IReadOnlySet<string> EmptyDefinitionIds =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlySet<ProphecyCategory> EmptyCategories =
        new HashSet<ProphecyCategory>();
}
