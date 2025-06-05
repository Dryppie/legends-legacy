using Application.UsesCases.Chats.Dtos;
using Application.UsesCases.Chats.Queries.GetChatHistory;
using Microsoft.AspNetCore.Mvc;

namespace API.Chat.Controllers.V1;
public class ChatController : BaseController
{
    public record GetChatRequest(string Channel = "Global", int Take = 50);

    [HttpGet("GetChatHistory")]
    public async Task<List<ChatMessageDto>> GetChatHistory([FromQuery] GetChatRequest chatRequest) =>
        await Mediator.Send(new GetChatHistoryQuery(chatRequest.Channel, chatRequest.Take));

}
