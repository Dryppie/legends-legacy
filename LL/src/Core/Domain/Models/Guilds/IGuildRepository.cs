namespace Domain.Models.Guilds;
public interface IGuildRepository
{
    Task CreateAsync(Guid characterId, string name, CancellationToken cancellationToken);
    Task InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken);
    Task AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken);
    Task LeaveGuildAsync(Guid guildId, Guid characterId, CancellationToken cancellationToken);
    Task<Guild?> GetMyGuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken);
    Task<GuildMember> GetGuildMember(Guid guildId, Guid currentCharacterId, CancellationToken cancellationToken);
    Task<List<GuildInvite>> GetMyInvitesAsync(Guid characterId, CancellationToken cancellationToken);
}