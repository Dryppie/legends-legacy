# Tower balance status: floors 1–15 — 29 September 2026

**Subsequent results:** the [floor-10 calibration](Tower-Floor10-Expanded-Calibration-20260929.md) supersedes this review's old floor-10 entry: health **12.8371 (+1%)**, offense **7.13**, two viable compositions at **72/256 and 68/256** across 38 exact recipes. That completes the two-composition minimum across the fifteen preceding accepted families. The later [floor-5 alternate-gear challenge](Tower-Floor5-Alternate-Gear-Challenge-20260929.md) expands floor 5 to **103 recipes / fourteen compositions** without changing its settings. Its new teams won **35/96 and 25/96** on health-and-regeneration and **54/96 and 41/96** on resistance-and-health; the full screen exceeded the ceiling, so no acceptance confirmation ran. The old 89-cell confirmation does not establish balance for this wider family. Next calibrate the complete expanded family while preserving both gear options. The frozen review JSON and source registry remain unchanged historical evidence; use the [handoff](Tower-Continuation-Handoff-20260928.md) for current sources and **910,007 exclusions**.

**Historical review complete; its floor-10 recommendation has since been completed above.** Every floor has at least one viable composition in its latest accepted family, but floor 10 alone still has only one. Fourteen floors establish at least two. These are historical, per-family results at guardian definitions that still match current data; this review creates no new combat evidence or current-build replay claim.

Target: the primary LL game's World Tower and offline Balance Harness. No guardian, budget, combat code, search algorithm, economy, dungeon or supply behavior changed. All previous floor-12/13 work is preserved. **Zero new fights and zero seed reservations.**

## Latest accepted results

A viable composition has at least one exact recipe with an adjusted lower bound at least 10% and upper bound at most 50%. Each original complete family's approximate simultaneous 95% Bonferroni-Wilson bounds were independently recalculated; every intended recipe's upper bound stays below 50%. Bounds are per floor, not simultaneous coverage across all fifteen floors or every possible party.

Count compositions by each party slot's Essence set. Equipment, actor identities, recipe labels, Essence order and character level do not create additional compositions. Raw recipes are preserved. These counts measure distinct loadouts, not distinct combat archetypes.

| Floor | Exact recipes / intended compositions | Qualifying recipes / compositions | Strongest confirmation | Adjusted interval | Qualifying gear profiles |
| ---: | ---: | ---: | --- | --- | --- |
| 1 | 38 / 5 | 4 / **2** | 78/256 (30.47%) | 22.13%–40.32% | Armor + health, Health + regeneration, Resistance + health |
| 2 | 38 / 5 | 4 / **3** | 74/256 (28.91%) | 20.76%–38.68% | Armor + health |
| 3 | 38 / 5 | 3 / **2** | 90/256 (35.16%) | 26.31%–45.15% | Health + regeneration, Resistance + health |
| 4 | 38 / 5 | 2 / **2** | 69/256 (26.95%) | 19.07%–36.63% | Armor + health |
| 5 | 89 / 12 | 2 / **2** | 70/200 (35.00%) | 24.51%–47.18% | Resistance + health |
| 6 | 38 / 5 | 2 / **2** | 91/256 (35.55%) | 26.67%–45.55% | Restorer |
| 7 | 60 / 8 | 2 / **2** | 95/256 (37.11%) | 27.76%–47.54% | Resistance + health |
| 8 | 67 / 9 | 2 / **2** | 47/160 (29.38%) | 18.93%–42.56% | Armor + health |
| 9 | 38 / 5 | 3 / **2** | 58/256 (22.66%) | 15.41%–32.02% | Resistance + health |
| 10 | 21 / 3 | 1 / **1** | 55/256 (21.48%) | 14.75%–30.20% | Armor + health |
| 11 | 344 / 126 | 7 / **4** | 93/256 (36.33%) | 25.92%–48.19% | Armor + health, Resistance + health |
| 12 | 131 / 9 | 2 / **2** | 53/152 (34.87%) | 22.78%–49.27% | Retained Resistance + health |
| 13 | 130 / 9 | 5 / **5** | 42/140 (30.00%) | 18.38%–44.93% | Retained Resistance + health |
| 14 | 73 / 5 | 4 / **3** | 91/256 (35.55%) | 26.21%–46.13% | Retained Armor + health |
| 15 | 73 / 5 | 4 / **3** | 75/256 (29.30%) | 20.70%–39.68% | Retained Armor + health |

