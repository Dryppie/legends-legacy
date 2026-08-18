using Application.Common.Interfaces;
using Domain.Models.Administration;
using Domain.Models.Entities.Characters;
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
        var player = await SelectPlayerRows(
                BasePlayers().Where(x => x.UserId == accountId))
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
        var player = await SelectPlayerRows(
                BasePlayers().Where(x => x.Id == characterId))
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
            players = players.Where(x => x.UserId == id || x.Id == id);
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
                (x.User.NormalizedEmail != null && x.User.NormalizedEmail.Contains(normalized)) ||
                x.User.Username.ToUpper().Contains(normalized));
        }

        var matches = await SelectPlayerRows(
                players
                    .OrderBy(x => x.Name)
                    .Take(resultLimit))
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

    public async Task<IReadOnlyList<AdministrationItemCatalogEntry>> SearchItemsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var trimmed = query.Trim();
        if (trimmed.Length < 2)
        {
            return [];
        }

        var resultLimit = Math.Clamp(limit, 1, 50);
        var normalized = trimmed.ToUpper();
        return await context.ItemBases
            .AsNoTracking()
            .Where(x => x.Id.ToUpper().Contains(normalized) ||
                        x.Name.ToUpper().Contains(normalized))
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Take(resultLimit)
            .Select(x => new AdministrationItemCatalogEntry(
                x.Id,
                x.Name,
                x.Description,
                x.ItemType,
                x.Rarity,
                x.Stackable,
                x.IsBound))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdministrationHistoryEntry>> GetHistoryAsync(
        Guid accountId,
        Guid characterId,
        int limit,
        CancellationToken cancellationToken) =>
        await context.AdminActions
            .AsNoTracking()
            .Where(x => x.TargetAccountId == accountId ||
                        x.TargetCharacterId == characterId)
            .OrderByDescending(x => x.OccurredAt)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(x => new AdministrationHistoryEntry(
                x.Id,
                x.ActionType,
                x.Permission,
                x.ActorSubject,
                x.ActorDisplayName,
                x.TargetAccountId,
                x.TargetCharacterId,
                x.TargetResourceId,
                x.Reason,
                x.InternalNotes,
                x.DetailsJson,
                x.RiskLevel,
                x.OccurredAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AdministrationHistoryEntry>> GetAuditAsync(
        AdministrationAuditQuery query,
        CancellationToken cancellationToken)
    {
        var actions = context.AdminActions.AsNoTracking().AsQueryable();

        if (query.From.HasValue)
        {
            actions = actions.Where(x => x.OccurredAt >= query.From.Value);
        }
        if (query.To.HasValue)
        {
            actions = actions.Where(x => x.OccurredAt <= query.To.Value);
        }
        if (query.ActionType.HasValue)
        {
            actions = actions.Where(x => x.ActionType == query.ActionType.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Actor))
        {
            var actor = query.Actor.Trim().ToUpper();
            actions = actions.Where(x =>
                x.ActorSubject.ToUpper().Contains(actor) ||
                x.ActorDisplayName.ToUpper().Contains(actor));
        }
        if (!string.IsNullOrWhiteSpace(query.Permission))
        {
            var permission = query.Permission.Trim().ToUpper();
            actions = actions.Where(x => x.Permission.ToUpper() == permission);
        }
        if (!string.IsNullOrWhiteSpace(query.Reference))
        {
            var reference = query.Reference.Trim().ToUpper();
            actions = query.IncludeInternalNotesInReference
                ? actions.Where(x =>
                    x.Reason.ToUpper().Contains(reference) ||
                    (x.InternalNotes != null && x.InternalNotes.ToUpper().Contains(reference)))
                : actions.Where(x => x.Reason.ToUpper().Contains(reference));
        }
        if (query.RiskLevel.HasValue)
        {
            actions = actions.Where(x => x.RiskLevel == query.RiskLevel.Value);
        }
        if (query.OperationId.HasValue)
        {
            actions = actions.Where(x => x.Id == query.OperationId.Value);
        }

        var accountIds = query.TargetAccountIds.ToArray();
        var characterIds = query.TargetCharacterIds.ToArray();
        if (accountIds.Length > 0 || characterIds.Length > 0 || query.TargetResourceId.HasValue)
        {
            actions = actions.Where(x =>
                (x.TargetAccountId.HasValue && accountIds.Contains(x.TargetAccountId.Value)) ||
                (x.TargetCharacterId.HasValue && characterIds.Contains(x.TargetCharacterId.Value)) ||
                (query.TargetResourceId.HasValue &&
                 x.TargetResourceId == query.TargetResourceId.Value));
        }

        if (query.BeforeOccurredAt.HasValue && query.BeforeOperationId.HasValue)
        {
            var beforeAt = query.BeforeOccurredAt.Value;
            var beforeId = query.BeforeOperationId.Value;
            actions = actions.Where(x =>
                x.OccurredAt < beforeAt ||
                (x.OccurredAt == beforeAt && x.Id.CompareTo(beforeId) < 0));
        }

        return await actions
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Take(Math.Clamp(query.Limit, 1, 101))
            .Select(x => new AdministrationHistoryEntry(
                x.Id,
                x.ActionType,
                x.Permission,
                x.ActorSubject,
                x.ActorDisplayName,
                x.TargetAccountId,
                x.TargetCharacterId,
                x.TargetResourceId,
                x.Reason,
                x.InternalNotes,
                x.DetailsJson,
                x.RiskLevel,
                x.OccurredAt))
            .ToListAsync(cancellationToken);
    }

    public void AddAction(AdminAction action) => context.AdminActions.Add(action);

    public void AddRestriction(AccountRestriction restriction) =>
        context.AccountRestrictions.Add(restriction);

    private IQueryable<Character> BasePlayers() =>
        context.Characters.AsNoTracking();

    private static IQueryable<PlayerRow> SelectPlayerRows(IQueryable<Character> players) =>
        players.Select(character => new PlayerRow(
            character.UserId,
            character.Id,
            character.User.Username,
            character.User.Email,
            character.User.NormalizedEmail,
            character.Name,
            character.NormalizedName,
            character.Level,
            character.User.CreatedUtc));

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
