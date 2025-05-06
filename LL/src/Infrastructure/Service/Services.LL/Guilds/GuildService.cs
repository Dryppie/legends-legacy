using Application.Interfaces.Services.LL;
using Domain.Extensions.Guilds;
using Domain.Models.Guilds;

namespace Services.LL.Guilds;
public class GuildService : IGuildService
{
    private readonly IGuildRepository _guildRepository;

    public GuildService(IGuildRepository guildRepository)
    {
        _guildRepository = guildRepository;
    }

    #region guild
    public async Task<bool> CreateAsync(Guid characterId, string name, CancellationToken cancellationToken) => 
        await _guildRepository.CreateAsync(characterId, name, cancellationToken);

    public async Task<Guild?> GetMyGuildAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _guildRepository.GetMyGuildAsync(characterId, cancellationToken);

    public async Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken) =>
        await _guildRepository.GetAllGuildsAsync(cancellationToken);

    public async Task<bool> LeaveGuildAsync(Guid characterId, CancellationToken cancellationToken) => 
        await _guildRepository.LeaveGuildAsync(characterId, cancellationToken);

    public async Task<bool> DisbandGuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(characterId, cancellationToken);
        if (requestingMember == null || !requestingMember.IsGuildLeader()) return false;
        return await _guildRepository.DisbandGuildAsync(characterId, cancellationToken);
    }
    #endregion

    #region invites
    public async Task<bool> InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(currentCharacterId, cancellationToken);
        if (requestingMember == null || !requestingMember.HasInvitePermissions()) return false;

        return await _guildRepository.InviteAsync(currentCharacterId, guildId, invitedCharacterId, cancellationToken);
    }

    public async Task<bool> InviteCharacterByNameAsync(Guid currentCharacterId, Guid guildId, string invitedCharacterName, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(currentCharacterId, cancellationToken);
        if (requestingMember == null || !requestingMember.HasInvitePermissions()) return false;

        return await _guildRepository.InviteCharacterByNameAsync(currentCharacterId, guildId, invitedCharacterName, cancellationToken);
    }

    public async Task<bool> AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken) => 
        await _guildRepository.AcceptInviteAsync(characterId, guildId, cancellationToken);

    public async Task<List<GuildInvite>> GetMyInvitesAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _guildRepository.GetMyInvitesAsync(characterId, cancellationToken);

    public async Task<bool> ApplyToGuildAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken) => 
        await _guildRepository.ApplyToGuildAsync(characterId, guildId, cancellationToken);

    public async Task<bool> RejectApplicationAsync(Guid characterId, Guid applicationCharacterId, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(characterId, cancellationToken);
        if (requestingMember == null || !requestingMember.HasInvitePermissions()) return false;

        return await _guildRepository.RejectApplicationAsync(requestingMember.GuildId, applicationCharacterId, cancellationToken);
    }

    public async Task<bool> ApproveApplicationAsync(Guid characterId, Guid applicationCharacterId, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(characterId, cancellationToken);
        if (requestingMember == null || !requestingMember.HasInvitePermissions()) return false;

        return await _guildRepository.ApproveApplicationAsync(requestingMember.GuildId, applicationCharacterId, cancellationToken);
    }

    public async Task<bool> RejectInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken) => 
        await _guildRepository.RejectInviteAsync(characterId, guildId, cancellationToken);
    #endregion
}