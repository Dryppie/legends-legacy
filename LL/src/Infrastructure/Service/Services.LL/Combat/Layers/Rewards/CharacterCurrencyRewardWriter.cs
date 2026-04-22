using Application.Interfaces.Services.LL.Entities;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Combat.Layers.Rewards;

public sealed class CharacterCurrencyRewardWriter : ICurrencyRewardWriter
{
    private readonly ICharacterService _characterService;
    private readonly IEntityService _entityService;

    public CharacterCurrencyRewardWriter(
        ICharacterService characterService,
        IEntityService entityService)
    {
        _characterService = characterService;
        _entityService = entityService;
    }

    public async Task AddAsync(
        Guid characterId,
        int cinders,
        int soulstones,
        CancellationToken cancellationToken)
    {
        if (cinders <= 0 && soulstones <= 0)
        {
            return;
        }

        var character = await _characterService.GetCharacterByCharacterIdAsync(
            characterId,
            cancellationToken);

        if (character is null)
        {
            throw new InvalidOperationException(
                $"Could not award currency. Character '{characterId}' was not found.");
        }

        if (cinders > 0)
        {
            character.Cinders += cinders;
        }

        if (soulstones > 0)
        {
            character.Soulstones += soulstones;
        }

        _entityService.UpdateEntities([character]);
    }
}