Floor 11's 344 exact recipes include **312 intended recipes / 126 compositions** plus 32 lower-Essence controls representing four further compositions. Its seven qualifying recipes are **four compositions**, not seven. Repeated and alternating parents each qualify with a Shadow Imp or Gnoll Shaman addition; some qualify with both armor-and-health and resistance-and-health gear. All lower-Essence control upper bounds remain below 10% under the original 344-cell correction. This is sampled checkpoint separation, not proof of universal Essence necessity.

Floor 14's four qualifying recipes represent three compositions; floor 15 has the same distinction. Floors 2 and 9 also include duplicate original/projected representations. Gear and identity variants must not inflate the composition count.

Floor 10 retains its **21 recipes / three compositions**. The alternating armor-and-health party qualifies at **55/256**. Its repeated counterpart won **23/256 (8.98%)**, adjusted **4.89%–15.93%**, and does not establish the 10% lower bound. The other 19 recipes won zero. The separate five-Essence diagnostic contains **252 exact controls / 18 actual Essence compositions**, at two level/tier budgets; all controls won 0/32. Its 32 historical seeds overlap the accepted confirmation. It adds no fresh samples, no sub-10% true-rate guarantee and no broad proof that six Essences are necessary. Keep it separate from acceptance.

## Approved budgets and current guardian settings

All listed intended parties assume complete ownership, fixed rolls of 1, no active styles and level-1 unascended/unevolved Essences. The repeating equipment curve is unchanged: Rare/Standard/R2 at positions 1–3, Epic/Fine/R3 at 4–6, Unique/Exceptional/R4 at 7–9, Legendary/Masterpiece/R5 at 10. Stronger owned gear carries forward.

| Floor | Party / character level | Essences per character / tier | Tested intended equipment | Current health / offense |
| ---: | --- | --- | --- | --- |
| 1 | 5 × L30 | 4 / T1 | Rare/Standard/R2 | 2.5101634336 / 2.4508682344 |
| 2 | 5 × L30 | 4 / T1 | Rare/Standard/R2 | 2.6618600366 / 2.5333570679 |
| 3 | 5 × L30 | 4 / T1 | Rare/Standard/R2 | 1.5047963378 / 6.7842480469 |
| 4 | 5 × L30 | 4 / T1 | Epic/Fine/R3 | 2.3231953125 / 5.023125 |
| 5 | 10 × L40 | 5 / T1 | Epic/Fine/R3 | 3.3102803755 / 4.4702934848 |
| 6 | 5 × L40 | 5 / T1 | Epic/Fine/R3 | 6.153125 / 4.365625 |
| 7 | 5 × L40 | 5 / T1 | Unique/Exceptional/R4 | 4.4454238281 / 6.5953125 |
| 8 | 10 × L40 | 5 / T1 | Unique/Exceptional/R4 | 9.9064526367 / 8.8260253906 |
| 9 | 10 × L40 | 5 / T1 | Unique/Exceptional/R4 | 2.970703125 / 4.7036132812 |
| 10 | 15 × L50 | 6 / T2 | Legendary/Masterpiece/R5 | 12.71 / 7.13 |
| 11 | 10 × L60 | 7 / T2 | Legendary/Masterpiece/R5 | 17.94375 / 23.0175 |
| 12 | 10 × L60 | 7 / T2 | Rare/Standard/R2; Legendary/Masterpiece/R5 | 9.414125 / 11.9 |
| 13 | 10 × L60 | 7 / T2 | Rare/Standard/R2; Legendary/Masterpiece/R5 | 13.180078125 / 12.83625 |
| 14 | 10 × L60 | 7 / T2 | Epic/Fine/R3; Legendary/Masterpiece/R5 | 12.68625 / 10.933125 |
| 15 | 15 × L70 | 8 / T2 | Epic/Fine/R3; Legendary/Masterpiece/R5 | 15.275 / 10.28125 |

Floors 12–14 keep the user's seven-Essence budget; floor 15 keeps eight. Floors 12–15 explicitly compare cycle gear with carried Legendary gear. All their cycle-only recipes won zero in these confirmations; all qualifying recipes use retained Legendary equipment. Floor 11's later carried-equipment confirmation contains only Legendary gear and must not be described as a new Rare-versus-Legendary comparison. These fixed-ownership results do not establish normal acquisition times or ordinary-player success rates.

## Concentration and remaining diversity gap

**12 of 15 floors have only one qualifying gear profile.** Floors 1, 3 and 11 have more than one. A second composition often differs by just one or two Essence substitutions; meeting the count criterion does not establish broad archetype diversity.

The following descriptive marker audit counts the presence of Poisonous Rat, Venomous Snake, Venomous Spiderling or Viper in qualifying parties. It reports composition structure, not measured poison damage or a causal claim that poison is required. Other Essences may also apply poison; these four markers are not an exhaustive archetype classifier.

