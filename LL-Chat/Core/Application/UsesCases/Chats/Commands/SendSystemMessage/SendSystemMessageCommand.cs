using Application.Interfaces.Services.Chats;
using Application.UsesCases.Chats.Commands.SendMessage;
using Application.UsesCases.Chats.Dtos;
using AutoMapper;
using Domain.Models.Chats;
using MediatR;

namespace Application.UsesCases.Chats.Commands.SendSystemMessage;

public record SendSystemMessageCommand(
    string Body,
    bool IsGlobal,
    Guid? TargetCharacterId,
    string? SenderName = null,
    Guid? MessageId = null,
    DateTimeOffset? SentAt = null) : IRequest<ChatMessageDto?>;

public class SendSystemMessageCommandHandler : IRequestHandler<SendSystemMessageCommand, ChatMessageDto?>
{
    private readonly IChatService _chatService;
    private readonly IMapper _mapper;

    public SendSystemMessageCommandHandler(IChatService chatService, IMapper mapper)
    {
        _chatService = chatService;
        _mapper = mapper;
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

        var message = new ChatMessage
        {
            Id = request.MessageId ?? Guid.NewGuid(),
            SenderId = Guid.Empty,
            SenderName = string.IsNullOrWhiteSpace(request.SenderName)
                ? request.IsGlobal ? "World" : "System"
                : request.SenderName.Trim(),
            Body = request.Body.Trim(),
            ContextKey = "system",
            SentAt = request.SentAt ?? DateTimeOffset.UtcNow,
            ChannelType = ChatChannelType.System,
            TargetCharacterId = request.IsGlobal ? null : request.TargetCharacterId,
            TargetCharacterName = null
        };

        await _chatService.AddAsync(message, cancellationToken);

        return _mapper.Map<ChatMessageDto>(message);
    }
}
