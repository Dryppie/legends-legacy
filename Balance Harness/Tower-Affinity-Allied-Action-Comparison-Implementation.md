# Allied-action comparison: native implementation

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The frozen allied-action comparison is implemented and verified, but not admitted.** The [native plan](Tower-Affinity-Allied-Action-Comparison-Plan.json) exactly matches `plannedNativePlan` in the [unchanged frozen design](Tower-Affinity-Allied-Action-Comparison-Design.json). Its native hash is `714a88bf125c858e1bafc2e2ab04b95f949ca67a042a1c8854bcab2bccac8f82`. This plan retains the captured development execution identity; it cannot admit the newly built runtime without qualification and rebinding.

The control uses endpoint-preserving v4 under racing v6. The candidate uses allied-action-preserving v5 under the new `tower-proposal-racing-v7`. The new study identity is `tower-affinity-allied-action-comparison-v1`. Only that racing profile accepts v5; earlier contracts keep their restrictions and metadata. Production defaults are unchanged.

## Fixed behavior and implementation

Both arms retain the same benchmark parent, selected affinity IDs, 9+8 proposals, uniform eligible-pair then eligible-edit sampling, 128 attempts per wave, legal recipe checks and `tower-racing-benchmark-validation-v1` selector. The comparison reuses the existing paired allocation and decision rules: twelve fresh roots, 4,380 values, 528 charged search reports per arm/root, and a 21,888-fight ceiling. All 24 complete outputs must be frozen before any held-out observation. Incomplete work invalidates the study; no root replacement or refill is allowed.

The existing 109-value search block, shared nomination/validation panels, isolated 256-value held-out blocks, all-root weighting and method/benchmark thresholds remain fixed. The new plan factory checks that the complete allied-action-preserving benchmark neighborhood has at least 17 distinct legal edits before allocation. Both arms preflight before either executes, and shared physical requests must agree across arms.

`tower-proposal-resource-envelope-v2` is mandatory for the new study in the native request validator, launcher and independent auditor. It preserves the 10,800-second / 6-GiB total, with 9,000 seconds / 5,905,580,032 bytes for native work and 1,800 seconds / 536,870,912 bytes for audit/publication. Native underspend cannot enlarge the audit allowance. Earlier profiles retain their existing envelope behavior.

Native archive reconstruction regenerates proposals and checks exact eligible sets, provider reasons, effect-definition hashes, panel evidence and both validation gates. The Python auditor independently derives direct equipped providers from the captured ability/effect graph, checks complete applicable protection reasons, minimal edits, paired allocation, gate arithmetic, summaries, resource receipts and publication. Exact effect-definition hashing and RNG/eligible-set reconstruction remain complementary native-audit responsibilities; Python does not claim identical .NET floating-number serialization.

Changed source files:

- `TowerAlliedActionComparison.cs`, `TowerProposalComparison.cs`, `TowerBenchmarkValidationComparison.cs` and `Program.cs`: the new plan command, exact policy contrast, allocation/binding, feasibility and paired execution.
- `TowerProposalPolicies.cs`, `TowerProposalStudy.cs` and `TowerProposalStudyProtocol.cs`: explicit racing v7, both-arm diagnostics and mandatory v2 request envelope.
- `audit-proposal-affinity-study.py` and `run-proposal-affinity-study.py`: independent interpretation and owned launch of the new version.
- `BalanceHarnessAlliedActionComparisonTests.cs`, `BalanceHarnessProposalStudyTests.cs`, `test-proposal-affinity-study.py` and `test-proposal-affinity-study-owned.py`: native, independent, complete and failing synthetic fixtures.
- This report, the native plan, its `.gitattributes` rule, and line 3 of nine current-status documents. Their historical bodies remain byte-identical. The prior frozen design and its verifier remain unchanged.

## Verification

The required `build/run-tests.ps1` wrapper passed **125 backend tests**, including **22 new comparison cases**, with zero failures or skips. Build completed with zero errors and 44 existing warnings. The [retained TRX](../TestResults/affinity-allied-action-comparison-implementation-verification-20260924/backend.trx) covers the new comparison, prior preservation comparison, allied-action classification, policy contracts, benchmark validation and resource envelopes. Both full native comparison fixtures passed.

