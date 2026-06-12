using Application.UseCases.Characters.Events;
using Domain.Helpers.Constants;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Professions;
using MediatR;
using Services.LL.Interfaces;

namespace Services.LL.Levels;
public class LevelingService : ILevelingService
{
    private readonly IPublisher _publisher;
    
    public LevelingService(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task UpdateCharacterLevel(Character character, CancellationToken cancellationToken)
    {
        var xpRequired = EntityLevelConstants.XP_REQUIRED(character.Level);

        while (character.Experience >= xpRequired)
        {
            character.Level++;
            character.Experience -= xpRequired;

            // After leveling up and adjusting current experience, calculate whether there's enough experience left for more level ups
            xpRequired = EntityLevelConstants.XP_REQUIRED(character.Level);

            LevelUpPrimaryAttributes(character);

            //TODO: Add Publish Event to notify listeners that listen to level ups
            await _publisher.Publish(new CharacterLevelUpEvent(character.Id, character.Level), cancellationToken);
        }
    }

    private static void LevelUpPrimaryAttributes(Character character)
    {
        foreach (var attr in character.BaseAttributes)
        {
            if (attr.AttributeType == AttributeType.Power ||
                attr.AttributeType == AttributeType.Fortitude ||
                attr.AttributeType == AttributeType.Precision ||
                attr.AttributeType == AttributeType.Spirit)
            {
                attr.Value += 2;
            }
        }
    }

    public async Task UpdateProfessionLevel(Profession profession, CancellationToken cancellationToken)
    {
        var xpRequired = EntityLevelConstants.XP_REQUIRED(profession.Level);

        while (profession.Experience >= xpRequired)
        {
            profession.Level++;
            profession.Experience -= xpRequired;

            // After leveling up and adjusting current experience, calculate whether there's enough experience left for more level ups
            xpRequired = EntityLevelConstants.XP_REQUIRED(profession.Level);

            //TODO: Add Publish Event to notify listeners that listen to level ups
            //await _publisher.Publish(new CharacterLevelUpEvent(character.Id, profession.Level));
        }
    }
}
