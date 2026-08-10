namespace Domain.Models.Guilds;

public class GuildRolePermission
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public GuildRole Role { get; set; }
    public bool CanInvite { get; set; }
    public bool CanManageApplications { get; set; }
    public bool CanPromoteDemote { get; set; }
    public bool CanKick { get; set; }
    public bool CanBorrowVault { get; set; }
    public bool CanWithdrawVault { get; set; }

    public static GuildRolePermission CreateDefault(Guid guildId, GuildRole role) => role switch
    {
        GuildRole.Leader => new GuildRolePermission
        {
            GuildId = guildId,
            Role = role,
            CanInvite = true,
            CanManageApplications = true,
            CanPromoteDemote = true,
            CanKick = true,
            CanBorrowVault = true,
            CanWithdrawVault = true
        },
        GuildRole.Officer => new GuildRolePermission
        {
            GuildId = guildId,
            Role = role,
            CanInvite = true,
            CanManageApplications = true,
            CanBorrowVault = true,
            CanWithdrawVault = false
        },
        _ => new GuildRolePermission
        {
            GuildId = guildId,
            Role = role,
            CanBorrowVault = true,
            CanWithdrawVault = false
        }
    };
}
