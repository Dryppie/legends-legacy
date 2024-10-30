using Domain.Models.Entities.Creatures;

namespace Domain.Models.Regions.Areas;
public class Area
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Creature> Creatures { get; set; } = [];
}
