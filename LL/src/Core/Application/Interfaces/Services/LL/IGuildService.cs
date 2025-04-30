using Domain.Models.Guilds;

namespace Application.Interfaces.Services.LL;
public interface IGuildService
{
    Task CreateAsync(Guid characterId, string name, CancellationToken cancellationToken);
    Task InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken);
    Task AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken);
    Task LeaveGuildAsync(Guid guildId, Guid characterId, CancellationToken cancellationToken);
    Task<Guild?> GetMyGuildAsync(Guid guildId, CancellationToken cancellationToken);
    Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken);
    Task<List<GuildInvite>> GetMyInvitesAsync(Guid characterId, CancellationToken cancellationToken);
}