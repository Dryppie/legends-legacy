# Affinity creation with endpoint preservation

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The opt-in preservation policy is implemented and its two-wave generation export is verified. All twelve frozen development roots still fill seventeen unique slots. Accepted edits that remove selected partners fall from 48 to zero, while the legal neighborhood contracts from 63 recipes to 41.** No combat benefit has been demonstrated, and no new fights, entropy draws, reservations or promotions occurred.

## Rule and contract

The [edit diagnosis](Tower-Affinity-Creation-Edit-Diagnosis.md) identified a development hypothesis, not a causal result. The implementation uses only selected authored affinity relationships. For each target pair and owner, it computes the missing additions, unions them with the parent's loadout, and protects every existing endpoint of a selected route whose two endpoints are now present. This also preserves selected routes already active before the addition. Existing generic interaction and explicit damage-affinity protection remain additional constraints.

Only missing endpoints are added; removals have the same cardinality and come from outside the protected set. Family uniqueness and global owned-copy limits still apply. Sampling is uniform over eligible pairs, followed by uniform sampling over that pair's legal minimal edits. Multiple authored routes for one pair do not increase its sampling weight. The implementation contains no Essence blacklist, owner preference or outcome-derived score.

`BenchmarkPreservingAffinityCreation(ids)` produces `tower-proposal-policy-v4`, named `benchmark-preserving-affinity-creation-v4`, with the required rule `preserve-completable-affinity-endpoints-v1`. The existing v3 preset remains unchanged. Earlier policies reject the new rule; v4 rejects a missing or unknown rule. Nullable additions to the JSON records are omitted for earlier contracts.

Accepted and duplicate-rejected constructed proposals record protected endpoints, eligible pair count, and the chosen pair's eligible edit count in `affinityCreation.removalSelection`. Coverage retains every pair/owner opportunity and its full legal edit list, including empty lists. Failed construction reports `no-legal-preserving-affinity-creation`; already-active and duplicate rejections keep their existing meanings. Protection is never relaxed to fill a batch.

## Exporting both waves without combat

`tower-proposal-export-v4` exports nine first-wave and eight second-wave candidates using one generator instance per arm, including continuing owner schedules and cross-wave duplicate exclusion. Both waves must fill for an arm to be `Complete`. The `secondWave` object contains its batch and coverage, and `teams` includes every reference and accepted candidate with empty scenario seed lists. Feedback counts and panels stay empty; these are generation records, not simulated racing observations.

This export rejects beam parent tickets and recombination, which consult measured beam feedback. Other supported operators can be exported when independent of that feedback. Exports v1–v3 retain their original first-wave-only behavior. New exports retain the existing 64-MiB per-bundle limit, 128 attempts per wave, no-overwrite publication and full deterministic reconstruction checks.

The retained [root-01 request](../TestResults/affinity-creation-preservation-preview-20260924/root-01/request.json) is a complete example with the frozen context, inventory, original v3 arm and new v4 arm. Use the existing commands:

```powershell
dotnet <BalanceHarness.dll> tower-proposal-policy-check <request.json>
dotnet <BalanceHarness.dll> tower-proposal-policy-export <request.json> <new-output>
dotnet <BalanceHarness.dll> tower-proposal-policy-verify <output> <files.json-sha256>
```

These commands accept explicit requests; the default preset command and gameplay settings are unchanged. The new policy is deliberately **export-only** at this stage. Existing racing v1–v5 contracts reject it before evaluator dispatch. A separately versioned comparison and admission remain necessary before measuring it with the existing benchmark-validation selector.

## Frozen development-root coverage

The [preview package](../TestResults/affinity-creation-preservation-preview-20260924/summary.json) uses the exact twelve root values, benchmark parent, selected three affinity routes, complete inventory and physical scope captured in the prior v5 pilot. It does not allocate fresh roots or read historical outcomes into the generation decision. Reusing these roots is a development replay, not new statistical validation.

| Generation fact, twelve roots | Original v3 | Preserving v4 |
| --- | ---: | ---: |
| Roots filling 9 + 8 slots | 12 | 12 |
| Accepted root/party occurrences | 204 | 204 |
| Attempts | 252 | 266 |
| Already-active rejections | 27 | 27 |
| Duplicate-recipe rejections | 21 | 35 |
| Accepted edits removing a completable selected partner | 48 | 0 |
| Distinct observed recipes across roots | 57 | 39 |
| Distinct legal recipes in the fixed parent neighborhood | 63 | 41 |

