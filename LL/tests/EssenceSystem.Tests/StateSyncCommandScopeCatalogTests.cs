using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using Application.MediatR.Synchronization;
using Application.UseCases.CharacterActions.Commands.ResolveCharacterAction;
using Application.UseCases.Colosseum.Commands.BackfillChampionMarketTitleGrants;
using Application.UseCases.Colosseum.Commands.PurchaseChampionMarketItem;
using Application.UseCases.Colosseum.Commands.StartArenaBattle;
using Application.UseCases.Colosseum.Commands.UpdateArenaDefenseSnapshot;
using Application.UseCases.Crafting.Commands.CraftItems;
using Application.UseCases.Dungeons.Commands.ClaimDungeonRewards;
using Application.UseCases.Essences.Commands.AbsorbUnboundEssence;
using Application.UseCases.Essences.Commands.AscendEssence;
using Application.UseCases.Essences.Commands.DismantleUnboundEssence;
using Application.UseCases.Essences.Commands.EvolveEssence;
using Application.UseCases.Essences.Commands.FavoriteEssence;
using Application.UseCases.Essences.Commands.SpendEssenceDust;
using Application.UseCases.Equipments.Commands.EquipEquipment;
using Application.UseCases.Inventories.Commands.MarkInventoryItemSeen;
using Application.UseCases.Inventories.Commands.SetInventoryItemFavorite;
using Application.UseCases.MarketPlaces.Commands.BuyCommodity;
using Application.UseCases.MarketPlaces.Commands.BuyoutMarketPlaceListing;
using Application.UseCases.Prophecies.Commands.AcceptProphecy;
using Application.UseCases.Soulstones.Commands.PurchaseSoulstoneUpgrade;
using Application.UseCases.Titles.Commands.EquipTitle;
using Application.WebSockets.Contracts;

namespace EssenceSystem.Tests;