| Floor | Qualifying compositions containing a marker | Members with at least one marker per qualifying party |
| ---: | ---: | --- |
| 1 | 2/2 | 2–3 of 5 |
| 2 | 3/3 | 4 of 5 |
| 3 | 2/2 | 4 of 5 |
| 4 | 2/2 | 4 of 5 |
| 5 | 2/2 | 2 of 10 |
| 6 | 2/2 | 1 of 5 |
| 7 | 2/2 | 3 of 5 |
| 8 | 2/2 | 2 of 10 |
| 9 | 2/2 | 1–2 of 10 |
| 10 | 1/1 | 12 of 15 |
| 11 | 4/4 | 8 of 10 |
| 12 | 2/2 | 6–7 of 10 |
| 13 | 5/5 | 7–8 of 10 |
| 14 | 3/3 | 6 of 10 |
| 15 | 3/3 | 7 of 15 |

Keep the existing successful builds as ceiling checks in any broader-archetype study. A change that helps a weaker archetype must also retain their complete-family ceiling. Another scalar sweep alone cannot establish that broad diversity exists.

## Saved fight durations: floors 8 and 14

Every qualifying recipe's saved battle outcomes and durations were recounted. Duplicate exact representations remain separate and are never pooled. The two floor-14 rows with the same composition ID below are the original/projected form of the same lineup.

| Floor | Composition | Wins | Mean reported seconds | All-outcome median / p90 | Victory median / p90 | Exact engine median |
| ---: | --- | ---: | ---: | --- | --- | ---: |
| 8 | `244de30e` | 47/160 | 188.09 | 190.0 / 198.0 | 189.0 / 193.0 | 189.10 |
| 8 | `1910371d` | 33/160 | 188.25 | 190.0 / 196.0 | 189.0 / 194.0 | 189.15 |
| 14 | `d4f71fd0` | 51/256 | 135.29 | 135.0 / 150.0 | 122.0 / 141.0 | 134.60 |
| 14 | `13d8f410` | 91/256 | 132.44 | 132.0 / 145.0 | 123.0 / 138.0 | 131.35 |
| 14 | `2050cebe` | 64/256 | 130.50 | 130.5 / 145.0 | 124.5 / 139.0 | 129.95 |
| 14 | `d4f71fd0` | 51/256 | 135.29 | 135.0 / 150.0 | 122.0 / 141.0 | 134.60 |

Quantiles use nearest ranks for p90 and arithmetic midpoints for medians. Reported duration is the harness's rounded display duration; the last column uses the exact engine summary. These are simulated combat durations, not measured player waiting, animation time or acquisition time. Floor 8 is the clear pacing review priority among these two; no approved duration target exists, so neither floor receives a pacing pass/fail verdict. Changing health to shorten fights could alter win rates and would require a separately declared balance study.

## Content and runtime qualification

All fifteen complete target-floor definitions, not just health/offense, match current Tower data. All captured combat settings match the latest floor-13 reference: attributes 18, equipment release 4, `healing-v1`, the captured threat rules and ten ticks per checkpoint. The latest reference's **532 available input/runtime pins** were also checked against current files.

The archived `appsettings.json` is a sanitized offline snapshot, deliberately different from the application's full configuration. [TowerBundle.WriteSettings](../LL/tools/BalanceHarness/TowerBundle.cs) documents that behavior. Compare captured combat settings rather than treating this byte difference as a gameplay change.

The only other catalog difference is `Data/items/items.json` on floors 10 and 11: six legacy `item.tower_supply.v1.*` definitions were added; **no existing item was changed or removed**. The [catalog delta receipt](../TestResults/tower-balance-status-catalog-delta-20260929.json), SHA `b470a32840c01f7f9c097503abe589312f7762d0a7bd1a97bde3016974160c76`, authenticates this comparison. The withdrawn supply issuer remains withdrawn; this review restores nothing.

Captured assembly hashes differ from the latest floor-13 runtime on floors 1–11, 14 and 15; floors 12 and 13 match that runtime. Historical application/replay receipts remain evidence of their recorded executions, but are not replay checks performed by this review. See each floor's full assembly list in the review JSON. Before new floor-10 combat, prepare its exact saved family on the current runtime and verify its original inputs and declared representative reports. Preserve a mismatch as a compatibility finding instead of repinning historical archives, changing identities or substituting historical binaries.

## Recommended next bounded intervention

