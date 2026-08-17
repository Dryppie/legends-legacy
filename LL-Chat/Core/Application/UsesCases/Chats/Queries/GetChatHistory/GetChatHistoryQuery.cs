using Application.Interfaces.Services.Chats;
using Application.UsesCases.Chats.Dtos;
using MediatR;

namespace Application.UsesCases.Chats.Queries.GetChatHistory;
public record GetChatHistoryQuery(
    Guid UserId,
    string? GuildChannel,
    int Take,
    DateTimeOffset? After) : IRequest<List<ChatMessageDto>>;
public class GetChatHistoryQueryHandler : IRequestHandler<GetChatHistoryQuery, List<ChatMessageDto>>
{
    private readonly IChatService _chatService;

    public GetChatHistoryQueryHandler(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<List<ChatMessageDto>> Handle(GetChatHistoryQuery request, CancellationToken cancellationToken)
    {
        var chats = await _chatService.LatestAsync(
            request.UserId,
            request.Take,
            request.GuildChannel,
            request.After,
            cancellationToken);

        return chats.Select(ChatMessageDto.FromDomain).ToList();
    }
}
