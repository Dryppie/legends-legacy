# Saved V3 generation, selection and refinement diagnosis

16 September 2026. Target: offline BalanceHarness analysis only. The completed recovered-history V3 comparison is sealed; this scope reads its evidence without running combat or changing its decisions.

## Frozen scope and limits

New evidence: `TestResults/balance/tower-refinement-v3-trajectory-20260916`. Start with the execution seal `8682fe1243d54a46f3d97bdaa28fcdfc1e3113340dbc56b9c088da410b95c0c8`, its exact study inventory, and cumulative diagnostic time 3,711.0322723556355 seconds. Reconcile actual readiness, study and execution bytes from that receipt. Existing ceilings remain **4,080 seconds / 4 GiB + 192 MiB**.

This diagnosis allows **60 additional diagnostic seconds / 4 MiB output**, including setup, input verification, reader fixtures, calculations, independent checks, failures and publication. Source editing is excluded. Charge a conservative five seconds for schema/source inspection and initial setup. Zero fresh values, generated proposals, preparations, fights, replays, retries or full registry scans. Stop at the first failed diagnostic assertion; preserve evidence and publish the limitation.

Freeze scripts, exact input inventory, relevant producing-source hashes and this protocol before execution. Phases: setup at most five seconds (in addition to the five-second inspection charge); verification and analysis at most 40 seconds; publication at most ten seconds. All phases share the 60-second ceiling. Only the new evidence directory and active Markdown may change. Do not modify harness, gameplay, sealed packages or reservations.

## Exact checks

1. Verify the sealed execution and complete study inventory, source hashes and unchanged dirty checkout. Read all **288 existing combat records**: 64 baseline discovery, 64 V3 discovery, 32 selection and 128 confirmation. Validate seeds, outcomes, archive receipts and candidate mappings. Recompute each of the 32 discovery measurements from its four raw records, including win rate, boss health, survival and victory duration. Compare arithmetic means against an independent sum/count calculation.
2. Reconstruct the exact discovery rank (wins descending, boss health ascending, survival descending, victory duration ascending, ID), the top-two nominations per arm, and the finalist rule (selection wins descending, original discovery rank, ID). Compare selection boss-health ordering descriptively. Do not retroactively select another winner using confirmation. Unselected teams have no confirmation result.
3. Trace all 16 V3 proposals in order. Identify fresh versus refinement operators; verify each parent was the best completed discovery candidate at that point. Reconstruct recorded loadout libraries and validate donor provenance, library hashes and actual changes. For every edit, report changed slots/Essences, paired parent-child discovery results on the same four seeds, whether it improved the current best, and resulting parent changes. Count duplicate/rejected/no-op proposals and whether novelty checks exhausted their bounds.
4. Compare initial candidates, all fresh candidates, refinement children and final discovery leaders. Report search progress, parent concentration, operator outcomes, breadth and actual budget use. Any association with an Essence is descriptive: simultaneous edits and four reused discovery seeds cannot isolate an Essence's causal value or establish general strength.

Run twelve frozen pure-reader fixtures before analyzing results: complete-record aggregation; empty-record rejection; duplicate-seed rejection; win-rate priority; boss-health tie break; survival tie break; victory-duration tie break; ordinal-ID tie break; selection win priority; selection discovery-rank tie break; one-slot recipe difference; and multi-slot recipe difference. These run no backend or game code. Reuse the existing 45 passing backend tests from `build/run-tests.ps1`; no backend changes justify repeating them.

Persist machine-readable per-candidate and per-edit results plus a readable report addressing all three questions. Distinguish implementation correctness, observed search weakness and untested explanations. Recommend the smallest evidence-supported follow-up; this scope does not implement or run a changed policy.

All **482,956 reservations** remain reserved, including the old failed V3 allocation's 45 values, the earlier failure's 40, and V19's separate 512 unused values and 253 recipes. Fixed ability order, V19 reliability Unresolved, later Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged. No configuration, migration or deployment changes.
