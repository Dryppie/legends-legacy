# Floor 5: joint midpoint check — 29 September 2026

**Subsequent result:** [isolated ability-catalog support and the three-candidate damage-mix trial](Tower-Floor5-Ability-Mix-20260929.md) are complete. All three candidates failed the full selection requirements; no game content changed. That report supersedes this document's implementation-next recommendation. The frozen midpoint scope below remains historical.

Target: primary LL World Tower guardian data and offline Balance Harness. Continue the closed [smaller-offset trial](Tower-Floor5-Joint-Refinement-20260929.md): +6% health / −5% offense exceeded the selection ceiling, while the earlier +7% / −5% panel missed the alternate-gear minimum. Test a single midpoint on a larger panel. Different seeds and sampling uncertainty mean these prior observations are not a measured interpolation curve.

**Completed: `NoEligibleGearSetting`.** The midpoint failed both the strongest-recipe ceiling and the two-composition minimum. No confirmation or application ran. Live floor-5 health/offense remains **3.3102803755 / 4.4702934848**. All **103 exact recipes / fourteen compositions / seven gear profiles** remain retained. This scope completed **13,184 fights / 128 fresh reservations**, ending at **911,095 exclusions**. No active study remains. The reassessment below replaces further fractional health adjustments as the next recommendation.

## Frozen protocol

1. Authenticate preceding evidence `TestResults/tower-floor5-joint-refinement-evidence-20260929.json`, SHA `3adf37f41be75ff6725b442bf68122b7a103833a2f033954b9716c76ad97a190`, and its `NoGridCandidate` closure. Start from **910,967 exclusions**, live health/offense **3.3102803755 / 4.4702934848**, Tower SHA `aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`. Reuse authenticated 91 backend passes / four opt-in skips and fourteen Python passes at entry; explicitly label them reused. Run nineteen fresh selection safeguards. Use preserved assemblies without rebuilding and retain the documented source LF-to-CRLF entry exception exactly.
2. Keep all **103 exact recipes / fourteen real compositions / seven gear profiles** from `TestResults/tower-balance-pass-floor5-alternate-gear-expanded-screen-study-20260929`, manifest `46d30356581049e9e95528739ce3291dbc4caa13ff85b8f185e41d4da77a4f80`. Preserve raw Essence order, IDs and positions; count actual compositions by per-slot Essence sets. No searches or recipe imports. Player budget stays ten level-40 characters, five level-1 unascended/unevolved Essences, tier-1 Epic/Fine/rank-3 gear, baseline rolls and no styles. Complete ownership remains hypothetical.
3. Prepare this family from current content with zero fights. Test exactly **+6.5% health / −5% offense**, rounded to ten decimals: **3.5254485999 / 4.2467788106**. No other content changes. One **128-fresh-seed / 13,184-fight selection panel** requires every recipe at most **44/128**, at least two real compositions with a recipe at least **26/128**, and qualifying recipes on both **resistance-and-health and health-and-regeneration**. A failed panel closes the scope without a second candidate or another stability panel.
4. If eligible, freeze this setting and run one independent **184-seed / 18,952-fight complete-family confirmation**. Require approximate simultaneous 95% Bonferroni-Wilson upper bounds at most 50% for every recipe, at least two real compositions with a lower bound at least 10%, and qualifying recipes on both required gear profiles. The corresponding qualifying count range is 33–68/184; weaker recipes can remain below 33 if coverage requirements hold. Native one-composition Pass alone is insufficient. Do not pool previous, selection or confirmation results.
5. Apply the two floor-5 scalars locally only after full acceptance. Verify all **18,952 confirmed inputs and 103 complete replays** against applied content, and rerun the relevant backend regression through `build/run-tests.ps1`. Preserve every other floor, budget and guardian field. No deployment, migration, configuration change or shared database operation.

Maximum **32,136 study fights + 103 conditional replays = 32,239 executions**, **312 new reservations**. Each scientific phase stays within 20,000 fights / 840 native seconds / 900 owner seconds / 2 GiB, with 80% projected time/output admission before allocation. Application checks retain 600/660-second limits. Fresh paths use `floor5-joint-midpoint`. Preserve all archives and unused reservations; no retries, sample extensions, relaxed gear gates or dropped recipes.

