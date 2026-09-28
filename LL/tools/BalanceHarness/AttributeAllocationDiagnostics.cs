using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Items.Equipments.Progression;

namespace BalanceHarness;

public sealed record AttributeAllocationRank(string Group, string Cell, string Phase, int Rank, bool Pareto,
    double WinRate, double RemainingHealthFraction, double HealthDamage, double EffectiveHealing,
    double EnemyActionDeniedTicks, double MeanDurationTicks);

public static partial class AttributeAllocationStudy
{
    public static void ValidateDiagnostics(IReadOnlyDictionary<AttributeType, float>? points, int tier, int version, EquipmentBalance? balance = null)
    {
        if (points is null) return;
        if (version != AttributeRules.CurrentVersion || points.Count == 0)
            throw new InvalidDataException("Raw-point diagnostics require current rules and an explicit exchange.");
        double cost = 0;
        foreach (var (attribute, value) in points)
        {
            var priced = attribute switch { AttributeType.ArmorRating => AttributeType.Armor,
                AttributeType.ResistanceRating => AttributeType.Resistance, _ => attribute };
            if (!float.IsFinite(value) || Math.Abs(value) > 100000 || !AttributeRules.IsOrdinaryEquipmentAttribute(priced)
                || attribute is AttributeType.Armor or AttributeType.Resistance)
                throw new InvalidDataException("Diagnostic defense must use finite normalized ratings; only current gear attributes are priced.");
            var unitCost = balance?.AttributeCosts.GetValueOrDefault(priced, EquipmentStatBudgetCatalog.Get(priced).CostPerPoint)
                ?? EquipmentStatBudgetCatalog.Get(priced).CostPerPoint;
            if (priced is AttributeType.Power or AttributeType.MaxHealth or AttributeType.HealthRegeneration)
                unitCost /= EquipmentTierBudgetCurve.GetScale(tier);
            cost += value * unitCost;
        }
        if (Math.Abs(cost) > .001)
            throw new InvalidDataException($"Diagnostic exchanges must preserve normalized budget, delta was {cost}.");
    }

    private static void ApplyDiagnostics(CombatEntity actor, IReadOnlyDictionary<AttributeType, float>? points)
    {
        if (points is null) return;
        foreach (var (attribute, value) in points)
        {
            if (actor.CombatAttributes.GetValueOrDefault(attribute) + value < 0)
                throw new InvalidDataException($"Diagnostic exchange would make {attribute} negative.");
            actor.TemporaryModifiers.Add(new InstanceAttributeModifier(attribute, value, ModifierType.Flat));
        }
        AttributeCalculator.InitializeCombatAttributesFromBase(actor);
    }

    public static IReadOnlyList<AttributeAllocationRank> Rank(AttributeAllocationRequest request,
        IReadOnlyList<AttributeAllocationTrial> trials)
    {
        var rows = new List<AttributeAllocationRank>();
        foreach (var group in request.Cells.Where(x => x.ComparisonGroup is not null).GroupBy(x => x.ComparisonGroup!))
        {
            // A frontier is meaningful only when alternatives face the same encounter and build context.
            var contexts = group.Select(x => HarnessJson.Hash(new { x.Reference.CharacterLevel, x.Reference.Tier,
                x.Reference.EssenceIds, x.Doctrine, x.OpponentDoctrine, x.Idle, x.Tower, x.Dungeon, x.Opponent, x.Allies, x.AdditionalOpponents })).Distinct().Count();
            if (contexts != 1) throw new InvalidDataException($"Comparison group '{group.Key}' mixes encounter/build contexts.");
            var ids = group.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var phase in trials.Where(x => ids.Contains(x.Cell)).GroupBy(x => x.Phase))
            {
                var candidates = phase.GroupBy(x => x.Cell).Select(cell =>
                {
                    var wins = cell.Average(x => x.Candidate.EngineOutcome == (x.Mirrored ? BattleOutcome.Defeat : BattleOutcome.Victory) ? 1d : 0d);
                    double Stat(Func<EntityStats, double> metric, bool enemy = false) => cell.Average(x => x.Candidate.Statistics
                        .Where(s => s.Team == (x.Mirrored != enemy ? "Hostile" : "Friendly")).Sum(metric));
                    return new AttributeAllocationRank(group.Key, cell.Key, phase.Key, 0, false, wins,
                        cell.Average(x => x.Candidate.Statistics.Where(s => !s.IsSummonedEntity && s.Team == (x.Mirrored ? "Hostile" : "Friendly"))
                            .Select(s => s.MaxHealth > 0 ? (s.Health ?? 0) / (double)s.MaxHealth.Value : 0).DefaultIfEmpty().Average()),
                        Stat(s => s.DirectHealthDamage + s.PeriodicHealthDamage), Stat(s => s.HealingDone),
                        Stat(s => s.ActionDeniedTicks, enemy: true), cell.Average(x => x.Candidate.DurationTicks));
                }).ToArray();
                bool Dominates(AttributeAllocationRank a, AttributeAllocationRank b) =>
                    a.WinRate >= b.WinRate && a.RemainingHealthFraction >= b.RemainingHealthFraction
                    && a.HealthDamage >= b.HealthDamage && a.EffectiveHealing >= b.EffectiveHealing
                    && a.EnemyActionDeniedTicks >= b.EnemyActionDeniedTicks
                    && (a.WinRate > b.WinRate || a.RemainingHealthFraction > b.RemainingHealthFraction
                        || a.HealthDamage > b.HealthDamage || a.EffectiveHealing > b.EffectiveHealing
                        || a.EnemyActionDeniedTicks > b.EnemyActionDeniedTicks);
                rows.AddRange(candidates.OrderByDescending(x => x.WinRate).ThenByDescending(x => x.RemainingHealthFraction)
                    .ThenBy(x => x.Cell, StringComparer.Ordinal).Select((x, index) => x with
                    { Rank = index + 1, Pareto = !candidates.Any(other => Dominates(other, x)) }));
            }
        }
        return rows;
    }
}
