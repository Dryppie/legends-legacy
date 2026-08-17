namespace API.Chat.Hubs.Presence;

using Microsoft.Extensions.Options;

public sealed class RedisChatPresenceLeaseWorker(
    RedisChatPresenceTracker presence,
    IOptions<RedisChatPresenceOptions> options,
    ILogger<RedisChatPresenceLeaseWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var renewalInterval = options.Value.LeaseRenewalInterval;
        if (renewalInterval <= TimeSpan.Zero || renewalInterval >= options.Value.LeaseDuration)
        {
            throw new InvalidOperationException(
                "ChatPresence:LeaseRenewalInterval must be positive and shorter than LeaseDuration.");
        }

        using var timer = new PeriodicTimer(renewalInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await presence.RenewLocalConnectionsAsync();
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Unable to renew Redis chat presence leases.");
            }
        }
    }
}
