# Floor 4 equipment assessment — 30 September 2026

**Subsequent result:** The [floor-4 mixed-armor trial](Tower-Floor4-Mixed-Armor-20260930.md) closed **`ScreenDidNotQualifyMixedArmor`**. All **98 loadouts / five actual compositions** were retained. The best at-most-two-character armor variants for A/B won **5/128 and 4/128** in screen; **0** actual partial-armor compositions meet the lower-bound target. The maximum adjusted upper bound is **48.95%**. **12,544 fresh fights / 128 reservations**, plus native qualification of **9,728 historical inputs / 38 full replays**. **80 Python safeguards and 10 native fixture cases passed**; prior **108 backend passes/four skips** were authenticated and reused. Floor 4 remains **five level-30 characters / four Essences / T1 Epic Fine rank 3**; Vaelor and all live catalogs are unchanged. Exclusions **916,825**. The admission work and unchanged-Vaelor trial proposed below are now complete; preserve this report as their historical rationale.

**Review complete; no gameplay change or new combat.** The full saved **38-recipe / five-composition / 9,728-fight** family was authenticated and independently recounted. Its two qualifying compositions both require the tested full armor-and-health profile. Prepared an exact **98-recipe** follow-up that retains all 38 controls and adds every partial-armor subset for those two compositions. This review does not establish that partial armor is viable yet.

## Corrected progression budget

**Floor 4 uses five level-30 characters, four level-1 unascended/unevolved Essences each, tier-1 Epic/Fine/rank-3 equipment, roll 1 and no styles.** Both the saved raw recipes and current `requiredSlots` agree. The preceding floor-2 report incorrectly described floor 4 as a ten-character party; that documentation error is corrected. No party, level, Essence or equipment budget was changed to perform this review. The user's repeating ten-floor gear curve remains intact.

Vaelor remains at health **2.3231953125**, offense **5.023125**, defense/resistance **2.33**, penetration/regeneration **1.0**. The [accepted floor-2 Feast-pressure change](Tower-Floor2-Feast-Pressure-20260930.md), prior floor-5/floor-6 changes and all 102 current JSON catalogs remain unchanged.

## Saved equipment evidence

The source is `TestResults/tower-balance-pass-floor4-reference-coverage-confirmation-study-20260929`, manifest **`3f4f7cbccc46beb78d93da36b615373e04af0548f8144aee94be4688265f508f`**, independent audit **`150f185153a7e0eb469c53114aba5f8a1bfa3ba6f92f351f5beb85472a657907`**. Every archived file and saved outcome was checked. Intervals retain the original approximate simultaneous 95% Bonferroni-Wilson correction over **38 recipes**. These historical results are not fresh current-runtime acceptance.

The two strongest distinct qualifying armor compositions were selected by wins, then remaining guardian health and recipe ID. Their armor, baseline and health/regeneration variants give **1,536 paired saved outcomes**, a subset of the 9,728 already recounted, not additional fights.

| Composition | Equipment | Wins | Original adjusted interval | Mean engine seconds | Median first party death, seconds |
| --- | --- | ---: | ---: | ---: | ---: |
| A (`f4e5804c…`) | armor-and-health | 69/256 | 19.07%–36.63% | 41.41 | 16.0 |
| A (`f4e5804c…`) | baseline | 1/256 | 0.03%–4.59% | 33.50 | 14.0 |
| A (`f4e5804c…`) | health-and-regeneration | 8/256 | 1.06%–8.82% | 38.96 | 16.0 |
| B (`1fbb88f4…`) | armor-and-health | 65/256 | 17.72%–34.97% | 39.28 | 16.0 |
| B (`1fbb88f4…`) | baseline | 0/256 | 0.00%–3.88% | 31.98 | 14.0 |
| B (`1fbb88f4…`) | health-and-regeneration | 9/256 | 1.27%–9.37% | 36.46 | 16.0 |

Armor is the only qualifying profile in the complete family. The best alternative, B health/regeneration at **9/256**, has adjusted interval **1.27%–9.37%**, well below the required 10% lower bound. A baseline wins once; B baseline never wins. Full armor changes **Chest, Head, Legs and Necklace on all five characters: twenty specialized items**. IDs, ordered Essences, identity fields, levels, slots and every other equipment choice match within each pair.

Pairing the same 256 seeds strengthens the descriptive comparison. A armor wins **68** seeds where baseline loses, with zero reverse cases. B armor wins **65**, also with zero reverse cases. Against health/regeneration, A gains **67** and loses **6**; B gains **58** and loses **2**. These paired counts explain the saved profile gap; they do not serve as a fresh acceptance panel.

## What the recipient statistics show

