using System.Security.Claims;
using API.LL.Common;
using API.LL.Controllers.V1;
using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Synchronization;
using Application.UseCases.Guilds.Queries.GetGuildMissions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace EssenceSystem.Tests;

public sealed class GuildMissionsControllerTests
{
    [Fact]
    public async Task Refresh_cancels_lock_wait_without_faulting_controller_or_blocking_next_request()
    {
        var characterId = Guid.NewGuid();
        var keyedLock = new ReferenceCountedKeyedLock<Guid>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        using var holder = await keyedLock.AcquireAsync(characterId, timeout.Token);
        var entered = 0;
        var sender = new MissionsSender(async (query, token) =>
        {
            using var lease = await keyedLock.AcquireAsync(query.CharacterId, token);
            entered++;
            return null;
        });
        using var services = new ServiceCollection().AddSingleton<ISender>(sender).BuildServiceProvider();
        var controller = CreateController(services, characterId, cancellation.Token);

        var request = controller.GetMissions(cancellation.Token);
        Assert.False(request.IsCompleted);
        cancellation.Cancel();
        var response = await request.WaitAsync(timeout.Token);

        Assert.Equal(ClientDisconnectMiddleware.ClientClosedRequestStatusCode,
            Assert.IsType<StatusCodeResult>(response.Result).StatusCode);
        Assert.True(request.IsCompletedSuccessfully);
        Assert.Equal(0, entered);
        Assert.Equal(1, keyedLock.EntryCount);

        var nextController = CreateController(services, characterId, timeout.Token);
        var nextRequest = nextController.GetMissions(timeout.Token);
        Assert.False(nextRequest.IsCompleted);
        Assert.Equal(0, entered);
        holder.Dispose();
        var nextResponse = await nextRequest.WaitAsync(timeout.Token);

        Assert.Null(nextResponse.Result);
        Assert.Null(nextResponse.Value);
        Assert.Equal(1, entered);
        Assert.Equal(0, keyedLock.EntryCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Successful_overview_preserves_character_token_and_nullable_payload(bool noGuild)
    {
        var characterId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var now = DateTimeOffset.UtcNow;
        GuildMissionOverviewDto? overview = noGuild ? null : new(
            Guid.NewGuid(), 0, 1, now.AddDays(1), now.AddDays(7), false,
            [], null, null, [], new("daily", "weekly", 0, 0, 0, 0, 0, 0), []);
        var sender = new MissionsSender((query, token) =>
        {
            Assert.Equal(characterId, query.CharacterId);
            Assert.Equal(cancellation.Token, token);
            return Task.FromResult(overview);
        });
        using var services = new ServiceCollection().AddSingleton<ISender>(sender).BuildServiceProvider();
        var controller = CreateController(services, characterId, cancellation.Token);

        var response = await controller.GetMissions(cancellation.Token);

        Assert.Null(response.Result);
        Assert.Same(overview, response.Value);
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
            ? new TaskCanceledException("Mission query cancelled")
            : new InvalidOperationException("Mission query failed");
        var sender = new MissionsSender((_, _) => Task.FromException<GuildMissionOverviewDto?>(failure));
        using var services = new ServiceCollection().AddSingleton<ISender>(sender).BuildServiceProvider();
        var controller = CreateController(services, Guid.NewGuid(), requestCancellation.Token);

        if (aborted && cancellation)
        {
            var response = await controller.GetMissions(requestCancellation.Token);
            Assert.Equal(ClientDisconnectMiddleware.ClientClosedRequestStatusCode,
                Assert.IsType<StatusCodeResult>(response.Result).StatusCode);
        }
        else
        {
            var thrown = await Assert.ThrowsAnyAsync<Exception>(
                () => controller.GetMissions(requestCancellation.Token));
            Assert.Same(failure, thrown);
        }
    }

    private static GuildController CreateController(
        IServiceProvider services, Guid characterId, CancellationToken requestAborted) => new()
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = services,
                RequestAborted = requestAborted,
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("CharacterId", characterId.ToString())], "test"))
            }
        }
    };

    private sealed class MissionsSender(
        Func<GetGuildMissionsQuery, CancellationToken, Task<GuildMissionOverviewDto?>> getMissions) : ISender
    {
        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            (TResponse)(object)(await getMissions(Assert.IsType<GetGuildMissionsQuery>(request), cancellationToken))!;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
