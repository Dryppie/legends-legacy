using Application.UseCases.Characters.Events;
using Domain.Helpers.Constants;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.NPCs;
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

    public async Task UpdateCharacterLevel(Character character)
    {
        var xpRequired = EntityLevelConstants.XP_REQUIRED(character.Level);

        while (character.Experience >= xpRequired)
        {
            character.Level++;
            character.Experience -= xpRequired;

            // After leveling up and adjusting current experience, calculate whether there's enough experience left for more level ups
            xpRequired = EntityLevelConstants.XP_REQUIRED(character.Level);

            //TODO: Add Publish Event to notify listeners that listen to level ups
            await _publisher.Publish(new CharacterLevelUpEvent(character.Id, character.Level));
        }
    }
}
