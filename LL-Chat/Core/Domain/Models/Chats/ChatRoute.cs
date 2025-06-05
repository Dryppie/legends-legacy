namespace Domain.Models.Chats;
public sealed record ChatRoute(ChatScope Scope, string? TargetId = null)
{
    // “guild:42”  “whisper:5f2b…” etc.
    public string ToGroupName() =>
        TargetId is null ? Scope.ToString().ToLowerInvariant()
                         : $"{Scope.ToString().ToLowerInvariant()}:{TargetId}";
}