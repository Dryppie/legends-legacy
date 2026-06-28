namespace Application.Interfaces.Services.LL.CombatStyles;

public interface ICombatStyleSwitchValidator
{
    Task<CombatStyleSwitchValidationResult> ValidateCanSwitchAsync(Guid characterId, CancellationToken cancellationToken);
}

public sealed record CombatStyleSwitchValidationResult(bool CanSwitch, string? Reason)
{
    public static CombatStyleSwitchValidationResult Allowed() => new(true, null);
    public static CombatStyleSwitchValidationResult Blocked(string reason) => new(false, reason);
}

public sealed record CombatStyleOperationResult(bool Succeeded, string Message, string? ActiveStyleId = null)
{
    public static CombatStyleOperationResult Success(string message, string? activeStyleId = null) =>
        new(true, message, activeStyleId);

    public static CombatStyleOperationResult Fail(string message) =>
        new(false, message);
}

public sealed record CombatStyleOperationResult<T>(bool Succeeded, string Message, T? Value = default)
{
    public static CombatStyleOperationResult<T> Success(T value, string message) =>
        new(true, message, value);

    public static CombatStyleOperationResult<T> Fail(string message) =>
        new(false, message);
}
