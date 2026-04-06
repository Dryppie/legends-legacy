namespace Domain.Models.Snapshots;

public sealed class CharacterSnapshot
{
    public Guid Id { get; init; }
    public Guid CharacterId { get; init; }
    public string Name { get; init; } = default!;
    public int Level { get; init; }

    public ICollection<EntityAttributeSnapshot> BaseAttributes { get; init; } = [];

    public List<Guid> ActiveEssenceIds { get; init; } = [];

    public ICollection<EquipmentSnapshot> Equipment { get; init; } = [];
}