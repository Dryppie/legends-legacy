namespace Domain.Models.Items.Equipments;

public sealed record EquipmentEquipResult(bool Succeeded, string? ErrorMessage = null)
{
    public static EquipmentEquipResult Success() => new(true);

    public static EquipmentEquipResult Fail(string errorMessage) => new(false, errorMessage);
}
