using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Mastery;
using Domain.Models.Dungeons.Runs;

namespace Services.LL.Dungeons;

public sealed class DungeonMasteryService : IDungeonMasteryService
{
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

    public async Task<DungeonMasteryAwardResult> AwardCompletionAsync(
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
        var reasons = CalculateCompletionExperienceReasons(run);
        var experienceAwarded = reasons.Sum(x => x.Experience);

        mastery.Experience += experienceAwarded;
        mastery.Level = CalculateLevel(mastery.Experience);
        mastery.MaxLevelRewardClaimed |= mastery.Level >= DungeonMasteryBenefits.MaxLevel;
        mastery.CompletionCount++;
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
            MaxLevelRewardPreviouslyClaimed = rewardPreviouslyClaimed
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

    private static IReadOnlyList<DungeonMasteryAwardReason> CalculateCompletionExperienceReasons(DungeonRun run)
    {
        var reasons = new List<DungeonMasteryAwardReason>();
        var completedRooms = run.Rooms.Count(x => x.Status == RoomInstanceStatus.Completed);
        var roomExperience = Math.Max(1, completedRooms) * 5;
        reasons.Add(new DungeonMasteryAwardReason
        {
            Id = "completion",
            Description = "Dungeon completed",
            Experience = 100 + roomExperience
        });

        var bossExperience = run.Rooms.Any(x => x.Type == RoomType.Boss && x.Status == RoomInstanceStatus.Completed)
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

        var miniBossExperience = run.Rooms.Count(x => x.Type == RoomType.MiniBoss && x.Status == RoomInstanceStatus.Completed) *
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
}
