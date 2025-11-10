using Application.Common.Interfaces;
using Common.Exceptions;
using Common.Helpers.Essences;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences.EssenceSlots;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Seeds.Helpers;

namespace Persistence.LL.Repositories.Entities.Characters;
public class CharacterRepository : ICharacterRepository
{
    private readonly IDbContext _context;

    public CharacterRepository(IDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Character> CreateCharacterAsync(Guid userId, string username, CancellationToken cancellationToken)
    {
        var character = new Character()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = username,
            ImagePath = "player",
            Level = 1
        };

        var essenceSlots = new List<EssenceSlot>()
        {
            new EssenceSlot()
            {
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
            },
        };
        character.Soulstones = 750;
        character.EssenceSlots = essenceSlots;
        character.Professions = ProfessionsSeederHelper.CreateProfessions(character.Id);
        await _context.EssenceSlots.AddRangeAsync(essenceSlots, cancellationToken);
        SeedEquipmentSlots(character);
        await _context.Characters.AddAsync(character, cancellationToken);

        return character;
    }

    /// <inheritdoc/>
    public async Task<Character?> GetCharacterByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            //.Include(c => c.Modifiers)
            //.Include(c => c.RawAttributes)
            //.ThenInclude(a => a.AttributeBase)
            .FirstOrDefaultAsync(c => c.UserId.Equals(userId), cancellationToken);

        return character;
    }

    /// <inheritdoc/>
    public async Task<Character> GetCharacterByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            //.Include(c => c.Modifiers)
            //.Include(c => c.RawAttributes)
            //.ThenInclude(a => a.AttributeBase)
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId));
        NotFoundException.ThrowIfNull(character, nameof(Character), characterId);

        return character;
    }

    /// <inheritdoc/>
    public async Task<Character?> GetCharacterOverviewByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .Include(c => c.EssenceSlots)
                .ThenInclude(es => es.OccupiedEssence)
            .Include(c => c.BaseAttributes)
            .Include(c => c.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.InstanceModifiers)
            .Include(c => c.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.ItemBase)
                        .ThenInclude(ib => (ib as EquipmentBase).AttributeModifiers)
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId), cancellationToken);

        if (character == null) return character;

        foreach (var essenceSlot in character.EssenceSlots.Where(es => es.OccupiedEssence != null))
        {
            EssenceLoader.Instance.LoadAbilitiesForEssence(essenceSlot.OccupiedEssence!);
        }

        return character;
    }

    public async Task<Character?> GetCharacterOverviewByCharacterNameAsync(string characterName, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .Include(c => c.EssenceSlots)
                .ThenInclude(es => es.OccupiedEssence)
            .Include(c => c.BaseAttributes)
            .Include(c => c.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.InstanceModifiers)
            .Include(c => c.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.ItemBase)
                        .ThenInclude(ib => (ib as EquipmentBase).AttributeModifiers)
            .FirstOrDefaultAsync(c => c.Name.ToLower() == characterName.ToLower(), cancellationToken);

        if (character == null) return character;

        foreach (var essenceSlot in character.EssenceSlots.Where(es => es.OccupiedEssence != null))
        {
            EssenceLoader.Instance.LoadAbilitiesForEssence(essenceSlot.OccupiedEssence!);
        }

        return character;
    }

    private static void SeedEquipmentSlots(Entity entity)
    {

        var slotTypes = Enum.GetValues(typeof(EquipmentSlotType)).Cast<EquipmentSlotType>();

        // Create an equipment slot for each enum value
        var equipmentSlots = slotTypes
            .Select(type => new EquipmentSlot
            {
                EntityId = entity.Id,
                EquipmentSlotType = type
            })
            .ToList();

        entity.EquipmentSlots = equipmentSlots;
    }

    
    public async Task<Character> GetBaseCharacterByIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId));
        NotFoundException.ThrowIfNull(character, nameof(Character), characterId);

        return character;
    }

    public async Task<Character?> UpdateCharacterNameAsync(Guid userId, string username, CancellationToken cancellationToken)
    {
        // Check if the desired username is already taken by someone else
        var nameTaken = await _context.Characters
            .AnyAsync(c => c.Name == username && c.UserId != userId, cancellationToken);

        if (nameTaken)
            return null; // or throw a custom exception if you prefer

        var character = await _context.Characters
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (character == null) return null;

        character.Name = username;

        return character;
    }

    public async Task<Character?> GetCharacterWithSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _context.Characters
            .Include(c => c.CharacterSoulstoneUpgrades)
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId), cancellationToken);
    }

    public async Task<Guid?> GetCharacterIdByNameAsync(string name, CancellationToken cancellationToken)
    {
        return await _context.Characters
            .Where(c => c.Name.ToLower() == name.ToLower())
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}