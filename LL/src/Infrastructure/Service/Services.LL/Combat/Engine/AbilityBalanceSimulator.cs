using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;

namespace Services.LL.Combat.Engine;

public sealed class AbilityBalanceSimulator : IAbilityBalanceSimulator
{
    private const string TrainingEssenceId = "essence.training";
    private const int DefaultMaxTicks = 6000;
    private const int BattleSummaryLimit = 250;
    private readonly IAbilityCatalogProvider _catalogProvider;
    private readonly IEssenceDefinitionRepository? _essenceDefinitions;

    public AbilityBalanceSimulator(
        IAbilityCatalogProvider catalogProvider,
        IEssenceDefinitionRepository? essenceDefinitions = null)
    {
        _catalogProvider = catalogProvider;
        _essenceDefinitions = essenceDefinitions;
    }

    public AbilityBalanceSimulationReport Run(AbilityBalanceSimulationRequest request)
    {
        var catalog = _catalogProvider.GetCatalog();
        var essenceNames = CreateEssenceNameIndex(_essenceDefinitions);
        var availableEssenceIds = catalog.AbilityIdsByOwningEssence.Keys
            .Where(x => !x.Equals(TrainingEssenceId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var normalized = NormalizeRequest(request, availableEssenceIds.Count);
        var random = new Random(normalized.RandomSeed);
        var compiledAbilities = AbilityCompiler.CompileAbilities(catalog.Abilities);
        var compiledAbilitiesByEssenceId = CreateCompiledAbilitiesByEssence(catalog, compiledAbilities);
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var compiledSummons = AbilityCompiler.CompileSummons(catalog.Summons);
        var savedCandidates = NormalizeCandidateTeams(normalized.CandidateTeams, normalized.TeamSize, normalized.EssencesPerParticipant)
            .ToList();
        var mode = savedCandidates.Count > 0 ? "SavedRoundRobin" : "RandomPool";
        var results = new Dictionary<string, TeamAccumulator>(StringComparer.Ordinal);
        var battleSummaries = new List<AbilityBalanceBattleSummary>();
        var battleIndex = 0;
        var candidateTeamCount = savedCandidates.Count;

        if (savedCandidates.Count > 0)
        {
            for (var leftIndex = 0; leftIndex < savedCandidates.Count; leftIndex++)
            {
                for (var rightIndex = leftIndex + 1; rightIndex < savedCandidates.Count; rightIndex++)
                {
                    for (var run = 0; run < normalized.BattleCount; run++)
                    {
                        var left = savedCandidates[leftIndex];
                        var right = savedCandidates[rightIndex];
                        var swapSides = run % 2 == 1;
                        RunBattle(
                            swapSides ? right : left,
                            swapSides ? left : right,
                            battleIndex++,
                            normalized.RandomSeed + battleIndex,
                            compiledAbilities,
                            compiledAbilitiesByEssenceId,
                            compiledStatuses,
                            compiledSummons,
                            essenceNames,
                            results,
                            battleSummaries);
                    }
                }
            }
        }
        else
        {
            var randomCandidates = CreateRandomCandidatePool(
                random,
                availableEssenceIds,
                normalized.TeamSize,
                normalized.EssencesPerParticipant,
                normalized.CandidatePoolSize);
            candidateTeamCount = randomCandidates.Count;

            for (var run = 0; run < normalized.BattleCount; run++)
            {
                var leftIndex = random.Next(randomCandidates.Count);
                var rightIndex = randomCandidates.Count == 1
                    ? leftIndex
                    : SelectDifferentIndex(random, randomCandidates.Count, leftIndex);
                var swapSides = run % 2 == 1;
                var friendly = randomCandidates[swapSides ? rightIndex : leftIndex];
                var hostile = randomCandidates[swapSides ? leftIndex : rightIndex];
                RunBattle(
                    friendly,
                    hostile,
                    battleIndex++,
                    normalized.RandomSeed + battleIndex,
                    compiledAbilities,
                    compiledAbilitiesByEssenceId,
                    compiledStatuses,
                    compiledSummons,
                    essenceNames,
                    results,
                    battleSummaries);
            }
        }

        var ranked = results.Values
            .Select(x => x.ToResult())
            .OrderByDescending(x => x.WinRate)
            .ThenByDescending(x => x.Battles)
            .ThenBy(x => x.AverageDuration)
            .ThenBy(x => x.Signature, StringComparer.Ordinal)
            .Take(normalized.TopResults)
            .ToList();

        return new AbilityBalanceSimulationReport(
            mode,
            normalized.BattleCount,
            battleIndex,
            normalized.TeamSize,
            normalized.EssencesPerParticipant,
            normalized.RandomSeed,
            candidateTeamCount,
            normalized.CandidatePoolSize,
            availableEssenceIds.Count,
            ranked,
            battleSummaries);
    }

    private static AbilityBalanceSimulationRequest NormalizeRequest(
        AbilityBalanceSimulationRequest request,
        int availableEssenceCount)
    {
        var teamSize = Math.Clamp(request.TeamSize <= 0 ? 1 : request.TeamSize, 1, 10);
        var maxEssenceCount = Math.Max(1, Math.Min(10, availableEssenceCount));
        var essenceCount = Math.Clamp(request.EssencesPerParticipant <= 0 ? 1 : request.EssencesPerParticipant, 1, maxEssenceCount);
        var battleCount = Math.Clamp(request.BattleCount <= 0 ? 100 : request.BattleCount, 1, 100_000);
        var topResults = Math.Clamp(request.TopResults <= 0 ? 25 : request.TopResults, 1, 500);
        var candidatePoolSize = Math.Clamp(request.CandidatePoolSize <= 0 ? Math.Max(25, topResults) : request.CandidatePoolSize, 2, 1_000);
        var seed = request.RandomSeed == 0 ? 1337 : request.RandomSeed;
        return request with
        {
            BattleCount = battleCount,
            TeamSize = teamSize,
            EssencesPerParticipant = essenceCount,
            RandomSeed = seed,
            TopResults = topResults,
            CandidatePoolSize = candidatePoolSize
        };
    }

    private static IEnumerable<AbilityBalanceTeamLoadout> NormalizeCandidateTeams(
        IReadOnlyList<AbilityBalanceTeamLoadout>? candidates,
        int fallbackTeamSize,
        int fallbackEssenceCount)
    {
        if (candidates is null)
            yield break;

        foreach (var candidate in candidates)
        {
            var participants = candidate.Participants
                .Where(x => x.EssenceIds.Count > 0)
                .Take(fallbackTeamSize)
                .Select(x => new AbilityBalanceParticipantLoadout(
                    x.EssenceIds
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                        .Take(fallbackEssenceCount)
                        .ToList()))
                .Where(x => x.EssenceIds.Count > 0)
                .ToList();

            if (participants.Count > 0)
                yield return new AbilityBalanceTeamLoadout(participants);
        }
    }

    private static AbilityBalanceTeamLoadout CreateRandomTeam(
        Random random,
        IReadOnlyList<string> availableEssenceIds,
        int teamSize,
        int essencesPerParticipant)
    {
        var participants = new List<AbilityBalanceParticipantLoadout>(teamSize);
        for (var index = 0; index < teamSize; index++)
        {
            var essenceIds = availableEssenceIds
                .OrderBy(_ => random.Next())
                .Take(essencesPerParticipant)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
            participants.Add(new AbilityBalanceParticipantLoadout(essenceIds));
        }

        return NormalizeTeam(new AbilityBalanceTeamLoadout(participants));
    }

    private static IReadOnlyList<AbilityBalanceTeamLoadout> CreateRandomCandidatePool(
        Random random,
        IReadOnlyList<string> availableEssenceIds,
        int teamSize,
        int essencesPerParticipant,
        int candidatePoolSize)
    {
        var candidates = new List<AbilityBalanceTeamLoadout>(candidatePoolSize);
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        var attempts = 0;
        var maxAttempts = candidatePoolSize * 50;

        while (candidates.Count < candidatePoolSize && attempts++ < maxAttempts)
        {
            var candidate = CreateRandomTeam(random, availableEssenceIds, teamSize, essencesPerParticipant);
            if (signatures.Add(CreateTeamSignature(candidate)))
                candidates.Add(candidate);
        }

        if (candidates.Count == 0)
            candidates.Add(CreateRandomTeam(random, availableEssenceIds, teamSize, essencesPerParticipant));

        return candidates;
    }

    private static int SelectDifferentIndex(Random random, int count, int excludedIndex)
    {
        var selected = random.Next(count - 1);
        return selected >= excludedIndex ? selected + 1 : selected;
    }

    private static void RunBattle(
        AbilityBalanceTeamLoadout friendlyLoadout,
        AbilityBalanceTeamLoadout hostileLoadout,
        int battleIndex,
        int randomSeed,
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities,
        IReadOnlyDictionary<string, IReadOnlyList<CompiledAbility>> compiledAbilitiesByEssenceId,
        IReadOnlyDictionary<string, CompiledStatus> compiledStatuses,
        IReadOnlyDictionary<string, CompiledSummon> compiledSummons,
        IReadOnlyDictionary<string, string> essenceNames,
        IDictionary<string, TeamAccumulator> results,
        ICollection<AbilityBalanceBattleSummary> battleSummaries)
    {
        var friendly = CreateCombatants("friendly", CombatTeam.Friendly, friendlyLoadout, compiledAbilitiesByEssenceId);
        var hostile = CreateCombatants("hostile", CombatTeam.Hostile, hostileLoadout, compiledAbilitiesByEssenceId);
        var engine = new FastCombatEngine(
            compiledStatuses,
            compiledSummons,
            compiledAbilities,
            new FastCombatEngineOptions(MaxTicks: DefaultMaxTicks, RandomSeed: randomSeed));
        var result = engine.Run(friendly, hostile);
        var friendlySignature = CreateTeamSignature(friendlyLoadout);
        var hostileSignature = CreateTeamSignature(hostileLoadout);
        var friendlyDisplayName = CreateTeamDisplayName(friendlyLoadout, essenceNames);
        var hostileDisplayName = CreateTeamDisplayName(hostileLoadout, essenceNames);
        var friendlyDamageDone = SumTeamStats(result, "Friendly", x => x.DamageDone);
        var friendlyDamageTaken = SumTeamStats(result, "Friendly", x => x.DamageTaken);
        var hostileDamageDone = SumTeamStats(result, "Hostile", x => x.DamageDone);
        var hostileDamageTaken = SumTeamStats(result, "Hostile", x => x.DamageTaken);

        AddResult(
            GetAccumulator(results, friendlySignature, friendlyDisplayName, friendlyLoadout),
            result.Outcome,
            friendlyPerspective: true,
            result.Duration,
            friendlyDamageDone,
            friendlyDamageTaken);
        AddResult(
            GetAccumulator(results, hostileSignature, hostileDisplayName, hostileLoadout),
            result.Outcome,
            friendlyPerspective: false,
            result.Duration,
            hostileDamageDone,
            hostileDamageTaken);

        if (battleSummaries.Count < BattleSummaryLimit)
        {
            battleSummaries.Add(new AbilityBalanceBattleSummary(
                battleIndex + 1,
                friendlySignature,
                friendlyDisplayName,
                hostileSignature,
                hostileDisplayName,
                result.Outcome.ToString(),
                result.Duration,
                friendlyDamageDone,
                friendlyDamageTaken,
                hostileDamageDone,
                hostileDamageTaken));
        }
    }

    private static List<RuntimeCombatant> CreateCombatants(
        string idPrefix,
        CombatTeam team,
        AbilityBalanceTeamLoadout loadout,
        IReadOnlyDictionary<string, IReadOnlyList<CompiledAbility>> compiledAbilitiesByEssenceId)
    {
        return loadout.Participants
            .Select((participant, index) => new RuntimeCombatant(
                $"{idPrefix}-{index + 1}",
                $"{idPrefix} {index + 1}",
                team,
                CreateBaselineAttributes(),
                SelectAbilities(participant, compiledAbilitiesByEssenceId),
                ["Role.Balance"]))
            .ToList();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<CompiledAbility>> CreateCompiledAbilitiesByEssence(
        AbilityCatalog catalog,
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities)
    {
        return catalog.AbilityIdsByOwningEssence.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<CompiledAbility>)pair.Value
                .Where(compiledAbilities.ContainsKey)
                .Select(abilityId => compiledAbilities[abilityId])
                .ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<CompiledAbility> SelectAbilities(
        AbilityBalanceParticipantLoadout participant,
        IReadOnlyDictionary<string, IReadOnlyList<CompiledAbility>> compiledAbilitiesByEssenceId)
    {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var essenceId in participant.EssenceIds)
        {
            if (!compiledAbilitiesByEssenceId.TryGetValue(essenceId, out var abilities))
                continue;

            foreach (var ability in abilities)
            {
                if (selected.Add(ability.Id))
                    yield return ability;
            }
        }
    }

    private static Dictionary<AttributeType, float> CreateBaselineAttributes() =>
        new()
        {
            [AttributeType.MaxHealth] = 200,
            [AttributeType.Power] = 50,
            [AttributeType.CritDamage] = 100
        };

    private static int SumTeamStats(
        CombatResult result,
        string team,
        Func<EntityStats, int> selector) =>
        result.EntityStats
            .Where(x => x.Team.Equals(team, StringComparison.OrdinalIgnoreCase))
            .Sum(selector);

    private static AbilityBalanceTeamLoadout NormalizeTeam(AbilityBalanceTeamLoadout team) =>
        new(team.Participants
            .Select(x => new AbilityBalanceParticipantLoadout(
                x.EssenceIds
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .OrderBy(x => string.Join("+", x.EssenceIds), StringComparer.Ordinal)
            .ToList());

    private static string CreateTeamSignature(AbilityBalanceTeamLoadout team) =>
        string.Join(" | ", NormalizeTeam(team).Participants.Select(x => string.Join("+", x.EssenceIds)));

    private static IReadOnlyDictionary<string, string> CreateEssenceNameIndex(
        IEssenceDefinitionRepository? essenceDefinitions) =>
        essenceDefinitions?.GetAll()
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => string.IsNullOrWhiteSpace(x.First().Name) ? FormatEssenceId(x.Key) : x.First().Name,
                StringComparer.OrdinalIgnoreCase)
        ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private static string CreateTeamDisplayName(
        AbilityBalanceTeamLoadout team,
        IReadOnlyDictionary<string, string> essenceNames) =>
        string.Join(" / ", NormalizeTeam(team).Participants.Select(participant =>
            string.Join(" + ", participant.EssenceIds.Select(essenceId => GetEssenceDisplayName(essenceId, essenceNames)))));

    private static string GetEssenceDisplayName(
        string essenceId,
        IReadOnlyDictionary<string, string> essenceNames) =>
        essenceNames.TryGetValue(essenceId, out var name) ? name : FormatEssenceId(essenceId);

    private static string FormatEssenceId(string essenceId)
    {
        var cleaned = essenceId
            .Replace("essence.", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("_", " ", StringComparison.OrdinalIgnoreCase)
            .Replace(".", " ", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return string.IsNullOrWhiteSpace(cleaned)
            ? essenceId
            : string.Join(" ", cleaned
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }

    private static TeamAccumulator GetAccumulator(
        IDictionary<string, TeamAccumulator> results,
        string signature,
        string displayName,
        AbilityBalanceTeamLoadout loadout)
    {
        if (!results.TryGetValue(signature, out var accumulator))
        {
            accumulator = new TeamAccumulator(signature, displayName, NormalizeTeam(loadout));
            results.Add(signature, accumulator);
        }

        return accumulator;
    }

    private static void AddResult(
        TeamAccumulator accumulator,
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

    private sealed class TeamAccumulator
    {
        public TeamAccumulator(string signature, string displayName, AbilityBalanceTeamLoadout loadout)
        {
            Signature = signature;
            DisplayName = displayName;
            Loadout = loadout;
        }

        public string Signature { get; }
        public string DisplayName { get; }
        public AbilityBalanceTeamLoadout Loadout { get; }
        public int Battles { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public double TotalDuration { get; set; }
        public double TotalDamageDone { get; set; }
        public double TotalDamageTaken { get; set; }

        public AbilityBalanceCombinationResult ToResult() =>
            new(
                Signature,
                DisplayName,
                Loadout.Participants,
                Battles,
                Wins,
                Losses,
                Draws,
                Battles == 0 ? 0 : Wins / (double)Battles,
                Battles == 0 ? 0 : Losses / (double)Battles,
                Battles == 0 ? 0 : Draws / (double)Battles,
                Battles == 0 ? 0 : TotalDuration / Battles,
                Battles == 0 ? 0 : TotalDamageDone / Battles,
                Battles == 0 ? 0 : TotalDamageTaken / Battles);
    }
}
