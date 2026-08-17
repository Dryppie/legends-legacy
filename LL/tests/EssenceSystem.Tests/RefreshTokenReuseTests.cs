using Common.Exceptions;
using Common.Options;
using Domain.Models.Users;
using Microsoft.Extensions.Options;
using Services.LL.Authorization;

public sealed class RefreshTokenReuseTests
{
    [Fact]
    public async Task RefreshAsync_RevokesActiveUserTokens_WhenRotatedTokenIsReused()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeRefreshTokenRepository
        {
            Token = new RefreshToken
            {
                UserId = userId,
                TokenHash = "hash",
                ExpiresUtc = DateTime.UtcNow.AddDays(1),
                RevokedUtc = DateTime.UtcNow.AddMinutes(-5),
                ReplacedBy = "replacement-hash"
            }
        };
        var generator = new JwtGenerator(
            repository,
            userRepo: null!,
            hasher: null!,
            characterService: null!,
            guildService: null!,
            accountAccess: null!,
            Options.Create(new JwtOptions
            {
                SigningKey = "TestSigningKeyTestSigningKeyTestSigningKeyTestSigningKey",
                Issuer = "issuer",
                Audience = "audience",
                AccessMinutes = 30,
                RefreshDays = 30
            }));

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            generator.RefreshAsync("reused-refresh-token", CancellationToken.None));

        Assert.Equal(userId, repository.RevokedUserId);
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public RefreshToken? Token { get; init; }
        public Guid? RevokedUserId { get; private set; }

        public void Add(RefreshToken token)
        {
        }

        public Task<RefreshToken?> FindAsync(string plaintext, CancellationToken cancellationToken) =>
            Task.FromResult(Token);

        public Task RevokeActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            RevokedUserId = userId;
            return Task.CompletedTask;
        }
    }
}
