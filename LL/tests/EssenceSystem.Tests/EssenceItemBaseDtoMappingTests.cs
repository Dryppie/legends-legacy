using Application;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Quests;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Application.UseCases.Items.Dtos;
using Application.UseCases.Quests.Dtos;
using Application.WebSockets.Contracts;
using AutoMapper;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences.Definitions;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.EssenceItems;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class EssenceItemBaseDtoMappingTests
{
    [Fact]
    public void Application_mapper_embeds_the_definition_for_an_essence_item_base()
    {
        var definition = new EssenceDefinition
        {
            Id = "essence.goblin_warrior",
            Name = "Goblin Warrior",
            ActiveAbility = new AbilitySpec
            {
                Id = "ability.raging_cleave",
                Kind = AbilitySpecKind.Active,
                Name = "Raging Cleave"
            },
            PassiveAbility = new AbilitySpec
            {
                Id = "ability.battle_fury",
                Kind = AbilitySpecKind.Passive,
                Name = "Battle Fury"
            }
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IEssenceDefinitionRepository>(
            new SingleEssenceDefinitionRepository(definition));
        services.AddApplication();

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();
        ItemBase itemBase = new EssenceItemBase
        {
            Id = "item.essence.goblin_warrior",
            Name = "Unbound Goblin Warrior Essence"
        };

        var result = Assert.IsType<EssenceItemBaseDto>(
            mapper.Map<ItemBaseDto>(itemBase));

        Assert.Equal(definition.Id, result.EssenceDefinitionId);
        Assert.NotNull(result.Essence);
        Assert.Equal("Raging Cleave", result.Essence.ActiveAbility.Name);
        Assert.Equal("Battle Fury", result.Essence.PassiveAbility.Name);

        var option = new QuestChoiceOption(
            "goblin_warrior",
            "Hunt the Goblin Warrior",
            "An aggressive bruiser.",
            Guid.NewGuid(),
            "Goblin Warrior",
            definition.Id,
            itemBase.Id,
            "first-hunt",
            itemBase);
        var optionResult = mapper.Map<QuestChoiceOptionDto>(option);
        var optionItem = Assert.IsType<EssenceItemBaseDto>(
            optionResult.RewardItemBase);

        Assert.NotNull(optionItem.Essence);
        Assert.Equal("Raging Cleave", optionItem.Essence.ActiveAbility.Name);
        Assert.Equal("Battle Fury", optionItem.Essence.PassiveAbility.Name);

        var inventoryItem = new InventoryItem
        {
            ItemInstanceId = Guid.NewGuid(),
            ItemInstance = new EssenceItemInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase
            }
        };
        var combatResult = mapper.Map<CombatResultDto>(new CombatResult
        {
            Loot = [inventoryItem]
        });
        var combatLoot = Assert.Single(combatResult.Loot);
        var combatLootItem = Assert.IsType<EssenceItemBaseDto>(
            combatLoot.ItemInstance.ItemBase);

        Assert.NotNull(combatLootItem.Essence);
        Assert.Equal("Raging Cleave", combatLootItem.Essence.ActiveAbility.Name);
        Assert.Equal("Battle Fury", combatLootItem.Essence.PassiveAbility.Name);

        var combatJson = JsonSerializer.Serialize(
            combatResult,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"essence\":", combatJson, StringComparison.Ordinal);
        Assert.Contains("Raging Cleave", combatJson, StringComparison.Ordinal);
        Assert.Contains("Battle Fury", combatJson, StringComparison.Ordinal);

        var realtimeEvent = new QuestJournalChanged(
            new QuestJournalDto
            {
                Quests =
                [
                    new QuestStateDto
                    {
                        QuestId = "quest.onboarding.training_day",
                        Choice = new QuestChoiceDto { Options = [optionResult] }
                    }
                ]
            },
            1);
        var json = JsonSerializer.Serialize(
            realtimeEvent,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"essence\":", json, StringComparison.Ordinal);
        Assert.Contains("Raging Cleave", json, StringComparison.Ordinal);
        Assert.Contains("Battle Fury", json, StringComparison.Ordinal);
    }

    private sealed class SingleEssenceDefinitionRepository(EssenceDefinition definition)
        : IEssenceDefinitionRepository
    {
        public IReadOnlyList<EssenceDefinition> GetAll() => [definition];

        public IReadOnlyList<AbilitySpec> GetAllAbilities() =>
            [definition.ActiveAbility, definition.PassiveAbility];

        public EssenceDefinition? GetById(string essenceDefinitionId) =>
            essenceDefinitionId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase)
                ? definition
                : null;

        public AbilitySpec? GetAbilityById(string abilityId) =>
            GetAllAbilities().FirstOrDefault(ability =>
                ability.Id.Equals(abilityId, StringComparison.OrdinalIgnoreCase));
    }
}
