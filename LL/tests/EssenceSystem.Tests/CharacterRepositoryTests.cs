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
            "visibleactivity",
            CancellationToken.None);

        Assert.NotNull(character?.CharacterAction);
        Assert.Equal(activityAt, character.CharacterAction.UpdatedAt);
    }

    [Fact]
    public async Task GetSigilFragmentsAsync_returns_only_the_character_balance()
    {
        await using var db = CreateDb();
        var character = AddCharacter(db, "Sigil Holder");
        character.SigilFragments = 37;
        await db.SaveChangesAsync();
        var repository = new CharacterRepository(db);

        var fragments = await repository.GetSigilFragmentsAsync(
            character.Id,
            CancellationToken.None);
        var missing = await repository.GetSigilFragmentsAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(37, fragments);
        Assert.Null(missing);
    }

    [Fact]
    public async Task Character_name_search_is_case_insensitive_limited_and_excludes_sender()
    {
        await using var db = CreateDb();
        var sender = AddCharacter(db, "Ember");
        AddCharacter(db, "Ember Knight");
        AddCharacter(db, "Ember Mage");
        AddCharacter(db, "Emberfall");
        AddCharacter(db, "Other");
        await db.SaveChangesAsync();
        var repository = new CharacterRepository(db);

        var suggestions = await repository.SearchCharacterNamesAsync(
            "eMbEr",
            sender.Id,
            2,
            CancellationToken.None);

        Assert.Equal(["Ember Knight", "Ember Mage"], suggestions);
    }

    private static Character AddCharacter(LLDbContext db, string name)
    {
        var character = new Character
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = name
        };
        character.NormalizeName();
        db.Characters.Add(character);
        return character;
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }
}
