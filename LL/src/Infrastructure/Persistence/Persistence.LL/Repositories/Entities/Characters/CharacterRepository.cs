using Application.Common.Interfaces;
using Common.Exceptions;
using Common.Helpers.Essences;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Entities.Characters;
public class CharacterRepository : ICharacterRepository
{
    private readonly IDbContext _context;

    public CharacterRepository(IDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Character> CreateCharacterAsync(string userId, string username, CancellationToken cancellationToken)
    {
        var character = new Character()
        {
            UserId = userId,
            Name = username
        };

        // TODO: This is only temporary, so guests have abilities
        

        var essences = new List<Essence>()
            {
                new Essence()
                {
                    Id = Guid.NewGuid(),
                    Name = "Goblin's Essence",
                    ActiveAbilityId = "sneakAttack",
                    PassiveAbilityId = "pocketDirt"
                },
            };

        var essenceSlots = new List<EssenceSlot>()
        {
            new EssenceSlot()
            {
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = essences.First()
            },
        };

        character.EssenceSlots = essenceSlots;

        await _context.Essences.AddRangeAsync(essences);
        await _context.EssenceSlots.AddRangeAsync(essenceSlots);
        await _context.Characters.AddAsync(character);

        await _context.SaveChangesAsync(cancellationToken);

        return character;
    }

    /// <inheritdoc/>
    public async Task<Character> GetCharacterByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            //.Include(c => c.Modifiers)
            //.Include(c => c.RawAttributes)
            //.ThenInclude(a => a.AttributeBase)
            .FirstOrDefaultAsync(c => c.UserId.Equals(userId.ToString()));
        NotFoundException.ThrowIfNull(character, nameof(Character), userId);

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
    public async Task<Character> GetCharacterOverviewByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .Include(c => c.EssenceSlots)
                .ThenInclude(es => es.OccupiedEssence)
            .Include(c => c.BaseAttributes)
            .Include(c => c.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.ItemBase)
                        .ThenInclude(ib => (ib as EquipmentBase).AttributeModifiers)
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId), cancellationToken);

        NotFoundException.ThrowIfNull(character, nameof(Character), characterId);

        foreach (var essenceSlot in character.EssenceSlots.Where(es => es.OccupiedEssence != null))
        {
            EssenceLoader.Instance.LoadAbilitiesForEssence(essenceSlot.OccupiedEssence!);
        }

        return character;
    }

    public async Task<List<CharacterLeaderboardItem>> GetLeaderboardCharactersAsync(CancellationToken cancellationToken)
    {
        var leaderboard = await _context.Characters
            .OrderByDescending(c => c.Level)
            .ThenByDescending(c => c.Experience)
            .Take(10)
            .Select(c => new CharacterLeaderboardItem
            {
                Name = c.Name,
                Level = c.Level,
                Experience = (int)c.Experience
            })
            .ToListAsync(cancellationToken);

        return leaderboard;
    }
}