using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Chats;

namespace Application.UsesCases.Chats.Dtos;
public class ChatMessageDto : IMapFrom<ChatMessage>
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ChatChannelType ChannelType { get; init; } = ChatChannelType.General;  // default to public
    public string ContextKey { get; init; } = "global";
    public Guid SenderId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;   // markup lives here
    public Guid? TargetUserId { get; init; } = null; // for whispers, null for public/guild
    public DateTimeOffset SentAt { get; init; } = DateTimeOffset.UtcNow;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ChatMessage, ChatMessageDto>();
    }
}
