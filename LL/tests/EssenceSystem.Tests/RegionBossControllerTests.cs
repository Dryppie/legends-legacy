using System.Security.Claims;
using API.LL.Controllers.V1;
using Application.MediatR.Attributes;
using Application.UseCases.RegionBosses;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace EssenceSystem.Tests;

public sealed class RegionBossControllerTests
{
    [Fact]
    public void Development_spawn_owns_its_transaction_boundary()
    {
        Assert.True(Attribute.IsDefined(
            typeof(SpawnDevelopmentRegionBossCommand),
            typeof(NonTransactionalAttribute)));
    }

    [Theory]
    [InlineData(typeof(SignupRegionBossCommand))]
    [InlineData(typeof(WithdrawRegionBossCommand))]
    public void Signup_mutations_own_their_transaction_boundary(Type commandType)
    {
        Assert.True(Attribute.IsDefined(
            commandType,
            typeof(NonTransactionalAttribute)));
    }

    [Fact]
    public async Task Development_spawn_dispatches_authenticated_character_and_requested_population()
    {
        var characterId = Guid.NewGuid();
        var sender = new RecordingSender();
        var controller = CreateController(sender, characterId);

        await controller.SpawnDevelopment(
            new RegionBossController.SpawnDevelopmentRegionBossRequest(2, 24),
            new TestWebHostEnvironment("Development"));

        var command = Assert.IsType<SpawnDevelopmentRegionBossCommand>(Assert.Single(sender.Requests));
        Assert.Equal((characterId, 2, 24),
            (command.CharacterId, command.RegionId, command.AdditionalSignupCount));
    }

    [Fact]
    public async Task Development_spawn_is_not_routable_outside_development()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender, Guid.NewGuid());

        var result = await controller.SpawnDevelopment(
            new RegionBossController.SpawnDevelopmentRegionBossRequest(2, 24),
            new TestWebHostEnvironment("Production"));

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Empty(sender.Requests);
    }

    private static RegionBossController CreateController(RecordingSender sender, Guid characterId)
    {
        var services = new ServiceCollection().AddSingleton<ISender>(sender).BuildServiceProvider();
        return new RegionBossController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = services,
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim("CharacterId", characterId.ToString())],
                        "Test"))
                }
            }
        };
    }

    private sealed class RecordingSender : ISender
    {
        public List<object> Requests { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
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
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
