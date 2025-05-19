using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Professions;

namespace Services.LL.Professions;
public class ProfessionService : IProfessionService
{
    private readonly IProfessionRepository _professionRepository;

    public ProfessionService(IProfessionRepository professionRepository)
    {
        _professionRepository = professionRepository;
    }
    public async Task<bool> CanPerformProfession(Guid characterId, ProfessionType professionType, int requiredLevel, CancellationToken cancellationToken)
    {
        return await _professionRepository.CanPerformProfession(characterId, professionType, requiredLevel, cancellationToken);
    }

    public async Task<List<Profession>> GetProfessionsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _professionRepository.GetProfessionsAsync(characterId, cancellationToken);
    }
}
