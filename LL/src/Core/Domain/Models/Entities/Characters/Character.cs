using System.ComponentModel.DataAnnotations.Schema;
using Domain.Models.CharacterActions;
using Domain.Models.Colosseum;
using Domain.Models.Guilds;
using Domain.Models.Inventories;
using Domain.Models.Users;

namespace Domain.Models.Entities.Characters;
public class Character : Entity
{
    public AppUser User { get; set; } = null!;
    /// <summary>
    /// This should only ever be used in the backend, as it's used for authentication
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    public CharacterAction? CharacterAction { get; set; }
    public float Experience { get; set; } = 0;
    [NotMapped]
    public float ExperienceUntilNextLevel { get; set; }
    public int Gold { get; set; } = 0;
    public Inventory Inventory { get; set; } = null!;
    public int ArenaRating { get; set; } = 1000;
    public ICollection<ColosseumMatchResult> ColosseumMatches { get; set; } = [];
    public ArenaTicketStatus ArenaTicketStatus { get; set; } = null!;
    public Guild? Guild { get; set; }
    //public GuildMember? GuildMember { get; set; }
    //public ICollection<GuildInvite> GuildInvites { get; set; } = [];
    //public List<Effect> ActiveEffects { get; set; } = [];

    //public void AddEffect(Effect effect)
    //{
    //    ActiveEffects.Add(effect);
    //    CalculateAttributes();
    //}

    //public void RemoveEffect(Effect effect)
    //{
    //    ActiveEffects.Remove(effect);
    //    CalculateAttributes();
    //}

    //public void UpdateEffects()
    //{
    //    foreach (var effect in ActiveEffects.ToList()) // ToList() to avoid errors when modifying the list during iteration
    //    {
    //        effect.Update();

    //        if(!effect.Duration.IsActive())
    //        {
    //            RemoveEffect(effect);
    //        }
    //    }
    //}

    //public ICollection<Item> Items { get; set; } = [];
}