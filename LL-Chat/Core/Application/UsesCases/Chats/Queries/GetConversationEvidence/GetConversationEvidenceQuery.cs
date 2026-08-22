using Application.Interfaces.Services.Chats;
using Application.UsesCases.Chats.Dtos;
using Domain.Models.Chats;
using MediatR;

namespace Application.UsesCases.Chats.Queries.GetConversationEvidence;

public sealed record GetConversationEvidenceQuery(
    Guid FirstCharacterId,
    Guid SecondCharacterId,
    DateTimeOffset From,
    DateTimeOffset To,
    DateTimeOffset ImmediateFrom,
    DateTimeOffset ImmediateTo,
    DateTimeOffset? BeforeSentAt,
    Guid? BeforeMessageId,
    int Take) : IRequest<ChatConversationEvidenceDto>;

public sealed class GetConversationEvidenceQueryHandler(IChatService chatService)
    : IRequestHandler<GetConversationEvidenceQuery, ChatConversationEvidenceDto>
{
    public async Task<ChatConversationEvidenceDto> Handle(
        GetConversationEvidenceQuery request,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.Take, 0, 25);
        var evidence = await chatService.ConversationEvidenceAsync(
            new ChatConversationEvidenceQuery(
                request.FirstCharacterId,
                request.SecondCharacterId,
                request.From,
                request.To,
                request.ImmediateFrom,
                request.ImmediateTo,
                request.BeforeSentAt,
                request.BeforeMessageId,
                pageSize == 0 ? 0 : pageSize + 1),
            cancellationToken);
        var messages = evidence.Messages.Take(pageSize)
            .Select(ChatMessageDto.FromDomain)
            .ToList();

        return new ChatConversationEvidenceDto(
            evidence.FirstToSecondMessageCount,
            evidence.SecondToFirstMessageCount,
            evidence.ImmediateMessageCount,
            evidence.FirstMessageAt,
            evidence.LastMessageAt,
            evidence.SharedChannelCount,
            evidence.SharedChannelMessageCount,
            messages,
            evidence.Messages.Count > pageSize);
    }
}
