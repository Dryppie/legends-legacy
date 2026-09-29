# Tower floors 4 and 6 — calibration, 29 September 2026

**Completed: both guardian changes are applied locally and verified.** Floor 4's strongest two confirmed combinations won **29.30% / 23.83%**; floor 6's won **34.77% / 25.78%**. Each floor passed a separate 35-cell, 8,960-fight confirmation. Application checks matched **17,920 native inputs and 70 complete battle replays** across the two floors. The supported search and repeating gear curve are unchanged. Broader build viability and the remaining floors still need work; this is not a claim that the entire Tower is balanced.

Target: primary LL game Tower content and offline Balance Harness. The user's instruction is to continue actual Tower balancing after the confirmed floor-5 change. Preserve that change, the repeating gear curve, the supported affinity search and the withdrawal of selectable dungeon supplies. These are declared benchmark loadouts with hypothetical ownership, not ordinary-player acquisition claims.

## Frozen budgets and sequence

| Floor | Characters | Level | Essences each | Gear | Original health / offense | Applied health / offense |
| --- | ---: | ---: | ---: | --- | --- | --- |
| 4, Vaelor | 5 | 30 | 4 | Tier 1, Epic / Fine / rank 3 | 1.30425 / 2.82 | **2.3231953125 / 5.023125** |
| 6, Orsenn | 5 | 40 | 5 | Tier 1, Epic / Fine / rank 3 | 1.79 / 1.27 | **6.153125 / 4.365625** |

Both budgets use baseline equipment rolls, no styles, and unascended/unevolved Essences. Essence order is fixed ordinal, never an optimization parameter. Each initial family has three retained compositions × seven gear profiles. The preceding current-content screens returned 32/32 wins in **every** cell on both floors. Their normal-gear reference fights averaged roughly 33 seconds on floor 4 and 27 seconds on floor 6.

1. Extend the existing projection and preparation regressions to floors 4 and 6. Build through `build/run-tests.ps1` into isolated artifacts. Capture current content before each floor's studies.
2. For each floor, begin with separate 32-seed screens at **2× and 4× health and offense together**, retaining defense, resistance, penetration, regeneration and mechanics. Increasing both avoids relying solely on longer health bars. If both remain above target, extend to 8×; if both are below target, screen 1.5×. These are bounded selection experiments, not acceptance evidence.
3. Refine a bracket with up to four midpoint screens per round, stopping once a candidate's strongest observed rate is near 25% (prefer a 15–35% observed range). Preserve every candidate and do not pool different settings. This prospective adaptive selection rule permits new declared refinements; it does not permit extending or retrying a completed panel.
4. At the selected nontrivial setting, run the unchanged supported 528-fight affinity search, starting from the three strongest measured same-gear references and using 128 separate evaluation seeds for all five nominees. Add both generated finalists at all seven gear profiles to the retained 21-cell family. Keep all **35** combinations for final selection and confirmation, even if search validation retains its benchmark.
5. If the expanded family exposes a stronger route above target, refine the setting again with the entire family; do not discard the stronger route. Freeze the selected setting before **256 fresh seeds per cell** (8,960 fights). Acceptance is the existing approximate simultaneous 95% Bonferroni-Wilson rule across the complete family: all upper bounds at most 50%, at least one lower bound at least 10%. Any failure/inconclusive result stays separate and cannot be pooled or extended.
6. Apply only a confirmed setting, compare all native inputs, and replay one complete fight per cell on the current build. Finish floor 4 before freezing floor 6's current content, so the final combined Tower file retains both confirmed changes and floor 5. Record composition diversity and pacing limits separately from the numerical acceptance rule.

Every phase uses a new owner/output pair, a 900-second process limit, 840-second native deadline, at most 2 GiB output and at most 20,000 fights, with zero retries. Durable allocations exclude all **886,182** inherited values and every subsequent balance-pass owner ledger, including unused reservations. Do not overwrite or repin existing archives. No deployments, database operations, migrations or replacement acquisition sources.

## Results and verification

The expanded current-build regression suite passes **52 checks**, with three opt-in experiment skips. Additional scalar checks verify independent health/offense factors, unchanged defenses, input immutability, identity factors and rejection of twelve nonfinite/out-of-range cases. The previous floor-5 report and archives remain unchanged evidence for that floor.

Floor 4 preparation completed without combat. Both first joint-factor screens completed 672 fights: **every cell had 0/32 wins at 2× and at 4×**. At 2×, the strongest retained armor-and-health team left 27.66% boss health on average, with a 37.19-second mean duration. These settings are too hard for this family. The predeclared 1.5× follow-up is the next bracket point; no boss change has been applied from these screens.

