namespace Domain.Models.Guilds;
public interface IGuildRepository
{
    Task<bool> CreateAsync(Guid characterId, string name, CancellationToken cancellationToken);
    Task<bool> InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken);
    Task<bool> InviteCharacterByNameAsync(Guid currentCharacterId, Guid guildId, string invitedCharacterName, CancellationToken cancellationToken);
    Task<bool> AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken);
    Task<bool> LeaveGuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> DisbandGuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task<Guild?> GetMyGuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken);
    Task<GuildMember?> GetGuildMember(Guid currentCharacterId, CancellationToken cancellationToken);
    Task<List<GuildInvite>> GetMyInvitesAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> ApplyToGuildAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken);
    Task<bool> RejectGuildInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken);
    Task<bool> ApproveApplicationAsync(Guid guildId, Guid applicationCharacterId, CancellationToken cancellationToken);
    Task<Guild?> GetGuildForMemberAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> ChangeMemberRoleAsync(Guid guildId, Guid characterId, GuildRole role, CancellationToken cancellationToken);
    Task<bool> KickMemberAsync(Guid guildId, Guid characterId, CancellationToken cancellationToken);
    Task<bool> UpdateRolePermissionsAsync(Guid guildId, GuildRolePermission permissions, CancellationToken cancellationToken);
}
