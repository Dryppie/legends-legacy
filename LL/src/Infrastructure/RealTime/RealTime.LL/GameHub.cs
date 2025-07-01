using Application.WebSockets.Contracts;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace RealTime.LL;
public sealed class GameHub : Hub<IGameClient>
{
    public override Task OnConnectedAsync()
    {
        Guid charId = Context.RequireCharacterId();                // guaranteed non-null
        return Groups.AddToGroupAsync(Context.ConnectionId, CharacterGroup(charId));
    }

    public async Task SubscribeToAudience(AudienceDto dto)
    {
        var charId = Context.RequireCharacterId();        // you already trust this

        switch (dto)
        {
            case AudienceDto.World:
                await Groups.AddToGroupAsync(Context.ConnectionId, "world");
                break;

            case AudienceDto.Guild g:
                //if (!await UserIsMemberOfGuild(charId, g.GuildId))
                //    throw new HubException("Forbidden – not a member of that guild.");

                //await Groups.AddToGroupAsync(Context.ConnectionId, GuildGroup(g.GuildId));
                break;

            default:
                throw new HubException($"Unsupported audience: {dto.GetType().Name}");
        }
    }

    public override Task OnDisconnectedAsync(Exception? ex)
    {
        Guid? charId = Context.TryGetCharacterId();                // may be null
        return charId is null
            ? Task.CompletedTask
            : Groups.RemoveFromGroupAsync(Context.ConnectionId, CharacterGroup(charId.Value));
    }

    private static string CharacterGroup(Guid id) => $"char:{id}";
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
            var claims = ctx.User?.Claims
                                 .Select(c => $"{c.Type}:{c.Value}")
                                 .DefaultIfEmpty("<none>")
                                 .Aggregate((a, b) => $"{a}, {b}");
            throw new HubException($"CharacterId claim missing or invalid. Current claims: [{claims}]");
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
