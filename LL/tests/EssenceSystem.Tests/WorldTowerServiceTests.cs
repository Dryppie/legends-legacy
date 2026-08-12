using System.Text.Json;
using System.Globalization;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.WorldTower;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using AutoMapper;
using Domain.Models.Combat;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;
using Domain.Models.Snapshots;
using Domain.Models.Users;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.Repositories.Snapshots;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Snapshots;
using Services.LL.WorldTower;

namespace EssenceSystem.Tests;

public sealed class WorldTowerServiceTests
{
    [Fact]
    public async Task CreateRally_AllowsRatingBelowRecommendation_AndLocksExistingSnapshot()
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, "Underprepared", level: 7, accountId: Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(db, new FixedPowerRatingService((character.Id, 250)));

        var result = await service.CreateRallyAsync(
            character.Id,
            floorNumber: 1,
            TowerRallyMode.FirstClear,
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal(250, result.Value.Participants.Single().PowerRating);
        Assert.Equal(1_000, result.Value.Readiness.RecommendedPowerRating);
        Assert.Contains(
            result.Value.Readiness.Warnings,
            warning => warning.Contains("below", StringComparison.OrdinalIgnoreCase));
        db.ChangeTracker.Clear();
        var floor = await service.GetFloorAsync(character.Id, 1, CancellationToken.None);
        Assert.False(floor!.CanCreateRally);
        Assert.Equal(result.Value.Id, floor.CurrentCharacterRallyId);

        var snapshotId = await db.TowerRallyParticipants
            .Select(participant => participant.CharacterSnapshotId)
            .SingleAsync();
        character.Name = "Changed Later";
        character.Level = 99;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var snapshot = await db.CharacterSnapshots.SingleAsync(x => x.Id == snapshotId);
        Assert.Equal("Underprepared", snapshot.Name);
        Assert.Equal(7, snapshot.Level);
    }

