# Saved combat diagnosis completed

15 September 2026. **The missing-value bug is corrected and all eight fixture tests plus the independent saved-data verification pass.** The review covers 1,024 archived fight records, 384 confirmation observations, 136 measured teams and 1,360 character loadouts. It ran **zero new fights, replays, preparations, generated candidates or seed allocations**. All **482,416 reservations** and **3,499 indexed files** across eight source packages are preserved, including the [failed first analysis](Tower-Joined-Mechanics-Combat-Diagnosis-Review.md).

The substantive finding is a remaining exploration gap: joined construction placed more catalogue groups, but sampled them sparsely and the selected joined finalist contains **none of the 214 groups**. The controls' saved gameplay inputs and prepared actors match across studies; their combat statistics remain broadly similar despite the new 0/32 outcomes. The completed comparison still provides no supported win-rate improvement or reliability recovery. Adoption remains **Hold**.

## The controls did not receive different saved gameplay inputs

Both controls have identical ordered Essence/equipment recipes after excluding scenario ID, descriptive assumptions and seed schedule. Materialized inputs match after excluding the recipe hash and normalizing the combat seed. Each control's prepared participant objects and hashes match exactly across all of its old/new confirmation records, including neutral identities, attributes, gear and resolved abilities. All **16 archived content files** and **24 of 25 executable-directory files** match; only `BalanceHarness.dll` differs between the two policy drivers. The gameplay dependencies, including `Services.LL.dll`, match. The independent check verifies both file-set equality and those hashes.

Manual inspection of the sealed metadata also finds the same settings hash for both controls in both studies, and identical captured sources for TowerBattleRunner, TowerPreparedBattle, IdleBattleInput and OfflineContent; exact hashes and source observations are retained in [source-review.md](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-v2-20260915/source-review.md).

The old 64-seed and new 32-seed confirmation schedules are disjoint. Scenario IDs/assumptions differ deliberately between studies; within the joined comparison both policies and controls use the same scenario identity. The captured harness sets explicit combat seeds and fresh per-trial runtime state. Prepared descriptions are not the entire runtime, and this review performed no same-seed replay. These checks find no saved content, equipment, recipe or prepared-actor drift; they do not prove that every unrecorded runtime influence is identical or that seed variation is the sole explanation.

| Control | Old → new wins | Mean boss health remaining | Mean duration | Closest new attempt: boss health remaining |
| --- | --- | --- | --- | --- |
| control-1 | 7/64 → 0/32 | 24.01% → 25.90% | 92.80 → 92.85 s | 2.55% |
| control-2 | 4/64 → 0/32 | 30.24% → 25.52% | 90.93 → 94.34 s | 0.16% |

The new controls still do much more damage than either selected search team: the baseline finalist leaves **86.22%** mean boss health and the joined finalist **78.16%**, versus roughly 26% for both controls. The second control's average remaining boss health improved while its observed wins fell from 4/64 to 0/32. Thus “all zero wins” conceals meaningful differences and does not by itself show that the controls stopped functioning.

Low underlying win rates with different small seed samples are a plausible explanation for the zeroes. This is an inference, not a proved causal attribution or a new significance test. The previous adjusted intervals remain the relevant uncertainty statement; no results are pooled, finalists reselected or claims of equivalent strength made.

## Saved counters show activity, not exact interaction timing

All counters below sum the original ten characters and average per fight. Damage can include attacks against summoned enemies; it is not attributed exclusively to the boss. A counter's `uses` field is not automatically a count of downstream effects.

| Control | Recorded counter | Old mean / fight | New mean / fight |
| --- | --- | ---: | ---: |
| control-1 | Coordinated Attack: uses | 47.34 | 46.53 |
| control-1 | Royal Venom: totalDamage | 4081.31 | 4024.69 |
| control-1 | Toxic Opportunity: totalDamage | 1659.84 | 1622.16 |
| control-2 | Coordinated Attack: uses | 44.75 | 46.50 |
| control-2 | Royal Venom: totalDamage | 3637.31 | 3868.75 |
| control-2 | Toxic Opportunity: totalDamage | 1485.53 | 1596.78 |

The Howler/Royal Venom/Spiderling abilities remain active in both control samples. Selected joined groups also produced recorded activity: proposal 0, character 8 (Howler + Royal Venom + Wind Harpy) records 6 Coordinated Attack uses, 6 Royal Venom uses and 12 Sharp Feathers uses across its four saved discovery fights. Proposal 38, character 7 (Giant Bat + Howler + Royal Venom) records 8 Coordinated Attack, 8 Royal Venom and 12 Resonant Cry uses. All **71 inserted-owner activity rows** were independently recounted. These examples show registered activity in assembled groups; the archive cannot establish that a particular trigger caused a later event or quantify a synergy's benefit. Full event logs were not captured.

## More assembled groups, little repetition and no group in the finalist

“Owner” means one character. The floor uses ten characters arranged as two five-player parties, with five Essence slots per character. Counts across candidates are descriptive observations from an adaptive search, not independent samples of strength. The old pilot used a different seed set; even the two current policies have separate generation streams.

| Search | Evaluated teams | Owners containing any catalogue group | Most owners sharing one group in a team | Owners with any 3 shared control ingredients | Owners with all 4 |
| --- | ---: | ---: | ---: | ---: | ---: |
| pilot | 48 | 38/480 | 2 | 0 | 0 |
| baseline | 44 | 35/440 | 5 | 8 | 0 |
| joined | 44 | 146/440 | 3 | 3 | 0 |

