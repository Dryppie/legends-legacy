using System.Text.Json;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.Quests;
using Application.UseCases.Outbox;
using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Quests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence.LL;
using Persistence.LL.Repositories.Quests;
using Services.LL.Quests;

namespace EssenceSystem.Tests;

public sealed partial class QuestSystemTests
{
    private static JsonQuestDefinitionProvider EquipmentDefinitions(bool enabled = true, bool capabilities = true)
    {
        var flags = new Dictionary<string, string?> { ["EquipmentProgression:QuestIntegrationEnabled"] = enabled.ToString() };
        foreach (var key in new[] { "StarterAcquisitionEnabled", "ForgeEnabled", "BaselineRecoveryEnabled", "ProtectedAcquisitionEnabled", "OrdinaryAcquisitionEnabled" })
            flags["EquipmentProgression:" + key] = capabilities.ToString();
        return new(new ConfigurationBuilder().AddInMemoryCollection(flags).Build(), FindApiRoot(), new(JsonSerializerDefaults.Web));
    }

    [Fact]
    public void EquipmentProgression_quest_catalog_is_opt_in_and_preserves_area_tokens_and_core_rewards()
    {
        var definitions = EquipmentDefinitions();
        Assert.Equal(29, definitions.GetAll().Count);
        Assert.Equal(3, EquipmentDefinitions(false).Get(QuestConstants.SoulArchive).Version);
        Assert.Equal(3, EquipmentDefinitions(false).GetLatestVersion(QuestConstants.SoulArchive));
        Assert.Equal(500, Assert.Single(definitions.Get(QuestConstants.SoulArchive).Rewards).Quantity);
        Assert.Equal("Cinders", Assert.Single(definitions.Get(QuestConstants.SoulArchive).Rewards).Type);
        Assert.Equal(10, Assert.Single(definitions.Get(QuestConstants.FirstWeapon).Rewards).Quantity);
        foreach (var quest in definitions.GetAll().Where(x => x.Chain?.Id == "quest.chain.shenic" && x.Version == 4))
        {
            Assert.Equal(2, Assert.Single(quest.Rewards, x => x.ItemBaseId == "tempered_scrap").Quantity);
            Assert.DoesNotContain(quest.Rewards, x => new[] { "ore", "wood", "rawhide", "soul_dust" }.Contains(x.ItemBaseId));
        }
        Assert.Contains(definitions.Get(QuestConstants.TrialOfLumo).Rewards, x => x.ItemBaseId == "sigil_goblin_mines" && x.Quantity == 1);
        Assert.Contains(definitions.Get(QuestConstants.CrystalCurrents).Rewards, x => x.ItemBaseId == "sigil_forgotten_catacombs" && x.Quantity == 1);
        var adaptation = definitions.Get(QuestConstants.RestlessDead);
        Assert.Equal("Sequential", adaptation.ObjectiveMode);
        Assert.Equal("ModelEPlainTargetEquipped", adaptation.Objectives[0].Type);
    }

