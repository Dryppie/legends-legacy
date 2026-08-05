using Application.Interfaces.Services.LL.Professions;
using Domain.Helpers.Constants;
using Domain.Models.Professions;

namespace Services.LL.Professions;
public class ProfessionService : IProfessionService
{
    private const int JewelryCraftingProfessionValue = 2;
    private const int WeaponSmithingProfessionValue = 3;
    private const int RetiredFishingProfessionValue = 6;
    private readonly IProfessionRepository _professionRepository;

    public ProfessionService(IProfessionRepository professionRepository)
    {
        _professionRepository = professionRepository;
    }

    public async Task<Profession> GetOrCreateProfessionAsync(Guid characterId, ProfessionType professionType, CancellationToken cancellationToken)
    {
        professionType = NormalizeProfessionType(professionType);
        var profession = await _professionRepository.GetProfessionAsync(characterId, professionType, cancellationToken);
        if (profession is not null)
        {
            profession.ExperienceUntilNextLevel = EntityLevelConstants.XP_REQUIRED(profession.Level);
            return profession;
        }

        profession = new Profession
        {
            CharacterId = characterId,
            ProfessionType = professionType,
            Level = 1,
            Experience = 0,
            ExperienceUntilNextLevel = EntityLevelConstants.XP_REQUIRED(1)
        };

        _professionRepository.AddProfession(profession);

        return profession;
    }

    public async Task<int> GetProfessionLevelAsync(Guid characterId, ProfessionType professionType, CancellationToken cancellationToken)
    {
        return await _professionRepository.GetProfessionLevelAsync(characterId, NormalizeProfessionType(professionType), cancellationToken);
    }

    public async Task<List<Profession>> GetProfessionsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var professions = await _professionRepository.GetProfessionsAsync(characterId, cancellationToken);
        var visibleProfessions = professions
            .Where(profession => !IsRetiredProfession(profession.ProfessionType))
            .ToList();

        foreach (var profession in visibleProfessions)
        {
            profession.ExperienceUntilNextLevel = EntityLevelConstants.XP_REQUIRED(profession.Level);
        }
        return visibleProfessions;
    }

    public void UpdateProfessionLevel(List<Profession> professions)
    {
        _professionRepository.UpdateProfessionLevels(professions);
    }

    private static ProfessionType NormalizeProfessionType(ProfessionType professionType)
    {
        return IsDeprecatedCraftingProfession(professionType)
            ? ProfessionType.Crafting
            : professionType;
    }

    private static bool IsDeprecatedCraftingProfession(ProfessionType professionType)
    {
        var value = (int)professionType;
        return value is JewelryCraftingProfessionValue or WeaponSmithingProfessionValue;
    }

    private static bool IsRetiredProfession(ProfessionType professionType)
    {
        return IsDeprecatedCraftingProfession(professionType) ||
            (int)professionType == RetiredFishingProfessionValue;
    }
}
