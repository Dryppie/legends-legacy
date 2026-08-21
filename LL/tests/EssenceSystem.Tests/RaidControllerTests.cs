using System.Security.Claims;
using API.LL.Controllers.V1;
using Application.UseCases.Raids;
using Application.UseCases.Raids.Commands.UpdateRaidParties;
using Domain.Models.Raids;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace EssenceSystem.Tests;

public sealed class RaidControllerTests
{
    [Fact]
    public async Task HistoryEndpointDispatchesAuthenticatedCharacterAndFilters()
    {
        var characterId = Guid.NewGuid();
        var sender = new RecordingSender();
        var controller = CreateController(sender, characterId);

        await controller.GetHistory("raid-boss.hives-abyss", 12);

        var query = Assert.IsType<GetRaidHistoryQuery>(Assert.Single(sender.Requests));
        Assert.Equal(
            (characterId, "raid-boss.hives-abyss", 12),
            (query.CharacterId, query.RaidBossId, query.Take));
    }

    [Fact]
    public async Task UpdatePartiesEndpointDispatchesCompleteLayoutForAuthenticatedLeader()
    {
        var leaderId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var raidRunId = Guid.NewGuid();
        var sender = new RecordingSender();
        var controller = CreateController(sender, leaderId);

        await controller.UpdateParties(
            raidRunId,
            new RaidController.UpdateRaidPartiesRequest(
            [
                new RaidController.RaidPartyAssignmentRequest(
                    leaderId,
                    RaidLane.Vanguard,
                    0),
                new RaidController.RaidPartyAssignmentRequest(
                    participantId,
                    null,
                    null)
            ]));

        var command = Assert.IsType<UpdateRaidPartiesCommand>(Assert.Single(sender.Requests));
        Assert.Equal((leaderId, raidRunId), (command.CharacterId, command.RaidRunId));
        Assert.Collection(
            command.Assignments,
            assignment => Assert.Equal(
                (leaderId, RaidLane.Vanguard, 0),
                (assignment.CharacterId, assignment.Lane, assignment.WingSlotIndex)),
            assignment => Assert.Equal(
                (participantId, null, null),
                (assignment.CharacterId, assignment.Lane, assignment.WingSlotIndex)));
    }

    [Fact]
    public async Task ApproveSignupEndpointDispatchesLeaderAndApplicant()
    {
        var leaderId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        var raidRunId = Guid.NewGuid();
        var sender = new RecordingSender();
        var controller = CreateController(sender, leaderId);

        await controller.ApproveSignup(
            raidRunId,
            new RaidController.RaidSignupDecisionRequest(applicantId));

        var command = Assert.IsType<ApproveRaidSignupCommand>(Assert.Single(sender.Requests));
        Assert.Equal(
            (leaderId, raidRunId, applicantId),
            (command.CharacterId, command.RaidRunId, command.TargetCharacterId));
    }

    [Fact]
    public async Task RemoveSignupEndpointDispatchesLeaderAndTarget()
    {
        var leaderId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var raidRunId = Guid.NewGuid();
        var sender = new RecordingSender();
        var controller = CreateController(sender, leaderId);

        await controller.RemoveSignup(
            raidRunId,
            new RaidController.RaidSignupDecisionRequest(targetId));

        var command = Assert.IsType<RemoveRaidSignupCommand>(Assert.Single(sender.Requests));
        Assert.Equal(
            (leaderId, raidRunId, targetId),
            (command.CharacterId, command.RaidRunId, command.TargetCharacterId));
    }

    [Fact]
    public async Task DevelopmentCreateEndpointDispatchesAuthenticatedCharacterInDevelopment()
    {
        var characterId = Guid.NewGuid();
        var sender = new RecordingSender();
        var controller = CreateController(sender, characterId);

        await controller.CreateDevelopment(
            "raid-boss.hives-abyss",
            new RaidController.CreateDevelopmentRaidRequest(2),
            new TestWebHostEnvironment("Development"));

        var command = Assert.IsType<CreateDevelopmentRaidCommand>(
            Assert.Single(sender.Requests));
        Assert.Equal(
            (characterId, "raid-boss.hives-abyss", 2),
            (command.CharacterId, command.RaidBossId, command.PlusLevel));
    }

    [Fact]
    public async Task DevelopmentCreateEndpointReturnsNotFoundOutsideDevelopment()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender, Guid.NewGuid());

        var result = await controller.CreateDevelopment(
            "raid-boss.hives-abyss",
            new RaidController.CreateDevelopmentRaidRequest(1),
            new TestWebHostEnvironment("Production"));

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Empty(sender.Requests);
    }

    private static RaidController CreateController(RecordingSender sender, Guid characterId)
    {
        var services = new ServiceCollection()
            .AddSingleton<ISender>(sender)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("CharacterId", characterId.ToString())],
                "Test"))
        };

        return new RaidController
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
            return Task.FromResult(default(TResponse)!);
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