If this midpoint fails, reassess the joint tuning approach from existing evidence before further fractional adjustments. No automatic follow-on grid, new search algorithm, repeated diagnostic combat, dungeon work or equipment supply is included. The old 89-cell confirmation remains historical evidence and does not establish acceptance for the current 103-cell family.

Driver, application, collector and checks are frozen under `TestResults/tower-floor5-joint-midpoint-*-20260929.py` before execution.

## Result

The isolated candidate used health **3.5254485999** and offense **4.2467788106**. Every exact recipe received the same 128 fresh seeds; none was removed or replaced.

| Selection requirement | Observed result | Decision |
| --- | --- | --- |
| Every recipe at most 44/128 wins | Strongest resistance-and-health recipe: **58/128 (45.31%)** | Failed |
| At least two actual compositions with a recipe at least 26/128 | Best composition: **58/128**; second: **23/128 (17.97%)** | Failed: only one |
| Resistance-and-health viability minimum: 26/128 | **58/128** | Minimum met; ceiling still failed |
| Health-and-regeneration viability minimum: 26/128 | **42/128 (32.81%)** | Minimum met |

The two gear leaders share composition `6b30bf986f2fe6fa34e905a84a1947ceb5198bee155e191742d7005307f85dca`; gear variants do not count as additional compositions. Composition `bc569faffb22731bb55b3aac8eb162fb809a3898685da2490258b3b81947efa6` leads the remainder at 23/128. The fourteen composition leaders won **58, 23, 13, 9, 8, 5, 3, 1, 0, 0, 0, 0, 0, 0** times. These are selection observations, not an accepted confirmation or a claim that the strongest true win rate exceeds 50%.

The frozen failure branch closed this scope. No second candidate, stability panel, independent confirmation, application replay or post-application regression ran. No failed result was pooled with another panel.

## Reassessment and next work

**Stop the fractional health sweep at −5% offense.** The tested settings have alternated between excessive strength for the leading recipe and insufficient coverage. The 128-seed midpoint does not bridge those requirements. Different fresh panels and nonlinear combat prevent a reliable interpolation from these samples; this is not proof that every possible health/offense pair must fail. It is sufficient reason to change the next question instead of choosing another nearby fraction.

The next useful hypothesis is **Kharad's magical versus physical damage balance**. The [completed paired diagnostic](Tower-Floor5-Gear-Diagnostic-Recovery-20260929.md) reproduced 32 historical fights at the unapplied +4.5% health candidate. During the first 40 seconds, mean Seal damage was 4,647.50 with resistance gear versus 7,364.31 with regeneration gear; actual regeneration was 2,490.19 versus 4,451.31. The extra 2,716.81 magical damage exceeded the extra 1,961.12 regenerated health in that window, before any original character died. This supports examining the pulse directly. It does not establish a coefficient, guarantee two viable compositions, or transfer those diagnostic outcomes to the midpoint candidate. Whole-battle totals remain confounded by different fight lengths; regeneration gear did not have an earlier average first death across all saved pairs.

A read-only [ability catalog](../LL/src/API/API.LL/Data/combat/abilities.json) review identifies two existing data fields:

| Effect | Current behavior | Potential controlled variable |
| --- | --- | --- |
| `effect.creature.kharad.seal_of_ascension.pulse` | Magical Power coefficient **0.5**, all enemies, every 20 ticks for up to 100 ticks while its linked barrier holds | Pulse damage coefficient |
| `effect.creature.kharad.crushing_verdict.damage` | Physical Power coefficient **2.6**, highest-MaxHealth enemy, 140-tick ability cooldown | Physical damage coefficient |

Seal's barrier remains **0.05 × MaxHealth**, its ability cooldown 160 ticks. Whole-guardian offense scales Power across attacks, while health also scales that barrier; neither scalar isolates the damage mix. The API catalog reference check found Seal only in Kharad's creature profile and Kharad's guardian ID/profile only on Tower floor 5. This is a repository-content check, not an audit of external database contents.

