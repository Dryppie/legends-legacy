using Application.UsesCases.Chats.Commands.SendSystemMessage;
using Application.UsesCases.Chats.Dtos;
using Application.UsesCases.Chats.Queries.GetChatHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Chat.Controllers.V1;

public class ChatController : BaseController
{
    private const string SystemMessageSecretHeader = "X-LL-System-Chat-Secret";
    private readonly IConfiguration _configuration;

    public ChatController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public record GetChatRequest(string? GuildChannel, int Take = 50);
    public record SendSystemMessageRequest(
        string Body,
        bool IsGlobal,
        Guid? TargetCharacterId,
        string? SenderName,
        Guid? MessageId,
        DateTimeOffset? SentAt);

    [HttpGet("GetChatHistory")]
    public async Task<List<ChatMessageDto>> GetChatHistory([FromQuery] GetChatRequest chatRequest) =>
        await Mediator.Send(new GetChatHistoryQuery(CurrentCharacterGuid, chatRequest.GuildChannel, chatRequest.Take));

    [AllowAnonymous]
    [HttpPost("System")]
    public async Task<ActionResult<ChatMessageDto>> SendSystemMessage([FromBody] SendSystemMessageRequest request)
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

        var message = await Mediator.Send(new SendSystemMessageCommand(
            request.Body,
            request.IsGlobal,
            request.TargetCharacterId,
            request.SenderName,
            request.MessageId,
            request.SentAt));

        return message is null ? BadRequest("Invalid system chat message.") : Ok(message);
    }
}
