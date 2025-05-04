using Domain.Models.Guilds;

namespace Application.Interfaces.Services.LL;
public interface IGuildService
{
    Task CreateAsync(Guid characterId, string name, CancellationToken cancellationToken);
    Task InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken);
    Task InviteCharacterByNameAsync(Guid currentCharacterId, Guid guildId, string invitedCharacterName, CancellationToken cancellationToken);
    Task AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken);
    Task LeaveGuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task DisbandGuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task<Guild?> GetMyGuildAsync(Guid guildId, CancellationToken cancellationToken);
    Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken);
    Task<List<GuildInvite>> GetMyInvitesAsync(Guid characterId, CancellationToken cancellationToken);
    Task ApplyToGuildAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken);
    Task RejectInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken);
    Task ApproveApplicationAsync(Guid characterId, Guid applicationCharacterId, CancellationToken cancellationToken);
    Task RejectApplicationAsync(Guid characterId, Guid applicationCharacterId, CancellationToken cancellationToken);
}