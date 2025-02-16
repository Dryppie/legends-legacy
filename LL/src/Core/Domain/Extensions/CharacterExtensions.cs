using Domain.Helpers.Constants;
using Domain.Models.Entities.Characters;

namespace Domain.Extensions;
public static class CharacterExtensions
{
    public static void UpdateCharacterLevel(this Character character)
    {
        var xpRequired = EntityLevelConstants.XP_REQUIRED(character.Level);

        while (character.Experience >= xpRequired)
        {
            character.Level++;
            character.Experience -= xpRequired;

            // After leveling up and adjusting current experience, calculate whether there's enough experience left for more level ups
            xpRequired = EntityLevelConstants.XP_REQUIRED(character.Level);

            //TODO: Add Publish Event to notify listeners that listen to level ups
        }
    }
}