using System.ComponentModel.DataAnnotations.Schema;
using Domain.Models.Entities.Characters;

namespace Domain.Models.Colosseum;
public class ArenaTicketStatus
{
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public int CurrentTickets { get; set; }
    public DateTimeOffset LastTicketUpdate { get; set; }
    [NotMapped]
    public int MaxTickets { get; } = 5;
}
