# Floor 5 Tower calibration — 29 September 2026

**Applied locally after fresh confirmation:** floor-5 Kharad health is **3.1829618995**, down 10% from **3.5366243328**. Offense remains **4.4702934848** and all other boss fields are unchanged. The strongest tested combination won **56/256 (21.88%)**, with an approximate simultaneous interval of **14.55–31.52%**. This passes the existing fixed-family target; it does not establish broad composition viability or ordinary-player acquisition feasibility.

Target: the primary game's offline Balance Harness and floor-5 guardian content. Acquisition studies remain preserved; this continuation prioritizes Tower difficulty rather than expanding dungeon simulation.

**Concurrent correction:** the user has [withdrawn selectable supply rewards](Tower-Supply-Withdrawal-20260929.md). The historical “earned” parties used below depended on that withdrawn economy. Their Essence composition remains a legal benchmark input, but their inventory, progression and acquisition feasibility do not represent the normal game. Existing frozen result labels are preserved. This pass grants no items, adds no replacement acquisition source and makes no ordinary-player acquisition claim.

## Fixed player budget

Ten level-40 characters, five distinct source-family Essences per character, tier-1 Epic / Fine / rank-3 equipment, baseline rolls, no styles, no ascension or evolution. These are the existing declared evaluation assumptions, not an assertion that every player already owns the required items. Essence order is fixed ordinal and is never optimized. Stronger retained equipment is not removed from actual player inventories.

The preparation projects three retained reference compositions and the fifteen earned parties onto identical declared equipment and actor templates. The fifteen earned parties contain one distinct ordered composition after canonicalization, so the screen has four compositions × seven gear profiles = **28 cells**, not fifteen independent composition discoveries. This is a controlled composition comparison; it does not replay the original earned inventories or their account identities.

## Sequence and decision rule

1. Prepare the complete family with production combat preparation and no fights.
2. Screen every cell on the same 32 fresh seeds. Rank by wins, then lower mean guardian health, then ordinal ID. Preserve every outcome.
3. Run the existing `affinity-creation-with-benchmark-validation-v1` search at the best measured gear profile, with three legal starting compositions, unchanged 528-fight policy and 128 separate evaluation seeds for all five nominees. A search against the historical references is retained separately from a follow-up including the stronger earned composition. These stages are not pooled.
4. If no evaluated team reaches the target at the declared budget, test health-only boss factors **0.60, 0.70 and 0.80** against the captured current floor-5 health value. Keep offense, other scaling and mechanics unchanged. Each candidate gets 32 separate screening seeds across the original 28 cells plus all four generated finalists from both searches at all seven profiles (**56 cells**). Rank candidate settings by distance of the strongest observed win rate from 25%, preferring higher health on a tie. No claim of balance acceptance comes from these selection screens. This family and gate were finalized before any candidate fights, after the earned-reference search produced 2/128 wins: “failure” means failure to reach the target, not a requirement for exactly zero wins.
5. Confirm the selected setting once on **256 fresh seeds per cell** across the entire frozen family. Use approximate simultaneous 95% Bonferroni-Wilson intervals across all cells: every upper bound at most 50%, and at least one lower bound at least 10%. Otherwise report failure/inconclusive rather than treating the selected screen as confirmation. Report how many compositions establish viability and fight duration separately; this rule does not itself establish broad build diversity.
6. Apply only a confirmed setting to the local floor file, check that the only production change is that floor's health multiplier, and verify native inputs/replays against the confirmed content. No deployment or database operation.

Each phase has a separate, bounded process owner (900 seconds), native deadline (840 seconds), maximum 2 GiB output, exact fight cap, immutable captured content/settings, durable precombat reservations and zero retries or overwritten studies. New values exclude all **885,164** inherited reservations and all earlier values from this balance pass, including unused values. Full combat reports, input hashes, native reconstruction and independent outcome recounts remain under ignored `TestResults`.

## Initial findings

The first screen completed **896 fights**. All 28 cells won **0/32**. The earned composition with resistance-and-health equipment was strongest by remaining boss health (**35.07%** mean), ahead of the best retained reference (**44.79%**). The original earned-composition gear baseline left **53.09%** health. This corroborates the earlier zero-win result without assuming that equipment or composition alone explains it.

The historical-reference search completed **528 search fights plus 640 evaluation fights**. Both generated finalists and all three references won **0/128**. The best generated finalist left **40.80%** boss health. The policy returned `BenchmarkRetained`; this is not evidence that its benchmark was stronger than the separately measured earned composition.

The follow-up starts from the strongest screen composition and the next two references at the same resistance-and-health profile. It completed another **1,168 fights**. The stronger generated finalist won **2/128 (1.56%)**, leaving **30.16%** boss health on average; the other finalist and all references won zero. Its search gate again retained the benchmark (one gained win, no lost wins on the 60-pair internal panel). These isolated wins are insufficient to reach the 10% target. All four generated finalists enter the candidate family; neither search is promoted or pooled with the other.

