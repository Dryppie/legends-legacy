namespace Domain.Models.Guilds;
public interface IGuildRepository
{
    Task CreateAsync(Guid characterId, string name, CancellationToken cancellationToken);
    Task InviteAsync(Guid guildId, Guid targetCharacterId, CancellationToken cancellationToken);
    Task AcceptInviteAsync(Guid guildId, Guid characterId, CancellationToken cancellationToken);
    Task LeaveGuildAsync(Guid guildId, Guid characterId, CancellationToken cancellationToken);
    Task<Guild?> GetMyGuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken);
}