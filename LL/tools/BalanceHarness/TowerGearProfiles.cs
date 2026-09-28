using System.Text.Json;
using Domain.Models.Items.Equipments.Slots;

namespace BalanceHarness;

public sealed record TowerGearProfile(string Id, string Description, IReadOnlyList<int> PartySlots,
    IReadOnlyDictionary<EquipmentSlotType, string> Specializations);
public sealed record TowerGearProfileCatalog(string Version, IReadOnlyList<TowerGearProfile> Profiles);

/// <summary>Explicit equal-budget specialization choices, frozen before Essence search.</summary>
public static class TowerGearProfiles
{
    public const string CatalogVersion = "tower-gear-specialization-screen-v1";

    public static TowerGearProfileCatalog Read(string path)
    {
        var catalog = TowerContractJson.Read<TowerGearProfileCatalog>(path);
        if (catalog.Version != CatalogVersion || catalog.Profiles is not { Count: > 0 and <= 32 }
            || catalog.Profiles.Any(p => p is null)
            || catalog.Profiles.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != catalog.Profiles.Count)
            throw new InvalidDataException("Expected a versioned catalog of distinct gear profiles.");
        foreach (var profile in catalog.Profiles) Validate(profile);
        return catalog;
    }

    public static TowerGearProfile Select(TowerGearProfileCatalog catalog, string id) =>
        catalog.Profiles.SingleOrDefault(p => p.Id == id)
        ?? throw new InvalidDataException($"Unknown gear profile '{id}'.");

    private static void Validate(TowerGearProfile profile)
    {
        if (profile is null || !TowerBenchmark.SafeId(profile.Id) || profile.Id == "baseline"
            || string.IsNullOrWhiteSpace(profile.Description) || profile.PartySlots is null
            || profile.PartySlots.Any(slot => slot is < 1 or > 50)
            || profile.PartySlots.Distinct().Count() != profile.PartySlots.Count
            || profile.Specializations is not { Count: > 0 and <= 8 }
            || profile.Specializations.Any(p => !Enum.IsDefined(p.Key) || string.IsNullOrWhiteSpace(p.Value)))
            throw new InvalidDataException("A gear profile needs a name, distinct target positions and valid slot specializations.");
    }

    // Empty PartySlots means every member. Named positions outside a smaller party
    // are ignored, but at least one position must match. Missing equipment is an error.
    public static IReadOnlyList<TowerPartyRecipe> Apply(IReadOnlyList<TowerPartyRecipe> party,
        TowerGearProfile profile, OfflineContent content)
    {
        Validate(profile);
        if (party is not { Count: > 0 } || party.Any(p => p is null)
            || party.Select(p => p.PartySlot).Distinct().Count() != party.Count
            || !party.Any(p => profile.PartySlots.Count == 0 || profile.PartySlots.Contains(p.PartySlot)))
            throw new InvalidDataException("Gear profile must target a nonempty party with distinct positions.");
        var result = TowerBatchRacing.Copy(party.ToArray());
        var evaluator = content.Equipment.Evaluator;
        for (var i = 0; i < result.Length; i++)
        {
            var member = result[i];
            if (profile.PartySlots.Count != 0 && !profile.PartySlots.Contains(member.PartySlot)) continue;
            var build = member.Build;
            var before = content.CreateBuild(build);
            if (build.Equipment.Any(e => e.ActiveStyleId is not null || e.UseNativeStyle)
                || profile.Specializations.Keys.Except(build.Equipment.Select(e => e.Slot)).Any())
                throw new InvalidDataException("Specialization profiles require unstyled equipment in every requested slot.");
            var equipment = build.Equipment.Select(item => {
                if (!profile.Specializations.TryGetValue(item.Slot, out var specialization)) return item;
                var original = evaluator.GetDefinition(item.DefinitionId);
                var matches = evaluator.Definitions.Where(d => d.ArchetypeId == original.ArchetypeId
                    && d.Rarity == original.Rarity && d.SpecializationId == specialization && d.NativeStyleId is null).ToArray();
                if (matches.Length != 1)
                    throw new InvalidDataException($"Expected one '{specialization}' definition for '{original.ArchetypeId}' at {original.Rarity}.");
                var replacement = matches[0];
                var first = evaluator.Evaluate(original.Id, build.Tier, build.Rank, null, build.Quality, build.AttributeRollMultiplier);
                var second = evaluator.Evaluate(replacement.Id, build.Tier, build.Rank, null, build.Quality, build.AttributeRollMultiplier);
                if (first.TargetBudget != second.TargetBudget)
                    throw new InvalidDataException("Gear specialization cannot change the item's target budget.");
                return item with { DefinitionId = replacement.Id };
            }).ToArray();
            var changed = build with { Equipment = equipment, IdentityEquipment = build.IdentityEquipment ?? build.Equipment };
            var after = content.CreateBuild(changed);
            if (before.Character.Id != after.Character.Id || !before.Equipment.Select(e => e.Id).SequenceEqual(after.Equipment.Select(e => e.Id))
                || !before.EquippedEssences.Select(e => e.Id).SequenceEqual(after.EquippedEssences.Select(e => e.Id)))
                throw new InvalidDataException("Gear application changed the original instance identities.");
            result[i] = member with { Build = changed };
        }
        return result;
    }

    public static TowerScenario Apply(TowerScenario scenario, TowerGearProfile profile, OfflineContent content)
    {
        var copy = TowerBatchRacing.Copy(scenario);
        return copy with { Party = Apply(copy.Party, profile, content) };
    }

    public static int Command(string[] args, CancellationToken token)
    {
        if (args is not ["tower-gear-profile-apply", var scenarioPath, var catalogPath, var id, var root, var output])
            throw new InvalidDataException("Use tower-gear-profile-apply <scenario.json> <catalog.json> <profile-id> <content-root> <new-scenario.json>.");
        token.ThrowIfCancellationRequested();
        var settings = TowerBundle.ReadSettings(root);
        var scenario = Apply(TowerContractJson.Read<TowerScenario>(scenarioPath), Select(Read(catalogPath), id), OfflineContent.ForTower(root, settings));
        HarnessJson.WriteNew(output, scenario);
        Console.WriteLine(JsonSerializer.Serialize(new { status = "GearProfileApplied", profile = id,
            scenarioHash = HarnessJson.Hash(scenario), newFights = 0 }, HarnessJson.Options));
        return 0;
    }
}
