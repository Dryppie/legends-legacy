using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Users;
using Domain.Models.Entities.Characters;
using Domain.Models.Users;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Users;
using Services.LL.Authorization;
using Services.LL.Users;

namespace EssenceSystem.Tests;

public sealed class AccountBindingTests
{
    [Fact]
    public async Task UserRepository_allows_duplicate_internal_account_labels()
    {
        await using var db = CreateDb();
        var repository = new UserRepository(db);

        var first = AppUser.Register("HeroName", "first@example.com", "hash");
        Assert.True(await repository.AddAsync(first, CancellationToken.None));
        await db.SaveChangesAsync();

        var duplicate = AppUser.Register(" heroname ", "second@example.com", "hash");

        Assert.True(await repository.AddAsync(duplicate, CancellationToken.None));
    }

    [Fact]
    public async Task ConvertGuestToUser_rejects_case_insensitive_email_duplicates()
    {
        await using var db = CreateDb();
        var repository = new UserRepository(db);
        var service = CreateUserService(repository);

        var existing = AppUser.Register("ExistingHero", "taken@example.com", "hash");
        Assert.True(await repository.AddAsync(existing, CancellationToken.None));

        var guest = AppUser.Guest();
        guest.Username = "GuestHero";
        Assert.True(await repository.AddAsync(guest, CancellationToken.None));
        await db.SaveChangesAsync();

        var converted = await service.ConvertGuestToUser(
            guest.Id,
            " TAKEN@example.com ",
            "strong-password",
            CancellationToken.None);

        Assert.Null(converted);
        Assert.True(guest.IsGuest);
        Assert.Null(guest.Email);
    }

