using Services.LL.PowerRatings;
using Domain.Models.Items.Equipments.Progression;

namespace BalanceHarness;

/// <summary>Remove one ordered Essence position while retaining actor, gear and surviving Essence identities.</summary>
public static class TowerEssenceAblation
{
    public static TowerScenario Remove(TowerScenario source, int removedIndex, int level, OfflineContent content)
    {
        if (source.Party is not { Count: > 0 } || removedIndex is < 0 or >= 6
            || source.Party.Any(p => p.Build.EssenceIds.Count != 6 || p.Build.IdentityEssenceIndices is not null))
            throw new InvalidDataException("This controlled ablation requires six-Essence parties and one original position 0–5.");
        var indices = Enumerable.Range(0, 6).Where(i => i != removedIndex).ToArray();
        var copy = TowerBatchRacing.Copy(source);
        var party = copy.Party.Select(member => {
            var build = member.Build; var before = content.CreateBuild(build);
            var tier = build.Tier;
            while (tier > 1 && level < EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(tier)) tier--;
            var changed = build with {
                CharacterLevel = level, Tier = tier, EssenceIds = indices.Select(i => build.EssenceIds[i]).ToArray(),
                IdentityProgression = build.IdentityProgression ?? new EquipmentReferenceProgressionIdentity(
                    build.CharacterLevel, build.IdentityEssenceIds ?? build.EssenceIds, build.Tier),
                IdentityEssenceIds = null, IdentityEssenceIndices = indices
            };
            var after = content.CreateBuild(changed);
            if (before.Character.Id != after.Character.Id
                || !before.Equipment.Select(e => e.Id).SequenceEqual(after.Equipment.Select(e => e.Id))
                || !indices.Select(i => before.EquippedEssences[i].Id).SequenceEqual(after.EquippedEssences.Select(e => e.Id)))
                throw new InvalidDataException("Ablation changed actor, equipment or surviving Essence identities.");
            return member with { Build = changed };
        }).ToArray();
        return copy with { Party = party, Seeds = [], Assumptions = [.. copy.Assumptions,
            $"Controlled five-Essence ablation: remove original position {removedIndex + 1}; level {level}; equipment tier capped to that level's eligibility; retained Essence order and actor/item/surviving Essence identities. Hypothetical gear ownership; historical diagnostic only."] };
    }
}
