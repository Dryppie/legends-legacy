using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Entities.Characters;
using Microsoft.Extensions.Logging;

namespace Services.LL.PowerRatings;

public sealed class PowerRatingService : IPowerRatingService
{
    private const string AttributeRatingStatus =
        "Combat Rating includes base and equipment attributes; Essence abilities are not yet included.";

    private readonly PowerBuildSnapshotFactory _snapshots;
    private readonly ILogger<PowerRatingService> _logger;

    public PowerRatingService(
        PowerBuildSnapshotFactory snapshots,
        ILogger<PowerRatingService> logger)
    {
        _snapshots = snapshots;
        _logger = logger;
    }

    public async Task<OverallPowerRating> GetCharacterOverallRatingAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var build = await _snapshots.CreateAsync(
            characterId,
            DungeonPartySelection.Solo,
            cancellationToken);
        return CreateOverallRating(build);
    }

    public async Task<OverallPowerRating> GetCharacterOverallRatingAsync(
        Character character,
        CancellationToken cancellationToken)
    {
        var build = await _snapshots.CreateAsync(
            character,
            DungeonPartySelection.Solo,
            cancellationToken);
        return CreateOverallRating(build);
    }

    private OverallPowerRating CreateOverallRating(PowerBuildSnapshot? build)
    {
        if (build is null)
        {
            return new OverallPowerRating(
                0,
                PowerAnalysisState.InsufficientCombatData,
                "The character attribute snapshot could not be built.");
        }

        try
        {
            return new OverallPowerRating(
                build.Rating.Overall,
                PowerAnalysisState.Available,
                AttributeRatingStatus);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Combat Rating calculation failed for fingerprint {BuildFingerprint}.",
                build.Fingerprint);
            return new OverallPowerRating(
                0,
                PowerAnalysisState.CalculationFailed,
                "Combat Rating could not be calculated.");
        }
    }

    public Task<PowerRatingSnapshot> GetCharacterRatingAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        GetPartyRatingAsync(characterId, DungeonPartySelection.Solo, cancellationToken);

    public async Task<PowerRatingSnapshot> GetPartyRatingAsync(
        Guid characterId,
        DungeonPartySelection partySelection,
        CancellationToken cancellationToken)
    {
        if (partySelection.CompanionIds.Count > 0)
        {
            return Unavailable(
                PowerAnalysisState.Unsupported,
                "NPC dungeon companions are not represented by the current game model yet.");
        }

        var build = await _snapshots.CreateAsync(characterId, partySelection, cancellationToken);
        if (build is null)
        {
            return Unavailable(
                PowerAnalysisState.InsufficientCombatData,
                "The character attribute snapshot could not be built.");
        }

        try
        {
            return CreateSnapshot(
                build.Rating,
                build.Fingerprint);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Combat Rating calculation failed for fingerprint {BuildFingerprint}.",
                build.Fingerprint);
            return Unavailable(
                PowerAnalysisState.CalculationFailed,
                "Combat Rating could not be calculated.",
                build.Fingerprint);
        }
    }

    private static PowerRatingSnapshot CreateSnapshot(
        CombatRatingBreakdown rating,
        string fingerprint) =>
        new(
            PowerRatingAlgorithm.Version,
            fingerprint,
            rating.Overall,
            rating.SingleTargetOffense,
            rating.MultiTargetOffense,
            rating.PhysicalDurability,
            rating.MagicalDurability,
            rating.Sustain,
            rating.ControlUtility,
            DateTimeOffset.UtcNow,
            PowerRatingConfidence.High,
            PowerAnalysisState.Available,
            AttributeRatingStatus);

    private static PowerRatingSnapshot Unavailable(
        PowerAnalysisState state,
        string message,
        string fingerprint = "") =>
        new(
            PowerRatingAlgorithm.Version,
            fingerprint,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            DateTimeOffset.UtcNow,
            PowerRatingConfidence.Low,
            state,
            message);
}
