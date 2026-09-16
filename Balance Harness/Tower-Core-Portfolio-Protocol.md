# Authored core portfolio: implementation and zero-combat verification

16 September 2026. Offline `LL/tools/BalanceHarness` only. Implement the bounded allocation change following the [saved selection diagnosis](Tower-Filler-Selection-Diagnosis-Review.md), preserving the dirty checkout. Freeze all diagnostics before running them. This scope has zero fights, fresh balance seeds, runtime preparations and retries.

## Exact opt-in implementation

Add `independent-team-core-portfolio-v1` / `team-core-portfolio` / `fresh-team-core-portfolio`, retaining all existing policies/defaults. Reuse the **identical filler-diversity recipe pool** and construction schedule, including 256 states/16 recorded recipes per core. Change allocation traversal only through a new `AllocateCorePortfolio` entry point; existing allocator entry points keep their exact comparator and serialization.

Each complete recipe's profile is SHA-256 of the ordinal array of every authored core ID whose Essence IDs are contained in it. No core is synthesized and no control or combat outcome enters this profile. Empty containment is one explicit empty profile. Validate at most 64 unique, nonempty core IDs with nonempty, known, family-compatible Essence sets that fit the loadout; snapshot and sort metadata. Profiles do not certify synergy or strength.

For each compatible candidate recipe at an allocation step, sort by these keys in order:

1. Descending newly covered required roles **only when remaining character slots are no more than the number of missing roles**; otherwise zero. This is an ordering heuristic, never a feasibility waiver or pruning proof.
2. Ascending profile exposure across previously completed teams, counting each character placement.
3. Ascending current-team profile uses on variety teams (indices 0,2,...); descending on reuse teams (1,3,...).
4. Ordinal profile hash.
5. Ascending full-recipe exposure across completed teams.
6. Ascending/descending current-team full-recipe uses with the same parity schedule.
7. Descending newly covered roles, then ordinal recipe hash.

Do not rank by new Essence IDs in this new path. Profiles/recipe exposure update only upon retaining a complete distinct team. Temporary use counters unwind on backtracking. Terminal role coverage, family legality, shared-copy limits, distinct/repetition constraints and canonical equipped ability order remain authoritative. Traversal, durable proposal/checkpoint charging, cancellation and incomplete-result semantics remain. No new combat score or extra evaluation budget.

Keep **256 allocation states / 250,000 candidate checks / 16 retained teams**, one generation label and at most 16 proposals/evaluations. Generation labels are not independent structural restarts. The new policy reuses the existing trace schema with its own version; no default promotion, gameplay/configuration edit or old experiment modification.

## Frozen correctness checks

New package: `TestResults/balance/tower-core-portfolio-20260916`. Record before/after source, checkout hashes, exact scripts/protocol and captured gameplay DLLs. Use isolated builds against the same sealed assemblies/content. Preserve all 63 predecessor packages before and after diagnostics.

Run **83 backend tests** through `build/run-tests.ps1`: the previous 71 constructor/allocator/structural/team/filler tests plus 12 new portfolio tests. New coverage: exhaustive 243 small role/copy/party cases against independently enumerated feasible assignments; profile exposure; specialized owners/shared inventory; state/check/party caps and cancellation; metadata/input immutability; invalid cores/bounds; empty pools/profiles; registration/metadata gates; canonical generated roles/provenance; missing-role attempt charging; checkpointed failure/cancellation; generation-label independence. Every evaluator is fabricated and combat entry is guarded. No broad combat integration filter.

Replay the seven old-policy fixtures (224 synthetic evaluations), then captured legacy/diverse/team/filler outputs (16 each), new portfolio (16) and metadata-reversed portfolio (16): **320 synthetic evaluations, zero fights**. Require exact full-output parity for **all eleven existing policies**, and exact new-policy metadata-order parity. Each evaluation returns the same fabricated zero-win/100-health score; attempt checkpoints are checked before evaluation. Require 16 completed captured proposals; a failure is retained without retuning or retry.

Run one additional new allocator call over the sealed 768-recipe pool to retain the complete native allocation result, without generating another pool or invoking combat. The independent Python audit derives profiles from raw authored cores and verifies every captured choice from its observed prefix using the frozen comparator, exposure updates and urgency condition. Require complete legal coverage and exact placement/party identities, proposal traces and state/check counts; any discrepancy stops the audit. Exhaustive unit cases cover general constrained/backtracking behavior; observed-prefix reconstruction covers the captured unrestricted case only.

## Fixed measurements and interpretation

Measure old filler versus new portfolio on the unchanged pool: distinct full recipes/profiles/cores and same-owner core pairs represented; first-slot recipe and role counts; per-team repetition/profile counts; all 48 core and 80 provider placement frequencies; all profile exposure counts; allocation counters and saved output identities. Preserve full data, not only improvements. A profile is the complete contained-core set; a core pair is an unordered pair simultaneously present within a character's recipe. Pool availability and generated exposure have separate denominators.

After generation only, measure all 191 control subsets from the diagnosis, full control recipe presence and the predeclared most frequent size-two/three/four examples. Saved controls never affect traversal or the success condition. Exact control recipes remain outside this unchanged pool. There is no requirement to reproduce a control or infer its strength from overlap.

Retain native per-case wall time/allocated bytes, total wall/CPU/peak memory and existing trace timings. Use one predeclared pass; do not rerun for a favorable timing. Construction/coverage changes cannot establish combat benefit or whole-campaign performance. Record regressions and incomplete checks explicitly.

## Limits and closure

Start from **2,554.347 cumulative diagnostic seconds**, leaving **145.653 seconds** under the approved **2,700-second** ceiling. This complete scope has an **80-second** maximum including preservation, compilation, tests, captured diagnostics, independent audit and publication. Non-publication phases stop by 65 seconds, reserving 15 for closure. Phase maxima: freeze 20, build 25, test-build 15, tests 15, captured 10, audit 10 seconds, each reduced by the aggregate/cumulative remainder. Engineering edits are not diagnostic execution. Create-new start/result/failure receipts; first failure stops all dependent diagnostics and permits closure only. No retries, reruns or replacement runs under this protocol.

Retained output ≤**192 MiB**, also subject to the unchanged cumulative **4 GiB** cap. Carry preceding receipt bytes plus actual sealed diagnosis size forward; include a 1 MiB shared-test allowance and one closure second. Never delete or modify sealed evidence. Preserve all **482,776 reservations**, including 512 original unused confirmation values; no new live registry scan or allocation. The latest 45-value combat approval is exhausted.

Run once: engineering `setup.py`, source/script preparation, then frozen `workflow.py freeze`, `build`, `test-build`, `tests`, `captured`, `audit`, `publish.py`. Publish measured effects, tests/parity, exact commands, actual remaining time/output, limitations and active Markdown. Preserve unrelated dirty files; changes are restricted to new policy, allocator opt-in path, structural adapter, five registration files, focused tests and documentation.

V19 remains sealed with 253 required recipes and no confirmation, reliability Unresolved. Current reliability Fail 1/3, deep recovery 0/3, adoption Hold. No ability-order tuning, Kharad tuning, gameplay change, migration, deployment, cap increase or 129,536-fight confirmation.
