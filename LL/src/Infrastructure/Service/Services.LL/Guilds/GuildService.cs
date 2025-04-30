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

    public async Task AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken)
    {
        await _guildRepository.AcceptInviteAsync(characterId, guildId, cancellationToken);
    }

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

    public async Task InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(guildId, currentCharacterId, cancellationToken);
        if (!requestingMember.HasInvitePermissions()) throw new UnauthorizedAccessException("You do not have permission to invite members to this guild.");

        await _guildRepository.InviteAsync(currentCharacterId, guildId, invitedCharacterId, cancellationToken);
    }

    public async Task LeaveGuildAsync(Guid guildId, Guid characterId, CancellationToken cancellationToken)
    {
        await _guildRepository.LeaveGuildAsync(guildId, characterId, cancellationToken);
    }

    public async Task<List<GuildInvite>> GetMyInvitesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _guildRepository.GetMyInvitesAsync(characterId, cancellationToken);
    }
}