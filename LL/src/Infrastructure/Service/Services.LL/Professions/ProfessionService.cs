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
    public async Task<int> GetProfessionLevelAsync(Guid characterId, ProfessionType professionType, CancellationToken cancellationToken)
    {
        return await _professionRepository.GetProfessionLevelAsync(characterId, professionType, cancellationToken);
    }

    public async Task<List<Profession>> GetProfessionsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _professionRepository.GetProfessionsAsync(characterId, cancellationToken);
    }
}
