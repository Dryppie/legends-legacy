using System.Text.Json;
using System.Globalization;
using System.IO.Compression;
using Application.UseCases.WorldTower.Dtos;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Combat;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.WorldTower;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using AutoMapper;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.Repositories.Snapshots;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Combat.Engine;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Snapshots;
using Services.LL.WorldTower;

namespace EssenceSystem.Tests;

public sealed class WorldTowerServiceTests
{
    [Theory]
    [InlineData(24, 0)]
    [InlineData(25, 1)]
    [InlineData(50, 2)]
    [InlineData(75, 3)]
    [InlineData(100, 4)]
    public async Task Scouting_reveals_guardian_abilities_at_quarters_with_passive_last(
        int scoutingProgress,
        int expectedCount)
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, "Scout", level: 20, accountId: Guid.NewGuid());
        db.TowerFloorProgresses.Add(new TowerFloorProgress
        {
            ServerId = "test-server",
            FloorNumber = 1,
            UnlockedAt = DateTimeOffset.UtcNow,
            ScoutingProgress = scoutingProgress,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db, new FixedPowerRatingService((character.Id, 1_000)));

        var floor = await service.GetFloorAsync(character.Id, 1, CancellationToken.None);

        Assert.NotNull(floor);
        Assert.Equal(expectedCount, floor.Guardian.KnownReveals.Count);
        Assert.Equal(
            [.. new[] { 25, 50, 75, 100 }.Take(expectedCount)],
            floor.Guardian.KnownReveals.Select(reveal => reveal.Threshold));
        string[] expectedTags = expectedCount switch
        {
            0 => [],
            1 => ["Physical", "Melee"],
            2 => ["Physical", "Melee", "Magical", "Area"],
            3 => ["Physical", "Melee", "Magical", "Area", "Debuff"],
            _ => ["Physical", "Melee", "Magical", "Area", "Debuff", "Defensive"]
        };
        Assert.Equal(expectedTags, floor.Guardian.Tags);
        if (expectedCount > 0)
        {
            Assert.Equal(
                ["Physical", "Melee"],
                floor.Guardian.KnownReveals[0].Tags);
            Assert.All(floor.Guardian.KnownReveals.Take(Math.Min(3, expectedCount)), reveal =>
            {
                Assert.Equal(AbilitySpecKind.Active, reveal.Kind);
                Assert.NotNull(reveal.CooldownSeconds);
                Assert.NotEmpty(reveal.Tags);
            });
        }
        if (expectedCount == 4)
        {
            Assert.Equal(AbilitySpecKind.Passive, floor.Guardian.KnownReveals[^1].Kind);
            Assert.Null(floor.Guardian.KnownReveals[^1].CooldownSeconds);
        }
    }

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
        Assert.Equal(25, result.Value.Participants.Single().PowerRating);
        Assert.Equal(100, result.Value.Readiness.RecommendedPowerRating);
        Assert.Contains(
            result.Value.Readiness.Warnings,
            warning => warning.Contains("below", StringComparison.OrdinalIgnoreCase));
        db.ChangeTracker.Clear();
        var floor = await service.GetFloorAsync(character.Id, 1, CancellationToken.None);
        Assert.False(floor!.CanCreateRally);
        Assert.Equal(result.Value.Id, floor.CurrentCharacterRallyId);
        Assert.Contains(floor.Unlocks, unlock =>
            unlock.Key == "test_floor_one_unlock"
            && unlock.Description == "Test floor one feature");

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
        Assert.Contains("Expedition", conflictingRally.Error, StringComparison.Ordinal);
        Assert.False(altApplication.Succeeded);
        Assert.Contains("account already occupies", altApplication.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(accepted.Value!.Participants, x => x.CharacterId == guest.Id);
        Assert.Null(accepted.Value.Participants.Single(x => x.CharacterId == guest.Id).PartySlot);
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
    public async Task UpdateRallyLoadout_ReplacesLockedSnapshotForParticipantAndApplicant()
    {
        await using var db = CreateDbContext();
        var leader = SeedCharacter(db, "Leader", 20, Guid.NewGuid());
        var applicant = SeedCharacter(db, "Applicant", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(
            db,
            new FixedPowerRatingService((leader.Id, 1_100), (applicant.Id, 900)));
        var created = await service.CreateRallyAsync(
            leader.Id,
            1,
            TowerRallyMode.FirstClear,
            CancellationToken.None);
        var rallyId = Assert.IsType<Guid>(created.Value?.Id);
        db.ChangeTracker.Clear();
        var leaderSnapshotId = (await db.TowerRallyParticipants
            .AsNoTracking()
            .SingleAsync(x => x.TowerRallyId == rallyId && x.CharacterId == leader.Id))
            .CharacterSnapshotId;

        var leaderUpdate = await service.UpdateRallyLoadoutAsync(
            leader.Id,
            rallyId,
            CancellationToken.None);

        Assert.True(leaderUpdate.Succeeded, leaderUpdate.Error);
        Assert.True(leaderUpdate.Value!.CanUpdateLoadout);
        db.ChangeTracker.Clear();
        Assert.NotEqual(
            leaderSnapshotId,
            (await db.TowerRallyParticipants
                .AsNoTracking()
                .SingleAsync(x => x.TowerRallyId == rallyId && x.CharacterId == leader.Id))
                .CharacterSnapshotId);

        var application = await service.ApplyToRallyAsync(
            applicant.Id,
            rallyId,
            CancellationToken.None);
        Assert.True(application.Succeeded, application.Error);
        db.ChangeTracker.Clear();
        var applicationId = Assert.Single(application.Value!.Applications).Id;
        var applicationSnapshotId = (await db.TowerRallyApplications
            .AsNoTracking()
            .SingleAsync(x => x.Id == applicationId))
            .CharacterSnapshotId;

        var applicantUpdate = await service.UpdateRallyLoadoutAsync(
            applicant.Id,
            rallyId,
            CancellationToken.None);

        Assert.True(applicantUpdate.Succeeded, applicantUpdate.Error);
        db.ChangeTracker.Clear();
        Assert.NotEqual(
            applicationSnapshotId,
            (await db.TowerRallyApplications
                .AsNoTracking()
                .SingleAsync(x => x.Id == applicationId))
                .CharacterSnapshotId);
    }

    [Fact]
    public async Task UpdateRallyLoadout_RejectsCharactersOutsideTheExpedition()
    {
        await using var db = CreateDbContext();
        var leader = SeedCharacter(db, "Leader", 20, Guid.NewGuid());
        var outsider = SeedCharacter(db, "Outsider", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(
            db,
            new FixedPowerRatingService((leader.Id, 1_100), (outsider.Id, 900)));
        var created = await service.CreateRallyAsync(
            leader.Id,
            1,
            TowerRallyMode.FirstClear,
            CancellationToken.None);
        var rallyId = Assert.IsType<Guid>(created.Value?.Id);
        db.ChangeTracker.Clear();

        var result = await service.UpdateRallyLoadoutAsync(
            outsider.Id,
            rallyId,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task TransferRallyLeadership_MovesLeadershipToAnotherParticipant()
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

        var transferred = await service.TransferRallyLeadershipAsync(
            leader.Id,
            rallyId,
            member.Id,
            CancellationToken.None);

        Assert.True(transferred.Succeeded, transferred.Error);
        Assert.False(transferred.Value!.CanManageApplications);
        Assert.False(transferred.Value.CanTransferLeadership);
        Assert.Equal(member.Id, transferred.Value.CreatedByCharacterId);
        Assert.True(transferred.Value.Participants.Single(x => x.CharacterId == member.Id).IsLeader);
        db.ChangeTracker.Clear();
        Assert.Equal(
            member.Id,
            (await db.TowerRallies.AsNoTracking().SingleAsync(x => x.Id == rallyId)).CreatedByCharacterId);
    }

    [Fact]
    public async Task TransferRallyLeadership_RejectsNonLeadersAndNonParticipants()
    {
        await using var db = CreateDbContext();
        var leader = SeedCharacter(db, "Leader", 20, Guid.NewGuid());
        var member = SeedCharacter(db, "Member", 20, Guid.NewGuid());
        var outsider = SeedCharacter(db, "Outsider", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(
            db,
            new FixedPowerRatingService(
                (leader.Id, 1_100),
                (member.Id, 900),
                (outsider.Id, 800)));
        var created = await service.CreateRallyAsync(
            leader.Id,
            1,
            TowerRallyMode.FirstClear,
            CancellationToken.None);
        var rallyId = Assert.IsType<Guid>(created.Value?.Id);
        db.ChangeTracker.Clear();
        var application = await service.ApplyToRallyAsync(member.Id, rallyId, CancellationToken.None);
        var applicationId = Assert.Single(application.Value!.Applications).Id;
        db.ChangeTracker.Clear();
        Assert.True((await service.AcceptRallyApplicationAsync(
            leader.Id,
            rallyId,
            applicationId,
            CancellationToken.None)).Succeeded);
        db.ChangeTracker.Clear();

        var byMember = await service.TransferRallyLeadershipAsync(
            member.Id,
            rallyId,
            member.Id,
            CancellationToken.None);
        db.ChangeTracker.Clear();
        var toOutsider = await service.TransferRallyLeadershipAsync(
            leader.Id,
            rallyId,
            outsider.Id,
            CancellationToken.None);

        Assert.False(byMember.Succeeded);
        Assert.False(toOutsider.Succeeded);
        db.ChangeTracker.Clear();
        Assert.Equal(
            leader.Id,
            (await db.TowerRallies.AsNoTracking().SingleAsync(x => x.Id == rallyId)).CreatedByCharacterId);
    }

    [Fact]
    public async Task DevelopmentRosterFill_UsesSeededGuestsAndBenchesNewParticipants()
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
        Assert.Equal(TowerRallyStatus.Recruiting, filled.Value!.Status);
        Assert.Equal(4, filled.Value.Participants.Count);
        Assert.False(filled.Value.CanStart);
        Assert.True(filled.Value.DevelopmentToolsEnabled);
        Assert.Equal(1, filled.Value.Participants.Single(x => x.IsLeader).PartySlot);
        Assert.All(filled.Value.Participants.Where(x => !x.IsLeader), participant => Assert.Null(participant.PartySlot));
        Assert.All(
            filled.Value.Participants.Where(x => !x.IsLeader),
            participant => Assert.StartsWith("SeedGuest_Helper_", participant.CharacterName));
        Assert.Equal(4, await db.CharacterSnapshots.CountAsync());
        var helperSnapshots = await db.CharacterSnapshots
            .AsNoTracking()
            .Where(snapshot => helpers.Select(helper => helper.Id).Contains(snapshot.CharacterId))
            .ToArrayAsync();
        Assert.Equal(3, helperSnapshots.Length);
        Assert.All(helperSnapshots, snapshot => Assert.Equal(30, snapshot.Level));
        Assert.All(helpers, helper => Assert.Equal(10, helper.Level));
        Assert.Contains(
            outbox.RallyEvents,
            towerEvent => towerEvent.Event == "DevelopmentRosterFilled");
    }

    [Fact]
    public async Task UpdateRallyParties_RequiresLeaderAndPersistsCompleteSlotLayout()
    {
        await using var db = CreateDbContext();
        var characters = Enumerable.Range(1, 4)
            .Select(number => SeedCharacter(db, $"Member {number}", 20, Guid.NewGuid()))
            .ToArray();
        await db.SaveChangesAsync();
        var service = CreateService(
            db,
            new FixedPowerRatingService(characters.Select(character => (character.Id, 1_000)).ToArray()));
        var created = await service.CreateRallyAsync(
            characters[0].Id,
            1,
            TowerRallyMode.FirstClear,
            CancellationToken.None);
        var rallyId = Assert.IsType<Guid>(created.Value?.Id);
        db.ChangeTracker.Clear();
        foreach (var character in characters.Skip(1))
        {
            var applied = await service.ApplyToRallyAsync(character.Id, rallyId, CancellationToken.None);
            var applicationId = Assert.Single(applied.Value!.Applications).Id;
            db.ChangeTracker.Clear();
            Assert.True((await service.AcceptRallyApplicationAsync(
                characters[0].Id,
                rallyId,
                applicationId,
                CancellationToken.None)).Succeeded);
            db.ChangeTracker.Clear();
        }

        var unauthorized = await service.UpdateRallyPartiesAsync(
            characters[1].Id,
            rallyId,
            characters.Select((character, index) => new TowerPartyAssignment(character.Id, index + 1)).ToArray(),
            CancellationToken.None);
        db.ChangeTracker.Clear();
        var duplicateSlot = await service.UpdateRallyPartiesAsync(
            characters[0].Id,
            rallyId,
            characters.Select(character => new TowerPartyAssignment(character.Id, 1)).ToArray(),
            CancellationToken.None);
        db.ChangeTracker.Clear();
        var outOfRangeSlot = await service.UpdateRallyPartiesAsync(
            characters[0].Id,
            rallyId,
            characters.Select((character, index) => new TowerPartyAssignment(character.Id, index + 2)).ToArray(),
            CancellationToken.None);
        db.ChangeTracker.Clear();
        var incomplete = await service.UpdateRallyPartiesAsync(
            characters[0].Id,
            rallyId,
            characters.Select((character, index) => new TowerPartyAssignment(
                character.Id,
                index == 3 ? null : index + 1)).ToArray(),
            CancellationToken.None);
        db.ChangeTracker.Clear();
        var completed = await service.UpdateRallyPartiesAsync(
            characters[0].Id,
            rallyId,
            characters.Select((character, index) => new TowerPartyAssignment(character.Id, index + 1)).ToArray(),
            CancellationToken.None);

        Assert.False(unauthorized.Succeeded);
        Assert.Contains("leader", unauthorized.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(duplicateSlot.Succeeded);
        Assert.Contains("one participant", duplicateSlot.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(outOfRangeSlot.Succeeded);
        Assert.Contains("between 1 and 4", outOfRangeSlot.Error, StringComparison.OrdinalIgnoreCase);
        Assert.True(incomplete.Succeeded, incomplete.Error);
        Assert.Equal(TowerRallyStatus.Recruiting, incomplete.Value!.Status);
        Assert.False(incomplete.Value.CanStart);
        Assert.True(completed.Succeeded, completed.Error);
        Assert.Equal(TowerRallyStatus.Ready, completed.Value!.Status);
        Assert.True(completed.Value.CanStart);
        Assert.Equal([1, 2, 3, 4], completed.Value.Participants.Select(x => x.PartySlot).Order().ToArray());
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
        Assert.Contains("Expedition", outsiderStart.Error, StringComparison.Ordinal);
        Assert.False(leaderStart.Succeeded);
        Assert.Contains("fill every slot", leaderStart.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.TowerAttempts.ToListAsync());
    }

    [Fact]
    public async Task StartRally_RejectsAnotherAttemptWhileTheFloorIsActive()
    {
        await using var db = CreateDbContext();
        var characters = Enumerable.Range(1, 8)
            .Select(number => SeedCharacter(db, $"Concurrent {number}", 30, Guid.NewGuid()))
            .ToArray();
        await db.SaveChangesAsync();
        var service = CreateCombatService(db, characters, BattleOutcome.Defeat, BattleOutcome.Victory);
        var firstRallyId = await CreateReadyRallyAsync(db, service, characters[..4], TowerRallyMode.FirstClear);
        var secondRallyId = await CreateReadyRallyAsync(db, service, characters[4..], TowerRallyMode.FirstClear);

        var firstStart = await service.StartRallyAsync(
            characters[0].Id,
            firstRallyId,
            CancellationToken.None);
        Assert.True(firstStart.Succeeded, firstStart.Error);
        var playback = await SimulatePlaybackAsync(db, service, firstStart.Value!);
        db.ChangeTracker.Clear();
        var secondStart = await service.StartRallyAsync(
            characters[4].Id,
            secondRallyId,
            CancellationToken.None);

        Assert.False(secondStart.Succeeded);
        Assert.Contains("already attempting", secondStart.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Expedition", secondStart.Error, StringComparison.Ordinal);
        Assert.Single(await db.TowerAttempts.ToListAsync());
        Assert.Equal(
            TowerRallyStatus.Ready,
            (await db.TowerRallies.SingleAsync(x => x.Id == secondRallyId)).Status);

        await FinalizePlaybackAsync(db, service, playback);
        db.ChangeTracker.Clear();
        var startAfterCompletion = await service.StartRallyAsync(
            characters[4].Id,
            secondRallyId,
            CancellationToken.None);

        Assert.True(startAfterCompletion.Succeeded, startAfterCompletion.Error);
        Assert.Equal(2, await db.TowerAttempts.CountAsync());
    }

    [Fact]
    public async Task CreateRally_RejectsEchoModeForASovereignFloor()
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, "Echo Sovereign", 50, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(db, new FixedPowerRatingService((character.Id, 1_000)));
        await service.GetOverviewAsync(character.Id, CancellationToken.None);
        var now = DateTimeOffset.UtcNow;
        foreach (var progress in await db.TowerFloorProgresses.Where(x => x.FloorNumber <= 5).ToListAsync())
            progress.RecordFirstClear(Guid.NewGuid(), now);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.CreateRallyAsync(
            character.Id,
            5,
            TowerRallyMode.Echo,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Sovereign", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.TowerRallies.ToListAsync());
    }

    [Fact]
    public async Task Contributions_KeepThreeClickScoutingAndPreparationCapsSeparate()
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, "Researcher", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(db, new FixedPowerRatingService((character.Id, 1_000)));
        await service.GetOverviewAsync(character.Id, CancellationToken.None);

        TowerOperationResult<Application.UseCases.WorldTower.Dtos.TowerFloorDetailDto>? research = null;
        for (var click = 0; click < 3; click++)
        {
            research = await service.ContributeAsync(
                character.Id,
                1,
                TowerContributionKind.Research,
                1,
                CancellationToken.None);
            Assert.True(research.Succeeded, research.Error);
        }
        var researchOverCap = await service.ContributeAsync(
            character.Id,
            1,
            TowerContributionKind.Research,
            1,
            CancellationToken.None);

        Assert.Equal(3, research?.Value?.ScoutingProgress);
        Assert.Equal(3, research?.Value?.WeeklyResearchContribution);
        Assert.Equal(3, research?.Value?.WeeklyResearchCap);
        Assert.False(researchOverCap.Succeeded);
        Assert.Contains("weekly limit", researchOverCap.Error, StringComparison.OrdinalIgnoreCase);

        foreach (var kind in new[]
                 {
                     TowerContributionKind.SupplyWeapons,
                     TowerContributionKind.InscribeWards,
                     TowerContributionKind.ScoutWeakPoints
                 })
        {
            var result = await service.ContributeAsync(
                character.Id,
                1,
                kind,
                1,
                CancellationToken.None);
            Assert.True(result.Succeeded, result.Error);
        }
        var preparation = await service.GetFloorAsync(character.Id, 1, CancellationToken.None);
        var preparationOverCap = await service.ContributeAsync(
            character.Id,
            1,
            TowerContributionKind.ScoutWeakPoints,
            1,
            CancellationToken.None);

        Assert.NotNull(preparation);
        Assert.Equal(0.25m, preparation.Preparation.SupplyWeaponsPercent);
        Assert.Equal(0.25m, preparation.Preparation.InscribeWardsPercent);
        Assert.Equal(0.25m, preparation.Preparation.ScoutWeakPointsPercent);
        Assert.Equal(3, preparation.Preparation.WeeklyCharacterContribution);
        Assert.Equal(3, preparation.Preparation.WeeklyCharacterCap);
        Assert.Equal(10m, preparation.Preparation.MaximumEffectPercent);
        Assert.False(preparationOverCap.Succeeded);
        Assert.Contains("weekly limit", preparationOverCap.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Scouting_IsImmediateOnLockedFloors_AndUsesOneTowerWideWeeklyLimit()
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, "Forward Scout", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(db, new FixedPowerRatingService((character.Id, 1_000)));
        await service.GetOverviewAsync(character.Id, CancellationToken.None);

        var lockedFloor = await service.ContributeAsync(
            character.Id,
            2,
            TowerContributionKind.Research,
            1,
            CancellationToken.None);

        Assert.True(lockedFloor.Succeeded, lockedFloor.Error);
        Assert.Equal(TowerFloorStateType.Locked, lockedFloor.Value?.State);
        Assert.Equal(1, lockedFloor.Value?.ScoutingProgress);
        Assert.Equal(1, lockedFloor.Value?.WeeklyResearchContribution);

        for (var click = 0; click < 2; click++)
        {
            var result = await service.ContributeAsync(
                character.Id,
                1,
                TowerContributionKind.Research,
                1,
                CancellationToken.None);
            Assert.True(result.Succeeded, result.Error);
        }

        var overCap = await service.ContributeAsync(
            character.Id,
            2,
            TowerContributionKind.Research,
            1,
            CancellationToken.None);
        var preparationOnLockedFloor = await service.ContributeAsync(
            character.Id,
            2,
            TowerContributionKind.SupplyWeapons,
            1,
            CancellationToken.None);

        Assert.False(overCap.Succeeded);
        Assert.Contains("weekly limit", overCap.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(preparationOnLockedFloor.Succeeded);
        Assert.Contains("unlocked", preparationOnLockedFloor.Error, StringComparison.OrdinalIgnoreCase);
        var refreshedLockedFloor = await service.GetFloorAsync(character.Id, 2, CancellationToken.None);
        Assert.Equal(3, refreshedLockedFloor?.WeeklyResearchContribution);
        Assert.Equal(1, refreshedLockedFloor?.ScoutingProgress);
    }

    [Fact]
    public async Task Contributions_RequireExactlyOneActionPerRequest()
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, "Single Action", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(db, new FixedPowerRatingService((character.Id, 1_000)));
        await service.GetOverviewAsync(character.Id, CancellationToken.None);

        var result = await service.ContributeAsync(
            character.Id,
            1,
            TowerContributionKind.Research,
            2,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("one action", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.TowerContributions.ToListAsync());
        Assert.Equal(0, (await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 1)).ScoutingProgress);
    }

    [Fact]
    public async Task PreparationContribution_ReachesTenPercentCapAndRejectsFurtherActions()
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
            Amount = 39,
            WeekKey = weekKey,
            CreatedAt = now
        };
        db.TowerContributions.Add(sharedContribution);
        await db.SaveChangesAsync();

        var reachesCap = await service.ContributeAsync(
            character.Id,
            1,
            TowerContributionKind.SupplyWeapons,
            1,
            CancellationToken.None);

        Assert.True(reachesCap.Succeeded, reachesCap.Error);
        Assert.Equal(10m, reachesCap.Value?.Preparation.SupplyWeaponsPercent);
        var alreadyMaxed = await service.ContributeAsync(
            character.Id,
            1,
            TowerContributionKind.SupplyWeapons,
            1,
            CancellationToken.None);

        Assert.False(alreadyMaxed.Succeeded);
        Assert.Contains("already maxed", alreadyMaxed.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await db.TowerContributions.Where(x => x.CharacterId == character.Id).ToListAsync());
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
    public async Task EchoRally_RequiresFloorOneClear()
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
        Assert.Contains("Floor 1", locked.Error, StringComparison.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;
        var floorOne = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 1);
        floorOne.RecordFirstClear(Guid.NewGuid(), now);
        await db.SaveChangesAsync();

        var available = await service.CreateRallyAsync(
            character.Id,
            1,
            TowerRallyMode.Echo,
            CancellationToken.None);

        Assert.True(available.Succeeded, available.Error);
        Assert.Equal(TowerRallyMode.Echo, available.Value?.Mode);
        Assert.Contains(
            await db.ServerUnlocks.ToArrayAsync(),
            unlock => unlock.UnlockKey == "tower_echo_mode_unlock"
                      && unlock.SourceFloorNumber == 1);
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
        Assert.Equal(125, participant.PowerRating);
    }

    [Fact]
    public async Task PersonalExpeditions_OnlyReturnsAttemptsForCurrentCharacter()
    {
        await using var db = CreateDbContext();
        var current = SeedCharacter(db, "Journal Keeper", 20, Guid.NewGuid());
        var other = SeedCharacter(db, "Other Climber", 20, Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(
            db,
            new FixedPowerRatingService((current.Id, 1_250), (other.Id, 1_100)));
        var ownCreated = await service.CreateRallyAsync(
            current.Id,
            1,
            TowerRallyMode.FirstClear,
            CancellationToken.None);
        var otherCreated = await service.CreateRallyAsync(
            other.Id,
            1,
            TowerRallyMode.FirstClear,
            CancellationToken.None);
        var ownRally = await db.TowerRallies
            .Include(x => x.Participants)
            .SingleAsync(x => x.Id == ownCreated.Value!.Id);
        var otherRally = await db.TowerRallies
            .Include(x => x.Participants)
            .SingleAsync(x => x.Id == otherCreated.Value!.Id);
        var completedAt = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        var ownAttempt = new TowerAttempt
        {
            TowerRally = ownRally,
            TowerRallyId = ownRally.Id,
            ServerId = "test-server",
            FloorNumber = 1,
            Mode = TowerRallyMode.FirstClear,
            Status = TowerAttemptStatus.Failed,
            AttemptNumberForFloor = 4,
            StartedAt = completedAt.AddSeconds(-37),
            CompletedAt = completedAt,
            FightDurationSeconds = 37,
            FailureReason = "The Guardian endured."
        };
        var otherAttempt = new TowerAttempt
        {
            TowerRally = otherRally,
            TowerRallyId = otherRally.Id,
            ServerId = "test-server",
            FloorNumber = 1,
            Mode = TowerRallyMode.FirstClear,
            Status = TowerAttemptStatus.Succeeded,
            Succeeded = true,
            AttemptNumberForFloor = 5,
            StartedAt = completedAt.AddMinutes(1),
            CompletedAt = completedAt.AddMinutes(2),
            FightDurationSeconds = 60
        };
        ownRally.Status = TowerRallyStatus.Completed;
        ownRally.Attempt = ownAttempt;
        otherRally.Status = TowerRallyStatus.Completed;
        otherRally.Attempt = otherAttempt;
        db.TowerAttempts.AddRange(ownAttempt, otherAttempt);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var history = await service.GetPersonalExpeditionsAsync(
            current.Id,
            CancellationToken.None);

        var entry = Assert.Single(history);
        Assert.Equal(ownRally.Id, entry.RallyId);
        Assert.Equal(ownAttempt.Id, entry.AttemptId);
        Assert.Equal("The Waking Step", entry.FloorName);
        Assert.Equal("Lumo Sentinel", entry.GuardianName);
        Assert.Equal(TowerAttemptStatus.Failed, entry.Status);
        Assert.Equal(4, entry.AttemptNumber);
        Assert.Equal(37, entry.FightDurationSeconds);
        Assert.Equal(current.Id, Assert.Single(entry.Participants).CharacterId);
    }

    [Fact]
    public async Task Overview_IgnoresStaleFirstClearReferencesToUnreleasedFloors()
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, "Catalog Boundary", 20, Guid.NewGuid());
        var staleAttemptId = Guid.NewGuid();
        var staleRally = new TowerRally
        {
            ServerId = "test-server",
            FloorNumber = 10,
            Mode = TowerRallyMode.FirstClear,
            Status = TowerRallyStatus.Completed,
            RequiredSlots = 1,
            CreatedByCharacterId = character.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.TowerFloorProgresses.Add(new TowerFloorProgress
        {
            ServerId = "test-server",
            FloorNumber = 10,
            FirstClearAttemptId = staleAttemptId,
            IsCleared = true,
            UnlockedAt = DateTimeOffset.UtcNow,
            ClearedAt = DateTimeOffset.UtcNow,
            ScoutingProgress = 100,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        db.TowerAttempts.Add(new TowerAttempt
        {
            Id = staleAttemptId,
            TowerRally = staleRally,
            ServerId = "test-server",
            FloorNumber = 10,
            Mode = TowerRallyMode.FirstClear,
            Status = TowerAttemptStatus.Succeeded,
            AttemptNumberForFloor = 1,
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            Succeeded = true,
            FightDurationSeconds = 10
        });
        await db.SaveChangesAsync();
        var service = CreateService(db, new FixedPowerRatingService((character.Id, 1_000)));

        var overview = await service.GetOverviewAsync(character.Id, CancellationToken.None);
        var directRally = await service.GetRallyAsync(
            character.Id,
            staleRally.Id,
            CancellationToken.None);

        Assert.Equal(5, overview.Floors.Count);
        Assert.DoesNotContain(overview.RecentClears, entry => entry.FloorNumber == 10);
        Assert.Null(directRally);
    }

    [Fact]
    public void PersistenceModel_ContainsConcurrencySensitiveUniqueIndexes()
    {
        using var db = CreateDbContext();

        AssertUniqueIndex<TowerFloorProgress>(db, nameof(TowerFloorProgress.ServerId), nameof(TowerFloorProgress.FloorNumber));
        AssertUniqueIndex<TowerFloorProgress>(db, nameof(TowerFloorProgress.FirstClearAttemptId));
        AssertUniqueIndex<TowerRallyParticipant>(db, nameof(TowerRallyParticipant.TowerRallyId), nameof(TowerRallyParticipant.CharacterId));
        AssertUniqueIndex<TowerRallyParticipant>(db, nameof(TowerRallyParticipant.TowerRallyId), nameof(TowerRallyParticipant.AccountId));
        AssertIndex<TowerRallyParticipant>(db, nameof(TowerRallyParticipant.TowerRallyId), nameof(TowerRallyParticipant.PartySlot));
        AssertUniqueIndex<TowerRallyApplication>(db, nameof(TowerRallyApplication.TowerRallyId), nameof(TowerRallyApplication.CharacterId));
        AssertUniqueIndex<TowerRallyApplication>(db, nameof(TowerRallyApplication.TowerRallyId), nameof(TowerRallyApplication.AccountId));
        AssertUniqueIndex<TowerAttempt>(db, nameof(TowerAttempt.TowerRallyId));
        AssertUniqueIndex<TowerEchoClear>(
            db,
            nameof(TowerEchoClear.ServerId),
            nameof(TowerEchoClear.CharacterId),
            nameof(TowerEchoClear.WeekKey));
        AssertUniqueIndex<ServerUnlock>(db, nameof(ServerUnlock.ServerId), nameof(ServerUnlock.UnlockKey));
    }

    [Fact]
    public async Task WorkerClaims_ExcludeActiveLeases_AndRecoverExpiredWork()
    {
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var rally = new TowerRally
        {
            ServerId = "test-server",
            FloorNumber = 1,
            Mode = TowerRallyMode.FirstClear,
            Status = TowerRallyStatus.InProgress,
            RequiredSlots = 4,
            CreatedByCharacterId = Guid.NewGuid(),
            CreatedAt = now
        };
        var attempt = new TowerAttempt
        {
            TowerRally = rally,
            ServerId = "test-server",
            FloorNumber = 1,
            Mode = TowerRallyMode.FirstClear,
            Status = TowerAttemptStatus.Started,
            AttemptNumberForFloor = 1,
            StartedAt = now
        };
        rally.Attempt = attempt;
        db.TowerRallies.Add(rally);
        await db.SaveChangesAsync();

        var firstClaim = await db.ClaimWorldTowerSimulationsAsync(
            "worker-one", now, now.AddSeconds(30), 1);
        var competingClaim = await db.ClaimWorldTowerSimulationsAsync(
            "worker-two", now.AddSeconds(1), now.AddSeconds(31), 1);
        var recoveredClaim = await db.ClaimWorldTowerSimulationsAsync(
            "worker-two", now.AddSeconds(31), now.AddSeconds(61), 1);

        Assert.Equal(attempt.Id, Assert.Single(firstClaim));
        Assert.Empty(competingClaim);
        Assert.Equal(attempt.Id, Assert.Single(recoveredClaim));
        Assert.Equal("worker-two", attempt.SimulationLeaseOwner);
        Assert.Equal(2, attempt.SimulationAttempts);
    }

    [Fact]
    public async Task SimulationLeaseRenewal_PreventsAnotherWorkerFromReclaimingActiveCombat()
    {
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var rally = new TowerRally
        {
            ServerId = "test-server",
            FloorNumber = 1,
            Mode = TowerRallyMode.FirstClear,
            Status = TowerRallyStatus.InProgress,
            RequiredSlots = 4,
            CreatedByCharacterId = Guid.NewGuid(),
            CreatedAt = now
        };
        var attempt = new TowerAttempt
        {
            TowerRally = rally,
            ServerId = "test-server",
            FloorNumber = 1,
            Mode = TowerRallyMode.FirstClear,
            Status = TowerAttemptStatus.Started,
            AttemptNumberForFloor = 1,
            StartedAt = now
        };
        rally.Attempt = attempt;
        db.TowerRallies.Add(rally);
        await db.SaveChangesAsync();
        await db.ClaimWorldTowerSimulationsAsync(
            "worker-one", now, now.AddSeconds(30), 1);

        var renewed = await db.RenewWorldTowerSimulationLeaseAsync(
            attempt.Id,
            "worker-one",
            now.AddSeconds(50));
        var competingClaim = await db.ClaimWorldTowerSimulationsAsync(
            "worker-two", now.AddSeconds(31), now.AddSeconds(61), 1);

        Assert.True(renewed);
        Assert.Empty(competingClaim);
        Assert.Equal(now.AddSeconds(50), attempt.SimulationLeaseUntil);
    }

    [Fact]
    public async Task PlaybackClaims_OnlyReturnFramesWhoseNextDispatchIsDue()
    {
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var rally = new TowerRally
        {
            ServerId = "test-server",
            FloorNumber = 1,
            Mode = TowerRallyMode.FirstClear,
            Status = TowerRallyStatus.InProgress,
            RequiredSlots = 4,
            CreatedByCharacterId = Guid.NewGuid(),
            CreatedAt = now
        };
        var attempt = new TowerAttempt
        {
            TowerRally = rally,
            ServerId = "test-server",
            FloorNumber = 1,
            Mode = TowerRallyMode.FirstClear,
            Status = TowerAttemptStatus.Playback,
            AttemptNumberForFloor = 1,
            StartedAt = now
        };
        rally.Attempt = attempt;
        attempt.Playback = new TowerCombatPlayback
        {
            TowerAttempt = attempt,
            PlaybackStartedAt = now,
            PlaybackEndsAt = now.AddMinutes(1),
            NextFrameDueAt = now.AddSeconds(10),
            FrameCount = 2,
            TimelineJson = "[]",
            SimulationCompletedAt = now
        };
        db.TowerRallies.Add(rally);
        await db.SaveChangesAsync();

        var earlyClaim = await db.ClaimWorldTowerPlaybackDispatchesAsync(
            "worker-one", now.AddSeconds(5), now.AddSeconds(35), 1);
        var dueClaim = await db.ClaimWorldTowerPlaybackDispatchesAsync(
            "worker-one", now.AddSeconds(10), now.AddSeconds(40), 1);

        Assert.Empty(earlyClaim);
        Assert.Equal(attempt.Id, Assert.Single(dueClaim));
    }

    [Fact]
    public async Task StartRally_AnnouncesTheBattleInChat_WithALinkToTheExpedition()
    {
        await using var db = CreateDbContext();
        var characters = Enumerable.Range(1, 4)
            .Select(number => SeedCharacter(db, $"Ascendant {number}", 20, Guid.NewGuid()))
            .ToArray();
        await db.SaveChangesAsync();
        var outbox = new TestGameEventOutbox();
        var service = CreateService(
            db,
            new FixedPowerRatingService(characters.Select(x => (x.Id, 1_000)).ToArray()),
            new FixedGuardianEntityService(),
            new SimpleCombatSetupService(),
            new QueuedCombatEngineExecutor(BattleOutcome.Victory),
            new PassthroughCombatEncounterResultFactory(),
            outbox);
        var rallyId = await CreateReadyRallyAsync(db, service, characters, TowerRallyMode.FirstClear);

        var result = await service.StartRallyAsync(characters[0].Id, rallyId, CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        var announcement = Assert.Single(outbox.ChatAnnouncements);
        Assert.Equal(rallyId, announcement.RallyId);
        Assert.Equal($"/game/world/tower/expeditions/{rallyId}", announcement.TargetUrl);
        Assert.Contains("is starting!", announcement.Body, StringComparison.Ordinal);
        Assert.Contains(
            GameEventTypes.WorldTowerChatAnnouncement,
            outbox.EventTypes);
    }

    [Fact]
    public async Task SuccessfulFirstClear_AnnouncesTheConqueredFloorToEveryone()
    {
        await using var db = CreateDbContext();
        var characters = Enumerable.Range(1, 4)
            .Select(number => SeedCharacter(db, $"Conqueror {number}", 20, Guid.NewGuid()))
            .ToArray();
        await db.SaveChangesAsync();
        var outbox = new TestGameEventOutbox();
        var service = CreateService(
            db,
            new FixedPowerRatingService(characters.Select(x => (x.Id, 1_000)).ToArray()),
            new FixedGuardianEntityService(),
            new SimpleCombatSetupService(),
            new QueuedCombatEngineExecutor(BattleOutcome.Victory),
            new PassthroughCombatEncounterResultFactory(),
            outbox);
        var rallyId = await CreateReadyRallyAsync(db, service, characters, TowerRallyMode.FirstClear);
        var result = await service.StartRallyAsync(characters[0].Id, rallyId, CancellationToken.None);
        Assert.True(result.Succeeded, result.Error);
        Assert.NotNull(result.Value);
        var playback = await SimulatePlaybackAsync(db, service, result.Value);

        await FinalizePlaybackAsync(db, service, playback);
        db.ChangeTracker.Clear();

        Assert.True((await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 1)).IsCleared);
        var conquest = Assert.Single(
            outbox.ChatAnnouncements,
            announcement => announcement.Body.Contains("conquered", StringComparison.Ordinal));
        Assert.Equal(rallyId, conquest.RallyId);
        Assert.Equal("/game/world/tower/hall-of-fame", conquest.TargetUrl);
        Assert.Contains("Lumo Sentinel", conquest.Body, StringComparison.Ordinal);
        Assert.Contains("Floor 1", conquest.Body, StringComparison.Ordinal);
        // The rally-start and conquest messages share a rally, so their deterministic ids
        // must still differ or LL-Chat would swallow the second one as a duplicate.
        Assert.Equal(2, outbox.ChatAnnouncements.Count);
        Assert.Equal(2, outbox.ChatAnnouncements.Select(x => x.MessageId).Distinct().Count());
    }

    [Fact]
    public async Task EchoClear_DoesNotReannounceAnAlreadyConqueredFloor()
    {
        await using var db = CreateDbContext();
        var characters = Enumerable.Range(1, 4)
            .Select(number => SeedCharacter(db, $"Echo Conqueror {number}", 20, Guid.NewGuid()))
            .ToArray();
        await db.SaveChangesAsync();
        var outbox = new TestGameEventOutbox();
        var service = CreateService(
            db,
            new FixedPowerRatingService(characters.Select(x => (x.Id, 1_000)).ToArray()),
            new FixedGuardianEntityService(),
            new SimpleCombatSetupService(),
            new QueuedCombatEngineExecutor(BattleOutcome.Victory),
            new PassthroughCombatEncounterResultFactory(),
            outbox);
        await service.GetOverviewAsync(characters[0].Id, CancellationToken.None);
        db.ChangeTracker.Clear();
        var floorOne = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 1);
        floorOne.RecordFirstClear(Guid.NewGuid(), DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var rallyId = await CreateReadyRallyAsync(db, service, characters, TowerRallyMode.Echo);
        var result = await service.StartRallyAsync(characters[0].Id, rallyId, CancellationToken.None);
        Assert.True(result.Succeeded, result.Error);
        Assert.NotNull(result.Value);
        var playback = await SimulatePlaybackAsync(db, service, result.Value);

        await FinalizePlaybackAsync(db, service, playback);
        db.ChangeTracker.Clear();

        Assert.Equal(TowerAttemptStatus.Succeeded, (await db.TowerAttempts.SingleAsync()).Status);
        Assert.DoesNotContain(
            outbox.ChatAnnouncements,
            announcement => announcement.Body.Contains("conquered", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FailedFirstClear_DoesNotAnnounceAConquest()
    {
        await using var db = CreateDbContext();
        var characters = Enumerable.Range(1, 4)
            .Select(number => SeedCharacter(db, $"Challenger {number}", 20, Guid.NewGuid()))
            .ToArray();
        await db.SaveChangesAsync();
        var outbox = new TestGameEventOutbox();
        var service = CreateService(
            db,
            new FixedPowerRatingService(characters.Select(x => (x.Id, 1_000)).ToArray()),
            new FixedGuardianEntityService(),
            new SimpleCombatSetupService(),
            new QueuedCombatEngineExecutor(BattleOutcome.Defeat),
            new PassthroughCombatEncounterResultFactory(),
            outbox);
        var rallyId = await CreateReadyRallyAsync(db, service, characters, TowerRallyMode.FirstClear);
        var result = await service.StartRallyAsync(characters[0].Id, rallyId, CancellationToken.None);
        Assert.True(result.Succeeded, result.Error);
        Assert.NotNull(result.Value);
        var playback = await SimulatePlaybackAsync(db, service, result.Value);

        await FinalizePlaybackAsync(db, service, playback);
        db.ChangeTracker.Clear();

        Assert.False((await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 1)).IsCleared);
        Assert.DoesNotContain(
            outbox.ChatAnnouncements,
            announcement => announcement.Body.Contains("conquered", StringComparison.Ordinal));
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
        Assert.Equal(TowerAttemptStatus.Started, result.Value.Status);
        Assert.Null(result.Value.Playback);
        var playback = await SimulatePlaybackAsync(db, service, result.Value);
        Assert.False(playback.IsCompleted);
        Assert.Equal(TowerCombatPlayback.CompactBundleSchemaVersion, playback.SchemaVersion);
        Assert.Null(playback.CurrentFrame);
        Assert.NotNull(playback.BundleETag);
        var bundleContent = await service.GetAttemptPlaybackBundleAsync(
            characters[0].Id,
            result.Value.AttemptId,
            CancellationToken.None);
        Assert.NotNull(bundleContent);
        Assert.Equal("br", bundleContent.ContentEncoding);
        Assert.Equal(playback.BundleETag, bundleContent.ETag);
        await using var compressed = new MemoryStream(bundleContent.Bytes);
        await using var decompressed = new BrotliStream(compressed, CompressionMode.Decompress);
        var bundle = await JsonSerializer.DeserializeAsync<TowerPlaybackBundleDto>(
            decompressed,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(bundle);
        Assert.NotEmpty(bundle.Frames);
        Assert.True(bundle.Frames[^1].IsFinal);
        Assert.All(bundle.Frames, frame => Assert.NotNull(frame.EntityStates));
        var damagingAbilityTotals = bundle.Frames
            .SelectMany(frame => frame.AbilityTotals)
            .Where(ability => ability.TotalDamage > 0)
            .ToArray();
        Assert.All(damagingAbilityTotals, ability =>
        {
            Assert.NotNull(ability.DamageByType);
            Assert.Equal(
                ability.TotalDamage,
                ability.DamageByType.Sum(entry => entry.TotalDamage));
        });
        var spectatorId = Guid.NewGuid();
        Assert.NotNull(await service.GetAttemptPlaybackBundleAsync(
            spectatorId,
            result.Value.AttemptId,
            CancellationToken.None));
        Assert.NotNull(await service.GetAttemptPlaybackAsync(
            spectatorId,
            result.Value.AttemptId,
            CancellationToken.None));
        Assert.NotNull(await service.GetAttemptPlaybackFramesAsync(
            spectatorId,
            result.Value.AttemptId,
            -1,
            CancellationToken.None));
        var spectatorRally = await service.GetRallyAsync(
            spectatorId,
            rallyId,
            CancellationToken.None);
        Assert.NotNull(spectatorRally);
        Assert.NotNull(spectatorRally.Attempt);
        Assert.NotNull(spectatorRally.Attempt.Playback);
        Assert.False(spectatorRally.Attempt.CanViewCombatResult);
        Assert.Empty(spectatorRally.Applications);
        Assert.All(await db.Characters.ToArrayAsync(), character => Assert.Equal(0, character.TowerTokens));
        Assert.Null(await service.GetAttemptCombatResultAsync(
            characters[0].Id,
            result.Value.AttemptId,
            CancellationToken.None));
        Assert.Null(await service.GetAttemptReportAsync(
            characters[0].Id,
            result.Value.AttemptId,
            CancellationToken.None));
        await FinalizePlaybackAsync(db, service, playback);
        db.ChangeTracker.Clear();

        var progress = await db.TowerFloorProgresses.OrderBy(x => x.FloorNumber).ToArrayAsync();
        Assert.True(progress.Single(x => x.FloorNumber == 1).IsCleared);
        Assert.Equal(100, progress.Single(x => x.FloorNumber == 1).ScoutingProgress);
        Assert.NotNull(progress.Single(x => x.FloorNumber == 2).UnlockedAt);
        Assert.Contains(
            await db.ServerUnlocks.ToArrayAsync(),
            unlock => unlock.UnlockKey == "test_floor_one_unlock");
        Assert.All(await db.Characters.ToArrayAsync(), character =>
        {
            Assert.Equal(400, character.TowerTokens);
            Assert.Equal(0, character.Cinders);
        });
        var clearedFloor = await service.GetFloorAsync(
            characters[0].Id,
            1,
            CancellationToken.None);
        Assert.NotNull(clearedFloor);
        Assert.Equal(10m, clearedFloor.Preparation.SupplyWeaponsPercent);
        Assert.Equal(10m, clearedFloor.Preparation.InscribeWardsPercent);
        Assert.Equal(10m, clearedFloor.Preparation.ScoutWeakPointsPercent);
        Assert.Equal(0, clearedFloor.Preparation.WeeklyCharacterContribution);

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
        Assert.Null(await service.GetAttemptPlaybackBundleAsync(
            spectatorId,
            attempt.Id,
            CancellationToken.None));
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
            var playback = await SimulatePlaybackAsync(db, service, result.Value);
            await FinalizePlaybackAsync(db, service, playback);
            db.ChangeTracker.Clear();
        }

        var floorOne = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 1);
        var floorTwo = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 2);
        Assert.False(floorOne.IsCleared);
        Assert.Equal(30, floorOne.ScoutingProgress);
        Assert.Null(floorTwo.UnlockedAt);
        Assert.Empty(await db.ServerUnlocks.ToListAsync());
        Assert.All(await db.Characters.ToArrayAsync(), character => Assert.Equal(0, character.TowerTokens));
        Assert.Equal(4, await db.TowerAttempts.CountAsync(x => x.Status == TowerAttemptStatus.Failed));
    }

    [Fact]
    public async Task EchoVictory_GrantsEachCharacterOnlyOneRewardAcrossTowerPerWeek()
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
        var floorTwo = await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 2);
        floorOne.RecordFirstClear(Guid.NewGuid(), now);
        floorTwo.RecordFirstClear(Guid.NewGuid(), now);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        foreach (var floorNumber in new[] { 1, 2 })
        {
            var rallyId = await CreateReadyRallyAsync(
                db,
                service,
                characters,
                TowerRallyMode.Echo,
                floorNumber);
            var result = await service.StartRallyAsync(characters[0].Id, rallyId, CancellationToken.None);
            Assert.True(result.Succeeded, result.Error);
            Assert.NotNull(result.Value);
            var playback = await SimulatePlaybackAsync(db, service, result.Value);
            await FinalizePlaybackAsync(db, service, playback);
            db.ChangeTracker.Clear();
        }

        Assert.All(await db.Characters.ToArrayAsync(), character => Assert.Equal(100, character.TowerTokens));
        Assert.Equal(4, await db.TowerEchoClears.CountAsync());
        Assert.Equal(2, await db.TowerAttempts.CountAsync(x => x.Status == TowerAttemptStatus.Succeeded));
        var floorOneDetails = await service.GetFloorAsync(characters[0].Id, 1, CancellationToken.None);
        var floorTwoDetails = await service.GetFloorAsync(characters[0].Id, 2, CancellationToken.None);
        Assert.NotNull(floorOneDetails);
        Assert.NotNull(floorTwoDetails);
        Assert.True(floorOneDetails.EchoRewardClaimedThisWeek);
        Assert.True(floorTwoDetails.EchoRewardClaimedThisWeek);
        Assert.Equal(100, floorOneDetails.EchoTowerTokens);
        Assert.Equal(400, floorOneDetails.FirstClearTowerTokens);
        Assert.Equal(104, floorTwoDetails.EchoTowerTokens);
        Assert.Equal(416, floorTwoDetails.FirstClearTowerTokens);
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
        floorOne.RecordFirstClear(Guid.NewGuid(), now);
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
        var playback = await SimulatePlaybackAsync(db, service, result.Value);
        await FinalizePlaybackAsync(db, service, playback);
        db.ChangeTracker.Clear();
        var balances = await db.Characters.ToDictionaryAsync(x => x.Id, x => x.TowerTokens);
        Assert.Equal(0, balances[characters[0].Id]);
        Assert.Equal(0, balances[characters[1].Id]);
        Assert.Equal(100, balances[characters[2].Id]);
        Assert.Equal(100, balances[characters[3].Id]);
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

        for (var click = 0; click < 3; click++)
        {
            Assert.True((await service.ContributeAsync(
                characters[0].Id,
                1,
                TowerContributionKind.SupplyWeapons,
                1,
                CancellationToken.None)).Succeeded);
        }
        db.ChangeTracker.Clear();
        for (var click = 0; click < 3; click++)
        {
            Assert.True((await service.ContributeAsync(
                characters[1].Id,
                1,
                TowerContributionKind.InscribeWards,
                1,
                CancellationToken.None)).Succeeded);
        }
        db.ChangeTracker.Clear();
        for (var click = 0; click < 3; click++)
        {
            Assert.True((await service.ContributeAsync(
                characters[2].Id,
                1,
                TowerContributionKind.ScoutWeakPoints,
                1,
                CancellationToken.None)).Succeeded);
        }
        db.ChangeTracker.Clear();

        var rallyId = await CreateReadyRallyAsync(db, service, characters, TowerRallyMode.FirstClear);
        var result = await service.StartRallyAsync(characters[0].Id, rallyId, CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.NotNull(result.Value);
        await SimulatePlaybackAsync(db, service, result.Value);
        Assert.NotNull(combat.Runtime);
        Assert.All(combat.Runtime.FriendlyParticipants, participant =>
        {
            var modifiers = participant.Combatant.TemporaryModifiers
                .OfType<InstanceAttributeModifier>()
                .ToArray();
            Assert.Contains(modifiers, modifier =>
                modifier.AttributeType == AttributeType.Power && modifier.Amount == 0.75f);
            Assert.Contains(modifiers, modifier =>
                modifier.AttributeType == AttributeType.ArmorPenetration && modifier.Amount == 0.75f);
            Assert.Contains(modifiers, modifier =>
                modifier.AttributeType == AttributeType.MagicPenetration && modifier.Amount == 0.75f);
        });
        var guardianModifiers = combat.Runtime.HostileParticipants.Single().Combatant.TemporaryModifiers
            .OfType<InstanceAttributeModifier>()
            .ToArray();
        Assert.Contains(guardianModifiers, modifier =>
            modifier.AttributeType == AttributeType.Power && modifier.Amount == -0.75f);
    }

    [Fact]
    public async Task EchoAttempt_ReceivesMaximumPreparationAfterFirstClear()
    {
        await using var db = CreateDbContext();
        var characters = Enumerable.Range(1, 4)
            .Select(number => SeedCharacter(db, $"Echo Prepared {number}", 20, Guid.NewGuid()))
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
        var now = DateTimeOffset.UtcNow;
        (await db.TowerFloorProgresses.SingleAsync(x => x.FloorNumber == 1))
            .RecordFirstClear(Guid.NewGuid(), now);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var rallyId = await CreateReadyRallyAsync(db, service, characters, TowerRallyMode.Echo);
        var result = await service.StartRallyAsync(
            characters[0].Id,
            rallyId,
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.NotNull(result.Value);
        await SimulatePlaybackAsync(db, service, result.Value);
        Assert.NotNull(combat.Runtime);
        Assert.All(combat.Runtime.FriendlyParticipants, participant =>
        {
            var modifiers = participant.Combatant.TemporaryModifiers
                .OfType<InstanceAttributeModifier>()
                .ToArray();
            Assert.Contains(modifiers, modifier =>
                modifier.AttributeType == AttributeType.Power && modifier.Amount == 10f);
            Assert.Contains(modifiers, modifier =>
                modifier.AttributeType == AttributeType.ArmorPenetration && modifier.Amount == 10f);
            Assert.Contains(modifiers, modifier =>
                modifier.AttributeType == AttributeType.MagicPenetration && modifier.Amount == 10f);
        });
        Assert.Contains(
            combat.Runtime.HostileParticipants.Single().Combatant.TemporaryModifiers
                .OfType<InstanceAttributeModifier>(),
            modifier => modifier.AttributeType == AttributeType.Power && modifier.Amount == -10f);
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

        Assert.True(result.Succeeded, result.Error);
        Assert.NotNull(result.Value);
        Assert.False(await SimulatePlaybackAttemptAsync(db, service, result.Value));
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
        bool developmentToolsEnabled = false,
        IWorldTowerDevelopmentRosterFactory? developmentRosters = null)
    {
        var snapshotService = new CharacterSnapshotService(new CharacterSnapshotRepository(db));
        var resolvedCombatSetup = combatSetup ?? new ThrowingCombatSetupService();
        return new WorldTowerService(
            db,
            new FixedDefinitionProvider(),
            snapshotService,
            powerRatings,
            entities ?? new ThrowingEntityService(),
            resolvedCombatSetup,
            combatEngine ?? new ThrowingCombatEngineExecutor(),
            new SnapshotCombatantBuilder(db, resolvedCombatSetup),
            developmentRosters ?? new FixedDevelopmentRosterFactory(),
            new FixedCreatureAbilityDefinitionProvider(),
            new FixedAbilityCatalogProvider(),
            resultFactory ?? new ThrowingCombatEncounterResultFactory(),
            outbox ?? new TestGameEventOutbox(),
            new TestRealtimeBroadcaster(),
            new MapperConfiguration(
                configuration => configuration.AddProfile<MappingProfile>(),
                NullLoggerFactory.Instance).CreateMapper(),
            Options.Create(new WorldTowerOptions
            {
                ServerId = "test-server",
                FailedAttemptScoutingGain = 10,
                FailedAttemptScoutingWeeklyCap = 3,
                ManualScoutingWeeklyCapPerCharacter = 3,
                PreparationWeeklyCapPerCharacter = 3,
                PreparationPercentPerPoint = 0.25m,
                PreparationMaxEffectPercent = 10m,
                DevelopmentToolsEnabled = developmentToolsEnabled
            }),
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            new MemoryCache(new MemoryCacheOptions()),
            TimeProvider.System,
            NullLogger<WorldTowerService>.Instance);
    }

    private static async Task<Application.UseCases.WorldTower.Dtos.TowerCombatPlaybackDto> SimulatePlaybackAsync(
        LLDbContext db,
        WorldTowerService service,
        Application.UseCases.WorldTower.Dtos.TowerAttemptResultDto result)
    {
        Assert.True(await SimulatePlaybackAttemptAsync(db, service, result));
        db.ChangeTracker.Clear();
        var playback = await db.TowerCombatPlaybacks
            .AsNoTracking()
            .SingleAsync(x => x.TowerAttemptId == result.AttemptId);
        var rallyId = await db.TowerAttempts
            .Where(x => x.Id == result.AttemptId)
            .Select(x => x.TowerRallyId)
            .SingleAsync();
        return (await service.GetAttemptPlaybackAsync(
            (await db.TowerRallyParticipants
                .Where(x => x.TowerRallyId == rallyId)
                .Select(x => x.CharacterId)
                .FirstAsync()),
            result.AttemptId,
            CancellationToken.None))!;
    }

    private static async Task<bool> SimulatePlaybackAttemptAsync(
        LLDbContext db,
        WorldTowerService service,
        Application.UseCases.WorldTower.Dtos.TowerAttemptResultDto result)
    {
        const string owner = "test-simulation-worker";
        var attempt = await db.TowerAttempts.SingleAsync(x => x.Id == result.AttemptId);
        attempt.SimulationLeaseOwner = owner;
        attempt.SimulationLeaseUntil = DateTimeOffset.UtcNow.AddMinutes(1);
        attempt.SimulationAttempts++;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return await service.SimulateQueuedAttemptAsync(
            result.AttemptId,
            owner,
            CancellationToken.None);
    }

    private static async Task FinalizePlaybackAsync(
        LLDbContext db,
        WorldTowerService service,
        Application.UseCases.WorldTower.Dtos.TowerCombatPlaybackDto playback)
    {
        const string owner = "test-playback-worker";
        var entity = await db.TowerCombatPlaybacks.SingleAsync(
            x => x.TowerAttemptId == playback.AttemptId);
        entity.DispatchLeaseOwner = owner;
        entity.DispatchLeaseUntil = playback.PlaybackEndsAt.AddMinutes(1);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.True(await service.PublishDuePlaybackFrameAsync(
            playback.AttemptId,
            owner,
            playback.PlaybackEndsAt.AddSeconds(1),
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
        TowerRallyMode mode,
        int floorNumber = 1)
    {
        var created = await service.CreateRallyAsync(
            characters[0].Id,
            floorNumber,
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

        var assigned = await service.UpdateRallyPartiesAsync(
            characters[0].Id,
            rallyId,
            characters.Select((character, index) => new TowerPartyAssignment(character.Id, index + 1)).ToArray(),
            CancellationToken.None);
        Assert.True(assigned.Succeeded, assigned.Error);
        Assert.Equal(TowerRallyStatus.Ready, assigned.Value!.Status);
        db.ChangeTracker.Clear();

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

    private static void AssertIndex<TEntity>(LLDbContext db, params string[] propertyNames)
        where TEntity : class
    {
        var entityType = db.Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entityType);
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
    }

    private sealed class FixedDefinitionProvider : IWorldTowerDefinitionProvider
    {
        private static readonly IReadOnlyList<TowerFloorDefinition> Floors = Enumerable.Range(1, 5)
            .Select(number => new TowerFloorDefinition
            {
                FloorNumber = number,
                Name = number == 1 ? "The Waking Step" : $"Floor {number}",
                Type = number == 5 ? TowerFloorType.Sovereign : TowerFloorType.Standard,
                GuardianCreatureId = Guid.Parse("bfe575f7-f60a-4e09-9452-654a7c8ad1d7"),
                GuardianName = number == 1 ? "Lumo Sentinel" : $"Guardian {number}",
                GuardianAbilityProfileId = "monster.test_guardian",
                RequiredSlots = 4,
                RecommendedPowerRating = 100,
                GuardianScaling = new TowerGuardianScalingDefinition
                {
                    Health = 2,
                    Offense = 2,
                    Defense = 2,
                    Resistance = 2,
                    Penetration = 2,
                    Regeneration = 2
                },
                BalanceBenchmark = new TowerBalanceBenchmarkDefinition
                {
                    CharacterLevel = 30,
                    EquipmentTier = 1,
                    EquipmentRarity = Domain.Models.Items.Rarity.Uncommon,
                    EssenceCount = 4
                },
                EchoEnabledAfterClear = number != 5,
                TowerTokens = new TowerRewardCurveDefinition().Calculate(number),
                Unlocks = number == 1
                    ?
                    [
                        new TowerUnlockDefinition
                        {
                            Key = "test_floor_one_unlock",
                            Description = "Test floor one feature"
                        },
                        new TowerUnlockDefinition
                        {
                            Key = "tower_echo_mode_unlock",
                            Description = "Echo Mode for cleared non-Sovereign floors"
                        }
                    ]
                    : []
            })
            .ToArray();

        public IReadOnlyList<TowerFloorDefinition> GetFloors() => Floors;

        public TowerFloorDefinition? GetFloor(int floorNumber) =>
            Floors.SingleOrDefault(x => x.FloorNumber == floorNumber);
    }

    private sealed class FixedDevelopmentRosterFactory : IWorldTowerDevelopmentRosterFactory
    {
        public WorldTowerDevelopmentBuild Create(
            Guid characterId,
            string characterName,
            TowerFloorDefinition floor,
            int rosterIndex) =>
            new(
                floor.RecommendedPowerRating,
                new Domain.Models.Snapshots.CharacterSnapshot
                {
                    Id = Guid.NewGuid(),
                    CharacterId = characterId,
                    Name = characterName,
                    Level = floor.BalanceBenchmark.CharacterLevel
                });
    }

    private sealed class FixedCreatureAbilityDefinitionProvider : ICreatureAbilityDefinitionProvider
    {
        public IReadOnlyList<string> GetAbilityIds(string monsterDefinitionId) =>
        [
            "ability.test.guardian.first",
            "ability.test.guardian.second",
            "ability.test.guardian.third",
            "ability.test.guardian.passive"
        ];
    }

    private sealed class FixedAbilityCatalogProvider : IAbilityCatalogProvider
    {
        private static readonly AbilityCatalog Catalog = new(
            [
                CreateAbility("ability.test.guardian.first", "First Strike", AbilitySpecKind.Active, 50, "Physical", "Melee"),
                CreateAbility("ability.test.guardian.second", "Second Strike", AbilitySpecKind.Active, 100, "Magical", "Area"),
                CreateAbility("ability.test.guardian.third", "Third Strike", AbilitySpecKind.Active, 150, "Debuff"),
                CreateAbility("ability.test.guardian.passive", "Final Secret", AbilitySpecKind.Passive, 0, "Defensive")
            ],
            [],
            [],
            new Dictionary<string, string>());

        public AbilityCatalog GetCatalog() => Catalog;

        private static AbilitySpec CreateAbility(
            string id,
            string name,
            AbilitySpecKind kind,
            int cooldownTicks,
            params string[] tags) =>
            new()
            {
                Id = id,
                Name = name,
                Description = $"{name} description.",
                Kind = kind,
                CooldownTicks = cooldownTicks,
                Tags = [.. tags]
            };
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

        public Task<OverallPowerRating> GetCharacterOverallRatingAsync(
            Character character,
            CancellationToken cancellationToken) =>
            GetCharacterOverallRatingAsync(character.Id, cancellationToken);

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
        public List<WorldTowerChatAnnouncementPayload> ChatAnnouncements { get; } = [];

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
            if (payload is WorldTowerChatAnnouncementPayload announcement)
                ChatAnnouncements.Add(announcement);
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
