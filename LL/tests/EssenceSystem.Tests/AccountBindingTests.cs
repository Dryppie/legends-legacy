using Application.Authorization.Interfaces;
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
    public async Task UserRepository_rejects_case_insensitive_username_duplicates()
    {
        await using var db = CreateDb();
        var repository = new UserRepository(db);

        var first = AppUser.Register("HeroName", "first@example.com", "hash");
        Assert.True(await repository.AddAsync(first, CancellationToken.None));
        await db.SaveChangesAsync();

        var duplicate = AppUser.Register(" heroname ", "second@example.com", "hash");

        Assert.False(await repository.AddAsync(duplicate, CancellationToken.None));
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
            "GuestHero",
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
            userService);

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
        Assert.Equal("GUESTHERO", result.User.NormalizedUsername);
        Assert.Equal("guest@example.com", result.User.Email);
        Assert.Equal("GUEST@EXAMPLE.COM", result.User.NormalizedEmail);

        var externalLogin = await db.ExternalLogins.SingleAsync();
        Assert.Equal(guest.Id, externalLogin.UserId);
        Assert.Equal(AuthProvider.Google, externalLogin.Provider);
        Assert.Equal("google-subject", externalLogin.ProviderUserId);
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
                Email = email
            });
    }
}
