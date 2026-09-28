# Floor-13 gear confirmation

**Decision: resistance-and-health is the confirmed working gear benchmark for this exact floor-13 team and budget.** It won **512/512 fresh fights**, compared with **194/512** for original gear and **260/512** for armor-and-health. Both predeclared comparisons passed. No Essence composition or search policy changed.

## Fixed question and scope

The [encounter coverage screen](Tower-Gear-Profile-Integration-20260928.md) observed 32/32 wins for resistance-and-health on floor 13, compared with 8/32 and 12/32 for the two controls. Those historical diagnostic results motivated this confirmation but contribute no observations to it.

This run froze the exact three floor-13 scenarios from the sealed coverage archive, removing only their old seed panels before fresh allocation. The candidate uses **resistance-specialized heads, chests and legs, plus health-specialized necklaces** on every character. The armor control uses armor specializations in the same three slots and the same health necklaces. Original gear is the second control.

Conditions: ten characters, seven level-1 unascended/unevolved Essences per character, character level 60, Uncommon Fine tier-2/rank-3 gear with baseline rolls, no styles, hypothetical ownership, and an uncleared floor without contributions. Character positions, Essence order and instance identities are preserved. This remains the inherited provisional progression budget, not an approved player-population target.

Attribute rules **18**, equipment release **4** and **healing-v1** are captured. All five combat assembly hashes exactly match the preceding coverage study. Before allocation the run prepared all three scenarios, reconstructed both specialization profiles from the original baseline, and matched **all 96 archived combat-input hashes** for their diagnostic fights. Test-only code was added; the combat executable and content were unchanged.

## Frozen rule and result

Exactly **512 fresh paired seeds per loadout**, or **1,536 fights**. To qualify, resistance-and-health had to meet both requirements against **each** control:

1. At least **26 net gained wins out of 512**, exceeding five percentage points observed.
2. Exact one-sided paired-binomial p-value at most **0.025**, using Bonferroni adjustment for the two comparisons.

The practical threshold applies to the observed difference, not a confidence bound on the true effect. The statistical interpretation treats outcomes from distinct fresh pseudorandom seeds as independent draws under the captured combat model. The shared controls/panel need not be independent of each other for the Bonferroni adjustment.

There was no pooling of diagnostic results, reselection, early stopping, extension or retry.

| Loadout | Wins | Observed clear rate | Mean guardian health remaining |
| --- | --- | --- | --- |
| **Resistance and health** | **512/512** | **100%** | **0%** |
| Original gear | 194/512 | 37.89% | 4.177% |
| Armor and health | 260/512 | 50.78% | 2.216% |

| Candidate versus | Gained / lost wins | Observed improvement | Exact one-sided p | Qualifies |
| --- | --- | --- | --- | --- |
| Original gear | 318 / 0 | +62.11 percentage points | `2^-318` ≈ 1.87e-96 | Yes |
| Armor and health | 252 / 0 | +49.22 percentage points | `2^-252` ≈ 1.38e-76 | Yes |

The 100% sample result does not guarantee every future fight will be won. This confirms the exact gear profile on the fixed team, floor and budget. It does not prove global optimality, a universal gear recommendation, practical item acquisition, or improved reliability of an Essence-search algorithm. Other promising profiles, such as health-and-regeneration, were not part of this fixed confirmation and are not ranked by it.

## Decision and next step

Retain resistance-and-health as the working floor-13 benchmark in this scope, with both original controls preserved. Keep the separately confirmed armor-and-health benchmark for its floor-15 team and budget. The complete seed-free scenarios are saved in [teams.json](../TestResults/balance/tower-floor13-gear-confirmation-20260928/teams.json); the candidate is the entry with `profile: resistance-and-health`. The reusable gear-profile API and explicit search-plan commands remain available. No production defaults or authored encounter values were changed.

Close this confirmation without another extension. The current test conditions now include two independently confirmed gear profiles with sample clear rates of 100%. Further algorithm comparisons need encounter/progression conditions with room to improve against the strongest retained gear-aware controls. The most useful next step is to review the intended progression budgets and encounter balance with those controls, then define one informative benchmark. Keep the supported search during that review. Do not weaken the chosen reference's gear merely to create apparent search gains, and do not infer live balance changes from this provisional budget alone.

## Implementation and verification

Target: the offline Balance Harness in the primary game service.

