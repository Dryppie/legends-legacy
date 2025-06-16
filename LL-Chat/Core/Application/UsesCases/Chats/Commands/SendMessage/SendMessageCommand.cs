using Application.Interfaces.Services.Chats;
using Application.UsesCases.Chats.Dtos;
using AutoMapper;
using Domain.Models.Chats;
using MediatR;

namespace Application.UsesCases.Chats.Commands.SendMessage;
public record SendMessageCommand(string Channel, string Body, string SenderId, string SenderName) : IRequest<ChatMessageDto?>;
public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ChatMessageDto?>
{
    private readonly IChatService _chatService;
    private readonly IMapper _mapper;

    public SendMessageCommandHandler(IChatService chatService, IMapper mapper)
    {
        _chatService = chatService;
        _mapper = mapper;
    }

    public async Task<ChatMessageDto?> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.SenderId, out var senderId)) return null;
        if (!SendMessageValidator.IsValid(request.Body))
            return null;

        var message = new ChatMessage()
        {
            SenderId = senderId,
            SenderName = request.SenderName,
            Body = request.Body,
            Channel = request.Channel,
            SentAt = DateTime.UtcNow,
        };

        await _chatService.AddAsync(message, cancellationToken);

        return _mapper.Map<ChatMessageDto>(message);
    }
}
