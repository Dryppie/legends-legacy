using Application.UseCases.Characters.Events;
using Application.Interfaces.Services.LL.Entities;
using Domain.Helpers;
using Domain.Helpers.Constants;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using MediatR;
using Services.LL.Interfaces;

namespace Services.LL.Levels;
public class LevelingService : ILevelingService
{
    private readonly IPublisher _publisher;
    private readonly ICharacterExperienceProgressionProvider _experienceProgression;
    
    public LevelingService(
        IPublisher publisher,
        ICharacterExperienceProgressionProvider experienceProgression)
    {
        _publisher = publisher;
        _experienceProgression = experienceProgression;
    }

    public async Task UpdateCharacterLevel(Character character, CancellationToken cancellationToken)
    {
        var xpRequired = _experienceProgression.GetRequiredExperience(character.Level);

        while (character.Experience >= xpRequired)
        {
            character.Level = checked(character.Level + 1);
            character.Experience -= xpRequired;

            // After leveling up and adjusting current experience, calculate whether there's enough experience left for more level ups
            xpRequired = _experienceProgression.GetRequiredExperience(character.Level);

            LevelUpCombatAttributes(character);

            //TODO: Add Publish Event to notify listeners that listen to level ups
            await _publisher.Publish(
                new CharacterLevelUpEvent(
                    character.Id,
                    character.Level,
                    character.Experience,
                    xpRequired),
                cancellationToken);
        }
    }

    private static void LevelUpCombatAttributes(Character character)
    {
        foreach (var attr in character.BaseAttributes)
        {
            if (attr.AttributeType is AttributeType.Power or AttributeType.MaxHealth)
            {
                attr.Value = EntityBaseAttributeHelper.GetValueForCharacterLevel(
                    attr.AttributeType,
                    character.Level);
            }
        }
    }

}
