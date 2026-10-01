# Floor 2: composition search with two armored characters — 30 September 2026

**Subsequent result:** The [pressure diagnostic](Tower-Floor2-Pressure-Diagnostic-20260930.md) verified 32 historical replays, followed by the [three-setting Gale/Feast trial](Tower-Floor2-Gale-Feast-Trial-20260930.md) with **44,160 fresh fights**. At 0.50/0.40, two partial-gear teams passed the minimum but the full-armor ceiling failed; stronger Feast settings made teams too weak. Closed **`NoEligiblePressureSetting`** with no game edit. Exclusions **914,649**. The complete 115-recipe source below remains the current live-catalog family for a separately frozen joint refinement.

Target: primary LL World Tower and offline Balance Harness. Continue from the unchanged-catalog [mixed armor screen](Tower-Floor2-Mixed-Armor-20260930.md), following the rejected [Gale trial](Tower-Floor2-Gale-Trial-20260930.md). Search for two viable actual compositions with armor/health equipment on party slots **3 and 4**, using the supported **`affinity-creation-with-benchmark-validation-v1`** policy. Floor-2 guardian health/offense remain **2.6618600366 / 2.5333570679**, and Crimson Gale direct damage remains **0.65**. Floor 6 stays closed at its accepted values.

## Prospective protocol

1. Authenticate the unchanged-catalog 98-recipe source `TestResults/tower-balance-pass-floor2-mixed-armor-screen-study-20260929`, manifest **`0d0be643c70c46fae49e735c8d303571e3e59769892248e06d77e202e91bd3bc`**, every archived outcome, current catalog/runtime and preceding publication. Reuse the hash-checked isolated build `TestResults/tower-floor6-partial-restoration-build-20260930` and the authenticated 97-pass/four-skip backend regression receipt. Run fresh reference/projection safeguards and every new native phase through `build/run-tests.ps1`. Initial exclusion union: **913,900**, including the rejected Gale trial.
2. Reconstruct the saved proposal `TestResults/tower-floor2-two-armor-reference-proposal-20260930.json`, SHA **`c176cfa33286bbe7d1780dd32926913ecb70aaf203d915fa3e7fb561f606bf6c`**. Copy only the equipment of template `mixed-armor/92b0481cf6d81eac7f5c224cd99eca08de7fa53ec63c83de10882a0503ec4cf2` onto saved reference `projected-reference/0f81ee3b078acb9d8ac3cc47a88286f77aeb6298eba531c9d1a29f249c4117ed`. Preserve all other raw fields, party slots, identities, Essence order and progression budgets. The new reference is `gear-reference/a0d703e1bfe91cc2519e91aed4dd64ab6eca9242d583d78c4aab5a818406c198`. Retain every original: **99 recipes / five actual compositions**.
3. Prepare those recipes without combat, then measure all 99 on **64 fresh shared seeds: 6,336 fights**. This panel measures the previously untested third reference and selects parents; it cannot accept balance. Select three distinct actual compositions at `mixed-armor-baseline-slots-3-4`, ordered by wins, remaining guardian health and stable ID.
4. Run the existing supported search once: **528 search fights / 109 search reservations**, followed by all five exact references/nominees on **64 fresh held-out seeds: 320 fights**. Total **848 fights / 173 reservations**. Preserve its search, selection and benchmark policy; no parent replacement, repeated search or algorithm redesign.
5. Retain all 99 measured recipes, all three exact projected search references, both exact nominees and each nominee on the seven original full gear profiles. Deduplicate only identical raw scenarios. The original 60 mixed armor variants remain. Freeze the resulting family (at most **118 exact recipes**) before new allocation. Actual compositions use each party slot's Essence set; identity, ordering and equipment variants do not add compositions.
6. Screen the entire expanded family on **128 fresh shared seeds**. Approximate simultaneous 95% Bonferroni-Wilson intervals use the entire retained family size. Require **every upper bound ≤50%**, and **at least two actual compositions** at the declared two-character armor profile with lower bounds **≥10%**. Do not pool earlier panels or discard ceiling controls.
7. Only a passing screen admits one independent **168-seed confirmation** of the identical complete family, with the same bounds and diversity rule. Maximum confirmation size is **19,824 fights**, below the native cap. Only a passing confirmation receives current-catalog input checks and one full existing-seed replay per recipe before acceptance. A failed gate closes this scope without further search, calibration, retry, extension or game change.
8. Each native phase is limited to **20,000 fights, 840 native seconds, 2 GiB**, and a **900-second process owner**. Admit twice the preceding measured time/storage estimate only below 80% of the envelope. Preserve unsuccessful outputs and all reservations. Use fresh exclusive paths; do not overwrite, resume or repin historical studies.

