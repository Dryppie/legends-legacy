# Tower Essence build search — 9 September 2026

The harness now investigates whether a legal reference party is a useful build before interpreting losses as evidence for boss tuning. Tower Lab's **Search Essence builds** button runs a bounded discovery and confirmation workflow. It generates current-content profiles automatically and preserves the original balanced party as its control.

## Fixed budget and hypotheses

All 15 released floors use the existing `tower-curve-v1` balanced composition: Guardian, Restorer, two Strikers and Controller, repeated to fill RequiredSlots. The curve supplies four through ten Essences per character. Every alternative keeps its floor's level, gear, equipment ranks/rarity/quality, Essence level and party composition unchanged. Floor 1 uses level 30, four unascended level-1 Essences and Uncommon Standard tier-1 rank-1 gear, within the user's intended entry range. Floor 10 uses six Essences and provisional Uncommon Standard tier-2 rank-2 gear. Other progression and ownership assumptions remain provisional.

| Bit value | Role | Replace | With | Hypothesis and tradeoff |
| --- | --- | --- | --- | --- |
| 1 | Guardian | Hobgoblin | Horned Wolf | Ally-dependent armor and physical-defense reduction, retaining Transparent/Brown Slime protection; trades the existing Vulnerable package. |
| 2 | Restorer | Goblin Shaman | Pack Howler | Living-ally Power scaling for retained Power-based healing and coordinated basic attacks; trades Recovery and a heal-over-time ability. |
| 4 | Both Strikers | Pixie | Dire Wolf | Physical Bleed and outnumbering-dependent critical chance with the same gauntlets; trades magical area damage and critical-damage buildup. |
| 8 | Controller | Giant Bat | Wood Nymph | Cover, barrier and Renewal/Ward alongside retained Enchanted Fairy control and Frost Imp Chill. |

All 16 combinations are tested, including each swap alone. Candidate IDs are the zero-padded decimal sum of enabled bit values; `candidate-00` is the unchanged control. For example, `candidate-12` changes both Strikers and the Controller. Every generated floor/candidate party passes production preparation. These hypotheses follow the current authored effects; synergy is something to measure, not a guarantee that a recipe is good.

## Selection and confirmation protocol

The protocol was recorded before the browser started measurement. Discovery uses master seed **20260911**, eight trials for each of 16 candidates on 15 floors: **1,920 battles**. Rank equal-budget candidates by total wins, then lower mean remaining guardian health, higher party survival and stable ID. Select the unchanged control plus two noncontrol generalists. Add up to three distinct per-floor winners, ordered by discovery win gain over control and floor number. This bounds the shortlist at six.

Save `selection.json` before any confirmation fight. Confirmation uses master seed **20260912**, 30 trials per shortlisted candidate on every floor. The actual generated seed sets must be disjoint, and candidates share the same per-floor seeds within each stage. Source content, selected nonsecret settings, executable identity, materialized builds and budgets stay fixed between stages. There is no post-confirmation reselection or automatic baseline promotion.

The search report is regenerated from verified benchmark bundles. It rejects altered selection, mismatched content/settings/execution, changed recipes, missing evidence and overlapping/unpaired schedules. Draws are nonwins. Gained/lost wins compare the same seeds against the control. The JSON includes the existing approximate 95% Bonferroni-Wilson paired-difference interval, which does not adjust for all candidates/floors examined. Results remain descriptive; the starter 50–90% target is not applied.

## Measured results

The browser button completed **1,920 discovery and 2,700 confirmation battles**, with no invalid/cancelled/missing trials. The frozen shortlist was `00, 05, 07, 11, 01, 03`. Discovery generalists 05 and 07 won 85/120 and 83/120 respectively, versus control 00 at 58/120. Candidates 11, 01 and 03 entered through the floor-winner selection. A saved hash confirms that selection did not change during confirmation.

Every entry below is **wins out of 30 reserved-seed trials**, in candidate-ID order. Draws are excluded from wins.

| Floor | 00 control | 01 Guardian | 03 Guardian + Restorer | 05 Guardian + Strikers | 07 Guardian + Restorer + Strikers | 11 Guardian + Restorer + Controller |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 22 | 30 | 30 | 30 | 30 | 21 |
| 2 | 6 | 17 | 15 | 26 | 25 | 14 |
| 3 | 5 | 30 | 28 | 29 | 29 | 22 |
| 4 | 0 | 0 | 0 | 0 | 0 | 0 |
| 5 | 30 | 30 | 30 | 30 | 30 | 30 |
| 6 | 30 | 30 | 30 | 30 | 30 | 30 |
| 7 | 0 | 0 | 0 | 0 | 0 | 0 |
| 8 | 30 | 30 | 30 | 30 | 30 | 30 |
| 9 | 30 | 30 | 30 | 30 | 30 | 30 |
| 10 | 30 | 30 | 30 | 30 | 30 | 30 |
| 11 | 16 | 20 | 21 | 26 | 26 | 11 |
| 12 | 0 | 5 | 1 | 13 | 6 | 3 |
| 13 | 0 | 0 | 1 | 1 | 3 | 1 |
| 14 | 13 | 30 | 30 | 30 | 30 | 30 |
| 15 | 6 | 23 | 23 | 25 | 27 | 19 |
| **Total / 450** | **218** | **305** | **299** | **330** | **326** | **271** |

