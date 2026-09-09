using System.Security.Claims;
using API.LL.Common;
using API.LL.Controllers.V1;
using Application.Interfaces.Services.LL;
using Application.WebSockets.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EssenceSystem.Tests;

public sealed class StateSyncControllerTests
{
    [Fact]
    public async Task Refresh_cancels_pending_checkpoint_without_faulting_controller_task()
    {
        using var requestCancellation = new CancellationTokenSource();
        var pendingCheckpoint = new TaskCompletionSource<StateSyncCheckpoint>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new CheckpointService((_, token) => pendingCheckpoint.Task.WaitAsync(token));
        var controller = CreateController(service, Guid.NewGuid(), requestCancellation.Token);

        var request = controller.GetCheckpoint(requestCancellation.Token);
        Assert.False(request.IsCompleted);
        requestCancellation.Cancel();
        var response = await request;

        Assert.Equal(ClientDisconnectMiddleware.ClientClosedRequestStatusCode,
            Assert.IsType<StatusCodeResult>(response.Result).StatusCode);
        Assert.True(request.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Successful_checkpoint_preserves_character_token_and_payload()
    {
        using var requestCancellation = new CancellationTokenSource();
        var characterId = Guid.NewGuid();
        var checkpoint = new StateSyncCheckpoint(characterId,
            new Dictionary<string, long> { [StateSyncScopes.Character] = 7 }, DateTimeOffset.UtcNow);
        var service = new CheckpointService((id, token) =>
        {
            Assert.Equal(characterId, id);
            Assert.Equal(requestCancellation.Token, token);
            return Task.FromResult(checkpoint);
        });
        var controller = CreateController(service, characterId, requestCancellation.Token);

        var response = await controller.GetCheckpoint(requestCancellation.Token);

        Assert.Same(checkpoint, Assert.IsType<OkObjectResult>(response.Result).Value);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Only_cancellation_of_an_aborted_request_is_handled(bool aborted, bool cancellation)
    {
        using var requestCancellation = new CancellationTokenSource();
        if (aborted)
            requestCancellation.Cancel();
        Exception failure = cancellation
            ? new OperationCanceledException("Database operation cancelled")
            : new InvalidOperationException("Database operation failed");
        var service = new CheckpointService((_, _) => Task.FromException<StateSyncCheckpoint>(failure));
        var controller = CreateController(service, Guid.NewGuid(), requestCancellation.Token);

        if (aborted && cancellation)
        {
            var response = await controller.GetCheckpoint(requestCancellation.Token);
            Assert.Equal(ClientDisconnectMiddleware.ClientClosedRequestStatusCode,
                Assert.IsType<StatusCodeResult>(response.Result).StatusCode);
        }
        else
        {
            var thrown = await Assert.ThrowsAnyAsync<Exception>(
                () => controller.GetCheckpoint(requestCancellation.Token));
            Assert.Same(failure, thrown);
        }
    }

    private static StateSyncController CreateController(
        IStateSyncService service, Guid characterId, CancellationToken requestAborted) => new(service)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestAborted = requestAborted,
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("CharacterId", characterId.ToString())], "test"))
            }
        }
    };

    private sealed class CheckpointService(
        Func<Guid, CancellationToken, Task<StateSyncCheckpoint>> getCheckpoint) : IStateSyncService
    {
        public Task<StateSyncCheckpoint> GetCheckpointAsync(Guid characterId, CancellationToken cancellationToken = default) =>
            getCheckpoint(characterId, cancellationToken);

        public IReadOnlyDictionary<string, long> GetChangedRevisions(Guid? characterId) => throw new NotSupportedException();
        public Task InvalidateCharacterAsync(Guid characterId, string reason, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task InvalidateCharacterScopeAsync(Guid characterId, string scope, string reason, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task InvalidateWorldScopeAsync(string scope, string reason, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
