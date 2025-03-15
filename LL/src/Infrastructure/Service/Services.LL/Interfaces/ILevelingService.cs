using Domain.Models.Entities.Characters;

namespace Services.LL.Interfaces;
public interface ILevelingService
{
    Task UpdateCharacterLevel(Character entity);
}
