# Floor 8: limited-armor trial — 30 September 2026

**Latest floor-8 diagnostic (30 September):** The [Kodoku replay diagnostic](Tower-Floor8-Kodoku-Diagnostic-20260930.md) completed **96 exact historical replays / 893,993 audited events**. All first casualties were physical basic attacks (54 Kodoku/42 Venomspawn). Miasma compounds slower regeneration pulses with smaller regeneration amounts; nominal rate is 4% while both -80% effects are active, before other modifiers. Next: the frozen, unallocated trial removing only the extra regeneration-rate effect while preserving 80% healing reduction, all 177 recipes and independent confirmation. **No gameplay edit or new acceptance seeds; floor 8 remains unresolved.** 29 fresh Python checks pass; authenticated 214 backend passes/four skips reused. Exclusions remain **923,900**.

**Limited armor did not qualify.** The declared trial closed **`LimitedArmorScreenNotAccepted`** after **22,656 fresh fights / 128 reservations**. All **177 exact recipes / nine actual compositions** were retained. The final phase has **0 qualifying limited-equipment compositions**, **0 ceiling failures**, and largest adjusted upper **44.87%**. No further acceptance combat or gameplay adjustment occurred. All **110 partial-armor variants and nine baseline recipes won 0/128**; the qualifying range was **26–43 wins/128**. All 102 live catalogs remain unchanged. Exclusions are **923,900**.

Target: primary LL World Tower (`LL/src/API/API.LL`) and offline Balance Harness. Continue the [completed equipment assessment](Tower-Floor8-Gear-Assessment-20260930.md) and its frozen trial proposal. The original 67-recipe family is qualified against the current catalogs and combat runtime; all 177 comparison recipes have prepared natively. No earlier outcomes transfer into this trial.

## Frozen protocol

Authenticate publication `92858cc4d9333d7decb8685a0f31198670bfaa6ea4ed64ebfdbcda378db9c7df` and proposal `b0b239c05f229bd42d6156d439a2f574ccab601e93fa51ac50e6ffd13646db8f`. Preserve pre-edit maintained sources and all **923,772 excluded seeds**. The source is `TestResults/tower-balance-pass-floor8-limited-armor-preparation-study-20260929`, manifest `5092e4c65735416affaf4dab4a083e920d88827d20d6fde325abc29876380ae3`.

Retain **177 exact recipes / nine actual compositions**: all 67 original controls plus all 110 one/two-character armor variants for the two leading compositions. Preserve raw identifiers, ordered Essences, party slots, ten level-40 characters with five Essences each, T1 Unique/Exceptional/rank-4 equipment, roll 1 and no styles. Every new variant uses four or eight specialized items; gear and Essence-order variants cannot add compositions.

**Keep all gameplay unchanged.** Kodoku health remains 9.9064526367, offense 8.8260253906, defense/resistance 2.61 and penetration/regeneration 1.0. Retain all accepted previous floor changes and every shared ability definition.

Add a separate `tower-balance-limited-armor-aggregate-v1` contract for **exactly four complete-family batches of 32 fresh shared seeds per phase**. Preserve the existing four/eight-by-128 contracts. Reject any candidate modification, missing recipe, repeated seed, altered runtime/catalog, incomplete phase or weakened equipment limit. Add matching native application guards. Verify through `build/run-tests.ps1`, then bind the new test assembly to the unchanged qualified production assemblies before allocating combat.

Freeze implementation, protocol, runtime, full family, both phase schedules and all eight fresh paths before allocation. Screen **22,656 fights / 128 reservations**. Decide only after all four batches complete. Require at least two distinct actual compositions with a recipe using **at most eight specialized items on two characters** and adjusted lower bound **≥10%**. Every recipe, including full-party armor controls, must have adjusted upper bound **≤50%**. Use approximate simultaneous 95% Bonferroni-Wilson intervals across all 177 recipes: **26–43 wins / 128**.

