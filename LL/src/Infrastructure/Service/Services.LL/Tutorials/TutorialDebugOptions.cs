namespace Services.LL.Tutorials;

public sealed class TutorialDebugOptions
{
    public const string SectionName = "Debug:Tutorial";

    public bool Enabled { get; set; } = true;
    public bool IsDevelopment { get; set; }
}
