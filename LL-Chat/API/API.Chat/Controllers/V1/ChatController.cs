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
using Application.UsesCases.Chats.Queries.GetPlayerMessageHistory;
using Application.UsesCases.Chats.Queries.GetConversationEvidence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
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
        DateTimeOffset UpdatedAt,
        RaidChatLifecycleMessageRequest? LifecycleMessage = null);
    public sealed record RaidChatLifecycleMessageRequest(
        Guid MessageId,
        string Body,
        DateTimeOffset SentAt);
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
    public sealed record ConversationEvidenceBatchRequest(
        IReadOnlyList<ConversationEvidenceRequest> Evidence);
    public sealed record ConversationEvidenceRequest(
        Guid EvidenceId,
        Guid FirstCharacterId,
        Guid SecondCharacterId,
        DateTimeOffset From,
        DateTimeOffset To,
        DateTimeOffset TransferOccurredAt,
        DateTimeOffset ImmediateFrom,
        DateTimeOffset ImmediateTo,
        string? Cursor,
        int Take = 0);
    public sealed record ConversationEvidenceBatchResponse(
        IReadOnlyList<ConversationEvidenceResponse> Evidence);
    public sealed record ConversationEvidenceResponse(
        Guid EvidenceId,
        int FirstToSecondMessageCount,
        int SecondToFirstMessageCount,
        int ImmediateMessageCount,
        DateTimeOffset? FirstMessageAt,
        DateTimeOffset? LastMessageAt,
        int SharedChannelCount,
        int SharedChannelMessageCount,
        IReadOnlyList<ChatMessageDto> Messages,
        string? NextCursor);

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
        if (request.LifecycleMessage is not null
            && (!request.IsOpen
                || request.LifecycleMessage.MessageId == Guid.Empty
                || string.IsNullOrWhiteSpace(request.LifecycleMessage.Body)))
        {
            return BadRequest("Invalid raid channel lifecycle message.");
        }

        var isCurrentRevision = await _raidChat.ApplySnapshotAsync(
            request.RaidRunId,
            request.Revision,
            request.IsOpen,
            request.MemberCharacterIds,
            request.UpdatedAt,
            HttpContext.RequestAborted);

        if (!isCurrentRevision || request.LifecycleMessage is null)
            return NoContent();

        var message = await Mediator.Send(new SendMessageCommand(
            request.RaidRunId.ToString(),
            request.LifecycleMessage.Body,
            Guid.Empty.ToString(),
            "Raid",
            null,
            ChatChannelType.Raid,
            MessageId: request.LifecycleMessage.MessageId,
            SentAt: request.LifecycleMessage.SentAt,
            IsSystemGenerated: true));
        if (message is null)
            return BadRequest("Invalid raid channel lifecycle message.");

        var recipients = request.MemberCharacterIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Select(x => x.ToString())
            .ToArray();
        if (recipients.Length > 0)
            await _hub.Clients.Users(recipients).Receive(message);

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
    [HttpGet("Moderation/{characterId:guid}/Messages")]
    public async Task<ActionResult<PlayerMessageHistoryPageDto>> GetPlayerMessageHistory(
        Guid characterId,
        [FromQuery] string? cursor,
        [FromQuery] int take = 25)
    {
        var authorizationFailure = AuthorizeInternalModeration();
        if (authorizationFailure is not null) return authorizationFailure;

        if (!TryDecodeMessageCursor(cursor, out var beforeSentAt, out var beforeMessageId))
        {
            return BadRequest("The player-message cursor is invalid.");
        }

        var pageSize = Math.Clamp(take, 1, 50);
        var messages = await Mediator.Send(new GetPlayerMessageHistoryQuery(
            characterId,
            pageSize + 1,
            beforeSentAt,
            beforeMessageId));
        var entries = messages.Take(pageSize).ToList();
        var nextCursor = messages.Count > pageSize && entries.Count > 0
            ? EncodeMessageCursor(entries[^1])
            : null;

        return Ok(new PlayerMessageHistoryPageDto(entries, nextCursor));
    }

    [AllowAnonymous]
    [HttpPost("Moderation/ConversationEvidence")]
    public async Task<ActionResult<ConversationEvidenceBatchResponse>> GetConversationEvidence(
        [FromBody] ConversationEvidenceBatchRequest request)
    {
        var authorizationFailure = AuthorizeInternalModeration();
        if (authorizationFailure is not null) return authorizationFailure;
        if (request.Evidence is null || request.Evidence.Count is < 1 or > 25)
        {
            return BadRequest("Between 1 and 25 conversation-evidence requests are required.");
        }
        if (request.Evidence.Select(item => item.EvidenceId).Distinct().Count() !=
            request.Evidence.Count)
        {
            return BadRequest("Conversation-evidence IDs must be unique.");
        }

        var response = new List<ConversationEvidenceResponse>(request.Evidence.Count);
        foreach (var item in request.Evidence)
        {
            if (item.EvidenceId == Guid.Empty ||
                item.FirstCharacterId == Guid.Empty ||
                item.SecondCharacterId == Guid.Empty ||
                item.FirstCharacterId == item.SecondCharacterId ||
                item.From > item.TransferOccurredAt ||
                item.TransferOccurredAt > item.To ||
                item.ImmediateFrom < item.From ||
                item.ImmediateTo > item.To ||
                item.ImmediateFrom > item.TransferOccurredAt ||
                item.TransferOccurredAt > item.ImmediateTo ||
                item.To - item.From > TimeSpan.FromDays(90) ||
                item.Take is < 0 or > 25)
            {
                return BadRequest("A conversation-evidence request is invalid.");
            }
            if (!TryDecodeMessageCursor(
                    item.Cursor,
                    out var beforeSentAt,
                    out var beforeMessageId))
            {
                return BadRequest("A conversation-evidence cursor is invalid.");
            }

            var evidence = await Mediator.Send(new GetConversationEvidenceQuery(
                item.FirstCharacterId,
                item.SecondCharacterId,
                item.From,
                item.To,
                item.ImmediateFrom,
                item.ImmediateTo,
                beforeSentAt,
                beforeMessageId,
                item.Take),
                HttpContext.RequestAborted);
            var nextCursor = evidence.HasMoreMessages && evidence.Messages.Count > 0
                ? EncodeMessageCursor(evidence.Messages[^1])
                : null;
            response.Add(new ConversationEvidenceResponse(
                item.EvidenceId,
                evidence.FirstToSecondMessageCount,
                evidence.SecondToFirstMessageCount,
                evidence.ImmediateMessageCount,
                evidence.FirstMessageAt,
                evidence.LastMessageAt,
                evidence.SharedChannelCount,
                evidence.SharedChannelMessageCount,
                evidence.Messages,
                nextCursor));
        }

        return Ok(new ConversationEvidenceBatchResponse(response));
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

    private static string EncodeMessageCursor(ChatMessageDto message)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new PlayerMessageCursor(
            message.SentAt,
            message.Id));
        return WebEncoders.Base64UrlEncode(payload);
    }

    private static bool TryDecodeMessageCursor(
        string? cursor,
        out DateTimeOffset? sentAt,
        out Guid? messageId)
    {
        sentAt = null;
        messageId = null;
        if (string.IsNullOrWhiteSpace(cursor)) return true;
        if (cursor.Length > 256) return false;

        try
        {
            var decoded = WebEncoders.Base64UrlDecode(cursor);
            var value = JsonSerializer.Deserialize<PlayerMessageCursor>(decoded);
            if (value is null || value.MessageId == Guid.Empty) return false;

            sentAt = value.SentAt;
            messageId = value.MessageId;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record PlayerMessageCursor(
        DateTimeOffset SentAt,
        Guid MessageId);
}
