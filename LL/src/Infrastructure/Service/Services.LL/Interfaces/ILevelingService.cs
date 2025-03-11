using Domain.Models.Entities.Characters;

namespace Services.LL.Interfaces;
public interface ILevelingService
{
    void UpdateCharacterLevel(Character entity);
}
