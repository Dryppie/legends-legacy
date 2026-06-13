using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
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
            Professions = ProfessionsSeederHelper.CreateProfessions(characterId)
        };

        SeedEquipmentSlots(character);
        await _context.Characters.AddAsync(character, cancellationToken);
        return character;
    }

    public async Task<Character?> GetCharacterByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        await _context.Characters.FirstOrDefaultAsync(c => c.UserId.Equals(userId), cancellationToken);

    public async Task<Character> GetCharacterByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters.FirstOrDefaultAsync(c => c.Id.Equals(characterId), cancellationToken);
        NotFoundException.ThrowIfNull(character, nameof(Character), characterId);
        return character;
    }

    public async Task<Character?> GetCharacterOverviewByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.Characters
            .AsNoTracking()
            .EntireCharacter()
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId), cancellationToken);

    public async Task<Character?> GetCharacterOverviewByCharacterNameAsync(string characterName, CancellationToken cancellationToken) =>
        await _context.Characters
            .AsNoTracking()
            .EntireCharacter()
            .FirstOrDefaultAsync(c => c.Name.ToLower() == characterName.ToLower(), cancellationToken);

    public async Task<Character> GetBaseCharacterByIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters.FirstOrDefaultAsync(c => c.Id.Equals(characterId), cancellationToken);
        NotFoundException.ThrowIfNull(character, nameof(Character), characterId);
        return character;
    }

    public async Task<Character?> UpdateCharacterNameAsync(Guid userId, string username, CancellationToken cancellationToken)
    {
        var nameTaken = await _context.Characters.AnyAsync(c => c.Name == username && c.UserId != userId, cancellationToken);
        if (nameTaken) return null;

        var character = await _context.Characters.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (character == null) return null;

        character.Name = username;
        return character;
    }

    public async Task<Character?> GetCharacterWithSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.Characters
            .Include(c => c.CharacterSoulstoneUpgrades)
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId), cancellationToken);

    public async Task<Guid?> GetCharacterIdByNameAsync(string name, CancellationToken cancellationToken) =>
        await _context.Characters
            .Where(c => c.Name.ToLower() == name.ToLower())
            .Select(c => c.Id)
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