using Domain.Models.Dungeons;
using Domain.Models.Essences;

namespace Domain.Models.Dungeons.Definitions;

public static class DungeonRewardCatalog
{
    public static IReadOnlyList<string> GetMonsterCoreRewardItemIds(DungeonGrade grade) =>
        GetMonsterCoreRewardGrants(grade)
            .Select(grant => grant.ItemId)
            .ToArray();

    public static IReadOnlyList<DungeonRewardGrant> GetMonsterCoreRewardGrants(DungeonGrade grade) =>
        grade switch
        {
            DungeonGrade.GradeII =>
            [
                new DungeonRewardGrant
                {
                    ItemId = EssenceProgressionConstants.GreaterMonsterCoreItemId,
                    MinAmount = 2,
                    MaxAmount = 5
                },
                new DungeonRewardGrant
                {
                    ItemId = EssenceProgressionConstants.LesserMonsterCoreItemId,
                    MinAmount = 2,
                    MaxAmount = 4
                }
            ],
            DungeonGrade.GradeIII =>
            [
                new DungeonRewardGrant
                {
                    ItemId = EssenceProgressionConstants.PrimalMonsterCoreItemId,
                    MinAmount = 1,
                    MaxAmount = 4
                },
                new DungeonRewardGrant
                {
                    ItemId = EssenceProgressionConstants.GreaterMonsterCoreItemId,
                    MinAmount = 2,
                    MaxAmount = 5
                },
                new DungeonRewardGrant
                {
                    ItemId = EssenceProgressionConstants.LesserMonsterCoreItemId,
                    MinAmount = 4,
                    MaxAmount = 8
                }
            ],
            _ =>
            [
                new DungeonRewardGrant
                {
                    ItemId = EssenceProgressionConstants.LesserMonsterCoreItemId,
                    MinAmount = 3,
                    MaxAmount = 6
                }
            ]
        };

    public static IReadOnlyList<DungeonRewardGrant> GetFirstCompletionGrants(DungeonDefinition dungeon)
    {
        if (dungeon.RewardTable.FirstClearRewards.Count > 0)
        {
            return dungeon.RewardTable.FirstClearRewards;
        }

        return dungeon.Grade switch
        {
            DungeonGrade.GradeII =>
            [
                new DungeonRewardGrant
                {
                    ItemId = EssenceProgressionConstants.GreaterMonsterCoreItemId,
                    MinAmount = 12,
                    MaxAmount = 12
                }
            ],
            DungeonGrade.GradeIII =>
            [
                new DungeonRewardGrant
                {
                    ItemId = EssenceProgressionConstants.PrimalMonsterCoreItemId,
                    MinAmount = 24,
                    MaxAmount = 24
                }
            ],
            _ =>
            [
                new DungeonRewardGrant
                {
                    ItemId = EssenceProgressionConstants.LesserMonsterCoreItemId,
                    MinAmount = 6,
                    MaxAmount = 6
                }
            ]
        };
    }
}
