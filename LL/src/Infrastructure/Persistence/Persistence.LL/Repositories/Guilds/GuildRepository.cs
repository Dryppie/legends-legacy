using Application.Common.Interfaces;
using Domain.Extensions.Guilds;
using Domain.Models.Guilds;
using Microsoft.EntityFrameworkCore;
using System;

namespace Persistence.LL.Repositories.Guilds;
public class GuildRepository : IGuildRepository
{
    private readonly IDbContext _context;

    public GuildRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CreateAsync(Guid ownerCharacterId, string name, CancellationToken cancellationToken)
    {
        if (await _context.Guilds.AnyAsync(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase), cancellationToken)) return false;

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
        return true;
    }

    public async Task<Guild?> GetMyGuildAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.Guilds
            .Include(g => g.Members)
                .ThenInclude(m => m.Character)
            .Include(g => g.Invites)
                .ThenInclude(i => i.Character)
            .SingleOrDefaultAsync(g => g.Members.Select(gm => gm.CharacterId).Contains(characterId), cancellationToken);

    public async Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken) =>
        await _context.Guilds
            .Include(g => g.Owner)
            .Include(g => g.Members)
            .ToListAsync(cancellationToken);

    public async Task<bool> LeaveGuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var member = await _context.GuildMembers.FirstOrDefaultAsync(gm => gm.CharacterId == characterId, cancellationToken);

        if (member == null) return false;

        _context.GuildMembers.Remove(member);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DisbandGuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.Invites)
            .FirstOrDefaultAsync(g => g.OwnerId == characterId, cancellationToken);

        if (guild == null) return false;

        _context.GuildMembers.RemoveRange(guild.Members);
        _context.GuildInvites.RemoveRange(guild.Invites);
        _context.Guilds.Remove(guild);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<GuildMember?> GetGuildMember(Guid currentCharacterId, CancellationToken cancellationToken) =>
        await _context.GuildMembers
            .FirstOrDefaultAsync(gm => gm.CharacterId == currentCharacterId, cancellationToken);

    public async Task<bool> InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.Invites)
            .FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken);

        if (guild == null || guild.IsGuildFull()) return false;

        if (guild.Members.Count >= guild.MaxMembers) return false;

        guild.Invites.Add(new GuildInvite
        {
            GuildId = guildId,
            CharacterId = invitedCharacterId,
            IsInvite = true,
        });
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> InviteCharacterByNameAsync(Guid currentCharacterId, Guid guildId, string invitedCharacterName, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.Invites)
            .FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken);

        var invitedCharacter = await _context.Characters
            .FirstOrDefaultAsync(c => c.Name.Equals(invitedCharacterName), cancellationToken);

        if (guild == null || invitedCharacter == null || guild.IsGuildFull()) return false;

        if (guild.Members.Count >= guild.MaxMembers) return false;

        guild.Invites.Add(new GuildInvite
        {
            GuildId = guildId,
            CharacterId = invitedCharacter.Id,
            IsInvite = true,
        });
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds.FindAsync([guildId], cancellationToken);
        if (guild == null || guild.IsGuildFull()) return false;

        var invite = await _context.GuildInvites
            .FirstOrDefaultAsync(i => i.GuildId == guildId && i.CharacterId == characterId, cancellationToken);

        if (invite == null) return false;

        // Player can not accept an invitation to a guild that they applied for
        if (!invite.IsInvite) return false;

        _context.GuildMembers.Add(new GuildMember
        {
            GuildId = guildId,
            CharacterId = characterId,
            Role = GuildRole.Member
        });
        _context.GuildInvites.Remove(invite);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<GuildInvite>> GetMyInvitesAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.GuildInvites
            .Include(gi => gi.Guild)
            .Where(gi => gi.CharacterId == characterId)
            .ToListAsync(cancellationToken);

    public async Task<bool> ApplyToGuildAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters.FindAsync([characterId], cancellationToken);

        if (character == null) return false;

        var guild = await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.Invites)
            .FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken);

        if (guild == null) return false;

        if (guild.IsGuildFull()) return false;

        guild.Invites.Add(new GuildInvite
        {
            GuildId = guildId,
            CharacterId = characterId,
            IsInvite = false, // This means its an application to the guild, not an invitation from the guild
        });
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ApproveApplicationAsync(Guid guildId, Guid applicationCharacterId, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds.FindAsync([guildId], cancellationToken);
        if (guild == null || guild.IsGuildFull()) return false;

        var invite = await _context.GuildInvites
            .FirstOrDefaultAsync(i => i.GuildId == guildId && i.CharacterId == applicationCharacterId, cancellationToken);

        if (invite == null) return false;

        _context.GuildMembers.Add(new GuildMember
        {
            GuildId = guildId,
            CharacterId = applicationCharacterId,
            Role = GuildRole.Member
        });
        _context.GuildInvites.Remove(invite);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RejectApplicationAsync(Guid guildId, Guid applicationCharacterId, CancellationToken cancellationToken)
    {
        var invite = await _context.GuildInvites
            .FirstOrDefaultAsync(i => i.GuildId == guildId && i.CharacterId == applicationCharacterId, cancellationToken);

        if (invite == null) return false;

        _context.GuildInvites.Remove(invite);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RejectInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken)
    {
        var invite = await _context.GuildInvites
            .FirstOrDefaultAsync(i => i.GuildId == guildId && i.CharacterId == characterId, cancellationToken);

        if (invite == null) return false;

        _context.GuildInvites.Remove(invite);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}