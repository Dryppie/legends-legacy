using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Entities.Actors.Characters;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Interfaces;

namespace Persistence.LL.Repositories.Characters;
public class CharacterRepository : ICharacterRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public CharacterRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<Character> CreateCharacterAsync(string userId, string username, CancellationToken cancellationToken)
    {
        var character = new Character()
        {
            UserId = userId,
            Name = username
        };
        _unitOfWork.Context.Characters.Add(character);

        await _unitOfWork.Context.SaveChangesAsync(cancellationToken);

        return character;
    }

    /// <inheritdoc/>
    public async Task<Character> GetCharacterByUserIdAsync(Guid userId)
    {
        var character = await _unitOfWork.Context.Characters
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
        var character = await _unitOfWork.Context.Characters
            //.Include(c => c.Modifiers)
            //.Include(c => c.RawAttributes)
            //.ThenInclude(a => a.AttributeBase)
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId));
        NotFoundException.ThrowIfNull(character, nameof(Character), characterId);

        return character;
    }
}