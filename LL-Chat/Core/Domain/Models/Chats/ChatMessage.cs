namespace Domain.Models.Chats;
public sealed class ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ChatChannelType ChannelType { get; init; } = ChatChannelType.General;  // default to public
    public string ContextKey { get; init; } = "general"; // e.g., "general", "trade", "help", "guild", "whisper"
    public Guid SenderId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;   // markup lives here
    public Guid? TargetUserId { get; init; } = null; // for whispers, null for public/guild
    public DateTimeOffset SentAt { get; init; } = DateTimeOffset.UtcNow;
}