    [Fact]
    public async Task RallyApplication_AllowsDistinctGuestAccount_AndLeaderAcceptsIt()
    {
        await using var db = CreateDbContext();
        var leaderAccountId = Guid.NewGuid();
        var leader = SeedCharacter(db, "Leader", 20, leaderAccountId);
        var leaderAlt = SeedCharacter(db, "Leader Alt", 20, leaderAccountId);
        var guest = SeedCharacter(db, "Guest", 4, Guid.NewGuid());
        await db.SaveChangesAsync();
        var outbox = new TestGameEventOutbox();
        var service = CreateService(
            db,
            new FixedPowerRatingService(
                (leader.Id, 1_100),
                (leaderAlt.Id, 900),
                (guest.Id, 400)),
            outbox: outbox);
        var created = await service.CreateRallyAsync(
            leader.Id,
            1,
            TowerRallyMode.FirstClear,
            CancellationToken.None);
        var rallyId = Assert.IsType<Guid>(created.Value?.Id);
        db.ChangeTracker.Clear();

        var guestApplication = await service.ApplyToRallyAsync(guest.Id, rallyId, CancellationToken.None);
        var applicationId = Assert.Single(guestApplication.Value!.Applications).Id;
        Assert.DoesNotContain(guestApplication.Value.Participants, x => x.CharacterId == guest.Id);
        Assert.False(guestApplication.Value.CanApply);
        db.ChangeTracker.Clear();
        var leaderView = await service.GetRallyAsync(leader.Id, rallyId, CancellationToken.None);
        Assert.Equal(applicationId, Assert.Single(leaderView!.Applications).Id);
        Assert.True(leaderView.CanManageApplications);
        db.ChangeTracker.Clear();
        var floorView = await service.GetFloorAsync(leader.Id, 1, CancellationToken.None);
        var rallySummary = Assert.Single(floorView!.ActiveRallies);
        Assert.Equal(1, rallySummary.PendingApplicationCount);
        Assert.Equal("Leader", rallySummary.LeaderCharacterName);
        db.ChangeTracker.Clear();
        var accepted = await service.AcceptRallyApplicationAsync(
            leader.Id,
            rallyId,
            applicationId,
            CancellationToken.None);
        db.ChangeTracker.Clear();
        var conflictingRally = await service.CreateRallyAsync(
            guest.Id,
            1,
            TowerRallyMode.FirstClear,
            CancellationToken.None);
        db.ChangeTracker.Clear();
        var altApplication = await service.ApplyToRallyAsync(leaderAlt.Id, rallyId, CancellationToken.None);

        Assert.True(guestApplication.Succeeded, guestApplication.Error);
        Assert.True(accepted.Succeeded, accepted.Error);
        Assert.False(conflictingRally.Succeeded);
        Assert.Contains("already locked", conflictingRally.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(altApplication.Succeeded);
        Assert.Contains("account already occupies", altApplication.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(accepted.Value!.Participants, x => x.CharacterId == guest.Id);
        Assert.Equal(2, await db.TowerRallyParticipants.CountAsync(x => x.TowerRallyId == rallyId));
        Assert.Equal(
            ["Created", "ApplicationSubmitted", "ApplicationAccepted"],
            outbox.RallyEvents.Take(3).Select(x => x.Event));
        Assert.All(outbox.EventTypes, eventType => Assert.Equal(GameEventTypes.WorldTowerRallyUpdated, eventType));
    }

    [Fact]
    public async Task RallyApplication_CanOnlyBeResolvedByLeader_AndApplicantCanWithdraw()
    {
        await using var db = CreateDbContext();
        var leader = SeedCharacter(db, "Leader", 20, Guid.NewGuid());
        var applicant = SeedCharacter(db, "Applicant", 20, Guid.NewGuid());
        var outsider = SeedCharacter(db, "Outsider", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(
            db,
            new FixedPowerRatingService(
                (leader.Id, 1_100),
                (applicant.Id, 1_000),
                (outsider.Id, 1_000)));
        var created = await service.CreateRallyAsync(
            leader.Id,
            1,
            TowerRallyMode.FirstClear,
            CancellationToken.None);
        var rallyId = Assert.IsType<Guid>(created.Value?.Id);
        db.ChangeTracker.Clear();
        var applied = await service.ApplyToRallyAsync(applicant.Id, rallyId, CancellationToken.None);
        var applicationId = Assert.Single(applied.Value!.Applications).Id;
        db.ChangeTracker.Clear();

        var unauthorized = await service.DeclineRallyApplicationAsync(
            outsider.Id,
            rallyId,
            applicationId,
            CancellationToken.None);
        db.ChangeTracker.Clear();
        var withdrawn = await service.LeaveRallyAsync(applicant.Id, rallyId, CancellationToken.None);

        Assert.False(unauthorized.Succeeded);
        Assert.Contains("leader", unauthorized.Error, StringComparison.OrdinalIgnoreCase);
        Assert.True(withdrawn.Succeeded, withdrawn.Error);
        Assert.Empty(withdrawn.Value!.Applications);
        Assert.True(withdrawn.Value.CanApply);
        Assert.False(withdrawn.Value.CanLeave);
        Assert.Equal(
            TowerRallyApplicationStatus.Withdrawn,
            (await db.TowerRallyApplications.SingleAsync(x => x.Id == applicationId)).Status);
        Assert.Single(withdrawn.Value.Participants);
    }

    [Fact]
    public async Task LeaveRally_ReopensForMember_AndCancelsForLeader()
    {
        await using var db = CreateDbContext();
        var leader = SeedCharacter(db, "Leader", 20, Guid.NewGuid());
        var member = SeedCharacter(db, "Member", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(
            db,
            new FixedPowerRatingService((leader.Id, 1_100), (member.Id, 900)));
        var created = await service.CreateRallyAsync(
            leader.Id,
            1,
            TowerRallyMode.FirstClear,
            CancellationToken.None);
        var rallyId = Assert.IsType<Guid>(created.Value?.Id);
        db.ChangeTracker.Clear();
        var application = await service.ApplyToRallyAsync(member.Id, rallyId, CancellationToken.None);
        Assert.True(application.Succeeded, application.Error);
        var applicationId = Assert.Single(application.Value!.Applications).Id;
        db.ChangeTracker.Clear();
        Assert.True((await service.AcceptRallyApplicationAsync(
            leader.Id,
            rallyId,
            applicationId,
            CancellationToken.None)).Succeeded);
        db.ChangeTracker.Clear();

        var memberLeave = await service.LeaveRallyAsync(member.Id, rallyId, CancellationToken.None);

        Assert.True(memberLeave.Succeeded, memberLeave.Error);
        Assert.Equal(TowerRallyStatus.Recruiting, memberLeave.Value?.Status);
        Assert.DoesNotContain(memberLeave.Value!.Participants, x => x.CharacterId == member.Id);
        Assert.Empty(memberLeave.Value.Applications);
        Assert.True(memberLeave.Value.CanApply);
        Assert.False(memberLeave.Value.CanLeave);
        Assert.Equal(
            TowerRallyApplicationStatus.Withdrawn,
            (await db.TowerRallyApplications.SingleAsync(x => x.Id == applicationId)).Status);
        db.ChangeTracker.Clear();

        var reapplied = await service.ApplyToRallyAsync(member.Id, rallyId, CancellationToken.None);

        Assert.True(reapplied.Succeeded, reapplied.Error);
        var reappliedApplication = Assert.Single(reapplied.Value!.Applications);
        Assert.Equal(applicationId, reappliedApplication.Id);
        Assert.Equal(TowerRallyApplicationStatus.Pending, reappliedApplication.Status);
        Assert.True(reapplied.Value.CanLeave);
        Assert.False(reapplied.Value.CanApply);
        db.ChangeTracker.Clear();

        var leaderLeave = await service.LeaveRallyAsync(leader.Id, rallyId, CancellationToken.None);

        Assert.True(leaderLeave.Succeeded, leaderLeave.Error);
        Assert.Equal(TowerRallyStatus.Cancelled, leaderLeave.Value?.Status);
        Assert.NotNull((await db.TowerRallies.SingleAsync(x => x.Id == rallyId)).CancelledAt);
    }

    [Fact]
    public async Task DevelopmentRosterFill_UsesSeededGuestsAndMakesRallyReady()
    {
        await using var db = CreateDbContext();
        var leader = SeedCharacter(db, "Leader", 20, Guid.NewGuid());
        var helpers = Enumerable.Range(1, 3)
            .Select(number => SeedDevelopmentCharacter(db, $"SeedGuest_Helper_{number}", 10))
            .ToArray();
        await db.SaveChangesAsync();
        var outbox = new TestGameEventOutbox();
        var ratings = new[] { leader }
            .Concat(helpers)
            .Select(character => (character.Id, 1_000))
            .ToArray();
        var service = CreateService(
            db,
            new FixedPowerRatingService(ratings),
            outbox: outbox,
            developmentToolsEnabled: true);
        var created = await service.CreateRallyAsync(
            leader.Id,
            1,
            TowerRallyMode.FirstClear,
            CancellationToken.None);
        var rallyId = Assert.IsType<Guid>(created.Value?.Id);
        db.ChangeTracker.Clear();

        var filled = await service.FillRallyWithDevelopmentCharactersAsync(
            leader.Id,
            rallyId,
            CancellationToken.None);

        Assert.True(filled.Succeeded, filled.Error);
        Assert.Equal(TowerRallyStatus.Ready, filled.Value!.Status);
        Assert.Equal(4, filled.Value.Participants.Count);
        Assert.True(filled.Value.CanStart);
        Assert.True(filled.Value.DevelopmentToolsEnabled);
        Assert.All(
            filled.Value.Participants.Where(x => !x.IsLeader),
            participant => Assert.StartsWith("SeedGuest_Helper_", participant.CharacterName));
        Assert.Equal(4, await db.CharacterSnapshots.CountAsync());
        Assert.Contains(
            outbox.RallyEvents,
            towerEvent => towerEvent.Event == "DevelopmentRosterFilled");
    }

    [Fact]
    public async Task StartRally_RequiresLeaderAndEverySlot_BeforeCombatBegins()
    {
        await using var db = CreateDbContext();
        var leader = SeedCharacter(db, "Leader", 20, Guid.NewGuid());
        var outsider = SeedCharacter(db, "Outsider", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(
            db,
            new FixedPowerRatingService((leader.Id, 1_100), (outsider.Id, 1_100)));
        var created = await service.CreateRallyAsync(
            leader.Id,
            1,
            TowerRallyMode.FirstClear,
            CancellationToken.None);
        var rallyId = Assert.IsType<Guid>(created.Value?.Id);
        db.ChangeTracker.Clear();

        var outsiderStart = await service.StartRallyAsync(outsider.Id, rallyId, CancellationToken.None);
        db.ChangeTracker.Clear();
        var leaderStart = await service.StartRallyAsync(leader.Id, rallyId, CancellationToken.None);

        Assert.False(outsiderStart.Succeeded);
        Assert.Contains("leader", outsiderStart.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(leaderStart.Succeeded);
        Assert.Contains("fill every slot", leaderStart.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.TowerAttempts.ToListAsync());
    }

    [Fact]
    public async Task Contributions_KeepResearchAndPreparationCapsSeparate()
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, "Researcher", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(db, new FixedPowerRatingService((character.Id, 1_000)));
        await service.GetOverviewAsync(character.Id, CancellationToken.None);

        Assert.True((await service.ContributeAsync(
            character.Id,
            1,
            TowerContributionKind.Research,
            5,
            CancellationToken.None)).Succeeded);
        var research = await service.ContributeAsync(
            character.Id,
            1,
            TowerContributionKind.Research,
            5,
            CancellationToken.None);
        var researchOverCap = await service.ContributeAsync(
            character.Id,
            1,
            TowerContributionKind.Research,
            1,
            CancellationToken.None);

        Assert.True(research.Succeeded, research.Error);
        Assert.Equal(10, research.Value?.ScoutingProgress);
        Assert.Equal(10, research.Value?.WeeklyResearchContribution);
        Assert.Equal(10, research.Value?.WeeklyResearchCap);
        Assert.False(researchOverCap.Succeeded);
        Assert.Contains("weekly cap", researchOverCap.Error, StringComparison.OrdinalIgnoreCase);

        Assert.True((await service.ContributeAsync(
            character.Id,
            1,
            TowerContributionKind.SupplyWeapons,
            5,
            CancellationToken.None)).Succeeded);
        var preparation = await service.ContributeAsync(
            character.Id,
            1,
            TowerContributionKind.InscribeWards,
            5,
            CancellationToken.None);
        var preparationOverCap = await service.ContributeAsync(
            character.Id,
            1,
            TowerContributionKind.ScoutWeakPoints,
            1,
            CancellationToken.None);

        Assert.True(preparation.Succeeded, preparation.Error);
        Assert.Equal(1.25m, preparation.Value?.Preparation.SupplyWeaponsPercent);
        Assert.Equal(1.25m, preparation.Value?.Preparation.InscribeWardsPercent);
        Assert.Equal(10, preparation.Value?.Preparation.WeeklyCharacterContribution);
        Assert.False(preparationOverCap.Succeeded);
        Assert.Contains("weekly cap", preparationOverCap.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreparationContribution_RejectsAnyPointsThatCannotIncreaseTheBonus()
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, "Contributor", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(db, new FixedPowerRatingService((character.Id, 1_000)));
        await service.GetOverviewAsync(character.Id, CancellationToken.None);
        var now = DateTimeOffset.UtcNow;
        var weekKey = ISOWeek.GetYear(now.UtcDateTime) * 100 + ISOWeek.GetWeekOfYear(now.UtcDateTime);
        var sharedContribution = new TowerContribution
        {
            ServerId = "test-server",
            FloorNumber = 1,
            CharacterId = Guid.NewGuid(),
            Kind = TowerContributionKind.SupplyWeapons,
            Amount = 19,
            WeekKey = weekKey,
            CreatedAt = now
        };
        db.TowerContributions.Add(sharedContribution);
        await db.SaveChangesAsync();

        var wouldOverflow = await service.ContributeAsync(
            character.Id,
            1,
            TowerContributionKind.SupplyWeapons,
            2,
            CancellationToken.None);

        Assert.False(wouldOverflow.Succeeded);
        Assert.Contains("Only 1 more", wouldOverflow.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(await db.TowerContributions.AnyAsync(x => x.CharacterId == character.Id));

        sharedContribution.Amount = 20;
        await db.SaveChangesAsync();
        var alreadyMaxed = await service.ContributeAsync(
            character.Id,
            1,
            TowerContributionKind.SupplyWeapons,
            1,
            CancellationToken.None);

        Assert.False(alreadyMaxed.Succeeded);
        Assert.Contains("already maxed", alreadyMaxed.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(await db.TowerContributions.AnyAsync(x => x.CharacterId == character.Id));
    }

    [Fact]
    public async Task ResearchContribution_DoesNotConsumeAllowanceWhenScoutingIsComplete()
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, "Scout", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(db, new FixedPowerRatingService((character.Id, 1_000)));
        await service.GetOverviewAsync(character.Id, CancellationToken.None);
        var progress = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 1);
        progress.AddScoutingProgress(100, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();

        var result = await service.ContributeAsync(
            character.Id,
            1,
            TowerContributionKind.Research,
            1,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("already complete", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.TowerContributions.ToListAsync());
    }

    [Fact]
    public async Task EchoRally_RequiresFloorFiveAndTargetFloorClears()
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, "Echoer", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(db, new FixedPowerRatingService((character.Id, 1_000)));
        await service.GetOverviewAsync(character.Id, CancellationToken.None);

        var locked = await service.CreateRallyAsync(
            character.Id,
            1,
            TowerRallyMode.Echo,
            CancellationToken.None);
        Assert.False(locked.Succeeded);
        Assert.Contains("Floor 5", locked.Error, StringComparison.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;
        var floorOne = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 1);
        var floorFive = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 5);
        floorOne.RecordFirstClear(Guid.NewGuid(), now);
        floorFive.RecordFirstClear(Guid.NewGuid(), now);
        await db.SaveChangesAsync();

        var available = await service.CreateRallyAsync(
            character.Id,
            1,
            TowerRallyMode.Echo,
            CancellationToken.None);

        Assert.True(available.Succeeded, available.Error);
        Assert.Equal(TowerRallyMode.Echo, available.Value?.Mode);
    }

    [Fact]
    public async Task HallOfFame_IsProjectedFromImmutableFirstClearAttemptAndRoster()
    {
        await using var db = CreateDbContext();
        var leader = SeedCharacter(db, "First Clearer", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(db, new FixedPowerRatingService((leader.Id, 1_250)));
        var created = await service.CreateRallyAsync(
            leader.Id,
            1,
            TowerRallyMode.FirstClear,
            CancellationToken.None);
        var rallyId = Assert.IsType<Guid>(created.Value?.Id);
        var rally = await db.TowerRallies.Include(x => x.Participants).SingleAsync(x => x.Id == rallyId);
        var completedAt = new DateTimeOffset(2026, 8, 11, 20, 15, 0, TimeSpan.Zero);
        var attempt = new TowerAttempt
        {
            Id = Guid.NewGuid(),
            TowerRallyId = rally.Id,
            TowerRally = rally,
            ServerId = "test-server",
            FloorNumber = 1,
            Mode = TowerRallyMode.FirstClear,
            Status = TowerAttemptStatus.Succeeded,
            AttemptNumberForFloor = 3,
            StartedAt = completedAt.AddSeconds(-42),
            CompletedAt = completedAt,
            Succeeded = true,
            FightDurationSeconds = 42
        };
        rally.Status = TowerRallyStatus.Completed;
        rally.Attempt = attempt;
        db.TowerAttempts.Add(attempt);
        var progress = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 1);
        Assert.True(progress.RecordFirstClear(attempt.Id, completedAt));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var hall = await service.GetHallOfFameAsync(CancellationToken.None);

        var entry = Assert.Single(hall);
        Assert.Equal("The Waking Step", entry.FloorName);
        Assert.Equal(3, entry.AttemptNumber);
        Assert.Equal(42, entry.FightDurationSeconds);
        Assert.Equal(completedAt, entry.ClearedAt);
        var participant = Assert.Single(entry.Participants);
        Assert.Equal(leader.Id, participant.CharacterId);
        Assert.Equal("First Clearer", participant.CharacterName);
        Assert.Equal(1_250, participant.PowerRating);
    }

    [Fact]
    public void PersistenceModel_ContainsConcurrencySensitiveUniqueIndexes()
    {
        using var db = CreateDbContext();

        AssertUniqueIndex<TowerFloorProgress>(db, nameof(TowerFloorProgress.ServerId), nameof(TowerFloorProgress.FloorNumber));
        AssertUniqueIndex<TowerFloorProgress>(db, nameof(TowerFloorProgress.FirstClearAttemptId));
        AssertUniqueIndex<TowerRallyParticipant>(db, nameof(TowerRallyParticipant.TowerRallyId), nameof(TowerRallyParticipant.CharacterId));
        AssertUniqueIndex<TowerRallyParticipant>(db, nameof(TowerRallyParticipant.TowerRallyId), nameof(TowerRallyParticipant.AccountId));
        AssertUniqueIndex<TowerRallyApplication>(db, nameof(TowerRallyApplication.TowerRallyId), nameof(TowerRallyApplication.CharacterId));
        AssertUniqueIndex<TowerRallyApplication>(db, nameof(TowerRallyApplication.TowerRallyId), nameof(TowerRallyApplication.AccountId));
        AssertUniqueIndex<TowerAttempt>(db, nameof(TowerAttempt.TowerRallyId));
        AssertUniqueIndex<TowerEchoClear>(
            db,
            nameof(TowerEchoClear.ServerId),
            nameof(TowerEchoClear.FloorNumber),
            nameof(TowerEchoClear.CharacterId),
            nameof(TowerEchoClear.WeekKey));
        AssertUniqueIndex<ServerUnlock>(db, nameof(ServerUnlock.ServerId), nameof(ServerUnlock.UnlockKey));
    }

    [Fact]
    public async Task SuccessfulFirstClear_PersistsReportRewardsProgressionUnlockAndHallRecord()
    {
        await using var db = CreateDbContext();
        var characters = Enumerable.Range(1, 4)
            .Select(number => SeedCharacter(db, $"Ascendant {number}", 20, Guid.NewGuid()))
            .ToArray();
        await db.SaveChangesAsync();
        var service = CreateCombatService(db, characters, BattleOutcome.Victory);
        var rallyId = await CreateReadyRallyAsync(db, service, characters, TowerRallyMode.FirstClear);

        var result = await service.StartRallyAsync(characters[0].Id, rallyId, CancellationToken.None);

        var resolutionError = result.Succeeded
            ? null
            : (await db.TowerAttempts.SingleAsync()).FailureReason;
        Assert.True(result.Succeeded, $"{result.Error} {resolutionError}");
        Assert.NotNull(result.Value);
        Assert.Equal(TowerAttemptStatus.Playback, result.Value.Status);
        Assert.False(result.Value.Playback.IsCompleted);
        Assert.All(await db.Characters.ToArrayAsync(), character => Assert.Equal(0, character.Cinders));
        Assert.Null(await service.GetAttemptCombatResultAsync(
            characters[0].Id,
            result.Value.AttemptId,
            CancellationToken.None));
        Assert.Null(await service.GetAttemptReportAsync(
            characters[0].Id,
            result.Value.AttemptId,
            CancellationToken.None));
        await FinalizePlaybackAsync(service, result.Value);
        db.ChangeTracker.Clear();

        var progress = await db.TowerFloorProgresses.OrderBy(x => x.FloorNumber).ToArrayAsync();
        Assert.True(progress.Single(x => x.FloorNumber == 1).IsCleared);
        Assert.Equal(100, progress.Single(x => x.FloorNumber == 1).ScoutingProgress);
        Assert.NotNull(progress.Single(x => x.FloorNumber == 2).UnlockedAt);
        Assert.Equal("test_floor_one_unlock", (await db.ServerUnlocks.SingleAsync()).UnlockKey);
        Assert.All(await db.Characters.ToArrayAsync(), character => Assert.Equal(100, character.Cinders));

        var attempt = await db.TowerAttempts.SingleAsync();
        Assert.Equal(TowerAttemptStatus.Succeeded, attempt.Status);
        Assert.False(string.IsNullOrWhiteSpace(attempt.BattleReportJson));
        Assert.False(string.IsNullOrWhiteSpace(attempt.CombatResultJson));
        var report = await service.GetAttemptReportAsync(characters[0].Id, attempt.Id, CancellationToken.None);
        Assert.NotNull(report);
        Assert.True(report.Succeeded);
        Assert.Equal(0, report.GuardianHealthRemainingPercent);
        var replay = await service.GetAttemptCombatResultAsync(
            characters[0].Id,
            attempt.Id,
            CancellationToken.None);
        Assert.NotNull(replay);
        Assert.Equal(BattleOutcome.Victory, replay.Outcome);
        Assert.Equal(4, replay.PlayerTeam.Count);
        Assert.Equal(4, replay.EntityStats.Count);
        Assert.Null(await service.GetAttemptCombatResultAsync(
            Guid.NewGuid(),
            attempt.Id,
            CancellationToken.None));
        Assert.Single(await service.GetHallOfFameAsync(CancellationToken.None));
    }

    [Fact]
    public async Task FailedFirstClear_StopsScoutingRewardsAtWeeklyAttemptCap()
    {
        await using var db = CreateDbContext();
        var characters = Enumerable.Range(1, 4)
            .Select(number => SeedCharacter(db, $"Scout {number}", 20, Guid.NewGuid()))
            .ToArray();
        await db.SaveChangesAsync();
        var service = CreateCombatService(
            db,
            characters,
            BattleOutcome.Defeat,
            BattleOutcome.Defeat,
            BattleOutcome.Defeat,
            BattleOutcome.Defeat);

        for (var attemptNumber = 0; attemptNumber < 4; attemptNumber++)
        {
            var rallyId = await CreateReadyRallyAsync(db, service, characters, TowerRallyMode.FirstClear);
            var result = await service.StartRallyAsync(characters[0].Id, rallyId, CancellationToken.None);
            Assert.True(result.Succeeded, result.Error);
            Assert.NotNull(result.Value);
            await FinalizePlaybackAsync(service, result.Value);
            db.ChangeTracker.Clear();
        }

        var floorOne = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 1);
        var floorTwo = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 2);
        Assert.False(floorOne.IsCleared);
        Assert.Equal(30, floorOne.ScoutingProgress);
        Assert.Null(floorTwo.UnlockedAt);
        Assert.Empty(await db.ServerUnlocks.ToListAsync());
        Assert.All(await db.Characters.ToArrayAsync(), character => Assert.Equal(0, character.Cinders));
        Assert.Equal(4, await db.TowerAttempts.CountAsync(x => x.Status == TowerAttemptStatus.Failed));
    }

    [Fact]
    public async Task EchoVictory_GrantsEachCharacterOnlyOneRewardPerFloorAndWeek()
    {
        await using var db = CreateDbContext();
        var characters = Enumerable.Range(1, 4)
            .Select(number => SeedCharacter(db, $"Echo {number}", 20, Guid.NewGuid()))
            .ToArray();
        await db.SaveChangesAsync();
        var service = CreateCombatService(db, characters, BattleOutcome.Victory, BattleOutcome.Victory);
        await service.GetOverviewAsync(characters[0].Id, CancellationToken.None);
        db.ChangeTracker.Clear();
        var now = DateTimeOffset.UtcNow;
        var floorOne = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 1);
        var floorFive = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 5);
        floorOne.RecordFirstClear(Guid.NewGuid(), now);
        floorFive.RecordFirstClear(Guid.NewGuid(), now);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        for (var clearNumber = 0; clearNumber < 2; clearNumber++)
        {
            var rallyId = await CreateReadyRallyAsync(db, service, characters, TowerRallyMode.Echo);
            var result = await service.StartRallyAsync(characters[0].Id, rallyId, CancellationToken.None);
            Assert.True(result.Succeeded, result.Error);
            Assert.NotNull(result.Value);
            await FinalizePlaybackAsync(service, result.Value);
            db.ChangeTracker.Clear();
        }

        Assert.All(await db.Characters.ToArrayAsync(), character => Assert.Equal(25, character.Cinders));
        Assert.Equal(4, await db.TowerEchoClears.CountAsync());
        Assert.Equal(2, await db.TowerAttempts.CountAsync(x => x.Status == TowerAttemptStatus.Succeeded));
    }

    [Fact]
    public async Task EchoVictory_RewardsOnlyEligibleMembersOfMixedRoster()
    {
        await using var db = CreateDbContext();
        var characters = Enumerable.Range(1, 4)
            .Select(number => SeedCharacter(db, $"Mixed Echo {number}", 20, Guid.NewGuid()))
            .ToArray();
        await db.SaveChangesAsync();
        var service = CreateCombatService(db, characters, BattleOutcome.Victory);
        await service.GetOverviewAsync(characters[0].Id, CancellationToken.None);
        db.ChangeTracker.Clear();
        var now = DateTimeOffset.UtcNow;
        var floorOne = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 1);
        var floorFive = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 5);
        floorOne.RecordFirstClear(Guid.NewGuid(), now);
        floorFive.RecordFirstClear(Guid.NewGuid(), now);
        var weekKey = ISOWeek.GetYear(now.UtcDateTime) * 100 + ISOWeek.GetWeekOfYear(now.UtcDateTime);
        db.TowerEchoClears.AddRange(characters.Take(2).Select(character => new TowerEchoClear
        {
            ServerId = "test-server",
            FloorNumber = 1,
            CharacterId = character.Id,
            WeekKey = weekKey,
            ClearedAt = now.AddDays(-1)
        }));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var rallyId = await CreateReadyRallyAsync(db, service, characters, TowerRallyMode.Echo);
        var result = await service.StartRallyAsync(characters[0].Id, rallyId, CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.NotNull(result.Value);
        await FinalizePlaybackAsync(service, result.Value);
        db.ChangeTracker.Clear();
        var balances = await db.Characters.ToDictionaryAsync(x => x.Id, x => x.Cinders);
        Assert.Equal(0, balances[characters[0].Id]);
        Assert.Equal(0, balances[characters[1].Id]);
        Assert.Equal(25, balances[characters[2].Id]);
        Assert.Equal(25, balances[characters[3].Id]);
        Assert.Equal(4, await db.TowerEchoClears.CountAsync());
    }

    [Fact]
    public async Task PreparationContributions_ReachCombatRuntimeAsExpectedModifiers()
    {
        await using var db = CreateDbContext();
        var characters = Enumerable.Range(1, 4)
            .Select(number => SeedCharacter(db, $"Prepared {number}", 20, Guid.NewGuid()))
            .ToArray();
        await db.SaveChangesAsync();
        var combat = new CapturingCombatEngineExecutor(BattleOutcome.Victory);
        var service = CreateService(
            db,
            new FixedPowerRatingService(characters.Select(x => (x.Id, 1_000)).ToArray()),
            new FixedGuardianEntityService(),
            new SimpleCombatSetupService(),
            combat,
            new PassthroughCombatEncounterResultFactory());
        await service.GetOverviewAsync(characters[0].Id, CancellationToken.None);
        db.ChangeTracker.Clear();

        Assert.True((await service.ContributeAsync(
            characters[0].Id,
            1,
            TowerContributionKind.SupplyWeapons,
            5,
            CancellationToken.None)).Succeeded);
        db.ChangeTracker.Clear();
        Assert.True((await service.ContributeAsync(
            characters[1].Id,
            1,
            TowerContributionKind.InscribeWards,
            4,
            CancellationToken.None)).Succeeded);
        db.ChangeTracker.Clear();
        Assert.True((await service.ContributeAsync(
            characters[2].Id,
            1,
            TowerContributionKind.ScoutWeakPoints,
            3,
            CancellationToken.None)).Succeeded);
        db.ChangeTracker.Clear();

        var rallyId = await CreateReadyRallyAsync(db, service, characters, TowerRallyMode.FirstClear);
        var result = await service.StartRallyAsync(characters[0].Id, rallyId, CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.NotNull(combat.Runtime);
        Assert.All(combat.Runtime.FriendlyParticipants, participant =>
        {
            var modifiers = participant.Combatant.TemporaryModifiers
                .OfType<InstanceAttributeModifier>()
                .ToArray();
            Assert.Contains(modifiers, modifier =>
                modifier.AttributeType == AttributeType.Power && modifier.Amount == 1.25f);
            Assert.Contains(modifiers, modifier =>
                modifier.AttributeType == AttributeType.ArmorPenetration && modifier.Amount == 0.75f);
            Assert.Contains(modifiers, modifier =>
                modifier.AttributeType == AttributeType.MagicPenetration && modifier.Amount == 0.75f);
        });
        var guardianModifiers = combat.Runtime.HostileParticipants.Single().Combatant.TemporaryModifiers
            .OfType<InstanceAttributeModifier>()
            .ToArray();
        Assert.Contains(guardianModifiers, modifier =>
            modifier.AttributeType == AttributeType.Power && modifier.Amount == -1f);
    }

    [Fact]
    public async Task CombatException_RecordsErroredAttemptAndCompletesRally()
    {
        await using var db = CreateDbContext();
        var characters = Enumerable.Range(1, 4)
            .Select(number => SeedCharacter(db, $"Error Tester {number}", 20, Guid.NewGuid()))
            .ToArray();
        await db.SaveChangesAsync();
        var ratings = new FixedPowerRatingService(characters.Select(x => (x.Id, 1_000)).ToArray());
        var service = CreateService(
            db,
            ratings,
            new FixedGuardianEntityService(),
            new SimpleCombatSetupService(),
            new ThrowingCombatEngineExecutor(),
            new PassthroughCombatEncounterResultFactory());
        var rallyId = await CreateReadyRallyAsync(db, service, characters, TowerRallyMode.FirstClear);

        var result = await service.StartRallyAsync(characters[0].Id, rallyId, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("could not be resolved", result.Error, StringComparison.OrdinalIgnoreCase);
        db.ChangeTracker.Clear();
        Assert.Equal(TowerAttemptStatus.Errored, (await db.TowerAttempts.SingleAsync()).Status);
        Assert.Equal(TowerRallyStatus.Completed, (await db.TowerRallies.SingleAsync()).Status);
    }

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new LLDbContext(options);
    }

    private static WorldTowerService CreateService(
        LLDbContext db,
        IPowerRatingService powerRatings,
        IEntityService? entities = null,
        ICombatSetupService? combatSetup = null,
        ICombatEngineExecutor? combatEngine = null,
        ICombatEncounterResultFactory? resultFactory = null,
        IGameEventOutbox? outbox = null,
        bool developmentToolsEnabled = false)
    {
        var snapshotService = new CharacterSnapshotService(new CharacterSnapshotRepository(db));
        return new WorldTowerService(
            db,
            new FixedDefinitionProvider(),
            snapshotService,
            powerRatings,
            entities ?? new ThrowingEntityService(),
            combatSetup ?? new ThrowingCombatSetupService(),
            combatEngine ?? new ThrowingCombatEngineExecutor(),
            resultFactory ?? new ThrowingCombatEncounterResultFactory(),
            outbox ?? new TestGameEventOutbox(),
            new TestRealtimeBroadcaster(),
            new MapperConfiguration(
                configuration => configuration.AddProfile<MappingProfile>(),
                NullLoggerFactory.Instance).CreateMapper(),
            Options.Create(new WorldTowerOptions
            {
                ServerId = "test-server",
                EchoModeUnlockFloor = 5,
                FailedAttemptScoutingGain = 10,
                FailedAttemptScoutingWeeklyCap = 3,
                ManualScoutingWeeklyCapPerCharacter = 10,
                PreparationWeeklyCapPerCharacter = 10,
                PreparationPercentPerPoint = 0.25m,
                PreparationMaxEffectPercent = 5m,
                DevelopmentToolsEnabled = developmentToolsEnabled
            }),
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            TimeProvider.System,
            NullLogger<WorldTowerService>.Instance);
    }

    private static async Task FinalizePlaybackAsync(
        WorldTowerService service,
        Application.UseCases.WorldTower.Dtos.TowerAttemptResultDto result)
    {
        Assert.True(await service.PublishDuePlaybackFrameAsync(
            result.AttemptId,
            result.Playback.PlaybackEndsAt.AddSeconds(1),
            CancellationToken.None));
    }

    private static WorldTowerService CreateCombatService(
        LLDbContext db,
        IReadOnlyCollection<Character> characters,
        params BattleOutcome[] outcomes) =>
        CreateService(
            db,
            new FixedPowerRatingService(characters.Select(x => (x.Id, 1_000)).ToArray()),
            new FixedGuardianEntityService(),
            new SimpleCombatSetupService(),
            new QueuedCombatEngineExecutor(outcomes),
            new PassthroughCombatEncounterResultFactory());

    private static async Task<Guid> CreateReadyRallyAsync(
        LLDbContext db,
        WorldTowerService service,
        IReadOnlyList<Character> characters,
        TowerRallyMode mode)
    {
        var created = await service.CreateRallyAsync(
            characters[0].Id,
            1,
            mode,
            CancellationToken.None);
        Assert.True(created.Succeeded, created.Error);
        var rallyId = Assert.IsType<Guid>(created.Value?.Id);
        db.ChangeTracker.Clear();
        foreach (var character in characters.Skip(1))
        {
            var applied = await service.ApplyToRallyAsync(character.Id, rallyId, CancellationToken.None);
            Assert.True(applied.Succeeded, applied.Error);
            var applicationId = Assert.Single(applied.Value!.Applications).Id;
            db.ChangeTracker.Clear();
            var accepted = await service.AcceptRallyApplicationAsync(
                characters[0].Id,
                rallyId,
                applicationId,
                CancellationToken.None);
            Assert.True(accepted.Succeeded, accepted.Error);
            db.ChangeTracker.Clear();
        }

        return rallyId;
    }

    private static Character SeedCharacter(
        LLDbContext db,
        string name,
        int level,
        Guid accountId)
    {
        var character = new Character
        {
            Id = Guid.NewGuid(),
            UserId = accountId,
            Name = name,
            Level = level
        };
        db.Characters.Add(character);
        return character;
    }

    private static Character SeedDevelopmentCharacter(
        LLDbContext db,
        string name,
        int level)
    {
        var user = AppUser.Guest();
        user.Username = name;
        var character = SeedCharacter(db, name, level, user.Id);
        character.User = user;
        db.Users.Add(user);
        return character;
    }

    private static void AssertUniqueIndex<TEntity>(LLDbContext db, params string[] propertyNames)
        where TEntity : class
    {
        var entityType = db.Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entityType);
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.IsUnique
                     && index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
    }

    private sealed class FixedDefinitionProvider : IWorldTowerDefinitionProvider
    {
        private static readonly IReadOnlyList<TowerFloorDefinition> Floors = Enumerable.Range(1, 5)
            .Select(number => new TowerFloorDefinition
            {
                FloorNumber = number,
                Name = number == 1 ? "The Waking Step" : $"Floor {number}",
                Type = TowerFloorType.Standard,
                GuardianCreatureId = Guid.Parse("bfe575f7-f60a-4e09-9452-654a7c8ad1d7"),
                GuardianName = number == 1 ? "Lumo Sentinel" : $"Guardian {number}",
                RequiredSlots = 4,
                RecommendedPowerRating = 1_000,
                GuardianStrengthMultiplier = 2,
                EchoEnabledAfterClear = true,
                FirstClearCinders = 100,
                EchoCinders = 25,
                UnlockKeys = number == 1 ? ["test_floor_one_unlock"] : []
            })
            .ToArray();

        public IReadOnlyList<TowerFloorDefinition> GetFloors() => Floors;

        public TowerFloorDefinition? GetFloor(int floorNumber) =>
            Floors.SingleOrDefault(x => x.FloorNumber == floorNumber);
    }

    private sealed class FixedPowerRatingService(params (Guid CharacterId, int Rating)[] ratings)
        : IPowerRatingService
    {
        private readonly IReadOnlyDictionary<Guid, int> _ratings = ratings.ToDictionary(x => x.CharacterId, x => x.Rating);

        public Task<OverallPowerRating> GetCharacterOverallRatingAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new OverallPowerRating(
                _ratings[characterId],
                PowerAnalysisState.Available));

        public Task<PowerRatingSnapshot> GetCharacterRatingAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PowerRatingSnapshot(
                PowerRatingAlgorithm.Version,
                $"test-{characterId:N}",
                _ratings[characterId],
                0,
                0,
                0,
                0,
                0,
                0,
                DateTimeOffset.UtcNow,
                PowerRatingConfidence.High,
                PowerAnalysisState.Available));

