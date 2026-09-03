namespace Domain.Models.Items.Equipments.Progression;

public enum StarterEquipmentGrantKind { FirstWeapon, ReadyForRoad }

/// <summary>Durable entitlement, retained even after the awarded items leave inventory.</summary>
public sealed class StarterEquipmentGrant
{
    private StarterEquipmentGrant() { }

    public StarterEquipmentGrant(Guid characterId, StarterEquipmentGrantKind kind,
        IReadOnlyList<EquipmentData> equipment, DateTimeOffset grantedAtUtc)
    {
        if (characterId == Guid.Empty || !Enum.IsDefined(kind) || equipment.Count == 0
            || equipment.Select(x => x.State.Id).Distinct().Count() != equipment.Count
            || equipment.Any(x => x.State.Ownership.OwnerId != characterId
                || x.State.Ownership.Kind != EquipmentOwnershipKind.BoundPersonal
                || x.State.Provenance.Kind != EquipmentAwardKind.QuestReward
                || x.State.Tier != 1 || x.State.Rank != 0 || x.State.ActiveStyleId != null
                || x.State.BaseSalvageScrap != 0 || x.State.Investments.Count != 0))
            throw new ArgumentException("Invalid starter equipment grant.");
        CharacterId = characterId;
        Kind = kind;
        Equipment = Array.AsReadOnly(equipment.ToArray());
        GrantedAtUtc = grantedAtUtc;
    }

    public Guid CharacterId { get; private set; }
    public StarterEquipmentGrantKind Kind { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }
    public IReadOnlyList<EquipmentData> Equipment { get; private set; } = [];

    public bool MatchesSelection(IReadOnlyList<string> definitionIds) =>
        Equipment.Select(x => x.State.DefinitionId).Order(StringComparer.Ordinal)
            .SequenceEqual(definitionIds.Order(StringComparer.Ordinal));
}
