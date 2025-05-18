namespace Domain.Models.Professions;
public interface IProfessionRepository
{
    Task<bool> CanPerformProfession(Guid characterId, ProfessionType professionType, int requiredLevel, CancellationToken cancellationToken);
}
