using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Common.Interfaces;
using Application.UseCases.Outbox;
using Domain.Models.Colosseum.Tournaments;
using Domain.Models.MarketPlaces;
using Domain.Models.WorldTower;
using Domain.Models.Guilds;
using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;

namespace Services.LL.Outbox;

public sealed class AccountRestrictionCleanupOutboxConsumer(
    IMarketPlaceRepository marketplaceRepository,
    IMarketPlaceService marketplace,
    IDbContext context,
    JsonSerializerOptions jsonOptions) : IGameEventOutboxConsumer
{
    public string Consumer => GameEventOutboxConsumerNames.AccountRestrictionCleanup;

    public bool CanHandle(string eventType) =>
        string.Equals(
            eventType,
            GameEventTypes.AccountMultiplayerRestricted,
            StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(
        Domain.Models.Outbox.GameEventOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<AccountMultiplayerRestrictedPayload>(
            message.PayloadJson,
            jsonOptions) ?? throw new InvalidOperationException(
                "The multiplayer-restriction cleanup payload is invalid.");
        await WithdrawPendingTournamentParticipationAsync(payload, cancellationToken);
        await RemovePendingTowerParticipationAsync(payload, cancellationToken);
        await TransferGuildLeadershipAsync(payload, cancellationToken);
        var orders = await marketplaceRepository.GetActiveOrderIdsAsync(
            payload.CharacterId,
            cancellationToken);

        foreach (var listingId in orders.ListingIds)
        {
            await marketplace.CancelMarketPlaceListingAsync(
                payload.CharacterId,
                listingId,
                cancellationToken);
        }
        foreach (var buyOrderId in orders.BuyOrderIds)
        {
            await marketplace.CancelMarketPlaceBuyOrderAsync(
                payload.CharacterId,
                buyOrderId,
                cancellationToken);
        }
    }

    private async Task WithdrawPendingTournamentParticipationAsync(
        AccountMultiplayerRestrictedPayload payload,
        CancellationToken cancellationToken)
    {
        var participants = await context.TournamentParticipants
            .Where(x => x.AccountId == payload.AccountId &&
                        x.Status != TournamentParticipantStatus.Withdrawn &&
                        (x.Tournament.Status == TournamentStatus.Scheduled ||
                         x.Tournament.Status == TournamentStatus.RegistrationOpen ||
                         x.Tournament.Status == TournamentStatus.RegistrationClosed))
            .ToListAsync(cancellationToken);
        if (participants.Count == 0)
        {
            return;
        }

        var participantIds = participants.Select(x => x.Id).ToArray();
        var requests = await context.TournamentTeamApplications
            .Where(x => participantIds.Contains(x.ApplicantParticipantId) &&
                        x.Status == TournamentTeamRequestStatus.Pending)
            .ToListAsync(cancellationToken);
        var invites = await context.TournamentTeamInvites
            .Where(x => (participantIds.Contains(x.InviterParticipantId) ||
                         participantIds.Contains(x.InvitedParticipantId)) &&
                        x.Status == TournamentTeamRequestStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var request in requests)
        {
            request.Status = TournamentTeamRequestStatus.Cancelled;
            request.UpdatedAtUtc = payload.AppliedAt;
        }
        foreach (var invite in invites)
        {
            invite.Status = TournamentTeamRequestStatus.Cancelled;
            invite.UpdatedAtUtc = payload.AppliedAt;
        }
        foreach (var participant in participants)
        {
            participant.Status = TournamentParticipantStatus.Withdrawn;
            participant.UpdatedAtUtc = payload.AppliedAt;
        }

        var teamIds = participants
            .Where(x => x.TeamId.HasValue)
            .Select(x => x.TeamId!.Value)
            .Distinct()
            .ToArray();
        var teams = await context.TournamentTeams
            .Where(x => teamIds.Contains(x.Id) &&
                        x.Status == TournamentTeamStatus.Forming)
            .ToListAsync(cancellationToken);
        foreach (var team in teams)
        {
            var remaining = await context.TournamentParticipants
                .Where(x => x.TeamId == team.Id &&
                            x.Status != TournamentParticipantStatus.Withdrawn)
                .OrderBy(x => x.RegisteredAtUtc)
                .ToListAsync(cancellationToken);
            team.MemberCount = remaining.Count;
            team.UpdatedAtUtc = payload.AppliedAt;
            if (remaining.Count == 0)
            {
                team.Status = TournamentTeamStatus.Disbanded;
                continue;
            }
            if (participantIds.Contains(team.OwnerParticipantId))
            {
                team.OwnerParticipantId = remaining[0].Id;
                remaining[0].IsTeamOwner = true;
            }
        }
    }

    private async Task RemovePendingTowerParticipationAsync(
        AccountMultiplayerRestrictedPayload payload,
        CancellationToken cancellationToken)
    {
        var applications = await context.TowerRallyApplications
            .Where(x => x.AccountId == payload.AccountId &&
                        x.Status == TowerRallyApplicationStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var application in applications)
        {
            application.Status = TowerRallyApplicationStatus.Withdrawn;
            application.ResolvedAt = payload.AppliedAt;
        }

        var rallies = await context.TowerRallies
            .Include(x => x.Participants)
            .Where(x => (x.Status == TowerRallyStatus.Recruiting ||
                         x.Status == TowerRallyStatus.Ready) &&
                        x.Participants.Any(participant =>
                            participant.AccountId == payload.AccountId))
            .ToListAsync(cancellationToken);
        foreach (var rally in rallies)
        {
            var removed = rally.Participants
                .Where(x => x.AccountId == payload.AccountId)
                .ToList();
            foreach (var participant in removed)
            {
                rally.Participants.Remove(participant);
                context.TowerRallyParticipants.Remove(participant);
            }

            if (rally.Participants.Count == 0)
            {
                rally.Status = TowerRallyStatus.Cancelled;
                rally.CancelledAt = payload.AppliedAt;
                continue;
            }
            if (removed.Any(x => x.CharacterId == rally.CreatedByCharacterId))
            {
                rally.CreatedByCharacterId = rally.Participants
                    .OrderBy(x => x.JoinedAt)
                    .First()
                    .CharacterId;
            }
            rally.Status = rally.Participants.Count == rally.RequiredSlots
                ? TowerRallyStatus.Ready
                : TowerRallyStatus.Recruiting;
        }
    }

    private async Task TransferGuildLeadershipAsync(
        AccountMultiplayerRestrictedPayload payload,
        CancellationToken cancellationToken)
    {
        var leaderships = await context.GuildMembers
            .Include(x => x.Guild)
            .Where(x => x.CharacterId == payload.CharacterId &&
                        x.Role == GuildRole.Leader)
            .ToListAsync(cancellationToken);
        foreach (var leadership in leaderships)
        {
            var replacement = await context.GuildMembers
                .Include(x => x.Character)
                .Where(x => x.GuildId == leadership.GuildId &&
                            x.CharacterId != payload.CharacterId &&
                            !context.AccountRestrictions.Any(restriction =>
                                restriction.AccountId == x.Character.UserId &&
                                restriction.RevokedAt == null &&
                                (restriction.ExpiresAt == null ||
                                 restriction.ExpiresAt > payload.AppliedAt) &&
                                (restriction.RestrictionType == AccountRestrictionType.Ban ||
                                 restriction.RestrictionType == AccountRestrictionType.MultiplayerRestriction)))
                .OrderBy(x => x.Role == GuildRole.Officer ? 0 : 1)
                .ThenBy(x => x.JoinedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (replacement is null)
            {
                continue;
            }

            leadership.Role = GuildRole.Member;
            replacement.Role = GuildRole.Leader;
            leadership.Guild.OwnerId = replacement.CharacterId;
        }
    }
}
