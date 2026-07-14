using System.Reflection;
using System.Security.Claims;
using API.LL.Controllers.V1;
using Application.UseCases.Prophecies.Commands.AcceptProphecy;
using Application.UseCases.Prophecies.Commands.ClaimProphecy;
using Application.UseCases.Prophecies.Commands.ClaimWeeklyRevelationMilestone;
using Application.UseCases.Prophecies.Commands.GetPropheciesOverview;
using Application.UseCases.Prophecies.Commands.OpenProphecyCache;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace EssenceSystem.Tests;

public sealed class PropheciesControllerTests
{
    [Fact]
    public async Task Endpoints_use_authenticated_player_and_character_claims()
    {
        var playerId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var prophecyId = Guid.NewGuid();
        var sender = new RecordingSender();
        var controller = CreateController(sender, playerId, characterId);

        await controller.GetOverview();
        await controller.Accept(prophecyId);
        await controller.Claim(prophecyId);
        await controller.ClaimWeeklyMilestone(new PropheciesController.ClaimWeeklyMilestoneRequest(5));
        await controller.OpenCache(new PropheciesController.OpenCacheRequest("small_prophecy_cache"));

        var overview = Assert.IsType<GetPropheciesOverviewCommand>(sender.Requests[0]);
        var accept = Assert.IsType<AcceptProphecyCommand>(sender.Requests[1]);
        var claim = Assert.IsType<ClaimProphecyCommand>(sender.Requests[2]);
        var milestone = Assert.IsType<ClaimWeeklyRevelationMilestoneCommand>(sender.Requests[3]);
        var cache = Assert.IsType<OpenProphecyCacheCommand>(sender.Requests[4]);

        Assert.Equal((playerId, characterId), (overview.PlayerId, overview.CharacterId));
        Assert.Equal((playerId, characterId, prophecyId), (accept.PlayerId, accept.CharacterId, accept.ProphecyId));
        Assert.Equal((playerId, characterId, prophecyId), (claim.PlayerId, claim.CharacterId, claim.ProphecyId));
        Assert.Equal((playerId, characterId, 5), (milestone.PlayerId, milestone.CharacterId, milestone.FavorRequired));
        Assert.Equal((characterId, "small_prophecy_cache"), (cache.CharacterId, cache.CacheItemId));
    }

    [Fact]
    public async Task Endpoint_rejects_a_missing_character_claim_before_dispatch()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender, Guid.NewGuid(), characterId: null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.GetOverview());
        Assert.Empty(sender.Requests);
    }

    [Fact]
    public void Controller_requires_authorization()
    {
        Assert.NotEmpty(typeof(PropheciesController).GetCustomAttributes<AuthorizeAttribute>());
    }

    private static PropheciesController CreateController(
        RecordingSender sender,
        Guid playerId,
        Guid? characterId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, playerId.ToString())
        };
        if (characterId.HasValue)
        {
            claims.Add(new Claim("CharacterId", characterId.Value.ToString()));
        }

        var services = new ServiceCollection()
            .AddSingleton<ISender>(sender)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };

        return new PropheciesController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private sealed class RecordingSender : ISender
    {
        public List<object> Requests { get; } = [];

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(Activator.CreateInstance<TResponse>()!);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult<object?>(null);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
