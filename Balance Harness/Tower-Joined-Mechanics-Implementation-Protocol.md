# Joined mechanic groups: implementation and zero-combat verification

15 September 2026. Authorized follow-up to the saved composition-only trajectory diagnosis. Target only the offline BalanceHarness. No gameplay/content changes, ability-order search, seed allocation, preparation, combat, archive replay, historical run modification or cap increase.

## Frozen scope

Add an explicit `independent-joined-mechanics-v1` / `joined-mechanics-joint` policy. Inherit composition-only canonical ordinal Essence order, candidate/proposal budgets, scheduling, fitness, selection and attempt/cancellation behavior. Historical policies keep their existing streams and output serialization when new optional trace fields are absent.

Join two content-derived mechanic groups when they share an Essence and their union adds to both groups. Canonicalize member sets, reject unavailable members, duplicate source families and unions larger than the character budget or five Essences. Consider at most 128 distinct base recipes (at most 8,128 pairs); retain at most 256 distinct unions. Stable ordinal/hash ordering resolves duplicate paths and truncation. No recursive closure, recipe imports, control outcomes, names or item-specific weights. Record bounds and truncation.

On the fresh guided route, retain coverage reservations, attempt a compatible joined group on the sampled owners, then fill spare slots with existing construction. Preserve the one-in-eight uniform route and existing small-core fallback when no joined group fits. Trace provider choices and joined-group selection, insertion or rejection; rejected insertions are atomic. Existing mutation/loadout operators can reuse assembled groups without changing ability order.

## Exact diagnostic workload

Zero fights, combat replays, preparations and new balance seeds. Synthetic fixture constants (generation 17, synthetic measurement labels 101/102) have no combat engine or allocator behind them. Each test installs an engine-entry guard. Freeze scripts/source/test hashes before execution.

1. One pre-change reference executable call: run the existing composition-only synthetic fixture with 128 candidates, two owners, 12 eligible Essences, 2,048 proposals, both empty metadata and a fixed overlapping-core/coverage fixture. Save full hashes and synthetic counts. Use the sealed prior executable and identical fixture for the candidate parity checks.
2. One backend test invocation through `build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarnessCompositionSearchTests|FullyQualifiedName~BalanceHarnessJoinedMechanicsTests' -ArtifactsPath <new-package>/tests`. Retain the existing 17 composition tests. New tests cover canonical/deduplicated joins, disjoint/subset/illegal/ownership rejection, bounds and input-order invariance, atomic insertion, guided and uniform routes, legal complete synthetic search, order exclusion, trace roundtrip/checkpoint/cancellation, proposal exhaustion, policy boundary checks and both frozen composition-only hashes. Exact test source freezes before execution.
3. One captured-content catalogue audit: read the sealed pilot's input-only generation mechanics and discovery inputs, enumerate joins once, independently enumerate all legal pair unions and compare, and verify the previously identified Howler/Spiderling/Royal Venom triple is structurally reachable. No teams generated or outcomes read in this audit. Report catalogue counts/timing/truncation, not combat strength.
4. Verify all indexed pilot preparation/study/execution and trajectory files before and after; retain checkout source hashes and scoped diff checks. Seal the new evidence separately.

Maximum additional diagnostic workload 300 seconds, bounded by the original cumulative 1,800 seconds (258.482 previously used); maximum new output 512 MiB, within the original 4 GiB. Zero diagnostic retries. Compilation is engineering work, separately measured with per-command 300-second limits. A diagnostic failure or limit stops dependent diagnostics and remains preserved; do not change a test expectation to hide it. No larger or unchanged combat batch is authorized by this implementation.

## Build isolation and completion

Compile copied harness source against the existing captured dependency DLLs; compile only the two zero-combat test classes and shared synthetic fixture. This preserves the gameplay snapshot and avoids building unrelated dirty application changes. The repository test wrapper runs the resulting isolated test assembly. Preserve source, compiler projects, dependency hashes, logs, TRX and command receipts. This is targeted verification, not a full backend suite or proof of search-quality improvement.

Update the active search plans and README with implementation status, exact commands, measured results and limitations. All 482,371 seed reservations remain retained. Historical reliability, v19 Unresolved and adoption Hold remain unchanged. No configuration, migration or deployment changes.