public sealed class StateSyncCommandScopeCatalogTests
{
    [Fact]
    public void SpecializedTransactionalCommandsRequireExplicitScopeRegistration()
    {
        var missingCommands = typeof(ICommandBase).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ICommandBase).IsAssignableFrom(type))
            .Where(StateSyncCommandScopeCatalog.RequiresExplicitRegistration)
            .Where(type => !Attribute.IsDefined(type, typeof(NonTransactionalAttribute)))
            .Where(type => !StateSyncCommandScopeCatalog.IsExplicitlyRegistered(type))
            .Select(type => type.FullName)
            .Order()
            .ToArray();

        Assert.True(
            missingCommands.Length == 0,
            $"Commands missing an explicit state-sync scope contract:{Environment.NewLine}{string.Join(Environment.NewLine, missingCommands)}");
    }

    [Fact]
    public void MarketplaceCommandKeepsIncompleteInventoryOnTargetedReconciliation()
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(typeof(BuyCommodityCommand));

        Assert.Contains(StateSyncScopes.Inventory, profile.CharacterScopes);
        Assert.DoesNotContain(StateSyncScopes.Inventory, profile.CharacterResponseSemantics.Keys);
        Assert.Contains(StateSyncScopes.Marketplace, profile.WorldResponseSemantics.Keys);
        Assert.Equal(
            StateSyncResponseSemantics.OrderedDelta,
            profile.WorldResponseSemantics[StateSyncScopes.Marketplace]);
    }

    [Fact]
    public void ColosseumUsesCharacterGenerationsInsteadOfGlobalFanout()
    {
        Assert.Contains(StateSyncScopes.Colosseum, StateSyncScopes.CharacterResources);
        Assert.DoesNotContain(StateSyncScopes.Colosseum, StateSyncScopes.WorldResources);

        var battle = StateSyncCommandScopeCatalog.GetProfile(typeof(StartArenaBattleCommand));
        Assert.Contains(StateSyncScopes.Colosseum, battle.CharacterScopes);
        Assert.Contains(StateSyncScopes.Colosseum, battle.CharacterResponseSemantics.Keys);
        Assert.Contains(StateSyncScopes.Character, battle.CharacterResponseSemantics.Keys);
        Assert.Empty(battle.WorldScopes);
    }

    [Fact]
    public void GuildUsesAudienceGenerationsInsteadOfGlobalFanout()
    {
        Assert.Contains(StateSyncScopes.Guild, StateSyncScopes.GuildResources);
        Assert.Contains(StateSyncScopes.GuildBuildings, StateSyncScopes.GuildResources);
        Assert.Contains(StateSyncScopes.GuildMissions, StateSyncScopes.GuildResources);
        Assert.DoesNotContain(StateSyncScopes.Guild, StateSyncScopes.WorldResources);
        Assert.DoesNotContain(StateSyncScopes.Guild, StateSyncScopes.CharacterResources);
        Assert.Contains(StateSyncScopes.GuildMembership, StateSyncScopes.CharacterResources);
        Assert.Contains(StateSyncScopes.GuildInvites, StateSyncScopes.CharacterResources);
        Assert.Contains(StateSyncScopes.GuildShop, StateSyncScopes.CharacterResources);
        Assert.Contains(StateSyncScopes.GuildDirectory, StateSyncScopes.WorldResources);
    }

    [Fact]
    public void RaidsUseDomainVersionsAndKeepOnlyWorldDirectoryRecovery()
    {
        Assert.Contains(StateSyncScopes.RaidDirectory, StateSyncScopes.WorldResources);

        var join = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Raids.JoinRaidCommand));
        var refresh = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Raids.RefreshRaidSnapshotCommand));
        var claim = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Raids.ClaimRaidRewardsCommand));

        Assert.Empty(join.CharacterScopes);
        Assert.Contains(StateSyncScopes.RaidDirectory, join.WorldResponseSemantics.Keys);
        Assert.Empty(refresh.CharacterScopes);
        Assert.Empty(refresh.WorldScopes);
        Assert.Contains(StateSyncScopes.Inventory, claim.CharacterScopes);
        Assert.Empty(claim.WorldScopes);
    }

    [Fact]
    public void GuildCommandsInvalidateOnlyTheirOwnedSubresources()
    {
        var description = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Guilds.Commands.UpdateGuildDescription.UpdateGuildDescriptionCommand));
        var donation = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Guilds.Commands.DonateGuildVaultItem.DonateGuildVaultItemCommand));
        var building = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Guilds.Commands.ConstructGuildBuilding.ConstructGuildBuildingCommand));
        var mission = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Guilds.Commands.SelectGuildMission.SelectGuildMissionCommand));
        var shop = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Guilds.Commands.PurchaseGuildShopItem.PurchaseGuildShopItemCommand));

        Assert.Empty(description.CharacterScopes);
        Assert.Equal([StateSyncScopes.Guild], description.WorldScopes);

        Assert.Equal([StateSyncScopes.Inventory], donation.CharacterScopes);
        Assert.Equal([StateSyncScopes.Guild], donation.WorldScopes);

        Assert.Contains(StateSyncScopes.Guild, building.WorldScopes);
        Assert.Contains(StateSyncScopes.GuildBuildings, building.WorldScopes);
        Assert.Contains(StateSyncScopes.GuildBuildings, building.WorldResponseSemantics.Keys);
        Assert.Contains(StateSyncScopes.GuildShop, building.CharacterScopes);
        Assert.DoesNotContain(StateSyncScopes.Inventory, building.CharacterScopes);

        Assert.Equal([StateSyncScopes.GuildMissions], mission.WorldScopes);
        Assert.Contains(StateSyncScopes.GuildMissions, mission.WorldResponseSemantics.Keys);
        Assert.Empty(mission.CharacterScopes);

        Assert.Contains(StateSyncScopes.GuildShop, shop.CharacterResponseSemantics.Keys);
        Assert.Contains(StateSyncScopes.Inventory, shop.CharacterResponseSemantics.Keys);
        Assert.Contains(StateSyncScopes.Achievements, shop.CharacterScopes);
        Assert.Empty(shop.WorldScopes);
    }

    [Fact]
    public void GuildLifecycleSeparatesSharedMembershipInvitesAndDirectoryRecovery()
    {
        var lifecycle = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Guilds.Commands.AcceptInvite.AcceptInviteCommand));
        var invitation = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Guilds.Commands.Invite.InviteCommand));
        var departure = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Guilds.Commands.LeaveGuild.LeaveGuildCommand));
        var sharedMutation = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Guilds.Commands.UpdateGuildDescription.UpdateGuildDescriptionCommand));

        Assert.Contains(StateSyncScopes.GuildMembership, lifecycle.CharacterScopes);
        Assert.Contains(StateSyncScopes.GuildInvites, lifecycle.CharacterScopes);
        Assert.Contains(StateSyncScopes.Achievements, lifecycle.CharacterScopes);
        Assert.Contains(StateSyncScopes.GuildDirectory, lifecycle.WorldScopes);
        Assert.Contains(StateSyncScopes.GuildDirectory, lifecycle.WorldResponseSemantics.Keys);
        Assert.DoesNotContain(StateSyncScopes.Guild, lifecycle.WorldResponseSemantics.Keys);
        Assert.Contains(StateSyncScopes.GuildInvites, invitation.CharacterScopes);
        Assert.DoesNotContain(StateSyncScopes.GuildDirectory, invitation.WorldScopes);
        Assert.Contains(StateSyncScopes.Inventory, departure.CharacterScopes);
        Assert.Contains(StateSyncScopes.Equipment, departure.CharacterScopes);
        Assert.DoesNotContain(StateSyncScopes.GuildMembership, sharedMutation.CharacterScopes);
        Assert.DoesNotContain(StateSyncScopes.GuildInvites, sharedMutation.CharacterScopes);
    }

    [Theory]
    [InlineData(typeof(PurchaseChampionMarketItemCommand))]
    [InlineData(typeof(UpdateArenaDefenseSnapshotCommand))]
    public void CompleteColosseumMutationsOwnTheirCharacterGeneration(Type commandType)
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(commandType);

        Assert.Contains(StateSyncScopes.Colosseum, profile.CharacterScopes);
        Assert.Contains(StateSyncScopes.Colosseum, profile.CharacterResponseSemantics.Keys);
        Assert.Empty(profile.WorldScopes);
    }

    [Fact]
    public void ChampionMarketTitleBackfillDoesNotInvalidateArenaState()
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(
            typeof(BackfillChampionMarketTitleGrantsCommand));

        Assert.Empty(profile.CharacterScopes);
        Assert.Empty(profile.WorldScopes);
    }

    [Fact]
    public void CharacterActionResolutionRefreshesSummaryOnlyWhenCharacterChanged()
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(typeof(ResolveCharacterActionCommand));

        Assert.True(profile.RefreshCharacterSummaryWhenChanged);
    }

    [Theory]
    [InlineData(typeof(AbsorbUnboundEssenceCommand))]
    [InlineData(typeof(AscendEssenceCommand))]
    [InlineData(typeof(DismantleUnboundEssenceCommand))]
    [InlineData(typeof(EvolveEssenceCommand))]
    [InlineData(typeof(SpendEssenceDustCommand))]
    public void CompleteEssenceMutationsReturnTheirOwnedScopeVersions(Type commandType)
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(commandType);

        Assert.Equal(
            [StateSyncScopes.Essences, StateSyncScopes.Inventory, StateSyncScopes.Equipment],
            profile.CharacterScopes);
        Assert.Equal(
            profile.CharacterScopes.Order(),
            profile.CharacterResponseSemantics.Keys.Order());
        Assert.DoesNotContain(StateSyncScopes.Quests, profile.CharacterScopes);
        Assert.Empty(profile.WorldScopes);
        Assert.True(profile.RefreshCharacterOverview);
        Assert.True(profile.RefreshCharacterSummaryWhenChanged);
    }

    [Fact]
    public void EssenceStateMutationsOwnOnlyTheEssenceSnapshot()
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(typeof(FavoriteEssenceCommand));

        Assert.Equal([StateSyncScopes.Essences], profile.CharacterScopes);
        Assert.Contains(StateSyncScopes.Essences, profile.CharacterResponseSemantics.Keys);
        Assert.DoesNotContain(StateSyncScopes.Inventory, profile.CharacterScopes);
        Assert.DoesNotContain(StateSyncScopes.Equipment, profile.CharacterScopes);
        Assert.DoesNotContain(StateSyncScopes.Quests, profile.CharacterScopes);
    }

    [Theory]
    [InlineData(typeof(global::Application.UseCases.Essences.Commands.DeleteEssenceLoadout.DeleteEssenceLoadoutCommand))]
    [InlineData(typeof(global::Application.UseCases.Essences.Commands.SaveEssenceLoadout.SaveEssenceLoadoutCommand))]
    [InlineData(typeof(global::Application.UseCases.Essences.Commands.SetEssenceLoadoutAutoUseActivities.SetEssenceLoadoutAutoUseActivitiesCommand))]
    [InlineData(typeof(global::Application.UseCases.Essences.Commands.SetEssenceFocus.SetEssenceFocusCommand))]
    public void RemainingEssenceStateMutationsOwnTheirSnapshot(Type commandType)
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(commandType);

        Assert.Equal([StateSyncScopes.Essences], profile.CharacterScopes);
        Assert.Equal([StateSyncScopes.Essences], profile.CharacterResponseSemantics.Keys);
    }

    [Theory]
    [InlineData(typeof(global::Application.UseCases.Professions.Commands.CancelTemperingQueue.CancelTemperingQueueCommand))]
    [InlineData(typeof(global::Application.UseCases.Professions.Commands.RemoveCraftingQueueItem.RemoveCraftingQueueItemCommand))]
    public void TemperingQueueDeltasAdvanceInventoryWithoutCharacterRefreshes(Type commandType)
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(commandType);

        Assert.Equal([StateSyncScopes.Inventory], profile.CharacterScopes);
        Assert.Equal([StateSyncScopes.Inventory], profile.CharacterResponseSemantics.Keys);
        Assert.Equal(
            StateSyncResponseSemantics.OrderedDelta,
            profile.CharacterResponseSemantics[StateSyncScopes.Inventory]);
        Assert.False(profile.RefreshCharacterOverview);
        Assert.True(profile.RefreshCharacterSummaryWhenChanged);
    }

    [Fact]
    public void StartTemperingReturnsAnOrderedInventoryDelta()
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.CharacterActions.Commands.StartCraftingAction.StartCraftingActionCommand));

        Assert.Empty(profile.CharacterScopes);
        Assert.Equal(
            StateSyncResponseSemantics.OrderedDelta,
            profile.CharacterResponseSemantics[StateSyncScopes.Inventory]);
        Assert.True(profile.InventoryWhenChanged);
        Assert.False(profile.RefreshCharacterOverview);
    }

    [Theory]
    [InlineData(typeof(global::Application.UseCases.Dungeons.Commands.StartDungeonRun.StartDungeonRunCommand))]
    [InlineData(typeof(global::Application.UseCases.Dungeons.Commands.ExecuteDungeonAction.ExecuteDungeonActionCommand))]
    [InlineData(typeof(global::Application.UseCases.Dungeons.Commands.DismissFailedDungeonRun.DismissFailedDungeonRunCommand))]
    [InlineData(typeof(global::Application.UseCases.Dungeons.Commands.AssembleDungeonSigil.AssembleDungeonSigilCommand))]
    public void DungeonMutationResponsesOwnTheDungeonSnapshot(Type commandType)
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(commandType);

        Assert.Contains(StateSyncScopes.Dungeons, profile.CharacterResponseSemantics.Keys);
        Assert.All(
            profile.CharacterResponseSemantics.Keys.Where(scope => scope != StateSyncScopes.Character),
            scope => Assert.Contains(scope, profile.CharacterScopes));
    }

    [Fact]
    public void DungeonRewardClaimReturnsLocalVersionsButLeavesQuestProgressAsynchronous()
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(typeof(ClaimDungeonRewardsCommand));

        Assert.Contains(StateSyncScopes.Dungeons, profile.CharacterResponseSemantics.Keys);
        Assert.Contains(StateSyncScopes.Inventory, profile.CharacterResponseSemantics.Keys);
        Assert.Contains(StateSyncScopes.Character, profile.CharacterResponseSemantics.Keys);
        Assert.DoesNotContain(StateSyncScopes.Quests, profile.CharacterScopes);
    }

    [Fact]
    public void QuestMutationsSynchronizeTheirOwnedState()
    {
        var choice = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Quests.Commands.SelectQuestChoice.SelectQuestChoiceCommand));
        var encounter = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Quests.Commands.StartQuestEncounter.StartQuestEncounterCommand));

        Assert.Equal([StateSyncScopes.Quests], choice.CharacterScopes);
        Assert.Contains(StateSyncScopes.Quests, choice.CharacterResponseSemantics.Keys);
        Assert.DoesNotContain(StateSyncScopes.AreaAccess, choice.CharacterScopes);
        Assert.False(choice.RefreshCharacterOverview);

        Assert.Contains(StateSyncScopes.Quests, encounter.CharacterScopes);
        Assert.Contains(StateSyncScopes.AreaAccess, encounter.CharacterScopes);
        Assert.True(encounter.InventoryWhenChanged);
        Assert.DoesNotContain(StateSyncScopes.Inventory, encounter.CharacterScopes);
        Assert.Equal(
            StateSyncResponseSemantics.OrderedDelta,
            encounter.CharacterResponseSemantics[StateSyncScopes.Inventory]);
    }

    [Fact]
    public void EventQuestClaimsOwnTheirJournalButReconcileInventoryRewards()
    {
        var claim = StateSyncCommandScopeCatalog.GetProfile(
            typeof(global::Application.UseCases.Quests.Events.Commands.ClaimEventQuestReward.ClaimEventQuestRewardCommand));

        Assert.Contains(StateSyncScopes.EventQuests, claim.CharacterResponseSemantics.Keys);
        Assert.Contains(StateSyncScopes.Inventory, claim.CharacterScopes);
        Assert.DoesNotContain(StateSyncScopes.Inventory, claim.CharacterResponseSemantics.Keys);
    }

    [Theory]
    [InlineData(typeof(CraftItemsCommand), StateSyncScopes.Inventory)]
    [InlineData(typeof(AcceptProphecyCommand), StateSyncScopes.Prophecies)]
    [InlineData(typeof(PurchaseSoulstoneUpgradeCommand), StateSyncScopes.Soulstones)]
    [InlineData(typeof(EquipTitleCommand), StateSyncScopes.Achievements)]
    public void CompleteLocalMutationResponsesAdvanceOwnedVersionsOnly(
        Type commandType,
        string ownedScope)
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(commandType);

        Assert.Contains(ownedScope, profile.CharacterResponseSemantics.Keys);
        Assert.Empty(profile.WorldScopes);
    }

    [Theory]
    [InlineData(typeof(BuyCommodityCommand))]
    [InlineData(typeof(BuyoutMarketPlaceListingCommand))]
    public void MarketplaceUsesSemanticLiveEventsAndSilentReconnectVersions(Type commandType)
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(commandType);

        Assert.Contains(StateSyncScopes.Marketplace, profile.WorldScopes);
        Assert.Contains(StateSyncScopes.Marketplace, profile.WorldResponseSemantics.Keys);
        Assert.Contains(StateSyncScopes.Character, profile.CharacterResponseSemantics.Keys);
        Assert.Equal(
            StateSyncResponseSemantics.OrderedDelta,
            profile.WorldResponseSemantics[StateSyncScopes.Marketplace]);
    }

    [Theory]
    [InlineData(typeof(EquipEquipmentCommand))]
    [InlineData(typeof(MarkInventoryItemSeenCommand))]
    public void Complete_inventory_responses_own_their_scope_versions(Type commandType)
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(commandType);

        Assert.NotEmpty(profile.CharacterResponseSemantics.Keys);
        Assert.All(
            profile.CharacterResponseSemantics.Keys,
            scope => Assert.Contains(scope, profile.CharacterScopes));
    }

    [Fact]
    public void Favorite_response_owns_inventory_but_not_the_partial_equipment_view()
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(typeof(SetInventoryItemFavoriteCommand));

        Assert.Contains(StateSyncScopes.Inventory, profile.CharacterResponseSemantics.Keys);
        Assert.DoesNotContain(StateSyncScopes.Equipment, profile.CharacterResponseSemantics.Keys);
    }
}
