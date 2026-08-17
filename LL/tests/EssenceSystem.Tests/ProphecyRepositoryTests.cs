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
    public async Task SyncDefinitionsAsync_is_idempotent_when_json_formatting_differs()
    {
        var databaseName = Guid.NewGuid().ToString();
        var storedDefinition = CreateInstance("idempotent", ProphecyStatus.Offered).ProphecyDefinition!;
        storedDefinition.ObjectiveParameterJson =
            "{ \"requiredProfession\": \"Mining\", \"minimumEnemyCount\": 3 }";
        var authoredDefinition = CreateInstance("idempotent", ProphecyStatus.Offered).ProphecyDefinition!;
        authoredDefinition.ObjectiveParameterJson =
            "{\"minimumEnemyCount\":3,\"requiredProfession\":\"Mining\"}";

        await using (var seed = CreateDb(databaseName))
        {
            seed.ProphecyDefinitions.Add(storedDefinition);
            await seed.SaveChangesAsync();
        }

        await using var db = CreateDb(databaseName);
        var repository = new ProphecyRepository(db);

        await repository.SyncDefinitionsAsync([authoredDefinition], CancellationToken.None);

        Assert.False(db.HasChanges);
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

    private static LLDbContext CreateDb(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }
}
