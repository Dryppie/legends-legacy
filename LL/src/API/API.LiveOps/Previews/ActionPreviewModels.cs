namespace API.LiveOps.Previews;

public sealed record ActionPreviewField(string Label, string Value);

public sealed record ActionPreviewDto(
    Guid PreviewToken,
    Guid OperationId,
    string ActionKind,
    string Title,
    string TargetName,
    Guid TargetId,
    string RiskLevel,
    DateTimeOffset ExpiresAt,
    string? ConfirmationText,
    IReadOnlyList<ActionPreviewField> Fields,
    IReadOnlyList<string> Warnings);

public sealed record PreviewSubmissionResult(
    bool IsSuccess,
    bool IsConflict,
    string ErrorMessage)
{
    public static PreviewSubmissionResult Success() => new(true, false, string.Empty);
    public static PreviewSubmissionResult Fail(string error, bool conflict = false) =>
        new(false, conflict, error);
}
