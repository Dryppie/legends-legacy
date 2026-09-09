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
        return refinementId == selection.RefinementId ? selection : selection with { RefinementId = refinementId };
    }

    public static string? ValidateSelection(CharacterCombatStyle? owned, CombatStyleDefinition? definition,
        CombatStyleSelectionRequest selection, IReadOnlyList<CombatStyleFocusOption> focusOptions)
    {
        selection = NormalizeSelection(selection);
        if (selection.CombatStyleId is null)
            return selection.RefinementId is not null || selection.UpgradeIds.Count > 0 || selection.FocusPlayerEssenceId is not null
                || selection.MasteredUpgradeId is not null
                ? "An empty Combat Style cannot have a refinement, upgrades, mastery, or Focus." : null;
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
        if (definition.Kind == CombatStyleKind.Conduit
            && !focusOptions.Any(x => x.PlayerEssenceId == selection.FocusPlayerEssenceId && x.IsEligible))
            return "Choose an equipped Essence that deals direct damage, heals or grants Barrier when it casts, then set it as your Focus before battle.";
        if (definition.Kind != CombatStyleKind.Conduit && selection.FocusPlayerEssenceId is not null)
            return "Only Conduit uses a Focus Essence.";
        return null;
    }

    public static CombatStyleSnapshot Snapshot(CombatStyleCatalog catalog, CombatStyleDefinition definition,
        CharacterCombatStyle owned, CombatStyleSelectionRequest selection, string? focusDefinitionId)
    {
        selection = NormalizeSelection(selection);
        return new()
        {
            CombatStyleId = definition.Id, Kind = definition.Kind, ContentVersion = catalog.ContentVersion,
            Level = owned.Level,
            RefinementId = selection.RefinementId, UpgradeIds = selection.UpgradeIds.ToImmutableArray(),
            MasteredUpgradeId = selection.MasteredUpgradeId,
            FocusPlayerEssenceId = selection.FocusPlayerEssenceId, FocusEssenceDefinitionId = focusDefinitionId,
            Tuning = definition.Refinements.FirstOrDefault(x => x.Id == selection.RefinementId)?.Tuning ?? definition.Tuning,
            MilestoneTuning = definition.MilestoneTuning
        };
    }

    public static void ValidateCatalog(CombatStyleCatalog catalog)
    {
        if (string.IsNullOrWhiteSpace(catalog.ContentVersion)) throw new InvalidOperationException("Combat Style content requires a version.");
        if (catalog.XpRequirements.Count != CombatStyleProgression.MaximumLevel || catalog.XpRequirements.Any(x => x <= 0))
            throw new InvalidOperationException("Combat Styles require ten positive XP requirements.");
        if (catalog.Styles.Count != 2 || catalog.Styles.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != 2)
            throw new InvalidOperationException("The Combat Styles catalog must contain exactly Bastion and Conduit.");
        foreach (var definition in catalog.Styles)
        {
            var bastion = definition.Id == CombatStyleIds.Bastion && definition.Kind == CombatStyleKind.Bastion;
            var conduit = definition.Id == CombatStyleIds.Conduit && definition.Kind == CombatStyleKind.Conduit;
            if (!bastion && !conduit) throw new InvalidOperationException("Unsupported Combat Style mechanic.");
            var expectedRefinements = bastion
                ? new[] { CombatStyleIds.Rebuild, CombatStyleIds.Reprisal, CombatStyleIds.Shelter }
                : [CombatStyleIds.ShortCircuit, CombatStyleIds.DeepReservoir, CombatStyleIds.Relay];
            var expectedUpgrades = bastion
                ? new[] { CombatStyleIds.PreparedWall, CombatStyleIds.HoldTheBreach, CombatStyleIds.MeasuredRecovery }
                : [CombatStyleIds.FullCircuit, CombatStyleIds.PartialFlow, CombatStyleIds.EmergencyChannel];
            if (definition.Refinements.Count != 3 || !expectedRefinements.Order().SequenceEqual(definition.Refinements.Select(x => x.Id).Order())
                || definition.Upgrades.Count != 3 || !expectedUpgrades.Order().SequenceEqual(definition.Upgrades.Select(x => x.Id).Order()))
                throw new InvalidOperationException($"Invalid choices for Combat Style {definition.Id}.");
            foreach (var tuning in definition.Refinements.Select(x => x.Tuning ?? definition.Tuning).Append(definition.Tuning))
            {
                if (tuning.HealthFraction <= 0 || tuning.BarrierFraction <= 0 || Math.Abs(tuning.HealthFraction + tuning.BarrierFraction - 1) > .000001
                    || tuning.ChargeCap is < 2 or > 4 || tuning.FocusBaseMultiplier <= 0 || tuning.FocusBaseMultiplier >= 1
                    || tuning.FocusPerCharge <= 0
                    || tuning.BarrierPerMasteryLevel is not { } barrierPerLevel || !double.IsFinite(barrierPerLevel) || barrierPerLevel < 0
                    || tuning.FocusPerMasteryLevel is not { } focusPerLevel || !double.IsFinite(focusPerLevel) || focusPerLevel < 0
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
