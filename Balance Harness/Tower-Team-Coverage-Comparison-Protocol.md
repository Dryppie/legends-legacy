# Team coverage versus structural diversity: frozen comparison

15 September 2026. Offline BalanceHarness work only. Preparation is authorized; allocation and combat require a new, specific exception for 45 fresh seed values. Previous seed approvals are exhausted. This protocol is frozen before any diagnostic execution. No additional time is requested.

## Question and fixed inputs

Does team-level role coverage with specialist loadouts and permitted repetition outperform the previous structural diversity policy at the same small search budget?

- Baseline: `independent-joint-structural-diverse-v1`, method `joint-structural-diverse`.
- Candidate: `independent-team-coverage-v1`, method `team-coverage`.
- Each constructs and evaluates 16 teams, at most 16 proposals, with fixed ordinal ability order. Both constructors are deterministic and independent of their generation label; the label is not an independent structural restart. No controls or outcomes enter construction. No refilling, adaptive allocation, policy tuning or confirmation reselection.
- Gameplay comes from sealed `tower-deep-challenger-study-20260915`: identical DLLs, content/settings, floor 5, existing +10% health/power context, original timestamp, level 40/tier 1/rank 2, neutral equipment identities, 80 allowed essences and unrestricted copies. The team contains ten characters across two five-player parties, each with five essence slots. None of this content or configuration changes.
- Controls remain `team-040e60d3dbc5c127321653c47ed3a9d3` and `team-49f6979895354870c89362d4abf214bb`, in exactly the same context. Both controls are legal under team coverage, but their exact loadouts are absent from the generated pool; eligibility does not establish discovery.

Source is the sealed team-coverage implementation (`tower-team-coverage-20260915/files.json`, SHA-256 `08d7cb7f014cff2c3259c59beb0bc583189ec85b87b0709a14b59c3aac462036`). Its 59 tests and nine old-policy parity checks remain evidence; this preparation adds eight comparison tests. The preceding comparison runner/model and audits are adapted into a new package without modifying their sealed originals. All producing source, scripts, gameplay dependencies and protocol are pinned before execution; the compiled request is separately pinned before live preflight. Reference and candidate definitions may differ only in policy/method.

## Exact study allocation, conditional on approval

New namespace `tower-team-coverage-comparison-v1`; master label `2026091511`. Allocate exactly 45 unique values disjoint from the complete live reservation union: generation 1, discovery 4, selection 8, confirmation 32. Preserve all 482,686 existing reservations. Successful allocation would produce 482,731 total; rejected allocation candidates and interrupted attempts retain their durable records.

1. Baseline discovery first: 16 teams × 4 shared discovery seeds = 64 fights.
2. Candidate discovery: 16 teams × the same 4 seeds = 64 fights.
3. Freeze top two from each policy using existing rank (win rate, guardian health, survival, victory duration, ID). Merge exact recipes in the same context, preserving every origin. Screen these two to four teams on 8 new shared seeds, at most 32 fights. Choose one winner per policy by screen victories, then prior nomination rank and ID.
4. Freeze both winners and both controls. Merge exact duplicates, retain every origin, and run all two to four family members on 32 new shared confirmation seeds: at most 128 fights.

**Maximum 288 diagnostic fights including all repetitions; zero retries, resumes or replays.** A finalist plays 44 fights total, another screened team 12, an unscreened team 4, and each control 32. Discovery overlap between policies is still charged. Duplicate merging never creates replacement nominees. Incomplete discovery, screening or confirmation stops dependent work and cannot produce a success claim. Native per-policy definition cost remains 176; the wrapper runs each discovery separately and owns the combined 288 cap.

Primary endpoint is `team-coverage-finalist-minus-baseline-finalist` on paired confirmation victories. Draws are non-wins. Report the primary and four fixed finalist-minus-control contrasts, with existing simultaneous Wilson factors 8 for rates and 20 for paired gain/loss bounds. Identical merged members have exact zero paired difference. Boss health, duration and paired health differences are descriptive only, preserve missingness and never select finalists. No historical pooling, reliability pass, adoption, broad optimality or general performance claim follows from this exploratory pilot.

