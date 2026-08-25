namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonSigilAssemblyService
{
    Task<DungeonSigilAssemblyOperationResult> AssembleAsync(
        Guid characterId,
        string dungeonId,
        int quantity,
        CancellationToken cancellationToken);
}

public sealed record DungeonSigilAssemblyResult(
    string DungeonId,
    string SigilItemId,
    string SigilName,
    int QuantityAssembled,
    int InventoryQuantity,
    long SigilFragmentsRemaining);

public sealed record DungeonSigilAssemblyOperationResult(
    bool Succeeded,
    string? Error,
    DungeonSigilAssemblyResult? Value)
{
    public static DungeonSigilAssemblyOperationResult Success(DungeonSigilAssemblyResult value) =>
        new(true, null, value);

    public static DungeonSigilAssemblyOperationResult Fail(string error) =>
        new(false, error, null);
}
