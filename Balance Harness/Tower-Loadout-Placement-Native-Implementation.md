# Whole-loadout placement native implementation

Current status: **Native proposer and combat-free preview verified.** Policy v6, export v6 and racing v8 are implemented as explicit opt-ins. All 238 catalogue recipes match the independent design. Combat effectiveness remains unmeasured; no policy default changed.

## Behavior and design decisions

[TowerLoadoutPlacement](../LL/tools/BalanceHarness/TowerLoadoutPlacement.cs) implements the [reviewed design](Tower-Loadout-Placement-Design.md): move complete canonical Essence lists within exactly one production five-owner subgroup, preserving its complete-loadout multiset and Essence counts. It copies only Essence assignments, keeping actors, equipment, styles and identities at their destinations. It does not claim preserved combat value or support uptime.

The builder enumerates all 240 bijections including the two identities, validates legality and copy limits, excludes all three references, and deduplicates before sampling. Fewer than 17 distinct nonreference recipes rejects the profile before any evaluation panel or export directory is created. There is no fallback operator, larger search space or partial-wave success.

The catalogue hash binds the complete sorted catalogue, scope, parent, assignment accounting and derivations. Scope metadata includes the explicit generation seed and exclusions, so the complete catalogue hash varies by root while all 238 recipe records remain identical. A separately namespaced `StableRandom.Seed` stream uses that hash and the explicit root. One shuffle supplies nine candidates followed by eight without replacement. Outcomes and beam membership are recorded as existing racing context but never alter these candidate choices. Provenance records the catalogue hash, draw ordinal, subgroup and source-to-destination assignment.

[TowerProposalPolicies](../LL/tools/BalanceHarness/TowerProposalPolicies.cs) exposes `BenchmarkLoadoutPlacement()` and validates exact v6/v6/v8 pairings. Mixed generation exports still require two to four explicit arms. The existing preset command and defaults remain unchanged. New catalogue and proposal metadata are nullable and omitted from older versions. Earlier racing versions retain their original pre-cancelled receipts.

The new export's physical scenarios preserve the benchmark reference's seed-free envelope and exactly match the independently frozen design's scenario content. The historical Python and native SHA-256 hashes use different JSON escaping; both are verified from the same exact scenario and retained in the [238-entry hash bindings](../TestResults/loadout-placement-native-preview-20260924-v4/physical-hash-bindings.json). Native encoding escapes the timestamp's `+` as `\u002B`. Neither historical evidence nor the native hash format was changed. Racing continues to use the existing scope-based scenario envelope and panel seeds; no selection or evaluation kernel was replaced. The unchanged benchmark-validation selector retains its 16-sample nomination, 60-sample validation and total 528 evaluator requests. Those requests were exercised with literal test outcomes only.

## Captured native preview

The [sealed preview](../TestResults/loadout-placement-native-preview-20260924-v4/files.json) used all twelve already exposed development roots from the earlier allied-action preview. It retained the compiled runtime, producing source, requests, complete catalogues, both waves, command intents and command results. Its input aligns only the benchmark reference's diagnostic scenario ID with the later design anchor; every other scenario field must already match. No allocator, live gameplay-content loading, battle preparation or scientific campaign was invoked.

| Verification | Result |
| --- | ---: |
| Distinct catalogue recipes per root | 238 |
| Recipes per subgroup | 119 / 119 |
| Complete 9+8 roots | 12 / 12 |
| Accepted proposals across roots | 204 |
| Distinct sampled recipes across these roots | 142 |
| Earlier full policy arms with identical serialized bytes | 24 / 24 |
| Native export/reconstruction commands, all successful | 25 |
| New fights / reserved values / entropy draws | 0 / 0 / 0 |

Every native catalogue identity, owner build, changed-owner list, edit distance and assignment derivation matches the saved Python catalogue. Independent checks bind both physical hash encodings to each exact scenario and compare every sampled physical scenario and draw record. Native verification reconstructs each of the twelve new exports; one complete historical export is additionally reconstructed using the new binary. The existing v4 and v5 arms remain byte-for-byte identical in all twelve roots. All 25 commands in the successful attempt ran once without a command retry.

These are structural and compatibility results. The sampled-recipe count describes coverage on known development roots, not effective diversity, a success rate or fresh performance evidence. The retired insertion-neighborhood diagnostic remains retired unresolved, with no candidate eligible for confirmation.

## Tests and execution

