using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;

namespace Services.LL.Interfaces;

public interface ICreatureBuildProfileDiagnostics
{
    CreatureBuildProfileDiagnostic Create(Creature creature, Area area);
}