Maximum **42,112 fresh study fights / 533 reservations** (6,336 + 848 + 118 × [128 + 168]; 64 + 173 + 128 + 168). Conditional application replays use existing seeds and add at most 118 replays. Expected party budget remains five level-30 characters, four level-1 unascended/unevolved Essences each, tier-1 Rare/Standard/rank-2 gear, roll multiplier 1 and no styles. Two armored characters means Chest/Head/Legs armor and Necklace health on each: eight specialized items. Equipment ownership remains hypothetical; this study establishes neither ordinary acquisition nor a pacing target. No dungeon, supply, migration, configuration or deployment work is included.

## Results

**Completed: `NoEligibleTwoArmorConfirmation`.** The unchanged supported search found **two new actual compositions**, bringing the retained family to **115 exact recipes / seven compositions**. The stronger new nominee won **18/128** with armor on slots 3 and 4, compared with **10/128** for the previous leading reference on the same screen. Its full-armor version won **59/128**. Neither the two-composition lower-bound minimum nor the whole-family upper ceiling passed. No confirmation, application or game edit followed.

| Composition | Two armored characters | Adjusted interval | Full armor | Adjusted interval |
| --- | ---: | --- | ---: | --- |
| New D (`34497de3…`) | 18/128 (14.06%) | 6.43–28.03% | 59/128 (46.09%) | 31.63–61.24% |
| New C (`5a063eba…`) | 12/128 (9.38%) | 3.59–22.32% | 41/128 (32.03%) | 19.67–47.56% |
| Previous A (`170058c2…`) | 10/128 (7.81%) | 2.74–20.33% | 47/128 (36.72%) | 23.53–52.25% |

The other two-character references won **9/128** (B) and **5/128** (third reference). Every two-character recipe missed the required **25 wins / 128**. Two full-armor recipes exceeded the adjusted ceiling of **44 wins / 128**: D and previous A. None of the observed win rates exceeded 50%; their uncertainty bounds prevented acceptance. The unused 168-seed confirmation would require **31–61 wins** under the same 115-family method. The ranking is descriptive and does not prove universal superiority; earlier panels are not pooled into these intervals.

The 115 recipes comprise all **98 original controls**, the new measured gear reference, and **16 exact nominee/gear recipes**. All three exact projected search references were already present and merged by raw scenario equality. Every original mixed armor subset and full-gear ceiling control remains. This differs from the earlier floor-6 family size because no additional projected reference recipe was needed here.

Both nominees change actual Essences relative to A:

- **C:** on party slot 3, replace `essence.venomous_snake` with `essence.venomous_spiderling`.
- **D:** on party slot 1, replace `essence.alpha_wolf` and `essence.elder_treant_thornstorm` with `essence.venomous_snake` and `essence.viper`.

These remain related poison-focused builds. They are not Essence permutations or identity aliases, and they do not establish broad archetype diversity.

## Completed phases and verification

| Phase | Panel | Fresh fights | Result |
| --- | --- | ---: | --- |
| Preparation | 99 recipes / no seeds | 0 | Exact saved proposal reproduced |
| Reference measurement | 99 recipes × 64 seeds | 6,336 | A 5/64, B 2/64, third reference 3/64 |
| Supported search | 528 search + five recipes × 64 held-out seeds | 848 | C 5/64, D 12/64; retained A 5/64 |
| Expanded screen | 115 recipes × 128 seeds | 14,720 | Both acceptance gates failed |
| Confirmation/application | Not allocated | 0 | Correctly prevented by the screen |

Total **21,904 fresh study fights / 365 new reservations**, with zero retries. Exclusions increased **913,900 → 914,265**. All four native phases passed their fixtures through `build/run-tests.ps1 -NoBuild`, stayed within the declared caps, and exited with zero active children. Native execution took **67.93 seconds** for the reference panel, **21.91 seconds** for search and **149.85 seconds** for the expanded screen. The search took longer than its per-fight admission estimate, but remained far below the 672-second admission envelope and 840-second native cap.

