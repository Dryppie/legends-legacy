namespace API.LiveOps.Chat;

public sealed class ChatModerationOptions
{
    public const string SectionName = "Chat:Moderation";

    public string BaseUrl { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 5;
}
