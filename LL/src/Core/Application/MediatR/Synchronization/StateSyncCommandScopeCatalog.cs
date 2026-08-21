using Application.WebSockets.Contracts;

namespace Application.MediatR.Synchronization;

public sealed record StateSyncCommandScopeProfile(
    IReadOnlyList<string> CharacterScopes,
    IReadOnlyList<string> WorldScopes,
    bool RefreshCharacterOverview = true,
    bool InventoryWhenChanged = false,
    bool RefreshCharacterSummaryWhenChanged = false);

/// <summary>
/// Compile-time command-to-resource contract used by the transaction pipeline.
/// Feature commands that affect a specialized synchronization resource must be
/// registered here; architecture tests prevent new commands in these feature
/// families from silently falling back to character-only invalidation.
/// </summary>
public static class StateSyncCommandScopeCatalog
{
    private static readonly StateSyncCommandScopeProfile DefaultProfile = new([], []);
    private static readonly IReadOnlyDictionary<Type, StateSyncCommandScopeProfile> Profiles = BuildProfiles();

    public static StateSyncCommandScopeProfile GetProfile(Type commandType) =>
        Profiles.TryGetValue(commandType, out var profile) ? profile : DefaultProfile;

    public static bool IsExplicitlyRegistered(Type commandType) => Profiles.ContainsKey(commandType);

