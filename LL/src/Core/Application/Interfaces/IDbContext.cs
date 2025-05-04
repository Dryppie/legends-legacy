using Domain.Models.Abilities;
using Domain.Models.Attributes;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Colosseum;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;
using Domain.Models.GatheringNodes;
using Domain.Models.Guilds;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Items.EssenceItems;
using Domain.Models.LootTables;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Application.Common.Interfaces;
public interface IDbContext
{
    DbSet<AbilityId> AbilityIds { get; }
    //DbSet<Achievement> Achievements { get; }
    DbSet<Area> Areas { get; }
    DbSet<EntityAttribute> EntityAttributes { get; }
    //DbSet<Building> Buildings { get; }
    DbSet<ArenaTicketStatus> ArenaTicketStatus { get; }
    DbSet<ColosseumMatchResult> ColosseumMatches { get; }
    DbSet<Character> Characters { get; }
    DbSet<Creature> Creatures { get; }
    //DbSet<Echo> Echoes { get; }
    DbSet<Entity> Entities { get; }
    DbSet<EquipmentSlot> EquipmentSlots { get; }
    DbSet<Essence> Essences { get; }
    DbSet<EssenceSlot> EssenceSlots { get; }
    DbSet<EssenceItemBase> EssenceItems { get; }
    // Effects
    //DbSet<Modifier> Modifiers { get; }

    // Player Actions
    DbSet<CharacterAction> CharacterActions { get; }
    DbSet<ActionDetails> ActionDetails { get; }

    DbSet<GatheringNode> GatheringNodes { get; }

    //DbSet<Equipment> Equipments { get; }
    //DbSet<Essence> Essences { get; }
    DbSet<Guild> Guilds { get; }
    DbSet<GuildInvite> GuildInvites { get; }
    DbSet<GuildMember> GuildMembers { get; }
    DbSet<Inventory> Inventories { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<ItemBase> ItemBases { get; }
    DbSet<ItemInstance> ItemInstances { get; }
    DbSet<LootTable> LootTables { get; }
    DbSet<LootTableItem> LootTableItems { get; }
    //DbSet<Party> Parties { get; }
    //DbSet<PartyMember> PartyMembers { get; }
    //DbSet<Profession> Professions { get; }
    //DbSet<Quest> Quests { get; }
    //DbSet<QuestStage> QuestStages { get; }
    //DbSet<Stat> Stats { get; }
    //DbSet<Title> Titles { get; }
    //DbSet<Town> Towns { get; }
    //DbSet<TownBuilding> TownBuildings { get; }
    DbSet<Region> Regions { get; }
    DbSet<AppUser> Users { get; }
    DbSet<ExternalLogin> ExternalLogins { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Execute raw sql. Never use string interpolation to embed values as this can cause sql injection
    /// Instead parse extra args as sqlParams
    /// </summary>
    /// <param name="sql"></param>
    /// <param name="token"></param>
    /// <param name="sqlParams"></param>
    /// <returns></returns>
    Task<int> ExecuteSqlRawAsync(string sql, CancellationToken token = default, params object[] sqlParams);

    /// <summary>
    /// Exposes EF Core's Entry method to allow property state manipulation.
    /// </summary>
    EntityEntry<TEntity> GetEntry<TEntity>(TEntity entity) where TEntity : class;
}