Only a complete passing screen permits an independent identical **22,656-fight / 128-seed confirmation**. Each phase stands alone. Maximum scope: **45,312 new fights / 256 reservations**. No prior counts, inter-phase pooling, partial decisions, replacement batches, retries, extension, dropped controls or gameplay adjustments. A failed phase or execution closes this scope.

Each batch has **5,664 fights**. Before allocation, require doubled measured time and bytes below **672 seconds / 80% of 2 GiB**, retaining **840-second native / 900-second owner** limits and the 20,000-fight cap. Require free disk for doubled projected remaining archives plus 2 GiB reserve. Initial reference: the accepted historical floor-8 confirmation, 517.492893 seconds and 342,201,107 bytes for 10,720 fights; doubled batch projection **546.84 seconds / 361,609,528 bytes**. Subsequent batches use the preceding completed batch in that phase. Monitor supervisor stdout only; never open active study or owner files.

Independently reconstruct outcomes, native prepared-participant inputs, equipment counts, composition identities, intervals, resource admissions and the disjoint seed union after the declared phase closes. A passing confirmation additionally requires all **22,656 native inputs / 708 full historical replays** across its four batches to match live content. No synthetic passing per-batch assessment may substitute for complete aggregate acceptance. No data copy is needed because this trial keeps current content unchanged.

On rejection, record the strongest limited-equipment results and every ceiling failure, preserve all evidence/reservations and close the trial. Further diagnosis or balance tuning requires a separate prospective scope. No engine/search redesign, dungeon/acquisition work, duration target, migration, configuration change, database operation or deployment is part of this trial.

## Completed result

**Limited armor did not qualify.** The declared trial closed **`LimitedArmorScreenNotAccepted`** after **22,656 fresh fights / 128 reservations**. All **177 exact recipes / nine actual compositions** were retained. The final phase has **0 qualifying limited-equipment compositions**, **0 ceiling failures**, and largest adjusted upper **44.87%**. No further acceptance combat or gameplay adjustment occurred. All **110 partial-armor variants and nine baseline recipes won 0/128**; the qualifying range was **26–43 wins/128**. All 102 live catalogs remain unchanged. Exclusions are **923,900**.

| Phase | Qualifying limited compositions | Ceiling failures | Largest adjusted upper |
| --- | ---: | ---: | ---: |
| screen | 0 | 0 | 44.87% |

| Composition / equipment | Specialized items / characters | Wins | Adjusted interval | Mean guardian Health remaining |
| --- | ---: | ---: | ---: | ---: |
| A / Baseline | 0 / 0 | 0/128 | 0.00%–9.34% | 49.71% |
| A / Limited armor | 8 / 2 | 0/128 | 0.00%–9.34% | 38.34% |
| A / Full armor | 40 / 10 | 37/128 | 16.88%–44.87% | 11.19% |
| B / Baseline | 0 / 0 | 0/128 | 0.00%–9.34% | 48.89% |
| B / Limited armor | 8 / 2 | 0/128 | 0.00%–9.34% | 40.50% |
| B / Full armor | 40 / 10 | 28/128 | 11.60%–37.40% | 9.88% |

Composition A is original `049002811aed3c4effada8495158670086d9c55a1f900e81239832ac4769f27b`; B is `230ac3d6a65620dd44e949a10dd0083c3ce922069aa051f5ee8fb899bbd2b3ca`. The displayed limited recipes specialize party slots **[3, 7]** and **[3, 8]**. Each is selected descriptively by most wins, then least mean guardian Health remaining, then raw ID. This post-study selection does not change acceptance counts or gates. All 119 budget-eligible recipes and every full-gear control remain in the whole-family assessment.

Read-only review of **768 existing reports** gives the following recipient summaries. They contain no detailed event timeline and do not isolate an ability's causal effect.

