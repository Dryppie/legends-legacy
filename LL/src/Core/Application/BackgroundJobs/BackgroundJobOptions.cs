namespace Application.BackgroundJobs;

public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    public int MaxConcurrency { get; set; } = 5;
    public int RunningExecutionTimeoutMinutes { get; set; } = 30;
}
