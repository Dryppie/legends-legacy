using Application.Common.Interfaces;
using Domain.Models.Administration;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Administration;

public sealed class AdministrationRepository(IDbContext context) : IAdministrationRepository
{
    public Task<AdminAction?> GetActionAsync(Guid operationId, CancellationToken cancellationToken) =>
        context.AdminActions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == operationId, cancellationToken);

    public Task<AccountRestriction?> GetRestrictionAsync(
        Guid restrictionId,
        CancellationToken cancellationToken) =>
        context.AccountRestrictions
            .SingleOrDefaultAsync(x => x.Id == restrictionId, cancellationToken);

    public Task<AccountRestriction?> GetActiveAccountBanAsync(
        Guid accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        context.AccountRestrictions
            .AsNoTracking()
            .Where(x => x.AccountId == accountId &&
                        x.RestrictionType == AccountRestrictionType.Ban &&
                        x.RevokedAt == null &&
                        (x.ExpiresAt == null || x.ExpiresAt > now))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PlayerAdministrationSnapshot?> GetPlayerByAccountIdAsync(
        Guid accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var player = await BasePlayers()
            .Where(x => x.AccountId == accountId)
            .SingleOrDefaultAsync(cancellationToken);
        return player is null
            ? null
            : await AddActiveBanAsync(player, now, cancellationToken);
    }

    public async Task<PlayerAdministrationSnapshot?> GetPlayerByCharacterIdAsync(
        Guid characterId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var player = await BasePlayers()
            .Where(x => x.CharacterId == characterId)
            .SingleOrDefaultAsync(cancellationToken);
        return player is null
            ? null
            : await AddActiveBanAsync(player, now, cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerAdministrationSnapshot>> SearchPlayersAsync(
        string query,
        int limit,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var trimmed = query.Trim();
        var resultLimit = Math.Clamp(limit, 1, 50);
        var players = BasePlayers();

        if (Guid.TryParse(trimmed, out var id))
        {
            players = players.Where(x => x.AccountId == id || x.CharacterId == id);
        }
        else
        {
            var normalized = IdentityNormalizer.NormalizeOptional(trimmed);
            if (normalized is null || normalized.Length < 2)
            {
                return [];
            }

            players = players.Where(x =>
                x.NormalizedName.Contains(normalized) ||
                (x.NormalizedEmail != null && x.NormalizedEmail.Contains(normalized)) ||
                x.AccountLabel.ToUpper().Contains(normalized));
        }

        var matches = await players
            .OrderBy(x => x.CharacterName)
            .Take(resultLimit)
            .ToListAsync(cancellationToken);
        if (matches.Count == 0)
        {
            return [];
        }

        var accountIds = matches.Select(x => x.AccountId).ToArray();
        var activeBans = await context.AccountRestrictions
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.AccountId) &&
                        x.RestrictionType == AccountRestrictionType.Ban &&
                        x.RevokedAt == null &&
                        (x.ExpiresAt == null || x.ExpiresAt > now))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var bansByAccount = activeBans
            .GroupBy(x => x.AccountId)
            .ToDictionary(x => x.Key, x => x.First());

        return matches
            .Select(player => ToSnapshot(
                player,
                bansByAccount.GetValueOrDefault(player.AccountId)))
            .ToList();
    }

    public void AddAction(AdminAction action) => context.AdminActions.Add(action);

    public void AddRestriction(AccountRestriction restriction) =>
        context.AccountRestrictions.Add(restriction);

    private IQueryable<PlayerRow> BasePlayers() =>
        from user in context.Users.AsNoTracking()
        join character in context.Characters.AsNoTracking() on user.Id equals character.UserId
        select new PlayerRow(
            user.Id,
            character.Id,
            user.Username,
            user.Email,
            user.NormalizedEmail,
            character.Name,
            character.NormalizedName,
            character.Level,
            user.CreatedUtc);

    private async Task<PlayerAdministrationSnapshot> AddActiveBanAsync(
        PlayerRow player,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var ban = await GetActiveAccountBanAsync(player.AccountId, now, cancellationToken);
        return ToSnapshot(player, ban);
    }

    private static PlayerAdministrationSnapshot ToSnapshot(
        PlayerRow player,
        AccountRestriction? ban) =>
        new(
            player.AccountId,
            player.CharacterId,
            player.AccountLabel,
            player.Email,
            player.CharacterName,
            player.CharacterLevel,
            player.CreatedUtc,
            ban?.Id,
            ban?.Reason,
            ban?.ExpiresAt);

    private sealed record PlayerRow(
        Guid AccountId,
        Guid CharacterId,
        string AccountLabel,
        string? Email,
        string? NormalizedEmail,
        string CharacterName,
        string NormalizedName,
        int CharacterLevel,
        DateTime CreatedUtc);
}
