using Domain.Models.Prophecies;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;

namespace EssenceSystem.Tests;

public sealed class ProphecyRerollStateTests
{
    [Fact]
    public async Task Row_version_rejects_two_paid_reroll_updates_from_the_same_state()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var stateId = Guid.NewGuid();

        await using (var seed = new LLDbContext(options))
        {
            seed.DailyProphecyRerollStates.Add(new DailyProphecyRerollState
            {
                Id = stateId,
                PlayerId = Guid.NewGuid(),
                CharacterId = Guid.NewGuid(),
                PeriodStart = DateTimeOffset.UtcNow.Date,
                PeriodEnd = DateTimeOffset.UtcNow.Date.AddDays(1),
                RerollsUsed = 1,
                ShownDefinitionIdsJson = "[]"
            });
            await seed.SaveChangesAsync();
        }

        await using var first = new LLDbContext(options);
        await using var second = new LLDbContext(options);
        var firstState = await first.DailyProphecyRerollStates.SingleAsync(x => x.Id == stateId);
        var secondState = await second.DailyProphecyRerollStates.SingleAsync(x => x.Id == stateId);

        firstState.RerollsUsed++;
        firstState.RowVersion++;
        await first.SaveChangesAsync();

        secondState.RerollsUsed++;
        secondState.RowVersion++;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }
}
