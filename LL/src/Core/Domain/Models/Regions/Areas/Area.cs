using Domain.Models.Entities.Creatures;

namespace Domain.Models.Regions.Areas;
public class Area
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<Creature> Creatures { get; set; } = [];
}
