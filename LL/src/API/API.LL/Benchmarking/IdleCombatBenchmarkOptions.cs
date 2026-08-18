namespace API.LL.Benchmarking;

public sealed class IdleCombatBenchmarkOptions
{
    public const string SectionName = "Benchmarking:IdleCombat";

    public bool Enabled { get; init; }
    public DateTimeOffset? FixedUtcNow { get; init; }
}
