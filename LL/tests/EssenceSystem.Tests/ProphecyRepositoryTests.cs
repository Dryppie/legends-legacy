using Domain.Models.Prophecies;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Prophecies;

namespace EssenceSystem.Tests;

public sealed class ProphecyRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetInstanceAsync_requires_matching_player_and_character_ownership()
    {
        await using var db = CreateDb();
        var instance = CreateInstance("ownership", ProphecyStatus.Completed);
        db.ProphecyDefinitions.Add(instance.ProphecyDefinition!);
        db.PlayerProphecyInstances.Add(instance);
        await db.SaveChangesAsync();
        var repository = new ProphecyRepository(db);

        var owned = await repository.GetInstanceAsync(
            instance.Id,
            instance.PlayerId,
            instance.CharacterId,
            CancellationToken.None);
        var wrongPlayer = await repository.GetInstanceAsync(
            instance.Id,
            Guid.NewGuid(),
            instance.CharacterId,
            CancellationToken.None);
        var wrongCharacter = await repository.GetInstanceAsync(
            instance.Id,
            instance.PlayerId,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.NotNull(owned);
        Assert.Null(wrongPlayer);
        Assert.Null(wrongCharacter);
    }

    [Fact]
    public async Task Recent_queries_are_scoped_and_include_rerolled_definition_history()
    {
        await using var db = CreateDb();
        var claimed = CreateInstance("claimed", ProphecyStatus.Claimed);
        claimed.RerolledFromDefinitionId = "original.offer";
        claimed.ClaimedAt = Now;
        var offered = CreateInstance("offered", ProphecyStatus.Offered, claimed.PlayerId, claimed.CharacterId);
        var otherCharacter = CreateInstance("other", ProphecyStatus.Claimed, claimed.PlayerId, Guid.NewGuid());
        db.ProphecyDefinitions.AddRange(
            claimed.ProphecyDefinition!,
            offered.ProphecyDefinition!,
            otherCharacter.ProphecyDefinition!);
        db.PlayerProphecyInstances.AddRange(claimed, offered, otherCharacter);
        await db.SaveChangesAsync();
        var repository = new ProphecyRepository(db);

        var recent = await repository.GetRecentInstancesAsync(
            claimed.PlayerId,
            claimed.CharacterId,
            Now.AddDays(-1),
            10,
            CancellationToken.None);
        var definitionIds = await repository.GetRecentDefinitionIdsAsync(
            claimed.PlayerId,
            claimed.CharacterId,
            ProphecyScope.Daily,
            Now.AddDays(-1),
            Now.AddDays(1),
            CancellationToken.None);

        Assert.Equal(claimed.Id, Assert.Single(recent).Id);
        Assert.Contains(claimed.ProphecyDefinitionId, definitionIds);
        Assert.Contains("original.offer", definitionIds);
        Assert.Contains(offered.ProphecyDefinitionId, definitionIds);
        Assert.DoesNotContain(otherCharacter.ProphecyDefinitionId, definitionIds);
    }

    private static PlayerProphecyInstance CreateInstance(
        string id,
        ProphecyStatus status,
        Guid? playerId = null,
        Guid? characterId = null)
    {
        var definition = new ProphecyDefinition
        {
            Id = $"test.{id}",
            Title = id,
            FlavorText = "Test",
            ObjectiveText = "Test {target}",
            Scope = ProphecyScope.Daily,
            Category = ProphecyCategory.Combat,
            Difficulty = ProphecyDifficulty.Common,
            ObjectiveType = ProphecyObjectiveType.KillCreatures,
            RewardProfileId = "Daily.Test"
        };

        return new PlayerProphecyInstance
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId ?? Guid.NewGuid(),
            CharacterId = characterId ?? Guid.NewGuid(),
            ProphecyDefinitionId = definition.Id,
            ProphecyDefinition = definition,
            Scope = ProphecyScope.Daily,
            SlotType = ProphecySlotType.Steady,
            Status = status,
            PeriodStart = Now.Date,
            PeriodEnd = Now.Date.AddDays(1),
            GeneratedAt = Now.AddHours(-1),
            AcceptedAt = status == ProphecyStatus.Offered ? null : Now.AddMinutes(-30),
            CompletedAt = status is ProphecyStatus.Completed or ProphecyStatus.Claimed ? Now.AddMinutes(-10) : null,
            TargetValue = 1,
            CurrentValue = status is ProphecyStatus.Completed or ProphecyStatus.Claimed ? 1 : 0
        };
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }
}
