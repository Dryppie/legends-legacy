using Microsoft.EntityFrameworkCore;
using Domain.Models.CharacterActions;
using Domain.Models.Entities.Characters;
using Persistence.LL;
using Persistence.LL.Repositories.Entities.Characters;

namespace EssenceSystem.Tests;

public sealed class CharacterRepositoryTests
{
    [Fact]
    public async Task CreateCharacterAsync_creates_full_arena_ticket_status()
    {
        await using var db = CreateDb();
        var repository = new CharacterRepository(db);

        var character = await repository.CreateCharacterAsync(
            Guid.NewGuid(),
            "ArenaNewbie",
            CancellationToken.None);
        await db.SaveChangesAsync();

        var tickets = await db.ArenaTicketStatus.SingleAsync(
            x => x.CharacterId == character.Id);
        Assert.Equal(5, tickets.CurrentTickets);
        Assert.Equal(5, tickets.MaxTickets);
    }

    [Fact]
    public async Task Character_overview_includes_idle_action_activity()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var activityAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        db.Characters.Add(new Character
        {
            Id = characterId,
            UserId = Guid.NewGuid(),
            Name = "VisibleActivity"
        });
        db.CharacterActions.Add(new CharacterAction
        {
            CharacterId = characterId,
            UpdatedAt = activityAt
        });
        await db.SaveChangesAsync();
        var repository = new CharacterRepository(db);

        var character = await repository.GetCharacterOverviewByCharacterNameAsync(
            "VisibleActivity",
            CancellationToken.None);

        Assert.NotNull(character?.CharacterAction);
        Assert.Equal(activityAt, character.CharacterAction.UpdatedAt);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }
}
