# Floor 9: isolated Ni penetration trial — 2026-10-01

## Completed result

**Not accepted after the complete 18,944-fight screen. No confirmation or local application.** Every raw outcome and prepared participant was independently recounted. The 115 eligible recipes won **0–4 / 128**, far below the 25-win qualifying minimum. Four Health/Regeneration controls exceeded the 43-win ceiling; the largest adjusted upper bound was **72.71%**. Floor 9 remains unresolved.

| Actual composition (saved label) | Baseline wins | Best eligible wins | Full Resistance/Health wins | Full Health/Regeneration wins | Twelve-item Restoration wins |
| --- | ---: | ---: | ---: | ---: | ---: |
| reference-1 | 0 | 0 | 9 | 46 | 27 |
| reference-2 | 0 | 0 | 1 | 12 | 3 |
| reference-3 | 0 | 4 | 23 | 71 | 28 |
| 5b6c297c… | 0 | 4 | 23 | 75 | 37 |
| 4a9f8b9f… | 2 | 2 | 24 | 62 | 29 |

All counts are out of 128. Full Health/Regeneration uses **30 specialized items on ten characters**; Restoration uses **12 on two healers**, so neither is an eligible route. Imported aliases remain in the 148-recipe family and are counted by actual composition, without changing their raw order or identities.

This result does not support adopting the candidate or simply increasing its sample size. The strongest retained controls now use Health/Regeneration; twelve-item Restoration is a more promising source of gear-limited variants than the tested Resistance subsets. This is a descriptive finding, not a claim that new Restoration subsets are already viable.

**Next:** test the exact proposed Restoration subsets for all five compositions: six items on healer slot 2, six on healer slot 7, and four each on slots 2 and 7. Preserve all 148 controls, including the four ceiling failures: **15 additional recipes / 163 total / 130 eligible**. The saved proposal is **not natively prepared or allocated**. It specifies a separate diagnostic, two batches of 16 seeds (**5,216 fights / 32 new seeds**), with the same isolated candidate. This cannot accept an already rejected setting. Use the diagnostic to decide the next separately declared adjustment, then require fresh full-family screening and independent confirmation. Keep floor 9 ahead of floors 10 and 12–15 and the final current-version 1–15 sweep.

## Verification and evidence

- **459 Python checks** pass, including exact-plan mutations, prior aggregate contracts and catalog byte equality with the generic CLI. **295 native backend tests** pass through `build/run-tests.ps1`, covering the candidate/application guards, actual physical/magical damage and cap, Ni mechanics, and corrected summon defenses. All four native study fixtures pass. The authenticated unchanged broader proof retains **766 passes / four skips** and the separately recorded pre-existing Kharad behavior-manifest failure.
- Native preparation verified all **148 original/candidate pairs** with only Ni Power, ArmorPenetration and MagicPenetration changing. The qualified corrected production DLLs remain identical. Copy Health is still 10% / 1,125 HP.
- Completed batches took **103.427, 104.151, 94.023 and 93.993 seconds**, each about 122 MB. All time, size, disk and seed limits passed. Exactly **128 new reservations**; final exclusions **926,476**. No fight retry, replacement seed or historical outcome pooling.
- The first native compilation found two new-test dictionary construction errors, fixed before study allocation. Its copied old TRX is not fresh test evidence; only the successful fresh build/bound receipts are counted.
- After batch 1, the next admission check rejected a catalog hash: the native preview wrote `40`, while the generic float CLI wrote `40.0`. A separately preserved correction proved this was the **only byte difference**, with identical parsed catalogs, runtime and settings, before inspecting outcomes for the correction. The original declaration remains intact. The corrected declaration retained every numerical setting, phase, seed, recipe, gate and resource limit; the first batch was retained without rerunning it. After the complete independent audit, the preview helper was repaired and its byte-equivalence regression passed. Old helper/test hashes are preserved in snapshots.

Evidence: `TestResults/tower-floor9-ni-penetration-evidence-20261001.json`, SHA-256 `e77e5abc24e0175b6abf3141744ca9984c290350f1dc3c0c6d0e1d70ec383d50`.

Corrected declaration: `TestResults/tower-floor9-ni-penetration-driver-20261001-serialization/corrected-declaration.json`, SHA-256 `5fc5f460c7051b0b2326e5c8e09fceef223136607f3a6fc0e1c33014df0fc848`. Its serialization correction and the original declaration are saved alongside it.

Native proof: `TestResults/tower-floor9-ni-penetration-runtime-verification-20261001-repair1/completion.json`. Final Python proof: `TestResults/tower-floor9-ni-penetration-tests-20261001-serialization/completion.json`.

Next proposal: `TestResults/tower-floor9-limited-restoration-proposal-20261001.json`, SHA-256 `0c60c17b2b10aa0428a4d118ba8a30cf4f40c72d240ed7449955de7a3a37a5af`. All 15 exact proposed recipes are included; no new diagnostic fights or seeds.

All **102 live catalogs remain unchanged**. This continuation changes offline candidate validation, aggregate/application guards, tests and documentation only. No engine change, migration, configuration change or deployment.

## Candidate tested

Candidate: guardian offense **4.7036132812 → 4.4684326171** (×0.95) and guardian penetration multiplier **1 → 40**. Expected native typed penetration: **0.96 → 38.4 percentage points**. Health, abilities, 10% copy Health, corrected copy defenses, cooldowns and every other catalog value remain unchanged. This is an isolated candidate, not applied game balance.

The candidate follows the corrected-runtime replay diagnosis: lowering offense alone helps heavily specialized Resistance parties too much. Penetration is intended to narrow that defense advantage while the small offense reduction preserves lower-defense parties' opportunities. This mechanism must be tested; it does not imply acceptance.

## Frozen protocol

- Original seed-free corrected-runtime source: `tower-balance-pass-floor9-summon-defense-preparation-study-20260929`, manifest `cc3a2dd3dc02383d061cc7e5b0829aa88ca71b8e0787dc23eb96caf73036661f`.
- All **148 raw recipes**, **five actual compositions**, **115 equipment-eligible recipes** retained in original order. Eligibility: at most eight specialized items on at most two characters.
- Screen: four batches of 32 shared seeds, **128 per recipe / 18,944 fights**. Confirmation is an independent equal-size panel and starts only after a complete passing screen.
- Approximate simultaneous Bonferroni–Wilson bounds over all 148 recipes, alpha 0.05: at least two distinct eligible compositions with lower bound ≥10%, and every recipe's upper bound ≤50%. At 128 samples, qualifying lower threshold is **25 wins**; ceiling threshold is **43 wins**.
- No interim acceptance, sample extension, seed replacement, retry, dropped controls or pooling of historical outcomes. Maximum 37,888 fresh fights / 256 reservations. Initial exclusions: **926,348**.
- Each batch: 20,000-fight, 840-second native, 900-second process-owner and 2-GiB limits. Admit only when doubled prior measured cost is below 80% of native time/byte limits and projected remaining disk plus 2 GiB is available.
- Before allocation, require native typed-damage/cap checks and all 148 original/candidate prepared pairs; only Ni Power, ArmorPenetration and MagicPenetration may differ. Reuse the authenticated corrected production assemblies and broader 766-pass / four-skip proof, retaining the known pre-existing Kharad manifest failure.
- Before any local application, require independent confirmation and complete native input/replay parity against the isolated candidate. No deployment or shared database changes.

Frozen proposal: `TestResults/tower-floor9-ni-penetration-trial-proposal-20261001.json`, SHA-256 `8fc8c16d5d9821b6d83d167d7603f6da337dc91fa8eea3e09e6e6c099206f5fd`.