Python passed **67 tests**: 25 arithmetic/contract/owned-process cases, 17 new-version archive cases, five resource tests, and all 20 unchanged frozen-design tests. The current auditor also verified the externally pinned legacy published preservation archive. Resealed tampering tests reject changed provider reasons, exact native effect hashes, protection metadata, policy/version labels, panels, output freezes, gate results, summary arithmetic and discarded exposed tails.

The [complete owned fixture](../TestResults/tower-proposal-owned-fixture-allied-action-20260924/verification.json) passed native execution, native reconstruction, independent Python audit, publication and final native verification. It retained 17,280 literal reports: 12,672 search and 4,608 held-out. Both arms have six gate passes and six fallbacks. Some proposal positions differ, while these deliberately synthetic outcomes yield identical selected outputs. This is pipeline verification, not an efficacy result.

The [failure fixture](../TestResults/tower-proposal-owned-fixture-allied-action-failure-20260924/verification.json) retained one started attempt and all 16,384 exposed synthetic values, including the unused tail; 4,380 were selected. It stopped before audits and published no completed result. Both fixtures use separate test registries and literal entropy/content/outcomes. They report zero actual combat and zero production entropy draws.

Owned completion measured 244.985 seconds / 628,885,554 retained bytes; the failure fixture measured 7.297 seconds / 45,370,679 bytes. These concurrent engineering timings do not qualify the real campaign. Completion archive manifest: `317381ecc45142f9342d77d6d2bc9e227f9de77bbf7053a3faeefc4a47f1319b`; external closeout: `b4b480e943047eaae01ca10d0ddec666a03226e3c876860d6e8b72aafd7783b9`.

Commands used the bundled Python runtime with `-B -X utf8`:

```text
build/run-tests.ps1 -Configuration Release -ArtifactsPath TestResults/affinity-allied-action-comparison-build-20260924 -Filter 'FullyQualifiedName~BalanceHarnessAlliedActionComparisonTests|FullyQualifiedName~BalanceHarnessAlliedActionPreservationTests|FullyQualifiedName~BalanceHarnessAffinityPreservationComparisonTests|FullyQualifiedName~BalanceHarnessProposalPolicyTests|FullyQualifiedName~BalanceHarnessBenchmarkValidationTests|FullyQualifiedName~BalanceHarnessProposalResourceTests'
python build/test-proposal-affinity-study.py ArithmeticTests -v
python build/test-proposal-affinity-study.py --fixture TestResults/tower-allied-action-comparison-literal-20260924 ArchiveTests -v
python build/test-proposal-resource-envelope.py -v
python "Balance Harness/analysis/test-affinity-allied-action-comparison-design.py" -v
python build/test-proposal-affinity-study-owned.py --fixture-host TestResults/affinity-allied-action-comparison-build-20260924/bin/BalanceHarness.ProcessFixture/release/BalanceHarness.ProcessFixture.dll --source-fixture TestResults/tower-allied-action-comparison-literal-20260924 --output TestResults/tower-proposal-owned-fixture-allied-action-20260924 --resource-envelope tower-proposal-resource-envelope-v2
```

The failure command used another new output directory and `--mode attempt-failure`. The native plan command and checker passed, and both previously sealed v4/v5 generation previews reconstructed with the new binary. The initial sandbox build could not read NuGet configuration; the authorized retry succeeded. One plan-check wrapper invocation failed on unsupported Python arguments before any native dispatch; its sealed failure and full declared charge remain retained. No required command remains blocked.

## Accounting and next step

No real combat campaign, production entropy draw, new scientific reservation, admission or deployment occurred. Prior results remain `CompleteDiagnosticOnly` for recognition and `NoObservedOutputDifferentiation` for the source pilot. The last complete history audit remains 766,415 values across 260 files and was not rerun.

The separately bounded plan verification (including its failed wrapper invocation) recorded 7.395953 seconds / 10,815 bytes and charged 360 seconds / 128 MiB. Cumulative recorded work is **39835.925953 seconds / 31,216,003,784 bytes**; cumulative declared maxima are **103,920 seconds / 64,642,613,248 bytes**. Builds, literal fixture tests and documentation remain separate engineering verification.

Next, qualify the final captured runtime under the frozen 900-second / 1-GiB qualification bound, measure real-scenario resource feasibility, refresh complete history, rebind only execution identity and required history exclusions, and prepare a pinned admission package. Keep the frozen scientific rules and source inventory fixed. No fresh values or real campaign execution should precede that separate admission step.

There are no migrations, application configuration changes, gameplay-default changes or deployments. The offline request must explicitly select the new profile and v2 envelope.
