# Retained Tower builds and linked calibration

**Later progression follow-up:** see the [floors 2–5 audit and calibration](Tower-Progression-Floors-2-to-5-Review.md) for current coverage and retained controls. This historical experiment remains unchanged.

**Subsequent result:** the [fresh independent search](Post-Calibration-Tower-Team-Search-Review.md) found Kharad at 59% with a stronger new team. Its [separate expanded-portfolio calibration](Kharad-Expanded-Portfolio-Calibration-Review.md) adds another 8% linked Health/Power and confirms a strongest rate of 25.25%. Garran remains unchanged and its fresh strongest control reached 34.8%. The settings and measurements below describe this earlier completed calibration; its sealed package remains intact.

11 September 2026. Garran and Kharad now have **locally applied linked Health/Power settings** confirmed against their retained party portfolios. Both portfolios pass the approved [10–50% policy](Tower-Balance-Acceptance-Policy.md): every included team's adjusted upper bound is below 50%, and at least one team's adjusted lower bound exceeds 10%. This is a scoped retained-build result, not an exhaustive floor or whole-Tower balance certificate.

The user explicitly requested tuning against the builds from the [independent pilots](Automatic-Tower-Team-Pilot-Review.md). This follow-up therefore keeps their full 80-Essence pool, including Rare Essences, hypothetical ownership, level-1 untrained Essences, no styles/contributions and exact gear/character budgets. Floor 1 uses five level-30 characters with four Essences and Uncommon Standard tier-1/rank-1 gear; floor 5 uses ten level-40 characters with five Essences and tier-1/rank-2 gear. Practical acquisition timing remains a separate question.

## Applied results

Multipliers below apply to the **pre-campaign local inputs**, scaling both Health and offense/Power together. Defense, resistance, penetration, regeneration and every other floor are unchanged.

| Floor | Retained parties | Linked multiplier | Health | Power/offense | Strongest confirmed team | Adjusted interval | Scoped assessment |
| --- | ---: | ---: | ---: | ---: | ---: | --- | --- |
| 1 | 57 | 1.06 | 1.776984 | 1.735008 | 136/400 (34%) | 26.65–42.22% | Pass |
| 5 | 106 | 1.56 | 1.95 | 2.4648 | 97/400 (24.25%) | 17.59–32.43% | Pass |

Garran's inputs were Health 1.6764 / offense 1.6368 before this pass; they already represented 1.32 times the original values. The new 1.06 factor gives **1.3992 times the original Health/Power inputs**. Kharad's previous inputs were Health 1.25 / offense 1.58; both increase by 56%.

The original Garran generated primary now wins **48/400 (12%)**. A previously shortlisted independent team is stronger under the new setting at **136/400 (34%)**. This ranking change demonstrates why tuning retained the earlier shortlist instead of relying on one nominated winner. The five original Kharad generated finalists score **0, 1, 3, 87 and 97 wins out of 400**, in ascending result order. Weak builds may remain below 10%; the approved lower threshold requires at least one viable party per cohort, not viability for every party.

The [seven exact retained builds and new measurements](../TestResults/balance/tower-retained-build-calibration-20260911-v2/published/calibration-builds.md) preserve all ordered Essences, character identities, equipment and confirmation seeds. Original pilot results remain historical observations on older content and are not pooled with these results.

## Automatic reuse

`TowerRetainedBuilds.cs` loads the portable [benchmark catalog](../LL/tools/BalanceHarness/Fixtures/tower-retained-builds.json) and a persistent `retained-tower-builds.json` index in the configured results directory. The catalog contains the six original generated finalists plus each floor's strongest calibrated control; exact recipe deduplication yields **two floor-1 controls and five floor-5 controls**. The strongest floor-5 control was already one of those finalists.

New Tower Lab **Find teams** previews include compatible controls automatically. Matching requires the same floor, complete party size, character/gear budget, starting context, allowed Essence pool and owned-copy limits. Their historical schedules are excluded even when a changed budget makes the recipe ineligible. All controls are visible in the cost preview and remeasured on fresh confirmation seeds. Exact recipes are deduplicated without replacing identities or changing gear; the existing reference and battle caps still apply.

