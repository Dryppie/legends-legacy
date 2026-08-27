using Application.Interfaces.Services.LL.Essences;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.PowerRatings;

namespace Services.LL.Combat.Engine;

public sealed class AbilityBalanceSimulator : IAbilityBalanceSimulator
{
    public const int AlgorithmVersion = 2;
    private const string TrainingEssenceId = "essence.training";
    private const int DefaultMaxTicks = 6000;
    private const int BattleSummaryLimit = 250;
    private readonly IAbilityCatalogProvider _catalogProvider;
    private readonly IEssenceDefinitionRepository? _essenceDefinitions;
    private readonly CanonicalEquipmentBuildFactory? _canonicalBuilds;

    public AbilityBalanceSimulator(
        IAbilityCatalogProvider catalogProvider,
        IEssenceDefinitionRepository? essenceDefinitions = null,
        CanonicalEquipmentBuildFactory? canonicalBuilds = null)
    {
        _catalogProvider = catalogProvider;
        _essenceDefinitions = essenceDefinitions;
        _canonicalBuilds = canonicalBuilds;
    }

    public AbilityBalanceSimulationReport Run(
        AbilityBalanceSimulationRequest request,
        CancellationToken cancellationToken = default,
        Action<AbilityBalanceSimulationProgress>? progress = null)
    {
        var catalog = _catalogProvider.GetCatalog();
        var compiledAbilities = AbilityCompiler.CompileAbilities(catalog.Abilities);
        var balanceEssences = CreateBalanceEssenceIndex(catalog, compiledAbilities, _essenceDefinitions);
        var essenceNames = balanceEssences.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.DisplayName,
            StringComparer.OrdinalIgnoreCase);
        var availableEssenceIds = balanceEssences.Keys
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var essenceFamilies = CreateEssenceFamilies(balanceEssences);
        var normalized = NormalizeRequest(request, availableEssenceIds.Count, essenceFamilies.Count);
        var random = new Random(normalized.RandomSeed);
        var compiledAbilitiesByEssenceId = balanceEssences.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Abilities,
            StringComparer.OrdinalIgnoreCase);
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var compiledSummons = AbilityCompiler.CompileSummons(catalog.Summons);
        var participantAttributes = CreateParticipantAttributes(normalized);
        var participantAttributesByRole = CreateParticipantAttributesByRole(normalized);
        var savedCandidates = NormalizeCandidateTeams(
                normalized.CandidateTeams,
                normalized.TeamSize,
                normalized.EssencesPerParticipant,
                balanceEssences,
                normalized.UseCanonicalRoles)
            .ToList();
        var mode = savedCandidates.Count > 0 ? "SavedRoundRobin" : "RandomPool";
        var results = new Dictionary<string, TeamAccumulator>(StringComparer.Ordinal);
        var matchups = new Dictionary<string, MatchupAccumulator>(StringComparer.Ordinal);
        var battleSummaries = new List<AbilityBalanceBattleSummary>();
        var battleIndex = 0;
        var candidateTeamCount = savedCandidates.Count;
        var totalBattles = savedCandidates.Count > 0
            ? (long)savedCandidates.Count * (savedCandidates.Count - 1) / 2 * normalized.BattleCount
            : normalized.BattleCount;
        var progressInterval = Math.Max(1L, totalBattles / 100L);
        var batchSize = Math.Clamp(Environment.ProcessorCount * 16, 64, 1024);
        var battleBatch = new List<BalanceBattleWorkItem>(batchSize);

        void FlushBattleBatch()
        {
            if (battleBatch.Count == 0)
                return;

            var executions = new BalanceBattleExecutionResult[battleBatch.Count];
            Parallel.For(
                0,
                battleBatch.Count,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
                },
                index =>
                {
                    var work = battleBatch[index];
                    executions[index] = RunBattle(
                        work.Friendly,
                        work.Hostile,
                        work.BattleIndex,
                        work.RandomSeed,
                        compiledAbilities,
                        compiledAbilitiesByEssenceId,
                        compiledStatuses,
                        compiledSummons,
                        participantAttributesByRole);
                });

            foreach (var execution in executions)
            {
                AccumulateBattle(execution, essenceNames, results, matchups, battleSummaries);
                ReportProgress(
                    progress,
                    execution.BattleIndex + 1,
                    totalBattles,
                    progressInterval);
            }

            battleBatch.Clear();
        }

        void QueueBattle(
            AbilityBalanceTeamLoadout friendly,
            AbilityBalanceTeamLoadout hostile)
        {
            var index = battleIndex++;
            battleBatch.Add(new BalanceBattleWorkItem(
                friendly,
                hostile,
                index,
                normalized.RandomSeed + battleIndex));
            if (battleBatch.Count >= batchSize)
                FlushBattleBatch();
        }

        if (savedCandidates.Count > 0)
        {
            for (var leftIndex = 0; leftIndex < savedCandidates.Count; leftIndex++)
            {
                for (var rightIndex = leftIndex + 1; rightIndex < savedCandidates.Count; rightIndex++)
                {
                    for (var run = 0; run < normalized.BattleCount; run++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var left = savedCandidates[leftIndex];
                        var right = savedCandidates[rightIndex];
                        var swapSides = run % 2 == 1;
                        QueueBattle(
                            swapSides ? right : left,
                            swapSides ? left : right);
                    }
                }
            }
        }
        else
        {
            var randomCandidates = CreateRandomCandidatePool(
                random,
                essenceFamilies,
                normalized.TeamSize,
                normalized.EssencesPerParticipant,
                normalized.CandidatePoolSize,
                normalized.UseCanonicalRoles);
            candidateTeamCount = randomCandidates.Count;

            for (var run = 0; run < normalized.BattleCount; run++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var leftIndex = random.Next(randomCandidates.Count);
                var rightIndex = randomCandidates.Count == 1
                    ? leftIndex
                    : SelectDifferentIndex(random, randomCandidates.Count, leftIndex);
                var swapSides = run % 2 == 1;
                var friendly = randomCandidates[swapSides ? rightIndex : leftIndex];
                var hostile = randomCandidates[swapSides ? leftIndex : rightIndex];
                QueueBattle(
                    friendly,
                    hostile);
            }
        }

        FlushBattleBatch();

        var allResults = results.Values
            .Select(x => x.ToResult())
            .OrderByDescending(x => x.WinRate)
            .ThenByDescending(x => x.Battles)
            .ThenBy(x => x.AverageDuration)
            .ThenBy(x => x.Signature, StringComparer.Ordinal)
            .ToList();
        var ranked = allResults
            .Take(normalized.TopResults)
            .ToList();
        var essenceResults = CreateEssenceResults(allResults, essenceNames);
        progress?.Invoke(new AbilityBalanceSimulationProgress(totalBattles, totalBattles));

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
            normalized.EquipmentTier,
            normalized.EquipmentRarity,
            normalized.EquipmentProfile,
            participantAttributes.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value),
            balanceEssences.Values
                .OrderBy(essence => essence.Id, StringComparer.OrdinalIgnoreCase)
                .Select(essence => new AbilityBalanceEssenceDefinition(
                    essence.Id,
                    essence.SourceMonsterId,
                    essence.Abilities.Select(ability => ability.Id).ToList()))
                .ToList(),
            ranked,
            essenceResults,
            battleSummaries,
            matchups.Values
                .Select(matchup => matchup.ToResult())
                .OrderBy(matchup => matchup.FirstSignature, StringComparer.Ordinal)
                .ThenBy(matchup => matchup.SecondSignature, StringComparer.Ordinal)
                .ToArray(),
            participantAttributesByRole.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyDictionary<string, float>)pair.Value.ToDictionary(
                    attribute => attribute.Key.ToString(),
                    attribute => attribute.Value),
                StringComparer.OrdinalIgnoreCase));
    }

    private static AbilityBalanceSimulationRequest NormalizeRequest(
        AbilityBalanceSimulationRequest request,
        int availableEssenceCount,
        int availableCreatureCount)
    {
        var teamSize = Math.Clamp(request.TeamSize <= 0 ? 1 : request.TeamSize, 1, 15);
        var maxEssenceCount = Math.Max(1, Math.Min(10, availableCreatureCount));
        var essenceCount = Math.Clamp(request.EssencesPerParticipant <= 0 ? 1 : request.EssencesPerParticipant, 1, maxEssenceCount);
        var battleCount = Math.Max(request.BattleCount <= 0 ? 100 : request.BattleCount, 1);
        var topResults = Math.Clamp(request.TopResults <= 0 ? 25 : request.TopResults, 1, 1_000);
        var candidatePoolSize = Math.Clamp(request.CandidatePoolSize <= 0 ? Math.Max(25, topResults) : request.CandidatePoolSize, 2, 1_000);
        var seed = request.RandomSeed == 0 ? 1337 : request.RandomSeed;
        var equipmentTier = Math.Clamp(
            request.EquipmentTier <= 0 ? EquipmentTierBudgetCurve.ReferenceEndTier : request.EquipmentTier,
            EquipmentStatBudgetCatalog.MinimumTier,
            EquipmentTierBudgetCurve.MaximumSupportedTier);
        var equipmentRarity = Enum.TryParse<Rarity>(request.EquipmentRarity, true, out var parsedRarity)
            && parsedRarity <= Rarity.Legendary
            ? parsedRarity
            : Rarity.Epic;
        var equipmentProfile = Enum.TryParse<CanonicalPartyProfile>(request.EquipmentProfile, true, out var parsedProfile)
            ? parsedProfile
            : CanonicalPartyProfile.Balanced;
        return request with
        {
            BattleCount = battleCount,
            TeamSize = teamSize,
            EssencesPerParticipant = essenceCount,
            RandomSeed = seed,
            TopResults = topResults,
            CandidatePoolSize = candidatePoolSize,
            EquipmentTier = equipmentTier,
            EquipmentRarity = equipmentRarity.ToString(),
            EquipmentProfile = equipmentProfile.ToString()
        };
    }

    private IReadOnlyDictionary<AttributeType, float> CreateParticipantAttributes(
        AbilityBalanceSimulationRequest request)
    {
        if (_canonicalBuilds is null)
            return CreateBaselineAttributes();

        var rarity = Enum.Parse<Rarity>(request.EquipmentRarity, true);
        var profile = Enum.Parse<CanonicalPartyProfile>(request.EquipmentProfile, true);
        var rung = _canonicalBuilds.GetProgressionLadder().Single(candidate =>
            candidate.Tier == request.EquipmentTier &&
            candidate.Rarity == rarity &&
            candidate.Quality == ItemQuality.Standard);
        var build = _canonicalBuilds.CreateBuild(profile, rung);
        var baseAttributes = build.Character.BaseAttributes.ToDictionary(
            attribute => attribute.AttributeType,
            attribute => attribute.Value);
        var equipmentModifiers = AttributeCalculator.ProjectEquipmentModifiers(
            build.Equipment,
            build.Character.Level);

        var participantAttributes = AttributeCalculator.CalculateProjectedAttributes(
            baseAttributes,
            equipmentModifiers);

        return participantAttributes;
    }

    private IReadOnlyDictionary<string, IReadOnlyDictionary<AttributeType, float>>
        CreateParticipantAttributesByRole(AbilityBalanceSimulationRequest request)
    {
        if (!request.UseCanonicalRoles)
        {
            return new Dictionary<string, IReadOnlyDictionary<AttributeType, float>>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Balance"] = CreateParticipantAttributes(request)
            };
        }

        if (_canonicalBuilds is null)
            throw new InvalidOperationException("Canonical role discovery requires canonical equipment builds.");

        var rarity = Enum.Parse<Rarity>(request.EquipmentRarity, true);
        var rung = _canonicalBuilds.GetProgressionLadder().Single(candidate =>
            candidate.Tier == request.EquipmentTier
            && candidate.Rarity == rarity
            && candidate.Quality == ItemQuality.Standard);
        return CanonicalCooperativeRosterCatalog.CreateParty(request.TeamSize)
            .Select(slot => slot.Role)
            .Distinct()
            .ToDictionary(
                role => role.ToString(),
                role => (IReadOnlyDictionary<AttributeType, float>)ProjectAttributes(
                    _canonicalBuilds.CreateBuild(role, rung, essenceCount: 0)),
                StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<AttributeType, float> ProjectAttributes(
        CanonicalEquipmentBuild build)
    {
        var baseAttributes = build.Character.BaseAttributes.ToDictionary(
            attribute => attribute.AttributeType,
            attribute => attribute.Value);
        var equipmentModifiers = AttributeCalculator.ProjectEquipmentModifiers(
            build.Equipment,
            build.Character.Level);
        return AttributeCalculator.CalculateProjectedAttributes(baseAttributes, equipmentModifiers);
    }

    private static void ReportProgress(
        Action<AbilityBalanceSimulationProgress>? progress,
        long completed,
        long total,
        long interval)
    {
        if (progress is not null && (completed == total || completed % interval == 0))
            progress(new AbilityBalanceSimulationProgress(completed, total));
    }

    private static IEnumerable<AbilityBalanceTeamLoadout> NormalizeCandidateTeams(
        IReadOnlyList<AbilityBalanceTeamLoadout>? candidates,
        int fallbackTeamSize,
        int fallbackEssenceCount,
        IReadOnlyDictionary<string, BalanceEssence> balanceEssences,
        bool useCanonicalRoles)
    {
        if (candidates is null)
            yield break;

        foreach (var candidate in candidates)
        {
            var roles = useCanonicalRoles
                ? CanonicalCooperativeRosterCatalog.CreateParty(fallbackTeamSize)
                    .Select(slot => slot.Role.ToString())
                    .ToArray()
                : Enumerable.Repeat("Balance", fallbackTeamSize).ToArray();
            var participants = candidate.Participants
                .Where(x => x.EssenceIds.Count > 0)
                .Take(fallbackTeamSize)
                .Select((participant, participantIndex) =>
                {
                    var requestedEssenceIds = participant.EssenceIds
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    ValidateParticipantEssences(requestedEssenceIds, participantIndex, balanceEssences);
                    var essenceIds = requestedEssenceIds
                        .Take(fallbackEssenceCount)
                        .ToList();
                    return new AbilityBalanceParticipantLoadout(essenceIds, roles[participantIndex]);
                })
                .Where(x => x.EssenceIds.Count > 0)
                .ToList();

            if (participants.Count > 0)
                yield return new AbilityBalanceTeamLoadout(participants);
        }
    }

    private static AbilityBalanceTeamLoadout CreateRandomTeam(
        Random random,
        IReadOnlyList<BalanceEssenceFamily> essenceFamilies,
        int teamSize,
        int essencesPerParticipant,
        bool useCanonicalRoles)
    {
        var participants = new List<AbilityBalanceParticipantLoadout>(teamSize);
        var roles = useCanonicalRoles
            ? CanonicalCooperativeRosterCatalog.CreateParty(teamSize)
                .Select(slot => slot.Role.ToString())
                .ToArray()
            : Enumerable.Repeat("Balance", teamSize).ToArray();
        for (var index = 0; index < teamSize; index++)
        {
            var essenceIds = essenceFamilies
                .OrderBy(_ => random.Next())
                .Take(essencesPerParticipant)
                .Select(family => family.EssenceIds[random.Next(family.EssenceIds.Count)])
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
            participants.Add(new AbilityBalanceParticipantLoadout(essenceIds, roles[index]));
        }

        return NormalizeTeam(new AbilityBalanceTeamLoadout(participants));
    }

    private static IReadOnlyList<AbilityBalanceTeamLoadout> CreateRandomCandidatePool(
        Random random,
        IReadOnlyList<BalanceEssenceFamily> essenceFamilies,
        int teamSize,
        int essencesPerParticipant,
        int candidatePoolSize,
        bool useCanonicalRoles)
    {
        var candidates = new List<AbilityBalanceTeamLoadout>(candidatePoolSize);
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        var attempts = 0;
        var maxAttempts = candidatePoolSize * 50;

        while (candidates.Count < candidatePoolSize && attempts++ < maxAttempts)
        {
            var candidate = CreateRandomTeam(
                random,
                essenceFamilies,
                teamSize,
                essencesPerParticipant,
                useCanonicalRoles);
            if (signatures.Add(CreateTeamSignature(candidate)))
                candidates.Add(candidate);
        }

        if (candidates.Count == 0)
            candidates.Add(CreateRandomTeam(
                random,
                essenceFamilies,
                teamSize,
                essencesPerParticipant,
                useCanonicalRoles));

        return candidates;
    }

    private static int SelectDifferentIndex(Random random, int count, int excludedIndex)
    {
        var selected = random.Next(count - 1);
        return selected >= excludedIndex ? selected + 1 : selected;
    }

    private static BalanceBattleExecutionResult RunBattle(
        AbilityBalanceTeamLoadout friendlyLoadout,
        AbilityBalanceTeamLoadout hostileLoadout,
        int battleIndex,
        int randomSeed,
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities,
        IReadOnlyDictionary<string, IReadOnlyList<CompiledAbility>> compiledAbilitiesByEssenceId,
        IReadOnlyDictionary<string, CompiledStatus> compiledStatuses,
        IReadOnlyDictionary<string, CompiledSummon> compiledSummons,
        IReadOnlyDictionary<string, IReadOnlyDictionary<AttributeType, float>> participantAttributesByRole)
    {
        var friendly = CreateCombatants(
            "friendly",
            CombatTeam.Friendly,
            friendlyLoadout,
            compiledAbilitiesByEssenceId,
            participantAttributesByRole);
        var hostile = CreateCombatants(
            "hostile",
            CombatTeam.Hostile,
            hostileLoadout,
            compiledAbilitiesByEssenceId,
            participantAttributesByRole);
        var engine = new FastCombatEngine(
            compiledStatuses,
            compiledSummons,
            compiledAbilities,
            new FastCombatEngineOptions(
                MaxTicks: DefaultMaxTicks,
                RandomSeed: randomSeed,
                CaptureEventLog: false));
        var friendlySignature = CreateTeamSignature(friendlyLoadout);
        var hostileSignature = CreateTeamSignature(hostileLoadout);
        CombatResult result;
        try
        {
            result = engine.Run(friendly, hostile);
        }
        catch (InvalidOperationException exception) when (
            exception.Message.Contains("Combat event recursion", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Combat event recursion occurred in balance battle {battleIndex} " +
                $"(seed {randomSeed}). Friendly: {friendlySignature}. Hostile: {hostileSignature}.",
                exception);
        }
        var friendlyDamageDone = SumTeamStats(result, "Friendly", x => x.DamageDone);
        var friendlyDamageTaken = SumTeamStats(result, "Friendly", x => x.DamageTaken);
        var hostileDamageDone = SumTeamStats(result, "Hostile", x => x.DamageDone);
        var hostileDamageTaken = SumTeamStats(result, "Hostile", x => x.DamageTaken);

        return new BalanceBattleExecutionResult(
            battleIndex,
            friendlyLoadout,
            hostileLoadout,
            result.Outcome,
            result.Duration,
            friendlyDamageDone,
            friendlyDamageTaken,
            hostileDamageDone,
            hostileDamageTaken);
    }

    private static void AccumulateBattle(
        BalanceBattleExecutionResult execution,
        IReadOnlyDictionary<string, string> essenceNames,
        IDictionary<string, TeamAccumulator> results,
        IDictionary<string, MatchupAccumulator> matchups,
        ICollection<AbilityBalanceBattleSummary> battleSummaries)
    {
        var friendlySignature = CreateTeamSignature(execution.FriendlyLoadout);
        var hostileSignature = CreateTeamSignature(execution.HostileLoadout);
        var friendlyDisplayName = CreateTeamDisplayName(execution.FriendlyLoadout, essenceNames);
        var hostileDisplayName = CreateTeamDisplayName(execution.HostileLoadout, essenceNames);
        AddResult(
            GetAccumulator(results, friendlySignature, friendlyDisplayName, execution.FriendlyLoadout),
            execution.Outcome,
            friendlyPerspective: true,
            execution.Duration,
            execution.FriendlyDamageDone,
            execution.FriendlyDamageTaken);
        AddResult(
            GetAccumulator(results, hostileSignature, hostileDisplayName, execution.HostileLoadout),
            execution.Outcome,
            friendlyPerspective: false,
            execution.Duration,
            execution.HostileDamageDone,
            execution.HostileDamageTaken);
        AddMatchupResult(
            matchups,
            friendlySignature,
            hostileSignature,
            execution.Outcome);

        if (battleSummaries.Count < BattleSummaryLimit)
        {
            battleSummaries.Add(new AbilityBalanceBattleSummary(
                execution.BattleIndex + 1,
                friendlySignature,
                friendlyDisplayName,
                hostileSignature,
                hostileDisplayName,
                execution.Outcome.ToString(),
                execution.Duration,
                execution.FriendlyDamageDone,
                execution.FriendlyDamageTaken,
                execution.HostileDamageDone,
                execution.HostileDamageTaken));
        }
    }

    private static void AddMatchupResult(
        IDictionary<string, MatchupAccumulator> matchups,
        string friendlySignature,
        string hostileSignature,
        BattleOutcome outcome)
    {
        var friendlyIsFirst = string.CompareOrdinal(friendlySignature, hostileSignature) <= 0;
        var firstSignature = friendlyIsFirst ? friendlySignature : hostileSignature;
        var secondSignature = friendlyIsFirst ? hostileSignature : friendlySignature;
        var key = $"{firstSignature}\u001f{secondSignature}";
        if (!matchups.TryGetValue(key, out var accumulator))
        {
            accumulator = new MatchupAccumulator(firstSignature, secondSignature);
            matchups.Add(key, accumulator);
        }

        accumulator.Battles++;
        var friendlyWon = outcome == BattleOutcome.Victory;
        var hostileWon = outcome == BattleOutcome.Defeat;
        if (!friendlyWon && !hostileWon)
            accumulator.Draws++;
        else if (friendlyIsFirst == friendlyWon)
            accumulator.FirstWins++;
        else
            accumulator.SecondWins++;
    }

    private static List<RuntimeCombatant> CreateCombatants(
        string idPrefix,
        CombatTeam team,
        AbilityBalanceTeamLoadout loadout,
        IReadOnlyDictionary<string, IReadOnlyList<CompiledAbility>> compiledAbilitiesByEssenceId,
        IReadOnlyDictionary<string, IReadOnlyDictionary<AttributeType, float>> participantAttributesByRole)
    {
        return loadout.Participants
            .Select((participant, index) =>
            {
                var role = string.IsNullOrWhiteSpace(participant.Role)
                    ? "Balance"
                    : participant.Role.Trim();
                if (!participantAttributesByRole.TryGetValue(role, out var attributes))
                {
                    throw new InvalidOperationException(
                        $"No discovery attributes are configured for participant role '{role}'.");
                }

                return new RuntimeCombatant(
                    $"{idPrefix}-{index + 1}",
                    $"{idPrefix} {index + 1}",
                    team,
                    attributes.ToDictionary(pair => pair.Key, pair => pair.Value),
                    SelectAbilities(participant, compiledAbilitiesByEssenceId),
                    [$"Role.{role}"]);
            })
            .ToList();
    }

    private static IReadOnlyDictionary<string, BalanceEssence> CreateBalanceEssenceIndex(
        AbilityCatalog catalog,
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities,
        IEssenceDefinitionRepository? essenceDefinitions)
    {
        var definitions = essenceDefinitions?.GetAll()
            .Where(definition =>
                !string.IsNullOrWhiteSpace(definition.Id)
                && !definition.Id.Equals(TrainingEssenceId, StringComparison.OrdinalIgnoreCase))
            .ToList() ?? [];
        if (definitions.Count > 0)
        {
            return definitions.ToDictionary(
                definition => definition.Id,
                definition =>
                {
                    var abilityIds = new[] { definition.ActiveAbilityId, definition.PassiveAbilityId }
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var missingAbilityIds = abilityIds
                        .Where(id => !compiledAbilities.ContainsKey(id))
                        .ToList();
                    if (missingAbilityIds.Count > 0)
                    {
                        throw new InvalidOperationException(
                            $"Essence '{definition.Id}' references abilities missing from the combat catalog: " +
                            string.Join(", ", missingAbilityIds));
                    }

                    return new BalanceEssence(
                        definition.Id,
                        definition.SourceMonsterId,
                        string.IsNullOrWhiteSpace(definition.DisplayName)
                            ? FormatEssenceId(definition.Id)
                            : definition.DisplayName,
                        abilityIds.Select(id => compiledAbilities[id]).ToList());
                },
                StringComparer.OrdinalIgnoreCase);
        }

        return catalog.AbilityIdsByOwningEssence.ToDictionary(
            pair => pair.Key,
            pair => new BalanceEssence(
                pair.Key,
                pair.Key,
                FormatEssenceId(pair.Key),
                pair.Value
                    .Where(compiledAbilities.ContainsKey)
                    .Select(abilityId => compiledAbilities[abilityId])
                    .ToList()),
            StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<BalanceEssenceFamily> CreateEssenceFamilies(
        IReadOnlyDictionary<string, BalanceEssence> balanceEssences) =>
        balanceEssences.Values
            .GroupBy(essence => essence.SourceMonsterId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new BalanceEssenceFamily(
                group.Key,
                group.Select(essence => essence.Id)
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .ToList();

    private static void ValidateParticipantEssences(
        IReadOnlyList<string> essenceIds,
        int participantIndex,
        IReadOnlyDictionary<string, BalanceEssence> balanceEssences)
    {
        var unknownEssenceIds = essenceIds
            .Where(id => !balanceEssences.ContainsKey(id))
            .ToList();
        if (unknownEssenceIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Balance candidate participant {participantIndex + 1} contains unknown Essences: " +
                string.Join(", ", unknownEssenceIds));
        }

        var duplicateCreature = essenceIds
            .Select(id => balanceEssences[id])
            .GroupBy(essence => essence.SourceMonsterId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateCreature is not null)
        {
            throw new InvalidOperationException(
                $"Balance candidate participant {participantIndex + 1} cannot equip multiple Essences " +
                $"from '{duplicateCreature.Key}': " +
                string.Join(", ", duplicateCreature.Select(essence => essence.Id)));
        }
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

    private static AbilityBalanceTeamLoadout NormalizeTeam(AbilityBalanceTeamLoadout team)
    {
        var normalized = team.Participants
            .Select(participant => new AbilityBalanceParticipantLoadout(
                participant.EssenceIds
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                string.IsNullOrWhiteSpace(participant.Role) ? "Balance" : participant.Role.Trim()))
            .ToArray();
        if (normalized.All(participant => participant.Role.Equals(
                "Balance",
                StringComparison.OrdinalIgnoreCase)))
        {
            return new AbilityBalanceTeamLoadout(normalized
                .OrderBy(participant => string.Join("+", participant.EssenceIds), StringComparer.Ordinal)
                .ToArray());
        }

        var byRole = normalized
            .GroupBy(participant => participant.Role, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => new Queue<AbilityBalanceParticipantLoadout>(group.OrderBy(
                    participant => string.Join("+", participant.EssenceIds),
                    StringComparer.Ordinal)),
                StringComparer.OrdinalIgnoreCase);
        return new AbilityBalanceTeamLoadout(normalized
            .Select(participant => byRole[participant.Role].Dequeue())
            .ToArray());
    }

    private static string CreateTeamSignature(AbilityBalanceTeamLoadout team)
    {
        var participants = NormalizeTeam(team).Participants;
        var usesLegacyBalanceRole = participants.All(participant => participant.Role.Equals(
            "Balance",
            StringComparison.OrdinalIgnoreCase));
        return string.Join(" | ", participants.Select(participant =>
            usesLegacyBalanceRole
                ? string.Join("+", participant.EssenceIds)
                : $"{participant.Role}={string.Join("+", participant.EssenceIds)}"));
    }

    private static string CreateTeamDisplayName(
        AbilityBalanceTeamLoadout team,
        IReadOnlyDictionary<string, string> essenceNames)
    {
        var participants = NormalizeTeam(team).Participants;
        var usesLegacyBalanceRole = participants.All(participant => participant.Role.Equals(
            "Balance",
            StringComparison.OrdinalIgnoreCase));
        return string.Join(" / ", participants.Select(participant =>
        {
            var names = string.Join(
                " + ",
                participant.EssenceIds.Select(essenceId => GetEssenceDisplayName(essenceId, essenceNames)));
            return usesLegacyBalanceRole ? names : $"{participant.Role}: {names}";
        }));
    }

    private static IReadOnlyList<AbilityBalanceEssenceResult> CreateEssenceResults(
        IReadOnlyList<AbilityBalanceCombinationResult> combinations,
        IReadOnlyDictionary<string, string> essenceNames)
    {
        var adjustedDeltas = CalculateAdjustedEssenceDeltas(combinations);
        var accumulators = new Dictionary<string, EssenceAccumulator>(StringComparer.OrdinalIgnoreCase);
        foreach (var combination in combinations)
        {
            var essenceIds = combination.Participants
                .SelectMany(participant => participant.EssenceIds)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var essenceId in essenceIds)
            {
                if (!accumulators.TryGetValue(essenceId, out var accumulator))
                {
                    accumulator = new EssenceAccumulator();
                    accumulators.Add(essenceId, accumulator);
                }

                accumulator.TeamAppearances++;
                accumulator.Battles += combination.Battles;
                accumulator.Wins += combination.Wins;
                accumulator.Losses += combination.Losses;
                accumulator.Draws += combination.Draws;
                accumulator.TotalDuration += combination.AverageDuration * combination.Battles;
                accumulator.TotalDamageDone += combination.AverageDamageDone * combination.Battles;
                accumulator.TotalDamageTaken += combination.AverageDamageTaken * combination.Battles;
            }
        }

        return accumulators
            .Select(pair => pair.Value.ToResult(
                pair.Key,
                GetEssenceDisplayName(pair.Key, essenceNames),
                adjustedDeltas.GetValueOrDefault(pair.Key)))
            .OrderByDescending(result => result.AdjustedScoreDelta)
            .ThenByDescending(result => result.Battles)
            .ThenBy(result => result.EssenceId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyDictionary<string, double> CalculateAdjustedEssenceDeltas(
        IReadOnlyList<AbilityBalanceCombinationResult> combinations)
    {
        var rows = combinations
            .Where(combination => combination.Battles > 0)
            .Select(combination => new
            {
                EssenceCounts = combination.Participants
                    .SelectMany(participant => participant.EssenceIds)
                    .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase),
                Target = (combination.Wins + combination.Draws * 0.5d) / combination.Battles - 0.5d,
                Weight = (double)combination.Battles
            })
            .ToList();
        var essenceIds = rows
            .SelectMany(row => row.EssenceCounts.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var coefficients = essenceIds.ToDictionary(id => id, _ => 0d, StringComparer.OrdinalIgnoreCase);
        if (rows.Count == 0 || essenceIds.Count == 0)
            return coefficients;

        var totalWeight = rows.Sum(row => row.Weight);
        var ridgePenalty = Math.Max(1d, totalWeight * 0.0005d);
        for (var iteration = 0; iteration < 40; iteration++)
        {
            foreach (var essenceId in essenceIds)
            {
                var numerator = 0d;
                var denominator = ridgePenalty;
                foreach (var row in rows.Where(row =>
                             row.EssenceCounts.ContainsKey(essenceId)))
                {
                    var copyCount = row.EssenceCounts[essenceId];
                    var otherContribution = row.EssenceCounts
                        .Where(pair => !pair.Key.Equals(essenceId, StringComparison.OrdinalIgnoreCase))
                        .Sum(pair => pair.Value * coefficients[pair.Key]);
                    numerator += row.Weight * copyCount * (row.Target - otherContribution);
                    denominator += row.Weight * copyCount * copyCount;
                }

                coefficients[essenceId] = Math.Clamp(numerator / denominator, -0.25d, 0.25d);
            }

            var mean = coefficients.Values.Average();
            foreach (var essenceId in essenceIds)
                coefficients[essenceId] = Math.Clamp(coefficients[essenceId] - mean, -0.25d, 0.25d);
        }

        return coefficients;
    }

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

    private sealed class MatchupAccumulator(string firstSignature, string secondSignature)
    {
        public int Battles { get; set; }
        public int FirstWins { get; set; }
        public int SecondWins { get; set; }
        public int Draws { get; set; }

        public AbilityBalanceMatchupResult ToResult() =>
            new(
                firstSignature,
                secondSignature,
                Battles,
                FirstWins,
                SecondWins,
                Draws,
                Battles == 0 ? 0d : (FirstWins + Draws * 0.5d) / Battles);
    }

    private sealed class EssenceAccumulator
    {
        private const int MinimumClassificationBattles = 1_000;
        private const double PracticalDifference = 0.02d;

        public int TeamAppearances { get; set; }
        public int Battles { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public double TotalDuration { get; set; }
        public double TotalDamageDone { get; set; }
        public double TotalDamageTaken { get; set; }

        public AbilityBalanceEssenceResult ToResult(
            string essenceId,
            string displayName,
            double adjustedScoreDelta)
        {
            var score = Battles == 0 ? 0d : (Wins + Draws * 0.5d) / Battles;
            var (lower, upper) = WilsonInterval(score, Battles);
            var delta = score - 0.5d;
            var classification = Battles < MinimumClassificationBattles
                ? "InsufficientData"
                : adjustedScoreDelta >= PracticalDifference && lower > 0.5d
                    ? "Overperforming"
                    : adjustedScoreDelta <= -PracticalDifference && upper < 0.5d
                        ? "Underperforming"
                        : "Healthy";

            return new AbilityBalanceEssenceResult(
                essenceId,
                displayName,
                TeamAppearances,
                Battles,
                Wins,
                Losses,
                Draws,
                score,
                delta,
                adjustedScoreDelta,
                lower,
                upper,
                Battles == 0 ? 0d : TotalDuration / Battles,
                Battles == 0 ? 0d : TotalDamageDone / Battles,
                Battles == 0 ? 0d : TotalDamageTaken / Battles,
                classification);
        }

        private static (double Lower, double Upper) WilsonInterval(double score, int battles)
        {
            if (battles <= 0)
                return (0d, 1d);

            const double z = 1.959963984540054d;
            var denominator = 1d + z * z / battles;
            var center = (score + z * z / (2d * battles)) / denominator;
            var margin = z * Math.Sqrt(
                (score * (1d - score) + z * z / (4d * battles)) / battles) / denominator;
            return (Math.Max(0d, center - margin), Math.Min(1d, center + margin));
        }
    }

    private readonly record struct BalanceBattleWorkItem(
        AbilityBalanceTeamLoadout Friendly,
        AbilityBalanceTeamLoadout Hostile,
        int BattleIndex,
        int RandomSeed);

    private readonly record struct BalanceBattleExecutionResult(
        int BattleIndex,
        AbilityBalanceTeamLoadout FriendlyLoadout,
        AbilityBalanceTeamLoadout HostileLoadout,
        BattleOutcome Outcome,
        int Duration,
        int FriendlyDamageDone,
        int FriendlyDamageTaken,
        int HostileDamageDone,
        int HostileDamageTaken);

    private sealed record BalanceEssence(
        string Id,
        string SourceMonsterId,
        string DisplayName,
        IReadOnlyList<CompiledAbility> Abilities);

    private sealed record BalanceEssenceFamily(
        string SourceMonsterId,
        IReadOnlyList<string> EssenceIds);
}
