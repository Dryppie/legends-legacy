using Services.LL.PowerRatings;

namespace BalanceHarness;

/// <summary>Controlled, addition-only progression comparisons; no composition mutations.</summary>
public static class TowerProgressionUpgrades
{
    public static IReadOnlyList<string> UniformAdditions(TowerScenario source, OfflineContent content)
    {
        ValidateSource(source);
        var definitions = content.Essences.GetAll().ToDictionary(e => e.Id, StringComparer.Ordinal);
        var used = source.Party.SelectMany(p => p.Build.EssenceIds).Select(id => definitions[id].SourceMonsterId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return definitions.Values.Where(e => !used.Contains(e.SourceMonsterId))
            .Select(e => e.Id).Order(StringComparer.Ordinal).ToArray();
    }

    public static TowerScenario Apply(TowerScenario source, int level, string? addition, OfflineContent content)
    {
        ValidateSource(source);
        if (addition is not null && !UniformAdditions(source, content).Contains(addition, StringComparer.Ordinal))
            throw new InvalidDataException("The added Essence must be legal for every existing member.");
        var copy = TowerBatchRacing.Copy(source);
        var party = copy.Party.Select(member => {
            var build = member.Build; var before = content.CreateBuild(build);
            var changed = build with { CharacterLevel = level,
                EssenceIds = addition is null ? build.EssenceIds : [.. build.EssenceIds, addition],
                IdentityProgression = build.IdentityProgression ?? new EquipmentReferenceProgressionIdentity(
                    build.CharacterLevel, build.IdentityEssenceIds ?? build.EssenceIds), IdentityEssenceIds = null };
            var after = content.CreateBuild(changed);
            if (before.Character.Id != after.Character.Id
                || !before.Equipment.Select(e => e.Id).SequenceEqual(after.Equipment.Select(e => e.Id))
                || !before.EquippedEssences.Select(e => e.Id).SequenceEqual(after.EquippedEssences.Take(6).Select(e => e.Id)))
                throw new InvalidDataException("Progression changed existing actor/item/Essence identities.");
            return member with { Build = changed };
        }).ToArray();
        return copy with { Party = party, Seeds = [], Assumptions = [.. copy.Assumptions,
            $"Controlled progression comparison: level {level}; appended Essence {addition ?? "none"}; original six Essences, gear and instance identities retained. No transferred strength claim."] };
    }

    private static void ValidateSource(TowerScenario source)
    {
        if (source.Party is not { Count: > 0 } || source.Party.Any(p => p.Build.EssenceIds.Count != 6))
            throw new InvalidDataException("Progression upgrades require complete six-Essence source parties.");
    }
}
