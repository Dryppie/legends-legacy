using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;

namespace Services.LL.Interfaces;

public interface ICreatureScaler
{
    void ApplyScaling(Creature creature, Area area);
}
