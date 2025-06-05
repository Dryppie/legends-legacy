using API.Chat.Hubs.Interfaces;
using Application.UsesCases.Chats.Commands.SendMessage;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;

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
        // (optional) rate limit via Redis
        //await RateLimiter.EnsureAllowedAsync(_cache, Context.UserIdentifier!);

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
