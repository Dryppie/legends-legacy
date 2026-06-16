using Application.Interfaces.Services.LL;
using Application.WebSockets.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace RealTime.LL;

[Authorize]
public sealed class GameHub : Hub<IGameClient>
{
    private readonly IGuildService _guildService;

    public GameHub(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public override Task OnConnectedAsync()
    {
        var charId = Context.RequireCharacterId();
        return Groups.AddToGroupAsync(Context.ConnectionId, CharacterGroup(charId));
    }

    public async Task SubscribeToAudience(AudienceDto dto)
    {
        _ = Context.RequireCharacterId();

        switch (dto)
        {
            case AudienceDto.World:
                await Groups.AddToGroupAsync(Context.ConnectionId, "world");
                break;

            case AudienceDto.Guild guild:
                await SubscribeToGuild(guild.GuildId);
                break;

            default:
                throw new HubException($"Unsupported audience: {dto.GetType().Name}");
        }
    }

    public Task SubscribeToWorld()
    {
        _ = Context.RequireCharacterId();
        return Groups.AddToGroupAsync(Context.ConnectionId, "world");
    }

    public async Task SubscribeToGuild(Guid guildId)
    {
        var charId = Context.RequireCharacterId();
        var guild = await _guildService.GetGuildWithUpgradesAsync(charId, Context.ConnectionAborted);

        if (guild?.Id != guildId)
            throw new HubException("Forbidden - not a member of that guild.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GuildGroup(guildId));
    }

    public override Task OnDisconnectedAsync(Exception? ex)
    {
        var charId = Context.TryGetCharacterId();
        return charId is null
            ? Task.CompletedTask
            : Groups.RemoveFromGroupAsync(Context.ConnectionId, CharacterGroup(charId.Value));
    }

    private static string CharacterGroup(Guid id) => $"char:{id}";
    private static string GuildGroup(Guid id) => $"guild:{id}";
}

public static class HubCallerContextExtensions
{
    private const string ClaimType = "CharacterId";

    /// <summary>
    /// Returns the CharacterId claim as <see cref="Guid"/>.
    /// Throws <see cref="HubException"/> if the claim is missing or invalid.
    /// </summary>
    public static Guid RequireCharacterId(this HubCallerContext ctx)
    {
        string? raw = ctx.User?.FindFirstValue(ClaimType);
        if (!Guid.TryParse(raw, out var id))
        {
            throw new HubException("CharacterId claim missing or invalid.");
        }
        return id;
    }

    /// <summary>
    /// Returns the CharacterId claim if present; otherwise <c>null</c>. Never throws.
    /// </summary>
    public static Guid? TryGetCharacterId(this HubCallerContext ctx)
    {
        string? raw = ctx.User?.FindFirstValue(ClaimType);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
