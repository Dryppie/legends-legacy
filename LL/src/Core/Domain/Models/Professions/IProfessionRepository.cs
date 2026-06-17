
namespace Domain.Models.Professions;
public interface IProfessionRepository
{
    Task<Profession?> GetProfessionAsync(Guid characterId, ProfessionType professionType, CancellationToken cancellationToken);
    Task<int> GetProfessionLevelAsync(Guid characterId, ProfessionType professionType, CancellationToken cancellationToken);
    Task<List<Profession>> GetProfessionsAsync(Guid characterId, CancellationToken cancellationToken);
    void AddProfession(Profession profession);
    void UpdateProfessionLevels(List<Profession> professions);
}
