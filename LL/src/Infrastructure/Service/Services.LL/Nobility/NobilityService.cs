using Application.Interfaces.Services.LL.Nobility;
using Common.Primitives;
using Domain.Models.Nobility;

namespace Services.LL.Nobility;

public sealed class NobilityService(INobilityRepository repository, TimeProvider time) : INobilityService
{
    private readonly Dictionary<Guid, IReadOnlyList<NobilityCoverage>> _coverage = [];
    private readonly AsyncLocal<DateTimeOffset?> _combatTime = new();
    public DateTimeOffset? CombatEvaluationTime => _combatTime.Value;
    public IDisposable EvaluateCombatAt(DateTimeOffset at)
    {
        var previous = _combatTime.Value;
        _combatTime.Value = at;
        return new EvaluationScope(() => _combatTime.Value = previous);
    }
    private sealed class EvaluationScope(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }

    public async Task<IReadOnlyList<NobilityCoverage>> GetCoverageAsync(Guid characterId, CancellationToken ct)
    {
        if (!_coverage.TryGetValue(characterId, out var periods))
            _coverage[characterId] = periods = await repository.GetCoverageAsync(characterId, ct);
        return periods;
    }

    public async Task<NobilityBenefits> GetBenefitsAsync(Guid characterId, DateTimeOffset at, CancellationToken ct) =>
        (await GetCoverageAsync(characterId, ct)).Any(x => x.StartsAt <= at && at < x.EndsAt)
            ? NobilityBenefits.Noble : NobilityBenefits.Free;

    public async Task<NobilityStatus> GetStatusAsync(Guid accountId, Guid characterId, CancellationToken ct)
    {
        var character = await repository.GetCharacterAsync(characterId, ct);
        if (character?.UserId != accountId) throw new UnauthorizedAccessException("Character does not belong to this account.");
        var now = time.GetUtcNow();
        var membership = await repository.GetMembershipAsync(accountId, ct);
        var active = membership?.At(now);
        return new(active is not null, now, active?.EndsAt ?? membership?.Coverage.MaxBy(x => x.EndsAt)?.EndsAt,
            membership?.Version ?? Guid.Empty,
            (await repository.GetUnitsAsync(characterId, SignetState.Available, ct)).Count,
            (await repository.GetUnitsAsync(characterId, SignetState.Listed, ct)).Count,
            await repository.HasCashSupportAsync(accountId, ct), membership?.ShowBadge ?? true,
            active is null ? NobilityBenefits.Free : NobilityBenefits.Noble);
    }

    public async Task<Response<SignetPreview>> PreviewAsync(Guid accountId, Guid characterId, int quantity, CancellationToken ct)
    {
        if (quantity is <= 0 or > 1200) return Response<SignetPreview>.Fail("Select between one and 1200 Signets.");
        if (!await IsRecoverableOwnerAsync(accountId, characterId, ct))
            return Response<SignetPreview>.Fail("Register or recover your account before redeeming Signets.");
        var units = await repository.GetUnitsAsync(characterId, SignetState.Available, ct);
        if (units.Count < quantity) return Response<SignetPreview>.Fail("Not enough unreserved Signets.");
        var membership = await repository.GetMembershipAsync(accountId, ct);
        var now = time.GetUtcNow();
        try
        {
            var extension = (membership ?? new NobilityMembership()).Preview(quantity, now);
            return Response<SignetPreview>.Success(new(membership?.Version ?? Guid.Empty,
                units.Take(quantity).Select(x => x.Id).ToArray(), extension.ExpiresAt, membership?.At(now) is null));
        }
        catch (ArgumentOutOfRangeException)
        {
            return Response<SignetPreview>.Fail("That quantity exceeds the supported membership date range.");
        }
    }