## Diagnostic and storage envelope

Carry forward 2,060.913 diagnostic seconds, leaving **339.087 seconds** under the previously approved cumulative 2,400-second ceiling. Preparation, compilation, tests, registry checks, two control preparations, allocation, execution, verification, independent audits and publication all charge elapsed wall time. Engineering editing time is not diagnostic workload. A conservative one-second publication allowance and 1 MiB shared test-output allowance are charged. No time reset or extension. Preparation has a 150-second aggregate ceiling and reserves at least 15 seconds for closure; execution reserves 20 seconds for closure and must have at least 170 seconds remaining before allocation. Individual phase limits and commands are frozen in `workflow.py` and `execute.py`; the lower cumulative remainder always wins.

Retain the cumulative **4 GiB** output cap. Prior bytes are the preceding completion's prior bytes plus the actual sealed team-coverage package size; add all new preparation/execution/study output and the temporary allowance. Preparation ≤192 MiB, execution ≤128 MiB, study ≤1 GiB, also subject to the smaller cumulative remainder. No recursive archive deletion or output reuse. Full preservation verification covers all 54 predecessor packages. Unrelated dirty tracked files and harness source hashes must remain unchanged except the six named active Markdown handoffs. New comparison test source is the only root C# addition.

Existing durable reservation intent, write-through allocation journal, attempt-start charging, cancellation, incremental storage accounting with full phase-boundary reconciliation and native archive verification remain mandatory. Native timeouts receive the wrapper's remaining deadline; process termination is a last bounded fallback and preserves partial evidence. Every phase has a create-new start/result/failure receipt. Any failure stops dependent work, preserves evidence and permits closure only. No automatic retry, revised fixture rerun or replacement directory under this protocol.

## Frozen preparation and verification

Preparation commands run once, in order: `workflow.py freeze`, `build`, `test-build`, `tests`, `request`, `check`, `audit`, then `publish.py` even if a prior check failed. Freeze snapshots source, dirty checkout, prior test output and full preservation chain. Isolated builds use captured gameplay DLLs and cached restore metadata. Eight tests run through `build/run-tests.ps1`, with fabricated scores and a guard rejecting combat. They cover exact budgets/input equality, context drift, independent nominations/incomplete discovery, screen ties/incomplete evidence, duplicate origins, paired extremes/misalignment, incomplete confirmation, and durable interrupted attempt charging.

Live preflight enumerates the entire current reservation registry, verifies 482,686 distinct values, materializes definitions using 45 already-reserved fixture labels (never executable combat inputs), prepares the two controls without fights, and writes 66 interval fixtures. The separate audit verifies hashes, complete reservation union, exact policy definitions and costs, identical captured control participants, all interval fixtures and test exports. The sealed team-coverage/diverse proposal and structural-trace fixtures become the execution parity references.

After all checks pass, publish readiness, measured resource receipts and `preparation-files.json`. Ask for the specific **45 fresh values / at-most-288-fight** exception with the measured remaining time; do not create an approval receipt until the user actually approves. The new `authorize.py` requires the actual reply and its context. After approval: `execute.py bind`, `run`, `verify`, `audit-execution`, and `finish.py`. Binding rechecks the live registry and full preparation seal before writing any allocation. Native verification rereads durable archives without combat. Independent verification reconstructs all archived records, nominations, winners, merged origins, paired estimates, complete schedules and attempt charges; both policy recipes/traces must equal their sealed synthetic fixtures.

## Unchanged limits and interpretation

V19 is sealed: 253 required recipes, no confirmation, unused 512 original confirmation values retained; v19 reliability Unresolved. Current reliability Fail 1/3, deep recovery 0/3, adoption Hold. No Kharad tuning, ability-order optimization, gameplay edits, deployment, 129,536-fight confirmation or old experiment cap changes. If a limit or check fails, report the unresolved work and preserve all evidence and reservations. Measured results may justify a later decision, not another unapproved experiment.
