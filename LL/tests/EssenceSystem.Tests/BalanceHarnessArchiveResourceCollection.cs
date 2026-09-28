namespace EssenceSystem.Tests;

// These fixtures durably write and audit tens of thousands of reports under
// wall-clock resource limits. Concurrent suites must not consume that allowance.
[CollectionDefinition("Exclusive archive resource tests", DisableParallelization = true)]
public sealed class BalanceHarnessArchiveResourceCollection;
