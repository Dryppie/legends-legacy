using Domain.Models.Professions;

namespace Application.Interfaces.Services.LL.Professions;
public interface IProfessionService
{
    Task<bool> CanPerformProfession(Guid characterId, ProfessionType professionType, int requiredLevel, CancellationToken cancellationToken);
    Task<List<Profession>> GetProfessionsAsync(Guid characterId, CancellationToken cancellationToken);
}