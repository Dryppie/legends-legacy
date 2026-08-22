using Application.Interfaces.Services.Chats;
using Application.UsesCases.Chats.Dtos;
using MediatR;

namespace Application.UsesCases.Chats.Queries.GetPlayerMessageHistory;

public sealed record GetPlayerMessageHistoryQuery(
    Guid SenderId,
    int Take,
    DateTimeOffset? BeforeSentAt,
    Guid? BeforeMessageId) : IRequest<IReadOnlyList<ChatMessageDto>>;

public sealed class GetPlayerMessageHistoryQueryHandler(IChatService chatService)
    : IRequestHandler<GetPlayerMessageHistoryQuery, IReadOnlyList<ChatMessageDto>>
{
    public async Task<IReadOnlyList<ChatMessageDto>> Handle(
        GetPlayerMessageHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var messages = await chatService.SentByAsync(
            request.SenderId,
            request.Take,
            request.BeforeSentAt,
            request.BeforeMessageId,
            cancellationToken);

        return messages.Select(ChatMessageDto.FromDomain).ToList();
    }
}