At **1.5×**, three retained combinations still won 32/32. The midpoint **1.75×** produced a strongest result of **6/32 (18.75%)** and a second armor-and-health composition at **5/32 (15.63%)**. Their mean durations were 41.31 and 43.13 seconds. That meets the prospective nontrivial-screen range for the supported search. The search uses this captured setting (health **2.2824375**, offense **4.935**) and armor-and-health gear, with its own independent 128-seed evaluation panel. These rates are selection evidence only.

The floor-4 supported search completed **1,168 fights** including all five nominees' separate evaluations. The generated teams won **62/128 (48.44%)** and **51/128 (39.84%)**, versus retained references at **31/128, 21/128 and 0/128**. Its 60-pair internal gate returned `ChallengerNeedsConfirmation` (17 gained wins, seven lost wins). This is a useful stronger candidate from the existing algorithm, not an algorithm-superiority claim or final balance acceptance. All 35 composition/gear cells enter refinement at the next midpoint, **1.875×**, because the stronger routes leave too little room below the 50% acceptance ceiling at 1.75×.

The expanded 35-cell midpoint screens each completed 1,120 fights. Their strongest rates were **1/32 at 1.875×**, **4/32 at 1.8125×**, and **11/32 at 1.78125×**. The latter falls inside the declared 15–35% selection window. Freeze floor 4 at **health 2.3231953125 / offense 5.023125** for **35 × 256 = 8,960 fresh confirmation fights**. No selection observations enter the intervals, and no cell can be removed after confirmation starts.

### Floor 4 confirmation

All **8,960 fresh fights** completed in **86.01 seconds**, with zero retries. The independent 9,063-file audit returns **Pass**. The two generated armor-and-health compositions won **75/256 (29.30%)** and **61/256 (23.83%)**, with approximate simultaneous adjusted intervals **21.16–39.02%** and **16.44–33.21%**. Both establish the 10% viability threshold; all 35 upper bounds stay below 50%. The strongest retained reference won 21/256 (8.20%), which does not establish that threshold. This is two confirmed routes in the tested family, not broad gear/build viability or universal balance.

The confirmed setting is applied locally on floor 4. **All 8,960 native inputs and 35 complete replays match** in the checked application. Floor 5's confirmed health and all other guardian fields remain unchanged. This confirmation does not pool or reuse selection observations.

### Floor 6 selection

Floor 6 was freshly prepared after floor 4's checked application, so its captured whole-Tower content includes both prior confirmed changes. The first 21-cell screens each completed 672 fights. At **2×**, multiple combinations still won 32/32; at **4×**, every combination had zero wins. The strongest 4× combination left 47.15% boss health on average. Continue at the predeclared midpoint **3×**, retaining the same level-40/five-Essence budget and all original gear profiles.

At **3×**, the strongest restorer-specialization composition won **25/32**, followed by another at **17/32**. At **3.5×**, the strongest result fell to **1/32**. Continue at **3.25×** within that bracket. These screens keep healer-focused gear in the family rather than assuming floor 4's strongest armor-and-health profile transfers to a different guardian.

At **3.25×** (health **5.8175**, offense **4.1275**), the strongest two restorer-specialization references won **10/32 (31.25%)** and **9/32 (28.13%)**. Their mean durations were 71.81 and 65.47 seconds. This enters the supported search at its declared selection range, with all three references at that gear profile and a separate 128-seed evaluation panel.

The floor-6 supported search completed **1,168 fights**, including the five nominees' separate evaluations. Generated teams won **72/128 (56.25%)** and **74/128 (57.81%)**; retained references won **54/128, 25/128 and 0/128**. Its internal gate returned `ChallengerNeedsConfirmation` (23 gained wins, eight lost wins). Include both generated teams across all seven gear profiles, preserving the full 35-cell family, and refine at **3.375×** before independent confirmation. The search's 60-pair choice and the separate 128-seed ranking differ; both finalists remain included.

At **3.375×**, the expanded 35-cell screen completed 1,120 fights. The generated restorer-specialization teams won **14/32 (43.75%)** and **11/32 (34.38%)**; the best retained team won 3/32. Refine at **3.4375×**, between this setting and the earlier 3.5× reference screen, to seek the predeclared selection range for the entire expanded family.

At **3.4375×**, the 1,120-fight screen returned strongest results **9/32 (28.13%)** and **6/32 (18.75%)**, with mean durations 75.31 and 69.47 seconds; the best retained result was 2/32. Freeze floor 6 at **health 6.153125 / offense 4.365625**, keeping all 35 cells, for **8,960 fresh confirmation fights**. This completes selection; no observations above enter the acceptance intervals.

