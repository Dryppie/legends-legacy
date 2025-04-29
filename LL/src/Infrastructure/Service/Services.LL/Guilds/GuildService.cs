using Application.Interfaces.Services.LL;
using Domain.Models.Guilds;

namespace Services.LL.Guilds;
public class GuildService : IGuildService
{
    private readonly IGuildRepository _guildRepository;

    public GuildService(IGuildRepository guildRepository)
    {
        _guildRepository = guildRepository;
    }

    public async Task AcceptInviteAsync(Guid guildId, Guid characterId, CancellationToken cancellationToken)
    {
        await _guildRepository.AcceptInviteAsync(guildId, characterId, cancellationToken);
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

    public async Task InviteAsync(Guid guildId, Guid targetCharacterId, CancellationToken cancellationToken)
    {
        await _guildRepository.InviteAsync(guildId, targetCharacterId, cancellationToken);
    }

    public async Task LeaveGuildAsync(Guid guildId, Guid characterId, CancellationToken cancellationToken)
    {
        await _guildRepository.LeaveGuildAsync(guildId, characterId, cancellationToken);
    }
}