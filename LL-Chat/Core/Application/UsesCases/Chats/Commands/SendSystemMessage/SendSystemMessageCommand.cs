using Application.Interfaces.Services.Chats;
using Application.UsesCases.Chats.Commands.SendMessage;
using Application.UsesCases.Chats.Dtos;
using Domain.Models.Chats;
using MediatR;

namespace Application.UsesCases.Chats.Commands.SendSystemMessage;

public record SendSystemMessageCommand(
    string Body,
    bool IsGlobal,
    Guid? TargetCharacterId,
    string? SenderName = null,
    Guid? MessageId = null,
    DateTimeOffset? SentAt = null,
    string? TargetUrl = null) : IRequest<ChatMessageDto?>;

public class SendSystemMessageCommandHandler : IRequestHandler<SendSystemMessageCommand, ChatMessageDto?>
{
    private readonly IChatService _chatService;

    public SendSystemMessageCommandHandler(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<ChatMessageDto?> Handle(SendSystemMessageCommand request, CancellationToken cancellationToken)
    {
        if (!request.IsGlobal && request.TargetCharacterId is null)
        {
            return null;
        }

        if (!SendMessageValidator.IsValid(request.Body))
        {
            return null;
        }

        if (!IsValidTargetUrl(request.TargetUrl))
        {
            return null;
        }

        if (request.MessageId.HasValue)
        {
            var existing = await _chatService.GetByIdAsync(
                request.MessageId.Value,
                cancellationToken);
            if (existing is not null)
            {
                return ChatMessageDto.FromDomain(existing);
            }
        }

        var message = new ChatMessage
        {
            Id = request.MessageId ?? Guid.NewGuid(),
            SenderId = Guid.Empty,
            SenderName = string.IsNullOrWhiteSpace(request.SenderName)
                ? request.IsGlobal ? "World" : "System"
                : request.SenderName.Trim(),
            SenderTitleDisplayName = null,
            Body = request.Body.Trim(),
            TargetUrl = string.IsNullOrWhiteSpace(request.TargetUrl) ? null : request.TargetUrl.Trim(),
            ContextKey = "system",
            SentAt = request.SentAt ?? DateTimeOffset.UtcNow,
            ChannelType = ChatChannelType.System,
            TargetCharacterId = request.IsGlobal ? null : request.TargetCharacterId,
            TargetCharacterName = null,
            TargetCharacterTitleDisplayName = null
        };

        await _chatService.AddAsync(message, cancellationToken);

        return ChatMessageDto.FromDomain(message);
    }

    private static bool IsValidTargetUrl(string? targetUrl)
    {
        if (string.IsNullOrWhiteSpace(targetUrl)) return true;

        var trimmed = targetUrl.Trim();
        return trimmed.Length <= 512
               && trimmed.StartsWith('/')
               && !trimmed.StartsWith("//", StringComparison.Ordinal);
    }
}
