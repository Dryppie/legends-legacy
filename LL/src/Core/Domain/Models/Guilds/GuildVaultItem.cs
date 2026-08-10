using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments;

namespace Domain.Models.Guilds;

public class GuildVaultItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Guid EquipmentInstanceId { get; set; }
    public EquipmentInstance EquipmentInstance { get; set; } = null!;
    public Guid DonatedByCharacterId { get; set; }
    public Character DonatedByCharacter { get; set; } = null!;
    public DateTimeOffset DonatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? BorrowedByCharacterId { get; set; }
    public Character? BorrowedByCharacter { get; set; }
    public DateTimeOffset? BorrowedAt { get; set; }
}
