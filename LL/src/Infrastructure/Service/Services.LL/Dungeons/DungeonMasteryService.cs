using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Mastery;
using Domain.Models.Dungeons.Runs;

namespace Services.LL.Dungeons;

public sealed class DungeonMasteryService : IDungeonMasteryService
{
    private const int AttemptExperience = 5;
    private const int ClearedRoomExperience = 5;
    private const int BossDefeatExperience = 50;
    private const int MiniBossDefeatExperience = 25;

    private readonly ICharacterDungeonMasteryRepository _masteries;

    public DungeonMasteryService(ICharacterDungeonMasteryRepository masteries)
    {
        _masteries = masteries;
    }

    public int CalculateLevel(long experience) => DungeonMasteryProgression.CalculateLevel(experience);

    public int? GetExperienceRequiredForNextLevel(int level) =>
        DungeonMasteryProgression.GetExperienceRequiredForNextLevel(level);

    public async Task<DungeonMasteryAwardResult> AwardRunMasteryAsync(
        DungeonRun run,
        CancellationToken cancellationToken)
    {
        var familyId = DungeonDefinitionIdentity.GetFamilyId(run.DungeonDefinitionId);
        var mastery = await _masteries.GetAsync(
            run.CharacterId,
            familyId,
            cancellationToken);

        if (mastery is null)
        {
            var now = DateTimeOffset.UtcNow;
            mastery = new CharacterDungeonMastery
            {
                CharacterId = run.CharacterId,
                DungeonDefinitionId = familyId,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _masteries.AddAsync(mastery, cancellationToken);
        }

        // The run receipt remains valid after another difficulty updates the shared row.
        if (mastery.LastAwardedRunId == run.Id || run.State?.MasteryAwardReasons.Count > 0)
        {
            return new DungeonMasteryAwardResult(
                mastery.DungeonDefinitionId,
                0,
                mastery.Experience,
                mastery.Level,
                mastery.Level,
                mastery.CompletionCount,
                [],
                AlreadyAwarded: true);
        }

        var previousLevel = mastery.Level;
        var rewardPreviouslyClaimed = mastery.MaxLevelRewardClaimed;
        var reasons = CalculateRunExperienceReasons(run).ToList();
        var experienceAwarded = reasons.Sum(x => x.Experience);
        mastery.Experience += experienceAwarded;
        mastery.Level = CalculateLevel(mastery.Experience);
        var isCompletion = run.Status == DungeonRunStatus.Completed;
        if (isCompletion)
        {
            mastery.MaxLevelRewardClaimed |= mastery.Level >= DungeonMasteryBenefits.MaxLevel;
            mastery.CompletionCount++;
        }
        mastery.LastAwardedRunId = run.Id;
        mastery.UpdatedAt = DateTimeOffset.UtcNow;
        run.State ??= new DungeonRunState { RunId = run.Id };
        run.State.MasteryAwardReasons = reasons.ToList();

        return new DungeonMasteryAwardResult(
            mastery.DungeonDefinitionId,
            experienceAwarded,
            mastery.Experience,
            previousLevel,
            mastery.Level,
            mastery.CompletionCount,
            reasons,
            AlreadyAwarded: false)
        {
            // The one-time cap reward remains a completion reward. A failed or
            // retreated run can reach Mastery 10 without consuming that reward.
            MaxLevelRewardPreviouslyClaimed = rewardPreviouslyClaimed || !isCompletion,
            MaxLevelRewardWasDeferred = isCompletion &&
                !rewardPreviouslyClaimed &&
                previousLevel >= DungeonMasteryBenefits.MaxLevel
        };
    }

    public async Task<IReadOnlyDictionary<string, DungeonMasterySnapshot>> GetMasteryByDungeonAsync(
        Guid characterId,
        IReadOnlyCollection<string> dungeonDefinitionIds,
        CancellationToken cancellationToken)
    {
        if (dungeonDefinitionIds.Count == 0)
        {
            return new Dictionary<string, DungeonMasterySnapshot>(StringComparer.OrdinalIgnoreCase);
        }

        var masteries = await _masteries.GetForCharacterAsync(
            characterId,
            dungeonDefinitionIds.Select(DungeonDefinitionIdentity.GetFamilyId)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            cancellationToken);

        var byFamily = masteries.ToDictionary(x => x.DungeonDefinitionId, StringComparer.OrdinalIgnoreCase);
        return dungeonDefinitionIds.Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(
            id => id,
            id => byFamily.TryGetValue(DungeonDefinitionIdentity.GetFamilyId(id), out var mastery)
                ? MapSnapshot(mastery) with { DungeonDefinitionId = id }
                : new DungeonMasterySnapshot(id, 0, 0, GetExperienceRequiredForNextLevel(0), 0),
            StringComparer.OrdinalIgnoreCase);
    }

    private DungeonMasterySnapshot MapSnapshot(CharacterDungeonMastery mastery) =>
        new(
            mastery.DungeonDefinitionId,
            mastery.Experience,
            mastery.Level,
            GetExperienceRequiredForNextLevel(mastery.Level),
            mastery.CompletionCount);

    private static IReadOnlyList<DungeonMasteryAwardReason> CalculateRunExperienceReasons(DungeonRun run)
    {
        var reasons = new List<DungeonMasteryAwardReason>();
        if (run.Status == DungeonRunStatus.Completed)
        {
            var completedRooms = run.Rooms.Count(x => x.Status == RoomInstanceStatus.Completed);
            reasons.Add(new DungeonMasteryAwardReason
            {
                Id = "completion",
                Description = "Dungeon completed",
                Experience = 100 + (Math.Max(1, completedRooms) * ClearedRoomExperience)
            });
        }
        else
        {
            reasons.Add(new DungeonMasteryAwardReason
            {
                Id = "attempt",
                Description = "Dungeon attempt",
                Experience = AttemptExperience
            });
        }

        var creditedRooms = GetCreditedRooms(run).ToList();
        if (run.Status != DungeonRunStatus.Completed)
        {
            var clearedRoomExperience = creditedRooms.Count * ClearedRoomExperience;
            if (clearedRoomExperience > 0)
            {
                reasons.Add(new DungeonMasteryAwardReason
                {
                    Id = "rooms_cleared",
                    Description = "Rooms cleared",
                    Experience = clearedRoomExperience
                });
            }
        }

        var bossExperience = creditedRooms.Any(x => x.Type == RoomType.Boss)
            ? BossDefeatExperience
            : 0;
        if (bossExperience > 0)
        {
            reasons.Add(new DungeonMasteryAwardReason
            {
                Id = "boss_defeated",
                Description = "Boss defeated",
                Experience = bossExperience
            });
        }

        var miniBossExperience = creditedRooms.Count(x => x.Type == RoomType.MiniBoss) *
            MiniBossDefeatExperience;
        if (miniBossExperience > 0)
        {
            reasons.Add(new DungeonMasteryAwardReason
            {
                Id = "miniboss_defeated",
                Description = "Miniboss defeated",
                Experience = miniBossExperience
            });
        }

        return reasons;
    }

    private static IEnumerable<RoomInstance> GetCreditedRooms(DungeonRun run)
    {
        var completedRooms = run.Rooms.Where(room =>
            room.Status == RoomInstanceStatus.Completed &&
            room.Type != RoomType.Entrance);

        // FailRun marks the current room completed for terminal-state handling,
        // even when the encounter was lost or never attempted due to expiry.
        if (run.Status == DungeonRunStatus.Failed &&
            run.State?.FailureAnalysis?.PrimaryCause is "Combat Readiness" or "Abandonment")
        {
            completedRooms = completedRooms.Where(room => room.RoomIndex != run.CurrentRoomIndex);
        }

        return completedRooms;
    }
}
