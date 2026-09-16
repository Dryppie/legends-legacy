# Discovery refinement: frozen implementation verification

16 September 2026. Offline `LL/tools/BalanceHarness` only. Implement opt-in `independent-discovery-refinement-v1`, method `discovery-refinement`. **Zero combat, runtime preparations, fresh balance seeds, retries or replays.** Preserve all 482,821 reservations, sealed experiments, dirty unrelated work, fixed ability order and adoption Hold.

## Policy and limits

Reuse the existing team-coverage constructor and its finite 560-recipe captured pool for fresh proposals. Do not modify either constructor or allocator comparator. The new policy remains bounded to one generation label, at most 16 evaluated teams and at most 16 total proposals, 10 characters, five Essence slots, 128 providers, 64 authored cores and existing construction/allocation limits. Defaults and every existing policy remain unchanged.

By zero-based **proposal** index: initial attempts 0–3 are fresh; subsequent indexes congruent to 3 modulo 4 are fresh; other indexes cycle loadout distribution, loadout refinement and whole-character replacement. A full 16-attempt run with completed initial proposals therefore has **seven fresh / nine refinement attempts**. If no legal parent has yet been evaluated, use a charged fresh attempt. Fresh requests advance the existing deterministic structural batch; they are not structural restarts. A rejected, duplicate or unavailable proposal consumes its attempt; there is no refill or cap increase. Fewer than 16 accepted evaluations yields an explicit incomplete result.

For each refinement select the highest-ranked completed measurement in the current arm using the existing fitness ordering, including the existing ID tie-break. Reuse `TowerLoadoutComposition.Library` (maximum 128 modules), `CoordinateLoadouts` and `Mutate`; do not introduce another mutation algorithm. Distribution may change loadout owner counts, refinement changes one/two Essence choices together across matching owners, and whole-character replacement can leave the original finite recipe pool. Parents/donors come only from completed current-arm discovery proposals. No reference/control input, confirmation result, separate feedback schedule or imported parent is accepted. Parent provenance and loadout library/use traces remain durable.

All proposals keep canonical ordinal Essence order. Existing family, copy, identity and slot checks remain authoritative; the new policy also rejects any edit that removes required collective role coverage. No silent repair. Existing checkpoint-before-evaluation, failure/cancellation and attempt accounting remain in the shared kernel. No ability-order mutation or combat-strength claim from structural coverage.

## Frozen diagnostics

Use isolated output under `TestResults/balance/tower-discovery-refinement-20260916`, captured gameplay dependencies from the existing isolated compiler package, and the exact saved `tower-core-portfolio-20260916` inputs/mechanics. Source editing, review and before-source snapshot precede the diagnostic freeze. No ordinary gameplay build or restore. Freeze all root/captured source, eight test classes, fixture inputs, protocol, scripts, dependencies, current ledger and previous fixture outputs before execution.

Run **95 backend tests** through `build/run-tests.ps1`: the prior 83 plus 12 focused tests covering policy bounds/metadata, repeat and reordered metadata parity, changed parents under reversed fabricated fitness, the seven/nine proposal schedule, canonical evaluated legality and completed ancestry, shared inventory after coordinated distribution, lost roles, missing-role attempt charging, duplicates within the fixed cap, invalid provenance/order, checkpointed failure/cancellation at the fifth attempt, and rejection of ability-order/separately sampled feedback input. All evaluators are fabricated and guarded against combat.

Run the seven earlier policy fixtures at 32 fabricated evaluations each, and captured legacy structural/diverse/team/filler/core-portfolio policies at 16 each. Compare their **complete saved outputs** with the sealed reference: twelve old policies. Then run the new policy four times, each with at most 16 fabricated evaluations and exactly 16 charged proposals: forward fitness, exact repeat, reversed metadata with forward fitness, reversed fitness. The forward metric assigns 5, 10, … health to successive accepted evaluations; reverse assigns 95, 90, …; all are fabricated defeats with zero survival. The same first four fresh teams must be retained; later parent choices and recipes must respond to reversed outcomes. Do not require sixteen successful evaluations or conceal rejection counts. Captured synthetic evaluations are bounded by **368**, independent of the finite focused test fixtures. No new balance values are allocated; fixture labels are not combat seeds.

A separate Python reader verifies all twelve old outputs, repeat/metadata parity, every new proposal's fresh/refinement schedule, the highest-ranked earlier parent, all donor ancestry/library hashes, distribution reconstruction, refinement target groups, bounded changed slots, role/family/canonical legality, duplicate/rejection accounting, fake measurement sequence, explicit stop status and reservation union. It does not reproduce the pseudorandom stream or claim an independent engine replay. Save every proposal, fitness, trace, checkpoint result, elapsed/CPU/allocation/working-set measurement and command outcome. Keep incomplete accepted-count results as a policy limitation, not a failed parity check, provided accounting and status agree.

## Resource accounting and stop conditions

Incoming cumulative diagnostic work **2,821.442 seconds**; remaining **178.558** under the approved **3,000-second** ceiling. This complete scope has an **80-second / 192 MiB** maximum and keeps cumulative output below **4 GiB**. Carry previous receipt bytes plus actual sealed diagnosis size and a 1 MiB shared-test allowance. Charge compilation, tests, captured synthetic evaluations, audits, preservation and publication plus one closure second. Source editing time is excluded. No deletion, output reuse or reset.

Non-publication phases stop at 65 aggregate seconds, reserving 15 for publication. Individual caps: freeze 20, build 25, test build 15, tests 15, captured 10, audit 10 seconds, each reduced by aggregate/cumulative remainder. Enforce child deadlines. First failure stops dependent execution, with partial artifacts and failure receipt preserved. No retry or replacement diagnostic. Publication checks all **68 predecessor packages** and unrelated checkout work unchanged and records the precise unresolved boundary if any.

Run once after reviewing source and freezing these exact checks:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-discovery-refinement-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

The test command is `build/run-tests.ps1 -NoBuild -ArtifactsPath "$work/tests" -Filter <the eight frozen fully-qualified class filters>`, captured verbatim in command receipts. Publish a measured review and update six active Markdown handoffs. No combat comparison or fresh-seed exception is requested until implementation verification succeeds and any incomplete search behavior is assessed.

No Kharad/content/configuration change, migration, deployment or 129,536-fight confirmation. V19 remains sealed with 253 required recipes and unused original 512 confirmation values, no confirmation, Unresolved; reliability Fail 1/3, deep recovery 0/3, adoption Hold.
