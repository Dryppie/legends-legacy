using Microsoft.AspNetCore.SignalR;

namespace API.Chat.Hubs.Providers;

public class CharacterIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("CharacterId")?.Value;
    }
}

