using Domain.Models.Regions.Areas;

namespace Domain.Models.Entities.Creatures;
public class Creature : Entity
{
    public ICollection<Area> Area { get; set; } = [];
}