The shared control ingredients remain Enchanted Fairy, Pack Howler, Royal Venom and Venomous Spiderling, defined descriptively as present on at least 8/10 owners in both controls. Both controls place the Howler–Royal Venom–Spiderling triple on seven owners; the complete four-set appears on seven and six owners. **None of the 136 generated teams contains that triple or any complete control loadout.** These are useful coverage comparisons, not recipes to feed into independent generation.

The current joined arm produced 19 fresh proposals: 17 guided traces with 170 owner visits, plus two uniform routes. The guided visits record **71 joined insertions spanning 56 distinct groups**, 14 small-core fallbacks, 84 skipped owner visits and one slot-limit rejection. Of the 71 joined insertions, 22 started with two free slots, 31 with three, 14 with four, one with five and three with one; overlap with already reserved ingredients allowed some larger groups to fit.

The Howler–Royal Venom–Spiderling triple was legal at **84 recorded owner visits**, including **40 visits where group insertion was sampled**, yet was selected zero times. It is present in the catalogue and is not globally blocked by family or availability rules. These are opportunities within correlated candidate constructions, not 40 independent combat trials. A shallow random sample can miss a legal group; this does not demonstrate a defective random generator.

The joined finalist carries Fairy on ten owners and Howler on nine, but **zero Royal Venom and zero Venomous Spiderling**. It contains no complete joined catalogue group at all. More construction events therefore did not translate into a selected recipe retaining one of those groups. Its lower remaining boss health than the baseline finalist is descriptive; both still won 0/32 and the adjusted paired interval spans zero. No individual module's contribution, library eviction cause or optimal owner count is established by this review.

## Next engineering target

Implement a bounded construction route that **chooses a content-derived group for the team and explicitly varies how many characters carry it**, before filling the remaining slots. Give catalogue groups a deterministic exploration schedule so that repeatedly drawing from large per-owner choice lists is not the only way to reach them. Keep generic coverage and a route to other legal compositions, enforce family/ownership constraints across the complete team, and record group ID, requested/placed owner counts, compatibility failures and final canonical recipes. Keep fixed ordinal ability order and exclude control recipes/outcomes from construction.

This targets the observed gap in group coverage and repetition. The current source reserves coverage providers first, then independently gives each owner a half-chance of one uniformly chosen compatible group. The existing whole-loadout distribution operator can repeat a generated module, but draws from a library ranked by its source team's fitness, not measured module value; the joined arm received only four `loadout-distribute` proposals. It does not provide deliberate group/count coverage. The next change should start with zero-combat fixtures for deterministic group/count scheduling, legal repeated placement, saturation/ownership limits and retained generic exploration. **This route is a recommended next implementation, not implemented or shown stronger by this review.** No extra fight batch is authorized here.

## Reader repair, verification and measured workload

The failing fields were minimum/maximum hostile-summon wave intervals. The old primary has 44 observed and 20 missing interval values; the new baseline finalist has 31 observed and one missing. These fields can be absent when no interval between successive waves was recorded. The corrected reader retains all keys across all fights, counts observed/missing values explicitly, preserves real zeroes, rejects nonnumeric/nonfinite values, and emits no numeric summary for all-missing observations. Its means are conditional on observations existing.

All **8 Python fixtures** pass, including intentional corruptions rejected by the independent recount. Saved-data verification checks **450 telemetry-field summaries** across all eight confirmation cells and their nonempty win/loss strata, 1,024 unique stored records/schedules, control inputs/preparation, content/dependency equality, all 136 candidate feature rows, all eight finalist/control recipes, 214 group coverage rows, 170 insertion visits, 71 owner activity rows and the complete 482,416-value ledger union. The already passing **24/40 backend tests through `build/run-tests.ps1`** and native archive verifications are retained by seal, not claimed as rerun tests. No backend code changed and no backend invocation was needed for these Python/Markdown edits.

| Step | Seconds | Result |
| --- | ---: | --- |
| Freeze / initial preservation | 0.968 | Passed |
| Eight missing-value fixtures | 0.344 | Passed |
| Primary saved-data analysis | 3.266 | Passed |
| Independent verification | 2.797 | Passed |
| Final source-package preservation | 0.859 | Passed |

Additional diagnostic workload is **8.234 / 300 seconds**; cumulative comparison, failed attempt and corrected review total **254.030 / 1,800 seconds**. Zero combat and zero retries within this corrected follow-up. No performance or win-rate speedup is inferred from analysis timing. Output bytes, publication time and final limits are retained in the [completion receipt](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-v2-20260915/completion.json).

Executed once from the repository root:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-joined-mechanics-combat-diagnosis-v2-20260915'
& $py -B "$w/freeze.py"
& $py -B "$w/run.py" fixtures
& $py -B "$w/run.py" analyze
& $py -B "$w/run.py" verify
& $py -B "$w/publish.py"
```

Do not rerun into the sealed output. Reproduction requires a separate location preserving the pinned scripts/inputs and limits. The [protocol](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-v2-20260915/protocol.md) and [authorization](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-v2-20260915/authorization.json) record this corrected follow-up without changing the failed attempt. [Analysis](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-v2-20260915/analysis.json), [control input comparison](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-v2-20260915/control-input-comparison.json), [combat statistics](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-v2-20260915/confirmation-statistics.json), [group coverage](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-v2-20260915/group-coverage.json), [independent verification](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-v2-20260915/verification.json), [file seal](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-v2-20260915/files.json).

Changed files are this new review, four active Markdown handoffs and the separate corrected diagnosis package. The old failure package, experiments and gameplay/harness source are preserved. No command in this follow-up failed or was blocked. No migrations, configuration changes, deployment, boss tuning, new seeds or old-cap changes. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption Hold remain unchanged.
