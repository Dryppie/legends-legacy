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
    public async Task CreateAsync(Guid characterId, string name, CancellationToken cancellationToken)
    {
        await _guildRepository.CreateAsync(characterId, name, cancellationToken);
    }

    public async Task<Guild?> GetMyGuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _guildRepository.GetMyGuildAsync(characterId, cancellationToken);
    }

    public async Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken)
    {
        return await _guildRepository.GetAllGuildsAsync(cancellationToken);
    }

    public async Task LeaveGuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        await _guildRepository.LeaveGuildAsync(characterId, cancellationToken);
    }

    public async Task DisbandGuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(characterId, cancellationToken);
        if (!requestingMember.IsGuildLeader()) throw new UnauthorizedAccessException("You do not have permission to invite members to this guild.");
        await _guildRepository.DisbandGuildAsync(characterId, cancellationToken);
    }
    #endregion

    #region invites
    public async Task InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(currentCharacterId, cancellationToken);
        if (!requestingMember.HasInvitePermissions()) throw new UnauthorizedAccessException("You do not have permission to invite members to this guild.");

        await _guildRepository.InviteAsync(currentCharacterId, guildId, invitedCharacterId, cancellationToken);
    }

    public async Task InviteCharacterByNameAsync(Guid currentCharacterId, Guid guildId, string invitedCharacterName, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(currentCharacterId, cancellationToken);
        if (!requestingMember.HasInvitePermissions()) throw new UnauthorizedAccessException("You do not have permission to invite members to this guild.");

        await _guildRepository.InviteCharacterByNameAsync(currentCharacterId, guildId, invitedCharacterName, cancellationToken);
    }

    public async Task AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken)
    {
        await _guildRepository.AcceptInviteAsync(characterId, guildId, cancellationToken);
    }

    public async Task<List<GuildInvite>> GetMyInvitesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _guildRepository.GetMyInvitesAsync(characterId, cancellationToken);
    }

    public async Task ApplyToGuildAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken)
    {
        await _guildRepository.ApplyToGuildAsync(characterId, guildId, cancellationToken);
    }

    public async Task RejectApplicationAsync(Guid characterId, Guid applicationCharacterId, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(characterId, cancellationToken);
        if (!requestingMember.HasInvitePermissions()) throw new UnauthorizedAccessException("You do not have permission to invite members to this guild.");

        await _guildRepository.RejectApplicationAsync(requestingMember.GuildId, applicationCharacterId, cancellationToken);
    }

    public async Task ApproveApplicationAsync(Guid characterId, Guid applicationCharacterId, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(characterId, cancellationToken);
        if (!requestingMember.HasInvitePermissions()) throw new UnauthorizedAccessException("You do not have permission to invite members to this guild.");

        await _guildRepository.ApproveApplicationAsync(requestingMember.GuildId, applicationCharacterId, cancellationToken);
    }

    public async Task RejectInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken)
    {
        await _guildRepository.RejectInviteAsync(characterId, guildId, cancellationToken);
    }
    #endregion
}