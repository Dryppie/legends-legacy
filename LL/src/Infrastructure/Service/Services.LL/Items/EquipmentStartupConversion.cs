using Application.UseCases.Colosseum.Commands.UpdateArenaDefenseSnapshot;
using Application.UseCases.Equipments.Commands.ConvertEquipmentOnStartup;
using Domain.Models.Attributes;
using Domain.Models.Items.Equipments.Progression;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Services.LL.Items;

public static class EquipmentStartupConversionExtensions
{
    public static Task ConvertExistingEquipmentAsync(this IServiceProvider services, CancellationToken ct = default) =>
        ActivatorUtilities.CreateInstance<EquipmentStartupConversion>(services).RunAsync(ct);
}

public sealed class EquipmentStartupConversion(IServiceScopeFactory scopes, IConfiguration configuration,
    AttributeRulesSelection rules, ILogger<EquipmentStartupConversion> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        if (!configuration.GetValue<bool>("EquipmentConversion:RunOnStartup")) return;
        var version = configuration.GetValue<int>("EquipmentConversion:TargetBalanceVersion");
        if (version != 4 || rules.Version != 18 || rules.EquipmentBalanceVersion != version
            || configuration["Combat:AbilityBalanceProfile"] != "healing-v1")
            throw new InvalidOperationException("Startup equipment conversion requires rules 18, equipment release 4, target release 4 and healing-v1.");

        await using var scope = scopes.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEquipmentStartupConversionRepository>();
        logger.LogInformation("Checking existing equipment for conversion to release {Version}.", version);
        await using var runnerLock = await repository.AcquireRunnerLockAsync(ct);
        var converted = 0;
        var refreshed = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            // Successful conversions leave this set. Always take its first page; never advance
            // an offset through a shrinking population. Fresh command scopes avoid stale tracking.
            var targets = await repository.GetTargetsAsync(version, 100, ct);
            if (targets.Count == 0) break;
            foreach (var target in targets)
            {
                await using var commandScope = scopes.CreateAsyncScope();
                try
                {
                    if (await commandScope.ServiceProvider.GetRequiredService<ISender>().Send(
                        new ConvertEquipmentOnStartupCommand(Guid.NewGuid(), target, version), ct)) converted++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    throw new InvalidOperationException(
                        $"Startup equipment conversion blocked for {target.Location} {target.ItemId} (container {target.ContainerId}). "
                        + "Committed conversions are retained; restarting retries the remaining items.", ex);
                }
            }
            logger.LogInformation("Equipment startup conversion has converted {Count} items in this run.", converted);
        }

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var defenses = await repository.GetArenaDefensesAsync(version, 100, ct);
            if (defenses.Count == 0) break;
            foreach (var characterId in defenses)
            {
                await using var commandScope = scopes.CreateAsyncScope();
                var result = await commandScope.ServiceProvider.GetRequiredService<ISender>()
                    .Send(new UpdateArenaDefenseSnapshotCommand(characterId), ct);
                if (!result.IsSuccess)
                    throw new InvalidOperationException($"Startup Arena defense refresh failed for {characterId}: {result.ErrorMessage}");
                refreshed++;
            }
        }

        var audit = await repository.AuditAsync(version, ct);
        if (!audit.Complete)
            throw new InvalidOperationException($"Startup equipment conversion verification failed: {audit.RemainingItems} older referenced items/rewards, "
                + $"{audit.UnsupportedPendingRewards} unsupported unversioned rewards, {audit.OutdatedArenaDefenses} outdated Arena defenses, "
                + $"{audit.ActiveLegacyTournamentSnapshots} active legacy tournament snapshots. "
                + "Resolve unsupported records or finish/cancel legacy tournaments, then restart. Historical snapshots have not been rewritten.");

        logger.LogInformation("Equipment release {Version} verified: converted {Converted}, refreshed {Refreshed} Arena defenses; "
            + "retained {Retained} unreferenced legacy items. Scheduled combat was not settled or paused.",
            version, converted, refreshed, audit.RetainedUnreferencedItems);
    }
}
