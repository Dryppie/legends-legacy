using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Colosseum;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Users;
using Domain.Models.Items.Equipments.Slots;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.QueryProfiles;
using Persistence.LL.Seeds.Helpers;

namespace Persistence.LL.Repositories.Entities.Characters;

public class CharacterRepository : ICharacterRepository
{
    private readonly IDbContext _context;

    public CharacterRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<Character> CreateCharacterAsync(Guid userId, string username, CancellationToken cancellationToken)
    {
        var characterId = Guid.NewGuid();
        var character = new Character
        {
            Id = characterId,
            UserId = userId,
            Name = username,
            ImagePath = "player",
            Level = 1,
            Soulstones = 0,
            ArenaProfile = new CharacterArenaProfile { CharacterId = characterId },
            ArenaTicketStatus = new ArenaTicketStatus
            {
                CharacterId = characterId,
                CurrentTickets = 5,
                LastTicketUpdate = DateTimeOffset.UtcNow
            },
            Professions = ProfessionsSeederHelper.CreateProfessions(characterId)
        };
        character.NormalizeName();

        SeedEquipmentSlots(character);
        await _context.Characters.AddAsync(character, cancellationToken);
        return character;
    }

    public async Task<Character?> GetCharacterByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var localCharacter = _context.Characters.Local
            .FirstOrDefault(c => c.UserId == userId);

        if (localCharacter is not null)
        {
            return localCharacter;
        }

        return await _context.Characters
            .Include(c => c.ArenaProfile)
            .Include(c => c.EquippedTitleDefinition)
            .FirstOrDefaultAsync(c => c.UserId.Equals(userId), cancellationToken);
    }

    public async Task<Character> GetCharacterByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .Include(c => c.ArenaProfile)
            .Include(c => c.EquippedTitleDefinition)
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId), cancellationToken);
        NotFoundException.ThrowIfNull(character, nameof(Character), characterId);
        return character;
    }

    public async Task<Character?> GetCharacterOverviewByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.Characters
            .AsNoTracking()
            .EntireCharacter()
            .Include(c => c.EquippedTitleDefinition)
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId), cancellationToken);

    public async Task<Character?> GetCharacterOverviewByCharacterNameAsync(string characterName, CancellationToken cancellationToken) =>
        await _context.Characters
            .AsNoTracking()
            .EntireCharacter()
            .Include(c => c.EquippedTitleDefinition)
            .FirstOrDefaultAsync(c => c.Name.ToLower() == characterName.ToLower(), cancellationToken);

    public async Task<Character> GetBaseCharacterByIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .Include(c => c.ArenaProfile)
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId), cancellationToken);
        NotFoundException.ThrowIfNull(character, nameof(Character), characterId);
        return character;
    }

    public async Task<Character?> UpdateCharacterNameAsync(Guid userId, string username, CancellationToken cancellationToken)
    {
        var normalizedName = IdentityNormalizer.NormalizeRequired(username);
        var nameTaken = await _context.Characters.AnyAsync(
            c => c.NormalizedName == normalizedName && c.UserId != userId,
            cancellationToken);
        if (nameTaken) return null;

        var character = await _context.Characters.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (character == null) return null;

        character.Name = username.Trim();
        character.NormalizeName();
        return character;
    }

    public async Task<bool> IsCharacterNameTakenAsync(string name, Guid? excludedCharacterId, CancellationToken cancellationToken)
    {
        var normalizedName = IdentityNormalizer.NormalizeOptional(name);
        if (normalizedName is null) return false;

        return await _context.Characters.AnyAsync(
            c => c.NormalizedName == normalizedName && (!excludedCharacterId.HasValue || c.Id != excludedCharacterId.Value),
            cancellationToken);
    }

    public async Task<Character?> GetCharacterWithSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.Characters
            .Include(c => c.CharacterSoulstoneUpgrades)
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId), cancellationToken);

    public async Task<Guid?> GetCharacterIdByNameAsync(string name, CancellationToken cancellationToken) =>
        await _context.Characters
            .Where(c => c.NormalizedName == IdentityNormalizer.NormalizeRequired(name))
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private static void SeedEquipmentSlots(Entity entity)
    {
        entity.EquipmentSlots = Enum.GetValues(typeof(EquipmentSlotType))
            .Cast<EquipmentSlotType>()
            .Select(type => new EquipmentSlot
            {
                EntityId = entity.Id,
                EquipmentSlotType = type
            })
            .ToList();
    }
}
