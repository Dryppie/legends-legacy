using Domain.Models.Dungeons;
using Domain.Models.Essences;

namespace Domain.Models.Dungeons.Definitions;

public static class DungeonRewardCatalog
{
    public static IReadOnlyList<string> GetMonsterCoreRewardItemIds(DungeonGrade grade) =>
        grade switch
        {
            DungeonGrade.GradeII =>
            [
                EssenceProgressionConstants.GreaterMonsterCoreItemId,
                EssenceProgressionConstants.LesserMonsterCoreItemId
            ],
            DungeonGrade.GradeIII =>
            [
                EssenceProgressionConstants.PrimalMonsterCoreItemId,
                EssenceProgressionConstants.GreaterMonsterCoreItemId,
                EssenceProgressionConstants.LesserMonsterCoreItemId
            ],
            _ =>
            [
                EssenceProgressionConstants.LesserMonsterCoreItemId
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
