using Application.WebSockets.Contracts;

namespace Application.MediatR.Synchronization;

public sealed record StateSyncCommandScopeProfile(
    IReadOnlyList<string> CharacterScopes,
    IReadOnlyList<string> WorldScopes,
    bool RefreshCharacterOverview = true,
    bool InventoryWhenChanged = false,
    bool RefreshCharacterSummaryWhenChanged = false)
{
    // Response-owned scopes still advance their persisted revision, but the
    // initiating client applies the authoritative mutation response instead of
    // receiving a StateInvalidated echo. Counterparties continue to invalidate.
    public IReadOnlySet<string> ResponseOwnedCharacterScopes { get; init; } =
        new HashSet<string>(StringComparer.Ordinal);
    public IReadOnlySet<string> ResponseOwnedWorldScopes { get; init; } =
        new HashSet<string>(StringComparer.Ordinal);
}

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
            typeof(global::Application.UseCases.Achievements.Commands.RecalculateAchievements.RecalculateAchievementsCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Achievements],
            [],
            [StateSyncScopes.Achievements, StateSyncScopes.Character],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Titles.Commands.EquipTitle.EquipTitleCommand),
            typeof(global::Application.UseCases.Titles.Commands.UnequipTitle.UnequipTitleCommand));

        Register(profiles, [], [], refreshCharacterOverview: false, inventoryWhenChanged: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.CharacterActions.Commands.DeleteCharacterAction.DeleteCharacterActionCommand),
            typeof(global::Application.UseCases.CharacterActions.Commands.ResolveCharacterAction.ResolveCharacterActionCommand),
            typeof(global::Application.UseCases.CharacterActions.Commands.ResumeTempering.ResumeTemperingCommand),
            typeof(global::Application.UseCases.CharacterActions.Commands.StartCombatAction.StartCombatActionCommand),
            typeof(global::Application.UseCases.CharacterActions.Commands.StartCraftingAction.StartCraftingActionCommand));

        Register(
            profiles,
            [],
            [],
            refreshCharacterOverview: false,
            inventoryWhenChanged: false,
            refreshCharacterSummaryWhenChanged: false,
            typeof(global::Application.UseCases.Colosseum.Commands.BackfillChampionMarketTitleGrants.BackfillChampionMarketTitleGrantsCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Colosseum, StateSyncScopes.Inventory],
            [],
            [StateSyncScopes.Colosseum],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Colosseum.Commands.PurchaseChampionMarketItem.PurchaseChampionMarketItemCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Colosseum],
            [],
            [StateSyncScopes.Colosseum, StateSyncScopes.Character],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: false,
            typeof(global::Application.UseCases.Colosseum.Commands.StartArenaBattle.StartArenaBattleCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Colosseum],
            [],
            [StateSyncScopes.Colosseum],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: false,
            typeof(global::Application.UseCases.Colosseum.Commands.UpdateArenaDefenseSnapshot.UpdateArenaDefenseSnapshotCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Inventory, StateSyncScopes.Quests],
            [],
            [StateSyncScopes.Inventory],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Crafting.Commands.CraftItems.CraftItemsCommand),
            typeof(global::Application.UseCases.Crafting.Commands.LearnBlueprint.LearnBlueprintCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Inventory],
            [],
            [StateSyncScopes.Inventory],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Professions.Commands.CancelTemperingQueue.CancelTemperingQueueCommand),
            typeof(global::Application.UseCases.Professions.Commands.RemoveCraftingQueueItem.RemoveCraftingQueueItemCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Dungeons, StateSyncScopes.Inventory, StateSyncScopes.Quests],
            [],
            [StateSyncScopes.Dungeons, StateSyncScopes.Inventory],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Dungeons.Commands.StartDungeonRun.StartDungeonRunCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Dungeons, StateSyncScopes.Quests],
            [],
            [StateSyncScopes.Dungeons],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Dungeons.Commands.ExecuteDungeonAction.ExecuteDungeonActionCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Dungeons],
            [],
            [StateSyncScopes.Dungeons],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Dungeons.Commands.DismissFailedDungeonRun.DismissFailedDungeonRunCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Dungeons, StateSyncScopes.Inventory],
            [],
            [StateSyncScopes.Dungeons, StateSyncScopes.Inventory, StateSyncScopes.Character],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: false,
            typeof(global::Application.UseCases.Dungeons.Commands.AssembleDungeonSigil.AssembleDungeonSigilCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Dungeons, StateSyncScopes.Inventory],
            [],
            [StateSyncScopes.Dungeons, StateSyncScopes.Inventory, StateSyncScopes.Character],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Dungeons.Commands.ClaimDungeonRewards.ClaimDungeonRewardsCommand));

        RegisterResponseOwned(profiles, [StateSyncScopes.Equipment, StateSyncScopes.Inventory], [],
            [StateSyncScopes.Equipment, StateSyncScopes.Inventory],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Equipments.Commands.EquipEquipment.EquipEquipmentCommand),
            typeof(global::Application.UseCases.Equipments.Commands.UnequipEquipment.UnequipEquipmentCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Essences, StateSyncScopes.Inventory, StateSyncScopes.Equipment],
            [],
            [StateSyncScopes.Essences, StateSyncScopes.Inventory, StateSyncScopes.Equipment],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Essences.Commands.AbsorbUnboundEssence.AbsorbUnboundEssenceCommand),
            typeof(global::Application.UseCases.Essences.Commands.AscendEssence.AscendEssenceCommand),
            typeof(global::Application.UseCases.Essences.Commands.DismantleUnboundEssence.DismantleUnboundEssenceCommand),
            typeof(global::Application.UseCases.Essences.Commands.EvolveEssence.EvolveEssenceCommand),
            typeof(global::Application.UseCases.Essences.Commands.SpendEssenceDust.SpendEssenceDustCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Essences],
            [],
            [StateSyncScopes.Essences],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Essences.Commands.FavoriteEssence.FavoriteEssenceCommand),
            typeof(global::Application.UseCases.Essences.Commands.SetEssenceFocus.SetEssenceFocusCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Essences],
            [],
            [StateSyncScopes.Essences],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Essences.Commands.ActivateEssenceLoadout.ActivateEssenceLoadoutCommand),
            typeof(global::Application.UseCases.Essences.Commands.DeleteEssenceLoadout.DeleteEssenceLoadoutCommand),
            typeof(global::Application.UseCases.Essences.Commands.SaveEssenceLoadout.SaveEssenceLoadoutCommand));

        RegisterSemanticWorldResponseOwned(
            profiles,
            [
                StateSyncScopes.Achievements,
                StateSyncScopes.GuildMembership,
                StateSyncScopes.GuildInvites
            ],
            [StateSyncScopes.Guild, StateSyncScopes.GuildDirectory],
            [],
            [StateSyncScopes.GuildDirectory],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: false,
            typeof(global::Application.UseCases.Guilds.Commands.AcceptInvite.AcceptInviteCommand),
            typeof(global::Application.UseCases.Guilds.Commands.ApproveApplication.ApproveApplicationCommand),
            typeof(global::Application.UseCases.Guilds.Commands.CreateGuild.CreateGuildCommand));

        RegisterSemanticWorldResponseOwned(
            profiles,
            [
                StateSyncScopes.Inventory,
                StateSyncScopes.Equipment,
                StateSyncScopes.GuildMembership,
                StateSyncScopes.GuildInvites
            ],
            [StateSyncScopes.Guild, StateSyncScopes.GuildDirectory],
            [],
            [StateSyncScopes.GuildDirectory],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: false,
            typeof(global::Application.UseCases.Guilds.Commands.DisbandGuild.DisbandGuildCommand),
            typeof(global::Application.UseCases.Guilds.Commands.KickGuildMember.KickGuildMemberCommand),
            typeof(global::Application.UseCases.Guilds.Commands.LeaveGuild.LeaveGuildCommand));

        Register(
            profiles,
            [StateSyncScopes.GuildInvites],
            [StateSyncScopes.Guild],
            refreshCharacterOverview: false,
            inventoryWhenChanged: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Guilds.Commands.ApplyToGuild.ApplyToGuildCommand),
            typeof(global::Application.UseCases.Guilds.Commands.Invite.InviteCommand),
            typeof(global::Application.UseCases.Guilds.Commands.InviteCharacterByName.InviteCharacterByNameCommand),
            typeof(global::Application.UseCases.Guilds.Commands.RejectApplication.RejectApplicationCommand),
            typeof(global::Application.UseCases.Guilds.Commands.RejectInvite.RejectInviteCommand));

        Register(
            profiles,
            [StateSyncScopes.Inventory],
            [StateSyncScopes.Guild],
            refreshCharacterOverview: false,
            inventoryWhenChanged: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Guilds.Commands.BorrowGuildVaultItem.BorrowGuildVaultItemCommand),
            typeof(global::Application.UseCases.Guilds.Commands.DonateGuildVaultItem.DonateGuildVaultItemCommand));

        Register(
            profiles,
            [StateSyncScopes.Inventory, StateSyncScopes.Equipment],
            [StateSyncScopes.Guild],
            refreshCharacterOverview: false,
            inventoryWhenChanged: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Guilds.Commands.ReturnGuildVaultItem.ReturnGuildVaultItemCommand),
            typeof(global::Application.UseCases.Guilds.Commands.WithdrawGuildVaultItem.WithdrawGuildVaultItemCommand));

        Register(
            profiles,
            [],
            [StateSyncScopes.Guild],
            refreshCharacterOverview: false,
            inventoryWhenChanged: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Guilds.Commands.ChangeGuildMemberRole.ChangeGuildMemberRoleCommand),
            typeof(global::Application.UseCases.Guilds.Commands.UpdateGuildDescription.UpdateGuildDescriptionCommand),
            typeof(global::Application.UseCases.Guilds.Commands.UpdateGuildRolePermissions.UpdateGuildRolePermissionsCommand));

        RegisterSemanticWorldResponseOwned(
            profiles,
            [StateSyncScopes.GuildShop],
            [StateSyncScopes.Guild, StateSyncScopes.GuildBuildings],
            [],
            [StateSyncScopes.GuildBuildings],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Guilds.Commands.ConstructGuildBuilding.ConstructGuildBuildingCommand),
            typeof(global::Application.UseCases.Guilds.Commands.UpgradeGuildBuilding.UpgradeGuildBuildingCommand));

        RegisterSemanticWorldResponseOwned(
            profiles,
            [],
            [StateSyncScopes.GuildBuildings],
            [],
            [StateSyncScopes.GuildBuildings],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Guilds.Commands.SetGuildBuildingTarget.SetGuildBuildingTargetCommand));

        RegisterSemanticWorldResponseOwned(
            profiles,
            [],
            [StateSyncScopes.GuildMissions],
            [],
            [StateSyncScopes.GuildMissions],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Guilds.Commands.SelectGuildMission.SelectGuildMissionCommand));

        RegisterSemanticWorldResponseOwned(
            profiles,
            [StateSyncScopes.GuildShop],
            [StateSyncScopes.Guild, StateSyncScopes.GuildMissions],
            [],
            [StateSyncScopes.GuildMissions],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Guilds.Commands.ClaimGuildOrderReward.ClaimGuildOrderRewardCommand),
            typeof(global::Application.UseCases.Guilds.Commands.ClaimGuildWeeklyMissionReward.ClaimGuildWeeklyMissionRewardCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Inventory, StateSyncScopes.Achievements, StateSyncScopes.GuildShop],
            [],
            [StateSyncScopes.Inventory, StateSyncScopes.GuildShop],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Guilds.Commands.PurchaseGuildShopItem.PurchaseGuildShopItemCommand));

        RegisterResponseOwned(profiles, [StateSyncScopes.Inventory], [],
            [StateSyncScopes.Inventory],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Inventories.Commands.MarkInventoryItemSeen.MarkInventoryItemSeenCommand),
            typeof(global::Application.UseCases.Inventories.Commands.OpenCatalystSelectionCrate.OpenCatalystSelectionCrateCommand),
            typeof(global::Application.UseCases.Inventories.Commands.ScrapEquipments.ScrapEquipmentsCommand),
            typeof(global::Application.UseCases.Inventories.Commands.TransferInventoryItem.TransferInventoryItemCommand));

        RegisterResponseOwned(profiles, [StateSyncScopes.Inventory, StateSyncScopes.Equipment], [],
            [StateSyncScopes.Inventory],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Inventories.Commands.SetInventoryItemFavorite.SetInventoryItemFavoriteCommand));

        RegisterSemanticWorldResponseOwned(
            profiles,
            [StateSyncScopes.Inventory],
            [StateSyncScopes.Marketplace],
            [StateSyncScopes.Inventory, StateSyncScopes.Character],
            [StateSyncScopes.Marketplace],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.MarketPlaces.Commands.BuyoutMarketPlaceListing.BuyoutMarketPlaceListingCommand),
            typeof(global::Application.UseCases.MarketPlaces.Commands.CancelMarketPlaceListing.CancelMarketPlaceListingCommand),
            typeof(global::Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceListing.CreateMarketPlaceListingCommand),
            typeof(global::Application.UseCases.MarketPlaces.Commands.FulfillMarketPlaceBuyOrder.FulfillMarketPlaceBuyOrderCommand),
            typeof(global::Application.UseCases.MarketPlaces.Commands.SellCommodity.SellCommodityCommand));

        RegisterSemanticWorldResponseOwned(
            profiles,
            [StateSyncScopes.Inventory],
            [StateSyncScopes.Marketplace],
            [StateSyncScopes.Character],
            [StateSyncScopes.Marketplace],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.MarketPlaces.Commands.BuyCommodity.BuyCommodityCommand),
            typeof(global::Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceBuyOrder.CreateMarketPlaceBuyOrderCommand));

        RegisterSemanticWorldResponseOwned(
            profiles,
            [],
            [StateSyncScopes.Marketplace],
            [StateSyncScopes.Character],
            [StateSyncScopes.Marketplace],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.MarketPlaces.Commands.CancelMarketPlaceBuyOrder.CancelMarketPlaceBuyOrderCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Prophecies, StateSyncScopes.Inventory],
            [],
            [StateSyncScopes.Prophecies],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Prophecies.Commands.AcceptProphecy.AcceptProphecyCommand),
            typeof(global::Application.UseCases.Prophecies.Commands.ClaimProphecy.ClaimProphecyCommand),
            typeof(global::Application.UseCases.Prophecies.Commands.ClaimWeeklyRevelationMilestone.ClaimWeeklyRevelationMilestoneCommand),
            typeof(global::Application.UseCases.Prophecies.Commands.OpenProphecyCache.OpenProphecyCacheCommand),
            typeof(global::Application.UseCases.Prophecies.Commands.RerollProphecy.RerollProphecyCommand));

        Register(profiles, [StateSyncScopes.Prophecies, StateSyncScopes.Inventory], [],
            typeof(global::Application.UseCases.Prophecies.Commands.GetPropheciesOverview.GetPropheciesOverviewCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.EventQuests, StateSyncScopes.Inventory],
            [],
            [StateSyncScopes.EventQuests],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Quests.Events.Commands.ClaimAllEventQuestMilestones.ClaimAllEventQuestMilestonesCommand),
            typeof(global::Application.UseCases.Quests.Events.Commands.ClaimEventQuestMilestone.ClaimEventQuestMilestoneCommand),
            typeof(global::Application.UseCases.Quests.Events.Commands.ClaimEventQuestReward.ClaimEventQuestRewardCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Quests],
            [],
            [StateSyncScopes.Quests],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: false,
            typeof(global::Application.UseCases.Quests.Commands.AcknowledgeQuestWelcome.AcknowledgeQuestWelcomeCommand),
            typeof(global::Application.UseCases.Quests.Commands.PinQuest.PinQuestCommand),
            typeof(global::Application.UseCases.Quests.Commands.SelectQuestChoice.SelectQuestChoiceCommand));

        Register(profiles, [StateSyncScopes.Quests, StateSyncScopes.AreaAccess], [],
            typeof(global::Application.UseCases.Quests.Commands.StartQuestEncounter.StartQuestEncounterCommand));

        RegisterSemanticWorldResponseOwned(
            profiles,
            [StateSyncScopes.Raids],
            [StateSyncScopes.RaidDirectory],
            [StateSyncScopes.Raids],
            [StateSyncScopes.RaidDirectory],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Raids.CreateRaidCommand),
            typeof(global::Application.UseCases.Raids.CreateDevelopmentRaidCommand),
            typeof(global::Application.UseCases.Raids.FillDevelopmentRaidTeamCommand),
            typeof(global::Application.UseCases.Raids.JoinRaidCommand),
            typeof(global::Application.UseCases.Raids.ApproveRaidSignupCommand),
            typeof(global::Application.UseCases.Raids.RemoveRaidSignupCommand),
            typeof(global::Application.UseCases.Raids.LeaveRaidCommand),
            typeof(global::Application.UseCases.Raids.CancelRaidCommand),
            typeof(global::Application.UseCases.Raids.TransferRaidLeadershipCommand),
            typeof(global::Application.UseCases.Raids.AssignRaidWingCommand),
            typeof(global::Application.UseCases.Raids.Commands.UpdateRaidParties.UpdateRaidPartiesCommand),
            typeof(global::Application.UseCases.Raids.CommenceRaidCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Raids],
            [],
            [StateSyncScopes.Raids],
            refreshCharacterOverview: false,
            refreshCharacterSummaryWhenChanged: true,
            typeof(global::Application.UseCases.Raids.RefreshRaidSnapshotCommand));

        Register(profiles, [StateSyncScopes.Raids, StateSyncScopes.Inventory], [],
            typeof(global::Application.UseCases.Raids.ClaimRaidRewardsCommand));

        Register(profiles, [StateSyncScopes.Raids, StateSyncScopes.Inventory], [],
            typeof(global::Application.UseCases.Raids.PurchaseRaidTrophyVendorItemCommand));

        RegisterResponseOwned(
            profiles,
            [StateSyncScopes.Soulstones, StateSyncScopes.Inventory, StateSyncScopes.Quests],
            [],
            [StateSyncScopes.Soulstones, StateSyncScopes.Character],
            refreshCharacterOverview: true,
            refreshCharacterSummaryWhenChanged: true,
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

    private static void RegisterResponseOwned(
        IDictionary<Type, StateSyncCommandScopeProfile> profiles,
        IReadOnlyList<string> characterScopes,
        IReadOnlyList<string> worldScopes,
        IReadOnlyList<string> responseOwnedCharacterScopes,
        bool refreshCharacterOverview,
        bool refreshCharacterSummaryWhenChanged,
        params Type[] commandTypes)
    {
        var profile = new StateSyncCommandScopeProfile(
            characterScopes,
            worldScopes,
            refreshCharacterOverview,
            InventoryWhenChanged: false,
            refreshCharacterSummaryWhenChanged)
        {
            ResponseOwnedCharacterScopes = responseOwnedCharacterScopes.ToHashSet(StringComparer.Ordinal)
        };
        foreach (var commandType in commandTypes)
        {
            profiles.Add(commandType, profile);
        }
    }

    private static void RegisterSemanticWorldResponseOwned(
        IDictionary<Type, StateSyncCommandScopeProfile> profiles,
        IReadOnlyList<string> characterScopes,
        IReadOnlyList<string> worldScopes,
        IReadOnlyList<string> responseOwnedCharacterScopes,
        IReadOnlyList<string> responseOwnedWorldScopes,
        bool refreshCharacterOverview,
        bool refreshCharacterSummaryWhenChanged,
        params Type[] commandTypes)
    {
        var profile = new StateSyncCommandScopeProfile(
            characterScopes,
            worldScopes,
            refreshCharacterOverview,
            InventoryWhenChanged: false,
            refreshCharacterSummaryWhenChanged)
        {
            ResponseOwnedCharacterScopes = responseOwnedCharacterScopes.ToHashSet(StringComparer.Ordinal),
            ResponseOwnedWorldScopes = responseOwnedWorldScopes.ToHashSet(StringComparer.Ordinal)
        };
        foreach (var commandType in commandTypes)
        {
            profiles.Add(commandType, profile);
        }
    }
}
