namespace Domain.Models.Items.Equipments.Progression;

/// <summary>Tracks region-tier equipment earned from ordinary combat for equipment quest objectives.</summary>
public sealed class PlainEquipmentEntitlement
{
    public Guid CharacterId { get; init; }
    public string DefinitionId { get; init; } = string.Empty;
    public int Tier { get; init; }
    public int Copies { get; private set; }
    public void RecordAward(EquipmentData award)
    {
        if (award.State.DefinitionId != DefinitionId || award.State.Tier != Tier
            || award.State.Ownership.OwnerId != CharacterId || award.State.Ownership.Kind != EquipmentOwnershipKind.UnboundPersonal
            || award.State.Provenance.Kind != EquipmentAwardKind.RandomDiscovery || award.State.Rank != 0)
            throw new ArgumentException("Only ordinary regional equipment drops establish quest credit.");
        Copies = checked(Copies + 1);
    }
}

public interface IPlainEquipmentRepository
{
    Task<IReadOnlyList<PlainEquipmentEntitlement>> GetAsync(Guid characterId, CancellationToken ct);
    Task RecordAwardAsync(Guid characterId, EquipmentData award, CancellationToken ct);
}
