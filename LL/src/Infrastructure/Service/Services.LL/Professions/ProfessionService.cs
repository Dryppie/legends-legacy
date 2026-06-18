using Application.Interfaces.Services.LL.Professions;
using Domain.Helpers.Constants;
using Domain.Models.Professions;

namespace Services.LL.Professions;
public class ProfessionService : IProfessionService
{
    private readonly IProfessionRepository _professionRepository;

    public ProfessionService(IProfessionRepository professionRepository)
    {
        _professionRepository = professionRepository;
    }

    public async Task<Profession> GetOrCreateProfessionAsync(Guid characterId, ProfessionType professionType, CancellationToken cancellationToken)
    {
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
        return await _professionRepository.GetProfessionLevelAsync(characterId, professionType, cancellationToken);
    }

    public async Task<List<Profession>> GetProfessionsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var professions = await _professionRepository.GetProfessionsAsync(characterId, cancellationToken);
        foreach (var profession in professions)
        {
            profession.ExperienceUntilNextLevel = EntityLevelConstants.XP_REQUIRED(profession.Level);
        }
        return professions;
    }

    public void UpdateProfessionLevel(List<Profession> professions)
    {
        _professionRepository.UpdateProfessionLevels(professions);
    }
}