1. **Floor-10 composition diversity at unchanged guardian values.** Keep health **12.71**, offense **7.13**, the full 21-cell reference family, fifteen level-50 characters, six Essences and tier-2 Legendary/Masterpiece/R5 equipment. First qualify the current runtime. Then declare one challenge using the existing supported search at **armor-and-health**, with the three exact reference compositions. Retain both generated finalists and every exact nominee, including unsuccessful ones, and project new compositions across all seven gear profiles. Require at least two actual viable compositions and every adjusted upper bound at most 50% in one fresh complete-family confirmation. No new algorithm campaign is needed.
2. **Floor-8 pacing needs a design target before tuning.** Its saved duration distribution is now explicit. Choose the desired typical and long-tail combat duration before changing health, offense, regeneration or playback behavior. Floor 14 is a secondary pacing concern. Do not infer a target from these observations.
3. **Broader archetype and gear coverage.** After the floor-10 gap, declare an intentional non-poison or alternate-gear challenge while preserving the known strongest builds. This is separate from finding another nearby poison composition. Ordinary-player acquisition remains a separate unproven dimension and does not justify restoring selectable supplies or diverting this Tower queue into dungeons.

Suggested size for the floor-10 search declaration: a 32-seed reference screen; one 528-fight supported search plus all five nominees on 128 fresh seeds; at most 38 exact cells after complete import; one 96-seed selection screen and one conditional 256-seed confirmation. This is at most **15,216 study fights + 38 post-confirmation replays**, with **621 fresh reservations**, excluding separately declared runtime-qualification replays. Keep the existing 840-second native / 900-second owner / 2-GiB limits and 80% resource preflights. Freeze selection gates, reference ranking, current-runtime qualification coverage and all paths before execution. These are recommended planning bounds, **not an active study or allocated seeds**; stop and redeclare if a complete exact import exceeds them. Do not retry, extend panels, pool prior observations or weaken the family to obtain acceptance.

## Evidence and verification

The [reviewer](analysis/review-tower-balance-status.py) and [pinned source registry](analysis/tower-balance-status-sources-20260929.json) authenticate **293,974 archive members / 6,672,294,624 bytes**. They independently recount **271,176 accepted-confirmation reports**, plus **8,736 historical floor-10 diagnostic reports**, including means and complete within-family seed coverage. Floor-11 runtime-qualification reports are authenticated archive members but are not counted as fresh confirmation samples. All original family sizes and uncertainty corrections are preserved.

Review output: [tower-balance-status-review-20260929.json](../TestResults/tower-balance-status-review-20260929.json), SHA **`145afabf5ca005c95b1d16a958471f08b42c7a2bb33e85123eb59ba5c4e13c9a`**. Reviewer SHA **`515656833a140802dc847a0b688e2f2d7ec15583449f8f447dadf7a19c3d4db1`**. Source registry SHA **`eb3322bd5d2bbd302ace579e5ef9e50b7f6c9189f442837eaeb74c86106b2788`**. Current Tower SHA **`0b52aedce19700d542dbcb9a0b2441d272f1181457642e03ba8c7a14e20f01fd`**. The latest ledger remains at **908,432 exclusions**, SHA **`7c4c95402a1a30857a31c525e69a2779e03fad0aab533dc46519291100142818`**. No new fights or reservations occurred, and no source archive was overwritten or repinned.

