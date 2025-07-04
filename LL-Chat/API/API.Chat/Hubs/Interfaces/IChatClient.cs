using Application.UsesCases.Chats.Dtos;

namespace API.Chat.Hubs.Interfaces;

public interface IChatClient        // strongly-typed hub (optional)
{
    Task Receive(ChatMessageDto dto);
    Task ReceiveStats(ChannelStatsDto stats);
}
