# Floor 1 tuning from Garran's original Health and Power

**Later calibration:** the [retained-build follow-up](Retained-Tower-Builds-Calibration-Review.md) now records applied tuning and automatic benchmark reuse. Measurements below remain historical evidence on their original captured content.

11 September 2026. The user requested restoring Garran's original scaling and adjusting from there using the agreed linked Health/Power approach. The previous offense-only edit was first reset to **Health 1.27 / offense 1.24**. A fresh bounded experiment selected **1.32 times both original values**, giving **Health 1.6764 / offense 1.6368**, now applied to local game content.

The unchanged user-authored party confirmed at **291/1,000 wins (29.1%)**, with **709 defeats, zero draws**, and a pointwise 95% Wilson interval of **26.37–31.99%**. This fits the approved 10–50% target for this exact party and budget. It supersedes the previous offense-only setting as the local floor-1 calibration. Nothing was deployed.

## Scope and baseline

Only the two floor-1 `guardianScaling` fields changed in [tower-floors.json](../LL/src/API/API.LL/Data/world-tower/tower-floors.json). Both are **32% above their original values**. Relative to the previous offense-only setting, HP rises 32% and the offense input falls **18.16%**, from 2.0 to 1.6368. Defense/resistance remain 1.09; penetration/regeneration and every other floor and captured content file remain unchanged.

The [exact supplied party](../LL/tools/BalanceHarness/Fixtures/tower-floor-1-user-party.json) retains all twenty ordered Essence selections, positions, identities and equipment: level 30, four level-1 unascended/unevolved Essences per character, seven Uncommon Standard tier-1/rank-1 items, baseline rolls, no styles, scouting or contributions, and the normal opening cooldowns and 600-second limit. The [original benchmark review](Tower-Floor-1-User-Party-Review.md) records all assumptions.

This is fixed-party calibration. Independent team generation and broad floor-1 acceptance remain open. The [automatic discovery plan](Automatic-Tower-Team-Discovery-Plan.md) should now freeze this content for its future pilot, remeasure references on fresh seeds and retain any stronger generated teams. This experiment did not implement the reusable linked control or change the team-search algorithm.

## Frozen experiment

The original content, retained production-parity executable, exact party, historical seed exclusions and new schedules were frozen before combat. The prior offense-only evidence package was fully checksum-verified without modifying it. Fresh seeds exclude the original user benchmark, earlier captured campaign exclusions and every seed from the preceding tuning experiment. The maximum was **7,708 actual combats**.

The primary search multiplied both original fields by the same factor. Coarse discovery used 50 paired samples per factor:

| Linked factor | Wins / 50 | Draws |
| --- | ---: | ---: |
| 1.00 | 50 | 0 |
| 1.10 | 50 | 0 |
| 1.20 | 50 | 0 |
| 1.30 | 30 | 0 |
| 1.40 | 2 | 0 |
| 1.50 | 0 | 0 |
| 1.60 | 0 | 0 |
| 1.80 | 0 | 0 |
| 2.00 | 0 | 0 |

The predeclared rule refined the adjacent interval straddling the 30% discovery aim: **1.30–1.40**, in eleven 0.01 steps, each on the same 100 new paired seeds. Wins were **45, 41, 33, 26, 18, 14, 13, 9, 8, 7, 4**; factor 1.31 had one draw and the others had none. The fine-only selection rule chose the eligible result closest to 30%, with lower factor breaking ties. **1.32** was frozen before confirmation.

Five settings were then frozen for a 1,000-seed paired comparison. Only the linked primary was eligible for application; the others were diagnostic comparators and could not replace it after confirmation outcomes were seen. The two mixed alternatives multiply primary Power by 0.9 or 0.8 and divide primary HP by that factor, resolving to six decimal places. Their similar HP×Power products are a proposal heuristic, not an assumption of equivalent difficulty.

## Fresh confirmation and tradeoffs

| Setting | Health | Offense / Power | Wins / 1,000 | Draws | Pointwise 95% Wilson | Mean winning duration |
| --- | ---: | ---: | ---: | ---: | --- | ---: |
| Original | 1.27 | 1.24 | 1,000 | 0 | 99.62–100% | 98.78 s |
| Previous offense-only | 1.27 | 2.0 | 362 | 1 | 33.28–39.23% | 116.11 s |
| **Linked primary, applied** | **1.6764** | **1.6368** | **291** | **0** | **26.37–31.99%** | **149.57 s** |
| Mixed, 90% of linked Power | 1.862667 | 1.47312 | 407 | 1 | 37.70–43.77% | 166.59 s |
| Mixed, 80% of linked Power | 2.0955 | 1.30944 | 553 | 0 | 52.20–58.36% | 188.59 s |

