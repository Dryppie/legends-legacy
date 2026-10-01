# Floor 4: penetration with lower offense — 30 September 2026

**Subsequent result — recovery diagnostic completed:** The [recovery diagnostic](Tower-Floor4-Recovery-Diagnostic-20260930.md) completed **80 exact historical replays / zero new seeds** across ten C/D damage/healer/gear recipes. Every complete saved report matched. The health/regeneration profile supplies **3,255 extra starting party health**, while the six-item Restoration healers start with **2,852 total barrier** and absorb **2,820 / 2,847 damage before 20 seconds**. In those eight-seed subsets, damage teams recover only **957 / 935** health through early natural regeneration. A blanket regeneration/healing reduction is not supported by this evidence. The full saved 128-seed matrix remains: damage teams **77/72 wins** on health/regeneration and **0/0** on Restoration; healer variants **11/13** on health/regeneration and **33/34** on Restoration. These are paired descriptive results, not new acceptance. **25 fresh Python diagnostic checks passed**; **108 backend passes / four opt-in skips** were authenticated and reused. No gameplay change; exclusions remain **917,862**. The diagnostic proposed below is now closed. Follow the linked report and current handoff for the unallocated target-health Hall proposal; the penetration trial remains rejected and unmodified.

Target: primary LL World Tower and offline Balance Harness. Follow the [rejected health-percentage targeting trial](Tower-Floor4-Health-Ratio-Target-Trial-20260930.md). Retain the complete unchanged-live-catalog 132-recipe family; no search or loadout changes.

## Assessment and prospective protocol

All 691 preceding publication bindings authenticated before editing. Entry snapshots and a seed-free assessment of all 660 prepared party members are in `TestResults/tower-floor4-penetration-entry-20260930`. The source is `TestResults/tower-balance-pass-floor4-fixed-support-diagnostic-study-20260929`, manifest `60950e8d1e7e6e31feb03e926e12dac57be9244fec763b23b6ceba3972b5d52f`. Its unchanged live abilities are retained; the rejected targeting archive is not the candidate source.

The existing native guardian scaling multiplies both penetration attributes. Attribute rules v18 subtract penetration percentage points from curved mitigation, capped at 40 points and floored at zero mitigation; block and general damage reduction still apply. Vaelor currently prepares with Power **1325.3955** and physical/magical penetration **0.36 / 0.36**.

Test exactly one candidate: **penetration multiplier 1 → 100**, producing **36 / 36** prepared penetration; **offense ×0.68**, changing its authored scalar **5.023125 → 3.415725** and expected Power to approximately **901.26894**. Health remains **2.3231953125**, with all defenses, regeneration, abilities, targeting, durations, cooldowns and other floors unchanged. The large authored multiplier reflects the small starting penetration; it means 36 percentage points, not 100% penetration or defense bypass.

The seed-free formula comparison gives the following unrounded single-hit changes for retained C, before dynamic statuses/corrosion. These are analytical ratios, not measured fight outcomes or a claim of improved wins.

| Party slot | Baseline: physical and magical | Full armor: physical | Full armor: magical |
| --- | ---: | ---: | ---: |
| 1 | +6.12% | +20.19% | +6.12% |
| 2–4 | −17.75% | +14.29% | −17.75% |
| 5 | −4.75% | +17.36% | −4.75% |

Thus the candidate reduces lightly armored damage while adding physical pressure to full armor. It does not reduce every incoming hit: the baseline heavy-armor slot takes slightly more damage, and fully armored characters take more physical damage. Gear also changes health, block, regeneration and tenacity. No win-rate prediction is derived from averaging these ratios. Choose this one material counter-pressure test, with no follow-up coefficient or penetration sweep inside this scope.