    public static bool RequiresExplicitRegistration(Type commandType)
    {
        var commandNamespace = commandType.Namespace ?? string.Empty;
        return ExplicitFeatureNamespacePrefixes.Any(prefix =>
            commandNamespace.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static readonly string[] ExplicitFeatureNamespacePrefixes =
    [
        "Application.UseCases.Achievements.",
        "Application.UseCases.CharacterActions.",
        "Application.UseCases.Colosseum.",
        "Application.UseCases.Crafting.",
        "Application.UseCases.Dungeons.",
        "Application.UseCases.Equipments.",
        "Application.UseCases.Essences.",
        "Application.UseCases.Guilds.",
        "Application.UseCases.Inventories.",
        "Application.UseCases.MarketPlaces.",
        "Application.UseCases.Prophecies.",
        "Application.UseCases.Quests.",
        "Application.UseCases.Raids.",
        "Application.UseCases.Soulstones.",
        "Application.UseCases.Titles."
    ];

    private static IReadOnlyDictionary<Type, StateSyncCommandScopeProfile> BuildProfiles()
    {
        var profiles = new Dictionary<Type, StateSyncCommandScopeProfile>();

        Register(profiles, [StateSyncScopes.Achievements], [],
            typeof(global::Application.UseCases.Achievements.Commands.RecalculateAchievements.RecalculateAchievementsCommand),
            typeof(global::Application.UseCases.Titles.Commands.EquipTitle.EquipTitleCommand),
            typeof(global::Application.UseCases.Titles.Commands.UnequipTitle.UnequipTitleCommand));

        Register(profiles, [], [], refreshCharacterOverview: false, inventoryWhenChanged: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.CharacterActions.Commands.DeleteCharacterAction.DeleteCharacterActionCommand),
            typeof(global::Application.UseCases.CharacterActions.Commands.ResolveCharacterAction.ResolveCharacterActionCommand),
            typeof(global::Application.UseCases.CharacterActions.Commands.ResumeTempering.ResumeTemperingCommand),
            typeof(global::Application.UseCases.CharacterActions.Commands.StartCombatAction.StartCombatActionCommand),
            typeof(global::Application.UseCases.CharacterActions.Commands.StartCraftingAction.StartCraftingActionCommand));

        Register(profiles, [StateSyncScopes.Inventory], [StateSyncScopes.Colosseum],
            typeof(global::Application.UseCases.Colosseum.Commands.BackfillChampionMarketTitleGrants.BackfillChampionMarketTitleGrantsCommand),
            typeof(global::Application.UseCases.Colosseum.Commands.PurchaseChampionMarketItem.PurchaseChampionMarketItemCommand),
            typeof(global::Application.UseCases.Colosseum.Commands.StartArenaBattle.StartArenaBattleCommand),
            typeof(global::Application.UseCases.Colosseum.Commands.UpdateArenaDefenseSnapshot.UpdateArenaDefenseSnapshotCommand));

        Register(profiles, [StateSyncScopes.Inventory, StateSyncScopes.Quests], [],
            typeof(global::Application.UseCases.Crafting.Commands.CraftItems.CraftItemsCommand),
            typeof(global::Application.UseCases.Crafting.Commands.LearnBlueprint.LearnBlueprintCommand));

        Register(profiles, [StateSyncScopes.Inventory], [],
            typeof(global::Application.UseCases.Professions.Commands.CancelTemperingQueue.CancelTemperingQueueCommand),
            typeof(global::Application.UseCases.Professions.Commands.RemoveCraftingQueueItem.RemoveCraftingQueueItemCommand));

        Register(profiles, [StateSyncScopes.Dungeons, StateSyncScopes.Inventory, StateSyncScopes.Quests], [],
            typeof(global::Application.UseCases.Dungeons.Commands.AssembleDungeonSigil.AssembleDungeonSigilCommand),
            typeof(global::Application.UseCases.Dungeons.Commands.ClaimDungeonRewards.ClaimDungeonRewardsCommand),
            typeof(global::Application.UseCases.Dungeons.Commands.DismissFailedDungeonRun.DismissFailedDungeonRunCommand),
            typeof(global::Application.UseCases.Dungeons.Commands.ExecuteDungeonAction.ExecuteDungeonActionCommand),
            typeof(global::Application.UseCases.Dungeons.Commands.StartDungeonRun.StartDungeonRunCommand));

        Register(profiles, [StateSyncScopes.Equipment, StateSyncScopes.Inventory, StateSyncScopes.Quests], [],
            typeof(global::Application.UseCases.Equipments.Commands.EquipEquipment.EquipEquipmentCommand),
            typeof(global::Application.UseCases.Equipments.Commands.UnequipEquipment.UnequipEquipmentCommand));

        Register(profiles, [StateSyncScopes.Essences, StateSyncScopes.Inventory, StateSyncScopes.Equipment, StateSyncScopes.Quests], [],
            typeof(global::Application.UseCases.Essences.Commands.AbsorbUnboundEssence.AbsorbUnboundEssenceCommand),
            typeof(global::Application.UseCases.Essences.Commands.ActivateEssenceLoadout.ActivateEssenceLoadoutCommand),
            typeof(global::Application.UseCases.Essences.Commands.AscendEssence.AscendEssenceCommand),
            typeof(global::Application.UseCases.Essences.Commands.DeleteEssenceLoadout.DeleteEssenceLoadoutCommand),
            typeof(global::Application.UseCases.Essences.Commands.DismantleUnboundEssence.DismantleUnboundEssenceCommand),
            typeof(global::Application.UseCases.Essences.Commands.EvolveEssence.EvolveEssenceCommand),
            typeof(global::Application.UseCases.Essences.Commands.FavoriteEssence.FavoriteEssenceCommand),
            typeof(global::Application.UseCases.Essences.Commands.SaveEssenceLoadout.SaveEssenceLoadoutCommand),
            typeof(global::Application.UseCases.Essences.Commands.SetEssenceFocus.SetEssenceFocusCommand));

        Register(profiles, [StateSyncScopes.Essences, StateSyncScopes.Inventory], [],
            refreshCharacterOverview: true, inventoryWhenChanged: false, refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Essences.Commands.SpendEssenceDust.SpendEssenceDustCommand));

        Register(profiles, [StateSyncScopes.Inventory, StateSyncScopes.Equipment], [StateSyncScopes.Guild],
            typeof(global::Application.UseCases.Guilds.Commands.AcceptInvite.AcceptInviteCommand),
            typeof(global::Application.UseCases.Guilds.Commands.ApplyToGuild.ApplyToGuildCommand),
            typeof(global::Application.UseCases.Guilds.Commands.ApproveApplication.ApproveApplicationCommand),
            typeof(global::Application.UseCases.Guilds.Commands.BorrowGuildVaultItem.BorrowGuildVaultItemCommand),
            typeof(global::Application.UseCases.Guilds.Commands.ChangeGuildMemberRole.ChangeGuildMemberRoleCommand),
            typeof(global::Application.UseCases.Guilds.Commands.ClaimGuildOrderReward.ClaimGuildOrderRewardCommand),
            typeof(global::Application.UseCases.Guilds.Commands.ClaimGuildWeeklyMissionReward.ClaimGuildWeeklyMissionRewardCommand),
            typeof(global::Application.UseCases.Guilds.Commands.ConstructGuildBuilding.ConstructGuildBuildingCommand),
            typeof(global::Application.UseCases.Guilds.Commands.CreateGuild.CreateGuildCommand),
            typeof(global::Application.UseCases.Guilds.Commands.DisbandGuild.DisbandGuildCommand),
            typeof(global::Application.UseCases.Guilds.Commands.DonateGuildVaultItem.DonateGuildVaultItemCommand),
            typeof(global::Application.UseCases.Guilds.Commands.Invite.InviteCommand),
            typeof(global::Application.UseCases.Guilds.Commands.InviteCharacterByName.InviteCharacterByNameCommand),
            typeof(global::Application.UseCases.Guilds.Commands.KickGuildMember.KickGuildMemberCommand),
            typeof(global::Application.UseCases.Guilds.Commands.LeaveGuild.LeaveGuildCommand),
            typeof(global::Application.UseCases.Guilds.Commands.PurchaseGuildShopItem.PurchaseGuildShopItemCommand),
            typeof(global::Application.UseCases.Guilds.Commands.RejectApplication.RejectApplicationCommand),
            typeof(global::Application.UseCases.Guilds.Commands.RejectInvite.RejectInviteCommand),
            typeof(global::Application.UseCases.Guilds.Commands.ReturnGuildVaultItem.ReturnGuildVaultItemCommand),
            typeof(global::Application.UseCases.Guilds.Commands.SelectGuildMission.SelectGuildMissionCommand),
            typeof(global::Application.UseCases.Guilds.Commands.SetGuildBuildingTarget.SetGuildBuildingTargetCommand),
            typeof(global::Application.UseCases.Guilds.Commands.UpdateGuildDescription.UpdateGuildDescriptionCommand),
            typeof(global::Application.UseCases.Guilds.Commands.UpdateGuildRolePermissions.UpdateGuildRolePermissionsCommand),
            typeof(global::Application.UseCases.Guilds.Commands.UpgradeGuildBuilding.UpgradeGuildBuildingCommand),
            typeof(global::Application.UseCases.Guilds.Commands.WithdrawGuildVaultItem.WithdrawGuildVaultItemCommand));

        Register(profiles, [StateSyncScopes.Inventory], [],
            typeof(global::Application.UseCases.Inventories.Commands.MarkInventoryItemSeen.MarkInventoryItemSeenCommand),
            typeof(global::Application.UseCases.Inventories.Commands.OpenCatalystSelectionCrate.OpenCatalystSelectionCrateCommand),
            typeof(global::Application.UseCases.Inventories.Commands.ScrapEquipments.ScrapEquipmentsCommand),
            typeof(global::Application.UseCases.Inventories.Commands.TransferInventoryItem.TransferInventoryItemCommand));

        Register(profiles, [StateSyncScopes.Inventory, StateSyncScopes.Equipment], [],
            typeof(global::Application.UseCases.Inventories.Commands.SetInventoryItemFavorite.SetInventoryItemFavoriteCommand));

        Register(profiles, [StateSyncScopes.Inventory], [StateSyncScopes.Marketplace],
            typeof(global::Application.UseCases.MarketPlaces.Commands.BuyCommodity.BuyCommodityCommand),
            typeof(global::Application.UseCases.MarketPlaces.Commands.BuyoutMarketPlaceListing.BuyoutMarketPlaceListingCommand),
            typeof(global::Application.UseCases.MarketPlaces.Commands.CancelMarketPlaceBuyOrder.CancelMarketPlaceBuyOrderCommand),
            typeof(global::Application.UseCases.MarketPlaces.Commands.CancelMarketPlaceListing.CancelMarketPlaceListingCommand),
            typeof(global::Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceBuyOrder.CreateMarketPlaceBuyOrderCommand),
            typeof(global::Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceListing.CreateMarketPlaceListingCommand),
            typeof(global::Application.UseCases.MarketPlaces.Commands.FulfillMarketPlaceBuyOrder.FulfillMarketPlaceBuyOrderCommand),
            typeof(global::Application.UseCases.MarketPlaces.Commands.SellCommodity.SellCommodityCommand));

        Register(profiles, [StateSyncScopes.Prophecies, StateSyncScopes.Inventory], [],
            typeof(global::Application.UseCases.Prophecies.Commands.AcceptProphecy.AcceptProphecyCommand),
            typeof(global::Application.UseCases.Prophecies.Commands.ClaimProphecy.ClaimProphecyCommand),
            typeof(global::Application.UseCases.Prophecies.Commands.ClaimWeeklyRevelationMilestone.ClaimWeeklyRevelationMilestoneCommand),
            typeof(global::Application.UseCases.Prophecies.Commands.GetPropheciesOverview.GetPropheciesOverviewCommand),
            typeof(global::Application.UseCases.Prophecies.Commands.OpenProphecyCache.OpenProphecyCacheCommand),
            typeof(global::Application.UseCases.Prophecies.Commands.RerollProphecy.RerollProphecyCommand));

        Register(profiles, [StateSyncScopes.EventQuests, StateSyncScopes.Inventory], [],
            typeof(global::Application.UseCases.Quests.Events.Commands.ClaimAllEventQuestMilestones.ClaimAllEventQuestMilestonesCommand),
            typeof(global::Application.UseCases.Quests.Events.Commands.ClaimEventQuestMilestone.ClaimEventQuestMilestoneCommand),
            typeof(global::Application.UseCases.Quests.Events.Commands.ClaimEventQuestReward.ClaimEventQuestRewardCommand));

        Register(profiles, [StateSyncScopes.Quests, StateSyncScopes.AreaAccess], [],
            typeof(global::Application.UseCases.Quests.Commands.AcknowledgeQuestWelcome.AcknowledgeQuestWelcomeCommand),
            typeof(global::Application.UseCases.Quests.Commands.PinQuest.PinQuestCommand),
            typeof(global::Application.UseCases.Quests.Commands.SelectQuestChoice.SelectQuestChoiceCommand),
            typeof(global::Application.UseCases.Quests.Commands.StartQuestEncounter.StartQuestEncounterCommand));

        Register(profiles, [], [StateSyncScopes.Raids],
            typeof(global::Application.UseCases.Raids.CreateRaidCommand),
            typeof(global::Application.UseCases.Raids.CreateDevelopmentRaidCommand),
            typeof(global::Application.UseCases.Raids.JoinRaidCommand),
            typeof(global::Application.UseCases.Raids.LeaveRaidCommand),
            typeof(global::Application.UseCases.Raids.CancelRaidCommand),
            typeof(global::Application.UseCases.Raids.TransferRaidLeadershipCommand),
            typeof(global::Application.UseCases.Raids.RefreshRaidSnapshotCommand),
            typeof(global::Application.UseCases.Raids.AssignRaidWingCommand),
            typeof(global::Application.UseCases.Raids.Commands.UpdateRaidParties.UpdateRaidPartiesCommand),
            typeof(global::Application.UseCases.Raids.CommenceRaidCommand));

        Register(profiles, [StateSyncScopes.Inventory], [StateSyncScopes.Raids],
            typeof(global::Application.UseCases.Raids.ClaimRaidRewardsCommand),
            typeof(global::Application.UseCases.Raids.PurchaseRaidTrophyVendorItemCommand));

        Register(profiles, [StateSyncScopes.Soulstones, StateSyncScopes.Inventory, StateSyncScopes.Quests], [],
            typeof(global::Application.UseCases.Soulstones.Commands.PurchaseSoulstoneUpgrade.PurchaseSoulstoneUpgradeCommand),
            typeof(global::Application.UseCases.Soulstones.Commands.ResetSoulstoneUpgrades.ResetSoulstoneUpgradesCommand));

        return profiles;
    }

    private static void Register(
        IDictionary<Type, StateSyncCommandScopeProfile> profiles,
        IReadOnlyList<string> characterScopes,
        IReadOnlyList<string> worldScopes,
        params Type[] commandTypes) =>
        Register(profiles, characterScopes, worldScopes, true, false, false, commandTypes);

    private static void Register(
        IDictionary<Type, StateSyncCommandScopeProfile> profiles,
        IReadOnlyList<string> characterScopes,
        IReadOnlyList<string> worldScopes,
        bool refreshCharacterOverview,
        bool inventoryWhenChanged,
        bool refreshCharacterSummaryWhenChanged,
        params Type[] commandTypes)
    {
        var profile = new StateSyncCommandScopeProfile(
            characterScopes,
            worldScopes,
            refreshCharacterOverview,
            inventoryWhenChanged,
            refreshCharacterSummaryWhenChanged);
        foreach (var commandType in commandTypes)
        {
            profiles.Add(commandType, profile);
        }
    }
}
