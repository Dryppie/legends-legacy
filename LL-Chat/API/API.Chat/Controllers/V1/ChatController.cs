using Application.UsesCases.Chats.Commands.SendSystemMessage;
using Application.Interfaces.Services.Chats;
using Application.UsesCases.Chats.Commands.SendMessage;
using Application.UsesCases.Chats.Commands.MuteCharacter;
using Application.UsesCases.Chats.Commands.UnmuteCharacter;
using Application.UsesCases.Chats.Dtos;
using API.Chat.Hubs;
using API.Chat.Hubs.Interfaces;
using Domain.Models.Chats;
using Application.UsesCases.Chats.Queries.GetChatHistory;
using Application.UsesCases.Chats.Queries.GetModerationState;
using Application.UsesCases.Chats.Queries.GetModerationAudit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Text.Json;

namespace API.Chat.Controllers.V1;

public class ChatController : BaseController
{
    private const string SystemMessageSecretHeader = "X-LL-System-Chat-Secret";
    private const string ModerationSecretHeader = "X-LL-Chat-Moderation-Secret";
    private readonly IConfiguration _configuration;
    private readonly IHubContext<ChatHub, IChatClient> _hub;
    private readonly IRaidChatService _raidChat;

    public ChatController(
        IConfiguration configuration,
        IHubContext<ChatHub, IChatClient> hub,
        IRaidChatService raidChat)
    {
        _configuration = configuration;
        _hub = hub;
        _raidChat = raidChat;
    }

    public record GetChatRequest(
        string? GuildChannel,
        string? RaidChannel,
        int Take = 50,
        DateTimeOffset? After = null);
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
    public sealed record UpdateRaidChannelRequest(
        Guid RaidRunId,
        long Revision,
        bool IsOpen,
        IReadOnlyCollection<Guid> MemberCharacterIds,
        DateTimeOffset UpdatedAt);
    public sealed record MuteCharacterRequest(
        Guid OperationId,
        Guid CharacterId,
        string ActorSubject,
        string ActorDisplayName,
        string Reason,
        DateTimeOffset? ExpiresAt);
    public sealed record UnmuteCharacterRequest(
        Guid OperationId,
        Guid RestrictionId,
        string ActorSubject,
        string ActorDisplayName,
        string Reason);

    [HttpGet("GetChatHistory")]
    public async Task<ActionResult<List<ChatMessageDto>>> GetChatHistory([FromQuery] GetChatRequest chatRequest)
    {
        if (!string.IsNullOrWhiteSpace(chatRequest.GuildChannel) && !CanAccessGuild(chatRequest.GuildChannel))
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(chatRequest.RaidChannel))
        {
            if (!Guid.TryParse(chatRequest.RaidChannel, out var raidRunId) ||
                !await _raidChat.CanAccessAsync(
                    raidRunId,
                    CurrentCharacterGuid,
                    HttpContext.RequestAborted))
            {
                return Forbid();
            }
        }

        return await Mediator.Send(new GetChatHistoryQuery(
            CurrentCharacterGuid,
            chatRequest.GuildChannel,
            chatRequest.RaidChannel,
            chatRequest.Take,
            chatRequest.After));
    }

    [AllowAnonymous]
    [HttpPost("RaidChannel")]
    public async Task<IActionResult> UpdateRaidChannel(
        [FromBody] UpdateRaidChannelRequest request)
    {
        var authorizationFailure = AuthorizeSystemMessage();
        if (authorizationFailure is not null) return authorizationFailure;
        if (request.RaidRunId == Guid.Empty || request.Revision < 1)
            return BadRequest("Invalid raid channel snapshot.");

        await _raidChat.ApplySnapshotAsync(
            request.RaidRunId,
            request.Revision,
            request.IsOpen,
            request.MemberCharacterIds,
            request.UpdatedAt,
            HttpContext.RequestAborted);
        return NoContent();
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

    [AllowAnonymous]
    [HttpPost("Mute")]
    public async Task<ActionResult<ChatModerationDto>> MuteCharacter(
        [FromBody] MuteCharacterRequest request)
    {
        var authorizationFailure = AuthorizeInternalModeration();
        if (authorizationFailure is not null) return authorizationFailure;

        var result = await Mediator.Send(new MuteCharacterCommand(
            request.OperationId,
            request.CharacterId,
            request.ActorSubject,
            request.ActorDisplayName,
            request.Reason,
            request.ExpiresAt));
        return result is null
            ? BadRequest("The chat mute could not be applied.")
            : Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("Moderation/{characterId:guid}")]
    public async Task<ActionResult<ChatModerationStateDto>> GetModerationState(
        Guid characterId,
        [FromQuery] int take = 50)
    {
        var authorizationFailure = AuthorizeInternalModeration();
        if (authorizationFailure is not null) return authorizationFailure;

        return Ok(await Mediator.Send(
            new GetModerationStateQuery(characterId, take)));
    }

    [AllowAnonymous]
    [HttpGet("ModerationAudit")]
    public async Task<ActionResult<IReadOnlyList<ChatModerationHistoryEntryDto>>> GetModerationAudit(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? actionType,
        [FromQuery] string? actor,
        [FromQuery] string? reference,
        [FromQuery] Guid? operationId,
        [FromQuery] Guid[]? characterId,
        [FromQuery] Guid? restrictionId,
        [FromQuery] DateTimeOffset? beforeOccurredAt,
        [FromQuery] Guid? beforeOperationId,
        [FromQuery] int take = 51)
    {
        var authorizationFailure = AuthorizeInternalModeration();
        if (authorizationFailure is not null) return authorizationFailure;
        if (from > to) return BadRequest("The audit start date must not be after the end date.");

        return Ok(await Mediator.Send(new GetModerationAuditQuery(
            from,
            to,
            actionType,
            actor,
            reference,
            operationId,
            characterId ?? [],
            restrictionId,
            beforeOccurredAt,
            beforeOperationId,
            Math.Clamp(take, 1, 101))));
    }

    [AllowAnonymous]
    [HttpPost("Unmute")]
    public async Task<ActionResult<ChatModerationDto>> UnmuteCharacter(
        [FromBody] UnmuteCharacterRequest request)
    {
        var authorizationFailure = AuthorizeInternalModeration();
        if (authorizationFailure is not null) return authorizationFailure;

        var result = await Mediator.Send(new UnmuteCharacterCommand(
            request.OperationId,
            request.RestrictionId,
            request.ActorSubject,
            request.ActorDisplayName,
            request.Reason));
        return result is null
            ? BadRequest("The chat mute could not be revoked.")
            : Ok(result);
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

    private ActionResult? AuthorizeInternalModeration()
    {
        var secret = _configuration["InternalModeration:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "Internal chat moderation is not configured.");
        }

        if (!Request.Headers.TryGetValue(ModerationSecretHeader, out var providedSecret) ||
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