1. Add guarded `--penetration-factor` support to the maintained runner. It must require screen/confirm mode, a completed fixed family, unchanged health, lower offense, no ability changes, no gear/Essence projections, no family imports and no current-catalog refresh. A hashed provenance receipt must reconstruct the exact two-field Tower delta and reject all other catalog/settings changes. Existing health/offense behavior remains compatible.
2. Run **12 new penetration safeguards**, **29 reference/family tests** and **37 ability-candidate tests**: **78 fresh Python checks**. Run **ten native penetration/mitigation/scaling/Vaelor cases** through `build/run-tests.ps1 -NoBuild`. Authenticate and reuse the unchanged runtime's **108 backend passes / four opt-in skips**; no C# changes or rebuild.
3. Freeze **132 exact recipes / nine actual compositions** and equipment eligibility before new seeds: at most **eight specialized items on at most two characters**, admitting **54 existing recipes**. Keep every fully armored control. Budget: five level-30 characters, four level-1 unevolved/unascended Essences each, T1 Epic/Fine/Rank-3 gear, roll 1, no styles. Keep the user's repeating progression curve and approved later-floor Essence budgets.
4. Screen the full family on **128 fresh shared seeds**, **16,896 fights**. Use approximate simultaneous 95% Bonferroni-Wilson bounds across all 132 recipes. At least two actual compositions need an eligible lower bound ≥10%; every retained recipe must have upper bound ≤50%. Integer gates: **minimum 25 / maximum 43 wins of 128**. Do not pool historical panels.
5. Only a passing screen permits **150 independent seeds**, **19,800 fights**, at the identical candidate/family. Confirmation gates: **minimum 29 / maximum 53**. Failure closes the scope without retries, replacement seeds, retuning, extension or dropped controls. A passing confirmation requires native current-catalog input/replay parity and appropriate local regression verification before any application.
6. Initial exclusions: **917,734**. Maximum scope: **36,696 fresh fights / 278 reservations**. Each native phase is capped at **20,000 fights, 840 seconds and 2 GiB**, with a 900-second owner. Admit doubled measured source time/storage only below 80% of that envelope. Use fresh paths and preserve unsuccessful output. Independently reconstruct the native outcomes, all prepared-party attributes, guardian Power/penetration, isolated content, eligibility, composition identities, bounds and seed accounting.

No gameplay change is authorized by screening alone. No dungeon, acquisition, supply, migration, configuration, database or deployment work belongs to this trial.

## Verified result

**Closed: `NoEligiblePenetrationConfirmation`.** The candidate established **two eligible compositions in the screen**, but **three health/regeneration controls failed the ceiling**, so it is rejected as a complete balance setting. All **132 recipes / nine actual compositions** ran on **128 fresh shared seeds**, totaling **16,896 fights**. No confirmation or application ran. Live Vaelor and all other floors are unchanged.

The two screen-qualified recipes are the existing **D and C healer variants**, each using **six specialized Restoration items on slot 2** and ordinary gear elsewhere. They meet the frozen maximum of eight items on at most two characters. Their observed win rates are **26.56% and 25.78%**; their simultaneous adjusted lower bounds are **15.26% and 14.67%**, above the 10% minimum. These are related supported poison compositions, not evidence of broad archetype diversity, independent confirmation or ordinary ownership of the gear.

For each actual composition, the following is its best eligible retained recipe. Eligibility covered **54 of the 132 recipes**, frozen before new seeds. Approximate simultaneous 95% Bonferroni-Wilson bounds use **all 132 recipes**, including controls outside the gear limit. The eligible minimum requires **25/128 wins**.

| Best eligible recipe | Wins | Adjusted interval |
| --- | ---: | ---: |
| D healer: six-item Restoration | 34/128 | 15.26%–42.07% |
| C healer: six-item Restoration | 33/128 | 14.67%–41.25% |
| B: armor on slots 1 and 5 | 7/128 | 1.57%–17.37% |
| D: armor on slots 2 and 4 | 7/128 | 1.57%–17.37% |
| C: armor on slots 2 and 4 | 7/128 | 1.57%–17.37% |
| A: armor on slots 1 and 3 | 4/128 | 0.63%–14.04% |
| Original reference 2: baseline | 2/128 | 0.19%–11.64% |
| Original reference 3: baseline | 0/128 | 0.00%–8.98% |
| Original reference 1: Restoration | 0/128 | 0.00%–8.98% |

The ceiling requires every retained recipe's adjusted upper bound ≤50%, equivalent to at most **43/128 wins**. The only failures were:

| Control | Wins | Adjusted interval |
| --- | ---: | ---: |
| C: health/regeneration | 77/128 | 44.54%–73.95% |
| D: health/regeneration | 72/128 | 40.81%–70.57% |
| B: health/regeneration | 67/128 | 37.16%–67.10% |

