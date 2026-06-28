using API.LL.Controllers.V1;
using Application.UseCases.CombatStyles.Commands.ActivateCombatStyle;
using Application.UseCases.CombatStyles.Commands.SelectCombatStyleFocus;
using Application.UseCases.CombatStyles.Dtos;
using Application.UseCases.CombatStyles.Queries.GetCombatBuildPreview;
using Application.UseCases.CombatStyles.Queries.GetCombatStyles;
using Common.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Security.Claims;

namespace EssenceSystem.Tests;

public sealed class CombatStylesControllerTests
{
    [Fact]
    public void Controller_exposes_expected_combat_style_routes()
    {
        var route = typeof(CombatStylesController)
            .GetCustomAttributes<RouteAttribute>()
            .Single(attribute => attribute.Template == "api/v{version:apiVersion}/combat-styles");
        var getOverview = typeof(CombatStylesController).GetMethod(nameof(CombatStylesController.GetCombatStyles));
        var activate = typeof(CombatStylesController).GetMethod(nameof(CombatStylesController.Activate));
        var selectFocus = typeof(CombatStylesController).GetMethod(nameof(CombatStylesController.SelectFocus));
        var buildPreview = typeof(CombatStylesController).GetMethod(nameof(CombatStylesController.GetBuildPreview));

        Assert.Equal("api/v{version:apiVersion}/combat-styles", route?.Template);
        Assert.NotNull(getOverview?.GetCustomAttribute<HttpGetAttribute>());
        Assert.Equal("{styleId}/activate", activate?.GetCustomAttribute<HttpPostAttribute>()?.Template);
        Assert.Equal("{styleId}/focus/{focusId}/select", selectFocus?.GetCustomAttribute<HttpPostAttribute>()?.Template);
        Assert.Equal("build-preview", buildPreview?.GetCustomAttribute<HttpGetAttribute>()?.Template);
    }

    [Fact]
    public async Task GetCombatStyles_sends_overview_query_for_current_character()
    {
        var characterId = Guid.NewGuid();
        var expected = new CombatStylesOverviewDto
        {
            ActiveStyleId = "fighter",
            Styles =
            [
                new()
                {
                    Id = "fighter",
                    Name = "Fighter",
                    IsActive = true
                }
            ]
        };
        var sender = new CapturingSender(request => request switch
        {
            GetCombatStylesQuery query when query.CharacterId == characterId => expected,
            _ => throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.")
        });
        var controller = CreateController(sender, characterId);

        var result = await controller.GetCombatStyles();

        Assert.Same(expected, result.Value);
        Assert.IsType<GetCombatStylesQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task Activate_sends_command_and_returns_response()
    {
        var characterId = Guid.NewGuid();
        var expected = Response<ActivateCombatStyleResponseDto>.Success(new()
        {
            Success = true,
            ActiveStyleId = "defensive",
            Message = "Defensive Style activated."
        });
        var sender = new CapturingSender(request => request switch
        {
            ActivateCombatStyleCommand command
                when command.CharacterId == characterId && command.StyleId == "defensive" => expected,
            _ => throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.")
        });
        var controller = CreateController(sender, characterId);

        var result = await controller.Activate("defensive");

        Assert.True(result.Value?.IsSuccess);
        Assert.Equal("defensive", result.Value?.Data?.ActiveStyleId);
        Assert.IsType<ActivateCombatStyleCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task Activate_returns_failed_response_when_switch_is_blocked()
    {
        var characterId = Guid.NewGuid();
        var expected = Response<ActivateCombatStyleResponseDto>.Fail("Cannot switch Combat Style during an active dungeon run.");
        var sender = new CapturingSender(request => request switch
        {
            ActivateCombatStyleCommand command
                when command.CharacterId == characterId && command.StyleId == "caster" => expected,
            _ => throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.")
        });
        var controller = CreateController(sender, characterId);

        var result = await controller.Activate("caster");

        Assert.False(result.Value?.IsSuccess);
        Assert.Equal("Cannot switch Combat Style during an active dungeon run.", result.Value?.ErrorMessage);
    }

    [Fact]
    public async Task SelectFocus_sends_command_and_returns_updated_style()
    {
        var characterId = Guid.NewGuid();
        var updated = new CombatStyleDto
        {
            Id = "caster",
            Name = "Caster",
            Level = 10,
            SelectedFocusId = "spellblade"
        };
        var expected = Response<CombatStyleDto>.Success(updated);
        var sender = new CapturingSender(request => request switch
        {
            SelectCombatStyleFocusCommand command
                when command.CharacterId == characterId &&
                     command.StyleId == "caster" &&
                     command.FocusId == "spellblade" => expected,
            _ => throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.")
        });
        var controller = CreateController(sender, characterId);

        var result = await controller.SelectFocus("caster", "spellblade");

        Assert.True(result.Value?.IsSuccess);
        Assert.Equal("spellblade", result.Value?.Data?.SelectedFocusId);
        Assert.IsType<SelectCombatStyleFocusCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task GetBuildPreview_sends_query_for_current_character()
    {
        var characterId = Guid.NewGuid();
        var expected = new CombatBuildPreviewDto
        {
            ActiveStyleId = "summoner",
            ActiveStyleName = "Summoner",
            BuildName = "Swarmcaller",
            TopTags = [new() { Tag = "Summon", Score = 4 }]
        };
        var sender = new CapturingSender(request => request switch
        {
            GetCombatBuildPreviewQuery query when query.CharacterId == characterId => expected,
            _ => throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.")
        });
        var controller = CreateController(sender, characterId);

        var result = await controller.GetBuildPreview();

        Assert.Same(expected, result.Value);
        Assert.IsType<GetCombatBuildPreviewQuery>(sender.LastRequest);
    }

    private static CombatStylesController CreateController(ISender sender, Guid characterId)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new SingleServiceProvider(sender),
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.UserData, Guid.NewGuid().ToString()),
                new Claim("CharacterId", characterId.ToString())
            ], "test"))
        };

        return new CombatStylesController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    private sealed class CapturingSender(Func<object, object?> responder) : ISender
    {
        public object? LastRequest { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult((TResponse)responder(request)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            LastRequest = request;
            responder(request);
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(responder(request));
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            AsyncEnumerable.Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            AsyncEnumerable.Empty<object?>();
    }

    private sealed class SingleServiceProvider(ISender sender) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(ISender) ? sender : null;
    }
}