Completed future Tower Lab studies automatically append their generated finalists, including above-ceiling winners. Cancelled, incomplete and invalid studies do not become controls. The index uses a file lock and atomic replacement, and checks each stored recipe hash. The original archives remain untouched. Historical win counts do not seed or rank independent candidate generation; references enter only after generated finalists freeze. Imported/CLI definitions remain explicit frozen plans and are not silently expanded with additional references.

## Frozen calibration protocol

The reusable [calibration script](../LL/tools/BalanceHarness/Scripts/calibrate-retained-tower-builds.py) uses the retained production-parity executable and normal Tower archives. Its [protocol](../TestResults/balance/tower-retained-build-calibration-20260911-v2/protocol.json), baseline snapshot, exact portfolios and all stage seeds were frozen before combat. Both floors retained every compatible previous confirmation party and all sixteen earlier shortlisted parties: 57 distinct floor-1 recipes and 106 floor-5 recipes.

Coarse discovery tested linked multipliers 1.0, 1.2, 1.4, 1.6, 1.8, 2.1, 2.5 and 3.0 on eight paired seeds for every retained party. Each floor then refined eleven points within its selected bracket. The fine portfolio included every original generated finalist and the strongest other parties by total coarse wins, up to twelve per floor, with 64 paired fresh seeds per point. All other parties remained in confirmation. Selection targeted the strongest observed fine performance near 30%, within a declared 15–40% selection range; it did not weaken the team-search objective.

Garran selected 1.06 from the 1.0–1.2 bracket. Kharad selected 1.56 from 1.4–1.6. Both settings and complete confirmation families were frozen before their 400-sample confirmation. No party was dropped, no extra samples were added, and no setting was selected from confirmation outcomes. The evaluator uses `bonferroni-wilson-95-v1` separately for the 57-cell and 106-cell families; no pooled average hides a stronger team.

The first preparation attempt stopped before combat because Python's plain JSON reader rejected comments in local appsettings. The script now strips JSON comments while preserving quoted strings; the complete protocol was frozen in the separate `-v2` directory before any battle. It copies only allowlisted game data and nonsecret combat settings. Concurrent unrelated workspace changes are preserved.

## Verification and scope

The campaign completed **92,803/92,820 allowed combats**, including **25 matching detailed replays**. Both confirmation archives passed the existing semantic evaluator. An independent audit reproduces selection, sample accounting and all pointwise/adjusted intervals, and verifies **92,778 saved battle hashes**.

The final current executable reproduced **120 exact complete battle reports** across the six original generated finalists after local application. Every one of the **65 unaffected-floor before/after report pairs** also matched. These repeated identities verify application and regression; they are not extra independent confirmation samples.

Relevant backend checks passed through `build/run-tests.ps1`; the retained logs record the full 289-test regression and final catalog/content checks. A running local dashboard initially held build DLLs open and was stopped. A later sandbox build could not update an existing Persistence.LL generated cache; the authorized local build outside the sandbox succeeded. The dashboard was restarted on port 5619. No required verification remains blocked.

Changed implementation files are `TowerRetainedBuilds.cs`, `TowerDashboardStudies.cs`, `TowerDashboardStudySeeds.cs`, the portable catalog, the reusable calibration script and two relevant test files. Local game changes are limited to the four Health/offense values in [tower-floors.json](../LL/src/API/API.LL/Data/world-tower/tower-floors.json). Planning/README updates and this report document the resulting scope. No engine/ability changes, new dependency, production configuration schema, migration, shared-database mutation or deployment are part of this work.

Useful commands, from the repository root:

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerRetainedBuildTests|FullyQualifiedName~BalanceHarnessTowerDashboard|FullyQualifiedName~WorldTowerTests'
```

For a new calibration campaign, run the script's `freeze`, `discover --floor 1`, `discover --floor 5`, `confirm --floor 1` and `confirm --floor 5` phases with a new `--output` directory and an explicit `--source-pilots` package on freeze. The retained `finalize.py` documents the reviewed local application, parity, replay and benchmark-registration steps for this campaign. Reusing old seeds is reproduction; later experiments need an updated historical exclusion union and a separately frozen protocol.

Those next independent searches are now complete; see the subsequent result above. Continue carrying saved controls into any further searches. The earlier floor-5 discovery was saturated at 100%, so its ranking alone did not establish the strongest possible team. Unconfirmed discovery candidates, other progression budgets, practical Essence availability and the wider floor-1–11 curve remain separate coverage; these two scoped passes do not establish full Tower balance.
