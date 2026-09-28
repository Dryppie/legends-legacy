using System.Security.Cryptography;
using System.Text.Json;
using Application.Interfaces.Services.LL.Administration;
using Common.Primitives;
using Domain.Models.Administration;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Services.LL.Administration;

namespace API.LiveOps.Previews;

public sealed class LiveOpsActionPreviewService(
    IDbContextFactory<LLDbContext> contextFactory,
    ILiveOpsService liveOps,
    IChatModerationGateway chat,
    IOptions<LiveOpsOptions> options,
    TimeProvider timeProvider,
    ICompensationPackageService? packages = null)
{
    private readonly LiveOpsOptions _options = options.Value;

    public async Task<Response<ActionPreviewDto>> CreatePackageGrantAsync(Guid operationId, Guid characterId,
        Guid packageId, int version, AdministrationActor actor, string reason, string? notes, CancellationToken ct)
    {
        var validation = ValidateCommon(operationId, reason, notes);
        if (validation is not null) return Response<ActionPreviewDto>.Fail(validation);
        var prepared = await packages!.PrepareAsync(operationId, characterId, packageId, version, ct);
        if (!prepared.IsSuccess || prepared.Data is null) return Response<ActionPreviewDto>.Fail(prepared.ErrorMessage);
        var plan = prepared.Data;
        var fields = new List<ActionPreviewField> { new("Package", $"{plan.Package.Name} · version {version}"), new("Purpose", plan.Package.Purpose), new("Reason", reason.Trim()), new("Internal notes", Normalize(notes) ?? "None") };
        for (var i = 0; i < plan.Items.Count; i++)
        {
            var item = plan.Items[i]; var line = plan.Package.Items[i]; var data = item.Equipment;
            fields.Add(new($"Item {i + 1}", $"{line.Quantity} × {item.ItemBase.Name} ({item.ItemBase.Id}) · {(data is not null || item.ItemBase.IsBound ? "Bound" : "Unbound")}"));
            if (data is not null)
            {
                fields.Add(new($"Equipment {i + 1}", $"{data.DisplayName} · Tier {data.State.Tier} / Rank {data.State.Rank} · {data.State.ActiveStyleId ?? "Plain"} · Balance version {data.State.BalanceVersion}"));
                fields.Add(new($"Stats {i + 1}", string.Join(", ", data.Stats.OrderBy(x => x.Key).Select(x => $"{x.Key}: {x.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)}"))));
            }
        }
        return await PersistAsync(operationId, AdminActionPreviewKinds.CompensationPackage, actor, characterId,
            PackageRequestHash(characterId, packageId, version, reason, notes), PackageStateHash(plan),
            new PreviewContext(characterId, null, PackageId: packageId, PackageVersion: version),
            "Grant compensation package", plan.Player.CharacterName, "HighValue", plan.Player.CharacterName, fields,
            ["All item lines are granted in one transaction. This creates compensation; it does not replay a missing reward entitlement.",
             "Equipment is bound to the recipient. Granted ranks carry no refundable investment; base salvage is zero."], ct);
    }
    public Task<PreviewSubmissionResult> BeginPackageGrantAsync(Guid token, Guid operationId, Guid characterId,
        Guid packageId, int version, AdministrationActor actor, string reason, string? notes, CancellationToken ct) =>
        BeginAsync(token, operationId, AdminActionPreviewKinds.CompensationPackage, characterId, actor,
            PackageRequestHash(characterId, packageId, version, reason, notes), ct);
    private static string PackageRequestHash(Guid characterId, Guid packageId, int version, string reason, string? notes) =>
        RequestHash(AdminActionPreviewKinds.CompensationPackage, new { CharacterId = characterId, PackageId = packageId,
            Version = version, Reason = NormalizeRequired(reason), Notes = Normalize(notes) });
    private static string PackageStateHash(CompensationPackagePlan plan) => StateHash(new { plan.Package,
        plan.Player.AccountId, plan.Player.CharacterId, Items = plan.Items.Select(x => new { x.ItemBase.Id, x.ItemBase.Name,
            x.ItemBase.Stackable, x.ItemBase.IsBound, x.ItemBase.Rarity, x.Equipment }).ToArray() });

    public async Task<Response<ActionPreviewDto>> CreateAlphaSignetGrantAsync(Guid operationId, Guid characterId,
        AdministrationActor actor, int quantity, string reason, CancellationToken ct)
    {
        var validation = ValidateCommon(operationId, reason, null);
        if (validation is not null) return Response<ActionPreviewDto>.Fail(validation);
        if (quantity is < 1 or > 1200 || reason.Trim().Length < 3)
            return Response<ActionPreviewDto>.Fail("Supply 1–1200 Signets and a reason of at least three characters.");
        var player = await liveOps.GetPlayerAsync(characterId, ct);
        if (player is null) return Response<ActionPreviewDto>.Fail("The character is unavailable.");
        await using var database = await contextFactory.CreateDbContextAsync(ct);
        if (!await database.Users.AnyAsync(x => x.Id == player.AccountId && !x.IsGuest, ct))
            return Response<ActionPreviewDto>.Fail("Signets require a registered account.");
        return await PersistAsync(operationId, AdminActionPreviewKinds.AlphaSignetGrant, actor, characterId,
            SignetRequestHash(characterId, quantity, reason), StateHash(new { player.AccountId, player.CharacterId }),
            new PreviewContext(characterId, null, quantity), "Grant Alpha Signets", player.CharacterName,
            "HighValue", player.CharacterName,
            [new("Quantity", quantity.ToString()), new("Item", "Alpha Signets"), new("Reason", reason.Trim())],
            ["Creates tradable Signets for alpha testing. This is compensation, not restoration of a previous issuance."], ct);
    }

    public Task<PreviewSubmissionResult> BeginAlphaSignetGrantAsync(Guid token, Guid operationId, Guid characterId,
        AdministrationActor actor, int quantity, string reason, CancellationToken ct) => BeginAsync(token, operationId,
            AdminActionPreviewKinds.AlphaSignetGrant, characterId, actor, SignetRequestHash(characterId, quantity, reason), ct);

    private static string SignetRequestHash(Guid characterId, int quantity, string reason) =>
        RequestHash(AdminActionPreviewKinds.AlphaSignetGrant, new { CharacterId = characterId, Quantity = quantity, Reason = NormalizeRequired(reason) });

    public async Task<Response<ActionPreviewDto>> CreateAccountBanAsync(
        Guid operationId,
        Guid accountId,
        AdministrationActor actor,
        string reason,
        string? internalNotes,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken, int? durationMinutes = null)
    {
        if (durationMinutes.HasValue)
        {
            if (durationMinutes is < 1 or > 43200 || expiresAt.HasValue)
                return Response<ActionPreviewDto>.Fail("Choose a duration of 1–43,200 minutes or an explicit expiry, not both.");
            expiresAt = timeProvider.GetUtcNow().AddMinutes(durationMinutes.Value);
        }
        var validation = ValidateCommon(operationId, reason, internalNotes);
        if (validation is not null) return Response<ActionPreviewDto>.Fail(validation);
        var now = timeProvider.GetUtcNow();
        if (expiresAt.HasValue && expiresAt.Value <= now)
        {
            return Response<ActionPreviewDto>.Fail("A temporary ban must expire in the future.");
        }

        var player = await liveOps.GetPlayerByAccountIdAsync(
            accountId,
            cancellationToken);
        if (player is null) return Response<ActionPreviewDto>.Fail("The target account was not found.");
        if (player.ActiveBanId.HasValue)
        {
            return Response<ActionPreviewDto>.Fail("The target account already has an active ban.");
        }

        var requestHash = RequestHash(AdminActionPreviewKinds.AccountBan, new
        {
            AccountId = accountId,
            Reason = reason.Trim(),
            InternalNotes = Normalize(internalNotes),
            ExpiresAt = expiresAt?.ToUniversalTime()
        });
        var stateHash = StateHash(new { player.AccountId, player.CharacterId, player.ActiveBanId });
        return await PersistAsync(
            operationId,
            AdminActionPreviewKinds.AccountBan,
            actor,
            accountId,
            requestHash,
            stateHash,
            new PreviewContext(player.CharacterId, null),
            "Apply account ban",
            player.CharacterName,
            expiresAt.HasValue ? "Normal" : "Permanent",
            expiresAt.HasValue ? null : player.CharacterName,
            [
                new("Account", player.AccountLabel),
                new("Character", player.CharacterName),
                new("Current account restriction", "None"),
                new("Expiry", expiresAt?.ToUniversalTime().ToString("O") ?? "Permanent"),
                new("Reason", reason.Trim()),
                new("Internal notes", Normalize(internalNotes) ?? "None")
            ],
            expiresAt.HasValue
                ? ["Active sessions will be revoked immediately."]
                : ["This ban is permanent until explicitly revoked.", "Active sessions will be revoked immediately."],
            cancellationToken, expiresAt);
    }

    public async Task<Response<ActionPreviewDto>> CreateAccountBanRevokeAsync(
        Guid operationId,
        Guid restrictionId,
        AdministrationActor actor,
        string reason,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCommon(operationId, reason, null);
        if (validation is not null) return Response<ActionPreviewDto>.Fail(validation);
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var restriction = await database.AccountRestrictions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == restrictionId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (restriction is null ||
            restriction.RestrictionType != AccountRestrictionType.Ban ||
            !restriction.IsActive(now))
        {
            return Response<ActionPreviewDto>.Fail("The account ban is no longer active.");
        }
        var player = await liveOps.GetPlayerByAccountIdAsync(
            restriction.AccountId,
            cancellationToken);
        if (player is null) return Response<ActionPreviewDto>.Fail("The target account was not found.");

        return await PersistAsync(
            operationId,
            AdminActionPreviewKinds.AccountBanRevoke,
            actor,
            restrictionId,
            RequestHash(AdminActionPreviewKinds.AccountBanRevoke, new
            {
                RestrictionId = restrictionId,
                Reason = reason.Trim()
            }),
            RestrictionStateHash(restriction),
            new PreviewContext(player.CharacterId, null),
            "Revoke account ban",
            player.CharacterName,
            "Normal",
            null,
            [
                new("Character", player.CharacterName),
                new("Current Chat restriction", "None"),
                new("Current restriction", restriction.Reason),
                new("Current expiry", restriction.ExpiresAt?.ToUniversalTime().ToString("O") ?? "Permanent"),
                new("Revocation reason", reason.Trim())
            ],
            ["Account access will be restored immediately."],
            cancellationToken);
    }

    public async Task<Response<ActionPreviewDto>> CreateMultiplayerRestrictionAsync(
        Guid operationId,
        Guid accountId,
        AdministrationActor actor,
        string reason,
        string? internalNotes,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken, int? durationMinutes = null)
    {
        if (durationMinutes.HasValue)
        {
            if (durationMinutes is < 1 or > 43200 || expiresAt.HasValue)
                return Response<ActionPreviewDto>.Fail("Choose a duration of 1–43,200 minutes or an explicit expiry, not both.");
            expiresAt = timeProvider.GetUtcNow().AddMinutes(durationMinutes.Value);
        }
        var validation = ValidateCommon(operationId, reason, internalNotes);
        if (validation is not null) return Response<ActionPreviewDto>.Fail(validation);
        var now = timeProvider.GetUtcNow();
        if (expiresAt.HasValue && expiresAt.Value <= now)
        {
            return Response<ActionPreviewDto>.Fail(
                "A temporary multiplayer restriction must expire in the future.");
        }

        var player = await liveOps.GetPlayerByAccountIdAsync(accountId, cancellationToken);
        if (player is null) return Response<ActionPreviewDto>.Fail("The target account was not found.");
        if (player.ActiveMultiplayerRestrictionId.HasValue)
        {
            return Response<ActionPreviewDto>.Fail(
                "The target account already has an active multiplayer restriction.");
        }

        var requestHash = RequestHash(AdminActionPreviewKinds.MultiplayerRestriction, new
        {
            AccountId = accountId,
            Reason = reason.Trim(),
            InternalNotes = Normalize(internalNotes),
            ExpiresAt = expiresAt?.ToUniversalTime()
        });
        var stateHash = StateHash(new
        {
            player.AccountId,
            player.CharacterId,
            player.ActiveMultiplayerRestrictionId
        });
        return await PersistAsync(
            operationId,
            AdminActionPreviewKinds.MultiplayerRestriction,
            actor,
            accountId,
            requestHash,
            stateHash,
            new PreviewContext(player.CharacterId, null),
            "Restrict multiplayer access",
            player.CharacterName,
            expiresAt.HasValue ? "Normal" : "Permanent",
            expiresAt.HasValue ? null : player.CharacterName,
            [
                new("Account", player.AccountLabel),
                new("Character", player.CharacterName),
                new("Current multiplayer restriction", "None"),
                new("Expiry", expiresAt?.ToUniversalTime().ToString("O") ?? "Permanent"),
                new("Reason", reason.Trim()),
                new("Internal notes", Normalize(internalNotes) ?? "None")
            ],
            expiresAt.HasValue
                ? ["Trading, transfers, competition, guild mutations, rankings, and server-event participation will be blocked."]
                : ["This restriction is permanent until explicitly revoked.", "Trading, transfers, competition, guild mutations, rankings, and server-event participation will be blocked."],
            cancellationToken, expiresAt);
    }

    public async Task<Response<ActionPreviewDto>> CreateMultiplayerRestrictionRevokeAsync(
        Guid operationId,
        Guid restrictionId,
        AdministrationActor actor,
        string reason,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCommon(operationId, reason, null);
        if (validation is not null) return Response<ActionPreviewDto>.Fail(validation);
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var restriction = await database.AccountRestrictions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == restrictionId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (restriction is null ||
            restriction.RestrictionType != AccountRestrictionType.MultiplayerRestriction ||
            !restriction.IsActive(now))
        {
            return Response<ActionPreviewDto>.Fail(
                "The multiplayer restriction is no longer active.");
        }
        var player = await liveOps.GetPlayerByAccountIdAsync(
            restriction.AccountId,
            cancellationToken);
        if (player is null) return Response<ActionPreviewDto>.Fail("The target account was not found.");

        return await PersistAsync(
            operationId,
            AdminActionPreviewKinds.MultiplayerRestrictionRevoke,
            actor,
            restrictionId,
            RequestHash(AdminActionPreviewKinds.MultiplayerRestrictionRevoke, new
            {
                RestrictionId = restrictionId,
                Reason = reason.Trim()
            }),
            RestrictionStateHash(restriction),
            new PreviewContext(player.CharacterId, null),
            "Revoke multiplayer restriction",
            player.CharacterName,
            "Normal",
            null,
            [
                new("Character", player.CharacterName),
                new("Current restriction", restriction.Reason),
                new("Current expiry", restriction.ExpiresAt?.ToUniversalTime().ToString("O") ?? "Permanent"),
                new("Revocation reason", reason.Trim())
            ],
            ["Shared-system access will be restored after restriction snapshots refresh. Review restricted-period progression before release."],
            cancellationToken);
    }

    public async Task<Response<ActionPreviewDto>> CreateChatMuteAsync(
        Guid operationId,
        Guid characterId,
        AdministrationActor actor,
        string reason,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken, int? durationMinutes = null)
    {
        if (durationMinutes.HasValue)
        {
            if (durationMinutes is < 1 or > 43200 || expiresAt.HasValue)
                return Response<ActionPreviewDto>.Fail("Choose a duration of 1–43,200 minutes or an explicit expiry, not both.");
            expiresAt = timeProvider.GetUtcNow().AddMinutes(durationMinutes.Value);
        }
        var validation = ValidateCommon(operationId, reason, null);
        if (validation is not null) return Response<ActionPreviewDto>.Fail(validation);
        var now = timeProvider.GetUtcNow();
        if (expiresAt.HasValue && expiresAt.Value <= now)
        {
            return Response<ActionPreviewDto>.Fail("A temporary mute must expire in the future.");
        }
        var player = await liveOps.GetPlayerAsync(characterId, cancellationToken);
        if (player is null) return Response<ActionPreviewDto>.Fail("The target character was not found.");
        var state = await chat.GetStateAsync(characterId, 1, cancellationToken);
        if (!state.IsSuccess) return Response<ActionPreviewDto>.Fail(state.ErrorMessage);
        if (state.ActiveMute is not null)
        {
            return Response<ActionPreviewDto>.Fail("The target character already has an active mute.");
        }

        return await PersistAsync(
            operationId,
            AdminActionPreviewKinds.ChatMute,
            actor,
            characterId,
            RequestHash(AdminActionPreviewKinds.ChatMute, new
            {
                CharacterId = characterId,
                Reason = reason.Trim(),
                ExpiresAt = expiresAt?.ToUniversalTime()
            }),
            ChatStateHash(characterId, state.ActiveMute),
            new PreviewContext(characterId, null),
            "Mute chat access",
            player.CharacterName,
            expiresAt.HasValue ? "Normal" : "Permanent",
            expiresAt.HasValue ? null : player.CharacterName,
            [
                new("Character", player.CharacterName),
                new("Expiry", expiresAt?.ToUniversalTime().ToString("O") ?? "Permanent"),
                new("Reason", reason.Trim())
            ],
            expiresAt.HasValue
                ? ["The player will be unable to send Chat messages until expiry."]
                : ["This mute is permanent until explicitly removed."],
            cancellationToken, expiresAt);
    }

    public async Task<Response<ActionPreviewDto>> CreateChatUnmuteAsync(
        Guid operationId,
        Guid restrictionId,
        Guid characterId,
        AdministrationActor actor,
        string reason,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCommon(operationId, reason, null);
        if (validation is not null) return Response<ActionPreviewDto>.Fail(validation);
        var player = await liveOps.GetPlayerAsync(characterId, cancellationToken);
        if (player is null) return Response<ActionPreviewDto>.Fail("The target character was not found.");
        var state = await chat.GetStateAsync(characterId, 1, cancellationToken);
        if (!state.IsSuccess) return Response<ActionPreviewDto>.Fail(state.ErrorMessage);
        if (state.ActiveMute?.Id != restrictionId)
        {
            return Response<ActionPreviewDto>.Fail("The Chat mute is no longer active.");
        }

        return await PersistAsync(
            operationId,
            AdminActionPreviewKinds.ChatUnmute,
            actor,
            restrictionId,
            RequestHash(AdminActionPreviewKinds.ChatUnmute, new
            {
                RestrictionId = restrictionId,
                CharacterId = characterId,
                Reason = reason.Trim()
            }),
            ChatStateHash(characterId, state.ActiveMute),
            new PreviewContext(characterId, null),
            "Remove chat mute",
            player.CharacterName,
            "Normal",
            null,
            [
                new("Character", player.CharacterName),
                new("Current restriction", state.ActiveMute.Reason),
                new("Current expiry", state.ActiveMute.ExpiresAt?.ToUniversalTime().ToString("O") ?? "Permanent"),
                new("Removal reason", reason.Trim())
            ],
            ["Chat posting access will be restored immediately."],
            cancellationToken);
    }

    public async Task<Response<ActionPreviewDto>> CreateCompensationGrantAsync(
        Guid operationId,
        Guid characterId,
        AdministrationActor actor,
        string itemBaseId,
        int quantity,
        string reason,
        string? internalNotes,
        CancellationToken cancellationToken,
        EquipmentGrantRequest? equipment = null)
    {
        var validation = ValidateCommon(operationId, reason, internalNotes);
        if (validation is not null) return Response<ActionPreviewDto>.Fail(validation);
        var normalizedItemId = itemBaseId?.Trim() ?? string.Empty;
        if (normalizedItemId.Length == 0)
        {
            return Response<ActionPreviewDto>.Fail("An item-base ID is required.");
        }
        if (quantity <= 0 || quantity > _options.MaximumGrantQuantity)
        {
            return Response<ActionPreviewDto>.Fail(
                $"Grant quantity must be between 1 and {_options.MaximumGrantQuantity:N0}.");
        }
        var player = await liveOps.GetPlayerAsync(characterId, cancellationToken);
        if (player is null) return Response<ActionPreviewDto>.Fail("The target character was not found.");
        var item = (await liveOps.SearchItemsAsync(normalizedItemId, 20, cancellationToken))
            .FirstOrDefault(x => string.Equals(x.Id, normalizedItemId, StringComparison.Ordinal));
        if (item is null) return Response<ActionPreviewDto>.Fail("The item-base ID does not exist in the server catalog.");
        var prepared = await liveOps.PrepareCompensationGrantAsync(operationId, characterId, normalizedItemId, quantity, cancellationToken, equipment);
        if (!prepared.IsSuccess || prepared.Value is null) return Response<ActionPreviewDto>.Fail(prepared.ErrorMessage);
        var plan = prepared.Value;
        var data = plan.Equipment;
        var fields = new List<ActionPreviewField>
        {
            new("Character", player.CharacterName),
            new("Item", $"{item.Name} ({item.Id})"),
            new("Quantity", quantity.ToString("N0")),
            new("Type and rarity", $"{item.ItemType} · {data?.Rarity.ToString() ?? item.Rarity.ToString()}"),
            new("Behavior", data is not null ? "Individual instances · Bound to recipient" : $"{(item.Stackable ? "Stackable" : "Individual instances")} · {(item.IsBound ? "Bound" : "Unbound")}"),
            new("Reason", reason.Trim()),
            new("Internal notes", Normalize(internalNotes) ?? "None")
        };
        if (data is not null)
        {
            fields.AddRange([
                new("Definition", $"{data.DisplayName} ({data.State.DefinitionId})"),
                new("Archetype", data.State.ArchetypeId),
                new("Tier / Rank", $"{data.State.Tier} / {data.State.Rank}"),
                new("Native / Active style", $"{data.State.NativeStyleId ?? "Plain"} / {data.State.ActiveStyleId ?? "Plain"}"),
                new("Owner", data.State.Ownership.OwnerId.ToString()),
                new("Balance version", data.State.BalanceVersion.ToString()),
                new("Salvage", "0 base Scrap; no paid investment. Only later paid ranks create refundable value."),
                new("Stats", string.Join(", ", data.Stats.OrderBy(x => x.Key).Select(x => $"{x.Key}: {x.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)}")))
            ]);
        }
        var isHighValue = data is not null || quantity >= Math.Max(1, _options.LargeGrantAuditThreshold) ||
            !item.Stackable ||
            item.Rarity >= Domain.Models.Items.Rarity.Rare;

        return await PersistAsync(
            operationId,
            AdminActionPreviewKinds.CompensationGrant,
            actor,
            characterId,
            RequestHash(AdminActionPreviewKinds.CompensationGrant, new
            {
                CharacterId = characterId,
                ItemBaseId = normalizedItemId,
                Quantity = quantity,
                Reason = reason.Trim(),
                InternalNotes = Normalize(internalNotes),
                Equipment = equipment
            }),
            GrantStateHash(player, item, plan),
            new PreviewContext(characterId, normalizedItemId, quantity, equipment),
            "Grant compensation items",
            player.CharacterName,
            isHighValue ? "HighValue" : "Normal",
            isHighValue ? player.CharacterName : null,
            fields,
            isHighValue
                ? ["This quantity is classified as a high-value grant.", "The operation writes inventory, provenance, economy ledger, realtime, and audit records."]
                : ["The operation writes inventory, provenance, economy ledger, realtime, and audit records."],
            cancellationToken);
    }

    public Task<PreviewSubmissionResult> BeginAccountBanAsync(
        Guid token, Guid operationId, Guid accountId, AdministrationActor actor,
        string reason, string? internalNotes, DateTimeOffset? expiresAt,
        CancellationToken cancellationToken) => BeginAsync(
            token, operationId, AdminActionPreviewKinds.AccountBan, accountId, actor,
            RequestHash(AdminActionPreviewKinds.AccountBan, new
            {
                AccountId = accountId, Reason = NormalizeRequired(reason), InternalNotes = Normalize(internalNotes),
                ExpiresAt = expiresAt?.ToUniversalTime()
            }), cancellationToken);

    public Task<PreviewSubmissionResult> BeginAccountBanRevokeAsync(
        Guid token, Guid operationId, Guid restrictionId, AdministrationActor actor,
        string reason, CancellationToken cancellationToken) => BeginAsync(
            token, operationId, AdminActionPreviewKinds.AccountBanRevoke, restrictionId, actor,
            RequestHash(AdminActionPreviewKinds.AccountBanRevoke, new
            {
                RestrictionId = restrictionId, Reason = NormalizeRequired(reason)
            }), cancellationToken);

    public Task<PreviewSubmissionResult> BeginMultiplayerRestrictionAsync(
        Guid token, Guid operationId, Guid accountId, AdministrationActor actor,
        string reason, string? internalNotes, DateTimeOffset? expiresAt,
        CancellationToken cancellationToken) => BeginAsync(
            token, operationId, AdminActionPreviewKinds.MultiplayerRestriction, accountId, actor,
            RequestHash(AdminActionPreviewKinds.MultiplayerRestriction, new
            {
                AccountId = accountId, Reason = NormalizeRequired(reason), InternalNotes = Normalize(internalNotes),
                ExpiresAt = expiresAt?.ToUniversalTime()
            }), cancellationToken);

    public Task<PreviewSubmissionResult> BeginMultiplayerRestrictionRevokeAsync(
        Guid token, Guid operationId, Guid restrictionId, AdministrationActor actor,
        string reason, CancellationToken cancellationToken) => BeginAsync(
            token, operationId, AdminActionPreviewKinds.MultiplayerRestrictionRevoke, restrictionId, actor,
            RequestHash(AdminActionPreviewKinds.MultiplayerRestrictionRevoke, new
            {
                RestrictionId = restrictionId, Reason = NormalizeRequired(reason)
            }), cancellationToken);

    public Task<PreviewSubmissionResult> BeginChatMuteAsync(
        Guid token, Guid operationId, Guid characterId, AdministrationActor actor,
        string reason, DateTimeOffset? expiresAt, CancellationToken cancellationToken) => BeginAsync(
            token, operationId, AdminActionPreviewKinds.ChatMute, characterId, actor,
            RequestHash(AdminActionPreviewKinds.ChatMute, new
            {
                CharacterId = characterId, Reason = NormalizeRequired(reason), ExpiresAt = expiresAt?.ToUniversalTime()
            }), cancellationToken);

    public Task<PreviewSubmissionResult> BeginChatUnmuteAsync(
        Guid token, Guid operationId, Guid restrictionId, Guid characterId,
        AdministrationActor actor, string reason, CancellationToken cancellationToken) => BeginAsync(
            token, operationId, AdminActionPreviewKinds.ChatUnmute, restrictionId, actor,
            RequestHash(AdminActionPreviewKinds.ChatUnmute, new
            {
                RestrictionId = restrictionId, CharacterId = characterId, Reason = NormalizeRequired(reason)
            }), cancellationToken);

    public Task<PreviewSubmissionResult> BeginCompensationGrantAsync(
        Guid token, Guid operationId, Guid characterId, AdministrationActor actor,
        string itemBaseId, int quantity, string reason, string? internalNotes,
        CancellationToken cancellationToken, EquipmentGrantRequest? equipment = null) => BeginAsync(
            token, operationId, AdminActionPreviewKinds.CompensationGrant, characterId, actor,
            RequestHash(AdminActionPreviewKinds.CompensationGrant, new
            {
                CharacterId = characterId, ItemBaseId = itemBaseId?.Trim() ?? string.Empty, Quantity = quantity,
                Reason = NormalizeRequired(reason), InternalNotes = Normalize(internalNotes), Equipment = equipment
            }), cancellationToken);

    public async Task CompleteAsync(
        Guid token,
        bool success,
        CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var preview = await database.AdminActionPreviews
            .FirstOrDefaultAsync(x => x.Id == token, cancellationToken);
        if (preview is null) return;
        var now = timeProvider.GetUtcNow();
        if (success) preview.CompletedAt ??= now;
        else preview.InvalidatedAt ??= now;
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task<PreviewSubmissionResult> BeginAsync(
        Guid token,
        Guid operationId,
        string actionKind,
        Guid targetId,
        AdministrationActor actor,
        string requestHash,
        CancellationToken cancellationToken)
    {
        if (token == Guid.Empty)
        {
            return PreviewSubmissionResult.Fail("A server preview is required before submission.");
        }
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var preview = await database.AdminActionPreviews
            .FirstOrDefaultAsync(x => x.Id == token, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (preview is null || preview.ExpiresAt <= now)
        {
            return PreviewSubmissionResult.Fail("The preview expired. Review the operation again.", true);
        }
        if (preview.InvalidatedAt.HasValue)
        {
            return PreviewSubmissionResult.Fail("The preview is no longer valid. Review the operation again.", true);
        }
        if (preview.OperationId != operationId ||
            preview.TargetId != targetId ||
            !string.Equals(preview.ActionKind, actionKind, StringComparison.Ordinal) ||
            !string.Equals(preview.ActorSubject, actor.Subject.Trim(), StringComparison.Ordinal) ||
            !string.Equals(preview.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return PreviewSubmissionResult.Fail("The submitted operation does not match its preview.", true);
        }

        if (preview.SubmittedAt.HasValue && await database.AdminActions.AsNoTracking().AnyAsync(
            x => x.Id == operationId && x.ActorSubject == actor.Subject, cancellationToken))
            return PreviewSubmissionResult.Success();
        if (preview.SubmittedAt.HasValue && (preview.CompletedAt.HasValue || preview.ActionKind is not (AdminActionPreviewKinds.CompensationGrant or AdminActionPreviewKinds.CompensationPackage)))
            return PreviewSubmissionResult.Success();
        var currentState = await CurrentStateHashAsync(preview, cancellationToken);
        if (!currentState.IsSuccess)
        {
            return PreviewSubmissionResult.Fail(currentState.ErrorMessage);
        }
        if (!string.Equals(preview.StateHash, currentState.Hash, StringComparison.Ordinal))
        {
            preview.InvalidatedAt = now;
            await database.SaveChangesAsync(cancellationToken);
            return PreviewSubmissionResult.Fail(
                "The target changed after preview. Review the operation again.",
                true);
        }

        preview.SubmittedAt = now;
        await database.SaveChangesAsync(cancellationToken);
        return PreviewSubmissionResult.Success();
    }

    private async Task<StateResult> CurrentStateHashAsync(
        AdminActionPreview preview,
        CancellationToken cancellationToken)
    {
        var context = JsonSerializer.Deserialize<PreviewContext>(preview.ContextJson)
            ?? new PreviewContext(null, null);
        switch (preview.ActionKind)
        {
            case AdminActionPreviewKinds.AccountBan:
            {
                var player = await liveOps.GetPlayerByAccountIdAsync(
                    preview.TargetId,
                    cancellationToken);
                return player is null
                    ? StateResult.Fail("The target account is no longer available.")
                    : StateResult.Success(StateHash(new
                    {
                        player.AccountId,
                        player.CharacterId,
                        player.ActiveBanId
                    }));
            }
            case AdminActionPreviewKinds.AccountBanRevoke:
            {
                await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
                var restriction = await database.AccountRestrictions.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == preview.TargetId, cancellationToken);
                return restriction is null
                    ? StateResult.Fail("The account ban is no longer available.")
                    : StateResult.Success(RestrictionStateHash(restriction));
            }
            case AdminActionPreviewKinds.MultiplayerRestriction:
            {
                var player = await liveOps.GetPlayerByAccountIdAsync(
                    preview.TargetId,
                    cancellationToken);
                return player is null
                    ? StateResult.Fail("The target account is no longer available.")
                    : StateResult.Success(StateHash(new
                    {
                        player.AccountId,
                        player.CharacterId,
                        player.ActiveMultiplayerRestrictionId
                    }));
            }
            case AdminActionPreviewKinds.MultiplayerRestrictionRevoke:
            {
                await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
                var restriction = await database.AccountRestrictions.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == preview.TargetId, cancellationToken);
                return restriction is null
                    ? StateResult.Fail("The multiplayer restriction is no longer available.")
                    : StateResult.Success(RestrictionStateHash(restriction));
            }
            case AdminActionPreviewKinds.ChatMute:
            case AdminActionPreviewKinds.ChatUnmute:
            {
                if (!context.CharacterId.HasValue)
                {
                    return StateResult.Fail("The Chat preview context is invalid.");
                }
                var state = await chat.GetStateAsync(context.CharacterId.Value, 1, cancellationToken);
                return state.IsSuccess
                    ? StateResult.Success(ChatStateHash(context.CharacterId.Value, state.ActiveMute))
                    : StateResult.Fail(state.ErrorMessage);
            }
            case AdminActionPreviewKinds.CompensationPackage:
            {
                if (!context.PackageId.HasValue) return StateResult.Fail("Package preview context is missing.");
                var plan = await packages!.PrepareAsync(preview.OperationId, preview.TargetId, context.PackageId.Value, context.PackageVersion, cancellationToken);
                return plan.IsSuccess && plan.Data is not null ? StateResult.Success(PackageStateHash(plan.Data)) : StateResult.Fail(plan.ErrorMessage);
            }
            case AdminActionPreviewKinds.AlphaSignetGrant:
            {
                var player = await liveOps.GetPlayerAsync(preview.TargetId, cancellationToken);
                return player is null ? StateResult.Fail("The character is unavailable.")
                    : StateResult.Success(StateHash(new { player.AccountId, player.CharacterId }));
            }
            case AdminActionPreviewKinds.CompensationGrant:
            {
                if (!context.CharacterId.HasValue || string.IsNullOrWhiteSpace(context.ItemBaseId))
                {
                    return StateResult.Fail("The compensation preview context is invalid.");
                }
                var player = await liveOps.GetPlayerAsync(context.CharacterId.Value, cancellationToken);
                var item = (await liveOps.SearchItemsAsync(context.ItemBaseId, 20, cancellationToken))
                    .FirstOrDefault(x => string.Equals(x.Id, context.ItemBaseId, StringComparison.Ordinal));
                if (player is null || item is null) return StateResult.Fail("The player or item is no longer available.");
                var prepared = await liveOps.PrepareCompensationGrantAsync(preview.OperationId, context.CharacterId.Value,
                    context.ItemBaseId, context.Quantity, cancellationToken, context.Equipment);
                return prepared.IsSuccess && prepared.Value is not null
                    ? StateResult.Success(GrantStateHash(player, item, prepared.Value))
                    : StateResult.Fail(prepared.ErrorMessage);
            }
            default:
                return StateResult.Fail("The preview action type is unsupported.");
        }
    }

    private async Task<Response<ActionPreviewDto>> PersistAsync(
        Guid operationId,
        string actionKind,
        AdministrationActor actor,
        Guid targetId,
        string requestHash,
        string stateHash,
        PreviewContext context,
        string title,
        string targetName,
        string riskLevel,
        string? confirmationText,
        IReadOnlyList<ActionPreviewField> fields,
        IReadOnlyList<string> warnings,
        CancellationToken cancellationToken, DateTimeOffset? effectExpiresAt = null)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddSeconds(Math.Clamp(_options.PreviewLifetimeSeconds, 60, 600));
        var preview = new AdminActionPreview
        {
            Id = Guid.NewGuid(),
            OperationId = operationId,
            ActionKind = actionKind,
            ActorSubject = actor.Subject.Trim(),
            TargetId = targetId,
            RequestHash = requestHash,
            StateHash = stateHash,
            ContextJson = JsonSerializer.Serialize(context),
            CreatedAt = now,
            ExpiresAt = expiresAt
        };
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (database.Database.IsRelational())
        {
            await database.AdminActionPreviews
                .Where(x => x.ExpiresAt < now)
                .ExecuteDeleteAsync(cancellationToken);
        }
        database.AdminActionPreviews.Add(preview);
        await database.SaveChangesAsync(cancellationToken);
        return Response<ActionPreviewDto>.Success(new ActionPreviewDto(
            preview.Id,
            operationId,
            actionKind,
            title,
            targetName,
            targetId,
            riskLevel,
            expiresAt,
            confirmationText,
            fields,
            warnings, effectExpiresAt, now));
    }

    private static string? ValidateCommon(Guid operationId, string reason, string? internalNotes)
    {
        if (operationId == Guid.Empty) return "A non-empty operation ID is required.";
        if (string.IsNullOrWhiteSpace(reason)) return "A reason or support reference is required.";
        if (reason.Trim().Length > 1_000) return "The reason cannot exceed 1,000 characters.";
        if (!string.IsNullOrWhiteSpace(internalNotes) && internalNotes.Trim().Length > 4_000)
        {
            return "Internal notes cannot exceed 4,000 characters.";
        }
        return null;
    }

    private static string RequestHash<T>(string actionKind, T request) =>
        Hash(JsonSerializer.SerializeToUtf8Bytes(new { ActionKind = actionKind, Request = request }));

    private static string StateHash<T>(T state) =>
        Hash(JsonSerializer.SerializeToUtf8Bytes(state));

    private static string Hash(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes));

    private static string RestrictionStateHash(AccountRestriction restriction) =>
        StateHash(new
        {
            restriction.Id,
            restriction.AccountId,
            restriction.ExpiresAt,
            restriction.RevokedAt
        });

    private static string ChatStateHash(
        Guid characterId,
        ChatRestrictionGatewaySnapshot? mute) =>
        StateHash(new
        {
            CharacterId = characterId,
            RestrictionId = mute?.Id,
            mute?.ExpiresAt,
            mute?.RevokedAt
        });

    private static string GrantStateHash(
        PlayerAdministrationSnapshot player,
        AdministrationItemCatalogEntry item, CompensationGrantPlan plan) =>
        StateHash(new
        {
            player.AccountId,
            player.CharacterId,
            ItemId = item.Id,
            item.Name,
            item.ItemType,
            item.Rarity,
            item.Stackable,
            item.IsBound,
            plan.UsesEquipmentProgression,
            plan.Equipment
        });

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeRequired(string? value) =>
        value?.Trim() ?? string.Empty;

    private sealed record PreviewContext(Guid? CharacterId, string? ItemBaseId, int Quantity = 1, EquipmentGrantRequest? Equipment = null, Guid? PackageId = null, int PackageVersion = 0);
    private sealed record StateResult(bool IsSuccess, string Hash, string ErrorMessage)
    {
        public static StateResult Success(string hash) => new(true, hash, string.Empty);
        public static StateResult Fail(string error) => new(false, string.Empty, error);
    }
}
