namespace API.LiveOps.Operations;

[AttributeUsage(AttributeTargets.Method)]
public sealed class TrackOperationAttribute(string kind, string target, string targetKind = "character", string source = "Game") : Attribute
{
    public string Kind { get; } = kind;
    public string Target { get; } = target;
    public string TargetKind { get; } = targetKind;
    public string Source { get; } = source;
}