Full armor raises mean recorded physical mitigation from **3,253 to 8,238** for A and **3,221 to 7,805** for B. Magical mitigation also exists on this floor: full-armor means are **1,844 / 1,800**. Whole-fight totals include different fight lengths, so they do not establish the source or timing of an ability-level problem.

Armor's survival benefit is spread across the party. A's baseline slot 1 dies before 20 seconds **118/256** times, versus **83** with armor; slot 4 falls from **86 to 42**. A slot 2's median recorded death moves from **28 to 38 seconds**, alongside mean damage **755 to 1,055**. B's early slot-1 deaths fall **126 to 80**, slot 2 **73 to 48**, and slot 4 **62 to 28**. Death-time medians condition on a recorded death, not all battles.

Both armor and health/regeneration have a median first party death at **16 seconds**, yet their success rates differ markedly. Protecting just the first casualty is therefore not an adequate explanation. There are **zero saved event timelines** in the six paired recipes. No early-damage causal claim, preferred two-character subset or boss-ability adjustment follows from these summaries alone.

| Composition | Slot | Gear | Deaths before 20 s / 256 | Median recorded death, s | Mean reported damage |
| --- | ---: | --- | ---: | ---: | ---: |
| A | 1 | armor-and-health | 83 | 28.00 | 635.3 |
| A | 2 | armor-and-health | 4 | 38.00 | 1054.7 |
| A | 3 | armor-and-health | 48 | 32.00 | 959.6 |
| A | 4 | armor-and-health | 42 | 32.00 | 783.0 |
| A | 5 | armor-and-health | 63 | 28.00 | 585.0 |
| A | 1 | baseline | 118 | 26.90 | 494.2 |
| A | 2 | baseline | 17 | 28.00 | 754.7 |
| A | 3 | baseline | 64 | 28.00 | 679.1 |
| A | 4 | baseline | 86 | 28.00 | 596.3 |
| A | 5 | baseline | 63 | 28.00 | 543.5 |
| B | 1 | armor-and-health | 80 | 28.00 | 625.3 |
| B | 2 | armor-and-health | 48 | 32.00 | 1123.0 |
| B | 3 | armor-and-health | 33 | 32.00 | 822.3 |
| B | 4 | armor-and-health | 28 | 32.00 | 809.5 |
| B | 5 | armor-and-health | 37 | 28.00 | 616.3 |
| B | 1 | baseline | 126 | 21.45 | 462.6 |
| B | 2 | baseline | 73 | 28.00 | 885.8 |
| B | 3 | baseline | 43 | 28.00 | 664.3 |
| B | 4 | baseline | 62 | 28.00 | 633.0 |
| B | 5 | baseline | 52 | 28.00 | 544.4 |

## Concrete next trial

**Test partial armor at unchanged Vaelor settings first.** The declared target is at least **two actual compositions** qualifying with armor/health on **at most two of five characters**, hence at most **eight specialized items**. This is a proposed balance target based on reducing the observed twenty-item dependency; it is not an established outcome or a claim about ordinary acquisition.

The prepared family keeps all **38 original recipes**, including every full-armor winner and weaker reference. For each leading composition, enumerate every one-, two-, three- and four-character subset: **30 variants × two compositions = 60 additions**, **98 exact recipes / five actual compositions** total. The complete enumeration avoids guessing which two characters matter. Three- and four-character variants remain diagnostic controls; they cannot satisfy the at-most-two-character target. Raw Essence order, party order, character identities and all budget fields are preserved. Composition counting alone ignores Essence order and gear.

The family artifact is `TestResults/tower-floor4-mixed-armor-proposed-family-20260930.json`, SHA **`4807d0908c90bf54bd5ebe2545c37c85b8869b319c103dcbcd577eb7dfd3f8a9`**, `ProposedNotAdmitted`. The trial proposal is `TestResults/tower-floor4-mixed-armor-trial-proposal-20260930.json`, SHA **`b68fb84b2e3ab3d77c8a78249c9e98518181d97a930b27540c4ca3388eb7f693`**, `ProposedNotAllocated`. Neither allocates seeds or executes combat.

- Screen every recipe on **128 fresh shared seeds**: **12,544 fights**. Require every adjusted upper bound ≤50% and two actual partial-armor compositions with lower bounds ≥10%; integer gates **25–44 wins / 128** under the 98-recipe correction.
- Only a complete passing screen permits one separate **184-seed confirmation**: **18,032 fights**, gates **33–68 wins / 184**. Keep the complete 98-recipe family in both panels.
- Maximum proposed fresh work: **30,576 fights / 312 reservations**. Exclude all **916,697** prior seeds and later reservations. No historical pooling, dropped controls, seed replacement, extensions or retries.
- Keep **20,000 fights / 840 native seconds / 2 GiB / 900 owner seconds** per native phase. Doubled historical cost estimates are **229.90 s / 303,797,222 bytes** for screening and **330.48 s / 436,708,507 bytes** for confirmation, below 80% of the native limits. These estimates are provisional: recheck measured admission using the qualified current runtime before each phase.

