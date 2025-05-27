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

    public async Task UpdateProfessionLevelAsync(List<Profession> professions, CancellationToken cancellationToken)
    {
        await _professionRepository.UpdateProfessionLevelsAsync(professions, cancellationToken);
    }
}
