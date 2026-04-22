using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Entities;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Combat.Layers.Rewards;

public sealed class CharacterExperienceRewardWriter : IExperienceRewardWriter
{
    private readonly ICharacterService _characterService;
    private readonly ILevelingService _levelingService;
    private readonly IEntityService _entityService;

    public CharacterExperienceRewardWriter(
        ICharacterService characterService,
        ILevelingService levelingService,
        IEntityService entityService)
    {
        _characterService = characterService;
        _levelingService = levelingService;
        _entityService = entityService;
    }

    public async Task AddSplitExperienceAsync(
        IReadOnlyCollection<Guid> recipientCharacterIds,
        int totalExperience,
        CancellationToken cancellationToken)
    {
        if (totalExperience <= 0 || recipientCharacterIds.Count == 0)
        {
            return;
        }

        var distinctRecipientIds = recipientCharacterIds
            .Distinct()
            .ToArray();

        var baseShare = totalExperience / distinctRecipientIds.Length;
        var remainder = totalExperience % distinctRecipientIds.Length;

        var updatedEntities = new List<Entity>(distinctRecipientIds.Length);

        for (var index = 0; index < distinctRecipientIds.Length; index++)
        {
            var characterId = distinctRecipientIds[index];

            var character = await _characterService.GetCharacterByCharacterIdAsync(
                characterId,
                cancellationToken);

            if (character is null)
            {
                throw new InvalidOperationException(
                    $"Could not award experience. Character '{characterId}' was not found.");
            }

            var award = baseShare + (index < remainder ? 1 : 0);

            if (award <= 0)
            {
                continue;
            }

            character.Experience += award;

            await _levelingService.UpdateCharacterLevel(
                character,
                cancellationToken);

            updatedEntities.Add(character);
        }

        if (updatedEntities.Count > 0)
        {
            _entityService.UpdateEntities(updatedEntities);
        }
    }
}