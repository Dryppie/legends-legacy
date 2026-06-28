using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;

namespace Services.LL.CombatStyles;

public sealed class CombatStyleBalanceSimulator : ICombatStyleBalanceSimulator
{
    private const int BattleSummaryLimit = 250;
    private readonly ICombatStyleDefinitionProvider _definitions;

    public CombatStyleBalanceSimulator(ICombatStyleDefinitionProvider definitions)
    {
        _definitions = definitions;
    }

    public CombatStyleBalanceSimulationReport Run(CombatStyleBalanceSimulationRequest request)
    {
        var battleCount = Math.Clamp(request.BattleCount <= 0 ? 3 : request.BattleCount, 1, 1_000);
        var styleLevel = Math.Clamp(request.StyleLevel <= 0 ? 40 : request.StyleLevel, 1, 50);
        var randomSeed = request.RandomSeed == 0 ? 7331 : request.RandomSeed;
        var topResults = Math.Clamp(request.TopResults <= 0 ? 25 : request.TopResults, 1, 500);
        var candidates = CreateCandidates(styleLevel, request.IncludeFocuses).ToList();
        var abilities = CreateAbilitySpecs();
        var compiledAbilities = AbilityCompiler.CompileAbilities(abilities);
        var compiledStatuses = AbilityCompiler.CompileStatuses(CreateStatusSpecs());
        var compiledSummons = AbilityCompiler.CompileSummons(CreateSummonSpecs());
        var results = candidates.ToDictionary(
            candidate => candidate.Signature,
            candidate => new Accumulator(candidate),
            StringComparer.Ordinal);
        var summaries = new List<CombatStyleBalanceBattleSummary>();
        var battleIndex = 0;

        for (var leftIndex = 0; leftIndex < candidates.Count; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < candidates.Count; rightIndex++)
            {
                for (var run = 0; run < battleCount; run++)
                {
                    var swapSides = run % 2 == 1;
                    var friendly = swapSides ? candidates[rightIndex] : candidates[leftIndex];
                    var hostile = swapSides ? candidates[leftIndex] : candidates[rightIndex];
                    RunBattle(
                        friendly,
                        hostile,
                        battleIndex++,
                        randomSeed + battleIndex,
                        compiledAbilities,
                        compiledStatuses,
                        compiledSummons,
                        results,
                        summaries);
                }
            }
        }

        var ranked = results.Values
            .Select(x => x.ToResult())
            .OrderByDescending(x => x.WinRate)
            .ThenByDescending(x => x.Battles)
            .ThenBy(x => x.AverageDuration)
            .ThenBy(x => x.StyleId, StringComparer.Ordinal)
            .ThenBy(x => x.FocusId, StringComparer.Ordinal)
            .Take(topResults)
            .ToList();

