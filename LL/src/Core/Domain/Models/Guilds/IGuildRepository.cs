namespace Domain.Models.Guilds;
public interface IGuildRepository
{
    Task CreateAsync(Guid characterId, string name, CancellationToken cancellationToken);
    Task InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken);
    Task InviteCharacterByNameAsync(Guid currentCharacterId, Guid guildId, string invitedCharacterName, CancellationToken cancellationToken);
    Task AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken);
    Task LeaveGuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task DisbandGuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task<Guild?> GetMyGuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken);
    Task<GuildMember> GetGuildMember(Guid currentCharacterId, CancellationToken cancellationToken);
    Task<List<GuildInvite>> GetMyInvitesAsync(Guid characterId, CancellationToken cancellationToken);
    Task ApplyToGuildAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken);
    Task RejectInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken);
    Task ApproveApplicationAsync(Guid guildId, Guid applicationCharacterId, CancellationToken cancellationToken);
    Task RejectApplicationAsync(Guid guildId, Guid applicationCharacterId, CancellationToken cancellationToken);
}