The linked setting meets the fixed-party target while using both agreed scaling inputs. Its mean winning duration is about **2 minutes 30 seconds**, compared with about 1 minute 56 seconds for the previous offense-only setting on these same seeds. Linked defeats averaged **189.81 seconds**. These are descriptive comparisons; conditioning on victories compares different winning subsets, and no duration threshold has been approved.

Shifting more scaling into HP while reducing Power lengthened fights and increased this party's observed clear rate. The most HP-heavy comparison exceeds the 50% ceiling. This shows why equal HP×Power products cannot substitute for combat measurements. The 90% mixed comparator remains useful evidence, but was not a post-confirmation replacement for the selected linked setting.

All five comparisons are reported, including the above-ceiling outcomes. The intervals are pointwise; this study does not claim simultaneous acceptance of five settings. Its acceptance decision concerns one primary frozen before confirmation and one fixed party. The previous **394/1,000** offense-only result remains a separate historical seed set; the **362/1,000** remeasurement here neither overwrites it nor becomes an excuse for selecting seeds.

## Verification

After application, the same 1,000 primary trial identities ran through current local content. **Every complete battle report hash matched** the frozen linked candidate. This is reproduction, not an additional independent sample. Floors **2–15** also retained **70 identical before/after reports**, using five paired seeds per floor and the existing balanced curve parties, not the user's party. These checks assess regression, not those floors' balance.

All **13 detailed replays** matched saved preparation, combat and outcome: the first occurrence of each observed outcome in the five comparison settings and local parity. Compact batch reports provide final survivors and aggregate health pressure, but do not provide a full population distribution of first-death timing or peak damage. The retained detailed replays include those measurements for selected examples only; do not generalize them to all 1,000 fights.

The audit reconstructs **54 runs and 7,690 saved battles**, including ordered recipes/equipment, execution and content hashes, disjoint stage seeds, the deterministic selection rule, outcomes, Wilson intervals, paired regression and replays. Total cost was **7,703/7,708 combats**: 450 coarse discovery, 1,100 fine discovery, 5,000 paired comparison/confirmation fights, 1,000 live parity fights, 140 other-floor regression fights and 13 detailed replays.

**115 relevant backend tests passed**, with zero failures or skips. The cached build finished with zero warnings/errors, and all five current harness/runtime assembly hashes match the retained simulation executable. The initial sandbox build could not update an existing generated static-web-assets cache; the same local build and required tests succeeded outside the sandbox. No required verification remains blocked.

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerBenchmarkTests|FullyQualifiedName~WorldTowerTests'
```

## Evidence and reproduction

The [linked-tuning evidence package](../TestResults/balance/tower-floor-1-linked-tuning-20260911/) retains the original baseline, pre-reset content, executable, protocol, exclusions, seed ledger, coarse/fine plans and observations, frozen selection, all five comparison snapshots, exact scenarios, battle records, local application record, regression and replay artifacts, and audit scripts. The earlier sealed packages remain unchanged.

To reproduce the selected confirmation with its retained executable and content, use a new output directory:

```powershell
dotnet TestResults/balance/tower-floor-1-linked-tuning-20260911/executable/BalanceHarness.dll tower --scenario TestResults/balance/tower-floor-1-linked-tuning-20260911/scenarios/confirmation-linked-primary.json --content-root TestResults/balance/tower-floor-1-linked-tuning-20260911/variants/linked-primary --output TestResults/balance/tower-floor-1-linked-rerun
```

Reusing these seeds reproduces the result; it is not fresh confirmation. `audit.py --verify-existing` reconstructs the retained evidence without combat or writes. Add `--check-live` only when also checking that the working content and fixture still match this calibration. Future content changes do not invalidate the sealed historical result.

The package's `checksums.json` covers the retained evidence and publication snapshots. Use `seal.py --verify` for a read-only full inventory/checksum check; do not rerun `--seal` on the completed package. Documentation links and whitespace were checked across the eight changed Markdown files: **238 local links and 17 heading anchors passed**.

The previous offense-only review's reproduction now points to its retained snapshot. Its older `audit.py --verify-existing` additionally requires live offense 2.0 and will therefore reject the intentionally updated live content; do not restore old gameplay merely to satisfy that historical live-state assertion.

No engine or ability code, new production configuration, dependency, migration, account state or deployment changed. The two catalog values are local content changes for the normal release process.
