using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;

namespace EssenceSystem.Tests;

public sealed class CharacterActionConcurrencyTests
{
    [Fact]
    public async Task Row_version_rejects_two_resolutions_of_the_same_action_revision()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var characterId = Guid.NewGuid();

        await using (var seed = new LLDbContext(options))
        {
            seed.CharacterActions.Add(new CharacterAction
            {
                CharacterId = characterId,
                UpdatedAt = DateTimeOffset.Parse("2026-07-24T10:00:00Z"),
                ActionDetails = new CombatActionDetails(
                    [characterId],
                    new Area { Id = "test-area" })
            });
            await seed.SaveChangesAsync();
        }

        await using var first = new LLDbContext(options);
        await using var second = new LLDbContext(options);
        var firstAction = await first.CharacterActions.SingleAsync(x => x.CharacterId == characterId);
        var secondAction = await second.CharacterActions.SingleAsync(x => x.CharacterId == characterId);

        firstAction.UpdatedAt = firstAction.UpdatedAt.AddSeconds(10);
        firstAction.RowVersion++;
        await first.SaveChangesAsync();

        secondAction.UpdatedAt = secondAction.UpdatedAt.AddSeconds(10);
        secondAction.RowVersion++;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }
}
