using System.Reflection;
using System.Security.Claims;
using API.LL.Controllers.V1;
using Application.UseCases.WorldTower;
using Application.UseCases.WorldTower.Dtos;
using Domain.Models.WorldTower;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace EssenceSystem.Tests;

public sealed class WorldTowerControllerTests
{
    [Fact]
    public async Task EndpointsDispatchAuthenticatedCharacterAndRoutePayloads()
    {
        var characterId = Guid.NewGuid();
        var rallyId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var sender = new RecordingSender();
        var controller = CreateController(sender, characterId);

        await controller.GetOverview();
        await controller.GetFloor(3);
        await controller.GetRally(rallyId);
        await controller.GetAttemptReport(attemptId);
        await controller.GetAttemptCombatResult(attemptId);
        await controller.GetHallOfFame();
        await controller.GetPersonalExpeditions();
        await controller.CreateRally(new WorldTowerController.CreateRallyRequest(2, TowerRallyMode.Echo));
        await controller.ApplyToRally(rallyId);
        var applicationId = Guid.NewGuid();
        await controller.AcceptApplication(rallyId, applicationId);
        await controller.DeclineApplication(rallyId, applicationId);
        await controller.LeaveRally(rallyId);
        await controller.UpdateLoadout(rallyId);
        var newLeaderId = Guid.NewGuid();
        await controller.TransferLeadership(
            rallyId,
            new WorldTowerController.TransferLeadershipRequest(newLeaderId));
        await controller.FillDevelopmentRoster(rallyId, new TestWebHostEnvironment("Development"));
        await controller.StartRally(rallyId);
        await controller.Contribute(
            4,
            new WorldTowerController.ContributionRequest(TowerContributionKind.ScoutWeakPoints, 3));

        Assert.Equal(17, sender.Requests.Count);
        Assert.Equal(characterId, Assert.IsType<GetWorldTowerOverviewQuery>(sender.Requests[0]).CharacterId);

        var floor = Assert.IsType<GetTowerFloorQuery>(sender.Requests[1]);
        Assert.Equal((characterId, 3), (floor.CharacterId, floor.FloorNumber));

        var rally = Assert.IsType<GetTowerRallyQuery>(sender.Requests[2]);
        Assert.Equal((characterId, rallyId), (rally.CharacterId, rally.RallyId));
        var report = Assert.IsType<GetTowerAttemptReportQuery>(sender.Requests[3]);
        Assert.Equal((characterId, attemptId), (report.CharacterId, report.AttemptId));
        var combatResult = Assert.IsType<GetTowerAttemptCombatResultQuery>(sender.Requests[4]);
        Assert.Equal((characterId, attemptId), (combatResult.CharacterId, combatResult.AttemptId));
        Assert.IsType<GetTowerHallOfFameQuery>(sender.Requests[5]);
        Assert.Equal(
            characterId,
            Assert.IsType<GetPersonalTowerExpeditionsQuery>(sender.Requests[6]).CharacterId);

        var create = Assert.IsType<CreateTowerRallyCommand>(sender.Requests[7]);
        Assert.Equal((characterId, 2, TowerRallyMode.Echo), (create.CharacterId, create.FloorNumber, create.Mode));

        var apply = Assert.IsType<ApplyToTowerRallyCommand>(sender.Requests[8]);
        Assert.Equal((characterId, rallyId), (apply.CharacterId, apply.RallyId));
        var accept = Assert.IsType<AcceptTowerRallyApplicationCommand>(sender.Requests[9]);
        Assert.Equal((characterId, rallyId, applicationId), (accept.CharacterId, accept.RallyId, accept.ApplicationId));
        var decline = Assert.IsType<DeclineTowerRallyApplicationCommand>(sender.Requests[10]);
        Assert.Equal((characterId, rallyId, applicationId), (decline.CharacterId, decline.RallyId, decline.ApplicationId));
        var leave = Assert.IsType<LeaveTowerRallyCommand>(sender.Requests[11]);
        Assert.Equal((characterId, rallyId), (leave.CharacterId, leave.RallyId));
        var loadout = Assert.IsType<UpdateTowerRallyLoadoutCommand>(sender.Requests[12]);
        Assert.Equal((characterId, rallyId), (loadout.CharacterId, loadout.RallyId));
        var transfer = Assert.IsType<TransferTowerRallyLeadershipCommand>(sender.Requests[13]);
        Assert.Equal(
            (characterId, rallyId, newLeaderId),
            (transfer.CharacterId, transfer.RallyId, transfer.TargetCharacterId));
        var fill = Assert.IsType<FillTowerRallyWithDevelopmentCharactersCommand>(sender.Requests[14]);
        Assert.Equal((characterId, rallyId), (fill.CharacterId, fill.RallyId));
        var start = Assert.IsType<StartTowerRallyCommand>(sender.Requests[15]);
        Assert.Equal((characterId, rallyId), (start.CharacterId, start.RallyId));

        var contribute = Assert.IsType<ContributeToTowerCommand>(sender.Requests[16]);
        Assert.Equal(characterId, contribute.CharacterId);
        Assert.Equal(4, contribute.FloorNumber);
        Assert.Equal(TowerContributionKind.ScoutWeakPoints, contribute.Kind);
        Assert.Equal(3, contribute.Amount);
    }

