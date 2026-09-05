using Domain.Models.Items.Equipments.Progression;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class EquipmentProgressionQuestSupportTests
{
    [Fact]
    public void Ordinary_regional_drops_establish_equipment_quest_credit()
    {
        var characterId = Guid.NewGuid();
        var award = Award(characterId, EquipmentAwardKind.RandomDiscovery,
            EquipmentOwnershipKind.UnboundPersonal, rank: 0);
        var entitlement = new PlainEquipmentEntitlement
        {
            CharacterId = characterId,
            DefinitionId = award.State.DefinitionId,
            Tier = award.State.Tier
        };

        entitlement.RecordAward(award);

        Assert.Equal(1, entitlement.Copies);
    }

    [Fact]
    public void Dungeon_rank_and_non_random_awards_do_not_establish_ordinary_area_credit()
    {
        var characterId = Guid.NewGuid();
        var entitlement = new PlainEquipmentEntitlement
        {
            CharacterId = characterId,
            DefinitionId = "plain.dagger",
            Tier = 1
        };

        Assert.Throws<ArgumentException>(() => entitlement.RecordAward(Award(characterId,
            EquipmentAwardKind.RandomDiscovery, EquipmentOwnershipKind.UnboundPersonal, rank: 1)));
        Assert.Throws<ArgumentException>(() => entitlement.RecordAward(Award(characterId,
            EquipmentAwardKind.QuestReward, EquipmentOwnershipKind.BoundPersonal, rank: 0)));
    }

    private static EquipmentData Award(Guid characterId, EquipmentAwardKind kind,
        EquipmentOwnershipKind ownership, int rank)
    {
        var root = ContentRoot();
        var equipment = JsonStarterEquipmentCatalog.Load(Path.Combine(root, "equipment-starters.v1.json"));
        return EquipmentData.Create(EquipmentState.Award(Guid.NewGuid(), equipment.Evaluator,
            "plain.dagger", 1, rank, new(kind, "test", "test"), new(ownership, characterId)), equipment.Evaluator);
    }

    private static string ContentRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "LL/src/API/API.LL/Data/equipment");
            if (Directory.Exists(path)) return path;
        }
        throw new DirectoryNotFoundException();
    }
}