| Floor | Accepted source manifest | SHA-256 |
| ---: | --- | --- |
| 1 | [`tower-balance-pass-floor1-reference-coverage-confirmation-study-20260929`](../TestResults/tower-balance-pass-floor1-reference-coverage-confirmation-study-20260929/files.json) | `4404f9418132a1b1d7f449ce87be771e8a92bb404fbd53a2fdc0227793827773` |
| 2 | [`tower-balance-pass-floor2-reference-coverage-confirmation-study-20260929`](../TestResults/tower-balance-pass-floor2-reference-coverage-confirmation-study-20260929/files.json) | `e451d3fd718de05b741857c810ed9512930094f9d0064a13967cd1aaa8198a8d` |
| 3 | [`tower-balance-pass-floor3-diversity-refinement-confirmation-study-20260929`](../TestResults/tower-balance-pass-floor3-diversity-refinement-confirmation-study-20260929/files.json) | `8fe7e0cd82b13d34b4cfebc296117621281dee98393f5fa22b0a7f95cba1b561` |
| 4 | [`tower-balance-pass-floor4-reference-coverage-confirmation-study-20260929`](../TestResults/tower-balance-pass-floor4-reference-coverage-confirmation-study-20260929/files.json) | `3f4f7cbccc46beb78d93da36b615373e04af0548f8144aee94be4688265f508f` |
| 5 | [`tower-balance-pass-floor5-expanded-calibration-confirmation-study-20260929`](../TestResults/tower-balance-pass-floor5-expanded-calibration-confirmation-study-20260929/files.json) | `58d675f7350e6f56730c70f2807edbc71cd64f0d3b792489a5d393ecb9c0cab9` |
| 6 | [`tower-balance-pass-floor6-reference-coverage-confirmation-study-20260929`](../TestResults/tower-balance-pass-floor6-reference-coverage-confirmation-study-20260929/files.json) | `c628d02858d310977e778410be51d2f32c4cb71e35391dc216156a7d0604e924` |
| 7 | [`tower-balance-pass-floor7-diversity-refinement-confirmation-study-20260929`](../TestResults/tower-balance-pass-floor7-diversity-refinement-confirmation-study-20260929/files.json) | `497df7007dc1a715006ff9ffcf128cc572cbfef4657d408ea607e546e7117f20` |
| 8 | [`tower-balance-pass-floor8-health-refinement-confirmation-study-20260929`](../TestResults/tower-balance-pass-floor8-health-refinement-confirmation-study-20260929/files.json) | `0218835433af0414f788d5624b8c67f05b05a6995c7aa7ce8c20cdfc4a2f9947` |
| 9 | [`tower-balance-pass-floor9-reference-coverage-confirmation-study-20260929`](../TestResults/tower-balance-pass-floor9-reference-coverage-confirmation-study-20260929/files.json) | `2d5d67eb2fad561358f6e19e1c6573d7bd0ffc4abfe9fbdcddd53f4abe73ffb4` |
| 10 | [`tower-floor10-family-confirmation-20260928`](../TestResults/balance/tower-floor10-family-confirmation-20260928/files.json) | `95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944` |
| 11 | [`tower-carried-confirmation-20260928`](../TestResults/balance/tower-carried-confirmation-20260928/files.json) | `6a0d98f41362a3428816679efc9d0e8a90a88a37789030fbc38afa0e4652427d` |
| 12 | [`tower-balance-pass-floor12-expanded-calibration-confirmation-study-20260929`](../TestResults/tower-balance-pass-floor12-expanded-calibration-confirmation-study-20260929/files.json) | `7facc8455060b9c1c97bb0eb3ab36066bf665a9d288d4146b7ecc2156626e0c5` |
| 13 | [`tower-balance-pass-floor13-search-challenge-confirmation-study-20260929`](../TestResults/tower-balance-pass-floor13-search-challenge-confirmation-study-20260929/files.json) | `af56729b3f475edad39f99766bc8f64fdd4d1a789900c686fa361ec97f2685ac` |
| 14 | [`tower-balance-pass-floor14-later-complete-confirmation-study-20260929`](../TestResults/tower-balance-pass-floor14-later-complete-confirmation-study-20260929/files.json) | `5a172c2f4a5bdd2ac27aac762a3c0bb82c17514e26ea46dd0ab8542cbb4d43a7` |
| 15 | [`tower-balance-pass-floor15-later-complete-large-confirmation-study-20260929`](../TestResults/tower-balance-pass-floor15-later-complete-large-confirmation-study-20260929/files.json) | `40564a8540bea2ffbb313055a2f3a70ed0ffe269c96a799a977807c8057204ec` |

Four Python safeguards pass: actual-composition grouping, complete-family interval correction, duration quantiles, and rejection of changed/escaping archive members. The first test run exposed an overprecise handwritten expected interval; it was corrected to the documented 14.75% rounding before the passing run. No combat or acceptance rule changed. The full read-only review, documentation links and whitespace checks pass. No backend suite was rerun because no C# or gameplay content changed; previous 82-backend/14-Python receipts belong to the prior scopes and are not new tests here. No required verification remains blocked.

Commands run from the repository root (`python` denotes the bundled Python runtime):

```powershell
python -B -X utf8 'Balance Harness/analysis/test-tower-balance-status.py'
python -B -X utf8 'Balance Harness/analysis/review-tower-balance-status.py' --sources 'Balance Harness/analysis/tower-balance-status-sources-20260929.json' --output 'TestResults/tower-balance-status-review-20260929.json'
git -c core.safecrlf=false diff --check
```

The completed output path is immutable; a later read-only recheck needs a fresh receipt path. `TestResults` is ignored local evidence and is not available in a clean checkout without separate preservation. The maintained additions are this report, the reviewer, its four safeguards and the pinned registry. The handoff and two harness guides point to this review; floor-10/11 historical reports carry a follow-up notice. No game data, migrations, dependencies, configuration changes, service restart, database operation or deployment are part of this scope.
