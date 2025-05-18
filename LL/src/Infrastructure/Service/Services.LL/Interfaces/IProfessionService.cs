using Domain.Models.Professions;

namespace Services.LL.Interfaces;
public interface IProfessionService
{
    Task<bool> CanPerformProfession(Guid characterId, ProfessionType professionType, int requiredLevel, CancellationToken cancellationToken);
}
