using Application.Interfaces.Services.LL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace RealTime.LL;

[Authorize]
public sealed class GameHub : Hub<IGameClient>
{
    public const string WorldGroup = "world";
    public const string TournamentGroundsGroup = "tournament-grounds";

    private readonly IGuildService _guildService;

    public GameHub(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public override Task OnConnectedAsync()
    {
        var characterId = Context.RequireCharacterId();
        return Groups.AddToGroupAsync(Context.ConnectionId, CharacterGroup(characterId));
    }

    public Task SubscribeToWorld()
    {
        _ = Context.RequireCharacterId();
        return Groups.AddToGroupAsync(Context.ConnectionId, WorldGroup);
    }

    public async Task SubscribeToGuild(Guid guildId)
    {
        var characterId = Context.RequireCharacterId();
        var guild = await _guildService.GetGuildForMemberAsync(characterId, Context.ConnectionAborted);

        if (guild?.Id != guildId)
        {
            throw new HubException("Forbidden - not a member of that guild.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GuildGroup(guildId));
    }

    public Task UnsubscribeFromGuild(Guid guildId)
    {
        _ = Context.RequireCharacterId();
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GuildGroup(guildId));
    }

    public Task SubscribeToRaid(Guid raidRunId)
    {
        _ = Context.RequireCharacterId();
        if (raidRunId == Guid.Empty)
        {
            throw new HubException("Raid id is required.");
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, RaidGroup(raidRunId));
    }

    public Task UnsubscribeFromRaid(Guid raidRunId)
    {
        _ = Context.RequireCharacterId();
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, RaidGroup(raidRunId));
    }

    public Task SubscribeToTournamentGrounds()
    {
        _ = Context.RequireCharacterId();
        return Groups.AddToGroupAsync(Context.ConnectionId, TournamentGroundsGroup);
    }

    public Task UnsubscribeFromTournamentGrounds()
    {
        _ = Context.RequireCharacterId();
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, TournamentGroundsGroup);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var characterId = Context.TryGetCharacterId();
        return characterId is null
            ? Task.CompletedTask
            : Groups.RemoveFromGroupAsync(Context.ConnectionId, CharacterGroup(characterId.Value));
    }

    public static string CharacterGroup(Guid id) => $"char:{id}";
    public static string GuildGroup(Guid id) => $"guild:{id}";
    public static string RaidGroup(Guid id) => $"raid:{id}";
}

public static class HubCallerContextExtensions
{
    private const string ClaimType = "CharacterId";

    public static Guid RequireCharacterId(this HubCallerContext context)
    {
        string? raw = context.User?.FindFirstValue(ClaimType);
        if (!Guid.TryParse(raw, out var id))
        {
            throw new HubException("CharacterId claim missing or invalid.");
        }

        return id;
    }

    public static Guid? TryGetCharacterId(this HubCallerContext context)
    {
        string? raw = context.User?.FindFirstValue(ClaimType);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
