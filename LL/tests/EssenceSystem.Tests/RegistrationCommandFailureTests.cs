using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Behaviors;
using Application.UseCases.Users.Commands.GuestLogin;
using Application.UseCases.Users.Commands.Register;
using Common.Authorization.Security;
using Common.Primitives;
using Domain.Models.Entities.Characters;
using Domain.Models.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.LL;

namespace EssenceSystem.Tests;

public sealed class RegistrationCommandFailureTests
{
    [Fact]
    public async Task Register_propagates_token_failure_after_user_creation()
    {
        var user = AppUser.Register("Hero", "hero@example.test", "hash");
        var character = CreateCharacter(user, "Hero");
        var expected = new InvalidOperationException("Token issuance failed.");
        var publisher = new RecordingPublisher();
        var handler = new RegisterCommandHandler(
            new StubUserService { RegisteredUser = user },
            new StubJwtGenerator { IssueException = expected },
            new StubCharacterService { Character = character },
            publisher);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new RegisterCommand("Hero", "hero@example.test", "password123"),
                CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.Single(publisher.Notifications);
    }

    [Fact]
    public async Task Guest_registration_propagates_token_failure_after_user_creation()
    {
        var user = AppUser.Guest();
        var character = CreateCharacter(user, "Guest");
        var expected = new InvalidOperationException("Token issuance failed.");
        var publisher = new RecordingPublisher();
        var handler = new GuestLoginCommandHandler(
            new StubUserService { GuestUser = user },
            new StubJwtGenerator { IssueException = expected },
            publisher,
            new StubCharacterService { Character = character });

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new GuestLoginCommand(), CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.Single(publisher.Notifications);
    }

    [Fact]
    public async Task Missing_character_after_registration_is_an_invariant_failure()
    {
        var user = AppUser.Register("Hero", "hero@example.test", "hash");
        var publisher = new RecordingPublisher();
        var jwt = new StubJwtGenerator();
        var handler = new RegisterCommandHandler(
            new StubUserService { RegisteredUser = user },
            jwt,
            new StubCharacterService(),
            publisher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new RegisterCommand("Hero", "hero@example.test", "password123"),
                CancellationToken.None));

        Assert.Equal("Registration completed without creating a character.", exception.Message);
        Assert.Single(publisher.Notifications);
        Assert.Equal(0, jwt.IssueCalls);
    }

    [Fact]
    public async Task Duplicate_email_remains_an_expected_business_failure_before_publication()
    {
        var publisher = new RecordingPublisher();
        var jwt = new StubJwtGenerator();
        var handler = new RegisterCommandHandler(
            new StubUserService(),
            jwt,
            new StubCharacterService(),
            publisher);

        var response = await handler.Handle(
            new RegisterCommand("Hero", "hero@example.test", "password123"),
            CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal("Email is already in use.", response.ErrorMessage);
        Assert.Empty(publisher.Notifications);
        Assert.Equal(0, jwt.IssueCalls);
    }

    [Fact]
    public async Task Propagated_registration_failure_does_not_persist_tracked_mutations()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var db = new LLDbContext(options);
        var behavior = new TransactionBehavior<RegisterCommand, Response<Tokens>>(
            db,
            null!,
            NullLogger<TransactionBehavior<RegisterCommand, Response<Tokens>>>.Instance);
        var user = AppUser.Register("Hero", "hero@example.test", "hash");
        var expected = new InvalidOperationException("Token issuance failed.");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new RegisterCommand("Hero", "hero@example.test", "password123"),
                _ =>
                {
                    db.Users.Add(user);
                    return Task.FromException<Response<Tokens>>(expected);
                },
                CancellationToken.None));

        Assert.Same(expected, actual);

        await using var verification = new LLDbContext(options);
        Assert.False(await verification.Users.AnyAsync());
    }

    private static Character CreateCharacter(AppUser user, string name) => new()
    {
        User = user,
        UserId = user.Id,
        Name = name
    };

    private sealed class StubUserService : IUserService
    {
        public AppUser? RegisteredUser { get; init; }
        public AppUser? GuestUser { get; init; }

        public Task<AppUser?> RegisterAsync(
            string accountLabel,
            string email,
            string password,
            CancellationToken cancellationToken) =>
            Task.FromResult(RegisteredUser);

        public Task<AppUser?> RegisterGuestAsync(
            string accountLabel,
            CancellationToken cancellationToken) =>
            Task.FromResult(GuestUser);

        public Task<AppUser?> ValidateCredentialsAsync(
            string email,
            string password,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> EmailExistsAsync(
            string email,
            Guid? excludedUserId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UserInfo?> GetUserInfo(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AppUser?> ConvertGuestToUser(
            Guid userId,
            string email,
            string password,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public bool UpdateUserInfo(AppUser user) => throw new NotSupportedException();

        public Task<AppUser?> GetUserById(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubCharacterService : ICharacterService
    {
        public Character? Character { get; init; }
        public bool IsNameTaken { get; init; }

        public Task<Character?> GetMyCharacterAsync(
            Guid currentUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Character);

        public Task<bool> IsCharacterNameTakenAsync(
            string name,
            Guid? excludedCharacterId,
            CancellationToken cancellationToken) =>
            Task.FromResult(IsNameTaken);

        public Task<Character> CreateCharacterAsync(
            Guid userId,
            string username,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Character?> GetCharacterByCharacterIdAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Character?> GetMyCharacterOverviewAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Character?> GetCharacterOverviewByNameAsync(
            string characterName,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Character?> UpdateCharacterNameAsync(
            Guid userId,
            string username,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Character?> GetBaseCharacterByIdAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Character?> GetCharacterWithSoulstoneUpgradesAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Guid?> GetCharacterIdByNameAsync(
            string name,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubJwtGenerator : IJwtGenerator
    {
        public Exception? IssueException { get; init; }
        public int IssueCalls { get; private set; }

        public Task<Tokens> IssueTokens(AppUser user, Character character)
        {
            IssueCalls++;
            if (IssueException is not null)
            {
                return Task.FromException<Tokens>(IssueException);
            }

            return Task.FromResult(new Tokens("access", "refresh", 1));
        }

        public Task<Tokens?> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> RevokeRefreshTokenAsync(
            string refreshToken,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> ValidateAccessToken(string token) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingPublisher : IPublisher
    {
        public List<object> Notifications { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            Publish((object)notification, cancellationToken);
    }
}
