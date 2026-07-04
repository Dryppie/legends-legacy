using API.Chat.Hubs.Interfaces;
using API.Chat.Utility;
using Application.UsesCases.Chats.Commands.SendMessage;
using Domain.Models.Chats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;

namespace API.Chat.Hubs;

[Authorize]
public sealed class ChatHub : Hub<IChatClient>
{
    private const string PublicPrefix = "pub:";   // e.g.  "pub:general"
    private const string GuildPrefix = "guild:"; // e.g.  "guild:5f7e…"  (GUID or slug)
    private const string StatsGroup = "stats";   // e.g.  "pub:general"

    private readonly IMediator _mediator;
    private readonly IDistributedCache _cache;   // for rate-limit / presence

    public ChatHub(IMediator mediator, IDistributedCache cache)
    {
        _mediator = mediator;
        _cache = cache;
    }

    public async Task Send(
        string contextKey,
        string body,
        ChatChannelType channelType,
        string? targetCharacterId = null,
        string? targetCharacterName = null,
        string? targetCharacterTitleDisplayName = null,
        string? senderTitleDisplayName = null)
    {

        var senderId = Context.UserIdentifier;
        if (string.IsNullOrWhiteSpace(senderId))
        {
            throw new HubException("Chat connection is not authenticated.");
        }

        if (!CanWriteChat())
        {
            throw new HubException("Register your account before writing in chat.");
        }

        if (!await RateLimiter.EnsureAllowedAsync(_cache, senderId))
            return;

        if (channelType == ChatChannelType.Guild && !CanAccessGuild(contextKey))
        {
            throw new HubException("Forbidden - not a member of that guild.");
        }

        var senderName = Context.User!.Identity!.Name ?? "Unknown Sender";
        senderTitleDisplayName = NormalizeTitleDisplayName(senderTitleDisplayName)
            ?? NormalizeTitleDisplayName(Context.User.FindFirst("CharacterTitleDisplayName")?.Value);

        var msg = await _mediator.Send(new SendMessageCommand(
            contextKey,
            body,
            senderId,
            senderName,
            senderTitleDisplayName,
            channelType,
            targetCharacterId,
            targetCharacterName,
            targetCharacterTitleDisplayName));
        if (msg == null) return;

        switch (channelType)
        {
            case ChatChannelType.General:
            case ChatChannelType.Trade:
            case ChatChannelType.Help:
                if (string.IsNullOrWhiteSpace(contextKey))
                    return; // invalid room

                await Clients.Group(PublicPrefix).Receive(msg);
                break;

            case ChatChannelType.Guild:
                if (string.IsNullOrWhiteSpace(contextKey))
                    return; // invalid guild id

                await Clients.Group(GuildPrefix + contextKey).Receive(msg);
                break;

            case ChatChannelType.Whisper:
                if (string.IsNullOrWhiteSpace(targetCharacterId))
                    return; // recipient missing

                await Clients.User(targetCharacterId).Receive(msg); // recipient
                await Clients.User(senderId).Receive(msg);     // echo to sender
                break;
        }
    }

    /// <summary>Client requests to join a public room.</summary>
    public Task JoinPublic(string room)
        => Groups.AddToGroupAsync(Context.ConnectionId, PublicPrefix + room);

    public Task LeavePublic(string room)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, PublicPrefix + room);

    /// <summary>Server-side code (e.g. after auth) calls this to enrol a connection in its guilds.</summary>
    public Task JoinGuild(string guildId)
    {
        if (!CanAccessGuild(guildId))
        {
            throw new HubException("Forbidden - not a member of that guild.");
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, GuildPrefix + guildId);
    }

    public Task LeaveGuild(string guildId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GuildPrefix + guildId);

    // ---------------------------  LIFECYCLE  ---------------------------

    public override async Task OnConnectedAsync()
    {
        //await Groups.AddToGroupAsync(Context.ConnectionId, StatsGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, PublicPrefix);
    }

    private static string? NormalizeTitleDisplayName(string? titleDisplayName)
    {
        return string.IsNullOrWhiteSpace(titleDisplayName)
            ? null
            : titleDisplayName.Trim();
    }

    private bool CanAccessGuild(string guildId)
    {
        if (string.IsNullOrWhiteSpace(guildId))
        {
            return false;
        }

        var currentGuildId = Context.User?.FindFirstValue("GuildId");
        return string.Equals(currentGuildId, guildId, StringComparison.OrdinalIgnoreCase);
    }

    private bool CanWriteChat()
    {
        var guestClaim = Context.User?.FindFirstValue("guest");
        return bool.TryParse(guestClaim, out var isGuest) && !isGuest;
    }
}
