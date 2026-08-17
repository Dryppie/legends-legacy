using Application.UsesCases.Chats.Commands.SendSystemMessage;
using Application.UsesCases.Chats.Commands.SendMessage;
using Application.UsesCases.Chats.Dtos;
using API.Chat.Hubs;
using API.Chat.Hubs.Interfaces;
using Domain.Models.Chats;
using Application.UsesCases.Chats.Queries.GetChatHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Text.Json;

namespace API.Chat.Controllers.V1;

public class ChatController : BaseController
{
    private const string SystemMessageSecretHeader = "X-LL-System-Chat-Secret";
    private readonly IConfiguration _configuration;
    private readonly IHubContext<ChatHub, IChatClient> _hub;

    public ChatController(IConfiguration configuration, IHubContext<ChatHub, IChatClient> hub)
    {
        _configuration = configuration;
        _hub = hub;
    }

    public record GetChatRequest(string? GuildChannel, int Take = 50, DateTimeOffset? After = null);
    public record SendSystemMessageRequest(
        string Body,
        bool IsGlobal,
        Guid? TargetCharacterId,
        string? SenderName,
        Guid? MessageId,
        DateTimeOffset? SentAt,
        string? TargetUrl = null,
        bool Broadcast = false);
    public record SendGuildSystemMessageRequest(
        Guid GuildId,
        Guid ActorCharacterId,
        string ActorName,
        string Body,
        JsonElement? LinkedItem,
        Guid MessageId,
        DateTimeOffset SentAt);

    [HttpGet("GetChatHistory")]
    public async Task<ActionResult<List<ChatMessageDto>>> GetChatHistory([FromQuery] GetChatRequest chatRequest)
    {
        if (!string.IsNullOrWhiteSpace(chatRequest.GuildChannel) && !CanAccessGuild(chatRequest.GuildChannel))
        {
            return Forbid();
        }

        return await Mediator.Send(new GetChatHistoryQuery(
            CurrentCharacterGuid,
            chatRequest.GuildChannel,
            chatRequest.Take,
            chatRequest.After));
    }

    [AllowAnonymous]
    [HttpPost("System")]
    public async Task<ActionResult<ChatMessageDto>> SendSystemMessage([FromBody] SendSystemMessageRequest request)
    {
        var authorizationFailure = AuthorizeSystemMessage();
        if (authorizationFailure is not null) return authorizationFailure;

        var message = await Mediator.Send(new SendSystemMessageCommand(
            request.Body,
            request.IsGlobal,
            request.TargetCharacterId,
            request.SenderName,
            request.MessageId,
            request.SentAt,
            request.TargetUrl));

        if (message is null) return BadRequest("Invalid system chat message.");

        if (request.Broadcast && request.IsGlobal)
        {
            await _hub.Clients.All.Receive(message);
        }
        else if (request.Broadcast && request.TargetCharacterId.HasValue)
        {
            await _hub.Clients.User(request.TargetCharacterId.Value.ToString()).Receive(message);
        }

        return Ok(message);
    }

    [AllowAnonymous]
    [HttpPost("GuildSystem")]
    public async Task<ActionResult<ChatMessageDto>> SendGuildSystemMessage(
        [FromBody] SendGuildSystemMessageRequest request)
    {
        var authorizationFailure = AuthorizeSystemMessage();
        if (authorizationFailure is not null) return authorizationFailure;
        if (request.GuildId == Guid.Empty || request.ActorCharacterId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.ActorName) ||
            request.LinkedItem is { ValueKind: not JsonValueKind.Object })
        {
            return BadRequest("Invalid guild chat message.");
        }

        var message = await Mediator.Send(new SendMessageCommand(
            request.GuildId.ToString(),
            request.Body,
            request.ActorCharacterId.ToString(),
            request.ActorName,
            null,
            ChatChannelType.Guild,
            LinkedItemJson: request.LinkedItem?.GetRawText(),
            MessageId: request.MessageId,
            SentAt: request.SentAt,
            IsSystemGenerated: true));
        if (message is null) return BadRequest("Invalid guild chat message.");

        await _hub.Clients
            .Group(ChatHub.GuildGroupName(request.GuildId.ToString()))
            .Receive(message);
        return Ok(message);
    }

    private ActionResult? AuthorizeSystemMessage()
    {
        var secret = _configuration["SystemMessages:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "System chat message secret is not configured.");
        }

        if (!Request.Headers.TryGetValue(SystemMessageSecretHeader, out var providedSecret) ||
            !string.Equals(providedSecret.ToString(), secret, StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        return null;
    }

    private bool CanAccessGuild(string guildId)
    {
        var currentGuildId = User.FindFirstValue("GuildId");
        return string.Equals(currentGuildId, guildId, StringComparison.OrdinalIgnoreCase);
    }
}
