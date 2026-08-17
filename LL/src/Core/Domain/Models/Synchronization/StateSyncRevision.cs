namespace Domain.Models.Synchronization;

public sealed class StateSyncRevision
{
    public string ScopeKey { get; set; } = string.Empty;
    public long Revision { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
