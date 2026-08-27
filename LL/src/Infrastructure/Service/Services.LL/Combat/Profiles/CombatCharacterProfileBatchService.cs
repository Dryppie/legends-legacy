using Application.Interfaces.Services.LL.CombatProfiles;

namespace Services.LL.Combat.Profiles;

public sealed class CombatCharacterProfileBatchService(
    ICombatCharacterProfileService profiles,
    ICombatCharacterProfileCatalogService catalog) : ICombatCharacterProfileBatchService
{
    public async Task<CombatCharacterProfileBatchGenerationReport> GenerateCatalogAsync(
        CombatCharacterProfileBatchGenerationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Requests);
        if (request.Requests.Count is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "A profile batch must contain between one and 100 scenarios.");
        }

        var generated = new List<CombatCharacterProfileGenerationReport>(request.Requests.Count);
        foreach (var scenario in request.Requests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            generated.Add(await profiles.GenerateAsync(scenario, cancellationToken));
        }

        var validation = await catalog.ValidateAsync(
            new CombatCharacterProfileCatalogDocument(
                JsonCombatCharacterProfileCatalogService.SchemaVersion,
                JsonCombatCharacterProfileCatalogService.CatalogVersion,
                generated),
            cancellationToken);
        return new CombatCharacterProfileBatchGenerationReport(
            request.Requests.Count,
            validation);
    }
}