The final backend run passed **350 tests**, including 30 new placement cases and 320 related regression cases. The new cases cover an independent Cartesian-bijection oracle, bundle and inventory conservation, physical actor preservation, duplicate-loadout weighting, reference exclusion, tight copy budgets, atomic cycles, illegal assignments, profile mismatches, underfill before dispatch, cancellation, deterministic replay, literal 528-request selection, and rehashed export/provenance tampering. Fifteen independent Python verifier tests also passed, including a captured native hash golden value and rejection of changed anchors or unsupported encodings.

Backend tests ran through `build/run-tests.ps1`, using isolated artifacts at `.artifacts/loadout-placement-native-20260924`. The full [test command](../TestResults/loadout-placement-native-implementation-20260924/backend-test-command.json), [final output](../TestResults/loadout-placement-native-implementation-20260924/tests-final.log) and [TRX results](../TestResults/loadout-placement-native-implementation-20260924/tests-final.trx) are retained. Build warnings are in existing unrelated files; there are no compilation errors.

The first build was blocked by sandbox access to the user NuGet configuration and succeeded with the required filesystem access. The first new-fixture test run exposed a fixture-only mismatch: five Essence slots retained the four-slot level/rank/floor budget. The corrected fixture uses the legal five-slot progression and passed. Both initial logs are retained. No required command remains blocked.

Two technical preview attempts stopped after the first native export: the [first retained failure](../TestResults/loadout-placement-native-preview-20260924/failure.json) detected the old reference scenario label; the [second retained failure](../TestResults/loadout-placement-native-preview-20260924-v2/failure.json) detected the Python/native JSON hash encoding difference. The third attempt completed all 25 native commands but its final summary guard incorrectly required identical full catalogue hashes. Those hashes bind per-root seed metadata, so they correctly differ. The corrected guard verifies every scope binding and the identical recipe records separately; it passed all twelve sealed roots before the fourth attempt. The [third failure](../TestResults/loadout-placement-native-preview-20260924-v3/failure.json) also remains sealed, immutable and fully charged. The native proposer and its compiled binary were unchanged throughout. There was no scientific retry or new root allocation.

Completed commands included:

```powershell
./build/run-tests.ps1 -Filter <retained scoped filter> -ArtifactsPath .artifacts/loadout-placement-native-20260924
python -B -X utf8 "Balance Harness/analysis/test-preview-loadout-placement.py"
python -B -X utf8 "Balance Harness/analysis/preview-loadout-placement.py" --harness .artifacts/loadout-placement-native-20260924/bin/BalanceHarness/release/BalanceHarness.dll --output TestResults/loadout-placement-native-preview-20260924-v4
```

The Python launcher is a single-use bounded preview, not a scientific launch script. Each of the four attempts fully charged its 600-second / 536,870,912-byte allowance at start, including failure: **2,400 seconds / 2,147,483,648 bytes** in total. The successful attempt's measured time before sealing was **79.765 seconds**; its sealed preview retains **411,020,446 bytes**. Builds, synthetic tests and publication checks are separate engineering work.

Cumulative recorded charges are **53659.661 seconds / 38,492,029,246 bytes**. Cumulative declared maxima are **142,560 seconds / 93,633,642,496 bytes**. Prior costs remain intact. The last verified history remains 799,177 values across 264 files; this step did not rescan or change that live registry.

## Changed files and next step

Added the native catalogue builder and [30-case backend fixture](../LL/tests/EssenceSystem.Tests/BalanceHarnessLoadoutPlacementTests.cs). Extended the proposal records, adaptive generator dispatch and policy/export/racing validation. Added the [bounded preview/verifier](analysis/preview-loadout-placement.py), [its tests](analysis/test-preview-loadout-placement.py), this report and retained evidence. Two LF rules preserve the new Python helpers. Nine current-status documents change only line three, preserving their historical bodies and all 181 inherited immutable pins.

No gameplay service, Core domain rule, application configuration, migration or deployment changed. The opt-in native proposer is implemented; a fresh comparison study and its admission are not.

Next, freeze a bounded matched-budget comparison design for this proposer against the existing v5 allied-action policy with the same selector, retaining the fixed benchmark as an absolute comparator. Define the fresh independent roots, held-out panel, decision rules and resource admission before scientific execution. This preview does not authorize promotion or qualify a team. All previously exposed values, including unused permanent reservations, remain excluded from future fresh allocation.
