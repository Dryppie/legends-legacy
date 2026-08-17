using Application.BackgroundJobs;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Colosseum;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.WebSockets;
using Application.MediatR.Attributes;
using Application.UseCases.Colosseum.Tournaments.Commands;
using Application.UseCases.Colosseum.Tournaments;
using Application.UseCases.Inventories.SelectionCrates;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Colosseum;
using Domain.Models.Colosseum.Tournaments;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Inventories;
using Domain.Models.MarketPlaces;
using Domain.Models.Professions.Crafting;
using Domain.Models.Regions.Areas;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.BackgroundJobs;
using Persistence.LL.Repositories.Colosseum;
using Services.LL.Colosseum.Tournaments;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Inventories;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO.Compression;
using Quartz;
using Worker.LL.BackgroundJobs;

namespace EssenceSystem.Tests;

public sealed class TournamentGroundsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 27, 18, 30, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(typeof(RegisterTournamentCommand))]
    [InlineData(typeof(WithdrawTournamentRegistrationCommand))]
    [InlineData(typeof(ClaimTournamentRewardsCommand))]
    [InlineData(typeof(CreateTournamentTeamCommand))]
    [InlineData(typeof(InviteTournamentTeamMemberCommand))]
    [InlineData(typeof(AcceptTournamentTeamInviteCommand))]
    [InlineData(typeof(ApplyToTournamentTeamCommand))]
    [InlineData(typeof(AcceptTournamentTeamApplicationCommand))]
    [InlineData(typeof(KickTournamentTeamMemberCommand))]
    [InlineData(typeof(UpdateTournamentLoadoutCommand))]
    [InlineData(typeof(Application.UseCases.Colosseum.Tournaments.Commands.StartDevelopmentTournament.StartDevelopmentTournamentCommand))]
    public void Tournament_commands_opt_out_of_outer_transaction_pipeline(Type commandType)
    {
        Assert.True(
            Attribute.IsDefined(commandType, typeof(NonTransactionalAttribute)),
            $"{commandType.Name} should let TournamentGroundsService own its advisory-lock transaction.");
    }

    [Fact]
    public async Task EnsureUpcomingTournamentsAsync_creates_next_weekly_tournament_after_saturday_registration_close()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.EnsureUpcomingTournamentsAsync(CancellationToken.None);

        var tournament = await db.ArenaTournaments.SingleAsync();
        Assert.Equal("Weekly Open Grounds", tournament.Name);
        Assert.Equal(new DateTimeOffset(2026, 6, 29, 0, 0, 0, TimeSpan.Zero), tournament.RegistrationStartsAtUtc);
        Assert.Equal(new DateTimeOffset(2026, 7, 4, 0, 0, 0, TimeSpan.Zero), tournament.RegistrationEndsAtUtc);
        Assert.Equal(new DateTimeOffset(2026, 7, 4, 12, 0, 0, TimeSpan.Zero), tournament.StartsAtUtc);

        var definition = await db.TournamentDefinitions.SingleAsync();
        Assert.Equal("weekly-open-grounds", definition.Key);
        Assert.Equal(5 * 24 * 60, definition.RegistrationDurationMinutes);
        Assert.Equal(12 * 60, definition.StartDelayAfterRegistrationMinutes);
    }

    [Fact]
    public async Task EnsureUpcomingTournamentsAsync_moves_existing_upcoming_tournament_to_noon_without_changing_registration_close()
    {
        await using var db = CreateDbContext();
        var originalService = CreateService(
            db,
            options: new TournamentGroundsOptions
            {
                Enabled = true,
                UsePostgresAdvisoryLocks = false,
                DefaultStartDelayAfterRegistrationMinutes = 0
            });
        await originalService.EnsureUpcomingTournamentsAsync(CancellationToken.None);
        var tournament = await db.ArenaTournaments.SingleAsync();
        var registrationEndsAt = tournament.RegistrationEndsAtUtc;
        Assert.Equal(registrationEndsAt, tournament.StartsAtUtc);

        var updatedService = CreateService(
            db,
            options: new TournamentGroundsOptions
            {
                Enabled = true,
                UsePostgresAdvisoryLocks = false,
                DefaultStartDelayAfterRegistrationMinutes = 12 * 60
            });
        await updatedService.EnsureUpcomingTournamentsAsync(CancellationToken.None);

        Assert.Equal(registrationEndsAt, tournament.RegistrationEndsAtUtc);
        Assert.Equal(registrationEndsAt.AddHours(12), tournament.StartsAtUtc);
        Assert.Equal(
            12 * 60,
            (await db.TournamentDefinitions.SingleAsync()).StartDelayAfterRegistrationMinutes);
    }

    [Fact]
    public async Task EnsureUpcomingTournamentsAsync_acquires_schedule_lock()
    {
        await using var db = CreateDbContext();
        var lockService = new CapturingTournamentLockService();
        var service = CreateService(db, tournamentLockService: lockService);

        await service.EnsureUpcomingTournamentsAsync(CancellationToken.None);

        Assert.Equal(1, lockService.ScheduleLockCalls);
        Assert.Equal(0, lockService.TournamentLockCalls);
        Assert.Single(await db.ArenaTournaments.ToListAsync());
    }

    [Fact]
    public async Task GetStatusAsync_does_not_create_or_advance_tournaments()
    {
        await using var db = CreateDbContext();
        var lockService = new CapturingTournamentLockService();
        var service = CreateService(db, tournamentLockService: lockService);

        var status = await service.GetStatusAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(status.CurrentTournament);
        Assert.Empty(status.UpcomingTournaments);
        Assert.Empty(await db.ArenaTournaments.ToListAsync());
        Assert.Equal(0, lockService.ScheduleLockCalls);
        Assert.Equal(0, lockService.TournamentLockCalls);
    }

    [Fact]
    public async Task GetStatusAsync_includes_latest_completed_tournament_for_spectators()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var completed = SeedTournament(db, TournamentStatus.Completed);
        completed.CompletedAtUtc = Now.AddMinutes(-5);
        var scheduled = SeedTournament(db, TournamentStatus.Scheduled);
        scheduled.RegistrationStartsAtUtc = Now.AddHours(1);
        await db.SaveChangesAsync();

        var status = await service.GetStatusAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(scheduled.Id, status.CurrentTournament?.Id);
        var recent = Assert.Single(status.RecentTournaments);
        Assert.Equal(completed.Id, recent.Id);
        Assert.False(recent.IsRegistered);
    }

    [Fact]
    public void GetRewardTiers_returns_the_configured_placement_rewards()
    {
        using var db = CreateDbContext();
        var service = CreateService(
            db,
            options: new TournamentGroundsOptions
            {
                Rewards =
                [
                    new TournamentRewardTierOptions
                    {
                        Key = "winner",
                        MaxPlacement = 1,
                        ArenaGlory = 900
                    },
                    new TournamentRewardTierOptions
                    {
                        Key = "participant",
                        MaxPlacement = null,
                        ArenaGlory = 100
                    }
                ]
            });

        var tiers = service.GetRewardTiers();

        Assert.Collection(
            tiers,
            winner =>
            {
                Assert.Equal("winner", winner.Key);
                Assert.Equal(1, winner.MaxPlacement);
                Assert.Equal(900, winner.ArenaGlory);
            },
            participant =>
            {
                Assert.Equal("participant", participant.Key);
                Assert.Null(participant.MaxPlacement);
                Assert.Equal(100, participant.ArenaGlory);
            });
    }

    [Fact]
    public async Task StartDevelopmentTournamentAsync_creates_fills_and_starts_a_local_tournament()
    {
        await using var db = CreateDbContext();
        var combatExecutor = new QueuedCombatEngineExecutor(BattleOutcome.Victory);
        var options = new TournamentGroundsOptions
        {
            Enabled = true,
            DevelopmentToolsEnabled = true,
            UsePostgresAdvisoryLocks = false,
            DefaultMinParticipants = 4,
            DefaultMaxParticipants = 32
        };
        var service = CreateService(
            db,
            entityService: new DbEntityService(db),
            combatSetupService: new SimpleCombatSetupService(),
            combatEngineExecutor: combatExecutor,
            combatEncounterResultFactory: new PassthroughCombatEncounterResultFactory(),
            options: options);
        var player = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());
        for (var i = 0; i < 95; i++)
        {
            SeedDevelopmentGuestCharacter(db, i, 1400 - i);
        }
        await db.SaveChangesAsync();

        var result = await service.StartDevelopmentTournamentAsync(
            player.Id,
            CancellationToken.None);

        Assert.True(result.Started, result.ErrorMessage);
        Assert.Equal(96, result.RegisteredParticipantCount);
        Assert.Equal(32, result.TeamCount);
        var tournament = await db.ArenaTournaments.SingleAsync();
        Assert.Equal(TournamentStatus.InProgress, tournament.Status);
        Assert.Equal(96, await db.TournamentParticipants.CountAsync());
        Assert.Equal(
            32,
            await db.TournamentTeams.CountAsync(team => team.Status == TournamentTeamStatus.Active));
        Assert.All(
            await db.TournamentTeams.Where(team => team.Status == TournamentTeamStatus.Active).ToListAsync(),
            team => Assert.Equal(3, team.MemberCount));
        Assert.True(await db.TournamentParticipants.AnyAsync(
            participant => participant.CharacterId == player.Id));
        Assert.Equal(16, combatExecutor.ExecutionCount);
        Assert.Equal(
            16,
            await db.TournamentMatches.CountAsync(match => match.Status == TournamentMatchStatus.Resolving));

        var scheduledMatches = await db.TournamentMatches
            .Where(match => match.Status != TournamentMatchStatus.Bye)
            .OrderBy(match => match.RoundNumber)
            .ThenBy(match => match.MatchNumber)
            .ToListAsync();
        Assert.All(
            scheduledMatches.Where(match => match.RoundNumber == 1),
            match => Assert.Equal(Now, match.ScheduledAtUtc));
        Assert.All(
            scheduledMatches.Where(match => match.RoundNumber == 2),
            match => Assert.Equal(Now.AddMinutes(10), match.ScheduledAtUtc));
        Assert.All(
            scheduledMatches.Where(match => match.RoundNumber == 3),
            match => Assert.Equal(Now.AddMinutes(20), match.ScheduledAtUtc));
        Assert.Equal(
            [Now.AddMinutes(30), Now.AddMinutes(40)],
            scheduledMatches
                .Where(match => match.RoundNumber == 4)
                .Select(match => match.ScheduledAtUtc));
        Assert.Equal(
            Now.AddMinutes(50),
            Assert.Single(scheduledMatches, match => match.RoundNumber == 5).ScheduledAtUtc);
    }

    [Fact]
    public async Task StartDevelopmentTournamentAsync_does_nothing_when_tools_are_disabled()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var player = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());
        await db.SaveChangesAsync();

        var result = await service.StartDevelopmentTournamentAsync(
            player.Id,
            CancellationToken.None);

        Assert.False(result.Started);
        Assert.Contains("disabled", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.ArenaTournaments.ToListAsync());
        Assert.Empty(await db.TournamentParticipants.ToListAsync());
    }

    [Fact]
    public async Task RegisterAsync_creates_participant_and_snapshot()
    {
        await using var db = CreateDbContext();
        var realtime = new CapturingGameRealtimeBroadcaster();
        var service = CreateService(db, realtime);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationOpen);
        var character = SeedCharacter(db, rating: 1525, accountId: Guid.NewGuid());
        await db.SaveChangesAsync();

        var response = await service.RegisterAsync(character.Id, tournament.Id, CancellationToken.None);

        Assert.NotNull(response);
        Assert.True(response.Registered);
        Assert.Equal(1, tournament.RegisteredParticipantCount);

        var participant = await db.TournamentParticipants.SingleAsync();
        Assert.Equal(character.Id, participant.CharacterId);
        Assert.Equal(TournamentParticipantStatus.Registered, participant.Status);
        Assert.Equal(1525, participant.EntryArenaRating);
        Assert.NotEqual(Guid.Empty, participant.SnapshotId);

        var snapshot = await db.TournamentCombatSnapshots.SingleAsync();
        Assert.Equal(character.Id, snapshot.CharacterId);
        Assert.Equal(1525, snapshot.ArenaRatingAtSnapshot);
        using var payload = JsonDocument.Parse(snapshot.SnapshotJson);
        Assert.Equal(character.Id, payload.RootElement.GetProperty("characterId").GetGuid());
        Assert.Equal(1525, payload.RootElement.GetProperty("arenaRating").GetInt32());
        Assert.Equal("character-snapshot-v1", snapshot.SnapshotVersion);

        Assert.Contains(realtime.Events, e => e.Event == "TournamentRegistrationUpdated");
    }

    [Fact]
    public async Task RegisterAsync_rejects_second_character_from_same_account()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationOpen);
        var accountId = Guid.NewGuid();
        var first = SeedCharacter(db, rating: 1400, accountId);
        var second = SeedCharacter(db, rating: 1600, accountId);
        await db.SaveChangesAsync();

        var firstResponse = await service.RegisterAsync(first.Id, tournament.Id, CancellationToken.None);
        var secondResponse = await service.RegisterAsync(second.Id, tournament.Id, CancellationToken.None);

        Assert.NotNull(firstResponse);
        Assert.Null(secondResponse);
        Assert.Equal(1, await db.TournamentParticipants.CountAsync());
        Assert.Equal(1, tournament.RegisteredParticipantCount);
    }

    [Fact]
    public async Task WithdrawAsync_marks_participant_withdrawn_before_registration_closes()
    {
        await using var db = CreateDbContext();
        var realtime = new CapturingGameRealtimeBroadcaster();
        var service = CreateService(db, realtime);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationOpen);
        var character = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());
        await db.SaveChangesAsync();
        await service.RegisterAsync(character.Id, tournament.Id, CancellationToken.None);

        var response = await service.WithdrawAsync(character.Id, tournament.Id, CancellationToken.None);

        Assert.NotNull(response);
        Assert.True(response.Withdrawn);
        Assert.Equal(0, tournament.RegisteredParticipantCount);

        var participant = await db.TournamentParticipants.SingleAsync();
        Assert.Equal(TournamentParticipantStatus.Withdrawn, participant.Status);
        Assert.Contains(realtime.Events, e => e.Event == "TournamentRegistrationUpdated");
    }

    [Fact]
    public async Task AcceptTeamInviteAsync_rejects_and_cancels_invites_when_actual_team_is_full()
    {
        await using var db = CreateDbContext();
        var realtime = new CapturingGameRealtimeBroadcaster();
        var service = CreateService(db, realtime);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationOpen);
        var owner = SeedParticipant(db, tournament, 1600, 0);
        var secondMember = SeedParticipant(db, tournament, 1500, 1);
        var thirdMember = SeedParticipant(db, tournament, 1400, 2);
        var invited = SeedParticipant(db, tournament, 1300, 3);
        var team = SeedTeam(db, tournament, "Full Team", owner, secondMember, thirdMember);
        team.MemberCount = 2;
        var invite = new TournamentTeamInvite
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            TeamId = team.Id,
            Team = team,
            InviterParticipantId = owner.Id,
            InvitedParticipantId = invited.Id,
            Status = TournamentTeamRequestStatus.Pending,
            CreatedAtUtc = Now,
            UpdatedAtUtc = Now
        };
        db.TournamentTeamInvites.Add(invite);
        await db.SaveChangesAsync();

        var result = await service.AcceptTeamInviteAsync(invited.CharacterId, invite.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Contains("already full", result.ErrorMessage);
        Assert.Equal(TournamentTeamRequestStatus.Cancelled, invite.Status);
        Assert.Null(invited.TeamId);
        Assert.Equal(3, team.MemberCount);
        Assert.Contains(realtime.Events, e => e.Event == "TournamentTeamUpdated");
    }

    [Fact]
    public async Task AcceptTeamInviteAsync_uses_actual_capacity_and_cancels_requests_when_last_slot_is_filled()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationOpen);
        var owner = SeedParticipant(db, tournament, 1600, 0);
        var secondMember = SeedParticipant(db, tournament, 1500, 1);
        var invited = SeedParticipant(db, tournament, 1400, 2);
        var waitingInvitee = SeedParticipant(db, tournament, 1300, 3);
        var waitingApplicant = SeedParticipant(db, tournament, 1200, 4);
        var team = SeedTeam(db, tournament, "One Slot", owner, secondMember);
        team.MemberCount = 3;
        var acceptedInvite = new TournamentTeamInvite
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            TeamId = team.Id,
            Team = team,
            InviterParticipantId = owner.Id,
            InvitedParticipantId = invited.Id,
            Status = TournamentTeamRequestStatus.Pending,
            CreatedAtUtc = Now,
            UpdatedAtUtc = Now
        };
        var waitingInvite = new TournamentTeamInvite
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            TeamId = team.Id,
            Team = team,
            InviterParticipantId = owner.Id,
            InvitedParticipantId = waitingInvitee.Id,
            Status = TournamentTeamRequestStatus.Pending,
            CreatedAtUtc = Now,
            UpdatedAtUtc = Now
        };
        var waitingApplication = new TournamentTeamApplication
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            TeamId = team.Id,
            Team = team,
            ApplicantParticipantId = waitingApplicant.Id,
            Status = TournamentTeamRequestStatus.Pending,
            CreatedAtUtc = Now,
            UpdatedAtUtc = Now
        };
        db.TournamentTeamInvites.AddRange(acceptedInvite, waitingInvite);
        db.TournamentTeamApplications.Add(waitingApplication);
        await db.SaveChangesAsync();

        var result = await service.AcceptTeamInviteAsync(
            invited.CharacterId,
            acceptedInvite.Id,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Equal(team.Id, invited.TeamId);
        Assert.Equal(3, team.MemberCount);
        Assert.Equal(TournamentTeamRequestStatus.Accepted, acceptedInvite.Status);
        Assert.Equal(TournamentTeamRequestStatus.Cancelled, waitingInvite.Status);
        Assert.Equal(TournamentTeamRequestStatus.Cancelled, waitingApplication.Status);
    }

    [Fact]
    public async Task AcceptTeamApplicationAsync_cancels_remaining_invites_when_last_slot_is_filled()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationOpen);
        var owner = SeedParticipant(db, tournament, 1600, 0);
        var secondMember = SeedParticipant(db, tournament, 1500, 1);
        var applicant = SeedParticipant(db, tournament, 1400, 2);
        var invited = SeedParticipant(db, tournament, 1300, 3);
        var team = SeedTeam(db, tournament, "Application Slot", owner, secondMember);
        team.MemberCount = 3;
        var acceptedApplication = new TournamentTeamApplication
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            TeamId = team.Id,
            Team = team,
            ApplicantParticipantId = applicant.Id,
            Status = TournamentTeamRequestStatus.Pending,
            CreatedAtUtc = Now,
            UpdatedAtUtc = Now
        };
        var waitingInvite = new TournamentTeamInvite
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            TeamId = team.Id,
            Team = team,
            InviterParticipantId = owner.Id,
            InvitedParticipantId = invited.Id,
            Status = TournamentTeamRequestStatus.Pending,
            CreatedAtUtc = Now,
            UpdatedAtUtc = Now
        };
        db.TournamentTeamApplications.Add(acceptedApplication);
        db.TournamentTeamInvites.Add(waitingInvite);
        await db.SaveChangesAsync();

        var result = await service.AcceptTeamApplicationAsync(
            owner.CharacterId,
            acceptedApplication.Id,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Equal(team.Id, applicant.TeamId);
        Assert.Equal(3, team.MemberCount);
        Assert.Equal(TournamentTeamRequestStatus.Accepted, acceptedApplication.Status);
        Assert.Equal(TournamentTeamRequestStatus.Cancelled, waitingInvite.Status);
    }

    [Fact]
    public async Task RegisterAsync_reactivates_withdrawn_participant_instead_of_inserting_duplicate()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationOpen);
        var character = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());
        await db.SaveChangesAsync();

        var firstRegistration = await service.RegisterAsync(character.Id, tournament.Id, CancellationToken.None);
        var withdrawal = await service.WithdrawAsync(character.Id, tournament.Id, CancellationToken.None);
        character.ArenaProfile.Rating = 1625;
        var secondRegistration = await service.RegisterAsync(character.Id, tournament.Id, CancellationToken.None);

        Assert.NotNull(firstRegistration);
        Assert.NotNull(withdrawal);
        Assert.NotNull(secondRegistration);
        Assert.Equal(firstRegistration.ParticipantId, secondRegistration.ParticipantId);
        Assert.Equal(1, await db.TournamentParticipants.CountAsync());
        Assert.Equal(1, await db.TournamentCombatSnapshots.CountAsync());
        Assert.Equal(1, tournament.RegisteredParticipantCount);

        var participant = await db.TournamentParticipants.SingleAsync();
        Assert.Equal(TournamentParticipantStatus.Registered, participant.Status);
        Assert.Null(participant.TeamId);
        Assert.Equal(1625, participant.EntryArenaRating);
    }

    [Fact]
    public async Task UpdateLoadoutAsync_refreshes_team_member_combat_snapshot_and_preserves_entry_rating()
    {
        await using var db = CreateDbContext();
        var realtime = new CapturingGameRealtimeBroadcaster();
        var service = CreateService(db, realtime);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationOpen);
        var character = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());
        await db.SaveChangesAsync();

        var registration = await service.RegisterAsync(
            character.Id,
            tournament.Id,
            CancellationToken.None);
        var team = await service.CreateTeamAsync(
            character.Id,
            tournament.Id,
            "Snapshot Squad",
            CancellationToken.None);
        Assert.NotNull(registration);
        Assert.NotNull(team);

        var originalSnapshotId = (await db.TournamentCombatSnapshots.SingleAsync())
            .CharacterSnapshotId;
        character.Level = 27;
        character.ArenaProfile.Rating = 1750;
        await db.SaveChangesAsync();

        var result = await service.UpdateLoadoutAsync(
            character.Id,
            tournament.Id,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Succeeded, result.ErrorMessage);
        var tournamentSnapshot = await db.TournamentCombatSnapshots.SingleAsync();
        Assert.NotEqual(originalSnapshotId, tournamentSnapshot.CharacterSnapshotId);
        Assert.Equal(1500, tournamentSnapshot.ArenaRatingAtSnapshot);
        Assert.Equal(ArenaRank.GetTier(1500).Id, tournamentSnapshot.RankTierAtSnapshot);
        Assert.Contains("\"level\":27", tournamentSnapshot.SnapshotJson);
        var participant = await db.TournamentParticipants.SingleAsync();
        Assert.Equal(1500, participant.EntryArenaRating);
        Assert.Contains(realtime.Events, entry => entry.Event == "TournamentLoadoutUpdated");
    }

    [Fact]
    public async Task UpdateLoadoutAsync_requires_registered_participant_to_be_on_team()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationOpen);
        var character = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());
        await db.SaveChangesAsync();
        await service.RegisterAsync(character.Id, tournament.Id, CancellationToken.None);
        var snapshotCount = await db.CharacterSnapshots.CountAsync();

        var result = await service.UpdateLoadoutAsync(
            character.Id,
            tournament.Id,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Contains("team", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(snapshotCount, await db.CharacterSnapshots.CountAsync());
    }

    [Fact]
    public async Task AdvanceDueTournamentsAsync_cancels_registration_closed_tournament_below_minimum()
    {
        await using var db = CreateDbContext();
        var realtime = new CapturingGameRealtimeBroadcaster();
        var service = CreateService(db, realtime);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationClosed);
        tournament.RegisteredParticipantCount = 1;
        await db.SaveChangesAsync();

        await service.AdvanceDueTournamentsAsync(CancellationToken.None);

        Assert.Equal(TournamentStatus.Cancelled, tournament.Status);
        Assert.Equal("Minimum team count was not met.", tournament.CancellationReason);
        Assert.Contains(realtime.Events, e => e.Event == "TournamentStateChanged");
    }

    [Fact]
    public async Task GetDetailsAsync_does_not_publish_realtime_when_tournament_does_not_advance()
    {
        await using var db = CreateDbContext();
        var realtime = new CapturingGameRealtimeBroadcaster();
        var service = CreateService(db, realtime);
        var tournament = SeedTournament(db, TournamentStatus.Scheduled);
        var character = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());
        tournament.RegistrationStartsAtUtc = Now.AddHours(1);
        tournament.RegistrationEndsAtUtc = Now.AddHours(3);
        tournament.StartsAtUtc = Now.AddHours(4);
        await db.SaveChangesAsync();

        var details = await service.GetDetailsAsync(character.Id, tournament.Id, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(TournamentStatus.Scheduled, tournament.Status);
        Assert.Empty(realtime.Events);
    }

    [Fact]
    public async Task GetDetailsAsync_does_not_advance_due_tournament()
    {
        await using var db = CreateDbContext();
        var lockService = new CapturingTournamentLockService();
        var service = CreateService(db, tournamentLockService: lockService);
        var tournament = SeedTournament(db, TournamentStatus.Scheduled);
        var character = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());
        tournament.RegistrationStartsAtUtc = Now.AddMinutes(-10);
        await db.SaveChangesAsync();

        var details = await service.GetDetailsAsync(character.Id, tournament.Id, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(TournamentStatus.Scheduled, tournament.Status);
        Assert.Equal(0, lockService.ScheduleLockCalls);
        Assert.Equal(0, lockService.TournamentLockCalls);
    }

    [Fact]
    public async Task RegisterAsync_does_not_advance_due_scheduled_tournament()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var tournament = SeedTournament(db, TournamentStatus.Scheduled);
        var character = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());
        tournament.RegistrationStartsAtUtc = Now.AddMinutes(-10);
        tournament.RegistrationEndsAtUtc = Now.AddMinutes(30);
        await db.SaveChangesAsync();

        var response = await service.RegisterAsync(character.Id, tournament.Id, CancellationToken.None);

        Assert.Null(response);
        Assert.Equal(TournamentStatus.Scheduled, tournament.Status);
        Assert.Empty(await db.TournamentParticipants.ToListAsync());
    }

    [Fact]
    public async Task WithdrawAsync_does_not_advance_expired_open_tournament()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationOpen);
        var participant = SeedParticipant(db, tournament, rating: 1500, registeredOffsetMinutes: 0);
        tournament.RegistrationEndsAtUtc = Now.AddMinutes(-1);
        tournament.RegisteredParticipantCount = 1;
        await db.SaveChangesAsync();

        var response = await service.WithdrawAsync(participant.CharacterId, tournament.Id, CancellationToken.None);

        Assert.Null(response);
        Assert.Equal(TournamentStatus.RegistrationOpen, tournament.Status);
        Assert.Equal(TournamentParticipantStatus.Registered, participant.Status);
    }

    [Fact]
    public async Task AdvanceDueTournamentsAsync_generates_single_elimination_bracket_once()
    {
        await using var db = CreateDbContext();
        var realtime = new CapturingGameRealtimeBroadcaster();
        var service = CreateService(
            db,
            realtime,
            options: new TournamentGroundsOptions
            {
                DevelopmentToolsEnabled = true,
                UsePostgresAdvisoryLocks = false
            });
        var tournament = SeedTournament(db, TournamentStatus.RegistrationClosed);
        tournament.StartsAtUtc = Now.AddHours(1);

        for (var i = 0; i < 10; i++)
        {
            SeedParticipant(db, tournament, rating: 1800 - (i * 100), registeredOffsetMinutes: i);
        }

        tournament.RegisteredParticipantCount = 10;
        await db.SaveChangesAsync();

        await service.AdvanceDueTournamentsAsync(CancellationToken.None);
        await service.AdvanceDueTournamentsAsync(CancellationToken.None);

        Assert.Equal(TournamentStatus.BracketGenerated, tournament.Status);
        Assert.Equal(2, await db.TournamentRounds.CountAsync());
        Assert.Equal(3, await db.TournamentMatches.CountAsync());
        Assert.Equal(0, await db.TournamentMatches.CountAsync(m => m.Status == TournamentMatchStatus.Bye));
        Assert.Contains(realtime.Events, e => e.Event == "TournamentBracketGenerated");

        var participants = await db.TournamentParticipants.OrderBy(p => p.Seed).ToListAsync();
        Assert.Equal([1, 2, 2, 2, 3, 3, 3, 4, 4, 4], participants.Select(p => p.Seed));
        Assert.All(participants, p => Assert.Equal(TournamentParticipantStatus.Active, p.Status));

        var teams = await db.TournamentTeams
            .Where(t => t.Status == TournamentTeamStatus.Active)
            .OrderBy(t => t.Seed)
            .ToListAsync();
        Assert.Equal([1, 3, 3, 3], teams.Select(t => t.MemberCount));
        Assert.All(teams, team => Assert.Equal(TournamentTeamStatus.Active, team.Status));
    }

    [Fact]
    public async Task AdvanceDueTournamentsAsync_does_not_create_teams_outside_development_but_merges_existing_teams()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationClosed);
        tournament.StartsAtUtc = Now.AddHours(1);
        tournament.MinParticipants = 2;
        tournament.Definition.MinParticipants = 2;

        var first = SeedParticipant(db, tournament, rating: 1800, registeredOffsetMinutes: 0);
        var second = SeedParticipant(db, tournament, rating: 1700, registeredOffsetMinutes: 1);
        var third = SeedParticipant(db, tournament, rating: 1600, registeredOffsetMinutes: 2);
        var fourth = SeedParticipant(db, tournament, rating: 1500, registeredOffsetMinutes: 3);
        var fifth = SeedParticipant(db, tournament, rating: 1400, registeredOffsetMinutes: 4);
        var unassignedOne = SeedParticipant(db, tournament, rating: 1300, registeredOffsetMinutes: 5);
        var unassignedTwo = SeedParticipant(db, tournament, rating: 1200, registeredOffsetMinutes: 6);
        SeedTeam(db, tournament, "Player Pair One", first, second);
        SeedTeam(db, tournament, "Player Pair Two", third, fourth);
        SeedTeam(db, tournament, "Player Solo", fifth);
        tournament.RegisteredParticipantCount = 7;
        await db.SaveChangesAsync();

        await service.AdvanceDueTournamentsAsync(CancellationToken.None);

        Assert.Equal(TournamentStatus.BracketGenerated, tournament.Status);
        var teams = await db.TournamentTeams.ToListAsync();
        Assert.Equal(3, teams.Count);
        var activeTeams = teams
            .Where(team => team.Status == TournamentTeamStatus.Active)
            .OrderBy(team => team.MemberCount)
            .ToList();
        Assert.Equal([2, 3], activeTeams.Select(team => team.MemberCount));
        Assert.Single(teams, team => team.Status == TournamentTeamStatus.Disbanded);
        Assert.Null(unassignedOne.TeamId);
        Assert.Null(unassignedTwo.TeamId);
        Assert.Equal(TournamentParticipantStatus.Registered, unassignedOne.Status);
        Assert.Equal(TournamentParticipantStatus.Registered, unassignedTwo.Status);
        Assert.Single(await db.TournamentRounds.ToListAsync());
        Assert.Single(await db.TournamentMatches.ToListAsync());
    }

    [Fact]
    public async Task AdvanceDueTournamentsAsync_merges_two_player_team_with_one_player_team()
    {
        await using var db = CreateDbContext();
        var service = CreateService(
            db,
            options: new TournamentGroundsOptions
            {
                DevelopmentToolsEnabled = true,
                UsePostgresAdvisoryLocks = false
            });
        var tournament = SeedTournament(db, TournamentStatus.RegistrationClosed);
        tournament.MinParticipants = 1;
        tournament.Definition.MinParticipants = 1;

        var first = SeedParticipant(db, tournament, rating: 1800, registeredOffsetMinutes: 0);
        var second = SeedParticipant(db, tournament, rating: 1700, registeredOffsetMinutes: 1);
        var third = SeedParticipant(db, tournament, rating: 1600, registeredOffsetMinutes: 2);
        SeedTeam(db, tournament, "Two Stack", first, second);
        SeedTeam(db, tournament, "Solo Stack", third);
        tournament.RegisteredParticipantCount = 3;
        await db.SaveChangesAsync();

        await service.AdvanceDueTournamentsAsync(CancellationToken.None);

        Assert.Equal(TournamentStatus.Completed, tournament.Status);
        var activeTeam = await db.TournamentTeams.SingleAsync(t => t.Status == TournamentTeamStatus.Champion);
        Assert.Equal(3, activeTeam.MemberCount);
        Assert.Equal(3, await db.TournamentParticipants.CountAsync(p => p.TeamId == activeTeam.Id));
        Assert.Single(await db.TournamentTeams.Where(t => t.Status == TournamentTeamStatus.Disbanded).ToListAsync());
    }

    [Fact]
    public async Task AdvanceDueTournamentsAsync_does_not_merge_six_two_player_teams()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationClosed);
        tournament.StartsAtUtc = Now.AddHours(1);
        tournament.MinParticipants = 6;
        tournament.Definition.MinParticipants = 6;

        var participants = Enumerable.Range(0, 12)
            .Select(i => SeedParticipant(db, tournament, rating: 1800 - (i * 50), registeredOffsetMinutes: i))
            .ToArray();
        for (var teamIndex = 0; teamIndex < 6; teamIndex++)
        {
            SeedTeam(
                db,
                tournament,
                $"Pair {teamIndex + 1}",
                participants[teamIndex * 2],
                participants[(teamIndex * 2) + 1]);
        }

        tournament.RegisteredParticipantCount = 12;
        await db.SaveChangesAsync();

        await service.AdvanceDueTournamentsAsync(CancellationToken.None);

        Assert.Equal(TournamentStatus.BracketGenerated, tournament.Status);
        var activeTeams = await db.TournamentTeams
            .Where(t => t.Status == TournamentTeamStatus.Active)
            .OrderBy(t => t.Seed)
            .ToListAsync();
        Assert.Equal(6, activeTeams.Count);
        Assert.All(activeTeams, team => Assert.Equal(2, team.MemberCount));
        Assert.Empty(await db.TournamentTeams.Where(t => t.Status == TournamentTeamStatus.Disbanded).ToListAsync());
    }

    [Fact]
    public async Task AdvanceDueTournamentsAsync_adds_three_solo_teams_to_three_of_six_two_player_teams()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationClosed);
        tournament.StartsAtUtc = Now.AddHours(1);
        tournament.MinParticipants = 6;
        tournament.Definition.MinParticipants = 6;

        var participants = Enumerable.Range(0, 15)
            .Select(i => SeedParticipant(db, tournament, rating: 2000 - (i * 50), registeredOffsetMinutes: i))
            .ToArray();
        var pairTeams = new List<TournamentTeam>();
        for (var teamIndex = 0; teamIndex < 6; teamIndex++)
        {
            pairTeams.Add(SeedTeam(
                db,
                tournament,
                $"Pair {teamIndex + 1}",
                participants[teamIndex * 2],
                participants[(teamIndex * 2) + 1]));
        }

        for (var soloIndex = 0; soloIndex < 3; soloIndex++)
        {
            SeedTeam(
                db,
                tournament,
                $"Solo {soloIndex + 1}",
                participants[12 + soloIndex]);
        }

        tournament.RegisteredParticipantCount = 15;
        await db.SaveChangesAsync();

        await service.AdvanceDueTournamentsAsync(CancellationToken.None);

        Assert.Equal(TournamentStatus.BracketGenerated, tournament.Status);
        var activeTeams = await db.TournamentTeams
            .Where(team => team.Status == TournamentTeamStatus.Active)
            .OrderBy(team => team.MemberCount)
            .ToListAsync();
        Assert.Equal(6, activeTeams.Count);
        Assert.Equal([2, 2, 2, 3, 3, 3], activeTeams.Select(team => team.MemberCount));
        Assert.Equal(3, await db.TournamentTeams.CountAsync(team => team.Status == TournamentTeamStatus.Disbanded));

        for (var teamIndex = 0; teamIndex < pairTeams.Count; teamIndex++)
        {
            Assert.Equal(TournamentTeamStatus.Active, pairTeams[teamIndex].Status);
            Assert.Equal(pairTeams[teamIndex].Id, participants[teamIndex * 2].TeamId);
            Assert.Equal(pairTeams[teamIndex].Id, participants[(teamIndex * 2) + 1].TeamId);
        }
    }

    [Fact]
    public async Task ClaimRewardsAsync_claims_unclaimed_rewards_once()
    {
        await using var db = CreateDbContext();
        var realtime = new CapturingGameRealtimeBroadcaster();
        var service = CreateService(db, realtime);
        var tournament = SeedTournament(db, TournamentStatus.Completed);
        var character = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());

        await db.TournamentRewardGrants.AddAsync(new TournamentRewardGrant
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Tournament = tournament,
            CharacterId = character.Id,
            RewardKey = "placement-1",
            Placement = 1,
            ArenaGlory = 80,
            Cinders = 400,
            Soulstones = 8,
            Status = TournamentRewardStatus.Unclaimed,
            CreatedAtUtc = Now
        });
        await db.SaveChangesAsync();

        var firstClaim = await service.ClaimRewardsAsync(character.Id, tournament.Id, CancellationToken.None);
        var secondClaim = await service.ClaimRewardsAsync(character.Id, tournament.Id, CancellationToken.None);

        Assert.True(firstClaim.Claimed);
        Assert.Equal(80, firstClaim.ArenaGlory);
        Assert.Equal(400, firstClaim.Cinders);
        Assert.Equal(8, firstClaim.Soulstones);
        Assert.False(secondClaim.Claimed);

        Assert.Equal(80, character.ArenaProfile.Glory);
        Assert.Equal(400, character.Cinders);
        Assert.Equal(8, character.Soulstones);
        Assert.Equal(TournamentRewardStatus.Claimed, (await db.TournamentRewardGrants.SingleAsync()).Status);
        Assert.Contains(realtime.Events, e => e.Event == "TournamentRewardsAvailable");
    }

    [Fact]
    public async Task ClaimRewardsAsync_grants_finalist_milestone_items_and_sigil_fragments()
    {
        await using var db = CreateDbContext();
        var inventory = new RecordingInventoryService();
        var itemBases = new FakeItemBaseRepository(
        [
            new ItemBase
            {
                Id = CatalystSelectionCrateCatalog.ItemBaseId,
                Name = "Catalyst Selection Cache",
                ItemType = ItemType.Resource,
                Stackable = true
            },
            new ItemBase
            {
                Id = BlueprintSelectionBoxCatalog.ItemBaseId,
                Name = "Blueprint Selection Box",
                ItemType = ItemType.Resource,
                Stackable = true
            }
        ]);
        var service = CreateService(
            db,
            inventoryService: inventory,
            itemBaseRepository: itemBases);
        var tournament = SeedTournament(db, TournamentStatus.Completed);
        var character = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());

        await db.TournamentRewardGrants.AddAsync(new TournamentRewardGrant
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Tournament = tournament,
            CharacterId = character.Id,
            RewardKey = "finalist",
            Placement = 2,
            ArenaGlory = 425,
            Soulstones = 40,
            CatalystSelectionCaches = 1,
            BlueprintSelectionBoxes = 1,
            SigilFragments = 20,
            Status = TournamentRewardStatus.Unclaimed,
            CreatedAtUtc = Now
        });
        await db.SaveChangesAsync();

        var claim = await service.ClaimRewardsAsync(
            character.Id,
            tournament.Id,
            CancellationToken.None);

        Assert.True(claim.Claimed);
        Assert.Equal(425, claim.ArenaGlory);
        Assert.Equal(40, claim.Soulstones);
        Assert.Equal(20, claim.SigilFragments);
        Assert.Equal(1, claim.CatalystSelectionCaches);
        Assert.Equal(1, claim.BlueprintSelectionBoxes);
        Assert.NotNull(claim.InventoryGrantId);
        Assert.Equal(20, character.SigilFragments);
        Assert.Equal(2, claim.InventoryRewards.Count);
        Assert.Equal(2, inventory.AddedRewards.Count);
        Assert.Contains(
            inventory.AddedRewards,
            reward => reward.ItemInstance.ItemBaseId == CatalystSelectionCrateCatalog.ItemBaseId && reward.Quantity == 1);
        Assert.Contains(
            inventory.AddedRewards,
            reward => reward.ItemInstance.ItemBaseId == BlueprintSelectionBoxCatalog.ItemBaseId && reward.Quantity == 1);
    }

    [Fact]
    public async Task AdvanceDueTournamentsAsync_resolves_combat_records_history_and_grants_rewards()
    {
        await using var db = CreateDbContext();
        var realtime = new CapturingGameRealtimeBroadcaster();
        var combatExecutor = new QueuedCombatEngineExecutor(
            BattleOutcome.Victory,
            BattleOutcome.Victory,
            BattleOutcome.Victory);
        var outbox = new RecordingGameEventOutbox();
        var clock = new MutableTimeProvider(Now);
        var service = CreateService(
            db,
            realtime,
            entityService: new DbEntityService(db),
            combatSetupService: new SimpleCombatSetupService(),
            combatEngineExecutor: combatExecutor,
            combatEncounterResultFactory: new PassthroughCombatEncounterResultFactory(),
            timeProvider: clock,
            options: new TournamentGroundsOptions
            {
                DevelopmentToolsEnabled = true,
                UsePostgresAdvisoryLocks = false
            },
            outbox: outbox);
        var tournament = SeedTournament(db, TournamentStatus.RegistrationClosed);
        tournament.StartsAtUtc = Now.AddMinutes(-1);
        tournament.RoundIntervalMinutes = 0;

        for (var i = 0; i < 10; i++)
        {
            SeedParticipant(db, tournament, rating: 1800 - (i * 100), registeredOffsetMinutes: i);
        }

        tournament.RegisteredParticipantCount = 10;
        await db.SaveChangesAsync();

        await service.AdvanceDueTournamentsAsync(CancellationToken.None);

        Assert.Equal(TournamentStatus.InProgress, tournament.Status);
        Assert.Equal(1, combatExecutor.ExecutionCount);
        var liveMatch = Assert.Single(
            await db.TournamentMatches
                .Where(m => m.Status == TournamentMatchStatus.Resolving)
                .ToListAsync());
        Assert.Null(liveMatch.WinnerParticipantId);
        Assert.Null(liveMatch.BattleHistoryId);
        Assert.Empty(await db.ColosseumMatches.ToListAsync());
        Assert.Empty(await db.TournamentRewardGrants.ToListAsync());
        Assert.Collection(
            outbox.Events.Where(entry => entry.EventType == GameEventTypes.TournamentChatAnnouncement),
            entry =>
            {
                var announcement = Assert.IsType<TournamentChatAnnouncementPayload>(entry.Payload);
                Assert.Equal("Tournament Grounds has started! Enter the Colosseum to follow the action.", announcement.Body);
                Assert.Equal("/game/city/colosseum?tab=tournaments", announcement.TargetUrl);
            },
            entry =>
            {
                var announcement = Assert.IsType<TournamentChatAnnouncementPayload>(entry.Payload);
                Assert.Contains("has started!", announcement.Body);
                Assert.Equal("/game/city/colosseum?tab=tournaments", announcement.TargetUrl);
            });

        await service.AdvanceDueTournamentsAsync(CancellationToken.None);
        Assert.Equal(1, combatExecutor.ExecutionCount);
        Assert.Equal(TournamentMatchStatus.Resolving, liveMatch.Status);

        clock.SetUtcNow(liveMatch.PlaybackEndsAtUtc!.Value);
        await service.AdvanceDueTournamentsAsync(CancellationToken.None);
        Assert.Equal(TournamentMatchStatus.Resolving, liveMatch.Status);
        Assert.DoesNotContain(
            outbox.Events,
            entry => entry.EventType == GameEventTypes.TournamentBattleCompleted);

        clock.SetUtcNow(liveMatch.PlaybackEndsAtUtc.Value.AddSeconds(1));
        await service.AdvanceDueTournamentsAsync(CancellationToken.None);
        Assert.Equal(1, await db.TournamentMatches.CountAsync(m => m.Status == TournamentMatchStatus.Completed));
        Assert.Equal(1, combatExecutor.ExecutionCount);

        clock.SetUtcNow(Now.AddMinutes(10));
        await service.AdvanceDueTournamentsAsync(CancellationToken.None);
        Assert.Equal(2, combatExecutor.ExecutionCount);
        var secondLiveMatch = Assert.Single(
            await db.TournamentMatches
                .Where(m => m.Status == TournamentMatchStatus.Resolving)
                .ToListAsync());
        clock.SetUtcNow(secondLiveMatch.PlaybackEndsAtUtc!.Value.AddSeconds(1));
        await service.AdvanceDueTournamentsAsync(CancellationToken.None);

        var finalMatch = Assert.Single(
            await db.TournamentMatches
                .Where(m => m.RoundNumber == 2)
                .ToListAsync());
        var finalRound = Assert.Single(
            await db.TournamentRounds
                .Where(r => r.RoundNumber == 2)
                .ToListAsync());
        var expectedFinalStart = clock.GetUtcNow().AddSeconds(10);
        Assert.Equal(expectedFinalStart, finalMatch.ScheduledAtUtc);
        Assert.Equal(expectedFinalStart, finalRound.StartsAtUtc);
        Assert.Contains(
            outbox.Events,
            entry => entry.EventType == GameEventTypes.TournamentGroundsUpdated
                     && entry.Payload is TournamentGroundsUpdated update
                     && update.Event == "TournamentStateChanged"
                     && update.NextActionAtUtc == expectedFinalStart);

        clock.SetUtcNow(expectedFinalStart);
        await service.AdvanceDueTournamentsAsync(CancellationToken.None);
        Assert.Equal(3, combatExecutor.ExecutionCount);
        Assert.Contains(
            outbox.Events,
            entry => entry.EventType == GameEventTypes.TournamentGroundsUpdated
                     && entry.Payload is TournamentGroundsUpdated update
                     && update.Event == "TournamentStateChanged"
                     && update.NextActionAtUtc == finalMatch.PlaybackEndsAtUtc);
        clock.SetUtcNow(finalMatch.PlaybackEndsAtUtc!.Value.AddSeconds(1));
        await service.AdvanceDueTournamentsAsync(CancellationToken.None);

        Assert.Equal(TournamentStatus.Completed, tournament.Status);
        Assert.Equal(3, combatExecutor.ExecutionCount);
        Assert.Equal(3, await db.ColosseumMatches.CountAsync());
        Assert.Equal(3, await db.TournamentCombatReplays.CountAsync());
        var rewardGrants = await db.TournamentRewardGrants.ToListAsync();
        Assert.Equal(10, rewardGrants.Count);
        Assert.All(rewardGrants, reward =>
        {
            Assert.InRange(reward.ArenaGlory, 250, 500);
            Assert.InRange(reward.Soulstones, 20, 50);
            Assert.Equal(0, reward.Cinders);
        });
        Assert.All(rewardGrants.Where(reward => reward.Placement <= 8), reward =>
            Assert.Equal(1, reward.CatalystSelectionCaches));
        Assert.All(rewardGrants.Where(reward => reward.Placement <= 4), reward =>
            Assert.Equal(1, reward.BlueprintSelectionBoxes));
        Assert.All(rewardGrants.Where(reward => reward.Placement <= 2), reward =>
            Assert.Equal(20, reward.SigilFragments));
        Assert.Contains(realtime.Events, e => e.Event == "TournamentCompleted");
        var battleEvents = outbox.Events
            .Where(entry => entry.EventType == GameEventTypes.TournamentBattleCompleted)
            .ToList();
        Assert.All(
            battleEvents,
            entry => Assert.Equal(GameEventTypes.TournamentBattleCompleted, entry.EventType));
        Assert.Equal(
            10,
            battleEvents.Select(entry => entry.CharacterId).Distinct().Count());
        Assert.Equal(
            3,
            outbox.Events.Count(entry => entry.EventType == GameEventTypes.TournamentChatAnnouncement));

        var matches = await db.TournamentMatches.OrderBy(m => m.RoundNumber).ThenBy(m => m.MatchNumber).ToListAsync();
        Assert.All(matches, match =>
        {
            Assert.Equal(TournamentMatchStatus.Completed, match.Status);
            Assert.NotNull(match.CombatSessionId);
            Assert.Equal(match.Id, match.BattleHistoryId);
        });
        Assert.Equal(
            TimeSpan.FromMinutes(10),
            matches[1].ScheduledAtUtc!.Value - matches[0].ScheduledAtUtc!.Value);
        Assert.Equal(
            matches[1].ResolvedAtUtc!.Value.AddSeconds(10),
            matches[2].ScheduledAtUtc);

        var championTeam = await db.TournamentTeams.SingleAsync(t => t.Status == TournamentTeamStatus.Champion);

        var championMembers = await db.TournamentParticipants
            .Where(p => p.Status == TournamentParticipantStatus.Champion)
            .OrderBy(p => p.RegisteredAtUtc)
            .ToListAsync();
        Assert.Equal(championTeam.MemberCount, championMembers.Count);
        var champion = championMembers[0];
        Assert.Equal(1, champion.Seed);
        Assert.Equal(1, champion.FinalPlacement);

        var history = await db.ColosseumMatches.OrderBy(h => h.PlayedAt).ToListAsync();
        Assert.All(history, h =>
        {
            Assert.Equal(h.CharacterARatingBefore, h.CharacterARatingAfter);
            Assert.Equal(h.CharacterBRatingBefore, h.CharacterBRatingAfter);
            Assert.Equal(0, h.CharacterARatingDelta);
            Assert.Equal(0, h.CharacterBRatingDelta);
            Assert.StartsWith("Tournament", h.Outcome);
        });

        var bracket = await service.GetBracketAsync(champion.CharacterId, tournament.Id, CancellationToken.None);
        Assert.NotNull(bracket);
        Assert.All(bracket.Rounds.SelectMany(r => r.Matches), match => Assert.NotNull(match.BattleHistoryId));

        var replayMatch = matches[0];
        var replay = await service.GetMatchReplayAsync(champion.CharacterId, tournament.Id, replayMatch.Id, CancellationToken.None);
        Assert.NotNull(replay);
        Assert.NotEmpty(replay.EventLog);
        Assert.Equal(BattleOutcome.Victory, replay.Outcome);

        var spectatorId = Guid.NewGuid();
        var manifest = await service.GetMatchPlaybackAsync(
            spectatorId,
            tournament.Id,
            replayMatch.Id,
            CancellationToken.None);
        Assert.NotNull(manifest);
        Assert.Equal(TournamentCombatReplay.CompactBundleSchemaVersion, manifest.SchemaVersion);
        Assert.Equal(3000, manifest.OvertimeStartsAtTick);
        Assert.Equal(3000, manifest.OvertimeDurationTicks);
        Assert.Equal(100, manifest.OvertimePowerIncreaseIntervalTicks);
        Assert.Equal(10, manifest.OvertimePowerIncreasePercent);
        Assert.True(manifest.IsCompleted);
        var bundleContent = await service.GetMatchPlaybackBundleAsync(
            spectatorId,
            tournament.Id,
            replayMatch.Id,
            CancellationToken.None);
        Assert.NotNull(bundleContent);
        Assert.Equal("br", bundleContent.ContentEncoding);
        using var compressed = new MemoryStream(bundleContent.Bytes);
        using var brotli = new BrotliStream(compressed, CompressionMode.Decompress);
        var playbackJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        playbackJsonOptions.Converters.Add(new JsonStringEnumConverter());
        var playbackBundle = await JsonSerializer.DeserializeAsync<TournamentPlaybackBundleDto>(
            brotli,
            playbackJsonOptions);
        Assert.NotNull(playbackBundle);
        Assert.NotEmpty(playbackBundle.Frames);
        Assert.True(playbackBundle.Frames[^1].IsFinal);

        var historyEntries = await service.GetHistoryAsync(champion.CharacterId, CancellationToken.None);
        var historyEntry = Assert.Single(historyEntries);
        Assert.Equal(tournament.Id, historyEntry.TournamentId);
        Assert.Equal("Completed", historyEntry.Status);
        Assert.Equal(1, historyEntry.FinalPlacement);
        Assert.Equal("Unclaimed", historyEntry.RewardStatus);
        Assert.Equal(3, historyEntry.ReplayCount);

        var hallOfFame = await service.GetHallOfFameAsync(CancellationToken.None);
        var hallEntry = Assert.Single(hallOfFame);
        Assert.Equal(tournament.Id, hallEntry.TournamentId);
        Assert.Equal(championTeam.OwnerParticipantId, hallEntry.ChampionParticipantId);
        Assert.Equal(1, hallEntry.ChampionSeed);
        Assert.Equal(3, hallEntry.ReplayCount);

        var seasonLeaderboard = await service.GetSeasonLeaderboardAsync(CancellationToken.None);
        var championEntry = Assert.Single(seasonLeaderboard, entry => entry.CharacterId == champion.CharacterId);
        Assert.Equal(1, championEntry.Rank);
        Assert.Equal(100, championEntry.Points);
        Assert.Equal(1, championEntry.Championships);
        Assert.Equal("2026-06", championEntry.SeasonKey);
    }

    [Theory]
    [InlineData(120, 80, 1, TournamentMatchOutcome.DrawAdvancedByDamage)]
    [InlineData(80, 120, 2, TournamentMatchOutcome.DrawAdvancedByDamage)]
    [InlineData(100, 100, 1, TournamentMatchOutcome.DrawAdvancedBySeed)]
    public async Task AdvanceDueTournamentsAsync_resolves_draws_by_total_team_damage(
        int friendlyDamage,
        int hostileDamage,
        int expectedWinnerSeed,
        TournamentMatchOutcome expectedOutcome)
    {
        await using var db = CreateDbContext();
        var clock = new MutableTimeProvider(Now);
        var service = CreateService(
            db,
            entityService: new DbEntityService(db),
            combatSetupService: new SimpleCombatSetupService(),
            combatEngineExecutor: new DrawDamageCombatEngineExecutor(friendlyDamage, hostileDamage),
            combatEncounterResultFactory: new PassthroughCombatEncounterResultFactory(),
            timeProvider: clock,
            options: new TournamentGroundsOptions
            {
                DevelopmentToolsEnabled = true,
                UsePostgresAdvisoryLocks = false
            });
        var tournament = SeedTournament(db, TournamentStatus.RegistrationClosed);
        tournament.StartsAtUtc = Now.AddMinutes(-1);
        tournament.RoundIntervalMinutes = 0;
        tournament.MinParticipants = 2;
        tournament.Definition.MinParticipants = 2;

        for (var i = 0; i < 6; i++)
        {
            SeedParticipant(db, tournament, rating: 1800 - (i * 100), registeredOffsetMinutes: i);
        }

        tournament.RegisteredParticipantCount = 6;
        await db.SaveChangesAsync();

        await service.AdvanceDueTournamentsAsync(CancellationToken.None);

        var match = await db.TournamentMatches.SingleAsync();
        Assert.Equal(TournamentMatchStatus.Resolving, match.Status);

        clock.SetUtcNow(match.PlaybackEndsAtUtc!.Value.AddSeconds(1));
        await service.AdvanceDueTournamentsAsync(CancellationToken.None);

        var winner = await db.TournamentTeams.SingleAsync(team => team.Id == match.WinnerParticipantId);
        Assert.Equal(expectedWinnerSeed, winner.Seed);
        Assert.Equal(expectedOutcome, match.Outcome);
        Assert.Equal(TournamentStatus.Completed, tournament.Status);
    }

    [Fact]
    public async Task TournamentGroundsProgressionJob_creates_registers_advances_and_rewards_on_postgres()
    {
        var connectionString = Environment.GetEnvironmentVariable("LL_TEST_TOURNAMENT_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var schemaName = $"ll_tournament_job_tests_{Guid.NewGuid():N}";
        await using var adminDb = CreatePostgresDbContext(connectionString);
        var createSchemaSql = $"CREATE SCHEMA \"{schemaName}\"";
        var dropSchemaSql = $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE";
        await adminDb.Database.ExecuteSqlRawAsync(createSchemaSql);

        try
        {
            var isolatedConnectionString = WithSearchPath(connectionString, schemaName);
            await using (var migrationDb = CreatePostgresDbContext(isolatedConnectionString, schemaName))
            {
                await migrationDb.Database.MigrateAsync();
            }

            var clock = new MutableTimeProvider(new DateTimeOffset(2026, 6, 29, 0, 5, 0, TimeSpan.Zero));
            var options = new TournamentGroundsOptions
            {
                Enabled = true,
                DevelopmentToolsEnabled = true,
                AllowWithdrawDuringRegistration = true,
                DefaultMinParticipants = 4,
                DefaultMaxParticipants = 32,
                DefaultRoundIntervalMinutes = 0
            };

            Guid tournamentId;
            var characterIds = new List<Guid>();
            await using (var db = CreatePostgresDbContext(isolatedConnectionString, schemaName))
            {
                var realtime = new CapturingGameRealtimeBroadcaster();
                var service = CreateService(db, realtime, timeProvider: clock, options: options);
                var job = CreateProgressionJob(db, service, options);

                await job.Execute(new TournamentJobExecutionContext(clock.GetUtcNow()));

                var tournament = await db.ArenaTournaments.SingleAsync();
                tournamentId = tournament.Id;
                Assert.Equal(TournamentStatus.RegistrationOpen, tournament.Status);

                for (var i = 0; i < 10; i++)
                {
                    characterIds.Add(SeedCharacter(db, rating: 1800 - (i * 100), accountId: Guid.NewGuid()).Id);
                }

                await db.SaveChangesAsync();

                foreach (var characterId in characterIds)
                {
                    var registration = await service.RegisterAsync(characterId, tournamentId, CancellationToken.None);
                    Assert.NotNull(registration);
                    Assert.True(registration.Registered);
                }

                Assert.Equal(10, await db.TournamentParticipants.CountAsync(p => p.TournamentId == tournamentId));
            }

            clock.SetUtcNow(new DateTimeOffset(2026, 7, 4, 0, 5, 0, TimeSpan.Zero));
            await using (var db = CreatePostgresDbContext(isolatedConnectionString, schemaName))
            {
                var realtime = new CapturingGameRealtimeBroadcaster();
                var combatExecutor = new QueuedCombatEngineExecutor(
                    BattleOutcome.Victory,
                    BattleOutcome.Victory,
                    BattleOutcome.Victory);
                var service = CreateService(
                    db,
                    realtime,
                    entityService: new DbEntityService(db),
                    combatSetupService: new SimpleCombatSetupService(),
                    combatEngineExecutor: combatExecutor,
                    combatEncounterResultFactory: new PassthroughCombatEncounterResultFactory(),
                    timeProvider: clock,
                    options: options);
                var job = CreateProgressionJob(db, service, options);

                await job.Execute(new TournamentJobExecutionContext(clock.GetUtcNow()));
                clock.SetUtcNow(new DateTimeOffset(2026, 7, 4, 0, 6, 0, TimeSpan.Zero));
                await job.Execute(new TournamentJobExecutionContext(clock.GetUtcNow()));
                clock.SetUtcNow(new DateTimeOffset(2026, 7, 4, 0, 10, 0, TimeSpan.Zero));
                await job.Execute(new TournamentJobExecutionContext(clock.GetUtcNow()));
                clock.SetUtcNow(new DateTimeOffset(2026, 7, 4, 0, 11, 0, TimeSpan.Zero));
                await job.Execute(new TournamentJobExecutionContext(clock.GetUtcNow()));
                clock.SetUtcNow(new DateTimeOffset(2026, 7, 4, 0, 20, 0, TimeSpan.Zero));
                await job.Execute(new TournamentJobExecutionContext(clock.GetUtcNow()));
                clock.SetUtcNow(new DateTimeOffset(2026, 7, 4, 0, 21, 0, TimeSpan.Zero));
                await job.Execute(new TournamentJobExecutionContext(clock.GetUtcNow()));

                var tournament = await db.ArenaTournaments.SingleAsync(t => t.Id == tournamentId);
                Assert.Equal(TournamentStatus.Completed, tournament.Status);
                Assert.Equal(3, combatExecutor.ExecutionCount);
                Assert.Equal(10, await db.TournamentRewardGrants.CountAsync(r => r.TournamentId == tournamentId));
                Assert.Equal(7, await db.BackgroundJobExecutions.CountAsync(e => e.JobName == BackgroundJobNames.TournamentGroundsRollover));
                Assert.Contains(realtime.Events, e => e.Event == "TournamentCompleted");
            }
        }
        finally
        {
            await adminDb.Database.ExecuteSqlRawAsync(dropSchemaSql);
        }
    }

    [Fact]
    public async Task RegisterAsync_serializes_capacity_with_postgres_advisory_lock()
    {
        var connectionString = Environment.GetEnvironmentVariable("LL_TEST_TOURNAMENT_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var schemaName = $"ll_tournament_tests_{Guid.NewGuid():N}";
        await using var adminDb = CreatePostgresDbContext(connectionString);
        var createSchemaSql = $"CREATE SCHEMA \"{schemaName}\"";
        var dropSchemaSql = $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE";
        await adminDb.Database.ExecuteSqlRawAsync(createSchemaSql);

        try
        {
            var isolatedConnectionString = WithSearchPath(connectionString, schemaName);
            await using (var migrationDb = CreatePostgresDbContext(isolatedConnectionString, schemaName))
            {
                await migrationDb.Database.MigrateAsync();
            }

            Guid tournamentId;
            Guid firstCharacterId;
            Guid secondCharacterId;
            await using (var seedDb = CreatePostgresDbContext(isolatedConnectionString, schemaName))
            {
                var tournament = SeedTournament(seedDb, TournamentStatus.RegistrationOpen);
                tournament.MinParticipants = 1;
                tournament.MaxParticipants = 1;
                tournament.Definition.MinParticipants = 1;
                tournament.Definition.MaxParticipants = 1;
                tournamentId = tournament.Id;
                firstCharacterId = SeedCharacter(seedDb, rating: 1500, accountId: Guid.NewGuid()).Id;
                secondCharacterId = SeedCharacter(seedDb, rating: 1490, accountId: Guid.NewGuid()).Id;
                await seedDb.SaveChangesAsync();
            }

            await using var firstDb = CreatePostgresDbContext(isolatedConnectionString, schemaName);
            await using var secondDb = CreatePostgresDbContext(isolatedConnectionString, schemaName);
            var firstEnteredSnapshotCreation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirstRegistration = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var firstService = CreateService(
                firstDb,
                characterSnapshotService: new BlockingCharacterSnapshotService(
                    firstDb,
                    firstEnteredSnapshotCreation,
                    releaseFirstRegistration.Task));
            var secondService = CreateService(secondDb);

            var firstRegistration = firstService.RegisterAsync(firstCharacterId, tournamentId, CancellationToken.None);
            await firstEnteredSnapshotCreation.Task.WaitAsync(TimeSpan.FromSeconds(15));

            var secondRegistration = secondService.RegisterAsync(secondCharacterId, tournamentId, CancellationToken.None);
            var secondCompletedWhileFirstHeldLock = await Task.WhenAny(
                secondRegistration,
                Task.Delay(TimeSpan.FromMilliseconds(500))) == secondRegistration;

            Assert.False(secondCompletedWhileFirstHeldLock);

            releaseFirstRegistration.SetResult();
            var responses = await Task.WhenAll(firstRegistration, secondRegistration);

            await using var verifyDb = CreatePostgresDbContext(isolatedConnectionString, schemaName);
            Assert.Single(responses, response => response is { Registered: true });
            Assert.Equal(1, await verifyDb.TournamentParticipants.CountAsync(p => p.TournamentId == tournamentId));
            Assert.Equal(1, await verifyDb.ArenaTournaments
                .Where(t => t.Id == tournamentId)
                .Select(t => t.RegisteredParticipantCount)
                .SingleAsync());
        }
        finally
        {
            await adminDb.Database.ExecuteSqlRawAsync(dropSchemaSql);
        }
    }

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new LLDbContext(options);
    }

    private static LLDbContext CreatePostgresDbContext(string connectionString, string? migrationsSchema = null)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql(connectionString, postgres =>
            {
                if (!string.IsNullOrWhiteSpace(migrationsSchema))
                {
                    postgres.MigrationsHistoryTable("__EFMigrationsHistory", migrationsSchema);
                }
            })
            .Options;

        return new LLDbContext(options);
    }

    private static string WithSearchPath(string connectionString, string schemaName)
        => $"{connectionString.Trim().TrimEnd(';')};Search Path={schemaName}";

    private static TournamentGroundsService CreateService(
        LLDbContext db,
        CapturingGameRealtimeBroadcaster? realtime = null,
        IEntityService? entityService = null,
        ICombatSetupService? combatSetupService = null,
        ICharacterSnapshotService? characterSnapshotService = null,
        ICombatEngineExecutor? combatEngineExecutor = null,
        ICombatEncounterResultFactory? combatEncounterResultFactory = null,
        ITournamentLockService? tournamentLockService = null,
        TimeProvider? timeProvider = null,
        TournamentGroundsOptions? options = null,
        IGameEventOutbox? outbox = null,
        IInventoryService? inventoryService = null,
        IItemBaseRepository? itemBaseRepository = null,
        IInventoryItemFactory? inventoryItemFactory = null)
    {
        var tournaments = new TournamentGroundsRepository(db);
        var tournamentOptions = options ?? new TournamentGroundsOptions
        {
            Enabled = true,
            AllowWithdrawDuringRegistration = true,
            DefaultMinParticipants = 4,
            DefaultMaxParticipants = 32
        };

        return new TournamentGroundsService(
            tournaments,
            entityService ?? new NoOpEntityService(),
            combatSetupService ?? new NoOpCombatSetupService(),
            characterSnapshotService ?? new DbCharacterSnapshotService(db),
            itemBaseRepository ?? new NoOpItemBaseRepository(),
            inventoryService ?? new RecordingInventoryService(),
            inventoryItemFactory ?? new InventoryItemFactory(),
            combatEngineExecutor ?? new ThrowingCombatEngineExecutor(),
            combatEncounterResultFactory ?? new ThrowingCombatEncounterResultFactory(),
            realtime ?? new CapturingGameRealtimeBroadcaster(),
            tournamentLockService ?? new PostgresTournamentLockService(
                tournaments,
                Options.Create(tournamentOptions)),
            timeProvider ?? new FixedTimeProvider(Now),
            Options.Create(tournamentOptions),
            achievementService: null,
            outbox);
    }

    private static TournamentGroundsProgressionJob CreateProgressionJob(
        LLDbContext db,
        TournamentGroundsService service,
        TournamentGroundsOptions options)
    {
        var executionService = new BackgroundJobExecutionService(
            db,
            Options.Create(new BackgroundJobOptions
            {
                MaxConcurrency = 5,
                RunningExecutionTimeoutMinutes = 30
            }),
            NullLogger<BackgroundJobExecutionService>.Instance);

        return new TournamentGroundsProgressionJob(
            service,
            executionService,
            Options.Create(options),
            NullLogger<TournamentGroundsProgressionJob>.Instance);
    }

    private static TournamentInstance SeedTournament(LLDbContext db, TournamentStatus status)
    {
        var definition = new TournamentDefinition
        {
            Id = Guid.NewGuid(),
            Key = $"daily-open-grounds-{Guid.NewGuid():N}",
            Name = "Daily Open Grounds",
            Description = "Test tournament",
            Format = TournamentFormat.SingleElimination,
            MinParticipants = 4,
            MaxParticipants = 32,
            RegistrationDurationMinutes = 120,
            StartDelayAfterRegistrationMinutes = 10,
            RoundIntervalMinutes = 10,
            MinimumCharacterLevel = 1,
            Enabled = true,
            CreatedAtUtc = Now,
            UpdatedAtUtc = Now
        };

        var tournament = new TournamentInstance
        {
            Id = Guid.NewGuid(),
            DefinitionId = definition.Id,
            Definition = definition,
            TournamentNumber = 1,
            Name = definition.Name,
            Status = status,
            RegistrationStartsAtUtc = Now.AddMinutes(-30),
            RegistrationEndsAtUtc = Now.AddMinutes(30),
            StartsAtUtc = Now.AddMinutes(40),
            MinParticipants = definition.MinParticipants,
            MaxParticipants = definition.MaxParticipants,
            RoundIntervalMinutes = definition.RoundIntervalMinutes,
            CreatedAtUtc = Now,
            UpdatedAtUtc = Now
        };

        db.TournamentDefinitions.Add(definition);
        db.ArenaTournaments.Add(tournament);
        return tournament;
    }

    private static Character SeedCharacter(LLDbContext db, int rating, Guid accountId)
    {
        var character = new Character
        {
            Id = Guid.NewGuid(),
            UserId = accountId,
            Name = $"Hero {Guid.NewGuid():N}",
            Level = 20
        };

        var arenaProfile = new CharacterArenaProfile
        {
            CharacterId = character.Id,
            Character = character,
            Rating = rating,
            LifetimeHighestRating = rating
        };
        character.ArenaProfile = arenaProfile;

        db.Characters.Add(character);
        db.CharacterArenaProfiles.Add(arenaProfile);
        return character;
    }

    private static Character SeedDevelopmentGuestCharacter(LLDbContext db, int index, int rating)
    {
        var user = AppUser.Guest();
        user.Username = $"SeedGuest_Tournament_{index:D2}";
        var character = new Character
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Name = user.Username,
            Level = 20
        };
        var arenaProfile = new CharacterArenaProfile
        {
            CharacterId = character.Id,
            Character = character,
            Rating = rating,
            LifetimeHighestRating = rating
        };
        character.ArenaProfile = arenaProfile;

        db.Users.Add(user);
        db.Characters.Add(character);
        db.CharacterArenaProfiles.Add(arenaProfile);
        return character;
    }

    private static TournamentParticipant SeedParticipant(
        LLDbContext db,
        TournamentInstance tournament,
        int rating,
        int registeredOffsetMinutes)
    {
        var character = SeedCharacter(db, rating, Guid.NewGuid());
        var characterSnapshot = new Domain.Models.Snapshots.CharacterSnapshot
        {
            Id = Guid.NewGuid(),
            CharacterId = character.Id,
            Name = character.Name,
            Level = character.Level
        };
        var combatSnapshot = new TournamentCombatSnapshot
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Tournament = tournament,
            CharacterId = character.Id,
            CharacterSnapshotId = characterSnapshot.Id,
            CharacterSnapshot = characterSnapshot,
            SnapshotVersion = "character-snapshot-v1",
            SnapshotJson = "{}",
            ArenaRatingAtSnapshot = rating,
            RankTierAtSnapshot = ArenaRank.GetTier(rating).Id,
            CreatedAtUtc = Now
        };
        var participant = new TournamentParticipant
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Tournament = tournament,
            CharacterId = character.Id,
            AccountId = character.UserId,
            SnapshotId = combatSnapshot.Id,
            Snapshot = combatSnapshot,
            EntryArenaRating = rating,
            EntryRankTier = ArenaRank.GetTier(rating).Name,
            Status = TournamentParticipantStatus.Registered,
            RegisteredAtUtc = Now.AddMinutes(registeredOffsetMinutes),
            UpdatedAtUtc = Now
        };

        db.CharacterSnapshots.Add(characterSnapshot);
        db.TournamentCombatSnapshots.Add(combatSnapshot);
        db.TournamentParticipants.Add(participant);
        return participant;
    }

    private static TournamentTeam SeedTeam(
        LLDbContext db,
        TournamentInstance tournament,
        string name,
        params TournamentParticipant[] members)
    {
        var team = new TournamentTeam
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Tournament = tournament,
            Name = name,
            OwnerParticipantId = members[0].Id,
            Status = TournamentTeamStatus.Forming,
            MemberCount = members.Length,
            CreatedAtUtc = Now,
            UpdatedAtUtc = Now
        };

        for (var i = 0; i < members.Length; i++)
        {
            members[i].TeamId = team.Id;
            members[i].Team = team;
            members[i].IsTeamOwner = i == 0;
        }

        db.TournamentTeams.Add(team);
        return team;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void SetUtcNow(DateTimeOffset now)
        {
            _now = now;
        }
    }

    private sealed class TournamentJobExecutionContext(DateTimeOffset scheduledFireTime) : IJobExecutionContext
    {
        private readonly Dictionary<object, object> _data = [];

        public IScheduler Scheduler => null!;
        public ITrigger Trigger { get; } = TriggerBuilder.Create()
            .WithIdentity("pvp.tournament-grounds-progression.trigger", BackgroundJobGroups.PvP)
            .Build();
        public ICalendar Calendar => null!;
        public bool Recovering => false;
        public TriggerKey RecoveringTriggerKey => null!;
        public int RefireCount => 0;
        public JobDataMap MergedJobDataMap { get; } = [];
        public IJobDetail JobDetail { get; } = JobBuilder.Create<TournamentGroundsProgressionJob>()
            .WithIdentity(BackgroundJobNames.TournamentGroundsRollover, BackgroundJobGroups.PvP)
            .Build();
        public IJob JobInstance => null!;
        public DateTimeOffset FireTimeUtc => scheduledFireTime;
        public DateTimeOffset? ScheduledFireTimeUtc => scheduledFireTime;
        public DateTimeOffset? PreviousFireTimeUtc => null;
        public DateTimeOffset? NextFireTimeUtc => null;
        public string FireInstanceId => $"test-fire-instance-{scheduledFireTime:yyyyMMddHHmmss}";
        public object? Result { get; set; }
        public TimeSpan JobRunTime => TimeSpan.Zero;
        public CancellationToken CancellationToken => CancellationToken.None;

        public void Put(object key, object objectValue)
        {
            _data[key] = objectValue;
        }

        public object? Get(object key)
        {
            return _data.GetValueOrDefault(key);
        }
    }

    private sealed class CapturingGameRealtimeBroadcaster : IGameRealtimeBroadcaster
    {
        public List<TournamentGroundsUpdated> Events { get; } = [];

        public Task PublishAsync(
            Audience audience,
            GameRealtimeEvent message,
            string sender,
            CancellationToken cancellationToken = default)
        {
            if (message is TournamentGroundsUpdated tournamentEvent)
            {
                Events.Add(tournamentEvent);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class CapturingTournamentLockService : ITournamentLockService
    {
        public int ScheduleLockCalls { get; private set; }
        public int TournamentLockCalls { get; private set; }

        public Task LockTournamentScheduleAsync(CancellationToken cancellationToken)
        {
            ScheduleLockCalls++;
            return Task.CompletedTask;
        }

        public Task LockTournamentAsync(Guid tournamentId, CancellationToken cancellationToken)
        {
            TournamentLockCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class DbCharacterSnapshotService(LLDbContext db) : ICharacterSnapshotService
    {
        public async Task<Domain.Models.Snapshots.CharacterSnapshot> CreateAsync(Guid characterId, CancellationToken ct)
        {
            var character = await db.Characters.FirstAsync(c => c.Id == characterId, ct);
            var snapshot = new Domain.Models.Snapshots.CharacterSnapshot
            {
                Id = Guid.NewGuid(),
                CharacterId = character.Id,
                Name = character.Name,
                Level = character.Level
            };
            await db.CharacterSnapshots.AddAsync(snapshot, ct);
            return snapshot;
        }

        public Task<Domain.Models.Snapshots.CharacterSnapshot?> GetSnapshotByCharacterIdAsync(Guid characterId, CancellationToken ct)
            => Task.FromResult<Domain.Models.Snapshots.CharacterSnapshot?>(null);

        public Task<Domain.Models.Snapshots.CharacterSnapshot?> GetSnapshotByIdAsync(Guid snapshotId, CancellationToken ct)
            => Task.FromResult<Domain.Models.Snapshots.CharacterSnapshot?>(null);
    }

    private sealed class BlockingCharacterSnapshotService(
        LLDbContext db,
        TaskCompletionSource entered,
        Task release) : ICharacterSnapshotService
    {
        public async Task<Domain.Models.Snapshots.CharacterSnapshot> CreateAsync(Guid characterId, CancellationToken ct)
        {
            entered.TrySetResult();
            await release.WaitAsync(ct);
            var character = await db.Characters.FirstAsync(c => c.Id == characterId, ct);
            var snapshot = new Domain.Models.Snapshots.CharacterSnapshot
            {
                Id = Guid.NewGuid(),
                CharacterId = character.Id,
                Name = character.Name,
                Level = character.Level
            };
            await db.CharacterSnapshots.AddAsync(snapshot, ct);
            return snapshot;
        }

        public Task<Domain.Models.Snapshots.CharacterSnapshot?> GetSnapshotByCharacterIdAsync(Guid characterId, CancellationToken ct)
            => Task.FromResult<Domain.Models.Snapshots.CharacterSnapshot?>(null);

        public Task<Domain.Models.Snapshots.CharacterSnapshot?> GetSnapshotByIdAsync(Guid snapshotId, CancellationToken ct)
            => Task.FromResult<Domain.Models.Snapshots.CharacterSnapshot?>(null);
    }

    private sealed class NoOpEntityService : IEntityService
    {
        public Task<List<Entity>> GetEntitiesByIdsForCombatAsync(List<Guid> entityIds, CancellationToken cancellationToken)
            => Task.FromResult<List<Entity>>([]);

        public void UpdateEntities(List<Entity> playerCharacters)
        {
        }
    }

    private sealed class DbEntityService(LLDbContext db) : IEntityService
    {
        public async Task<List<Entity>> GetEntitiesByIdsForCombatAsync(List<Guid> entityIds, CancellationToken cancellationToken)
        {
            return [.. await db.Characters
                .Where(c => entityIds.Contains(c.Id))
                .ToListAsync(cancellationToken)];
        }

        public void UpdateEntities(List<Entity> playerCharacters)
        {
        }
    }

    private sealed class NoOpCombatSetupService : ICombatSetupService
    {
        public List<CombatEntity> CreatePlayerCombatEntities(List<Entity> entities) => [];

        public List<CombatEntity> CreateCreatureCombatEntities(List<Entity> entities, Area area) => [];

        public void AppendPrefixToId(List<CombatEntity> selectedCombatEnemyEntities)
        {
        }

        public Task PrepareEntitiesForCombat(List<CombatEntity> entities) => Task.CompletedTask;

        public List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> combatEntities) => [];
    }

    private sealed class SimpleCombatSetupService : ICombatSetupService
    {
        public List<CombatEntity> CreatePlayerCombatEntities(List<Entity> entities)
            => entities.Select(entity => new CombatEntity(entity)).ToList();

        public List<CombatEntity> CreateCreatureCombatEntities(List<Entity> entities, Area area) => [];

        public void AppendPrefixToId(List<CombatEntity> selectedCombatEnemyEntities)
        {
        }

        public Task PrepareEntitiesForCombat(List<CombatEntity> entities) => Task.CompletedTask;

        public List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> combatEntities)
            => combatEntities.Select(entity => new SimpleCombatEntity(entity.Id, entity.Name, entity.ImagePath, 1, 0)).ToList();
    }

    private sealed class NoOpItemBaseRepository : IItemBaseRepository
    {
        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(IReadOnlyCollection<string> itemIds, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(new Dictionary<string, ItemBase>());

        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());

        public Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(string itemBaseId, CancellationToken cancellationToken)
            => Task.FromResult<EquipmentBase?>(null);

        public Task AddMissingItemBasesAsync(IReadOnlyCollection<ItemBase> itemBases, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeItemBaseRepository(IEnumerable<ItemBase> itemBases) : IItemBaseRepository
    {
        private readonly IReadOnlyDictionary<string, ItemBase> _itemBases = itemBases.ToDictionary(item => item.Id);

        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(
            IReadOnlyCollection<string> itemIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(
                _itemBases
                    .Where(pair => itemIds.Contains(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value));

        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(string itemBaseId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task AddMissingItemBasesAsync(IReadOnlyCollection<ItemBase> itemBases, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class RecordingInventoryService : IInventoryService
    {
        public List<InventoryItem> AddedRewards { get; } = [];

        public Task AddItemsToInventory(
            Guid characterId,
            List<InventoryItem> loot,
            string acquisitionSource,
            CancellationToken cancellationToken)
        {
            AddedRewards.AddRange(loot);
            return Task.CompletedTask;
        }

        public Task<Inventory?> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> TryRemoveCraftingMaterialsAsync(Guid characterId, List<Material> materials, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> TryConsumeInventoryItemAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InventoryItem?> GetInventoryItemAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> MarkItemSeenAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> SetItemFavoriteAsync(Guid characterId, Guid itemInstanceId, bool isFavorite, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> TryRemoveItemsForMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketplaceListing, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> AddItemInstanceBackToInventory(Guid characterId, ItemInstance itemInstance, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddItemToInventoryFromMarketPlace(Guid characterId, InventoryItem inventoryItem, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InventoryItem?> ScrapEquipments(Guid characterId, List<Guid> parsedGuids, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InventoryTransferResult> TransferItemAsync(Guid senderCharacterId, Guid recipientCharacterId, Guid itemInstanceId, int quantity, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class ThrowingCombatEngineExecutor : ICombatEngineExecutor
    {
        public Task<CombatResult> ExecuteAsync(CombatEncounterRuntime runtime, CancellationToken cancellationToken)
            => throw new NotSupportedException("Combat resolution is outside this test scope.");
    }

    private sealed class QueuedCombatEngineExecutor : ICombatEngineExecutor
    {
        private readonly Queue<BattleOutcome> _outcomes;

        public QueuedCombatEngineExecutor(params BattleOutcome[] outcomes)
        {
            _outcomes = new Queue<BattleOutcome>(outcomes);
        }

        public int ExecutionCount { get; private set; }

        public Task<CombatResult> ExecuteAsync(CombatEncounterRuntime runtime, CancellationToken cancellationToken)
        {
            ExecutionCount++;
            var outcome = _outcomes.Count > 0 ? _outcomes.Dequeue() : BattleOutcome.Draw;
            return Task.FromResult(new CombatResult
            {
                Outcome = outcome,
                StartedAt = runtime.Plan.StartsAt,
                Duration = 1,
                EventLog =
                [
                    new CombatLogItem
                    {
                        Timestamp = 1,
                        ActorId = runtime.FriendlyParticipants[0].Combatant.Id,
                        TargetId = runtime.HostileParticipants[0].Combatant.Id,
                        Source = "test.tournament.strike",
                        EventType = EventType.Damage,
                        Magnitude = 1,
                        CombatEntity = new SimpleCombatEntity(
                            runtime.HostileParticipants[0].Combatant.Id,
                            runtime.HostileParticipants[0].Combatant.Name,
                            runtime.HostileParticipants[0].Combatant.ImagePath,
                            1,
                            0)
                    }
                ],
                PlayerTeam = runtime.FriendlyParticipants
                    .Select(p => new SimpleCombatEntity(p.Combatant.Id, p.Combatant.Name, p.Combatant.ImagePath, 1, 0))
                    .ToList(),
                EnemyTeam = runtime.HostileParticipants
                    .Select(p => new SimpleCombatEntity(p.Combatant.Id, p.Combatant.Name, p.Combatant.ImagePath, 1, 0))
                    .ToList()
            });
        }
    }

    private sealed class DrawDamageCombatEngineExecutor(
        int friendlyDamage,
        int hostileDamage) : ICombatEngineExecutor
    {
        public Task<CombatResult> ExecuteAsync(
            CombatEncounterRuntime runtime,
            CancellationToken cancellationToken)
        {
            var friendly = runtime.FriendlyParticipants
                .Select(participant => new SimpleCombatEntity(
                    participant.Combatant.Id,
                    participant.Combatant.Name,
                    participant.Combatant.ImagePath,
                    1,
                    0))
                .ToList();
            var hostile = runtime.HostileParticipants
                .Select(participant => new SimpleCombatEntity(
                    participant.Combatant.Id,
                    participant.Combatant.Name,
                    participant.Combatant.ImagePath,
                    1,
                    0))
                .ToList();
            var entityStats = runtime.FriendlyParticipants
                .Select((participant, index) => new EntityStats(
                    participant.Combatant.Id,
                    participant.Combatant.Name,
                    [],
                    DamageDone: index == 0 ? friendlyDamage : 0,
                    Team: "Friendly"))
                .Concat(runtime.HostileParticipants.Select((participant, index) => new EntityStats(
                    participant.Combatant.Id,
                    participant.Combatant.Name,
                    [],
                    DamageDone: index == 0 ? hostileDamage : 0,
                    Team: "Hostile")))
                .ToList();

            return Task.FromResult(new CombatResult
            {
                Outcome = BattleOutcome.Draw,
                StartedAt = runtime.Plan.StartsAt,
                Duration = 1,
                PlayerTeam = friendly,
                EnemyTeam = hostile,
                EntityStats = entityStats
            });
        }
    }

    private sealed class ThrowingCombatEncounterResultFactory : ICombatEncounterResultFactory
    {
        public CombatEncounterResolutionResult Create(CombatEncounterRuntime runtime, CombatResult combatResult)
            => throw new NotSupportedException("Combat resolution is outside this test scope.");
    }

    private sealed class PassthroughCombatEncounterResultFactory : ICombatEncounterResultFactory
    {
        public CombatEncounterResolutionResult Create(CombatEncounterRuntime runtime, CombatResult combatResult)
            => new(
                runtime.Plan.EncounterId,
                runtime.Plan.Mode,
                runtime.Plan.Sequence,
                runtime.Plan.StartsAt,
                combatResult.Outcome,
                combatResult,
                combatResult.PlayerTeam,
                combatResult.EnemyTeam);
    }

    private sealed class RecordingGameEventOutbox : IGameEventOutbox
    {
        public List<(string EventType, object Payload, Guid? CharacterId)> Events { get; } = [];

        public Task EnqueueAsync<TPayload>(
            string eventType,
            TPayload payload,
            Guid? characterId,
            Guid? accountId,
            CancellationToken cancellationToken)
        {
            Events.Add((eventType, payload!, characterId));
            return Task.CompletedTask;
        }
    }
}
