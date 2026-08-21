using Application.Interfaces.Services.LL.Essences;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.PowerRatings;

namespace Services.LL.Combat.Engine;

public sealed class AbilityBalanceSimulator : IAbilityBalanceSimulator
{
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
        var savedCandidates = NormalizeCandidateTeams(
                normalized.CandidateTeams,
                normalized.TeamSize,
                normalized.EssencesPerParticipant,
                balanceEssences)
            .ToList();
        var mode = savedCandidates.Count > 0 ? "SavedRoundRobin" : "RandomPool";
        var results = new Dictionary<string, TeamAccumulator>(StringComparer.Ordinal);
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
                        participantAttributes);
                });

            foreach (var execution in executions)
            {
                AccumulateBattle(execution, essenceNames, results, battleSummaries);
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
                normalized.CandidatePoolSize);
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
            battleSummaries);
    }

    private static AbilityBalanceSimulationRequest NormalizeRequest(
        AbilityBalanceSimulationRequest request,
        int availableEssenceCount,
        int availableCreatureCount)
    {
        var teamSize = Math.Clamp(request.TeamSize <= 0 ? 1 : request.TeamSize, 1, 10);
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
        var equipmentModifiers = build.Equipment
            .SelectMany(equipment => equipment.AttributeModifiers)
            .Cast<AttributeModifierBase>();

        var participantAttributes = AttributeCalculator.CalculateProjectedAttributes(
            baseAttributes,
            equipmentModifiers);

        return participantAttributes;
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
        IReadOnlyDictionary<string, BalanceEssence> balanceEssences)
    {
        if (candidates is null)
            yield break;

        foreach (var candidate in candidates)
        {
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
                    return new AbilityBalanceParticipantLoadout(essenceIds);
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
        int essencesPerParticipant)
    {
        var participants = new List<AbilityBalanceParticipantLoadout>(teamSize);
        for (var index = 0; index < teamSize; index++)
        {
            var essenceIds = essenceFamilies
                .OrderBy(_ => random.Next())
                .Take(essencesPerParticipant)
                .Select(family => family.EssenceIds[random.Next(family.EssenceIds.Count)])
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
            participants.Add(new AbilityBalanceParticipantLoadout(essenceIds));
        }

        return NormalizeTeam(new AbilityBalanceTeamLoadout(participants));
    }

    private static IReadOnlyList<AbilityBalanceTeamLoadout> CreateRandomCandidatePool(
        Random random,
        IReadOnlyList<BalanceEssenceFamily> essenceFamilies,
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
            var candidate = CreateRandomTeam(random, essenceFamilies, teamSize, essencesPerParticipant);
            if (signatures.Add(CreateTeamSignature(candidate)))
                candidates.Add(candidate);
        }

        if (candidates.Count == 0)
            candidates.Add(CreateRandomTeam(random, essenceFamilies, teamSize, essencesPerParticipant));

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
        IReadOnlyDictionary<AttributeType, float> participantAttributes)
    {
        var friendly = CreateCombatants(
            "friendly",
            CombatTeam.Friendly,
            friendlyLoadout,
            compiledAbilitiesByEssenceId,
            participantAttributes);
        var hostile = CreateCombatants(
            "hostile",
            CombatTeam.Hostile,
            hostileLoadout,
            compiledAbilitiesByEssenceId,
            participantAttributes);
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

    private static List<RuntimeCombatant> CreateCombatants(
        string idPrefix,
        CombatTeam team,
        AbilityBalanceTeamLoadout loadout,
        IReadOnlyDictionary<string, IReadOnlyList<CompiledAbility>> compiledAbilitiesByEssenceId,
        IReadOnlyDictionary<AttributeType, float> participantAttributes)
    {
        return loadout.Participants
            .Select((participant, index) => new RuntimeCombatant(
                $"{idPrefix}-{index + 1}",
                $"{idPrefix} {index + 1}",
                team,
                participantAttributes.ToDictionary(pair => pair.Key, pair => pair.Value),
                SelectAbilities(participant, compiledAbilitiesByEssenceId),
                ["Role.Balance"]))
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

    private static string CreateTeamDisplayName(
        AbilityBalanceTeamLoadout team,
        IReadOnlyDictionary<string, string> essenceNames) =>
        string.Join(" / ", NormalizeTeam(team).Participants.Select(participant =>
            string.Join(" + ", participant.EssenceIds.Select(essenceId => GetEssenceDisplayName(essenceId, essenceNames)))));

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
                EssenceIds = combination.Participants
                    .SelectMany(participant => participant.EssenceIds)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                Target = (combination.Wins + combination.Draws * 0.5d) / combination.Battles - 0.5d,
                Weight = (double)combination.Battles
            })
            .ToList();
        var essenceIds = rows
            .SelectMany(row => row.EssenceIds)
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
                             row.EssenceIds.Contains(essenceId, StringComparer.OrdinalIgnoreCase)))
                {
                    var otherContribution = row.EssenceIds
                        .Where(id => !id.Equals(essenceId, StringComparison.OrdinalIgnoreCase))
                        .Sum(id => coefficients[id]);
                    numerator += row.Weight * (row.Target - otherContribution);
                    denominator += row.Weight;
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