Each failing health/regeneration profile uses **15 specialized items across all five characters**. Those controls remain mandatory even though they cannot establish the limited-equipment minimum. Full-armor C/D scored **23/128 and 25/128**, both within the ceiling; all full-armor controls passed that ceiling. The strongest health/regeneration observed rate was **60.16%**, with adjusted upper bound **73.95%**. Full-armor is no longer the failing ceiling profile in this candidate, but the encounter is still not accepted. Different preceding candidates used different seed panels; count differences are unpaired and must not be reported as a causal effect size. Historical outcomes were not pooled.

This is a useful lead for the next bounded investigation: two existing builds can satisfy the limited-specialization minimum under a materially different pressure mix. It does not justify discarding the regeneration controls, applying the candidate, increasing the gear allowance, repeating the closed search, or claiming the original two-armored-character damage teams now work. Their best eligible results remain only **7/128** for B/C/D.

## Implementation and verification

Changed maintained code: `analysis/run-tower-balance-pass.py`; added `analysis/test-tower-penetration-candidate.py`. The runner accepts **`--penetration-factor`** from 1 through 128 only as a factor on the authored multiplier. A non-default factor requires a completed source, screen/confirm mode, lower offense, unchanged health, no ability plan, no source imports, no gear/Essence projections and no current-content refresh. Existing health/offense-only behavior remains compatible, with its previous factor limits unchanged. This capability does not change native attribute caps.

The runner materializes only an isolated catalog copy and writes a hashed `penetration-candidate-provenance.json`. Native outcome auditing re-authenticates the source, reconstructs the exact offense/penetration delta on one floor, and rejects unrelated Tower fields, other catalogs, catalog additions/removals, settings changes, mixed provenance or altered recipes. A candidate archive cannot be used to compound another candidate; redeclare from the original unchanged source. No combat-engine, C#, search-policy or ability changes were made.

**Twelve new safeguards passed**, covering source preservation, compatibility, finite/bounded factors, missing/invalid authored multipliers, overflow/no-op rounding, permitted modes, incompatible modifications, unrelated floor/field drift, catalog bytes/set equality, settings and malformed/no-op plans. **29 existing reference/family tests and 37 ability-candidate tests** also passed: **78 fresh Python checks**. **Ten fresh native mitigation/penetration/scaling/Vaelor cases** and **one complete-family study fixture** passed through `build/run-tests.ps1 -NoBuild`. The unchanged runtime's **108 backend passes / four intentional opt-in skips** were authenticated and reused, not rerun. No C# rebuild was needed.

The native candidate prepared with **Power 901.269**, physical/magical penetration **36 / 36**, and unchanged health **4270.9517**. The independent collector checked **every prepared participant in all 16,896 fights** against that recipe's original native preparation: all party fields remain exact, and only the three intended guardian combat attributes differ (Power and the two penetration attributes). This verifies candidate preparation; it is not the unrun current-catalog application/parity stage.

The screen completed all **16,896 attempts / fights** in **169.15 native seconds**, archiving **205,853,079 bytes**, within the declared 840-second / 2-GiB envelope. The owner exited successfully with **zero retries, no timeout and zero active children**. The collector independently recounted all outcomes and checked eligibility, actual compositions, intervals, verdict, content, settings/runtime, resource admission and seed accounting. Exclusions increased **917,734 → 917,862**. No repeats, replacements or extensions were used.

All **691 preceding publication bindings** authenticated before editing; the prior runner is archived in `TestResults/tower-floor4-penetration-entry-20260930`. Previous publications, proposals and native archives remain unchanged. The driver froze the prospective protocol before allocating seeds. Markdown links and `git diff --check` pass at publication. No required command was blocked. Conditional 150-seed confirmation, native application parity and live application were intentionally not run because the screen failed its ceiling.

All **102 live JSON files and six runtime/test assemblies** remain unchanged. Live Vaelor health/offense/penetration remains **2.3231953125 / 5.023125 / 1**; Mirror Lance physical/magical remains **2.00/1.00**, Hall magical/physical **0.50/0.50**, with original absolute-health targeting. The supported search remains **`affinity-creation-with-benchmark-validation-v1`**. The expected repeating gear curve, approved Essence progression and previous applied floor changes are preserved. No configuration, migration, database or deployment changes.

Added this report, safeguards, seed-free mitigation assessment and fresh driver/collector/evidence; updated the continuation handoff, balance status, gear coverage, preceding targeting report and both harness guides. **No accepted gameplay improvement was applied in this scope.**