## Coarse health calibration and declared refinement

The three completed selection screens each contain **1,792 fights** (56 cells × 32). Their strongest cell won **32/32 at 0.60**, **32/32 at 0.70**, and **29/32 at 0.80**. Every coarse candidate is too easy under the fixed-family target. None is eligible for application or a claim of balance acceptance.

Before further candidate fights, declare two smaller health reductions: **0.90 and 0.95**, with the same 56 cells and 32 new seeds per candidate. Also screen the complete 56-cell family at unchanged health (1.00), including every generated finalist's previously unmeasured gear variants. Use the same strongest-rate distance from 25% selection rule; retain all previous observations without pooling. If an unchanged-health cell already meets the target, include unchanged health as a confirmation candidate. If neither refinement provides a plausible target setting, report that result rather than applying any coarse candidate. The eventual confirmation still uses the whole 56-cell family on 256 fresh seeds each.

All three additional screens completed. The unchanged family won **0/32 in every cell**. The strongest 0.90 cell won **10/32 (31.25%)**, versus **2/32 (6.25%)** at 0.95. The declared selection rule therefore chooses **0.90: health 3.1829618995**, with offense still **4.4702934848**. The confirmation is frozen at **56 × 256 = 14,336 fights**; no selection observations enter its intervals. The projected earned composition won 3/32 with resistance-and-health at 0.90, which alone does not establish its viability.

## Fresh confirmation and checked application

All **14,336 fights** completed, with zero retries, cache reuse or omitted cells. The native run took **343.58 seconds**. The independent audit authenticated **14,460 files**, recounted all 56 cells and returned **Pass** under the predeclared simultaneous interval rule. No confirmation results were pooled with selection, used to change the setting, or used to remove weaker/stronger cells.

| Composition / best gear | Fresh wins | Rate | Mean fight duration, all outcomes |
| --- | ---: | ---: | ---: |
| Second search finalist `297b0b…` / resistance-and-health | 56/256 | 21.88% | 125.68 s |
| Second search finalist `1c2450…` / ability-haste | 29/256 | 11.33% | 109.85 s |
| Historical supply-dependent composition / resistance-and-health | 14/256 | 5.47% | 119.66 s |
| First search finalist `a370da…` / health-and-regeneration | 2/256 | 0.78% | 105.92 s |
| Other first-search finalist and all three original references, every gear profile | 0/256 each | 0% | See archived rows |

Only the strongest combination's adjusted lower bound reaches 10%. The second combination has an observed rate above 10%, but its interval does not establish that threshold. Most retained compositions remain ineffective. This is a confirmed route at the fixed budget, not a claim that a normal or arbitrary party is balanced. Fight pacing is measured, but no separate pacing acceptance threshold was invented. No lower-Essence control was run, so five-Essence necessity is not established.

The local production diff changes exactly **one health value on floor 5**. The application check uses the latest build after the concurrent supply withdrawal, compares all **14,336 native input hashes**, and matches **56 complete battle replays**, one per cell. All non-Tower content hashes and combat settings match; the complete Tower JSON matches the confirmed candidate semantically. Different build paths produce different binary hashes, so this explicitly measured parity is the evidence rather than an assertion of identical binaries. Application checks add zero new seeds and make no claims about unexecuted full replays.

Floor-5 studies total **28,320 fights**, plus **56 application replays**. Their latest reservation union is **886,118**; preserve the entire chain, including both generation-root values and any unused historical reservations. Adjacent-floor screens added later must also enter subsequent exclusion unions.

| Evidence | Path (under `TestResults` unless stated) | SHA-256 |
| --- | --- | --- |
| Confirmation manifest | `tower-balance-pass-floor5-confirmation-study-20260929/files.json` | `ff9434a8abc035ce2c02a5f4fbea325b36f41987d6c417494c92ff21d6eaf87a` |
| Confirmation result | `tower-balance-pass-floor5-confirmation-study-20260929/result.json` | `2f062e09c3d74ed45f7a28146b3ee1a3e78ebd6575cfed53b6257baa73bf42f5` |
| Floor-5 ledger | `tower-balance-pass-floor5-confirmation-owner-20260929/seed-ledger.json` | `f40b999760db8783cef7055b26fb98ccf596221a8226a0e484abce76cbdf28cd` |
| Checked local floor file | `LL/src/API/API.LL/Data/world-tower/tower-floors.json` (repository-relative) | `543866b2f60f130465392439227f61b120c1ef99efebff438d3c27ece8a711bd` |

The application receipt is `TestResults/tower-balance-pass-floor5-application-owner-20260929/completion.json`. Every phase has separate `*-owner-20260929` and `*-study-20260929` directories. These local, ignored artifacts are not present in a clean checkout. Never overwrite or repin them to later source. The first three phases' owner implementation is additionally preserved at `TestResults/tower-balance-pass-owner-source-v1-20260929.py`; subsequent owners save their own source copy.

