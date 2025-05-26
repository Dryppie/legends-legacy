using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Professions;
public class Profession
{
    public Guid CharacterId { get; set; }
    public ProfessionType ProfessionType { get; set; }
    public int Level { get; set; }
    public float Experience { get; set; }
    [NotMapped]
    public float ExperienceUntilNextLevel { get; set; }
}
