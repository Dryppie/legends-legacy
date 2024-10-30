using Domain.Models.Regions.Areas;

namespace Domain.Models.Regions;
public class Region
{
    public int Id { get; set; }
    //public ICollection<Rift> Rifts { get; set; }
    //public ICollection<Raid> Raids { get; set; }
    //public ICollection<Dungeon> Dungeons { get; set; }
    public ICollection<Area> Areas { get; set; } = [];
}
