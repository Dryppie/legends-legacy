using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures;

namespace Application.UseCases._AdminDashboard.Creatures.Dtos
{
    public class CreatureDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<EntityAttribute> BaseAttributes { get; set; } = [];
        public int ExperienceReward { get; set; }
        public int Level { get; set; }

        public void UpdateProperties(Creature creature)
        {
            creature.Name = Name;
            creature.BaseAttributes = BaseAttributes;
            creature.ExperienceReward = ExperienceReward;
            creature.Level = Level;
        }
    }
}
