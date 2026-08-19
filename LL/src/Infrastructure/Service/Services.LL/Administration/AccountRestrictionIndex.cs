using System.Collections.Immutable;
using Application.Interfaces.Services.LL.Administration;
using Domain.Models.Administration;

namespace Services.LL.Administration;

public sealed class AccountRestrictionIndex(TimeProvider timeProvider)
    : IAccountRestrictionIndex
{
    private ImmutableDictionary<Guid, ImmutableArray<AccountRestriction>> _restrictions =
        ImmutableDictionary<Guid, ImmutableArray<AccountRestriction>>.Empty;
    private long _refreshedAtUnixMilliseconds;

    public DateTimeOffset? RefreshedAt
    {
        get
        {
            var milliseconds = Interlocked.Read(ref _refreshedAtUnixMilliseconds);
            return milliseconds == 0
                ? null
                : DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        }
    }

    public AccountAccessSnapshot Get(Guid accountId)
    {
        var snapshot = Volatile.Read(ref _restrictions);
        return snapshot.TryGetValue(accountId, out var restrictions)
            ? AccountAccessSnapshot.From(restrictions, timeProvider.GetUtcNow())
            : AccountAccessSnapshot.Unrestricted;
    }

    public void Replace(
        IEnumerable<AccountRestriction> restrictions,
        DateTimeOffset refreshedAt)
    {
        var snapshot = restrictions
            .GroupBy(x => x.AccountId)
            .ToImmutableDictionary(
                group => group.Key,
                group => group.OrderByDescending(x => x.CreatedAt).ToImmutableArray());
        Volatile.Write(ref _restrictions, snapshot);
        Interlocked.Exchange(
            ref _refreshedAtUnixMilliseconds,
            refreshedAt.ToUnixTimeMilliseconds());
    }

    public async Task RefreshAsync(
        IAdministrationRepository administration,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var restrictions = await administration.GetActiveRestrictionsAsync(
            now,
            cancellationToken);
        Replace(restrictions, now);
    }
}
