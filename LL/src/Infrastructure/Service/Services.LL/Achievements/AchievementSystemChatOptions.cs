namespace Services.LL.Achievements;

public sealed class AchievementSystemChatOptions
{
    public string? BaseUrl { get; set; }
    public string? Secret { get; set; }
    public int TimeoutSeconds { get; set; } = 2;
}
