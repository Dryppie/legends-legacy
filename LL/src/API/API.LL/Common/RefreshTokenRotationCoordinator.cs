using Common.Authorization.Security;
using Common.Options;
using Common.Primitives;
using Domain.Models.Users;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace API.LL.Common;

public sealed class RefreshTokenRotationCoordinator
{
    private const int MaximumGraceSeconds = 30;

    private readonly ConcurrentDictionary<
        string,
        Lazy<Task<Response<Tokens>>>> _rotations = new();
    private readonly ITokenHasher _tokenHasher;
    private readonly TimeSpan _reuseGracePeriod;

    public RefreshTokenRotationCoordinator(
        ITokenHasher tokenHasher,
        IOptions<JwtOptions> options)
    {
        _tokenHasher = tokenHasher;
        _reuseGracePeriod = TimeSpan.FromSeconds(
            Math.Clamp(
                options.Value.RefreshReuseGraceSeconds,
                0,
                MaximumGraceSeconds));
    }

    public Task<Response<Tokens>> ExecuteAsync(
        string refreshToken,
        Func<Task<Response<Tokens>>> rotateTokens)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        ArgumentNullException.ThrowIfNull(rotateTokens);

        var key = _tokenHasher.Hash(refreshToken);
        var candidate = new Lazy<Task<Response<Tokens>>>(
            rotateTokens,
            LazyThreadSafetyMode.ExecutionAndPublication);
        var rotation = _rotations.GetOrAdd(key, candidate);

        if (ReferenceEquals(rotation, candidate))
        {
            _ = RemoveAfterGracePeriodAsync(key, rotation);
        }

        return rotation.Value;
    }

    private async Task RemoveAfterGracePeriodAsync(
        string key,
        Lazy<Task<Response<Tokens>>> rotation)
    {
        try
        {
            var result = await rotation.Value.ConfigureAwait(false);
            if (result.IsSuccess && result.Data is not null && _reuseGracePeriod > TimeSpan.Zero)
            {
                await Task.Delay(_reuseGracePeriod).ConfigureAwait(false);
            }
        }
        catch
        {
            // Failed rotations are removed immediately and remain observable to every waiter.
        }
        finally
        {
            if (_rotations.TryGetValue(key, out var current)
                && ReferenceEquals(current, rotation))
            {
                _rotations.TryRemove(key, out _);
            }
        }
    }
}