## Required admission work before fresh combat

The saved floor-4 guardian definition and all four Vaelor ability definitions match current data. Effective battle settings also match. However, all five captured combat assembly hashes differ from the current pinned runtime, and saved `appsettings.json` and the shared abilities catalog differ. The only changed ability definitions are the already accepted two Velka and two Kharad abilities. This review explains the catalog drift but does **not** establish native replay parity.

The existing qualification contract accepts a single applied confirmation. The newest applied catalog is bound to floor 2's **complete four-batch aggregate confirmation**, and its application receipts use the aggregate native contract. The family validator currently accepts only floor-2 mixed armor and floor-6 partial Restoration. Therefore the next implementation must explicitly support the complete accepted aggregate application and the new floor-4 family version; do not select one passing floor-2 batch, repin archives, claim qualification from matching JSON alone, or revert live content to satisfy old hashes.

Then qualify **all 9,728 original floor-4 inputs plus 38 full saved-seed replays** against the current accepted catalog/runtime. Freeze a new prospective driver, protocol and paths; independently validate the 98 exact recipes and both phase gates. Only successful qualification and resource admission permit screening. Any failure preserves the evidence and reservations and stops that declared scope. No new search, guardian change, dungeon work or guaranteed equipment supply is needed to establish this partial-gear baseline.

## Verification and retained state

**Six fresh reviewer safeguards passed**, covering archive integrity, modern/legacy formats, composition counting and interval/pacing rules. An independent collector reconstructed all 60 new raw recipes and rejected **ten deliberately corrupted in-memory proposals**: missing variants/controls, wrong slots, level, identity, Essence order, equipment, seed injection, duplicates and wrong floor. It recounted the full 9,728-fight source again, reconciled all paired outcomes, prepared attributes and recipient summaries, and checked the correct five-character budget.

The latest **108 backend passes / four opt-in skips** were authenticated and reused. No C# or maintained analysis implementation changed, so no duplicate backend run was needed. New review programs and artifacts live under `TestResults`; source archives were never edited. All current catalog/runtime/seed-ledger pins were rechecked. **Zero new fights, replays or reservations**; exclusions remain **916,697**. No migrations, configuration changes, database actions or deployment. No requested review check failed or remains blocked; native qualification and the trial are explicitly future work.

Changed Markdown files: this report, continuation handoff, balance status, both harness guides, the gear-coverage notice and the preceding floor-2 report's incorrect floor-4 party description. The frozen prior study protocol, drivers, candidate, replays, application receipt and acceptance result remain unchanged.

## Evidence and commands

- Independent review evidence: `TestResults/tower-floor4-gear-evidence-20260930.json`, SHA **`b81aff0507d26eb9090dad4fc5ac1b272e2b8567a50a3f7a431231812204ec10`**.
- Full family recount: `TestResults/tower-floor4-family-review-20260930.json`, SHA **`02ffdde72fb93cc280564fe2767ef608284186a82d2858d9b48d0b71230c99a0`**.
- Paired recipient review: `TestResults/tower-floor4-gear-review-20260930.json`, SHA **`939f02ccadde02530e9ef7531bd494dc35c23c2ed5434d12077d414ee7af9aad`**.
- Current publication: `TestResults/tower-floor4-gear-publication-check-20260930.json`. It binds these artifacts and the updated documents.
- Latest ledger remains `TestResults/tower-balance-pass-floor2-feast-pressure-confirm-4-owner-20260929/seed-ledger.json`, SHA **`211bfb253870962ea0e0804435544e97aa31d5ae69f8389101ae8a58cb45f446`**, plus ancestors/later reservations.
- Tower SHA **`0d416f3cbbf24f7b856fad129e13efcf8ed4a9d5c7677d4bc4b6c51a5061c5e6`**; abilities SHA **`169b61f23c2e1e301e64e176962bbe3dc8386ea941a87a7f5a8205c872963d6a`**. Floor-2 Gale/Feast stays **0.35/0.65**.

Executed with the configured Python runtime, `-B -X utf8`: `TestResults/tower-floor4-gear-prepare-20260930.py`, `TestResults/tower-floor4-gear-review-20260930.py`, `Balance Harness/analysis/test-tower-balance-status.py`, `TestResults/tower-floor4-gear-collect-20260930.py`, and the publication script. `git diff --check` and document-link validation pass. Existing backend evidence comes from `build/run-tests.ps1` in the completed floor-2 application; there was no backend invocation or new native combat in this review. Completed review programs require fresh output paths and must not be rerun over their archives.
