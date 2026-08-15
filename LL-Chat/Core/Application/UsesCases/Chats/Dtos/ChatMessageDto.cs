using Domain.Models.Chats;
using System.Text.Json;

namespace Application.UsesCases.Chats.Dtos;

public class ChatMessageDto
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ChatChannelType ChannelType { get; init; } = ChatChannelType.General;
    public string ContextKey { get; init; } = "global";
    public Guid SenderId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public string? SenderTitleDisplayName { get; init; }
    public string Body { get; init; } = string.Empty;
    public string? TargetUrl { get; init; }
    public bool IsSystemGenerated { get; init; }
    public JsonElement? LinkedItem { get; init; }
    public Guid? TargetCharacterId { get; init; }
    public string? TargetCharacterName { get; init; }
    public string? TargetCharacterTitleDisplayName { get; init; }
    public DateTimeOffset SentAt { get; init; } = DateTimeOffset.UtcNow;

    public static ChatMessageDto FromDomain(ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            ChannelType = message.ChannelType,
            ContextKey = message.ContextKey,
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            SenderTitleDisplayName = message.SenderTitleDisplayName,
            Body = message.Body,
            TargetUrl = message.TargetUrl,
            IsSystemGenerated = message.IsSystemGenerated,
            LinkedItem = ParseLinkedItem(message.LinkedItemJson),
            TargetCharacterId = message.TargetCharacterId,
            TargetCharacterName = message.TargetCharacterName,
            TargetCharacterTitleDisplayName = message.TargetCharacterTitleDisplayName,
            SentAt = message.SentAt
        };
    }

    private static JsonElement? ParseLinkedItem(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