Control had one draw, candidate 01 had two, and candidate 05 had one; other nonwins were defeats. Candidate 05 remains the leading tested generalist, adding 112 wins across the fixed all-floor matrix. This total weights every floor equally and is not an estimate of a player's overall journey success probability. Candidate 11's 8/8 discovery wins on floor 15 became 19/30 in confirmation, illustrating why discovery winners require reserved seeds.

For candidate 05 against control, floor 2 gained 21 paired wins and lost one; floor 12 gained 13 and lost none. Their nominal paired clear-rate differences are +66.67 percentage points (approximate 95% interval 29.97–84.24) and +43.33 points (11.15–63.08). The floor-1 30/30 result demonstrates observed beatability but does not guarantee a 100% clear rate; even its paired improvement interval includes zero at this sample budget. No simultaneous significance or balance acceptance is claimed.

Floor 1's stronger builds meet the stated four-Essence/Uncommon entry budget without additional gear. All six finalists won 30/30 at the six-slot floor-10 checkpoint. Floors 4 and 7 remain at zero wins across confirmation, while floor 13 has only rare wins. These are priorities for additional counter-build hypotheses and budget review, not immediate boss nerfs. The shortlist is retained as evidence; no original catalog is replaced with a discovered winner.

## Diagnostics and interpretation

Reports include candidate substitutions, the complete discovery ranking, per-floor confirmation results and role-level damage, healing, damage taken, attention, stagger and first-death timing. Role means are per original character/trial and exclude summons; first-death means condition on characters that died. These totals help locate failures but do not establish causal ability value. Detailed verified replay retains ability statistics and combat events.

A strong result means the candidate performed well under the declared budget and tested encounters. It does not certify an optimal build, player skill, accessibility or the wider metagame. This first search holds composition fixed and investigates four authored substitutions, not every legal Essence, order, progression level, equipment mix or counter-build. All Essences are assumed owned and remain level 1/unascended; acquisition and training budgets are outside this experiment. No boss tuning is justified solely by zero wins in this bounded pool. Additional hypotheses need a new recorded protocol and new confirmation seeds.

## Implementation and verification

Changed files: new `TowerEssenceSearch.cs` and `Fixtures/tower-essence-search.json`; the harness command dispatcher; dashboard service, routes and embedded HTML/JavaScript; a new search test class and expanded Tower parity/dashboard tests; the plan, harness README and this review. Implementation is confined to the offline harness and tests/docs. The existing Tower adapter supplies production preparation, combat and outcome interpretation.

```powershell
./build/run-tests.ps1 -Configuration TowerSearchFinalVerification -Filter 'FullyQualifiedName~BalanceHarnessTower'
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-search --output TestResults/balance/tower-search-new
```

**86 tests passed, zero failed/skipped**, with 33 existing build warnings. They cover all 240 legal floor/candidate combinations and fixed non-Essence recipe fields, deterministic selection, independent normal-Tower parity for three new build/floor cases, selection/seed separation, preserved output and partial cancellation, tampered evidence, verified replay, and HTTP session protection/report delivery. The initial run found a frozen-settings JSON casing defect; it was corrected before the passing run and full measurement. No verification command remains blocked. The full backend suite was not rerun because this increment changes only the harness and its tests.

**17 distinct detailed replays matched:** the first confirmation fight for every finalist on floor 1; candidate 05 on floors 4, 5, 7, 8, 10, 11, 12, 13 and 14; candidate 11 on floor 15; and candidate 05's floor-11 draw (`tower.0014`, 183 seconds). This covers every slot count from four through ten and all outcome types. Browser checks verified the search budget/button, 4,620/4,620 completion, 90 verified confirmation cells, report links and the winning candidate's replay controls.

Evidence and the matching executable are retained separately under `TestResults/balance/tower-search-20260909/`. Both full stage bundles are in `TestResults/balance/tower-dashboard-20260909/browser-runs/dashboard-20260909-125715-9bd66807fa2a49a1b14b29c3ef269f04/search/`. The former contains the premeasurement protocol, source snapshot, configs, logs/TRX, report copies, replays, verification script and checksums. Prior Tower catalogs/results, accepted starter evidence and unrelated working-tree changes are preserved. Phase 2 integration stays deferred. No production content/configuration, migrations or deployments change.
