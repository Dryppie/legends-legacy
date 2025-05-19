using Domain.Models.Professions;

namespace Persistence.LL.Seeds.Helpers;
public static class ProfessionsSeederHelper
{
    public static List<Profession> CreateProfessions(Guid characterId)
    {
        return new List<Profession>()
        {
            // Crafting professions
            new Profession()
            {
                CharacterId = characterId,
                ProfessionType = ProfessionType.ArmorForging,
                Level = 1,
                Experience = 0
            },
            new Profession()
            {
                CharacterId = characterId,
                ProfessionType = ProfessionType.JewelryCrafting,
                Level = 1,
                Experience = 0
            },
            new Profession()
            {
                CharacterId = characterId,
                ProfessionType = ProfessionType.WeaponSmithing,
                Level = 1,
                Experience = 0
            },

            // Gathering professions
            new Profession()
            {
                CharacterId = characterId,
                ProfessionType = ProfessionType.Mining,
                Level = 1,
                Experience = 0
            },
            new Profession()
            {
                CharacterId = characterId,
                ProfessionType = ProfessionType.Woodcutting,
                Level = 1,
                Experience = 0
            },
        };
    }
}