using System.Globalization;
using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Queries.GetAdministrationAudit;

public sealed record GetAdministrationAuditQuery(
    string? Cursor,
    int Take,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Source,
    string? ActionType,
    string? Actor,
    string? Permission,
    string? Reference,
    string? RiskLevel,
    string? Target,
    Guid? OperationId,
    bool IncludeInternalNotes)
    : IQuery<Response<AdministrationAuditPageDto>>;

public sealed class GetAdministrationAuditQueryHandler(
    ILiveOpsService liveOps,
    IChatModerationGateway chat)
    : IRequestHandler<GetAdministrationAuditQuery, Response<AdministrationAuditPageDto>>
{
    public async Task<Response<AdministrationAuditPageDto>> Handle(
        GetAdministrationAuditQuery request,
        CancellationToken cancellationToken)
    {
        if (request.From > request.To)
        {
            return Response<AdministrationAuditPageDto>.Fail(
                "The audit start date must not be after the end date.");
        }
        if (!AuditSource.TryParse(request.Source, out var source))
        {
            return Response<AdministrationAuditPageDto>.Fail(
                "Audit source must be All, Game, or Chat.");
        }
        if (!AuditCursor.TryDecode(request.Cursor, out var cursor))
        {
            return Response<AdministrationAuditPageDto>.Fail(
                "The audit cursor is invalid or expired.");
        }
        if (!TryParseRiskLevel(request.RiskLevel, out var riskLevel))
        {
            return Response<AdministrationAuditPageDto>.Fail(
                "Audit risk level must be Normal, Permanent, or HighValue.");
        }

        var take = Math.Clamp(request.Take, 10, 100);
        var target = await ResolveTargetAsync(request.Target, cancellationToken);
        if (target.HasFilter && target.AccountIds.Count == 0 && target.CharacterIds.Count == 0)
        {
            return Response<AdministrationAuditPageDto>.Success(
                new AdministrationAuditPageDto([], null, []));
        }

        var gameActionType = Enum.TryParse<AdminActionType>(
            request.ActionType,
            true,
            out var parsedGameActionType)
            ? parsedGameActionType
            : (AdminActionType?)null;
        var gameActionTypeCannotMatch = !string.IsNullOrWhiteSpace(request.ActionType) &&
            !gameActionType.HasValue;
        var chatActionTypeCannotMatch = !string.IsNullOrWhiteSpace(request.ActionType) &&
            !string.Equals(request.ActionType.Trim(), "Muted", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.ActionType.Trim(), "Unmuted", StringComparison.OrdinalIgnoreCase);
        var chatPermissionCannotMatch = !string.IsNullOrWhiteSpace(request.Permission) &&
            !string.Equals(
                request.Permission.Trim(),
                AdministrationPermissions.ChatModeration,
                StringComparison.OrdinalIgnoreCase);
        var chatRiskCannotMatch = riskLevel is not null and not AdministrationRiskLevel.Normal;

        var gameTask = source.IncludeGame && !gameActionTypeCannotMatch
            ? liveOps.GetAuditAsync(
                new AdministrationAuditQuery(
                    request.From,
                    request.To,
                    gameActionType,
                    request.Actor,
                    request.Permission,
                    request.Reference,
                    request.IncludeInternalNotes,
                    riskLevel,
                    request.OperationId,
                    target.AccountIds,
                    target.CharacterIds,
                    target.ResourceId,
                    cursor?.OccurredAt,
                    cursor?.OperationId,
                    take + 1),
                cancellationToken)
            : Task.FromResult<IReadOnlyList<AdministrationHistoryEntry>>([]);
        var chatTask = source.IncludeChat &&
            !chatActionTypeCannotMatch &&
            !chatPermissionCannotMatch &&
            !chatRiskCannotMatch
            ? chat.GetAuditAsync(
                new ChatModerationAuditGatewayQuery(
                    request.From,
                    request.To,
                    request.ActionType,
                    request.Actor,
                    request.Reference,
                    request.OperationId,
                    target.CharacterIds,
                    target.ResourceId,
                    cursor?.OccurredAt,
                    cursor?.OperationId,
                    take + 1),
                cancellationToken)
            : Task.FromResult(new ChatModerationAuditGatewayResult(true, [], string.Empty));

        await Task.WhenAll(gameTask, chatTask);
        var chatResult = await chatTask;
        if (source.ChatOnly && !chatResult.IsSuccess)
        {
            return Response<AdministrationAuditPageDto>.Fail(chatResult.ErrorMessage);
        }

        var entries = (await gameTask)
            .Select(x => new AdministrationAuditEntryDto(
                x.OperationId,
                "Game",
                x.ActionType.ToString(),
                x.Permission,
                x.ActorSubject,
                x.ActorDisplayName,
                x.TargetAccountId,
                x.TargetCharacterId,
                x.TargetResourceId,
                x.Reason,
                request.IncludeInternalNotes ? x.InternalNotes : null,
                x.DetailsJson,
                x.RiskLevel.ToString(),
                "Completed",
                x.OccurredAt))
            .Concat(chatResult.Entries.Select(x => new AdministrationAuditEntryDto(
                x.OperationId,
                "Chat",
                x.ActionType,
                AdministrationPermissions.ChatModeration,
                x.ActorSubject,
                x.ActorDisplayName,
                null,
                x.CharacterId,
                x.RestrictionId,
                x.Reason,
                null,
                "{}",
                AdministrationRiskLevel.Normal.ToString(),
                "Completed",
                x.OccurredAt)))
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.OperationId)
            .ToList();

        var hasMore = entries.Count > take;
        var pageEntries = entries.Take(take).ToList();
        var nextCursor = hasMore && pageEntries.Count > 0
            ? AuditCursor.Encode(pageEntries[^1].OccurredAt, pageEntries[^1].OperationId)
            : null;
        var unavailable = source.IncludeChat && !chatResult.IsSuccess
            ? new[] { "Chat" }
            : [];

        return Response<AdministrationAuditPageDto>.Success(
            new AdministrationAuditPageDto(pageEntries, nextCursor, unavailable));
    }

    private static bool TryParseRiskLevel(
        string? value,
        out AdministrationRiskLevel? riskLevel)
    {
        riskLevel = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (!Enum.TryParse<AdministrationRiskLevel>(value, true, out var parsed))
        {
            return false;
        }

        riskLevel = parsed;
        return true;
    }

    private async Task<AuditTarget> ResolveTargetAsync(
        string? value,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new AuditTarget(false, [], [], null);
        }

        var trimmed = value.Trim();
        if (Guid.TryParse(trimmed, out var id))
        {
            return new AuditTarget(true, [id], [id], id);
        }

        var players = await liveOps.SearchPlayersAsync(trimmed, 50, cancellationToken);
        return new AuditTarget(
            true,
            players.Select(x => x.AccountId).Distinct().ToArray(),
            players.Select(x => x.CharacterId).Distinct().ToArray(),
            null);
    }

    private sealed record AuditTarget(
        bool HasFilter,
        IReadOnlyCollection<Guid> AccountIds,
        IReadOnlyCollection<Guid> CharacterIds,
        Guid? ResourceId);

    private sealed record AuditSource(bool IncludeGame, bool IncludeChat)
    {
        public bool ChatOnly => IncludeChat && !IncludeGame;

        public static bool TryParse(string? value, out AuditSource source)
        {
            switch (value?.Trim().ToUpperInvariant())
            {
                case null or "" or "ALL":
                    source = new AuditSource(true, true);
                    return true;
                case "GAME":
                    source = new AuditSource(true, false);
                    return true;
                case "CHAT":
                    source = new AuditSource(false, true);
                    return true;
                default:
                    source = new AuditSource(false, false);
                    return false;
            }
        }
    }

    private sealed record AuditCursor(DateTimeOffset OccurredAt, Guid OperationId)
    {
        public static string Encode(DateTimeOffset occurredAt, Guid operationId)
        {
            var value = string.Create(
                CultureInfo.InvariantCulture,
                $"{occurredAt.UtcTicks}:{operationId:N}");
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static bool TryDecode(string? value, out AuditCursor? cursor)
        {
            cursor = null;
            if (string.IsNullOrWhiteSpace(value)) return true;

            try
            {
                var normalized = value.Replace('-', '+').Replace('_', '/');
                normalized = normalized.PadRight(
                    normalized.Length + ((4 - normalized.Length % 4) % 4),
                    '=');
                var decoded = System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(normalized));
                var separator = decoded.IndexOf(':');
                if (separator <= 0 ||
                    !long.TryParse(decoded[..separator], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) ||
                    !Guid.TryParseExact(decoded[(separator + 1)..], "N", out var operationId))
                {
                    return false;
                }

                cursor = new AuditCursor(new DateTimeOffset(ticks, TimeSpan.Zero), operationId);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}