The next implementation should add a narrowly declared **isolated ability-catalog candidate** path to the offline study owner and its independent verification. The current [owner](analysis/run-tower-balance-pass.py) exposes only health/offense factors and correctly rejects changed source catalogs during current-content preparation. Preserve that protection: authenticate the unchanged live source and all 103 recipes, record exact allowed effect IDs and before/after coefficients, verify that unrelated catalog fields and every other floor remain unchanged, and reject undeclared changes before allocation. Existing native requests already accept an isolated content root; assess reuse before adding another simulation path. Use shared data-driven effect handling, with no boss-specific combat code.

Only after that support and its corruption/regression checks pass should a separate bounded protocol freeze candidate coefficients and fresh selection/confirmation limits. Keep both required gear profiles, two actual compositions, the strongest-recipe ceiling and the complete retained family. A potential direction is reducing Seal's magical contribution while adjusting physical pressure; no coefficient pair has been selected or validated. Keep timing, targeting, barrier and Resonance mechanics unchanged unless a future protocol explicitly justifies changing them. Acceptance must precede local application; then verify all confirmed inputs, full recipe replays, affected ability behavior/description consistency and backend regressions through `build/run-tests.ps1`.

No new ability candidate, search, diagnostic fight, game edit or reservation was created by this reassessment. The supported search remains `affinity-creation-with-benchmark-validation-v1`; expected gear progression, hypothetical ownership and the supply withdrawal remain unchanged. Ordinary acquisition and broad archetype coverage are still separate gaps.

## Evidence and verification

- Evidence: `TestResults/tower-floor5-joint-midpoint-evidence-20260929.json`, SHA **`163e6ed4c2fdbd56e4f5746dc388e5f20b6840e20037b0482340ca8d8e6ebbbf`**.
- Preparation: `TestResults/tower-balance-pass-floor5-joint-midpoint-preparation-study-20260929`, manifest **`ce80517fa45dad479b7956fccdc56bca914b0008dd4dfba3ea4dc92046ea8938`**, independent audit **`28e4b76b4d034c28c4c11c25f8735f6650f6a3808a3bbd79991705cd8515d765`**.
- Selection: `TestResults/tower-balance-pass-floor5-joint-midpoint-selection-study-20260929`, manifest **`f7471dd3ee9ba9c3352c3cfb194a93bd4a02cb57fbd467aa205457c449dd46b1`**, independent audit **`10b165c95a4fb780cc21f128fbbfdfe2c6b391b941ce02ae5432c5b851ac46b7`**. Its isolated content is rejected candidate evidence, not live content.
- Latest ledger: `TestResults/tower-balance-pass-floor5-joint-midpoint-selection-owner-20260929/seed-ledger.json`, SHA **`2db64dec84bc5198177e4d73f01711d5ee29b8a08c3aa6072190c5e81bf5f724`**. Exclusions rose **910,967 → 911,095**; retain every ancestor and unused reservation.
- Nineteen fresh selection safeguards and two owned native fixtures passed; both process receipts show exit zero and zero remaining descendants. Native preparation plus selection took **320.4234345 seconds**. The collector checked **664 unchanged input/runtime pins**, the full recipe family, frozen decisions and seed accounting.
- The prior **91 backend passes / four opt-in skips** and **14 Python passes** were authenticated and reused, not rerun. No post-application suite was needed because application did not occur. The owned phases use `build/run-tests.ps1`; their exact commands and logs remain in the control archive. Conditional confirmation/application commands were intentionally not run after failed selection; no command is blocked.
- Publication verification is recorded separately in `TestResults/tower-floor5-joint-midpoint-publication-check-20260929.json`; it authenticates every new study member, input/runtime pin and local link in the updated documents.

The live Tower file remains SHA **`aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`**. No gameplay source/catalog, configuration, dependency, migration, shared database or deployment changed. The report, current status, handoff and two harness guides carry the new closure; frozen execution artifacts remain untouched.
