
namespace Domain.Models.Professions;
public interface IProfessionRepository
{
    Task<int> GetProfessionLevelAsync(Guid characterId, ProfessionType professionType, CancellationToken cancellationToken);
    Task<List<Profession>> GetProfessionsAsync(Guid characterId, CancellationToken cancellationToken);
}
