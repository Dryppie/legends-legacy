using API.LL.Common;
using Common.Authorization.Security;
using Common.Options;
using Common.Primitives;
using Domain.Models.Users;
using Microsoft.Extensions.Options;

public sealed class RefreshTokenRotationCoordinatorTests
{
    [Fact]
    public async Task ExecuteAsync_SharesAnInFlightRotationForTheSameRefreshToken()
    {
        var coordinator = CreateCoordinator();
        var completion = new TaskCompletionSource<Response<Tokens>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;

        Task<Response<Tokens>> Rotate()
        {
            Interlocked.Increment(ref calls);
            return completion.Task;
        }

        var first = coordinator.ExecuteAsync("refresh-token", Rotate);
        var second = coordinator.ExecuteAsync("refresh-token", Rotate);

        Assert.Same(first, second);
        Assert.Equal(1, calls);

        var expected = SuccessfulTokens();
        completion.SetResult(expected);

        Assert.Same(expected, await first);
        Assert.Same(expected, await second);
    }

    [Fact]
    public async Task ExecuteAsync_ReplaysARecentlyCompletedSuccessfulRotation()
    {
        var coordinator = CreateCoordinator();
        var expected = SuccessfulTokens();
        var calls = 0;

        Task<Response<Tokens>> Rotate()
        {
            Interlocked.Increment(ref calls);
            return Task.FromResult(expected);
        }

        var first = await coordinator.ExecuteAsync("refresh-token", Rotate);
        var second = await coordinator.ExecuteAsync("refresh-token", Rotate);

        Assert.Same(expected, first);
        Assert.Same(expected, second);
        Assert.Equal(1, calls);
    }

    private static RefreshTokenRotationCoordinator CreateCoordinator() =>
        new(
            new FakeTokenHasher(),
            Options.Create(new JwtOptions
            {
                RefreshReuseGraceSeconds = 5
            }));

    private static Response<Tokens> SuccessfulTokens() =>
        Response<Tokens>.Success(
            new Tokens("access-token", "replacement-refresh-token", 1234));

    private sealed class FakeTokenHasher : ITokenHasher
    {
        public string Hash(string input) => $"hash:{input}";
    }
}