        public Task<PowerRatingSnapshot> GetPartyRatingAsync(
            Guid characterId,
            DungeonPartySelection partySelection,
            CancellationToken cancellationToken) =>
            GetCharacterRatingAsync(characterId, cancellationToken);
    }

    private sealed class ThrowingEntityService : IEntityService
    {
        public Task<List<Entity>> GetEntitiesByIdsForCombatAsync(
            List<Guid> entityIds,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Combat should not run in this test.");

        public void UpdateEntities(List<Entity> playerCharacters) =>
            throw new InvalidOperationException("Combat should not run in this test.");
    }

    private sealed class TestGameEventOutbox : IGameEventOutbox
    {
        public List<string> EventTypes { get; } = [];
        public List<WorldTowerRallyUpdated> RallyEvents { get; } = [];

        public Task EnqueueAsync<TPayload>(
            string eventType,
            TPayload payload,
            Guid? characterId,
            Guid? accountId,
            CancellationToken cancellationToken)
        {
            EventTypes.Add(eventType);
            if (payload is WorldTowerRallyUpdated rallyEvent)
                RallyEvents.Add(rallyEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class TestRealtimeBroadcaster : IGameRealtimeBroadcaster
    {
        public List<WorldTowerCombatFrameUpdated> Frames { get; } = [];

        public Task PublishAsync(
            Audience audience,
            GameRealtimeEvent message,
            string sender,
            CancellationToken cancellationToken = default)
        {
            if (message is WorldTowerCombatFrameUpdated frame)
                Frames.Add(frame);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedGuardianEntityService : IEntityService
    {
        public Task<List<Entity>> GetEntitiesByIdsForCombatAsync(
            List<Guid> entityIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<List<Entity>>(entityIds.Select(id => (Entity)new Creature
            {
                Id = id,
                Name = "Lumo Sentinel",
                Level = 1
            }).ToList());

        public void UpdateEntities(List<Entity> playerCharacters)
        {
        }
    }

    private sealed class ThrowingCombatSetupService : ICombatSetupService
    {
        public List<CombatEntity> CreatePlayerCombatEntities(List<Entity> entities) =>
            throw new InvalidOperationException("Combat should not run in this test.");

        public List<CombatEntity> CreateCreatureCombatEntities(List<Entity> entities, Area area) =>
            throw new InvalidOperationException("Combat should not run in this test.");

        public void AppendPrefixToId(List<CombatEntity> selectedCombatEnemyEntities) =>
            throw new InvalidOperationException("Combat should not run in this test.");

        public Task PrepareEntitiesForCombat(List<CombatEntity> entities) =>
            throw new InvalidOperationException("Combat should not run in this test.");

        public List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> combatEntities) =>
            throw new InvalidOperationException("Combat should not run in this test.");
    }

    private sealed class SimpleCombatSetupService : ICombatSetupService
    {
        public List<CombatEntity> CreatePlayerCombatEntities(List<Entity> entities) =>
            entities.Select(entity => new CombatEntity(entity)).ToList();

        public List<CombatEntity> CreateCreatureCombatEntities(List<Entity> entities, Area area) =>
            entities.Select(entity => new CombatEntity(entity)).ToList();

        public void AppendPrefixToId(List<CombatEntity> selectedCombatEnemyEntities)
        {
        }

        public Task PrepareEntitiesForCombat(List<CombatEntity> entities) => Task.CompletedTask;

        public List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> combatEntities) =>
            combatEntities
                .Select(entity => new SimpleCombatEntity(entity.Id, entity.Name, entity.ImagePath, 100, 0))
                .ToList();
    }

    private sealed class ThrowingCombatEngineExecutor : ICombatEngineExecutor
    {
        public Task<CombatResult> ExecuteAsync(
            CombatEncounterRuntime runtime,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Combat should not run in this test.");
    }

    private sealed class QueuedCombatEngineExecutor(params BattleOutcome[] outcomes) : ICombatEngineExecutor
    {
        private readonly Queue<BattleOutcome> _outcomes = new(outcomes);

        public Task<CombatResult> ExecuteAsync(
            CombatEncounterRuntime runtime,
            CancellationToken cancellationToken)
        {
            var outcome = _outcomes.Dequeue();
            return Task.FromResult(CreateCombatResult(runtime, outcome));
        }
    }

    private sealed class CapturingCombatEngineExecutor(BattleOutcome outcome) : ICombatEngineExecutor
    {
        public CombatEncounterRuntime? Runtime { get; private set; }

        public Task<CombatResult> ExecuteAsync(
            CombatEncounterRuntime runtime,
            CancellationToken cancellationToken)
        {
            Runtime = runtime;
            return Task.FromResult(CreateCombatResult(runtime, outcome));
        }
    }

    private static CombatResult CreateCombatResult(
        CombatEncounterRuntime runtime,
        BattleOutcome outcome)
    {
        var friendlyHealth = outcome == BattleOutcome.Victory ? 100 : 0;
        var guardianHealth = outcome == BattleOutcome.Victory ? 0 : 50;
        return new CombatResult
        {
            Outcome = outcome,
            StartedAt = runtime.Plan.StartsAt,
            Duration = 42,
            PlayerTeam = runtime.FriendlyParticipants
                .Select(participant => new SimpleCombatEntity
                {
                    Id = participant.Combatant.Id,
                    Name = participant.Combatant.Name,
                    MaxHealth = 100,
                    Health = friendlyHealth
                })
                .ToList(),
            EnemyTeam = runtime.HostileParticipants
                .Select(participant => new SimpleCombatEntity
                {
                    Id = participant.Combatant.Id,
                    Name = participant.Combatant.Name,
                    MaxHealth = 100,
                    Health = guardianHealth
                })
                .ToList(),
            EntityStats = runtime.FriendlyParticipants
                .Select(participant => new EntityStats(
                    participant.Combatant.Id,
                    participant.Combatant.Name,
                    [],
                    DamageDone: 100,
                    DamageTaken: outcome == BattleOutcome.Victory ? 10 : 100))
                .ToList()
        };
    }

    private sealed class ThrowingCombatEncounterResultFactory : ICombatEncounterResultFactory
    {
        public CombatEncounterResolutionResult Create(
            CombatEncounterRuntime runtime,
            CombatResult combatResult) =>
            throw new InvalidOperationException("Combat should not run in this test.");
    }

    private sealed class PassthroughCombatEncounterResultFactory : ICombatEncounterResultFactory
    {
        public CombatEncounterResolutionResult Create(
            CombatEncounterRuntime runtime,
            CombatResult combatResult) =>
            new(
                runtime.Plan.EncounterId,
                runtime.Plan.Mode,
                runtime.Plan.Sequence,
                runtime.Plan.StartsAt,
                combatResult.Outcome,
                combatResult,
                combatResult.PlayerTeam,
                combatResult.EnemyTeam);
    }
}