## Next Tower work

Next priority: diagnose the **health/regeneration ceiling failures while preserving the two six-item Restoration healer builds**. Use a finite set of detailed native replays from the saved penetration panel to compare the original and healer C/D teams on both Restoration and health/regeneration gear, retaining full-armor controls and matching seeds. Quantify effective regeneration, active healing, incoming damage and first casualties before choosing another encounter adjustment. The summary archive does not establish that mechanism; do not infer it from win totals or silently equate gear regeneration with active healing. This candidate is closed: no confirmation, repeat screen, blind offense/penetration sweep or dropped control. Keep all **132 recipes**, the two-actual-composition minimum, **eight specialized items on at most two characters**, and every-recipe ceiling. Any later candidate needs its own frozen scope, resource admission, fresh complete-family screen, independent confirmation and native parity. No next candidate, replay subset or fresh seeds are selected; no active study remains. Dungeons, supplies and acquisition stay outside this work.

The candidate archive below can supply the exact inputs and saved seeds for a declared replay diagnostic at this rejected setting. It must not replace the **unchanged-live-catalog family source** for a later candidate: `TestResults/tower-balance-pass-floor4-fixed-support-diagnostic-study-20260929`, manifest **`60950e8d1e7e6e31feb03e926e12dac57be9244fec763b23b6ceba3972b5d52f`**, audit **`9f393b8dd8cce16c06811964b811b18c9c5ead743f265947a03fa17b15c82e3f`**. Keep all 132 recipes and independently include the newest ledger, even when starting from that earlier source. Do not use smaller 116/98-recipe families or resume a closed driver.

Before any replay work, authenticate the candidate archive, unchanged runtime/settings and the complete saved family, select the finite paired recipes/seeds without outcome-dependent replacement, and check the diagnostic's resource envelope. Detailed events are needed to separate damage, regeneration and active healing. Outcome summaries alone cannot identify whether a proposed pressure change will preserve the healer variants while bringing the stronger regeneration teams below the ceiling.

## Evidence and commands

- Independent evidence: `TestResults/tower-floor4-penetration-evidence-20260930.json`, SHA **`7400ddae5aad212fa342f5e35bb713b91d8920dc45b4ff7b1673aa0b452353c5`**.
- Frozen declaration, candidate, eligible recipes, prospective protocol, commands, resource admission, native test receipts and completion: `TestResults/tower-floor4-penetration-driver-20260930/`.
- Rejected measured screen: `TestResults/tower-balance-pass-floor4-penetration-screen-study-20260929`, manifest **`d78678a49e87c6f80a823860a4f960a37a1b51916ef6d9488cea23ca61ac9cd1`**, audit **`ef72f68c7162de52975f356544f61ce6ea69d286b131eec6de3dbfc5af42aed3`**.
- Latest ledger: `TestResults/tower-balance-pass-floor4-penetration-screen-owner-20260929/seed-ledger.json`, SHA **`2e5e2e944052e6c8cf2cdcbc1d8d9dd2ac82fd938837d78309c4b977ef8ee44f`**, plus all ancestors; exclusions **917,862**.
- Entry and 660-party-member analytical assessment: `TestResults/tower-floor4-penetration-entry-20260930/`. This analysis did not allocate seeds or run fights.
- Current Tower SHA **`0d416f3cbbf24f7b856fad129e13efcf8ed4a9d5c7677d4bc4b6c51a5061c5e6`**, abilities SHA **`169b61f23c2e1e301e64e176962bbe3dc8386ea941a87a7f5a8205c872963d6a`**. Runtime: `TestResults/tower-floor2-precision-runtime-20260930`.
- Current publication: `TestResults/tower-floor4-penetration-publication-check-20260930.json`. Authenticate its five current pin groups; older document/runner hashes remain historical.

Executed once with bundled Python using `-B -X utf8`: the three test files listed above, `TestResults/tower-floor4-penetration-prepare-scripts-20260930.py` (fresh script creation only), `TestResults/tower-floor4-penetration-driver-20260930.py`, and `TestResults/tower-floor4-penetration-collect-20260930.py`. Exact `build/run-tests.ps1 -NoBuild` commands and owned-process receipts are archived in the mechanic-test and control directories. Publication runs relative Markdown-link validation and `git diff --check`. No active combat work remains.