The old preview containing all four earlier arms reconstructs to its original contract hash. Every original v3 proposal, candidate, seen-before set and wave number in all twelve full trajectories matches the captured pilot. The new rule changes later choices and rejection patterns; the new pools are not just filtered copies of old accepted pools. Each v4 accepted edit was independently checked against the frozen selected endpoints and its exported eligibility counts, in addition to native export verification.

This demonstrates preservation and adequate fill on these development roots, with a real diversity cost. It cannot establish candidate quality, recognition quality or final-output improvement. The earlier recognition diagnostic remains `CompleteDiagnosticOnly`, its 132 unknown cells stay unknown, and the original v5 pilot remains `Inconclusive`.

## Changed files and verification

- [TowerAffinityCreation.cs](../LL/tools/BalanceHarness/TowerAffinityCreation.cs): per-target endpoint protection, explicit failure and reconstructed sampling metadata.
- [TowerProposalPolicies.cs](../LL/tools/BalanceHarness/TowerProposalPolicies.cs): explicit v4 policy/export contracts, feedback-independent two-wave export and racing exclusion.
- [TowerAdaptiveRacingGenerator.cs](../LL/tools/BalanceHarness/TowerAdaptiveRacingGenerator.cs): opt-in dispatch while retaining the old streams.
- [BalanceHarnessAffinityPreservationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityPreservationTests.cs): 22 new combat-free regression cases.
- This report, nine current-status lines, and retained engineering verification/export receipts. Historical report bodies and scientific source archives are preserved.

**168 distinct relevant tests passed:** an initial 167-case run, followed by the final 22-case preservation suite including one added exhaustion case. The shared 21 cases passed in both runs. Tests cover minimal edits, same-owner scope, active/incomplete routes, composed protection, family/copy legality, pair sampling, deterministic exports, version rejection, cancellation, rehashed tampering and bounded exhaustion. The fifteen-recipe fixture fills nine first-wave slots, then honestly reports only six second-wave slots and duplicate rejections. Existing policy, affinity, adaptive-racing, tie-selector and benchmark-validation fixtures retain their results and golden hashes. Guards reject native preparation or combat in these suites.

Tests ran through `build/run-tests.ps1` with a separate `-ArtifactsPath TestResults/affinity-creation-preservation-build-20260924`. The broad filter selected `BalanceHarnessAffinityPreservationTests`, `BalanceHarnessAffinityCreationTests`, `BalanceHarnessProposalPolicyTests`, `BalanceHarnessDamageAffinityTests`, `BalanceHarnessAdaptiveRacingTests`, `BalanceHarnessBenchmarkValidationTests` and `BalanceHarnessBenchmarkTieSelectionTests`; the final filter selected the preservation suite alone. [TRX receipts and logs](../TestResults/affinity-creation-preservation-verification-20260924/closeout.json) retain the exact results.

The first sandboxed build could not read the user's NuGet configuration. An authorized retry completed; no required verification remains blocked. Build warnings were in pre-existing files. All twelve new bundles passed native reconstruction, and their files and consumed source pins passed final hash verification. The old publication's 28 handoff members were authenticated before edits; 41 historical pins and nine historical document bodies were checked afterward.

## Accounting and next step

The bounded engineering preview declared **600 seconds /512 MiB** before running, charging that full maximum on start including failure. It completed once in **59.094 seconds**, retaining **261,322,720 bytes**. Recorded cumulative work is **33,534.953 seconds /27,153,095,523 bytes**; declared cumulative maxima are **76,980 seconds /48,469,377,024 bytes**. Builds, unit tests, source backups and documentation are separate engineering work, not combat allowance. No live-history rescan was needed; the last complete scan remains 743,892 values across 256 files.

The single next implementation step is a separately versioned, matched-budget **v3 versus preserving-v4 proposer comparison** using the same frozen affinity inventory and benchmark parent, with racing and benchmark-validation selection held fixed. It must compare complete search outputs on fresh independent evaluation, account for incomplete generation honestly, and retain references. Freeze its design and resource limits before admission; do not reuse recognition outcomes as validation for this rule. No comparison plan, admission, seed allocation or combat execution for the new policy has been created here.

Preview manifest SHA-256: `7b001519484ecac83fd6c3ce64085bd63cb56c595e56fa3eda5fe04427e85023`. No migrations, application configuration changes, deployments, gameplay edits or default-policy changes are involved. Unrelated working-tree changes were preserved.
