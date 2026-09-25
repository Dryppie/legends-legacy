# Practical search with three retained references

**Confirmation verification and accounting — 23 September 2026:** The [complete verifier improvement and prospective resource amendment](Tower-Practical-Three-Reference-Confirmation-Resource-Amendment.md) are ready. Full input verification measured 55.94 seconds with the original helper and 18.41 seconds with the new helper; all 30 relevant tests pass. The proposed 8,400-second /4.5-GiB cumulative allowance includes the failed admission and one new admission. Next is explicit v2 accounting implementation. No new admission, preparation, reservation or combat ran.

**Three-reference confirmation design frozen — 23 September 2026:** The [prospective plan](Tower-Practical-Three-Reference-Confirmation-Plan.md) fixes five exact challengers plus all three retained references, **6,500 common values per team /52,000 fights**, fifteen contrasts and family 38. The proposed study allowance is **7,800 seconds /4 GiB**, including admission and both audits. **Implementation is complete; the first admission failed before preparation. See the admission closeout above.** No new fights or values were allocated; defaults, recommendations and **633,313 exclusions across 240 files** remain unchanged.

**Execution completed — 22 September 2026:** The [single admitted search and both audits](Tower-Practical-Three-Reference-Execution.md) completed **3,528 fights** with `IncumbentRetained`: reference `96b94357…` won **762/1,000**, versus **700 /624** for the original controls. All three remain recommended. The run closed within its **1,200-second /1,536-MiB** remaining allowance, and history now contains **551,408 exclusions across 232 files**. The [native admission and reconciled closeout](Tower-Practical-Three-Reference-Admission.md) remain preserved. This one root does not establish general search improvement.

22 September 2026. Target: `LL/tools/BalanceHarness` and its backend tests. The separately versioned search contract and reuse-preset command are implemented. They support the confirmed `96b94357…` recipe alongside both original controls, without replacing either control. Implementation fixtures allocated no authoritative values; the separate real execution is linked above.

## Contract and decision

| Contract | Two-reference version, preserved | New three-reference version |
| --- | --- | --- |
| Generation policy | `retained-composition-incumbents-v1` | `retained-composition-three-references-v1` |
| Declared-schedule request and result | `tower-practical-search-v1` | `tower-practical-three-reference-search-v1` |
| Owned-allocation request | `tower-practical-allocated-search-v1` | `tower-practical-three-reference-allocated-search-v1` |
| Supplied references | Exactly two | Exactly three |
| Selection nominees | Both references and two challengers | All three references and two challengers |
| Confirmation recipes | Two or three distinct recipes | Three or four distinct recipes |
| Practical interval family | Seven quantities | Ten quantities |

All three supplied teams are evaluated during discovery and retained for selection even when discovery ranks them below generated teams. The existing retained-composition mutation operators operate over the three supplied starts; the two best eligible challengers fill the remaining nomination slots. This changes the search inputs and nomination contract, and makes no empirical claim of better search quality. It remains one construction root, one equipment context, one selected output and fixed canonical Essence order. Diagnostics and replays are unavailable.

Selection keeps the declared existing policy. An optional `tower-staged-incumbent-tie-v1` designation may name any one of the three exact reference IDs. The converter preserves an existing designation; importing the confirmed team does not designate it as the primary or change a default. With no explicit designation, the existing zero-win-aware selector applies.

Confirmation freezes the selected output and every reference before measurement. If a reference itself is selected, it is measured once and carries both identities. A generated challenger qualifies only when its adjusted win-rate lower bound supports at least 10% viability, its observed gain is at least five percentage points against **each** reference, and all three adjusted paired lower bounds are positive. Passing only the two historical controls cannot establish improvement over the confirmed team.

The approximate Bonferroni-Wilson family stays fixed at ten: at most four recipe rates plus the gained-win and lost-win probabilities for each of three paired comparisons. It remains ten when confirmation deduplicates a selected reference. The new result includes every measured recipe rate, and Markdown reports their intervals. Legacy result JSON omits the new nullable `rates` field, preserving its existing serialization and family-seven calculation.

On demonstrated improvement, the generated candidate is recommended. Otherwise all three supplied recipes remain recommended. All reference recipes remain in the seed-free export in either case. Encounter balance and general search reliability remain separate decisions.

## Prospective cost

For `N` discovery candidates and `d`, `s`, `c` samples per discovery, selection and confirmation recipe, the maximum fight count is:

```text
N × d + 5 × s + 4 × c
```

A selected reference reduces actual confirmation by `c` fights; those unused fights do not extend another stage. Sample counts remain shared panels, so adding a reference does not itself require an extra panel of random values.

| Example | Discovery | Selection | Maximum confirmation | Total |
| --- | ---: | ---: | ---: | ---: |
| 64 candidates; 8 /32 /256 samples | 512 | 160 | 1,024 | 1,696 |
| 46 candidates; 8 /32 /1,000 samples | 368 | 160 | 4,000 | 4,528 |

