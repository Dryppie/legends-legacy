using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Entities.Characters;
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

    public async Task<Guild?> GetMyGuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(g => g.Members)
                .ThenInclude(m => m.Character)
            .Include(g => g.Invites)
                .ThenInclude(i => i.Character)
            .SingleOrDefaultAsync(g => g.Members.Select(gm => gm.CharacterId).Contains(characterId), cancellationToken);

        return guild;
    }

    public async Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken)
    {
        return await _context.Guilds
            .Include(g => g.Owner)
            .Include(g => g.Members)
            .ToListAsync(cancellationToken);
    }

    public async Task LeaveGuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var member = await _context.GuildMembers.FirstOrDefaultAsync(gm => gm.CharacterId == characterId, cancellationToken);

        NotFoundException.ThrowIfNull(member, nameof(member), characterId);

        _context.GuildMembers.Remove(member);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DisbandGuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.Invites)
            .FirstOrDefaultAsync(g => g.OwnerId == characterId, cancellationToken);

        NotFoundException.ThrowIfNull(guild, nameof(guild), characterId);

        _context.GuildMembers.RemoveRange(guild.Members);
        _context.GuildInvites.RemoveRange(guild.Invites);
        _context.Guilds.Remove(guild);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<GuildMember> GetGuildMember(Guid currentCharacterId, CancellationToken cancellationToken)
    {
        var member = await _context.GuildMembers.FirstOrDefaultAsync(gm => gm.CharacterId == currentCharacterId, cancellationToken);

        NotFoundException.ThrowIfNull(member, nameof(member), currentCharacterId);

        return member;
    }

    public async Task InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.Invites)
            .FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken);

        NotFoundException.ThrowIfNull(guild, nameof(guild), guildId);

        if (guild.Members.Count >= guild.MaxMembers)
            throw new InvalidOperationException("Guild is full.");

        guild.Invites.Add(new GuildInvite
        {
            GuildId = guildId,
            CharacterId = invitedCharacterId,
            IsInvite = true,
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task InviteCharacterByNameAsync(Guid currentCharacterId, Guid guildId, string invitedCharacterName, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.Invites)
            .FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken);

        var invitedCharacter = await _context.Characters
            .FirstOrDefaultAsync(c => c.Name.Equals(invitedCharacterName), cancellationToken);

        NotFoundException.ThrowIfNull(guild, nameof(guild), guildId);
        NotFoundException.ThrowIfNull(invitedCharacter, nameof(invitedCharacter), invitedCharacterName);

        if (guild.Members.Count >= guild.MaxMembers)
            throw new InvalidOperationException("Guild is full.");

        guild.Invites.Add(new GuildInvite
        {
            GuildId = guildId,
            CharacterId = invitedCharacter.Id,
            IsInvite = true,
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken)
    {
        var invite = await _context.GuildInvites
            .FirstOrDefaultAsync(i => i.GuildId == guildId && i.CharacterId == characterId, cancellationToken);

        NotFoundException.ThrowIfNull(invite, nameof(invite), guildId);

        // Player can not accept an invitation to a guild that they applied for
        if (!invite.IsInvite) throw new Exception();

        _context.GuildMembers.Add(new GuildMember
        {
            GuildId = guildId,
            CharacterId = characterId,
            Role = GuildRole.Member
        });
        _context.GuildInvites.Remove(invite);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<GuildInvite>> GetMyInvitesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _context.GuildInvites
            .Include(gi => gi.Guild)
            .Where(gi => gi.CharacterId == characterId)
            .ToListAsync(cancellationToken);
    }

    public async Task ApplyToGuildAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters.FindAsync([characterId], cancellationToken);

        NotFoundException.ThrowIfNull(character, nameof(character), characterId);

        var guild = await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.Invites)
            .FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken);

        NotFoundException.ThrowIfNull(guild, nameof(guild), guildId);

        if (guild.Members.Count >= guild.MaxMembers)
            throw new InvalidOperationException("Guild is full.");

        guild.Invites.Add(new GuildInvite
        {
            GuildId = guildId,
            CharacterId = characterId,
            IsInvite = false, // This means its an application to the guild, not an invitation from the guild
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveApplicationAsync(Guid guildId, Guid applicationCharacterId, CancellationToken cancellationToken)
    {
        var invite = await _context.GuildInvites
            .FirstOrDefaultAsync(i => i.GuildId == guildId && i.CharacterId == applicationCharacterId, cancellationToken);

        NotFoundException.ThrowIfNull(invite, nameof(invite), guildId);

        _context.GuildMembers.Add(new GuildMember
        {
            GuildId = guildId,
            CharacterId = applicationCharacterId,
            Role = GuildRole.Member
        });
        _context.GuildInvites.Remove(invite);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectApplicationAsync(Guid guildId, Guid applicationCharacterId, CancellationToken cancellationToken)
    {
        var invite = await _context.GuildInvites
            .FirstOrDefaultAsync(i => i.GuildId == guildId && i.CharacterId == applicationCharacterId, cancellationToken);

        NotFoundException.ThrowIfNull(invite, nameof(invite), guildId);

        _context.GuildInvites.Remove(invite);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken)
    {

        var invite = await _context.GuildInvites
            .FirstOrDefaultAsync(i => i.GuildId == guildId && i.CharacterId == characterId, cancellationToken);

        NotFoundException.ThrowIfNull(invite, nameof(invite), guildId);

        _context.GuildInvites.Remove(invite);

        await _context.SaveChangesAsync(cancellationToken);
    }
}