    public async Task<Response<SignetRedemption>> RedeemAsync(Guid accountId, Guid characterId, Guid operationId,
        Guid membershipVersion, Guid[] unitIds, DateOnly expectedExpiryDate, CancellationToken ct)
    {
        if (operationId == Guid.Empty || unitIds is null || unitIds.Length is <= 0 or > 1200 ||
            unitIds.Any(x => x == Guid.Empty) || unitIds.Distinct().Count() != unitIds.Length)
            return Response<SignetRedemption>.Fail("Select valid, distinct Signets and supply an operation ID.");
        if (!await IsRecoverableOwnerAsync(accountId, characterId, ct))
            return Response<SignetRedemption>.Fail("Register or recover your account before redeeming Signets.");
        await repository.LockAccountAsync(accountId, characterId, ct);
        var previous = await repository.GetRedemptionAsync(operationId, ct);
        if (previous is not null)
            return previous.AccountId == accountId && previous.CharacterId == characterId &&
                previous.UnitIds.Order().SequenceEqual(unitIds.Order())
                ? Response<SignetRedemption>.Success(previous)
                : Response<SignetRedemption>.Conflict("This operation ID belongs to a different redemption.", "nobility_operation_conflict");

        var membership = await repository.GetMembershipAsync(accountId, ct);
        if ((membership?.Version ?? Guid.Empty) != membershipVersion)
            return Response<SignetRedemption>.Conflict("Nobility changed. Preview the redemption again.", "nobility_preview_stale");
        var available = await repository.GetUnitsAsync(characterId, SignetState.Available, ct);
        var selected = available.Where(x => unitIds.Contains(x.Id)).ToArray();
        if (selected.Length != unitIds.Length)
            return Response<SignetRedemption>.Conflict("A selected Signet is no longer available. Refresh and try again.", "signet_unavailable");
        var now = time.GetUtcNow();
        var isNew = membership is null;
        membership ??= new NobilityMembership
        {
            AccountId = accountId, RewardCharacterId = characterId
        };
        NobilityExtension extension;
        try { extension = membership.Preview(selected.Length, now); }
        catch (ArgumentOutOfRangeException)
        { return Response<SignetRedemption>.Fail("That quantity exceeds the supported membership date range."); }
        if (DateOnly.FromDateTime(extension.ExpiresAt.UtcDateTime) != expectedExpiryDate)
            return Response<SignetRedemption>.Conflict("The expiry date changed. Preview the redemption again.", "nobility_preview_stale");

        membership.Extend(selected.Length, now);
        if (isNew) repository.AddMembership(membership);
        var receipt = new SignetRedemption
        {
            Id = operationId, AccountId = accountId, CharacterId = characterId,
            UnitIds = unitIds.Order().ToArray(), MembershipVersion = membership.Version, RedeemedAt = now,
            PreviousExpiry = extension.PreviousExpiry, ExpiresAt = extension.ExpiresAt
        };
        repository.AddRedemption(receipt);
        foreach (var unit in selected)
        {
            unit.State = SignetState.Redeemed;
            unit.RedemptionId = operationId;
            unit.Version = Guid.NewGuid();
            repository.AddMovement(new SignetMovement { UnitId = unit.Id, OperationId = operationId,
                Kind = SignetMovementKind.Redeemed, FromCharacterId = characterId, OccurredAt = now });
        }
        await repository.SynchronizeInventoryAsync(characterId, ct);
        _coverage.Clear();
        return Response<SignetRedemption>.Success(receipt);
    }

    public async Task<Response<SignetIssuance>> GrantAlphaAsync(string actorSubject, Guid characterId,
        Guid operationId, int quantity, string reason, CancellationToken ct)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (operationId == Guid.Empty || quantity is <= 0 or > 1200 || reason.Length is < 3 or > 1000 ||
            string.IsNullOrWhiteSpace(actorSubject) || actorSubject.Length > 200)
            return Response<SignetIssuance>.Fail("Provide an operation ID, 1–1200 Signets and a reason (3–1000 characters).");
        var character = await repository.GetCharacterAsync(characterId, ct);
        if (character?.User is null || character.User.IsGuest)
            return Response<SignetIssuance>.Fail("Signets can only be granted to a registered account's character.");
        await repository.LockAccountAsync(character.UserId, characterId, ct);
        var previous = await repository.GetIssuanceAsync(operationId, ct);
        if (previous is not null)
            return previous.CharacterId == characterId && previous.Quantity == quantity && previous.Reason == reason &&
                previous.ActorSubject == actorSubject && previous.Origin == SignetOrigin.AlphaGrant
                ? Response<SignetIssuance>.Success(previous)
                : Response<SignetIssuance>.Conflict("This operation ID belongs to a different grant.", "signet_grant_conflict");
        var now = time.GetUtcNow();
        var issuance = new SignetIssuance { Id = operationId, AccountId = character.UserId, CharacterId = characterId,
            ActorSubject = actorSubject, Origin = SignetOrigin.AlphaGrant, Quantity = quantity, Reason = reason, IssuedAt = now };
        var units = Enumerable.Range(0, quantity).Select(index => new SignetUnit
        {
            IssuanceId = operationId, Ordinal = index, OwnerCharacterId = characterId, IssuedAt = now
        }).ToArray();
        repository.AddIssuance(issuance, units);
        foreach (var unit in units)
            repository.AddMovement(new SignetMovement { UnitId = unit.Id, OperationId = operationId,
                Kind = SignetMovementKind.Issued, ToCharacterId = characterId, OccurredAt = now });
        await repository.SynchronizeInventoryAsync(characterId, ct);
        return Response<SignetIssuance>.Success(issuance);
    }

    public async Task<Response<bool>> SetAppearanceAsync(Guid accountId, Guid characterId, bool showBadge, CancellationToken ct)
    {
        if (!await IsRecoverableOwnerAsync(accountId, characterId, ct)) return Response<bool>.Fail("Account ownership could not be verified.");
        await repository.LockAccountAsync(accountId, characterId, ct);
        var membership = await repository.GetMembershipAsync(accountId, ct);
        if (membership is null) return Response<bool>.Fail("Redeem a Signet before choosing Nobility appearance.");
        membership.ShowBadge = showBadge;
        membership.Version = Guid.NewGuid();
        return Response<bool>.Success(true);
    }

    private async Task<bool> IsRecoverableOwnerAsync(Guid accountId, Guid characterId, CancellationToken ct)
    {
        var character = await repository.GetCharacterAsync(characterId, ct);
        return character?.UserId == accountId && character.User is { IsGuest: false };
    }
}
