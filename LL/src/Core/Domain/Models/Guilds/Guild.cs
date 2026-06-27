using Domain.Models.Entities.Characters;
using Domain.Models.Guilds.Buildings;
using Domain.Models.Guilds.Missions;
using Domain.Models.Guilds.Shop;

namespace Domain.Models.Guilds;
public class Guild
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxMembers { get; set; } = 10;
    public long GuildXp { get; set; }
    public int GuildLevel { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid OwnerId { get; set; }
    public Character Owner { get; set; } = null!;
    public ICollection<GuildResource> Resources { get; set; } = [];
    public ICollection<GuildBuilding> Buildings { get; set; } = [];
    public ICollection<GuildActivityLog> ActivityLogs { get; set; } = [];
    public ICollection<GuildMember> Members { get; set; } = [];
    public ICollection<GuildInvite> Invites { get; set; } = [];
    public ICollection<GuildMissionOption> MissionOptions { get; set; } = [];
    public ICollection<GuildMissionInstance> MissionInstances { get; set; } = [];
    public ICollection<PersonalGuildOrder> PersonalGuildOrders { get; set; } = [];
    public ICollection<GuildShopPurchase> ShopPurchases { get; set; } = [];
}
