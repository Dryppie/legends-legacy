using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Guilds;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Guilds;
public class GuildRepository : IGuildRepository
{
    private readonly IDbContext _context;

    public GuildRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task AcceptInviteAsync(Guid guildId, Guid characterId, CancellationToken cancellationToken)
    {
        var invite = await _context.GuildInvites
            .FirstOrDefaultAsync(i => i.GuildId == guildId && i.CharacterId == characterId, cancellationToken);

        NotFoundException.ThrowIfNull(invite, nameof(invite), guildId);

        _context.GuildMembers.Add(new GuildMember
        {
            GuildId = guildId,
            CharacterId = characterId,
            Role = GuildRole.Member
        });
        _context.GuildInvites.Remove(invite);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateAsync(Guid ownerCharacterId, string name, CancellationToken cancellationToken)
    {
        if (await _context.Guilds.AnyAsync(g => g.Name == name, cancellationToken))
            throw new Exception("Name or tag already exists.");

        var newGuild = new Guild
        {
            Name = name,
            OwnerId = ownerCharacterId,
            Members =
            {
                new GuildMember { CharacterId = ownerCharacterId, Role = GuildRole.Leader }
            }
        };

        _context.Guilds.Add(newGuild);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guild?> GetAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(g => g.Members)
                .ThenInclude(m => m.Character)
            .SingleOrDefaultAsync(g => g.Members.Select(gm => gm.CharacterId).Contains(characterId), cancellationToken);

        return guild;
    }

    public async Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken)
    {
        return await _context.Guilds.ToListAsync(cancellationToken);
    }

    public async Task InviteAsync(Guid guildId, Guid targetCharacterId, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.Invites)
            .FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken);

        NotFoundException.ThrowIfNull(guild, nameof(guild), guildId);

        if (guild.Members.Count >= guild.MaxMembers)
            throw new InvalidOperationException("Guild is full.");

        guild.Invites.Remove(guild.Invites.Where(i => i.CharacterId == targetCharacterId).First());

        guild.Invites.Add(new GuildInvite
        {
            GuildId = guildId,
            CharacterId = targetCharacterId,
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task LeaveGuildAsync(Guid guildId, Guid characterId, CancellationToken cancellationToken)
    {
        var member = await _context.GuildMembers.FindAsync([guildId, characterId], cancellationToken);

        NotFoundException.ThrowIfNull(member, nameof(member), guildId);

        _context.GuildMembers.Remove(member);
        await _context.SaveChangesAsync(cancellationToken);
    }
}