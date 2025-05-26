using Domain.Models.Entities.Characters;
using Domain.Models.Professions;

namespace Services.LL.Interfaces;
public interface ILevelingService
{
    Task UpdateCharacterLevel(Character entity);
    Task UpdateProfessionLevel(Profession profession);
}
