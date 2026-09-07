using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;
using Services.LL.Items;
using Services.LL.Quests;

namespace EssenceSystem.Tests;

public sealed class EquipmentProgressionQuestSupportTests
{
    [Fact]
    public async Task Starter_chest_objective_uses_the_saved_claim_without_requiring_equipped_items()
    {
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var characterId = Guid.NewGuid();
        var starters = new StarterEquipmentRepository(db);
        var support = new EquipmentQuestSupport(null!, starters, null!);
        Assert.False(await support.HasStarterClaimAsync(characterId, "FirstWeapon", default));

        starters.AddGrant(new StarterEquipmentGrant(characterId, StarterEquipmentGrantKind.FirstWeapon,
            [Award(characterId, EquipmentAwardKind.QuestReward, EquipmentOwnershipKind.BoundPersonal, 0)],
            DateTimeOffset.UtcNow));
        Assert.True(await support.HasStarterClaimAsync(characterId, "FirstWeapon", default));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.True(await support.HasStarterClaimAsync(characterId, "FirstWeapon", default));
        Assert.False(await support.HasStarterClaimAsync(Guid.NewGuid(), "FirstWeapon", default));
        Assert.False(await support.HasStarterClaimAsync(characterId, "ReadyForRoad", default));
    }

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
