using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Domain.Models.LootTables;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.CharacterActions;
public class CharacterAction
{
    private const int OFFLINE_DURATION = 12;
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public CharacterActionType CharacterActionType { get; set; }
    public Guid LootTableId { get; set; }
    public LootTable LootTable { get; set; } = null!;
    public DateTimeOffset UpdatedAt { get; set; }
    //public bool IsCapped => DateTimeOffset.UtcNow >= UpdatedAt.AddHours(OFFLINE_DURATION);
    [NotMapped]
    public CombatResult? CombatResult { get; set; }

    public CharacterAction(Guid characterId, CharacterActionType characterActionType, Guid lootTableId)
    {
        CharacterId = characterId;
        CharacterActionType = characterActionType;
        LootTableId = lootTableId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public CharacterAction()
    {

    }
}