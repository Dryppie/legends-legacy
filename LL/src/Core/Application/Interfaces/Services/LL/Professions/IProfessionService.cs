using Domain.Models.Professions;

namespace Application.Interfaces.Services.LL.Professions;
public interface IProfessionService
{
    Task<Profession> GetOrCreateProfessionAsync(Guid characterId, ProfessionType professionType, CancellationToken cancellationToken);
    Task<int> GetProfessionLevelAsync(Guid characterId, ProfessionType professionType, CancellationToken cancellationToken);
    Task<List<Profession>> GetProfessionsAsync(Guid characterId, CancellationToken cancellationToken);
    void UpdateProfessionLevel(List<Profession> professions);
}
