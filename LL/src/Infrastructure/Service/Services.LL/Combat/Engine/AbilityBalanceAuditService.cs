using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.PowerRatings;

namespace Services.LL.Combat.Engine;

public sealed class AbilityBalanceAuditService : IAbilityBalanceAuditService
{
    private static readonly int[] DefaultSeeds = [1337, 2027, 9001];
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly IAbilityBalanceSimulator _simulator;
    private readonly IAbilityCatalogProvider _catalogProvider;
    private readonly IEssenceDefinitionRepository? _essenceDefinitions;

    public AbilityBalanceAuditService(
        IAbilityBalanceSimulator simulator,
        IAbilityCatalogProvider catalogProvider,
        IEssenceDefinitionRepository? essenceDefinitions = null)
    {
        _simulator = simulator;
        _catalogProvider = catalogProvider;
        _essenceDefinitions = essenceDefinitions;
    }

    public AbilityBalanceAuditReport Run(
        AbilityBalanceAuditRequest request,
        CancellationToken cancellationToken)
    {
        request = Normalize(request);
        var screeningReports = new List<AbilityBalanceSimulationReport>();
        var seeds = request.RandomSeeds!;
        for (var seedIndex = 0; seedIndex < seeds.Count; seedIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var report = _simulator.Run(
                new AbilityBalanceSimulationRequest(
                    request.ScreeningBattleCount,
                    request.TeamSize,
                    request.EssencesPerParticipant,
                    seeds[seedIndex],
                    request.CandidatePoolSize,
                    request.CandidatePoolSize,
                    null,
                    request.EquipmentTier,
                    request.EquipmentRarity,
                    request.EquipmentProfile),
                cancellationToken);
            screeningReports.Add(report);
        }

        var screenedCombinations = screeningReports
            .SelectMany(report => report.RankedCombinations)
            .GroupBy(combination => combination.Signature, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(combination => combination.WinRate)
                .ThenByDescending(combination => combination.Battles)
                .First())
            .OrderByDescending(combination => combination.WinRate)
            .ThenByDescending(combination => combination.Battles)
            .ThenBy(combination => combination.AverageDuration)
            .ToList();
        var finalistCandidates = SelectDiverseFinalists(screenedCombinations, request.FinalistCount);
        if (finalistCandidates.Count < 2)
            throw new InvalidOperationException("The screening stage did not produce at least two finalist teams.");

        var screeningEssenceResults = MergeEssenceResults(
            screeningReports.SelectMany(report => report.EssenceResults));
        var validationResults = new List<AbilityBalanceValidationResult>();
        long validationBattles = 0;
        var flaggedEssences = screeningEssenceResults
            .Where(result => result.Classification is "Overperforming" or "Underperforming")
            .OrderByDescending(result => Math.Abs(result.AdjustedScoreDelta))
            .Take(20)
            .ToList();
        for (var validationIndex = 0; validationIndex < flaggedEssences.Count; validationIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var flagged = flaggedEssences[validationIndex];
            var representative = screenedCombinations
                .Where(combination => GetEssenceIds(combination).Count(id =>
                    id.Equals(flagged.EssenceId, StringComparison.OrdinalIgnoreCase)) == 1)
                .OrderBy(combination => Math.Abs(CombinationScore(combination) - 0.5d))
                .FirstOrDefault();
            if (representative is null)
                continue;

            var originalLoadout = ToLoadout(representative);
            var teamEssenceIds = GetEssenceIds(representative)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var replacement = screeningEssenceResults
                .Where(result => result.Classification == "Healthy"
                    && !teamEssenceIds.Contains(result.EssenceId)
                    && CanReplaceEssence(originalLoadout, flagged.EssenceId, result.EssenceId))
                .OrderBy(result => Math.Abs(result.AdjustedScoreDelta))
                .FirstOrDefault();
            if (replacement is null)
                continue;

            var replacementLoadout = ReplaceEssence(
                originalLoadout,
                flagged.EssenceId,
                replacement.EssenceId);
            var validationReport = _simulator.Run(
                new AbilityBalanceSimulationRequest(
                    request.ValidationBattleCount,
                    request.TeamSize,
                    request.EssencesPerParticipant,
                    seeds[0] + 10_000 + validationIndex,
                    2,
                    request.CandidatePoolSize,
                    [originalLoadout, replacementLoadout],
                    request.EquipmentTier,
                    request.EquipmentRarity,
                    request.EquipmentProfile),
                cancellationToken);
            validationBattles += validationReport.BattlesRun;
            var originalResult = validationReport.RankedCombinations.Single(combination =>
                GetEssenceIds(combination).Contains(flagged.EssenceId, StringComparer.OrdinalIgnoreCase));
            var replacementResult = validationReport.RankedCombinations.Single(combination =>
                !GetEssenceIds(combination).Contains(flagged.EssenceId, StringComparer.OrdinalIgnoreCase));
            var originalScore = CombinationScore(originalResult);
            var replacementScore = CombinationScore(replacementResult);
            validationResults.Add(new AbilityBalanceValidationResult(
                flagged.EssenceId,
                flagged.DisplayName,
                replacement.EssenceId,
                replacement.DisplayName,
                validationReport.BattlesRun,
                originalScore,
                replacementScore,
                originalScore - replacementScore));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var finalistReport = _simulator.Run(
            new AbilityBalanceSimulationRequest(
                request.FinalistBattleCount,
                request.TeamSize,
                request.EssencesPerParticipant,
                seeds[0],
                request.FinalistCount,
                request.CandidatePoolSize,
                finalistCandidates.Select(ToLoadout).ToList(),
                request.EquipmentTier,
                request.EquipmentRarity,
                request.EquipmentProfile),
            cancellationToken);

        var screeningBattles = screeningReports.Sum(report => (long)report.BattlesRun);
        var finalistBattles = (long)finalistReport.BattlesRun;
        return new AbilityBalanceAuditReport(
            CreateContentHash(),
            screeningBattles,
            validationBattles,
            finalistBattles,
            screeningBattles + validationBattles + finalistBattles,
            screenedCombinations.Count,
            finalistReport.CandidateTeamCount,
            finalistReport.EquipmentTier,
            finalistReport.EquipmentRarity,
            finalistReport.EquipmentProfile,
            finalistReport.ParticipantAttributes,
            screeningEssenceResults,
            finalistReport.EssenceResults,
            validationResults,
            finalistReport.RankedCombinations);
    }

    private static AbilityBalanceAuditRequest Normalize(AbilityBalanceAuditRequest request)
    {
        var seeds = (request.RandomSeeds ?? DefaultSeeds)
            .Where(seed => seed != 0)
            .Distinct()
            .Take(10)
            .ToList();
        if (seeds.Count == 0)
            seeds.AddRange(DefaultSeeds);
        var rarity = Enum.TryParse<Rarity>(request.EquipmentRarity, true, out var parsedRarity)
            && parsedRarity <= Rarity.Legendary
            ? parsedRarity
            : Rarity.Epic;
        var profile = Enum.TryParse<CanonicalPartyProfile>(request.EquipmentProfile, true, out var parsedProfile)
            ? parsedProfile
            : CanonicalPartyProfile.Balanced;

        return request with
        {
            TeamSize = Math.Clamp(request.TeamSize, 1, 10),
            EssencesPerParticipant = Math.Clamp(request.EssencesPerParticipant, 1, 10),
            CandidatePoolSize = Math.Clamp(request.CandidatePoolSize, 2, 1_000),
            ScreeningBattleCount = Math.Max(1, request.ScreeningBattleCount),
            FinalistCount = Math.Clamp(request.FinalistCount, 2, 1_000),
            FinalistBattleCount = Math.Max(1, request.FinalistBattleCount),
            ValidationBattleCount = Math.Max(1, request.ValidationBattleCount),
            RandomSeeds = seeds,
            EquipmentTier = Math.Clamp(
                request.EquipmentTier,
                EquipmentStatBudgetCatalog.MinimumTier,
                EquipmentStatBudgetCatalog.MaximumTier),
            EquipmentRarity = rarity.ToString(),
            EquipmentProfile = profile.ToString()
        };
    }

    private static IReadOnlyList<AbilityBalanceCombinationResult> SelectDiverseFinalists(
        IReadOnlyList<AbilityBalanceCombinationResult> ranked,
        int count)
    {
        var selected = new Dictionary<string, AbilityBalanceCombinationResult>(StringComparer.Ordinal);
        var essenceIds = ranked
            .SelectMany(GetEssenceIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase);
        foreach (var essenceId in essenceIds)
        {
            var representative = ranked.FirstOrDefault(combination =>
                GetEssenceIds(combination).Contains(essenceId, StringComparer.OrdinalIgnoreCase));
            if (representative is not null)
                selected.TryAdd(representative.Signature, representative);
            if (selected.Count >= count)
                break;
        }

        foreach (var combination in ranked)
        {
            selected.TryAdd(combination.Signature, combination);
            if (selected.Count >= count)
                break;
        }

        return selected.Values.ToList();
    }

    private static IEnumerable<string> GetEssenceIds(AbilityBalanceCombinationResult combination) =>
        combination.Participants.SelectMany(participant => participant.EssenceIds);

    private static AbilityBalanceTeamLoadout ToLoadout(AbilityBalanceCombinationResult result) =>
        new(result.Participants.Select(participant =>
            new AbilityBalanceParticipantLoadout([.. participant.EssenceIds])).ToList());

    private static AbilityBalanceTeamLoadout ReplaceEssence(
        AbilityBalanceTeamLoadout loadout,
        string essenceId,
        string replacementEssenceId)
    {
        var replaced = false;
        return new AbilityBalanceTeamLoadout(loadout.Participants
            .Select(participant => new AbilityBalanceParticipantLoadout(participant.EssenceIds
                .Select(candidate =>
                {
                    if (!replaced && candidate.Equals(essenceId, StringComparison.OrdinalIgnoreCase))
                    {
                        replaced = true;
                        return replacementEssenceId;
                    }

                    return candidate;
                })
                .ToList()))
            .ToList());
    }

    private bool CanReplaceEssence(
        AbilityBalanceTeamLoadout loadout,
        string essenceId,
        string replacementEssenceId)
    {
        var participant = loadout.Participants.FirstOrDefault(candidate =>
            candidate.EssenceIds.Contains(essenceId, StringComparer.OrdinalIgnoreCase));
        if (participant is null)
            return false;

        var replacementSource = GetSourceMonsterId(replacementEssenceId);
        return participant.EssenceIds
            .Where(candidate => !candidate.Equals(essenceId, StringComparison.OrdinalIgnoreCase))
            .Select(GetSourceMonsterId)
            .All(source => !source.Equals(replacementSource, StringComparison.OrdinalIgnoreCase));
    }

    private string GetSourceMonsterId(string essenceId) =>
        _essenceDefinitions?.GetById(essenceId)?.SourceMonsterId ?? essenceId;

    private static double CombinationScore(AbilityBalanceCombinationResult combination) =>
        combination.Battles == 0
            ? 0d
            : (combination.Wins + combination.Draws * 0.5d) / combination.Battles;

    private static IReadOnlyList<AbilityBalanceEssenceResult> MergeEssenceResults(
        IEnumerable<AbilityBalanceEssenceResult> results) =>
        results
            .GroupBy(result => result.EssenceId, StringComparer.OrdinalIgnoreCase)
            .Select(group => MergeEssenceResult(group.Key, group))
            .OrderByDescending(result => result.AdjustedScoreDelta)
            .ThenByDescending(result => result.Battles)
            .ToList();

    private static AbilityBalanceEssenceResult MergeEssenceResult(
        string essenceId,
        IEnumerable<AbilityBalanceEssenceResult> values)
    {
        var entries = values.ToList();
        var battles = entries.Sum(entry => entry.Battles);
        var wins = entries.Sum(entry => entry.Wins);
        var losses = entries.Sum(entry => entry.Losses);
        var draws = entries.Sum(entry => entry.Draws);
        var score = battles == 0 ? 0d : (wins + draws * 0.5d) / battles;
        var adjustedDelta = WeightedAverage(entries, entry => entry.AdjustedScoreDelta);
        var (lower, upper) = WilsonInterval(score, battles);
        var delta = score - 0.5d;
        var classification = battles < 5_000
            ? "InsufficientData"
            : adjustedDelta >= 0.02d && lower > 0.5d
                ? "Overperforming"
                : adjustedDelta <= -0.02d && upper < 0.5d
                    ? "Underperforming"
                    : "Healthy";

        return new AbilityBalanceEssenceResult(
            essenceId,
            entries[0].DisplayName,
            entries.Sum(entry => entry.TeamAppearances),
            battles,
            wins,
            losses,
            draws,
            score,
            delta,
            adjustedDelta,
            lower,
            upper,
            WeightedAverage(entries, entry => entry.AverageDuration),
            WeightedAverage(entries, entry => entry.AverageDamageDone),
            WeightedAverage(entries, entry => entry.AverageDamageTaken),
            classification);
    }

    private static double WeightedAverage(
        IReadOnlyList<AbilityBalanceEssenceResult> entries,
        Func<AbilityBalanceEssenceResult, double> selector)
    {
        var battles = entries.Sum(entry => entry.Battles);
        return battles == 0 ? 0d : entries.Sum(entry => selector(entry) * entry.Battles) / battles;
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

    private string CreateContentHash()
    {
        var catalog = _catalogProvider.GetCatalog();
        var content = JsonSerializer.Serialize(new
        {
            catalog.Abilities,
            catalog.Statuses,
            catalog.Summons,
            catalog.AbilityIdsByOwningEssence,
            Essences = _essenceDefinitions?.GetAll().Select(definition => new
            {
                definition.Id,
                definition.SourceMonsterId,
                definition.ActiveAbilityId,
                definition.PassiveAbilityId
            }),
            EquipmentStatBudgetCatalog.BalanceVersion
        }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