**19 fresh Python reference/projection safeguards passed.** The unchanged runtime's **97 backend passes / four intentional opt-in skips** were authenticated and reused, not rerun. Independent reconstruction checked every saved evaluation outcome, the native search verification receipt, exact family and nominee coverage, measured parent selection, actual Essence differences, simultaneous intervals and all seed accounting. All **102 live JSON catalogs** are unchanged. The search algorithm and maintained Python/C# implementations are unchanged; this work added a frozen study driver/collector, archives and documentation. No rebuild was needed. Documentation links and `git diff --check` passed; no verification commands remain blocked or failed. No migration, configuration or deployment change.

## Evidence and next work

- Evidence: `TestResults/tower-floor2-two-armor-search-evidence-20260930.json`, SHA **`4dab5076b0c8482c3c4ff4ea83e21caa6470bdd04444ce588955f69a6cbc3a15`**.
- Frozen protocol, declaration, projection, measured parents, expanded family, assessments, resource receipts and fixture results: `TestResults/tower-floor2-two-armor-search-driver-20260930/`.
- Preparation manifest: **`0d5a25778881430fc2a746d025fb2ff9f3e705d42163ef379f593b74c48b7e3d`**.
- Reference panel manifest: **`b083affe31bbbfdcdd481cbdf150aa20b006c5c69feb1e49baed68b5b90119f4`**.
- Supported search: `TestResults/tower-balance-pass-floor2-two-armor-supported-search-study-20260929`, manifest **`8e87f8b63018b3bf153560c4e9499536246c1fdbdcc21a062ab6cb76dda9805b`**. Exact nominees C **`5a063eba5f7bbbb419a5fd25fb0958d5909b0b3efaf1c8c8c390cb2db96b9e90`**, D **`34497de3dcfec2b2b8f551acc8be20721fbba47e097128dab532218c1d012e0d`**.
- **Next measured source:** `TestResults/tower-balance-pass-floor2-two-armor-expanded-screen-study-20260929`, manifest **`b8e1f3b5bd00c1feac0035976c10f8960d12e1ea03d6a8378ee73baeb20d9862`**, audit **`d6dc71b1c540310189dc04dfb5e3e5df25afcd7d51f795281de0ccdfee5ca16f`**. Retain all **115** recipes. Do not return to the 98-cell source or reimport the search.
- Latest ledger: `TestResults/tower-balance-pass-floor2-two-armor-expanded-screen-owner-20260929/seed-ledger.json`, SHA **`0d94650be35e58ee17d9ad355e16dad06de6d71e9abef30c825deb8e469a881e`**. Include all ancestors and later reservations, including rejected candidates.
- Current Tower SHA **`0d416f3cbbf24f7b856fad129e13efcf8ed4a9d5c7677d4bc4b6c51a5061c5e6`**; abilities SHA **`fe9f03f9f6b35e49d8dc589199d690c930a781afb5335bd2736598a091c368db`**. Runtime remains `TestResults/tower-floor6-partial-restoration-build-20260930`.

**Recommended next work: one bounded investigation of Velka's opening-versus-later damage, retaining the 115-recipe family.** The search produced stronger candidates but did not remove the armor dependency. The preceding Gale-only reduction raised the fully armored controls too far; a global difficulty adjustment is not demonstrated to have enough room. Keep the supported search fixed and focus on the ability balance.

Specifically, investigate whether reducing early party-wide Gale damage while increasing later Feast on Wounds pressure can narrow the gap. This is an untested hypothesis: the earlier diagnostic placed the first Gale at 13 seconds and Feast at 21 seconds or later, and found that full armor also removes Tenacity. Check saved timelines and consumed Bleed stacks for the new nominees before selecting a small finite joint coefficient trial. If new detailed replays are needed, freeze a fixed existing-seed prefix and resource cap first; do not rerun the search or repeat a whole acceptance screen for diagnosis. Preserve Dive, cooldowns, targeting, Bleed, healing and guardian scalars in that proposed damage-only trial. The current isolated-candidate helper admits only direct `Damage` operations; changing Feast's `ConsumeConditionStacks` damage coefficient would require narrow, tested support preserving its stack and healing parameters. Do not bypass that guard.

No next coefficients or seeds have been selected. Any candidate must preserve all ceiling controls, pass the same two-composition/eight-item target, then pass an independent complete-family confirmation and application parity before a live edit. Keep the accepted floor-5 and floor-6 changes intact. The original 38-recipe floor-2 confirmation remains historical accepted evidence with current-catalog qualification; this expanded family has not passed. Ordinary acquisition, pacing and broad archetypes remain separate gaps.