    [Fact]
    public async Task EndpointRejectsMissingCharacterClaimBeforeDispatch()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender, characterId: null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.GetOverview());

        Assert.Empty(sender.Requests);
    }

    [Fact]
    public async Task PlaybackEndpointsDispatchAuthenticatedCharacter()
    {
        var characterId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var sender = new RecordingSender();
        var controller = CreateController(sender, characterId);

        await controller.GetAttemptPlayback(attemptId);
        await controller.GetAttemptPlaybackFrames(attemptId, 17);
        var bundleResult = await controller.GetAttemptPlaybackBundle(attemptId);

        Assert.IsType<NotFoundResult>(bundleResult);
        var manifest = Assert.IsType<GetTowerAttemptPlaybackQuery>(sender.Requests[0]);
        Assert.Equal((characterId, attemptId), (manifest.CharacterId, manifest.AttemptId));
        var frames = Assert.IsType<GetTowerAttemptPlaybackFramesQuery>(sender.Requests[1]);
        Assert.Equal((characterId, attemptId, 17), (frames.CharacterId, frames.AttemptId, frames.AfterSequence));
        var bundle = Assert.IsType<GetTowerAttemptPlaybackBundleQuery>(sender.Requests[2]);
        Assert.Equal((characterId, attemptId), (bundle.CharacterId, bundle.AttemptId));
    }

    [Fact]
    public async Task PlaybackBundleHonorsStrongETag()
    {
        var sender = new RecordingSender
        {
            NextResponse = new TowerPlaybackBundleContentDto(
                [1, 2, 3],
                "application/json",
                "br",
                "content-hash")
        };
        var controller = CreateController(sender, Guid.NewGuid());
        controller.Request.Headers.IfNoneMatch = "\"content-hash\"";

        var result = await controller.GetAttemptPlaybackBundle(Guid.NewGuid());

        Assert.Equal(StatusCodes.Status304NotModified, Assert.IsType<StatusCodeResult>(result).StatusCode);
        Assert.Equal("\"content-hash\"", controller.Response.Headers.ETag);
        Assert.Equal("private, max-age=31536000, immutable", controller.Response.Headers.CacheControl);
    }

    [Fact]
    public void ControllerRequiresAuthorization()
    {
        Assert.NotEmpty(typeof(WorldTowerController).GetCustomAttributes<AuthorizeAttribute>(inherit: true));
    }

    [Fact]
    public async Task DevelopmentRosterEndpointReturnsNotFoundOutsideDevelopment()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender, Guid.NewGuid());

        var result = await controller.FillDevelopmentRoster(
            Guid.NewGuid(),
            new TestWebHostEnvironment("Production"));

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Empty(sender.Requests);
    }

    private static WorldTowerController CreateController(
        RecordingSender sender,
        Guid? characterId)
    {
        var claims = characterId.HasValue
            ? new[] { new Claim("CharacterId", characterId.Value.ToString()) }
            : [];
        var services = new ServiceCollection()
            .AddSingleton<ISender>(sender)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };

        return new WorldTowerController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private sealed class RecordingSender : ISender
    {
        public List<object> Requests { get; } = [];
        public object? NextResponse { get; init; }

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(NextResponse is TResponse response
                ? response
                : default!);
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

    private sealed class TestWebHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = environmentName;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
