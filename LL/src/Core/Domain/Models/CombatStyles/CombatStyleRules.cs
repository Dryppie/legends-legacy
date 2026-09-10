using System.Collections.Immutable;

namespace Domain.Models.CombatStyles;

public static class CombatStyleRules
{
    public static string? CurrentRefinementId(string? styleId, string? refinementId) =>
        styleId == CombatStyleIds.Bastion && refinementId == CombatStyleIds.Counterweight
            ? CombatStyleIds.Reprisal : refinementId;

    public static CombatStyleSelectionRequest NormalizeSelection(CombatStyleSelectionRequest selection)
    {
        var refinementId = CurrentRefinementId(selection.CombatStyleId, selection.RefinementId);
        return selection with { RefinementId = refinementId, ChanneledPlayerEssenceId = null };
    }

    public static string? ValidateSelection(CharacterCombatStyle? owned, CombatStyleDefinition? definition,
        CombatStyleSelectionRequest selection)
    {
        selection = NormalizeSelection(selection);
        if (selection.CombatStyleId is null)
            return selection.RefinementId is not null || selection.UpgradeIds.Count > 0
                || selection.MasteredUpgradeId is not null
                ? "An empty Combat Style cannot have a refinement, upgrades, or mastery." : null;
        if (definition is null) return "Choose an available Combat Style.";
        var level = owned?.Level ?? 0;
        if (selection.RefinementId is not null && (level < 3 || !definition.Refinements.Any(x => x.Id == selection.RefinementId)))
            return "Choose an available refinement from this Combat Style (unlocked at level 3).";
        if (selection.UpgradeIds.Count > CombatStyleProgression.UpgradeSlots(level))
            return "This Combat Style has not unlocked enough upgrade slots.";
        if (selection.UpgradeIds.Distinct(StringComparer.Ordinal).Count() != selection.UpgradeIds.Count
            || selection.UpgradeIds.Any(id => !definition.Upgrades.Any(x => x.Id == id)))
            return "Choose distinct upgrades belonging to this Combat Style.";
        if (selection.MasteredUpgradeId is not null
            && (level < CombatStyleProgression.UpgradeMasteryLevel || !selection.UpgradeIds.Contains(selection.MasteredUpgradeId, StringComparer.Ordinal)))
            return "Choose one equipped upgrade to master (unlocked at level 9).";
        return null;
    }

    public static string? ValidateChanneledEssence(ChanneledEssenceOption? channeledEssence) => channeledEssence is { IsEligible: true }
        ? null
        : "Conduit requires an Essence that deals direct damage, heals, or grants Barrier when it casts in the first occupied Essence slot. Put one first before battle.";

    public static CombatStyleSnapshot Snapshot(CombatStyleCatalog catalog, CombatStyleDefinition definition,
        CharacterCombatStyle owned, CombatStyleSelectionRequest selection, ChanneledEssenceOption? channeledEssence = null)
    {
        selection = NormalizeSelection(selection);
        return new()
        {
            CombatStyleId = definition.Id, Kind = definition.Kind, ContentVersion = catalog.ContentVersion,
            Level = owned.Level,
            RefinementId = selection.RefinementId, UpgradeIds = selection.UpgradeIds.ToImmutableArray(),
            MasteredUpgradeId = selection.MasteredUpgradeId,
            ChanneledPlayerEssenceId = definition.Kind == CombatStyleKind.Conduit ? channeledEssence?.PlayerEssenceId : null,
            ChanneledEssenceDefinitionId = definition.Kind == CombatStyleKind.Conduit ? channeledEssence?.EssenceDefinitionId : null,
            Tuning = definition.Refinements.FirstOrDefault(x => x.Id == selection.RefinementId)?.Tuning ?? definition.Tuning,
            MilestoneTuning = definition.MilestoneTuning
        };
    }

