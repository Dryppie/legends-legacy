using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Abilities;
using Domain.Models.Entities.Characters;
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
        _context.Characters.Add(character);

        // TODO: This is only temporary, so guests have abilities

        var abilities = new List<AbilityId>()
            {
                new AbilityId()
                {
                    EntityId = character.Id,
                    Id = "fireball_01"
                },
                new AbilityId()
                {
                    EntityId = character.Id,
                    Id = "heal_01"
                }
            };
        _context.AbilityIds.AddRange(abilities);

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
}