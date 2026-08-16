namespace EssenceSystem.Tests;

/// <summary>
/// A <see cref="FactAttribute"/> for exhaustive balance tests. The test is reported as skipped
/// whenever <see cref="BalanceSuiteGate"/> decides the suite is not needed for this run.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class BalanceFactAttribute : FactAttribute
{
    public BalanceFactAttribute()
    {
        Skip = BalanceSuiteGate.SkipReason;
    }
}

/// <summary>
/// A <see cref="TheoryAttribute"/> for exhaustive balance tests. The test is reported as skipped
/// whenever <see cref="BalanceSuiteGate"/> decides the suite is not needed for this run.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class BalanceTheoryAttribute : TheoryAttribute
{
    public BalanceTheoryAttribute()
    {
        Skip = BalanceSuiteGate.SkipReason;
    }
}
