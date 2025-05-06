using Domain.Models.Guilds;

namespace Application.Interfaces.Services.LL;
public interface IGuildService
{
    Task<bool> CreateAsync(Guid characterId, string name, CancellationToken cancellationToken);
    Task<bool> InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken);
    Task<bool> InviteCharacterByNameAsync(Guid currentCharacterId, Guid guildId, string invitedCharacterName, CancellationToken cancellationToken);
    Task<bool> AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken);
    Task<bool> LeaveGuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> DisbandGuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task<Guild?> GetMyGuildAsync(Guid guildId, CancellationToken cancellationToken);
    Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken);
    Task<List<GuildInvite>> GetMyInvitesAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> ApplyToGuildAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken);
    Task<bool> RejectInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken);
    Task<bool> ApproveApplicationAsync(Guid characterId, Guid applicationCharacterId, CancellationToken cancellationToken);
    Task<bool> RejectApplicationAsync(Guid characterId, Guid applicationCharacterId, CancellationToken cancellationToken);
}