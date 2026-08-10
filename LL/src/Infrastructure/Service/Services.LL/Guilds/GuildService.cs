using Application.Interfaces.Services.LL;
using Domain.Extensions.Guilds;
using Application.Interfaces.Services.LL.Achievements;
using Domain.Models.Guilds;

namespace Services.LL.Guilds;
public class GuildService : IGuildService
{
    private const int MinimumGuildNameLength = 3;
    private readonly IGuildRepository _guildRepository;
    private readonly IAchievementService? _achievementService;

    public GuildService(IGuildRepository guildRepository, IAchievementService? achievementService = null)
    {
        _guildRepository = guildRepository;
        _achievementService = achievementService;
    }

    #region guild
    public async Task<bool> CreateAsync(Guid characterId, string name, CancellationToken cancellationToken)
    {
        var normalizedName = name?.Trim();
        if (string.IsNullOrEmpty(normalizedName) || normalizedName.Length < MinimumGuildNameLength)
            return false;

        var created = await _guildRepository.CreateAsync(characterId, normalizedName, cancellationToken);
        if (created && _achievementService is not null)
        {
            await _achievementService.RecordGuildJoinedAsync(characterId, cancellationToken);
        }

        return created;
    }

    public async Task<Guild?> GetMyGuildAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _guildRepository.GetMyGuildAsync(characterId, cancellationToken);

    public async Task<Guild?> GetGuildForMemberAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _guildRepository.GetGuildForMemberAsync(characterId, cancellationToken);

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
        if (requestingMember == null || !requestingMember.Guild.PermissionsFor(requestingMember.Role).CanInvite) return false;
        if (requestingMember.GuildId != guildId) return false;

        return await _guildRepository.InviteAsync(currentCharacterId, guildId, invitedCharacterId, cancellationToken);
    }

    public async Task<bool> InviteCharacterByNameAsync(Guid currentCharacterId, Guid guildId, string invitedCharacterName, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(currentCharacterId, cancellationToken);
        if (requestingMember == null || !requestingMember.Guild.PermissionsFor(requestingMember.Role).CanInvite) return false;
        if (requestingMember.GuildId != guildId) return false;

        return await _guildRepository.InviteCharacterByNameAsync(currentCharacterId, guildId, invitedCharacterName, cancellationToken);
    }

    public async Task<bool> AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken)
    {
        var accepted = await _guildRepository.AcceptInviteAsync(characterId, guildId, cancellationToken);
        if (accepted && _achievementService is not null)
        {
            await _achievementService.RecordGuildJoinedAsync(characterId, cancellationToken);
        }

        return accepted;
    }

    public async Task<List<GuildInvite>> GetMyInvitesAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _guildRepository.GetMyInvitesAsync(characterId, cancellationToken);

    public async Task<bool> ApplyToGuildAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken) => 
        await _guildRepository.ApplyToGuildAsync(characterId, guildId, cancellationToken);

    public async Task<bool> RejectApplicationAsync(Guid characterId, Guid applicationCharacterId, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(characterId, cancellationToken);
        if (requestingMember == null || !requestingMember.Guild.PermissionsFor(requestingMember.Role).CanManageApplications) return false;

        return await _guildRepository.RejectGuildInviteAsync(applicationCharacterId, requestingMember.GuildId, cancellationToken);
    }

    public async Task<bool> ApproveApplicationAsync(Guid characterId, Guid applicationCharacterId, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(characterId, cancellationToken);
        if (requestingMember == null || !requestingMember.Guild.PermissionsFor(requestingMember.Role).CanManageApplications) return false;

        var approved = await _guildRepository.ApproveApplicationAsync(requestingMember.GuildId, applicationCharacterId, cancellationToken);
        if (approved && _achievementService is not null)
        {
            await _achievementService.RecordGuildJoinedAsync(applicationCharacterId, cancellationToken);
        }

        return approved;
    }

    public async Task<bool> RejectInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken) => 
        await _guildRepository.RejectGuildInviteAsync(characterId, guildId, cancellationToken);
    #endregion

    public async Task<bool> ChangeMemberRoleAsync(Guid characterId, Guid targetCharacterId, GuildRole role, CancellationToken cancellationToken)
    {
        if (role == GuildRole.Leader || characterId == targetCharacterId) return false;

        var requester = await _guildRepository.GetGuildMember(characterId, cancellationToken);
        var target = await _guildRepository.GetGuildMember(targetCharacterId, cancellationToken);
        if (requester is null || target is null || requester.GuildId != target.GuildId) return false;
        if (!requester.Guild.PermissionsFor(requester.Role).CanPromoteDemote) return false;
        if (requester.Role != GuildRole.Leader && (target.Role <= requester.Role || role < requester.Role)) return false;

        return await _guildRepository.ChangeMemberRoleAsync(requester.GuildId, targetCharacterId, role, cancellationToken);
    }

    public async Task<bool> KickMemberAsync(Guid characterId, Guid targetCharacterId, CancellationToken cancellationToken)
    {
        if (characterId == targetCharacterId) return false;

        var requester = await _guildRepository.GetGuildMember(characterId, cancellationToken);
        var target = await _guildRepository.GetGuildMember(targetCharacterId, cancellationToken);
        if (requester is null || target is null || requester.GuildId != target.GuildId) return false;
        if (!requester.Guild.PermissionsFor(requester.Role).CanKick) return false;
        if (target.Role <= requester.Role) return false;

        return await _guildRepository.KickMemberAsync(requester.GuildId, targetCharacterId, cancellationToken);
    }

    public async Task<bool> UpdateRolePermissionsAsync(Guid characterId, GuildRolePermission permissions, CancellationToken cancellationToken)
    {
        var requester = await _guildRepository.GetGuildMember(characterId, cancellationToken);
        if (requester is null || requester.Role != GuildRole.Leader || permissions.Role == GuildRole.Leader) return false;

        return await _guildRepository.UpdateRolePermissionsAsync(requester.GuildId, permissions, cancellationToken);
    }

}
