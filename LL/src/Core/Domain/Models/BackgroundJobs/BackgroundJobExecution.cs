namespace Domain.Models.BackgroundJobs;

public sealed class BackgroundJobExecution
{
    public Guid Id { get; set; }

    public string JobName { get; set; } = null!;
    public string BusinessKey { get; set; } = null!;

    public BackgroundJobExecutionStatus Status { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }

    public int Attempt { get; set; }
    public Guid ConcurrencyStamp { get; set; }

    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
