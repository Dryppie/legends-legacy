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
        if (await _context.Guilds.AnyAsync(g => g.Name.ToLower() == name.ToLower(), cancellationToken)) return false;
        if (await _context.GuildMembers.AnyAsync(gm => gm.CharacterId == ownerCharacterId, cancellationToken)) return false;

        var newGuild = new Guild
        {
            Name = name,
            OwnerId = ownerCharacterId,
            Members =
            {
                new GuildMember { CharacterId = ownerCharacterId, Role = GuildRole.Leader }
            }
        };

        newGuild.RolePermissions.Add(GuildRolePermission.CreateDefault(newGuild.Id, GuildRole.Leader));
        newGuild.RolePermissions.Add(GuildRolePermission.CreateDefault(newGuild.Id, GuildRole.Officer));
        newGuild.RolePermissions.Add(GuildRolePermission.CreateDefault(newGuild.Id, GuildRole.Member));

        _context.Guilds.Add(newGuild);
        return true;
    }

    public async Task<Guild?> GetMyGuildAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.Guilds
            .Include(g => g.Owner)
            .Include(g => g.Members)
                .ThenInclude(m => m.Character)
            .Include(g => g.Invites)
                .ThenInclude(i => i.Character)
            .Include(g => g.Resources)
            .Include(g => g.Buildings)
            .Include(g => g.RolePermissions)
            .Include(g => g.VaultItems)
                .ThenInclude(x => x.DonatedByCharacter)
            .Include(g => g.VaultItems)
                .ThenInclude(x => x.BorrowedByCharacter)
            .Include(g => g.VaultItems)
                .ThenInclude(x => x.EquipmentInstance)
                    .ThenInclude(x => x.InstanceModifiers)
            .Include(g => g.VaultItems)
                .ThenInclude(x => x.EquipmentInstance)
                    .ThenInclude(x => x.ToolAffixes)
            .Include(g => g.VaultItems)
                .ThenInclude(x => x.EquipmentInstance)
                    .ThenInclude(x => x.ItemBase)
                        .ThenInclude(x => (x as Domain.Models.Items.Equipments.EquipmentBase).AttributeModifiers)
            .Include(g => g.VaultItems)
                .ThenInclude(x => x.EquipmentInstance)
                    .ThenInclude(x => x.ItemBase)
                        .ThenInclude(x => (x as Domain.Models.Items.Equipments.EquipmentBase).ToolBonuses)
            .SingleOrDefaultAsync(g => g.Members.Select(gm => gm.CharacterId).Contains(characterId), cancellationToken);

    public async Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken) =>
        await _context.Guilds
            .Include(g => g.Owner)
            .Include(g => g.Members)
            .Include(g => g.Buildings)
            .ToListAsync(cancellationToken);

    public async Task<bool> LeaveGuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var member = await _context.GuildMembers.FirstOrDefaultAsync(gm => gm.CharacterId == characterId, cancellationToken);

        if (member == null || member.Role == GuildRole.Leader) return false;

        await ReturnBorrowedItemsAsync(characterId, cancellationToken);
        _context.GuildMembers.Remove(member);
        return true;
    }

    public async Task<bool> DisbandGuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.Invites)
            .Include(g => g.VaultItems)
            .FirstOrDefaultAsync(g => g.OwnerId == characterId, cancellationToken);

        if (guild == null) return false;

        foreach (var vaultItem in guild.VaultItems.Where(x => x.BorrowedByCharacterId is null))
        {
            _context.InventoryItems.Add(new Domain.Models.Inventories.InventoryItem
            {
                InventoryId = vaultItem.DonatedByCharacterId,
                ItemInstanceId = vaultItem.EquipmentInstanceId,
                Quantity = 1
            });
        }

        _context.GuildMembers.RemoveRange(guild.Members);
        _context.GuildInvites.RemoveRange(guild.Invites);
        _context.Guilds.Remove(guild);
        return true;
    }

    public async Task<GuildMember?> GetGuildMember(Guid currentCharacterId, CancellationToken cancellationToken) =>
        await _context.GuildMembers
            .Include(x => x.Guild)
                .ThenInclude(x => x.RolePermissions)
            .FirstOrDefaultAsync(gm => gm.CharacterId == currentCharacterId, cancellationToken);

    public async Task<bool> InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.Invites)
            .Include(g => g.Buildings)
            .FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken);

        if (guild == null || guild.IsGuildFull()) return false;

        guild.Invites.Add(new GuildInvite
        {
            GuildId = guildId,
            CharacterId = invitedCharacterId,
            IsInvite = true,
        });
        return true;
    }

    public async Task<bool> InviteCharacterByNameAsync(Guid currentCharacterId, Guid guildId, string invitedCharacterName, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.Invites)
            .Include(g => g.Buildings)
            .FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken);

        if (guild == null || guild.IsGuildFull()) return false;

        var invitedCharacter = await _context.Characters
            .FirstOrDefaultAsync(c => c.Name.ToLower() == invitedCharacterName.ToLower(), cancellationToken);

        if (invitedCharacter == null) return false;

        guild.Invites.Add(new GuildInvite
        {
            GuildId = guildId,
            CharacterId = invitedCharacter.Id,
            IsInvite = true,
        });
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

        //if (guild.IsGuildFull()) return false;

        guild.Invites.Add(new GuildInvite
        {
            GuildId = guildId,
            CharacterId = characterId,
            IsInvite = false, // This means its an application to the guild, not an invitation from the guild
        });
        return true;
    }

    public async Task<bool> AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken)
    {
        var invite = await _context.GuildInvites
            .FirstOrDefaultAsync(i => i.GuildId == guildId && i.CharacterId == characterId, cancellationToken);

        // Must be a real invite, not an application
        if (invite == null || !invite.IsInvite) return false;

        return await TryAddCharacterToGuildAsync(characterId, guildId, cancellationToken);
    }

    public async Task<bool> ApproveApplicationAsync(Guid guildId, Guid applicationCharacterId, CancellationToken cancellationToken)
    {
        var application = await _context.GuildInvites
            .FirstOrDefaultAsync(i => i.GuildId == guildId && i.CharacterId == applicationCharacterId, cancellationToken);

        // Must be a real application, not an invite
        if (application == null || application.IsInvite) return false;

        return await TryAddCharacterToGuildAsync(applicationCharacterId, guildId, cancellationToken);
    }
    private async Task<bool> TryAddCharacterToGuildAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.Buildings)
            .FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken);

        if (guild == null || guild.IsGuildFull()) return false;

        var isInGuild = await _context.GuildMembers
            .AnyAsync(gm => gm.CharacterId == characterId, cancellationToken);

        if (isInGuild) return false;

        _context.GuildMembers.Add(new GuildMember
        {
            GuildId = guildId,
            CharacterId = characterId,
            Role = GuildRole.Member
        });

        // Remove *all* invites/applications by this character to clean up
        var allInvites = await _context.GuildInvites
            .Where(i => i.CharacterId == characterId)
            .ToListAsync(cancellationToken);
        _context.GuildInvites.RemoveRange(allInvites);

        return true;
    }

    public async Task<bool> RejectGuildInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken)
    {
        var invite = await _context.GuildInvites
            .FirstOrDefaultAsync(i => i.GuildId == guildId && i.CharacterId == characterId, cancellationToken);

        if (invite == null) return false;

        _context.GuildInvites.Remove(invite);
        return true;
    }

    public async Task<Guild?> GetGuildForMemberAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.Resources)
            .Include(g => g.Buildings)
            .Include(g => g.RolePermissions)
            .FirstOrDefaultAsync(g => g.Members.Select(gm => gm.CharacterId).Contains(characterId), cancellationToken);

    public async Task<bool> ChangeMemberRoleAsync(Guid guildId, Guid characterId, GuildRole role, CancellationToken cancellationToken)
    {
        var member = await _context.GuildMembers
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.CharacterId == characterId, cancellationToken);
        if (member is null) return false;

        member.Role = role;
        return true;
    }

    public async Task<bool> KickMemberAsync(Guid guildId, Guid characterId, CancellationToken cancellationToken)
    {
        var member = await _context.GuildMembers
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.CharacterId == characterId, cancellationToken);
        if (member is null) return false;

        await ReturnBorrowedItemsAsync(characterId, cancellationToken);
        _context.GuildMembers.Remove(member);
        return true;
    }

    public async Task<bool> UpdateRolePermissionsAsync(Guid guildId, GuildRolePermission permissions, CancellationToken cancellationToken)
    {
        var existing = await _context.GuildRolePermissions
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.Role == permissions.Role, cancellationToken);

        if (existing is null)
        {
            permissions.GuildId = guildId;
            _context.GuildRolePermissions.Add(permissions);
            return true;
        }

        existing.CanInvite = permissions.CanInvite;
        existing.CanManageApplications = permissions.CanManageApplications;
        existing.CanPromoteDemote = permissions.CanPromoteDemote;
        existing.CanKick = permissions.CanKick;
        existing.CanBorrowVault = permissions.CanBorrowVault;
        return true;
    }

    private async Task ReturnBorrowedItemsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var borrowed = await _context.GuildVaultItems
            .Where(x => x.BorrowedByCharacterId == characterId)
            .ToListAsync(cancellationToken);
        if (borrowed.Count == 0) return;

        var itemIds = borrowed.Select(x => x.EquipmentInstanceId).ToList();
        var inventoryItems = await _context.InventoryItems
            .Where(x => x.InventoryId == characterId && itemIds.Contains(x.ItemInstanceId))
            .ToListAsync(cancellationToken);
        var slots = await _context.EquipmentSlots
            .Where(x => x.EntityId == characterId && x.EquipmentInstanceId != null && itemIds.Contains(x.EquipmentInstanceId.Value))
            .ToListAsync(cancellationToken);

        _context.InventoryItems.RemoveRange(inventoryItems);
        foreach (var slot in slots)
        {
            slot.EquipmentInstanceId = null;
            slot.EquipmentInstance = null;
        }
        foreach (var item in borrowed)
        {
            item.BorrowedByCharacterId = null;
            item.BorrowedAt = null;
        }
    }
}