### Floor 6 confirmation and final application

All **8,960 fresh fights** completed in **91.24 seconds**, with zero retries. The independent 9,063-file audit returns **Pass**. The generated restorer-specialization teams won **89/256 (34.77%)** and **66/256 (25.78%)**; their approximate simultaneous adjusted intervals are **26.02–44.67%** and **18.11–35.31%**. Both establish the 10% viability threshold, and all 35 upper bounds stay below 50%. The strongest retained reference won 21/256 (8.20%), which does not establish viability under this rule.

The confirmed floor-6 values are applied locally. **All 8,960 native inputs and 35 complete battle replays match**. Whole-Tower semantic equality against this confirmation also preserves floor 4's applied values and floor 5's earlier health adjustment. The final Tower file SHA-256 is **`44a87b7872265bfc55c4253bbc83a9577f6acd287e5d7cff80100b7776d55830`**. Against this continuation's entry snapshot, only four data fields changed: health and offense on floors 4 and 6. Floor 5 remains health `3.1829618995`, offense `4.4702934848`.

### Interpretation and next work

The two strongest floor-4 combinations average **40.07 / 41.57 seconds** per battle; floor 6's average **77.79 / 69.60 seconds**, across wins and losses. Joint health/offense changes increase pressure as well as durability, while leaving guardian mechanics, defenses, resistance, penetration and regeneration intact. These engine durations are pacing observations, not player-time measurements or a newly approved pacing target.

Each floor has **two confirmed compositions**, but both use the same favored gear profile on that floor: armor-and-health on floor 4, restorer-specialization on floor 6. Other tested gear profiles did not establish the viability threshold. The scope is five tested compositions per floor, not all legal parties. The existing algorithm found useful stronger candidates on both floors; this does not establish algorithm superiority. No lower-Essence control or acquisition-feasibility conclusion follows from this pass.

**Next Tower work:** screen floors **1–3 and 7–9** against their explicit budgets and repeating gear bands, retain stronger discovered combinations, and independently confirm any proposed changes. Then finish the consistent pass for **12–15**, accounting for stronger gear carried from the preceding cycle. Preserve the earlier floor-10/11 evidence while checking whether its captured inputs still apply. Floor 5 still has only one confirmed viable combination, so broader composition viability remains a separate gap. Acquisition work must not replace this Tower queue or reinstate withdrawn supplies.

## Changed files and verification

- [Tower guardian data](../LL/src/API/API.LL/Data/world-tower/tower-floors.json): the four confirmed health/offense values above.
- [Floor projection tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs): admit floors 4 and 6 and verify their complete party projections.
- [Balance pass fixture](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBalancePassTests.cs): extend native preparation/gear/order regressions to floors 4 and 6.
- [Bounded owner](analysis/run-tower-balance-pass.py): independent finite health/offense factors in isolated content copies; the historical earned-party archive is required only for floor 5. Search policy is unchanged.
- This report, the [handoff](Tower-Continuation-Handoff-20260928.md), [search guide](../LL/tools/BalanceHarness/AFFINITY-SEARCH.md), and [harness README](../LL/tools/BalanceHarness/README.md): applied results, evidence limits and the Tower continuation queue. Historical acquisition headings are explicitly superseded.

