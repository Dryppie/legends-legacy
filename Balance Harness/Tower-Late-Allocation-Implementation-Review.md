# Late-allocation implementation review

Completed **14 September 2026**. Target: the offline BalanceHarness. The separately frozen [experiment plan](Tower-Late-Allocation-Plan.md) governs execution; the [proposal](Tower-Late-Allocation-Proposal.md) and [trajectory diagnosis](Tower-Allocation-Trajectory-Diagnosis-Review.md) remain historical design evidence.

## Result

The opt-in `independent-late-allocation-v18` generator and `tower-late-allocation-v1` comparison implement two isolated 256-candidate prefixes followed by one allocation decision. The preferred component continues to 512; its partner remains at 256. Each keeps exactly 96 initial fresh parties, its own random stream, proposal and mutation counters, parent pool and ranked module library. The comparator remains deep v13 with 768 candidates and 192 initial fresh parties.

The existing kernel now constructs an in-memory continuation for each arm. Historical policies invoke it once with their original final limit. V18 invokes the selected component twice while preserving all state. Completed reports record both complete prefix rankings, consumed attempts and the selected method. Complete-family freezing independently reconstructs that decision and rejects changed inputs, choices or final counts. The full saved-report verifier regenerates proposals and allocation from saved measurements without combat.

The new policy alone has 94 explicit controls, a 128-recipe family capacity, a 114,688-fight maximum and 10,800 execution seconds. Historical v17 retains 74 controls, capacity 112, 106,496 fights and 5,400 seconds. The 4 GiB storage limit, zero retries, top-32/64-trial screening, original/screened nominee retention, 512-trial confirmation and 2/3 reliability rule are unchanged. No transfer of unused proposal attempts, parents or modules occurs.

## Changed implementation files

- [TowerLateAllocation.cs](../LL/tools/BalanceHarness/TowerLateAllocation.cs): policy constants, exact prefix capture, deterministic choice and final-budget validation.
- [TowerBossGeneration.cs](../LL/tools/BalanceHarness/TowerBossGeneration.cs): persistent per-arm continuation and nullable allocation evidence, omitted for historical policies.
- [TowerSearchAllocation.cs](../LL/tools/BalanceHarness/TowerSearchAllocation.cs), [TowerGenerationComparisonDesign.cs](../LL/tools/BalanceHarness/TowerGenerationComparisonDesign.cs), [TowerFeedbackBenchmark.cs](../LL/tools/BalanceHarness/TowerFeedbackBenchmark.cs), and [TowerFeedbackBenchmarkRun.cs](../LL/tools/BalanceHarness/TowerFeedbackBenchmarkRun.cs): reuse the existing grouping, family, selection and execution contracts with policy-specific limits.
- [TowerBossDiscoveryContract.cs](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs), [TowerBossPartyGenerator.cs](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs), [TowerPartyCoverage.cs](../LL/tools/BalanceHarness/TowerPartyCoverage.cs), and [TowerLoadoutComposition.cs](../LL/tools/BalanceHarness/TowerLoadoutComposition.cs): explicitly register the new independent policy with existing legality and mechanic rules.
- [Program.cs](../LL/tools/BalanceHarness/Program.cs): `tower-late-allocation-prepare`, `-check`, `-run`, and `-verify` commands.
- [TowerLateAllocation tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerLateAllocationTests.cs) and [comparison tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerFeedbackBenchmarkTests.cs): continuation, old-prefix parity, both choices, ties, cancellation, exhaustion, provenance, all controls, overflow, fresh schedules, protocol integrity and unchanged statistical gates.

## Verification

Backend verification ran through `build/run-tests.ps1`: **349 distinct relevant tests passed**, including **39 new-policy cases**. The focused batch passed 39 and the broader batch passed 349; those overlapping totals are not added. Six unrelated existing build warnings remain; the new tests introduce no warning. The initial sandbox build could not read the user NuGet configuration; the same required runner succeeded with elevated filesystem access. No required check remains blocked.

A separate reconstruction audit matched complete generation and frozen shortlists for **13,632 saved evaluations and 48 feedback probes**, across the v13 rescreen, v14 feedback, v15 retention, v16 lineages and all nine v17 components. Gameplay assemblies and runtime identity matched. The audit and preparation executed zero fights.

The [preflight receipt](../TestResults/balance/tower-late-allocation-work-20260914/preflight.json) binds the producing executable, source, full content/settings/gameplay scope, all 94 controls and the 480,120-reservation ledger. Eight checks on a temporary prepared copy verified unchanged inputs and rejection of changed caps, retries, omitted controls, extra files and repeated starts. All prior packages remain preserved.

The implementation enables an experiment; it does not establish improved search reliability or promote a default. No game content, configuration, migration, catalog or deployment changed. Full producing source and test receipts are retained in [producing inputs](../TestResults/balance/tower-late-allocation-work-20260914/producing-inputs.json).
