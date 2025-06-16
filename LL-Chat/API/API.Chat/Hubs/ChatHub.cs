using API.Chat.Hubs.Interfaces;
using API.Chat.Utility;
using Application.UsesCases.Chats.Commands.SendMessage;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;

namespace API.Chat.Hubs;

public sealed class ChatHub : Hub<IChatClient>
{
    private readonly IMediator _mediator;
    private readonly IDistributedCache _cache;   // for rate-limit / presence

    public ChatHub(IMediator mediator, IDistributedCache cache)
    {
        _mediator = mediator;
        _cache = cache;
    }

    public async Task Send(string channel, string body)
    {
        var userId = Context.UserIdentifier!;
        var allowed = await RateLimiter.EnsureAllowedAsync(_cache, userId);

        if (!allowed)
        {
            //await Clients.Caller.ReceiveSystemMessage("You're sending messages too quickly. Please slow down.");
            return;
        }

        var senderId = Context.UserIdentifier!;
        var senderName = Context.User!.Identity!.Name ?? "Unknown Sender";

        var msg = await _mediator.Send(new SendMessageCommand(channel, body, senderId, senderName));

        if (msg == null) return;

        await Clients.Group(channel).Receive(msg);
    }

    public override Task OnConnectedAsync()
    {
        var ch = Context.GetHttpContext()!.Request.Query["channel"];
        return Groups.AddToGroupAsync(Context.ConnectionId, ch);
    }
}