These are cost examples, not execution allowances. In particular, the completed two-reference preset's 3,496-fight allowance does not fund the second example. Confirmation still requires 256–1,000 predeclared samples; the minimum is not a power guarantee for detecting a five-point gain.

## Import the verified reuse bundle

The [confirmed-team reuse package](Tower-Confirmed-Team-Reuse.md) contains the exact selected recipe and both seed-free controls. Its `files.json` SHA-256 is:

```text
84177584cafed74cdde9d6f4b51b986dda040affd410f47e799b2eabe882eb33
```

Start from a **newly declared** unscheduled two-reference incumbent request with the intended current history, runtime/content/settings bindings, sample counts, output, prior charges and sufficient fight/time/storage limits. The source must use `tower-practical-allocated-search-v1` and `retained-composition-incumbents-v1`. The command shape is:

```text
dotnet BalanceHarness.dll tower-practical-search-three-reference-preset <new-source-request.json> <reuse-bundle-directory> <reuse-manifest-sha256> <new-preset-directory>
```

The command authenticates the complete caller-pinned bundle, requires its successful reuse receipt, checks exact control scenarios and selected qualifying identity, and compares content and effective settings with the intended template. It appends reference `confirmed-96b943571150684df3a5be53` for this bundle, preserves the two original reference IDs, sets the new generation/request versions and five nominees, and validates the resulting cost against the caller's existing limits. It rejects a changed source or bundle, missing or altered controls, an incompatible candidate, insufficient fight capacity, and an existing output.

The new directory contains `template.json`, `request.json` and a last-written `preset.json` receipt with `PreparedNeedsAdmission`, `admissionRequired=true`, `fights=0` and `newValues=0`. Partial output without that receipt is not a successful preset. No values are derived and no native parties are prepared by this command. The source files, ownership limits, pool, actor identities, selection policy, samples, allocation master/domain and resource limits are preserved.

Historical preparation establishes the reuse bundle's captured context. It does not establish that a newly built harness has that execution identity or that historical fitness transfers to changed gameplay. The preset does not rebind execution identity. Before a future run, capture and bind the intended runtime, refresh the full live history, and use:

```text
dotnet BalanceHarness.dll tower-practical-search-allocation-check <new-preset-directory>/request.json
```

Native admission checks the intended runtime, recipes and full history without allocating values or running combat. Account for its costs before any separately scoped `tower-practical-search-allocate-run`. Existing `check|run` also support the new declared-schedule version, and `verify` reconstructs the new result. Request and generation versions must agree throughout admission, allocation, publication and recovery.

The new allocation version is part of the deterministic derivation namespace. Journal reconstruction and abandoned-allocation recovery use the saved source version, while retaining the existing recovery receipt protocols. Pending derivations cannot be resumed; the recorded values remain excluded. Legacy allocation values, archive reconstruction, comparison contracts and defaults keep their existing versions.

## Verification and changed files

The [verification receipt](../TestResults/three-reference-implementation-verification-20260922/verification.json) retains focused and regression test results, final source/binary pins and unchanged historical pins. All **32 final focused cases** passed. The new tests use literal combat outcomes, temporary histories and a combat-entry guard. They cover protected nomination, all-three improvement gates, selected-reference deduplication, cost limits, explicit tie designations, version mismatch rejection, deterministic allocation reconstruction, interrupted allocation recovery, worker publication/archive verification, the public preset command and invalid reuse bundles.

Backend verification uses `build/run-tests.ps1`. The initial regression build is in `TestResults/three-reference-build-20260922`; the final build and focused checks use `TestResults/three-reference-final-build-20260922`. The final build succeeded with **38 warnings outside changed files** and no errors. The broader suite finished **552 passed /1 failed /0 skipped** in 18 minutes 22 seconds, including the legacy 44,000-report synthetic archive fixture. Its one failure was a diagnostic phase timeout; both cases in that method subsequently passed a focused recheck with unchanged limits. The receipt retains the original failure and exact commands. The source/history pin audit, CLI help and scoped Markdown/whitespace checks also passed. No required command remains blocked.

No real search is part of these fixtures. Engineering builds, tests and retained output remain separately disclosed under the accepted accounting treatment; no complete historical engineering total is inferred.

Changes comprise the new `TowerPracticalThreeReference.cs` adapter; generation, definition validation and selection safeguards; practical results, allocation, recovery and archive handling; CLI help; `BalanceHarnessThreeReferenceTests.cs` and allocation-recovery tests; this guide and current pointers in the README, practical-search guide, reuse guide and state review. No gameplay rules, application configuration, migrations, database operations or deployment changes are included.

Native admission, the single bounded practical search and its saved-evidence diagnosis are complete in the follow-ups linked above. All prior values remain excluded in the 551,408-value union. The original 44,000-fight experiment remains closed with its historical NotAssessed balance; the practical execution reports a separate captured-cohort Fail. V19 reliability and the 162 current-family context exceptions remain unresolved. The next algorithm implementation scope is the opt-in exploration hypothesis defined in the coverage review, followed by a separately declared prospective comparison.
