namespace Domain.Models.Chats;
public sealed class ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Channel { get; init; } = "global";
    public Guid SenderId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;   // markup lives here
    public DateTimeOffset SentAt { get; init; } = DateTimeOffset.UtcNow;
}