        return new CombatStyleBalanceSimulationReport(
            battleCount,
            battleIndex,
            styleLevel,
            randomSeed,
            candidates.Count,
            ranked,
            summaries);
    }

    private IEnumerable<StyleCandidate> CreateCandidates(int styleLevel, bool includeFocuses)
    {
        foreach (var style in _definitions.GetAll().OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
        {
            yield return new StyleCandidate(style, null, styleLevel);

            if (!includeFocuses)
                continue;

            foreach (var focus in style.Focuses.Where(x => styleLevel >= x.UnlockLevel).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                yield return new StyleCandidate(style, focus, styleLevel);
        }
    }

    private void RunBattle(
        StyleCandidate friendlyStyle,
        StyleCandidate hostileStyle,
        int battleIndex,
        int randomSeed,
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities,
        IReadOnlyDictionary<string, CompiledStatus> compiledStatuses,
        IReadOnlyDictionary<string, CompiledSummon> compiledSummons,
        IReadOnlyDictionary<string, Accumulator> results,
        ICollection<CombatStyleBalanceBattleSummary> summaries)
    {
        var friendly = CreateCombatants("friendly", CombatTeam.Friendly, compiledAbilities);
        var hostile = CreateCombatants("hostile", CombatTeam.Hostile, compiledAbilities);
        var engine = new FastCombatEngine(
            compiledStatuses,
            compiledSummons,
            compiledAbilities,
            new FastCombatEngineOptions(MaxTicks: 1200, RandomSeed: randomSeed),
            friendlyStyle.ToSnapshot(),
            hostileStyle.ToSnapshot(),
            _definitions);
        var result = engine.Run(friendly, hostile);
        var friendlyDamageDone = SumTeamStats(result, "Friendly", x => x.DamageDone);
        var friendlyDamageTaken = SumTeamStats(result, "Friendly", x => x.DamageTaken);
        var hostileDamageDone = SumTeamStats(result, "Hostile", x => x.DamageDone);
        var hostileDamageTaken = SumTeamStats(result, "Hostile", x => x.DamageTaken);

        AddResult(
            results[friendlyStyle.Signature],
            result.Outcome,
            friendlyPerspective: true,
            result.Duration,
            friendlyDamageDone,
            friendlyDamageTaken);
        AddResult(
            results[hostileStyle.Signature],
            result.Outcome,
            friendlyPerspective: false,
            result.Duration,
            hostileDamageDone,
            hostileDamageTaken);

        if (summaries.Count >= BattleSummaryLimit)
            return;

        summaries.Add(new CombatStyleBalanceBattleSummary(
            battleIndex + 1,
            friendlyStyle.Style.Id,
            friendlyStyle.Style.Name,
            friendlyStyle.Focus?.Id,
            hostileStyle.Style.Id,
            hostileStyle.Style.Name,
            hostileStyle.Focus?.Id,
            result.Outcome.ToString(),
            result.Duration,
            friendlyDamageDone,
            friendlyDamageTaken,
            hostileDamageDone,
            hostileDamageTaken));
    }

    private static List<RuntimeCombatant> CreateCombatants(
        string idPrefix,
        CombatTeam team,
        IReadOnlyDictionary<string, CompiledAbility> abilities) =>
        [
            new RuntimeCombatant(
                $"{idPrefix}-1",
                $"{idPrefix} 1",
                team,
                CreateBaselineAttributes(),
                abilities.Values,
                ["Role.Balance"])
        ];

    private static Dictionary<AttributeType, float> CreateBaselineAttributes() =>
        new()
        {
            [AttributeType.MaxHealth] = 550,
            [AttributeType.Power] = 50,
            [AttributeType.Spirit] = 45,
            [AttributeType.Precision] = 35,
            [AttributeType.CritDamage] = 100
        };

    private static IReadOnlyList<AbilitySpec> CreateAbilitySpecs() =>
    [
        DamageAbility("ability.balance.melee", "Measured Cut", 26, AttackType.Melee, ["Melee", "Physical"], 24),
        DamageAbility("ability.balance.ranged", "Steady Shot", 24, AttackType.Ranged, ["Ranged", "Physical"], 26),
        DamageAbility("ability.balance.arcane", "Arcane Spark", 22, AttackType.None, ["Magic", "Spell"], 28, DamageType.Magical),
        new()
        {
            Id = "ability.balance.ward",
            Kind = AbilitySpecKind.Active,
            Name = "Guarding Light",
            CooldownTicks = 34,
            Tags = ["Barrier", "Healing", "Holy"],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "effect.balance.ward",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 38,
                    ProcCoefficient = 0.75m,
                    Tags = ["Barrier", "Holy"]
                }
            ]
        },
        new()
        {
            Id = "ability.balance.hex",
            Kind = AbilitySpecKind.Active,
            Name = "Binding Hex",
            CooldownTicks = 36,
            Tags = ["Curse", "Control", "Debuff", "DoT"],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "effect.balance.hex",
                    Operation = AbilityEffectOperation.ApplyStatus,
                    Target = AbilityTargetSelector.CurrentTarget,
                    StatusId = "status.balance_hex",
                    BaseValue = 12,
                    ProcCoefficient = 0.6m,
                    Tags = ["Curse", "Control", "Debuff", "DoT"]
                }
            ]
        },
        new()
        {
            Id = "ability.balance.summon",
            Kind = AbilitySpecKind.Active,
            Name = "Call Helper",
            CooldownTicks = 70,
            Tags = ["Summon"],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "effect.balance.summon",
                    Operation = AbilityEffectOperation.Summon,
                    Target = AbilityTargetSelector.Self,
                    SummonId = "summon.balance_helper",
                    ProcCoefficient = 0.4m,
                    Tags = ["Summon"]
                }
            ]
        },
        DamageAbility("ability.balance.summon_strike", "Helper Strike", 12, AttackType.Melee, ["Summon", "Melee"], 35)
    ];

    private static AbilitySpec DamageAbility(
        string id,
        string name,
        int baseValue,
        AttackType attackType,
        IReadOnlyList<string> tags,
        int cooldownTicks,
        DamageType damageType = DamageType.Physical) =>
        new()
        {
            Id = id,
            Kind = AbilitySpecKind.Active,
            Name = name,
            CooldownTicks = cooldownTicks,
            Tags = [.. tags],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = $"{id}.damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = baseValue,
                    AttackType = attackType,
                    DamageType = damageType,
                    ProcCoefficient = 1m,
                    Tags = [.. tags]
                }
            ]
        };

    private static IReadOnlyList<StatusSpec> CreateStatusSpecs() =>
    [
        new()
        {
            Id = "status.balance_hex",
            Name = "Binding Hex",
            DurationTicks = 90,
            MaxStacks = 1,
            Tags = ["Curse", "Control", "Debuff", "DoT"]
        }
    ];

    private static IReadOnlyList<SummonSpec> CreateSummonSpecs() =>
    [
        new()
        {
            Id = "summon.balance_helper",
            Name = "Helper",
            DurationTicks = 180,
            MaxActive = 1,
            Tags = ["Summon"],
            AbilityIds = ["ability.balance.summon_strike"],
            Attributes =
            [
                new SummonAttributeSpec
                {
                    Attribute = AttributeType.MaxHealth,
                    BaseValue = 90,
                    MinimumValue = 1
                },
                new SummonAttributeSpec
                {
                    Attribute = AttributeType.Power,
                    BaseValue = 22,
                    MinimumValue = 1
                }
            ]
        }
    ];

    private static int SumTeamStats(
        CombatResult result,
        string team,
        Func<EntityStats, int> selector) =>
        result.EntityStats
            .Where(x => x.Team.Equals(team, StringComparison.OrdinalIgnoreCase))
            .Sum(selector);

    private static void AddResult(
        Accumulator accumulator,
        BattleOutcome outcome,
        bool friendlyPerspective,
        int duration,
        int damageDone,
        int damageTaken)
    {
        var won = outcome == BattleOutcome.Victory && friendlyPerspective
                  || outcome == BattleOutcome.Defeat && !friendlyPerspective;
        var lost = outcome == BattleOutcome.Defeat && friendlyPerspective
                   || outcome == BattleOutcome.Victory && !friendlyPerspective;

        if (won)
            accumulator.Wins++;
        else if (lost)
            accumulator.Losses++;
        else
            accumulator.Draws++;

        accumulator.Battles++;
        accumulator.TotalDuration += duration;
        accumulator.TotalDamageDone += damageDone;
        accumulator.TotalDamageTaken += damageTaken;
    }

    private sealed record StyleCandidate(
        CombatStyleDefinition Style,
        CombatStyleFocusDefinition? Focus,
        int Level)
    {
        public string Signature => Focus is null ? Style.Id : $"{Style.Id}:{Focus.Id}";

        public CombatStyleSnapshot ToSnapshot() =>
            new(
                Style.Id,
                Style.Name,
                Level,
                0,
                Focus?.Id,
                Focus?.Name);
    }

    private sealed class Accumulator
    {
        private readonly StyleCandidate _candidate;

        public Accumulator(StyleCandidate candidate)
        {
            _candidate = candidate;
        }

        public int Battles { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public double TotalDuration { get; set; }
        public double TotalDamageDone { get; set; }
        public double TotalDamageTaken { get; set; }

        public CombatStyleBalanceResult ToResult() =>
            new(
                _candidate.Style.Id,
                _candidate.Style.Name,
                _candidate.Focus?.Id,
                _candidate.Focus?.Name,
                Battles,
                Wins,
                Losses,
                Draws,
                Battles == 0 ? 0 : Wins / (double)Battles,
                Battles == 0 ? 0 : TotalDuration / Battles,
                Battles == 0 ? 0 : TotalDamageDone / Battles,
                Battles == 0 ? 0 : TotalDamageTaken / Battles);
    }
}
