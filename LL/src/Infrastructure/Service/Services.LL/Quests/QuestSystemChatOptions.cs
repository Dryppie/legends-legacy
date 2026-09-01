namespace Services.LL.Quests;

public sealed class QuestSystemChatOptions
{
    public string? BaseUrl { get; set; }
    public string? Secret { get; set; }
    public int TimeoutSeconds { get; set; } = 2;
}
