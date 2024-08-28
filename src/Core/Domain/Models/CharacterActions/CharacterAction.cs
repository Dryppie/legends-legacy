using Domain.Models.Entities.Actors.Characters;
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
    public bool IsFinished => DateTime.UtcNow >= UpdatedAt.AddHours(OFFLINE_DURATION);

    public CharacterAction(Guid characterId, CharacterActionType characterActionType, Guid lootTableId)
    {
        CharacterId = characterId;
        CharacterActionType = characterActionType;
        LootTableId = lootTableId;
        UpdatedAt = DateTime.UtcNow;
    }

    public CharacterAction()
    {

    }
}