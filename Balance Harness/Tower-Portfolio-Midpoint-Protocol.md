# Captured-v19 midpoint study — frozen protocol

Frozen 14 September 2026 after the complete [four-factor screen](Tower-Portfolio-Ceiling-Screen-Review.md). The user requested proceeding with a separately frozen intermediate study. This new experiment tests **one +10% guardian Health/Power setting**, all **253 original recipes**, and **256 fresh shared seeds**: **64,768 maximum attempted fights**. No old study is reopened or pooled.

The midpoint is a directly tested experimental choice between +8% and +12%, not an interpolated certified setting. Apply factor **1.10** directly to captured baseline: Health **3.890286766080**, Offense **4.917322833280**. Every other scalar, file, combat rule, recipe, Essence/character order, role and nomination remains captured-v19. Each encounter contains two five-player parties. No live gameplay content changes.

## Statistical contract

Use 256 independent fresh values shared in the same declared order by all 253 recipes. All outcomes are new; prior samples guide the setting choice only. Draws are non-wins. Preserve the earlier per-cell threshold **alpha .05 / 1,012**, despite testing only one setting; do not substitute the ordinary 253-cell interval for this study's decision. The implementation reuses `TowerStagedBalance.Interval(wins, 256, 506)`, whose two-tail allocation is algebraically identical. Retain every recipe and interval.

The result can be **CandidateForFullFamilyConfirmation** only if every adjusted upper bound is ≤50%, at least one adjusted lower bound is ≥10%, and the largest observed win rate is in [15%,40%]. Otherwise report **Unresolved**. There is no selection among additional factors, early stopping for a promising outcome, pooling, interpolation, top-up or post-hoc family reduction. These fresh conditional bounds do not claim a cumulative 95% guarantee over an unlimited sequence of adaptive historical studies. Product acceptance still needs a separately designed independent full-family study of the relevant 43,879-entry inventory. Reliability **Fail 1/3** and adoption **Hold** remain unchanged.

## Seeds, resource bounds and order

The existing ledger contains **481,347 reservations**, including 512 unused original v19 confirmation values and the 128 values used in the completed screen. Audit every registered seed/history file before allocation; retain their paths/hashes and copies. Exclude their complete array union, used or unused.

Reserve exactly **256** new values durably before binding or simulation. Deterministic allocator: `StableRandom.Seed("tower-captured-v19-midpoint-v1", "2026091422", "midpoint", invariantDecimalAttemptIndex)`, index starting at zero, reject complete history and duplicates, at most 100,000 proposals. A rejected/interrupted study retains every reservation. No further allocation is permitted during execution or verification.

The frozen limits are **64,768 fights, 14,400 active seconds, 8 GiB total new output, zero combat retries/replays/resumes**. Scope and setup charges are recorded under `TestResults/balance/tower-captured-v19-midpoint-20260914`. Include initial review, engineering captures/builds, materialization, allocation, binding, all runtime, verification and final metadata. No existing evaluator or experiment cap changes: a schema-1 253×256 definition fits the existing 100,000-fight cap. Execute ordinal recipe IDs, then the shared seed order, using prepared compact execution with 32-record chunks.

One durable outer journal flushes each start before simulation and completion after a returned outcome; compact attempt charging remains intact. One deadline and owned-storage accountant cover the entire new root. Interrupted starts consume budget; a failure preserves partial output and stops. Verification and final publication remain inside the time/storage budget. Source packages remain immutable.

## Engineering and verification before first combat

Implement a dedicated fixed midpoint command, retaining existing source contracts. Build the producing harness against the sealed captured-v19 gameplay DLLs. Freeze exact command/script hashes before native diagnostics and freeze the bound executable, definition, history and full setup inventory before combat. Materialization and binding are guarded against engine entry. Verify all 253 baseline rosters against the previously reviewed hashes and compare each midpoint roster, allowing only guardian Power/MaxHealth and starting health to differ. Binding the fresh schedule must reproduce those midpoint rosters.

Backend tests run through `build/run-tests.ps1`. The diagnostic filter includes the new midpoint tests and existing ceiling controller/preparation tests; all use pure calculations or synthetic journal events, with **zero diagnostic fights**. Native zero-combat checks are one materialization, one bind, one prepared check, plus one post-completion native reconstruction and one independent audit. Freeze exact invocations before execution. Diagnostic work is bounded by 1,800 active seconds / 4 GiB, and the global study envelope also applies. Code/build failures can be repaired before producing inputs are frozen; production allocation/materialization/binding/run failures stop without retry.

After all fights, reconstruct every compact record and prepared roster. Independently verify the ordered seed schedule, all 253 outcome counts and intervals, the result, complete durable journal, producing identities and exact output inventory. Preserve prior studies and unrelated checkout edits. No combat is permitted in reconstruction.

Native command forms are `tower-midpoint-materialize|bind|check|run|verify <root>`. These expose no cap, factor, sample, retry or resume overrides. The materialized template has no seed schedule and cannot execute; the externally supplied ledger must match the exact allocator sequence. Retain all completed input/output and no-failure evidence. Any further study or gameplay application requires a new explicit scope.
