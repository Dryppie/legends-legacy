using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
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
                    Name = "Starter Essence 1",
                    ActiveAbilityId = "fireball_01",
                    PassiveAbilityId = "retaliate_01"
                },
                new Essence()
                {
                    Id = Guid.NewGuid(),
                    Name = "Starter Essence 2",
                    ActiveAbilityId = "heal_01",
                    PassiveAbilityId = "pocketDirt"
                }
            };

        character.EquippedEssences = essences;
        await _context.Essences.AddRangeAsync(essences);

        _context.Characters.Add(character);

        await _context.SaveChangesAsync(cancellationToken);

        return character;
    }

    /// <inheritdoc/>
    public async Task<Character> GetCharacterByUserIdAsync(Guid userId)
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
    public async Task<Character> GetCharacterByCharacterIdAsync(Guid characterId)
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
    public async Task<Character> GetCharacterOverviewByCharacterIdAsync(Guid characterId)
    {
        var character = await _context.Characters
            .Include(c => c.EquippedEssences)
            .Include(c => c.BaseAttributes)
            //.Include(c => c.RawAttributes)
            //.ThenInclude(a => a.AttributeBase)
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId));
        NotFoundException.ThrowIfNull(character, nameof(Character), characterId);

        return character;
    }
}