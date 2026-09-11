# Kharad calibration against the expanded build portfolio

**Later progression follow-up:** see the [floors 2–5 audit and calibration](Tower-Progression-Floors-2-to-5-Review.md) for current coverage and retained controls. This historical experiment remains unchanged.

11 September 2026. Kharad now has an additional **8% linked Health/Power increase**, confirmed against **122 complete parties** before local application. The strongest team won **101/400 (25.25%)**, with a family-adjusted interval of **18.40–33.60%**. Every included party supports an upper bound below 50%, and at least one supports a lower bound above 10%. This is a scoped portfolio pass.

The [fresh independent search](Post-Calibration-Tower-Team-Search-Review.md) triggered this follow-up by finding a team at **590/1,000 wins (59%)** against the preceding Kharad setting. That failed search remains unchanged. Its eight earlier above-ceiling candidates are all included in this separate calibration.

## Applied settings and results

| Input | Before this calibration | Applied |
| --- | ---: | ---: |
| Kharad Health | 1.95 | 2.106 |
| Kharad offense / Power | 2.4648 | 2.661984 |

The new factor is **1.08 times both previous inputs**, or **1.6848 times Kharad's original Health 1.25 / offense 1.58 inputs**. It preserves their ratio. Defense, resistance, penetration, regeneration and mechanics are unchanged. There is no additional production scaling layer.

The new independent 59% team now wins **101/400 (25.25%)** and remains the strongest observed party in the 122-party confirmation. The other 121 parties were all retained, including weak controls; their outcomes are not averaged with the strongest team. All confirmation draws total **0**. The [exact calibrated builds](../TestResults/balance/tower-kharad-followup-calibration-20260911/published/builds.md) preserve the six generated finalists and their fresh measurements, including every character's ordered Essences, equipment and identities.

Garran stays at Health **1.776984** / offense **1.735008**. Its preceding fresh 1,000-sample check remains **34.8%** for the strongest saved control, with a **31.14–38.65%** family-adjusted interval. The two new Garran builds reached 7% and 10.6%; generated-only viability was inconclusive, so this does not establish reliable rediscovery of its retained best.

## Frozen experiment

The [separate protocol](../TestResults/balance/tower-kharad-followup-calibration-20260911/protocol.json) keeps the same ten level-40 characters, five Essences each, Uncommon Standard tier-1/rank-2 gear, level-1 untrained Essences, no styles/contributions and full 80-Essence pool including Rare Essences with hypothetical ownership. Practical acquisition timing and different gear/slot budgets remain outside this scope.

The portfolio contains every one of the preceding 106 parties plus all 16 newly shortlisted parties. All earlier above-ceiling candidate IDs are explicitly covered. Complete recipes and character identities are preserved and deduplicated; no historical win count is treated as a new trial.

Before combat, the protocol reserved eight paired coarse seeds, 64 paired fine seeds and 400 paired confirmation seeds, disjoint from the preceding historical union and both new independent studies. The coarse sweep tested factors **1.00, 1.04, 1.08, 1.12, 1.16, 1.20, 1.30 and 1.50** for all 122 parties. Its predeclared rule selected the **1.04–1.08** bracket. Eleven equally spaced fine settings tested all six original/new generated finalists and six other parties ranked by total coarse wins. Every omitted fine-stage party remained in confirmation.

The selection rule chose the strongest fine performance closest to 30%, among settings within 15–40%, with lower-factor tie-breaking. It selected **1.08**, where the strongest fine result was **19/64 (29.6875%)**. One setting and the full 122-cell family were then frozen before the 400-sample confirmation. No setting was chosen from confirmation outcomes, no samples were added and no party was dropped.

The existing `bonferroni-wilson-95-v1` evaluator applies its adjustment across all 122 confirmation cells. Earlier samples are not pooled with confirmation. The experiment targets a strong-team win rate through boss scaling; independent team search continues to maximize strength without a 50% cap.

## Saved builds and verification

The preceding search automatically retained all three new generated finalists, including the 59% winner. Its strongest calibrated recipe is also registered with the new evidence and seed ledger. Exact deduplication leaves **4 floor-1 controls and 6 floor-5 controls** in fresh main-dashboard previews. Historical combat seeds are excluded, and reference recipes still have zero generation ancestry. The original portable seven-build fixture remains intact; additional records persist in `TestResults/balance/retained-tower-builds.json`.

This calibration completed **65,335/65,358 allowed combats**: 7,808 coarse, 8,448 fine and 48,800 fresh confirmation battles, plus application/regression/replay verification. The normal-Tower adapter reconstructed saved preparation and evaluated the entire confirmation family. An independent audit reproduced the bracket, fine selection, outcome accounting and all Wilson intervals, and checked **65,316 saved battle hashes**.

After local application, the current compiled executable reproduced **120 exact complete battle reports** across the six generated finalists. Every one of **70 unaffected-floor before/after pairs** matched, including Garran. **19 detailed replays** matched their saved preparation and outcomes. These repeated identities verify application; they are not extra independent confirmation samples.

Relevant checks passed through `build/run-tests.ps1`: **25 tests** for retention, Tower Lab and World Tower behavior, followed by another **25 passing tests after application**. Both independent search archives also reconstructed through the CLI and dashboard. The retained executable matched the current compiled harness assemblies; concurrent unbuilt application changes are outside this result. No rebuild was needed for the content/documentation change, and no required verification was blocked.

```powershell
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerRetainedBuildTests|FullyQualifiedName~BalanceHarnessTowerDashboard|FullyQualifiedName~WorldTowerTests'
```

Changed live game content is limited to the two Kharad fields in [tower-floors.json](../LL/src/API/API.LL/Data/world-tower/tower-floors.json). The local retained-build index, exact result exports, this review and the current planning/README notes were updated. There are no combat-engine changes, new dependencies, configuration schemas, migrations, shared-database changes or deployment. Existing unrelated working-tree changes are preserved.

New Tower Lab runs read the updated local catalog. An already running game API holds its catalog in a singleton [definition provider](../LL/src/Infrastructure/Service/Services.LL/WorldTower/JsonWorldTowerDefinitionProvider.cs) and needs a restart to load these file changes. This offline experiment did not restart or deploy the game API. The temporary frozen-study dashboard was stopped; the main Tower Lab remains on port 5619.

A verification-helper import error stopped the first replay invocation before any replay battle ran. It was corrected, then all 19 declared replay checks completed. The search, selection, confirmation seeds and sample counts were unaffected.

## Remaining coverage

Fresh independent searches can still discover stronger teams after tuning. Carry these controls into those searches and keep any further calibration separate. The remaining progression audit needs floors 2–4, 6–10 and then floor 11, with the approved checkpoints of four, five, six and at least seven Essences per character. A first explicit interpolation can use four through floor 4, five through floor 9 and six at floor 10; its intermediate transitions and gear assumptions must be stated, rather than presented as measured unlock requirements.

Keep lower-budget controls as separate diagnostics, particularly below seven Essences at Serevin. These two scoped floor results do not establish the whole Tower curve, practical Essence access or a global optimum.
