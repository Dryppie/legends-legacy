using Domain.Models.Guilds;

namespace Application.Interfaces.Services.LL;
public interface IGuildService
{
    Task<bool> CreateAsync(Guid characterId, string name, CancellationToken cancellationToken);

    /// <summary>
    /// This is seen when navigating to a character's profile
    /// </summary>
    /// <param name="currentCharacterId"></param>
    /// <param name="guildId"></param>
    /// <param name="invitedCharacterId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken);

    /// <summary>
    /// This is seen on the guild page, when wanting to invite someone by name
    /// </summary>
    /// <param name="currentCharacterId"></param>
    /// <param name="guildId"></param>
    /// <param name="invitedCharacterName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
    Task<Guild?> GetGuildWithUpgradesAsync(Guid characterId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<bool> DonateToGuildAsync(Guid characterId, Dictionary<GuildResourceType, int> donations, CancellationToken cancellationToken);
}