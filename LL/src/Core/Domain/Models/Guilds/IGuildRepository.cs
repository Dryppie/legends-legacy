namespace Domain.Models.Guilds;
public interface IGuildRepository
{
    Task CreateAsync(Guid ownerCharacterId, Guild guild, CancellationToken cancellationToken);
    Task InviteAsync(Guid guildId, Guid targetCharacterId, CancellationToken cancellationToken);
    Task AcceptInviteAsync(Guid guildId, Guid characterId, CancellationToken cancellationToken);
    Task LeaveGuildAsync(Guid guildId, Guid characterId, CancellationToken cancellationToken);
    Task<Guild> GetAsync(Guid guildId, CancellationToken cancellationToken);
    Task<List<Guild>> GetGuildsAsync(CancellationToken cancellationToken);
}