| Composition / equipment | Median first casualty | Mean party damage | Mean healing received | Mean regeneration |
| --- | ---: | ---: | ---: | ---: |
| A / Baseline | 60.25s | 80254 | 3748 | 1845 |
| A / Limited armor | 89.00s | 92410 | 4530 | 1841 |
| A / Full armor | 125.05s | 125660 | 6045 | 1572 |
| B / Baseline | 65.95s | 79383 | 3763 | 1991 |
| B / Limited armor | 71.65s | 89224 | 4123 | 2009 |
| B / Full armor | 118.00s | 127610 | 5770 | 1732 |

## Verification and maintained changes

**157 distinct Python cases and 214 distinct backend cases pass**, with **four intentional opt-in skips**, plus **4 completed native study fixtures**. Both a fresh build and the bound runtime pass the same 214 cases; repeated runs are not added together. The new test assembly retains all five exact qualified production assembly hashes. All backend tests ran through `build/run-tests.ps1`.

Added a separate four-by-32 aggregate version, unchanged-catalog/provenance checks, twelve Python safeguards and twenty native guard cases. The original four/eight-by-128 versions remain intact. The initial Python run exposed a temporary test fixture using exclusive-create for deliberate corruption; it was corrected before allocation. The first build could not read the existing NuGet configuration in the sandbox; the build completed with approved access using separate output paths. Failed logs are retained. Neither issue allocated study seeds.

The independent collector recounts every raw outcome, compares every native participant with the previously qualified original or exact mixed-equipment reference, independently reconstructs intervals/compositions/equipment eligibility, and reconciles resource admissions and all reservations. Every owned native process exited successfully without timeout or remaining child. No batch verdict was used as an interim aggregate decision. All current content hashes remain unchanged.

Changed maintained files: aggregate helper, new limited-armor Python safeguards, native aggregate application guards, this report and Tower continuation/status/gear documentation plus both harness guides. The supported search and production combat implementation are unchanged. No migration, environment configuration change, shared database action or deployment occurred. No required verification command remains blocked.

## Next work

Next is a separate **96-historical-replay diagnostic**, proposed and unallocated. Compare baseline, the strongest limited-armor setup and full armor for A/B on the first sixteen declared seeds of the first completed batch. Reconcile event logs with complete saved reports, attribute opening damage and first casualties, and inspect healing, regeneration, summon pressure and guardian recovery. The authored kit includes 80% healing/regeneration suppression and Venomspawn scaling, but summary statistics alone cannot establish their causal contribution. Freeze the diagnostic implementation, exact sample, runtime and resource limits before execution. **No new coefficient is selected and no extra acceptance fight is authorized by this result.**

Floors **8–10 and 12–15**, followed by the final current-version 1–15 sweep, remain unless this result explicitly accepts floor 8 above. The user's expected repeating gear curve, Essence budgets and stronger-equipment carry-forward remain unchanged. No dungeon or acquisition work is included.

## Evidence and commands

- Frozen declaration: `TestResults/tower-floor8-limited-armor-driver-20260930/declaration.json`, SHA `2912c556135972d58a9e448aaa57929affb8e0506db49f7ba988e6de5125a003`.
- Independent evidence: `TestResults/tower-floor8-limited-armor-evidence-20260930.json`, SHA `91e14ac1f743f1dd0679733c66a1fada1704d1304f6261a3fd7df530cb6b1e08`.
- Descriptive review: `TestResults/tower-floor8-limited-armor-review-20260930.json`, SHA `49327d26bb469518615321aeaa747e550cb3794f89b5859574138d30c26a2cba`.
- Current publication: `TestResults/tower-floor8-limited-armor-publication-check-20260930.json`.
- Next diagnostic proposal: `TestResults/tower-floor8-gear-diagnostic-proposal-20260930.json`, SHA `6dd48ba6049092e87ed1dc9cb6a01230290ae548ca3243336b6a2e822caa47f2`.

Executed the bundled Python runtime with `-B -X utf8`: the runtime build/binding scripts, six maintained safeguard suites, the frozen driver, independent collector, descriptive reviewer and publisher. Markdown links and `git diff --check` pass at publication. Completed archive paths are immutable and must not be reused.