## Verification and continuation

The adjacent-floor diagnostic used the **latest build with the supply withdrawal and applied floor-5 change**, three retained compositions × seven gear profiles × 32 fresh seeds per floor. **Every cell won 32/32 on both floor 4 and floor 6**: 672 fights each, 1,344 total. Floor 4 uses five level-30/four-Essence characters; floor 6 uses five level-40/five-Essence characters. Both have tier-1 Epic / Fine / rank-3 gear, unascended/unevolved Essences and fixed canonical order. These are strong fixed-budget screens, not ordinary-party win-rate estimates or fresh acceptance. They support prioritizing floor-4 and floor-6 calibration next; neither boss was changed in this pass.

Adjacent evidence manifests: floor 4 `553f71537f7b9bd3faf23b20f3bfa8f37dd424fa5c01563d232e56b6fce130dd`, floor 6 `611aca2f6614e0dda98b8b946975ccd9f472d9ae7201c0bc19fd26da9166083a`, in their respective `TestResults/tower-balance-pass-floor{4,6}-screen-study-20260929/files.json` files. The **latest complete exclusion union is 886,182**, anchored by `TestResults/tower-balance-pass-floor6-screen-owner-20260929/seed-ledger.json`, SHA `22df06e780b012752961ece5f8de9bf77a65c02ec86e30577bcd2efff068db72`. A continuation must include **all** balance-pass owner ledgers along with the 885,164 inherited values; the existing owner performs that union. Total study work is **29,664 fights plus 56 application replays**, or **29,720 total executions** in these scopes.

Use `build/run-tests.ps1` and an isolated artifacts directory. The initial focused suite passed **39 checks**, with two intentional experiment skips; the expanded current-build suite passed **48 checks**, with three opt-in study skips. `run-tower-balance-pass.py --help` describes phase invocation. The initial sandbox build could not read the user NuGet configuration; the wrapper succeeded with escalated access. The initial build had 62 warnings and no errors. No required verification remains blocked.

After applying the content change, the same current-build regression suite again passed **48 checks with three opt-in skips**. Both Python owners parse, 32 current-document links resolve, and `git diff --check` passes. An independent JSON comparison against HEAD verifies that the sole production Tower-content difference is `/floors/4/guardianScaling/health` (the zero-based floor-5 entry). The final receipt is `TestResults/tower-balance-pass-final-checks-20260929.json`; the authoritative post-application regression log is `TestResults/tower-balance-final-regression-20260929.log`. The shared wrapper TRX may be overwritten by other chats, so it is not used as this pass's retained evidence.

Changed files:

- `LL/src/API/API.LL/Data/world-tower/tower-floors.json`: the confirmed floor-5 health value only.
- `LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBalancePassTests.cs` and `analysis/run-tower-balance-pass.py`: controlled composition/gear families, native preparation, supported search, bounded selection/confirmation, durable exclusions and independent recounts.
- `LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBalanceApplicationTests.cs` and `analysis/check-tower-balance-application.py`: checked local application and full-report samples.
- `LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs`: existing projection helper admits floor 5, with a zero-combat regression for its ten-member/five-Essence budget. Search policy and production combat formulas are unchanged.
- This report, `Tower-Continuation-Handoff-20260928.md`, harness `README.md` and `AFFINITY-SEARCH.md`: results and the user's Tower-first continuation priority. Concurrent supply-withdrawal notes are preserved.

Representative completed commands (Python means the bundled runtime; output paths are retained and cannot be reused):

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-balance-final-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests|FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessAffinitySearchTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~WorldTowerTests'
python -B -X utf8 'Balance Harness/analysis/run-tower-balance-pass.py' --mode confirm --name floor5-confirmation --source TestResults/tower-balance-pass-floor5-health-090-study-20260929 --artifacts TestResults/tower-balance-pass-build-20260929 --samples 256
python -B -X utf8 'Balance Harness/analysis/check-tower-balance-application.py' --source TestResults/tower-balance-pass-floor5-confirmation-study-20260929 --audit TestResults/tower-balance-pass-floor5-confirmation-owner-20260929/independent-audit.json --artifacts TestResults/tower-balance-final-build-20260929 --owner TestResults/tower-balance-pass-floor5-application-owner-20260929
git -c core.safecrlf=false diff --check
```

Floors 1–4, 6–9 and 12–15 still need the consistent final-curve calibration and confirmation pass; existing dedicated floor-10/11 confirmations remain narrow fixed-budget evidence. The explicit level/Essence budget for floors 12–15 and carried stronger gear must be settled before accepting those floors. Broader composition viability also remains unfinished on floor 5. No migrations, dependency changes, environment-configuration changes or deployments occurred. The local guardian content change takes effect only through the normal game content rollout; no service was started or shared database accessed.
