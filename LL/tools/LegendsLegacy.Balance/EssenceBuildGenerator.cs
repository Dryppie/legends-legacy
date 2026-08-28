using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Items;
using Domain.Models.Essences.Definitions;
using Services.LL.Essences;

namespace LegendsLegacy.Balance;

public sealed record EssenceBuildSnapshot(
    string Id,
    string ProfileId,
    int SlotCount,
    int GenerationSeed,
    IReadOnlyList<EssenceBuildSelection> Essences,
    EssenceBuildCharacterSnapshot Character);

public sealed record EssenceBuildSelection(
    string EssenceId,
    string DisplayName,
    string SourceMonsterId,
    Rarity Rarity);

public sealed record EssenceBuildCharacterSnapshot(
    string GearPackageId,
    int CharacterLevel,
    int UnlockedEssenceSlots,
    GearPackageCombatRatingSnapshot CombatRating);

public sealed class EssenceBuildGenerator(
    IEssenceDefinitionRepository essenceDefinitions,
    GearPackageFactory gearPackages,
    IEssenceSlotUnlockService slotUnlocks)
{
    public static IReadOnlyList<int> InitialSlotCounts { get; } = Array.AsReadOnly([4, 5, 6]);

    public IReadOnlyList<EssenceBuildSnapshot> GenerateInitialProfiles(
        int seed,
        int buildsPerProfile)
    {
        if (buildsPerProfile is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(buildsPerProfile), "Build count must be between 1 and 1,000.");

        var definitions = GetSourceFamilies();

        return InitialSlotCounts
            .SelectMany(slotCount => GenerateProfile(
                definitions,
                seed,
                slotCount,
                buildsPerProfile,
                $"E{slotCount}_RANDOM",
                $"E{slotCount}_RANDOM"))
            .ToArray();
    }

    internal IReadOnlyList<EssenceDefinition[]> GetSourceFamilies()
    {
        var definitions = essenceDefinitions.GetAll()
            .Where(definition =>
                !string.IsNullOrWhiteSpace(definition.Id)
                && !definition.Id.Equals("essence.training", StringComparison.OrdinalIgnoreCase))
            .GroupBy(definition => definition.SourceMonsterId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray())
            .ToArray();

        if (definitions.Length < InitialSlotCounts.Max())
            throw new InvalidOperationException("The production catalog does not contain enough unique Essence sources.");

        return definitions;
    }

    internal IReadOnlyList<EssenceBuildSnapshot> GenerateProfile(
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        int seed,
        int slotCount,
        int buildCount,
        string profileId,
        string idPrefix)
    {
        var generationSeed = unchecked(seed ^ (slotCount * 1_103_515_245));
        var random = new Random(generationSeed);
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        var selectedBuilds = new List<EssenceDefinition[]>(buildCount);
        var maximumAttempts = buildCount * 100;

        for (var attempt = 0; attempt < maximumAttempts && selectedBuilds.Count < buildCount; attempt++)
        {
            var candidate = SelectCandidate(sourceFamilies, slotCount, random);
            var signature = string.Join('|', candidate.Select(definition => definition.Id));
            if (signatures.Add(signature))
                selectedBuilds.Add(candidate);
        }

        if (selectedBuilds.Count != buildCount)
        {
            throw new InvalidOperationException(
                $"Could not generate {buildCount} unique legal {slotCount}-slot Essence builds.");
        }

        return selectedBuilds.Select((definitions, index) => MaterializeBuild(
                $"{idPrefix}_{index + 1:000}",
                profileId,
                slotCount,
                generationSeed,
                definitions.Select(definition => definition.Id).ToArray()))
            .ToArray();
    }

    internal EssenceBuildSnapshot MaterializeBuild(
        string id,
        string profileId,
        int slotCount,
        int generationSeed,
        IReadOnlyList<string> essenceIds)
    {
        if (essenceIds.Count != slotCount)
            throw new InvalidOperationException($"Build '{id}' does not contain exactly {slotCount} Essences.");
        var definitions = essenceIds.Select(essenceId =>
                essenceDefinitions.GetById(essenceId)
                ?? throw new InvalidOperationException($"Build '{id}' references unknown Essence '{essenceId}'."))
            .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var gearPackage = ResolveReferenceGearPackage(slotCount);
        var canonicalBuild = gearPackages.CreateCanonicalBuild(
            gearPackage,
            definitions.Select(definition => definition.Id).ToArray());
        var unlockedSlots = slotUnlocks.GetUnlockedSlotCount(canonicalBuild.Character.Level);
        if (unlockedSlots < slotCount)
        {
            throw new InvalidOperationException(
                $"Generated character for '{id}' only unlocks {unlockedSlots} Essence slots.");
        }

        return new EssenceBuildSnapshot(
            id,
            profileId,
            slotCount,
            generationSeed,
            definitions.Select(definition => new EssenceBuildSelection(
                definition.Id,
                definition.DisplayName,
                definition.SourceMonsterId,
                definition.Rarity)).ToArray(),
            new EssenceBuildCharacterSnapshot(
                gearPackage.Id,
                canonicalBuild.Character.Level,
                unlockedSlots,
                GearPackageFactory.CreateRatingSnapshot(canonicalBuild.Rating)));
    }

    private static EssenceDefinition[] SelectCandidate(
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        int slotCount,
        Random random)
    {
        var familyIndexes = Enumerable.Range(0, sourceFamilies.Count).ToArray();
        for (var index = 0; index < slotCount; index++)
        {
            var selectedIndex = random.Next(index, familyIndexes.Length);
            (familyIndexes[index], familyIndexes[selectedIndex]) =
                (familyIndexes[selectedIndex], familyIndexes[index]);
        }

        return familyIndexes
            .Take(slotCount)
            .Select(index =>
            {
                var family = sourceFamilies[index];
                return family[random.Next(family.Length)];
            })
            .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static GearPackageDefinition ResolveReferenceGearPackage(int slotCount) =>
        slotCount switch
        {
            4 or 5 => GearPackageFactory.RegionOneDefinitions[0],
            6 => GearPackageFactory.RegionOneDefinitions[1],
            _ => throw new ArgumentOutOfRangeException(nameof(slotCount), slotCount, "Unsupported Essence profile.")
        };
}
