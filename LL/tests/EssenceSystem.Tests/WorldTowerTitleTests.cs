using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;
using Domain.Models.Achievements;
using Domain.Models.Combat;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.Repositories.Achievements;
using Persistence.LL.Repositories.WorldTower;
using Services.LL.Achievements;
using Services.LL.WorldTower;

namespace EssenceSystem.Tests;

public sealed partial class WorldTowerServiceTests
{
    private static WorldTowerTitleService CreateTitleService(LLDbContext db, IGameEventOutbox outbox) =>
        new(new WorldTowerTitleRepository(db), new AchievementRepository(db),
            new AchievementService(new AchievementRepository(db)), new FixedDefinitionProvider(), outbox,
            Options.Create(new WorldTowerOptions { ServerId = "test-server" }), TimeProvider.System);

    [Theory]
    [InlineData(BattleOutcome.Victory, 4)]
    [InlineData(BattleOutcome.Defeat, 0)]
    public async Task FinalizedCombat_GrantsOnlyWinningRosterAndNeverEquips(BattleOutcome outcome, int expected)
    {
        await using var db = CreateDbContext();
        var characters = Enumerable.Range(1, 4).Select(n => SeedCharacter(db, $"Winner {n}", 20, Guid.NewGuid())).ToArray();
        var spectator = SeedCharacter(db, "Spectator", 20, Guid.NewGuid());
        var accountAlt = SeedCharacter(db, "Account alt", 20, characters[0].UserId);
        await db.SaveChangesAsync();
        var outbox = new TestGameEventOutbox();
        var service = CreateService(db, new FixedPowerRatingService(characters.Select(c => (c.Id, 1000)).ToArray()),
            new FixedGuardianEntityService(), new SimpleCombatSetupService(), new QueuedCombatEngineExecutor(outcome),
            new PassthroughCombatEncounterResultFactory(), outbox);
        var rallyId = await CreateReadyRallyAsync(db, service, characters, TowerRallyMode.FirstClear);
        var start = await service.StartRallyAsync(characters[0].Id, rallyId, CancellationToken.None);
        var playback = await SimulatePlaybackAsync(db, service, start.Value!);
        Assert.Empty(await db.PlayerTitleUnlocks.ToArrayAsync());
        await FinalizePlaybackAsync(db, service, playback);
        db.ChangeTracker.Clear();
        var unlocks = await db.PlayerTitleUnlocks.Include(u => u.TitleDefinition).ToArrayAsync();
        Assert.Equal(expected, unlocks.Length);
        Assert.All(unlocks, u =>
        {
            Assert.Contains(characters, c => c.Id == u.CharacterId && c.UserId == u.AccountId);
            Assert.Equal("title.world_tower.floor_01", u.TitleDefinition.Key);
            Assert.Contains("AttemptId", u.MetadataJson!);
        });
        Assert.DoesNotContain(unlocks, u => u.CharacterId == spectator.Id || u.CharacterId == accountAlt.Id);
        Assert.All(await db.Characters.ToArrayAsync(), c => Assert.Null(c.EquippedTitleDefinitionId));
        Assert.Equal(expected, outbox.ChatAnnouncements.Count(a => a.TargetCharacterId.HasValue));
        Assert.False(await service.FinalizePlaybackAsync(playback.AttemptId, "expired-lease", DateTimeOffset.UtcNow.AddDays(1), CancellationToken.None));
        Assert.Equal(expected, await db.PlayerTitleUnlocks.CountAsync());
    }

    [Fact]
    public async Task TitleGrant_IsIdempotentAndBackfillUsesSuccessfulParticipantHistory()
    {
        await using var db = CreateDbContext();
        var winner = SeedCharacter(db, "Winner", 20, Guid.NewGuid());
        var loser = SeedCharacter(db, "Loser", 20, Guid.NewGuid());
        var applicant = SeedCharacter(db, "Applicant", 20, Guid.NewGuid());
        var outsider = SeedCharacter(db, "Other server", 20, Guid.NewGuid());
        foreach (var (character, status, server) in new[] {
            (winner, TowerAttemptStatus.Succeeded, "test-server"),
            (loser, TowerAttemptStatus.Failed, "test-server"),
            (outsider, TowerAttemptStatus.Succeeded, "another-server") })
        {
            var rally = new TowerRally { ServerId = server, FloorNumber = 1, Status = TowerRallyStatus.Completed };
            rally.Participants.Add(new TowerRallyParticipant { CharacterId = character.Id, AccountId = character.UserId });
            rally.Applications.Add(new TowerRallyApplication { CharacterId = applicant.Id, AccountId = applicant.UserId });
            rally.Attempt = new TowerAttempt { ServerId = server, FloorNumber = 1, Status = status,
                Succeeded = status == TowerAttemptStatus.Succeeded, CompletedAt = DateTimeOffset.UtcNow };
            db.TowerRallies.Add(rally);
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var outbox = new TestGameEventOutbox();
        var titles = CreateTitleService(db, outbox);
        Assert.Equal(1, await titles.BackfillAsync(1, CancellationToken.None));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.Equal(0, await titles.BackfillAsync(1, CancellationToken.None));
        var unlock = Assert.Single(await db.PlayerTitleUnlocks.ToArrayAsync());
        Assert.Equal(winner.Id, unlock.CharacterId);
        Assert.Empty(outbox.ChatAnnouncements);
        var floor = new FixedDefinitionProvider().GetFloor(1)!;
        var recipient = new TowerTitleRecipient(winner.UserId, winner.Id, Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(0, await titles.GrantAsync(floor, [recipient, recipient], true, CancellationToken.None));
        Assert.Empty(outbox.ChatAnnouncements);
    }
}
