using Application.Interfaces.Services.LL.CombatStyles;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.CombatStyles;
using Domain.Models.Essences;
using System.Globalization;

namespace Services.LL.CombatStyles;

public sealed class CombatStyleService(
    ICombatStyleRepository repository,
    ICombatStyleCatalogProvider catalogProvider,
    IEssenceCombatLoadoutResolver loadouts,
    IChanneledEssenceResolver channeledEssenceResolver,
    ICombatStyleMutationBoundary boundary) : ICombatStyleService
{
    private readonly Dictionary<Guid, IReadOnlyList<CharacterCombatStyle>> _owned = [];
    private readonly Dictionary<Guid, CharacterCombatStyleSelection?> _selections = [];
    private long _trackingGeneration = repository.TrackingGeneration;
    private CombatStyleCatalog Catalog => catalogProvider.Catalog;

    private async Task<IReadOnlyList<CharacterCombatStyle>> Owned(Guid id, CancellationToken ct)
    {
        RefreshTrackingGeneration();
        if (!_owned.TryGetValue(id, out var value)) _owned[id] = value = await repository.GetOwnedAsync(id, ct);
        return value;
    }

    private async Task<CharacterCombatStyleSelection?> Selected(Guid id, CancellationToken ct)
    {
        RefreshTrackingGeneration();
        if (!_selections.TryGetValue(id, out var value)) _selections[id] = value = await repository.GetSelectionAsync(id, ct);
        return value;
    }

    private async Task<IReadOnlyList<CharacterCombatStyle>> Available(Guid id, CancellationToken ct)
    {
        var owned = await Owned(id, ct);
        // Availability is independent of persistence: reads and previews never create progress rows.
        return Catalog.Styles.Select(definition => owned.FirstOrDefault(x => x.CombatStyleId == definition.Id)
            ?? new CharacterCombatStyle { CharacterId = id, CombatStyleId = definition.Id }).ToArray();
    }

    private async Task<CharacterCombatStyle> EnsureProgress(Guid id, string styleId, CancellationToken ct)
    {
        var owned = await Owned(id, ct);
        if (owned.FirstOrDefault(x => x.CombatStyleId == styleId) is { } existing) return existing;
        var progress = new CharacterCombatStyle { CharacterId = id, CombatStyleId = styleId };
        var updated = owned.Append(progress).ToArray();
        repository.Add(progress);
        _owned[id] = updated;
        return progress;
    }

    private void RefreshTrackingGeneration()
    {
        if (_trackingGeneration == repository.TrackingGeneration) return;
        _owned.Clear();
        _selections.Clear();
        _trackingGeneration = repository.TrackingGeneration;
    }

    private async Task<CombatStyleSelectionRequest> Selection(Guid id, CancellationToken ct)
    {
        var selected = await Selected(id, ct);
        return CombatStyleRules.NormalizeSelection(new(selected?.CombatStyleId, selected?.RefinementId,
            selected?.UpgradeIds ?? [], selected?.ChanneledPlayerEssenceId, MasteredUpgradeId: selected?.MasteredUpgradeId));
    }

    public async Task<CombatStyleOverview> GetOverviewAsync(Guid characterId, CancellationToken ct) =>
        await BuildOverview(characterId, await Selection(characterId, ct), ct);

    public Task<CombatStyleOverview> PreviewAsync(Guid characterId, CombatStyleSelectionRequest selection, CancellationToken ct) =>
        BuildOverview(characterId, selection, ct);

    private async Task<CombatStyleOverview> BuildOverview(Guid characterId, CombatStyleSelectionRequest selection, CancellationToken ct)
    {
        var owned = await Available(characterId, ct);
        selection = RestoreChoices(selection, owned);
        var entry = owned.FirstOrDefault(x => x.CombatStyleId == selection.CombatStyleId);
        var definition = Catalog.Styles.FirstOrDefault(x => x.Id == selection.CombatStyleId);
        var issue = CombatStyleRules.ValidateSelection(entry, definition, selection);
        var snapshot = issue is null && definition is not null && entry is not null
            ? CombatStyleRules.Snapshot(Catalog, definition, entry, selection) : null;
        var styles = Catalog.Styles.Select(def =>
        {
            var progress = owned.FirstOrDefault(x => x.CombatStyleId == def.Id);
            return new CombatStyleEntry(def, progress?.Level ?? 0, progress?.CurrentXp ?? 0,
                CombatStyleProgression.XpRequired(progress?.Level ?? 0, Catalog.XpRequirements),
                CombatStyleProgression.UpgradeSlots(progress?.Level ?? 0),
                CombatStyleRules.CurrentRefinementId(def.Id, progress?.RefinementId),
                progress?.UpgradeIds ?? [], progress?.MasteredUpgradeId);
        }).ToArray();
        return new(Catalog.ContentVersion, styles, selection, snapshot, issue, PreviewFacts(snapshot));
    }

    public async Task<CombatStyleSnapshot?> ResolveAsync(Guid characterId, EssenceCombatActivity activity, CancellationToken ct,
        IReadOnlyList<PlayerEssence>? equippedEssences = null)
    {
        ValidateActivity(activity);
        var selection = await Selection(characterId, ct);
        if (selection.CombatStyleId is null) return null;
        var owned = (await Available(characterId, ct)).FirstOrDefault(x => x.CombatStyleId == selection.CombatStyleId);
        var definition = Catalog.Styles.FirstOrDefault(x => x.Id == selection.CombatStyleId);
        var issue = CombatStyleRules.ValidateSelection(owned, definition, selection);
        if (issue is not null) throw new CombatStyleConfigurationException(issue);
        ChanneledEssenceOption? channeledEssence = null;
        if (definition!.Kind == CombatStyleKind.Conduit)
        {
            equippedEssences ??= (await loadouts.ResolveAsync(characterId, activity, ct)).EquippedEssences;
            if (equippedEssences.FirstOrDefault() is { } first) channeledEssence = channeledEssenceResolver.Resolve(first);
            if (CombatStyleRules.ValidateChanneledEssence(channeledEssence) is { } channeledEssenceIssue)
                throw new CombatStyleConfigurationException(channeledEssenceIssue);
        }
        return CombatStyleRules.Snapshot(Catalog, definition, owned!, selection, channeledEssence);
    }

    public async Task<CombatStyleOperationResult> SelectAsync(Guid characterId, CombatStyleSelectionRequest selection, CancellationToken ct)
    {
        var owned = await Available(characterId, ct);
        selection = RestoreChoices(selection, owned);
        var progress = owned.FirstOrDefault(x => x.CombatStyleId == selection.CombatStyleId);
        var definition = Catalog.Styles.FirstOrDefault(x => x.Id == selection.CombatStyleId);
        if (CombatStyleRules.ValidateSelection(progress, definition, selection) is { } issue)
            return new(false, issue);
        if (await boundary.PrepareMutationAsync(characterId, ct) is { } blocked) return new(false, blocked);
        // Settlement can award XP or refresh tracked entities. Use the current tracked progress.
        progress = selection.CombatStyleId is null ? null : await EnsureProgress(characterId, selection.CombatStyleId, ct);
        var row = await Selected(characterId, ct);
        if (row is null)
        {
            row = new() { CharacterId = characterId };
            repository.Add(row);
            _selections[characterId] = row;
        }
        row.CombatStyleId = selection.CombatStyleId;
        row.RefinementId = selection.RefinementId;
        row.UpgradeIds = selection.UpgradeIds.ToArray();
        row.MasteredUpgradeId = selection.MasteredUpgradeId;
        row.ChanneledPlayerEssenceId = null;
        if (progress is not null)
        {
            progress.RefinementId = selection.RefinementId;
            progress.UpgradeIds = selection.UpgradeIds.ToArray();
            progress.MasteredUpgradeId = selection.MasteredUpgradeId;
            progress.ChanneledPlayerEssenceId = null;
        }
        return new(true, "Combat Style saved.");
    }

    public async Task<CombatStyleXpGrantResult> GrantCapturedCombatXpAsync(Guid characterId, string? capturedCombatStyleId,
        long eligibleBaseXp, CancellationToken ct)
    {
        if (eligibleBaseXp < 0) throw new ArgumentOutOfRangeException(nameof(eligibleBaseXp));
        if (capturedCombatStyleId is null || eligibleBaseXp == 0) return new(0, 0, 0, 0, false);
        if (!Catalog.Styles.Any(x => x.Id == capturedCombatStyleId)) return new(0, 0, 0, 0, false);
        var style = await EnsureProgress(characterId, capturedCombatStyleId, ct);
        return CombatStyleProgression.Grant(style, eligibleBaseXp, Catalog.XpRequirements);
    }

    private static CombatStyleSelectionRequest RestoreChoices(CombatStyleSelectionRequest request, IReadOnlyList<CharacterCombatStyle> owned)
    {
        if (request.RestoreRememberedChoices
            && owned.FirstOrDefault(x => x.CombatStyleId == request.CombatStyleId) is { } style)
            request = request with { RefinementId = style.RefinementId,
                UpgradeIds = style.UpgradeIds.ToArray(), MasteredUpgradeId = style.MasteredUpgradeId,
                RestoreRememberedChoices = false };
        return CombatStyleRules.NormalizeSelection(request);
    }

    private static bool ValidActivity(EssenceCombatActivity activity) =>
        activity == EssenceCombatActivity.None || EssenceLoadoutSelection.IsValidSingleActivity(activity);
    private static void ValidateActivity(EssenceCombatActivity activity)
    {
        if (!ValidActivity(activity)) throw new ArgumentOutOfRangeException(nameof(activity), "The Essence loadout requires a single combat activity.");
    }

    private static IReadOnlyList<CombatStylePreviewFact> PreviewFacts(CombatStyleSnapshot? style)
    {
        if (style is null) return [];
        var tuning = style.Tuning;
        var milestones = style.MilestoneTuning;
        string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        string AdditiveBonus(double bonus) => $"+{Number(bonus * 100)}% flat increase";
        string BarrierBonus(double bonus) =>
            $"{AdditiveBonus(tuning.BarrierFraction * bonus)} · +{Number(200 * tuning.BarrierFraction * bonus)} Barrier";
        string HealthBonus(double bonus, double health) =>
            $"+{Number(bonus * 100)}% Health recovery (×{Number(1 + bonus)}) · {Number(health)} Health";
        var facts = new List<CombatStylePreviewFact>();
        if (style.Kind == CombatStyleKind.Reaper && tuning.Reaper is { } reaper)
        {
            var payout = style.RefinementId switch
            {
                CombatStyleIds.SoulSiphon => "Health restored",
                CombatStyleIds.DeathSentence => "Magical Damage after 15 seconds",
                _ => "damage dealt now"
            };
            facts.Add(new("100 damage harvested", $"{Number(100 * reaper.Multiplier(style.Level, style.RefinementId))} {payout}",
                "Your form decides how you use this damage. Upgrades, damage and healing bonuses, enemy defenses and missing Health can change the final amount."));
            facts.Add(new("Future ticks consumed", style.RefinementId == CombatStyleIds.LastRites ? "All remaining per stack" : "1 per stack",
                style.RefinementId == CombatStyleIds.LastRites
                    ? $"Your hit must leave the enemy at {Number(reaper.LastRitesHealthThreshold * 100)}% Health or less. Deep Roots has no effect with this form."
                    : "Harvest uses your existing Bleed, Burn and Poison. Stacks added during this cast must wait until the next cast."));
            if (style.HasUpgrade(CombatStyleIds.ClosingHand))
                facts.Add(new("Closing Hand", AdditiveBonus(reaper.UpgradeBonus),
                    $"Your opponent must be at or below {Number(100 * (style.HasMasteredUpgrade(CombatStyleIds.ClosingHand) ? reaper.MasteredClosingHandHealthThreshold : reaper.ClosingHandHealthThreshold))}% Health."));
            if (style.HasUpgrade(CombatStyleIds.Crosscut))
                facts.Add(new("Crosscut", AdditiveBonus(reaper.UpgradeBonus),
                    style.HasMasteredUpgrade(CombatStyleIds.Crosscut) ? "Harvest at least two of Bleed, Burn and Poison, or 3 stacks of any one condition." : "Harvest at least two of Bleed, Burn and Poison."));
            if (style.HasUpgrade(CombatStyleIds.DeepRoots))
                facts.Add(new("Deep Roots", style.RefinementId == CombatStyleIds.LastRites ? "Inactive with Last Rites" : AdditiveBonus(reaper.UpgradeBonus),
                    style.HasMasteredUpgrade(CombatStyleIds.DeepRoots) ? "At least one stack you harvest must still have damage left to deal afterwards." : "Every stack you harvest must still have damage left to deal afterwards."));
            if (style.Level >= CombatStyleProgression.OpeningTechniqueLevel)
                facts.Add(new("Opening Technique", $"Grave Seed: {reaper.OpeningPoisonStacks} Poison stacks", "Begin each battle by poisoning the enemy with the highest Max Health."));
            return facts;
        }
        if (style.Kind == CombatStyleKind.Bastion)
        {
            var health = 200 * tuning.HealthFraction * (style.HasUpgrade(CombatStyleIds.MeasuredRecovery) ? 1 + tuning.MeasuredRecoveryHealthBonus : 1);
            var barrier = 200 * tuning.BarrierFraction * (1 + style.BarrierMasteryBonus);
            facts.Add(new("200 healing received", $"{Number(health)} Health + {Number(barrier)} Barrier", "Before missing-Health and Barrier-cap limits."));
            if (style.RefinementId == CombatStyleIds.Rebuild) facts.Add(new("Rebuild", "200 Health", "Healing starts at or below 35% Health."));
            if (style.RefinementId == CombatStyleIds.Shelter) facts.Add(new("Shelter", $"{Number(barrier / 2)} Barrier per recipient", "Another living ally is present; recipient caps apply independently."));
            if (style.RefinementId == CombatStyleIds.Reprisal)
            {
                var absorbedFraction = tuning.ReprisalAbsorbedDamageFraction.GetValueOrDefault();
                var capFraction = tuning.ReprisalMaxHealthCapFraction.GetValueOrDefault();
                facts.Add(new("Reprisal", $"{Number(absorbedFraction * 100)}% of absorbed damage stored · cap {Number(capFraction * 100)}% Max Health",
                    "Enemy damage absorbed by your Barrier prepares bonus damage for your next damaging Essence."));
                facts.Add(new("Reprisal example", $"200 absorbed → {Number(Math.Min(200 * absorbedFraction, 1000 * capFraction))} bonus damage",
                    "With 1,000 Max Health and an empty reserve; added once to the first direct enemy attack attempt, before mitigation."));
            }
            if (style.HasUpgrade(CombatStyleIds.PreparedWall)) facts.Add(new("Prepared Wall", BarrierBonus(tuning.PreparedWallBarrierBonus),
                style.HasMasteredUpgrade(CombatStyleIds.PreparedWall) && milestones.PreparedWallEmptyBarrier
                    ? "Gain extra Barrier when you receive healing at 80% Health or higher, or when you have no Barrier."
                    : "Gain extra Barrier when you receive healing at 80% Health or higher."));
            if (style.HasUpgrade(CombatStyleIds.HoldTheBreach)) facts.Add(new("Hold the Breach", BarrierBonus(tuning.HoldTheBreachBarrierBonus),
                "Gain extra Barrier when you receive healing with no Barrier."));
            if (style.HasUpgrade(CombatStyleIds.MeasuredRecovery)) facts.Add(new("Measured Recovery",
                HealthBonus(tuning.MeasuredRecoveryHealthBonus, health),
                "Recover more Health when Fortification splits healing between Health and Barrier. Included in the healing example above."));
            if (style.HasMasteredUpgrade(CombatStyleIds.HoldTheBreach) && milestones.HoldTheBreachHealthBonus > 0)
            {
                facts.Add(new("Hold the Breach mastery", HealthBonus(milestones.HoldTheBreachHealthBonus, health * (1 + milestones.HoldTheBreachHealthBonus)),
                    "Recover more Health when you receive healing with no Barrier."));
                if (style.RefinementId == CombatStyleIds.Rebuild)
                    facts.Add(new("Rebuild with mastery", HealthBonus(milestones.HoldTheBreachHealthBonus, 200 * (1 + milestones.HoldTheBreachHealthBonus)),
                        "Recover more Health when you receive healing at 35% Health or lower with no Barrier."));
            }
            if (style.HasMasteredUpgrade(CombatStyleIds.MeasuredRecovery) && milestones.MeasuredRecoveryOverhealBarrierFraction > 0)
                facts.Add(new("Measured Recovery mastery", $"{Number(milestones.MeasuredRecoveryOverhealBarrierFraction * 100)}% of excess Health healing becomes Barrier",
                    "Uses the allocated Health portion after missing Health is restored; normal Barrier caps and Shelter distribution apply."));
            if (style.Level >= CombatStyleProgression.OpeningTechniqueLevel && milestones.OpeningBarrierFraction > 0)
                facts.Add(new("Opening Technique", $"{Number(milestones.OpeningBarrierFraction * 100)}% Max Health as starting Barrier", "Granted once at the start of combat."));
        }
        else
        {
            for (var charge = 0; charge <= tuning.ChargeCap; charge++)
            {
                var multiplier = tuning.ChanneledBaseMultiplier + charge * tuning.ChanneledPerCharge;
                if (charge > 0)
                {
                    multiplier += style.ChanneledMasteryBonus;
                    var fullCircuitMinimum = style.HasMasteredUpgrade(CombatStyleIds.FullCircuit) && milestones.FullCircuitMinimumCharge > 0
                        ? milestones.FullCircuitMinimumCharge : tuning.ChargeCap;
                    var partialFlowMaximum = style.HasMasteredUpgrade(CombatStyleIds.PartialFlow) && milestones.PartialFlowMaximumCharge > 0
                        ? milestones.PartialFlowMaximumCharge : 1;
                    if (charge >= fullCircuitMinimum && style.HasUpgrade(CombatStyleIds.FullCircuit)) multiplier += tuning.FullCircuitBonus;
                    if (charge <= partialFlowMaximum && style.HasUpgrade(CombatStyleIds.PartialFlow)) multiplier += tuning.PartialFlowBonus;
                }
                facts.Add(new($"{charge} Charge", $"{Number(multiplier * 100)}% of normal strength"));
            }
            facts.Add(new("Charge limit", $"{tuning.ChargeCap} Charge", tuning.DistinctContributors
                ? "Each of your other Essences builds 1 Charge when it casts, once between Channeled Essence casts."
                : "Your other Essences build 1 Charge every time they cast, even if the same Essence casts again."));
            if (style.HasUpgrade(CombatStyleIds.FullCircuit)) facts.Add(new("Full Circuit", AdditiveBonus(tuning.FullCircuitBonus),
                style.HasMasteredUpgrade(CombatStyleIds.FullCircuit) && milestones.FullCircuitMinimumCharge > 0
                    ? $"Gain this bonus when your Channeled Essence spends {milestones.FullCircuitMinimumCharge} or more Charge. Included in the Charge examples above."
                    : "Gain this bonus when your Channeled Essence spends maximum Charge. Included in the Charge examples above."));
            if (style.HasUpgrade(CombatStyleIds.PartialFlow)) facts.Add(new("Partial Flow", AdditiveBonus(tuning.PartialFlowBonus),
                style.HasMasteredUpgrade(CombatStyleIds.PartialFlow) && milestones.PartialFlowMaximumCharge > 1
                    ? $"Gain this bonus when your Channeled Essence spends 1 to {milestones.PartialFlowMaximumCharge} Charge. Included in the Charge examples above."
                    : "Gain this bonus when your Channeled Essence spends exactly 1 Charge. Included in the Charge examples above."));
            if (style.HasUpgrade(CombatStyleIds.EmergencyChannel)) facts.Add(new("Emergency Channel", $"{AdditiveBonus(tuning.EmergencyChannelBonus)} to healing and Barrier on yourself",
                style.HasMasteredUpgrade(CombatStyleIds.EmergencyChannel) && milestones.EmergencyChannelHealthThreshold >= 1
                    ? "Gain this bonus to the healing and Barrier your Channeled Essence gives you immediately whenever it spends at least 1 Charge, at any Health."
                    : "Gain this bonus to the healing and Barrier your Channeled Essence gives you immediately when it spends at least 1 Charge and you start the cast at 35% Health or lower."));
            if (style.Level >= CombatStyleProgression.OpeningTechniqueLevel && milestones.OpeningCharge > 0)
                facts.Add(new("Opening Technique", $"{milestones.OpeningCharge} starting Charge", "Granted once at the start of combat."));
        }
        return facts;
    }
}