Build and regression command (run once to build and again with `-NoBuild` after application):

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-floors46-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests|FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessAffinitySearchTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~WorldTowerTests'
```

Both regression runs passed **52 tests**, with **three intentional opt-in skips and zero failures**. The build initially required authorized escalation to read the existing user NuGet configuration. It completed; no verification command remains blocked. Logs are [build/regressions](../TestResults/tower-floors46-build-20260929.log) and [final regressions](../TestResults/tower-floors46-final-regression-20260929.log). Study/application owners invoke the same wrapper with `-NoBuild`. Existing application-check code is reused unchanged. Scalar invariants and twelve invalid-input checks pass; both Python owners parse. The final JSON comparison checks that exactly the four intended fields differ from the entry snapshot.

No migration, dependency, environment configuration, deployment, database operation or new reward source is introduced. The data changes are local and will take effect only when included in a later normal content release. All unrelated working-tree changes are preserved.

## Evidence and continuation exclusions

Selection plus confirmation completed **16,176 floor-4 fights** and **15,728 floor-6 fights**, followed by 35 application replays per floor: **31,974 actual fights total**. Replays reuse their original seeds and are not new statistical samples. Two preparation phases ran no combat. All separate phases remain archived; none were pooled or retried.

The final exclusion union is **887,616**, including every older unused reservation. The latest ledger is [floor-6 confirmation ledger](../TestResults/tower-balance-pass-floor6-calibration-confirmation-owner-20260929/seed-ledger.json), SHA-256 **`9025d61a1a3a18a58069d4f8ed586c328942908333e9cf9ba6584e68d18b04e8`**. For a new phase, authenticate the inherited history and every balance-pass ledger; do not allocate from an older count or omit failed/unused reservations. `TestResults` is ignored local evidence, so verify availability before continuing elsewhere.

Application receipts: [floor 4](../TestResults/tower-balance-pass-floor4-calibration-application-owner-20260929/completion.json), result SHA **`588e56c980bdf6c60de5948a7a06e345e5dfb0f8db092754795795cc305079ab`**; [floor 6](../TestResults/floor6-calibration-application-owner-20260929/completion.json), result SHA **`2a6e83453defc8536270918c348b2c5c1e85565775195c3a4401d2f84cf37fb4`**. Floor 4's application receipt records the intermediate whole-file hash; floor 6's receipt records the final combined file. The later change touches only floor 6's two fields.

### Phase manifest pins

For each phase below, the study directory is `TestResults/tower-balance-pass-{phase}-study-20260929`; its corresponding `-owner-` directory retains the request, source, process record, seed ledger and independent audit. The pin authenticates that study's `files.json`.

| Phase | Fights | Manifest SHA-256 |
| --- | ---: | --- |
| `floor4-calibration-preparation` | 0 | `43f7d6e1bb5b86104b91ad55b18c15c45f6d2fb1f49618d4f836150908843dbc` |
| `floor4-joint-2` | 672 | `5ec4307da2e5bfe2fc96541e182ebfffb04b7f4382878fb37f74c8ed7b40053e` |
| `floor4-joint-4` | 672 | `960dd770c36a2be77f203e31f59c4c3a16a77dad74c232d6180c62e4b5e481ea` |
| `floor4-joint-150` | 672 | `bce1dd0302a770bd7248e00b9f8e0bdb71b7f0edf3b6ccbe6509c02527dd6ed0` |
| `floor4-joint-175` | 672 | `a171bf1fd5f0b15ea4f36a233f68eaef76677cdf6d4f1851c929c7fbf379c66d` |
| `floor4-calibration-search` | 1,168 | `1cfae6e0eb5c80f7afc98021a11716c7e98443dd9d63f58e90e83e2beb0d74f3` |
| `floor4-expanded-1875` | 1,120 | `02d6448b96783fc938fa325ce5ae5bd805923a8b5e45d2b2d7751ca9e1b2e726` |
| `floor4-expanded-18125` | 1,120 | `e9ecafb1c2a8b8e63071d9251acfc914464d3437aaf53666b79b04b46799c7da` |
| `floor4-expanded-178125` | 1,120 | `17ded0b6da4f1a9fbdfcd7c2acf30a365f7d879961fad80644799ca62fd530ca` |
| `floor4-calibration-confirmation` | 8,960 | `453589a2a99055179e2bec8373d637461af02361cd98955442f7a4f802b2de41` |
| `floor6-calibration-preparation` | 0 | `661cb902d6a7654d4d2206c830496ef80ec753c688f3d2a83a1d4ba81f988ab4` |
| `floor6-joint-2` | 672 | `f741112c172b963a8c97677176a7ca2f515363500b4eee1e855eb732596cab91` |
| `floor6-joint-4` | 672 | `1917352fa3150dd56a392490a96c16867b379eae7d43e494b092d39546bbf680` |
| `floor6-joint-3` | 672 | `7f6f40422932f3bcc54ba4a61821bfe67557d06db3fb4d172bb8ae1db7ac6fb4` |
| `floor6-joint-350` | 672 | `7689a9784f0261f8490a9e2b71342c7dd77141ab8a59651b3211d2df13e7c195` |
| `floor6-joint-325` | 672 | `f792a53e6bc7cb281edd4e5e08d0acc8bddd61788b3ff1d87046094cd964a5cd` |
| `floor6-calibration-search` | 1,168 | `3699a125ffb18a2eb0fbe1861a7cd4c740e301666516d4340c51e5778f489b18` |
| `floor6-expanded-3375` | 1,120 | `c72b7125c0956ec681daba88501d7dc5fe3726e58e38ff95701b670ab76de7bb` |
| `floor6-expanded-34375` | 1,120 | `a46bcc5d8680fcb0e5b27bdfcb1d8a99f193ea0fa2fdea9978cbbfe517ab4b4e` |
| `floor6-calibration-confirmation` | 8,960 | `4e23cb6225b4e72dc07556fa24c5e3184cbce498d611994399e17efd408edb72` |