    [Fact]
    public async Task EquipmentProgression_currency_rewards_are_recorded_once_and_existing_active_quests_keep_their_version()
    {
        var id = Guid.NewGuid();
        var definitions = EquipmentDefinitions();
        var repository = new RecordingQuestRepository(1);
        var firstHunt = CreateCompletedProgress(id, definitions.Get(QuestConstants.TrainingDay));
        firstHunt.SelectedOptionKey = definitions.Get(QuestConstants.TrainingDay).Choice!.Options[0].Key;
        repository.Progresses.Add(firstHunt);
        var soul = CreateCompletedProgress(id, definitions.Get(QuestConstants.SoulArchive));
        soul.Status = QuestStatus.Active;
        soul.CompletedAt = null;
        repository.Progresses.Add(soul);
        repository.Progresses.Add(CreateActiveProgress(id, definitions.Get(QuestConstants.FirstWeapon, 2), false));
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Characters.Add(new Character { Id = id, UserId = Guid.NewGuid(), Name = "Quest", NormalizedName = "QUEST", Cinders = 7 });
        await db.SaveChangesAsync();
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(), new RecordingInventoryItemFactory(), new RecordingLootRewardWriter(), TimeProvider.System,
            currencyRewards: new QuestEquipmentRewardRepository(db), equipmentProgressionEquipment: new QuestEquipmentSupport());
        for (var i = 0; i < 2; i++)
            await service.ProcessAsync(id, QuestTrigger.EquipmentChanged(), null, GameEventTypes.EquipmentChanged, CancellationToken.None);
        await db.SaveChangesAsync();
        Assert.Equal(507, (await db.Characters.SingleAsync()).Cinders);
        var ledger = await db.EconomyLedger.SingleAsync();
        Assert.Equal(500, ledger.Quantity);
        Assert.Equal(QuestConstants.SoulArchive, ledger.Source);
        Assert.NotNull(soul.RewardsGrantedAt);
        Assert.Equal(2, repository.Progresses.Single(x => x.QuestId == QuestConstants.FirstWeapon).DefinitionVersion);
    }

    [Fact]
    public async Task EquipmentProgression_first_weapon_awards_accessories_once_when_the_kit_is_equipped()
    {
        var id = Guid.NewGuid();
        var definitions = EquipmentDefinitions();
        var repository = new RecordingQuestRepository(1);
        repository.Progresses.Add(CreateCompletedProgress(id, definitions.Get(QuestConstants.TrainingDay)));
        repository.Progresses.Add(CreateCompletedProgress(id, definitions.Get(QuestConstants.SoulArchive)));
        repository.Progresses.Add(CreateActiveProgress(id, definitions.Get(QuestConstants.FirstWeapon), true));
        var equipment = new QuestEquipmentSupport();
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(), new RecordingInventoryItemFactory(), new RecordingLootRewardWriter(), TimeProvider.System,
            equipmentProgressionEquipment: equipment, starterClaims: equipment);
        await service.GetJournalAsync(id, CancellationToken.None);
        Assert.Equal(0, equipment.Claims);
        await service.ProcessAsync(id, QuestTrigger.EquipmentChanged(), null, GameEventTypes.EquipmentChanged, CancellationToken.None);
        Assert.Equal(0, equipment.Claims);
        equipment.Equipped = true;
        var result = await service.ProcessAsync(id, QuestTrigger.EquipmentChanged(), null, GameEventTypes.EquipmentChanged, CancellationToken.None);
        Assert.Equal(10, Assert.Single(result.Loot).Quantity);
        Assert.Equal("tempered_scrap", Assert.Single(result.Loot).ItemInstance.ItemBaseId);
        Assert.Contains(result.Journal.Quests, x => x.QuestId == QuestConstants.ToolsOfTheTrade && x.Version == 2 && x.Title == "Ready for the Road");
        await service.ProcessAsync(id, QuestTrigger.EquipmentChanged(), null, GameEventTypes.EquipmentChanged, CancellationToken.None);
        Assert.Equal(1, equipment.Claims);
        Assert.Equal(QuestStatus.Completed, repository.Progresses.Single(x => x.QuestId == QuestConstants.ToolsOfTheTrade).Status);
    }

    private sealed class QuestEquipmentSupport : IEquipmentQuestSupport, IStarterEquipmentService
    {
        public bool Equipped { get; set; }
        public int Claims { get; private set; }
        public Task<bool> IsEquippedAsync(Guid id, string objective, string? kind, CancellationToken ct) => Task.FromResult(Equipped);
        public Task<StarterEquipmentClaimResult> ClaimAsync(Guid id, StarterEquipmentGrantKind kind, IReadOnlyList<string> ids, CancellationToken ct)
        {
            Assert.Equal(StarterEquipmentGrantKind.ReadyForRoad, kind);
            Assert.Empty(ids);
            Claims++;
            return Task.FromResult(new StarterEquipmentClaimResult(null, null));
        }
        public Task<EquipmentAccess> GetAccessAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public IReadOnlyList<StarterEquipmentOption> GetOptions() => throw new NotSupportedException();
    }
}