- Added `LL/tests/EssenceSystem.Tests/BalanceHarnessGearConfirmationTests.cs`: selects the exact archived floor-13 profiles, rejects nongear differences, matches source inputs, prepares before reservation, executes the bounded confirmation and reconstructs all native evidence.
- Updated `BalanceHarnessAffinityTeamConfirmationTests.cs`: exposes the existing allocator-plan helper and adds an optional allocation-domain argument. The previous floor-15 default remains unchanged. The new confirmation reuses its tested 512-sample exact comparison, two-control decision rule and durable two-block reservation.
- Added `Balance Harness/analysis/run-tower-gear-confirmation.py`: authenticates coverage/history inputs, requires the source runtime, freezes the declared design and owns the no-build test process. New output directories are required; no resume/retry path exists.
- Added `Balance Harness/analysis/verify-tower-gear-confirmation.py`: independently authenticates the archive, recomputes reservation derivations/rejections, checks exact frozen recipes and all raw outcomes, and recalculates both integer tails and the final decision. It reuses the coverage auditor's read/hash/path helpers.
- Added this report and updated `LL/tools/BalanceHarness/AFFINITY-SEARCH.md` with the confirmed result and current guidance.

**26 focused backend checks passed**, with two scientific opt-ins skipped. The actual confirmation then passed **all three checks** in its fixture. The reused tests cover practical/statistical thresholds and partial reservation failure; the new tests cover unchanged Essence order/source recipes and the separate allocation namespace. The build succeeded with existing warnings. Python command loading and `git diff --check` passed. No requested verification remains blocked.

Native verification reconstructed every input hash, cache identity, recipe and report binding. Independent readback authenticated **1,658 files**, all **1,536 raw reports**, the two allocation journals, both exact paired comparisons and the final result. It also verified the unchanged source runtime and recipes. The audit ran zero fights and saved its receipt outside the sealed study.

No migrations, production configuration changes, deployments or shared database operations. Prior working-tree changes, including the user's attribute tooltip edits, were preserved.

Commands (Python denotes the bundled runtime):

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-gear-profiles-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessGearConfirmationTests|FullyQualifiedName~BalanceHarnessAffinityTeamConfirmationTests|FullyQualifiedName~BalanceHarnessGearProfileTests'
python -B -X utf8 'Balance Harness/analysis/run-tower-gear-confirmation.py' --help
python -B -X utf8 'Balance Harness/analysis/run-tower-gear-confirmation.py' --package TestResults/tower-floor13-gear-confirmation-owner-20260928 --output TestResults/balance/tower-floor13-gear-confirmation-20260928 --artifacts TestResults/tower-gear-profiles-build-20260928
python -B -X utf8 'Balance Harness/analysis/verify-tower-gear-confirmation.py' --owner TestResults/tower-floor13-gear-confirmation-owner-20260928 --manifest-pin 5b91d70abee1c5cdde891918ad7001479c43568da090b2995111066b7a9e869f --receipt TestResults/tower-floor13-gear-confirmation-owner-20260928/independent-readback.json
git diff --check
```

## Accounting and evidence

**512 accepted fresh seeds** were disjoint from **833,546 historical exclusions**. One colliding derived value was rejected by the declared allocator; it was already excluded. Both 256-value blocks completed before any combat. Every accepted value was used, and the completed exclusion union is now **834,058**. Use this confirmation's ledger, alongside the full registry, for subsequent scientific allocation.

Master: `2026092891`; domains: `tower-floor13-gear-confirmation-v1/block-1` and `/block-2`. A parent Pending record prevents partial allocation from being treated as complete. The complete registry was checked before and after execution.

The experiment took **98.13 seconds**; its owner took **100.64 seconds** and drained all eight processes. The sealed archive occupies **125,306,394 bytes**, including the manifest. Limits were 840 fixture seconds, 900 owner seconds and 1 GiB. Exactly 1,536 attempts and completions, zero retries.

- Study, exact loadouts, raw reports and latest ledger: `TestResults/balance/tower-floor13-gear-confirmation-20260928/`.
- Frozen declaration, request, process receipt, test log and independent readback: `TestResults/tower-floor13-gear-confirmation-owner-20260928/`.
- Passing verification log: `TestResults/tower-gear-confirmation-verification-20260928.log`.
- Coverage source: `TestResults/tower-gear-coverage-20260928/`.
- Prior authoritative ledger source: `TestResults/balance/tower-gear-screen-20260928/`.

Result SHA-256: `f0b4adf3c633c33ce1279fce8ed8011d661394b194a4ac8d6883b573549660ca`.

Archive manifest SHA-256: `5b91d70abee1c5cdde891918ad7001479c43568da090b2995111066b7a9e869f`.

Coverage manifest SHA-256: `f715905d4bea3d3c9df43418ac2d264298936a4c1c8416c0d1eacce457569746`.

Prior history manifest SHA-256: `20b196238a4a51576d0e07202002aebe6882aa68a9260bbc80f576b176e7aa8a`.

These pinned archives are retained local ignored evidence. A clean checkout alone cannot reproduce this exact study; preserve the archives with their hashes.
