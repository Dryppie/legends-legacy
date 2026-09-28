namespace Domain.Models.Administration;

// Private working text, never an instruction to execute an administrative action.
public sealed class OperatorDraft
{
    public string ActorSubject { get; set; } = "";
    public string Key { get; set; } = "";
    public Guid Version { get; set; }
    public string Content { get; set; } = "{}";
    public DateTimeOffset UpdatedAt { get; set; }
}
