using Application.Common.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Domain.Extensions.Guilds;
using Domain.Models.Guilds;

namespace Services.LL.Guilds;
public class GuildService : IGuildService
{
    private readonly IGuildRepository _guildRepository;
    private readonly ICharacterService _characterService;
    private readonly IInventoryService _inventoryService;
    private readonly IDbContext _context;

    public GuildService(IGuildRepository guildRepository, ICharacterService characterService, IInventoryService inventoryService, IDbContext context)
    {
        _guildRepository = guildRepository;
        _characterService = characterService;
        _inventoryService = inventoryService;
        _context = context;
    }

    #region guild
    public async Task<bool> CreateAsync(Guid characterId, string name, CancellationToken cancellationToken) => 
        await _guildRepository.CreateAsync(characterId, name, cancellationToken);

    public async Task<Guild?> GetMyGuildAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _guildRepository.GetMyGuildAsync(characterId, cancellationToken);

    public async Task<Guild?> GetGuildWithUpgradesAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _guildRepository.GetGuildWithUpgradesAsync(characterId, cancellationToken);

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

        return await _guildRepository.RejectGuildInviteAsync(applicationCharacterId, requestingMember.GuildId, cancellationToken);
    }

    public async Task<bool> ApproveApplicationAsync(Guid characterId, Guid applicationCharacterId, CancellationToken cancellationToken)
    {
        var requestingMember = await _guildRepository.GetGuildMember(characterId, cancellationToken);
        if (requestingMember == null || !requestingMember.HasInvitePermissions()) return false;

        return await _guildRepository.ApproveApplicationAsync(requestingMember.GuildId, applicationCharacterId, cancellationToken);
    }

    public async Task<bool> RejectInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken) => 
        await _guildRepository.RejectGuildInviteAsync(characterId, guildId, cancellationToken);
    #endregion

    public async Task<bool> DonateToGuildAsync(Guid characterId, Dictionary<GuildResourceType, int> donations, CancellationToken cancellationToken)
    {
        var guild = await _guildRepository.GetMyGuildAsync(characterId, cancellationToken);
        if (guild == null) return false;

        var character = await _characterService.GetMyCharacterOverviewAsync(characterId, cancellationToken);
        if (character == null) return false;

        var inventory = await _inventoryService.GetInventoryByIdAsync(characterId, cancellationToken);
        if (inventory == null) return false;

        foreach (var (resourceType, amount) in donations)
        {
            if (amount <= 0)
                continue;

            switch (resourceType)
            {
                case GuildResourceType.Cinders:
                    if (character.Cinders < amount)
                        return false;
                    character.Cinders -= amount;
                    break;

                case GuildResourceType.Soulstones:
                    if (character.Soulstones < amount)
                        return false;
                    character.Soulstones -= amount;
                    break;

                default:
                    // Inventory-based resources
                    var matchingItem = inventory.InventoryItems
                        .FirstOrDefault(i =>
                            i.ItemInstance?.ItemBase?.Name.Replace(" ", "") == resourceType.ToString());

                    if (matchingItem == null || matchingItem.Quantity < amount)
                        return false;

                    if (matchingItem.Quantity == amount)
                    {
                        _context.InventoryItems.Remove(matchingItem);
                    }
                    else
                    {
                        matchingItem.Quantity -= amount;
                    }

                    break;
            }

            // Add to guild vault or resource list (pseudo, depends on how you're storing it)
            var resource = guild.Resources.FirstOrDefault(r => r.Resource == resourceType);
            if (resource == null)
                guild.Resources.Add(new GuildResource { Resource = resourceType, Amount = amount });
            else
                resource.Amount += amount;
        }

        return true;
    }
}