using Domain.Models.Professions;
using Services.LL.Interfaces;

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
}