    public static void ValidateCatalog(CombatStyleCatalog catalog)
    {
        if (string.IsNullOrWhiteSpace(catalog.ContentVersion)) throw new InvalidOperationException("Combat Style content requires a version.");
        if (catalog.XpRequirements.Count != CombatStyleProgression.MaximumLevel || catalog.XpRequirements.Any(x => x <= 0))
            throw new InvalidOperationException("Combat Styles require ten positive XP requirements.");
        if (catalog.Styles.Count != 4 || catalog.Styles.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != 4)
            throw new InvalidOperationException("The Combat Styles catalog must contain exactly Bastion, Conduit, Reaper and Duelist.");
        foreach (var definition in catalog.Styles)
        {
            var bastion = definition.Id == CombatStyleIds.Bastion && definition.Kind == CombatStyleKind.Bastion;
            var conduit = definition.Id == CombatStyleIds.Conduit && definition.Kind == CombatStyleKind.Conduit;
            var reaper = definition.Id == CombatStyleIds.Reaper && definition.Kind == CombatStyleKind.Reaper;
            var duelist = definition.Id == CombatStyleIds.Duelist && definition.Kind == CombatStyleKind.Duelist;
            if (!bastion && !conduit && !reaper && !duelist) throw new InvalidOperationException("Unsupported Combat Style mechanic.");
            var expectedRefinements = bastion
                ? new[] { CombatStyleIds.Rebuild, CombatStyleIds.Reprisal, CombatStyleIds.Shelter }
                : reaper ? [CombatStyleIds.SoulSiphon, CombatStyleIds.LastRites, CombatStyleIds.DeathSentence]
                : duelist ? [CombatStyleIds.Flurry, CombatStyleIds.PatientBlade, CombatStyleIds.GuardedThrust]
                : [CombatStyleIds.ShortCircuit, CombatStyleIds.DeepReservoir, CombatStyleIds.Relay];
            var expectedUpgrades = bastion
                ? new[] { CombatStyleIds.PreparedWall, CombatStyleIds.HoldTheBreach, CombatStyleIds.MeasuredRecovery }
                : reaper ? [CombatStyleIds.ClosingHand, CombatStyleIds.Crosscut, CombatStyleIds.DeepRoots]
                : duelist ? [CombatStyleIds.MeasuredStrikes, CombatStyleIds.KnowYourEnemy, CombatStyleIds.FinishingTouch]
                : [CombatStyleIds.FullCircuit, CombatStyleIds.PartialFlow, CombatStyleIds.EmergencyChannel];
            if (definition.Refinements.Count != 3 || !expectedRefinements.Order().SequenceEqual(definition.Refinements.Select(x => x.Id).Order())
                || definition.Upgrades.Count != 3 || !expectedUpgrades.Order().SequenceEqual(definition.Upgrades.Select(x => x.Id).Order()))
                throw new InvalidOperationException($"Invalid choices for Combat Style {definition.Id}.");
            foreach (var tuning in definition.Refinements.Select(x => x.Tuning ?? definition.Tuning).Append(definition.Tuning))
            {
                if (duelist)
                {
                    if (tuning.Duelist is not { } read
                        || read.ReadRequired <= 0 || read.ReturnedRead < 0 || read.ReturnedRead >= read.ReadRequired
                        || read.GuardCharges < 0 || read.FirstImpressionRead < 0
                        || !double.IsFinite(read.OpeningMultiplier) || read.OpeningMultiplier < 1
                        || !double.IsFinite(read.PerMasteryLevel) || read.PerMasteryLevel < 0
                        || !double.IsFinite(read.UpgradeBonus) || read.UpgradeBonus < 0
                        || !double.IsFinite(read.MasteredMeasuredStrikesBonus) || read.MasteredMeasuredStrikesBonus < read.UpgradeBonus
                        || new[] { read.FinishingTouchHealthThreshold, read.MasteredFinishingTouchHealthThreshold }
                            .Any(x => !double.IsFinite(x) || x is <= 0 or > 1)
                        || read.MasteredFinishingTouchHealthThreshold < read.FinishingTouchHealthThreshold)
                        throw new InvalidOperationException("Invalid Duelist tuning.");
                    continue;
                }
                if (reaper)
                {
                    if (tuning.Reaper is not { } harvest
                        || !double.IsFinite(harvest.BaseMultiplier) || harvest.BaseMultiplier <= 0
                        || !double.IsFinite(harvest.PerMasteryLevel) || harvest.PerMasteryLevel < 0
                        || !double.IsFinite(harvest.DeathSentenceBonus) || harvest.DeathSentenceBonus < 0
                        || !double.IsFinite(harvest.UpgradeBonus) || harvest.UpgradeBonus < 0
                        || new[] { harvest.LastRitesHealthThreshold, harvest.ClosingHandHealthThreshold,
                            harvest.MasteredClosingHandHealthThreshold }.Any(x => !double.IsFinite(x) || x is <= 0 or > 1)
                        || harvest.MasteredClosingHandHealthThreshold < harvest.ClosingHandHealthThreshold
                        || harvest.OpeningPoisonStacks <= 0)
                        throw new InvalidOperationException("Invalid Reaper tuning.");
                    continue;
                }
                if (tuning.HealthFraction <= 0 || tuning.BarrierFraction <= 0 || Math.Abs(tuning.HealthFraction + tuning.BarrierFraction - 1) > .000001
                    || tuning.ChargeCap is < 2 or > 4 || tuning.ChanneledBaseMultiplier <= 0 || tuning.ChanneledBaseMultiplier >= 1
                    || tuning.ChanneledPerCharge <= 0
                    || tuning.BarrierPerMasteryLevel is not { } barrierPerLevel || !double.IsFinite(barrierPerLevel) || barrierPerLevel < 0
                    || tuning.ChanneledPerMasteryLevel is not { } channeledPerLevel || !double.IsFinite(channeledPerLevel) || channeledPerLevel < 0
                    || tuning.RelayChargeReturn < 0 || tuning.RelayChargeReturn >= tuning.RelayMinimumSpent && tuning.RelayChargeReturn != 0)
                    throw new InvalidOperationException($"Invalid tuning for Combat Style {definition.Id}.");
                if (bastion && (tuning.ReprisalAbsorbedDamageFraction is not { } absorbedFraction
                    || !double.IsFinite(absorbedFraction) || absorbedFraction is < 0 or > 1
                    || tuning.ReprisalMaxHealthCapFraction is not { } capFraction
                    || !double.IsFinite(capFraction) || capFraction is < 0 or > 1))
                    throw new InvalidOperationException($"Invalid Reprisal tuning for Combat Style {definition.Id}.");
            }
            if (conduit && definition.Refinements.Any(x => x.Tuning is null))
                throw new InvalidOperationException("Conduit refinements require explicit resolved tuning.");
            var milestones = definition.MilestoneTuning;
            var fractions = new[] { milestones.OpeningBarrierFraction, milestones.HoldTheBreachHealthBonus,
                milestones.MeasuredRecoveryOverhealBarrierFraction, milestones.EmergencyChannelHealthThreshold };
            var minimumChargeCap = definition.Refinements.Select(x => x.Tuning ?? definition.Tuning)
                .Append(definition.Tuning).Min(x => x.ChargeCap);
            if (fractions.Any(x => !double.IsFinite(x) || x is < 0 or > 1)
                || milestones.OpeningCharge < 0 || milestones.OpeningCharge > minimumChargeCap
                || milestones.FullCircuitMinimumCharge < 0 || milestones.FullCircuitMinimumCharge > minimumChargeCap
                || milestones.PartialFlowMaximumCharge < 0 || milestones.PartialFlowMaximumCharge > minimumChargeCap)
                throw new InvalidOperationException($"Invalid milestone tuning for Combat Style {definition.Id}.");
        }
    }
}
