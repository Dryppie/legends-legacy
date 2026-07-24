using Application.Interfaces.Services.Chats;
using Application.UsesCases.Chats.Dtos;
using Domain.Models.Chats;
using MediatR;

namespace Application.UsesCases.Chats.Commands.SendMessage;
public record SendMessageCommand(
    string Channel,
    string Body,
    string SenderId,
    string SenderName,
    string? SenderTitleDisplayName,
    ChatChannelType ChannelType,
    string? TargetCharacterId = null,
    string? TargetCharacterName = null,
    string? TargetCharacterTitleDisplayName = null) : IRequest<ChatMessageDto?>;
public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ChatMessageDto?>
{
    private readonly IChatService _chatService;

    public SendMessageCommandHandler(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<ChatMessageDto?> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        if (request.ChannelType == ChatChannelType.System) return null;

        if (!Guid.TryParse(request.SenderId, out var senderId)) return null;
        var targetCharacterId = Guid.Empty; // Default to empty GUID for whisper messages
        if (request.TargetCharacterId != null && request.TargetCharacterName != null)
        {
            if (request.SenderId.Equals(request.TargetCharacterId)) return null;
            if (!Guid.TryParse(request.TargetCharacterId, out var targetCharacterGuid)) return null; // Invalid target user ID
            targetCharacterId = targetCharacterGuid; // Set the target user ID for whisper messages
        }

        if (!SendMessageValidator.IsValid(request.Body))
            return null;

        var message = new ChatMessage()
        {
            SenderId = senderId,
            SenderName = request.SenderName,
            SenderTitleDisplayName = NormalizeTitle(request.SenderTitleDisplayName),
            Body = request.Body,
            ContextKey = request.Channel,
            SentAt = DateTime.UtcNow,
            ChannelType = request.ChannelType,
            TargetCharacterId = targetCharacterId,
            TargetCharacterName = request.TargetCharacterName,
            TargetCharacterTitleDisplayName = NormalizeTitle(request.TargetCharacterTitleDisplayName)
        };

        await _chatService.AddAsync(message, cancellationToken);

        return ChatMessageDto.FromDomain(message);
    }

    private static string? NormalizeTitle(string? titleDisplayName)
    {
        return string.IsNullOrWhiteSpace(titleDisplayName)
            ? null
            : titleDisplayName.Trim();
    }
}
