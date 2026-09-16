# Prospective selection rule for zero-win nominees

16 September 2026. Target: offline `LL/tools/BalanceHarness`. **Implemented and synthetically verified.** The opt-in comparison v5 and staged `tower-staged-zero-win-health-v1` share the rule below. All 23 selector cases and 12 supplied-search facts passed through `build/run-tests.ps1`; see the [implementation review](Tower-Supplied-Composition-Review.md) for exact commands, limitations and retained engineering failures. The production compact-health reader has not been exercised against a real archive in this scope. This document is not authorization for new combat, seed allocation or resource extensions. The original rationale and coverage requirements below remain applicable.

## Reason for the change

The [V5 trajectory review](Tower-Fresh-First-Trajectory-Review.md) verified correct nominations and selection. Its selected local edit improved discovery boss health by 0.315 percentage points, but left 0.660 points more health on selection and 0.7240625 points more on confirmation. Both nominees had zero selection wins, so the existing selector used discovery rank. Both finalists subsequently won 0/32 confirmation fights.

The proposed rule lets the independent selection stage distinguish nominees when wins provide no ordering. It does not establish that health is a reliable predictor of future wins or that this change will improve search strength. The completed V5 result and its frozen endpoint remain unchanged.

## Proposed rule and scope

Apply the same opt-in selection version to both comparison arms, using their already frozen top-two nominations and complete selection evidence:

1. Prefer more selection wins, as today.
2. **Only when both nominees have zero wins**, prefer lower arithmetic mean guardian health remaining percent across all scheduled selection trials.
3. If that mean is exactly equal, retain original discovery rank, then ordinal recipe/member ID as deterministic fallbacks.
4. A tie with positive wins keeps the existing discovery-rank/ID behavior. Do not expand this proposal to all win-count ties.

Use the existing persisted guardian-health measurement and comparable, complete seed schedules. Incomplete, missing or invalid evidence must fail validation rather than select a winner from a partial sample. Confirmation outcomes never enter selection. No fitted thresholds, favorable-seed filtering or Essence-specific exceptions are proposed.

Keep current defaults and historical comparison versions reproducible. Version the changed selection contract explicitly and bind it through request validation, execution and completed reconstruction. The existing twelve-fresh/four-local search schedule, discovery ranking, top-two nominations, shared-recipe merge and origin retention remain the inputs to selection. No new candidate generation, order tuning, local-edit pruning or discovery-parent acceptance rule is part of this change. A local edit competes through the same frozen nominations and selection evidence as any other team.

## Next implementation and verification scope

Freeze an exact zero-combat implementation protocol before running diagnostics: source/input pins, named tests, build/run commands, time/output envelope and durable failure evidence. Any engineering correction must fit its prospectively declared attempt limit and unchanged resource caps; historical study reruns and combat retries remain excluded. Implement the opt-in selector and verify it through `build/run-tests.ps1` using fabricated evidence, without executing combat or allocating balance seeds.

Required coverage:

- More wins beats lower health; positive-win ties retain legacy behavior.
- Two zero-win nominees use selection health even when discovery rank disagrees; exact health ties use deterministic fallbacks.
- Nominee/evidence enumeration order cannot change the winner; both arms use the same selection version.
- Shared recipes retain all origins and each arm's original discovery rank. Shared finalists continue through the existing merge and accounting rules.
- Incomplete or invalid evidence is rejected. Selection does not read confirmation outcomes.
- Legacy defaults and completed reconstruction retain their existing serialized results. Version mismatches fail before allocation; attempt caps, durable charging, cancellation and archive verification are preserved.

Publish the implementation, test results and exact resource usage together. Reuse compatible existing verification where appropriate; do not represent the previously passing 85 backend tests or 18 saved-reader checks as tests of this unimplemented rule. A future strength comparison must be frozen prospectively and separately authorized for any fresh values and resource exceptions. Do not rerun V5 or reinterpret it as a comparison of this proposed selector.

## Resource and evidence boundary

The prior [trajectory completion receipt](../TestResults/balance/tower-fresh-first-trajectory-20260916/completion.json) recorded **4,049.7802723556424 / 4,380 diagnostic seconds** and **4,718,720,963 / 4,731,174,912 bytes**. Those were the starting balances, not an additional allocation. The completed [implementation receipt](../TestResults/balance/tower-supplied-composition-20260916/completion.json) now records **4,101.49827235565 seconds** and **4,730,423,384 bytes**, including builds, synthetic tests, retained failures and conservative publication reserves. It leaves **278.50172764434956 seconds and 751,528 bytes** within the unchanged overall caps. No further diagnostic is authorized by these remaining balances alone.

Preserve all **483,046 reservations**, including V19's 512 unused values and prior failed allocations. V19 retains 253 recipes and no confirmation; reliability remains Unresolved, later reliability Fail 1/3 and deep recovery 0/3. Keep baseline as reference, V5 exploratory and **adoption Hold**. Fixed ability order remains. No Kharad tuning, gameplay/content, migrations, configuration or deployment changes.
