using Application.Interfaces.Services.Chats;
using Application.UsesCases.Chats.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UsesCases.Chats.Queries.GetChatHistory;
public record GetChatHistoryQuery(string Channel, int Take) : IRequest<List<ChatMessageDto>>;
public class GetChatHistoryQueryHandler : IRequestHandler<GetChatHistoryQuery, List<ChatMessageDto>>
{
    private readonly IChatService _chatService;
    private readonly IMapper _mapper;

    public GetChatHistoryQueryHandler(IChatService chatService, IMapper mapper)
    {
        _chatService = chatService;
        _mapper = mapper;
    }

    public async Task<List<ChatMessageDto>> Handle(GetChatHistoryQuery request, CancellationToken cancellationToken)
    {
        var chats = await _chatService.LatestAsync(request.Channel, request.Take, cancellationToken);

        return _mapper.Map<List<ChatMessageDto>>(chats);
    }
}