    [Fact]
    public async Task BindGoogle_converts_guest_into_bound_account()
    {
        await using var db = CreateDb();
        var users = new UserRepository(db);
        var externals = new ExternalLoginRepository(db);
        var userService = CreateUserService(users);
        var google = new GoogleAuthService(
            new FakeGoogleTokenValidator("google-subject", "guest@example.com"),
            externals,
            users,
            userService,
            new CharacterNameAvailabilityService());

        var guest = AppUser.Guest();
        guest.Username = " GuestHero ";
        Assert.True(await users.AddAsync(guest, CancellationToken.None));
        await db.SaveChangesAsync();

        var result = await google.BindAsync(guest.Id, "id-token", CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.NotNull(result);
        Assert.False(result.User.IsGuest);
        Assert.True(result.User.EmailConfirmed);
        Assert.Equal("GuestHero", result.User.Username);
        Assert.Equal("guest@example.com", result.User.Email);
        Assert.Equal("GUEST@EXAMPLE.COM", result.User.NormalizedEmail);

        var externalLogin = await db.ExternalLogins.SingleAsync();
        Assert.Equal(guest.Id, externalLogin.UserId);
        Assert.Equal(AuthProvider.Google, externalLogin.Provider);
        Assert.Equal("google-subject", externalLogin.ProviderUserId);
    }

    [Theory]
    [InlineData("alexandra.smith@gmail.com")]
    [InlineData("alexandra+game@gmail.com")]
    [InlineData("alexandrasmithwithaverylongemailaddress@gmail.com")]
    public async Task Google_signup_uses_a_random_public_name_and_preserves_the_free_rename(string email)
    {
        await using var db = CreateDb();
        var names = new CharacterNameAvailabilityService();
        var google = CreateGoogleService(db, email, names);

        var result = await google.LoginOrCreateAsync("id-token", CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.NotNull(result);
        Assert.True(result.IsNewAccount);
        Assert.NotNull(result.CharacterName);
        Assert.Matches("^[A-Za-z]+_[0-9]{4}$", result.CharacterName);
        Assert.DoesNotContain("alexandra", result.CharacterName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("google-subject", result.CharacterName);
        Assert.True(AuthInputValidator.TryValidateName(result.CharacterName, "Character name", out _, out _));
        Assert.Equal(result.CharacterName, Assert.Single(names.CheckedNames));

        var user = await db.Users.SingleAsync();
        Assert.Equal(result.CharacterName, user.Username);
        Assert.Equal(email, user.Email);
        Assert.True(user.EmailConfirmed);
        Assert.False(user.IsGuest);
        Assert.False(user.IsNameEdited);
        var external = await db.ExternalLogins.SingleAsync();
        Assert.Equal(user.Id, external.UserId);
        Assert.Equal(AuthProvider.Google, external.Provider);
        Assert.Equal("google-subject", external.ProviderUserId);
    }

    [Fact]
    public async Task Google_signup_retries_taken_random_names()
    {
        await using var db = CreateDb();
        var names = new CharacterNameAvailabilityService(takenChecks: 9);
        var google = CreateGoogleService(db, "alexandra.smith@gmail.com", names);

        var result = await google.LoginOrCreateAsync("id-token", CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.NotNull(result);
        Assert.Equal(10, names.CheckedNames.Count);
        Assert.Equal(names.CheckedNames[^1], result.CharacterName);
        Assert.All(names.CheckedNames, name => Assert.Matches("^[A-Za-z]+_[0-9]{4}$", name));
        Assert.Single(await db.Users.ToListAsync());
        Assert.Single(await db.ExternalLogins.ToListAsync());
    }

    [Fact]
    public async Task Google_signup_fails_without_creating_an_account_when_all_random_names_are_taken()
    {
        await using var db = CreateDb();
        var names = new CharacterNameAvailabilityService(takenChecks: 10);
        var google = CreateGoogleService(db, "alexandra.smith@gmail.com", names);

        var result = await google.LoginOrCreateAsync("id-token", CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Null(result);
        Assert.Equal(10, names.CheckedNames.Count);
        Assert.Empty(await db.Users.ToListAsync());
        Assert.Empty(await db.ExternalLogins.ToListAsync());
    }

    [Fact]
    public async Task Returning_Google_player_keeps_their_character_name()
    {
        await using var db = CreateDb();
        var names = new CharacterNameAvailabilityService();
        var google = CreateGoogleService(db, "alexandra.smith@gmail.com", names);
        var firstLogin = await google.LoginOrCreateAsync("id-token", CancellationToken.None);
        Assert.NotNull(firstLogin);
        var character = new Character
        {
            UserId = firstLogin.User.Id,
            User = firstLogin.User,
            Name = "ChosenHero"
        };
        firstLogin.User.IsNameEdited = true;
        db.Characters.Add(character);
        await db.SaveChangesAsync();
        names.CheckedNames.Clear();

        var result = await google.LoginOrCreateAsync("id-token", CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.NotNull(result);
        Assert.False(result.IsNewAccount);
        Assert.Null(result.CharacterName);
        Assert.Equal(firstLogin.User.Id, result.User.Id);
        Assert.True(result.User.IsNameEdited);
        Assert.Equal("ChosenHero", (await db.Characters.SingleAsync()).Name);
        Assert.Empty(names.CheckedNames);
        Assert.Single(await db.Users.ToListAsync());
        Assert.Single(await db.ExternalLogins.ToListAsync());
    }

    [Fact]
    public async Task Google_login_linked_by_email_preserves_the_existing_account_and_character()
    {
        await using var db = CreateDb();
        var user = AppUser.Register("ExistingHero", "alexandra.smith@gmail.com", "hash");
        var character = new Character { UserId = user.Id, User = user, Name = "ChosenHero" };
        db.Users.Add(user);
        db.Characters.Add(character);
        await db.SaveChangesAsync();
        var names = new CharacterNameAvailabilityService();
        var google = CreateGoogleService(db, user.Email!, names);

        var result = await google.LoginOrCreateAsync("id-token", CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.NotNull(result);
        Assert.False(result.IsNewAccount);
        Assert.Null(result.CharacterName);
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal("ExistingHero", result.User.Username);
        Assert.Equal("ChosenHero", (await db.Characters.SingleAsync()).Name);
        Assert.Empty(names.CheckedNames);
        Assert.Single(await db.Users.ToListAsync());
        Assert.Equal(user.Id, (await db.ExternalLogins.SingleAsync()).UserId);
    }

    private static GoogleAuthService CreateGoogleService(
        LLDbContext db, string email, CharacterNameAvailabilityService names)
    {
        var users = new UserRepository(db);
        return new GoogleAuthService(
            new FakeGoogleTokenValidator("google-subject", email),
            new ExternalLoginRepository(db),
            users,
            CreateUserService(users),
            names);
    }

    private static UserService CreateUserService(UserRepository repository) =>
        new(repository, new PasswordHasher<AppUser>());

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private sealed class FakeGoogleTokenValidator(string subject, string email) : IGoogleTokenValidator
    {
        public Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, CancellationToken ct) =>
            Task.FromResult(new GoogleJsonWebSignature.Payload
            {
                Subject = subject,
                Email = email,
                Name = "Alexandra Smith",
                GivenName = "Alexandra",
                FamilyName = "Smith"
            });
    }

    private sealed class CharacterNameAvailabilityService(int takenChecks = 0) : ICharacterService
    {
        public List<string> CheckedNames { get; } = [];

        public Task<Character> CreateCharacterAsync(Guid userId, string username, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Character?> GetMyCharacterAsync(Guid currentUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Character?> GetCharacterByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Character?> GetMyCharacterOverviewAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Character?> GetCharacterOverviewByNameAsync(string characterName, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Character?> UpdateCharacterNameAsync(Guid userId, string username, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> IsCharacterNameTakenAsync(string name, Guid? excludedCharacterId, CancellationToken cancellationToken)
        {
            CheckedNames.Add(name);
            return Task.FromResult(CheckedNames.Count <= takenChecks);
        }

        public Task<Character?> GetBaseCharacterByIdAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Character?> GetCharacterWithSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Guid?> GetCharacterIdByNameAsync(string name, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

    }
}
