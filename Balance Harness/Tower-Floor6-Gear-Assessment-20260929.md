# Floor 6: saved gear-coverage assessment — 29 September 2026

**Subsequent result (30 September):** The [partial Restoration comparison](Tower-Floor6-Partial-Restoration-20260930.md) qualified the 38 historical recipes against the current runtime, then tested all 98 recipes in **12,544 fresh fights**. Four pieces were promising for one composition (**29/128**), but the second reached **12/128** and the full-Restorer leader exceeded the adjusted ceiling. No confirmation or guardian change followed. Use that report and the current handoff for continuation; this assessment below is preserved as historical evidence.

Target: primary LL World Tower and offline Balance Harness. Follow the [accepted floor-5 ability change](Tower-Floor5-Ability-Intermediate-20260929.md) with a read-only assessment of floor 6. The objective is to identify the closest unsuccessful gear/build alternatives and a justified next experiment, preserving every established leader.

**Completed: historical assessment verified.** All **9,728 saved fights / 38 exact recipes / five actual compositions / seven gear profiles** were reconstructed. Only two Restorer compositions qualify, at **91/256 and 50/256**. Every one of the **30 non-Restorer recipes** has **0/256** wins. Restorer specialization changes six items on the healer alone; it does not equip the whole party with Restoration. No game change, new battle, replay or seed reservation occurred.

## Declared assessment

- Authenticate the complete 38-recipe floor-6 confirmation `TestResults/tower-balance-pass-floor6-reference-coverage-confirmation-study-20260929`, manifest **`c628d02858d310977e778410be51d2f32c4cb71e35391dc216156a7d0604e924`**. Reconstruct all 9,728 saved outcomes and the original 38-cell Bonferroni-Wilson bounds. Count actual compositions by per-slot Essence sets, preserving all raw recipe ordering and identities.
- Compare every gear profile and retain all five actual compositions. Examine the saved first-death, healing, regeneration, barrier and duration summaries. Whole-fight totals are duration-dependent and cannot establish an early damage mechanism. The archived reports contain no event timeline; do not invent one or label inferred causes as measured.
- Check unchanged floor-6 definition and combat settings against the current catalog. Authenticate the floor-5 acceptance/application evidence and distinguish changes confined to floor 5 from the historical full-catalog identity. Do not repin or overwrite old requests, treat candidate provenance as accepted by default, or claim current-runtime parity without running it.
- This scope allocates **zero fresh seeds, zero new study fights and zero diagnostic/application replays**. It makes no game, search-policy, maintained-code, configuration, migration, database or deployment change. Existing equipment ownership remains hypothetical. Native baseline qualification and a new supported search require a separately declared follow-up.

The current live floor-5 settings remain Seal **0.4**, Crushing Verdict **4.9**, health **3.3102803755**, offense **4.4702934848**. Floor 6 remains health **6.153125**, offense **4.365625**. Initial and final exclusion union must remain **911,919**. Preserve the user's expected gear progression and the withdrawal of guaranteed selectable equipment supplies.


## Verified gear gap

| Gear profile | Exact recipes | Best wins | Lowest mean guardian health among tied leaders |
| --- | ---: | ---: | ---: |
| ability-haste | 5 | 0/256 | 31.24% |
| armor-and-health | 5 | 0/256 | 38.89% |
| baseline | 5 | 0/256 | 30.21% |
| health-and-regeneration | 5 | 0/256 | 37.98% |
| precision | 5 | 0/256 | 36.54% |
| resistance-and-health | 5 | 0/256 | 34.68% |
| restorer-specialization | 8 | 91/256 | 12.20% |

The two qualifying Restorer compositions are `8801c2d4e8e92f468f83cfca0200cb49fbafc80717872215b161e26ad0096d66` at **91/256 (35.55%)**, adjusted **26.67%–45.55%**, and `7f859e243932b4d1737e0a3e41f22d35394e19c64163dcf55154bdf4b2cea0c5` at **50/256 (19.53%)**, adjusted **12.82%–28.60%**. The other nonzero records are a reference and its projected representation, both 19/256; they are not another qualifying composition. All bounds retain the original 38-cell correction. Each zero-win recipe has an adjusted upper bound of **3.88%**, below the 10% viability minimum. This applies to these exact tested recipes, not every possible party using that gear.

Baseline gear is closest among the unsuccessful profiles by remaining guardian health, with ability haste next. All six unsuccessful gear maxima use `reference-3`. That tie-breaker is a descriptive search ranking, not evidence of viability or a reason to pool zero-win samples.

## What Restorer specialization means

For the leading composition, the Restorer profile changes **only party slot 2**, the healer. Its MainHand, Chest, Head, Ring, Necklace and Relic switch to their Restoration variants; Legs remains baseline, as do all items on the other four characters. Prepared healer Restoration rises from **0 to 280.98**, while Power **51.59** and MaxHealth **1,907** remain unchanged; AbilityHaste changes from **15.05 to 0**. Consequently, “one qualifying gear profile” does not mean every character needs the same specialization. It does leave the minimum required healer investment unresolved.

| Same leading composition | Wins | Mean exact duration | Mean healer healing over whole fight |
| --- | ---: | ---: | ---: |
| baseline | 0/256 | 56.08s | 1,633.84 |
| ability-haste | 0/256 | 59.61s | 2,305.84 |
| restorer-specialization | 91/256 | 77.62s | 7,717.41 |

The full Restorer variant delays the first original-character death by **15.48 seconds on average** relative to baseline in the **244 paired seeds with a recorded death in both variants**. Twelve Restorer fights have no original-character death; baseline has a death in all 256. The healer produces much more healing and the party survives longer, but total healing is duration-dependent. It is not a fixed-time estimate of healing efficiency.

Ashen Toll is the largest named whole-fight guardian damage source in this leading composition’s baseline and Restorer summaries. The reports contain **zero event timelines**, so they do not identify a particular early pulse, Cinder interaction or Doom event as the causal bottleneck. No damage coefficient change is justified from those totals alone.

## Recommended next experiment

**Measure partial Restoration investment before changing Orsenn or the search algorithm.** Keep the two existing successful compositions and their full Restorer gear as ceiling controls. For each, enumerate all **15 two-piece** and **15 four-piece** subsets of the six Restoration slots; revert every unselected slot to that composition’s baseline item. Keep all other characters, Essences, identities, positions, item rarity, tier, quality and rank unchanged. This proposes **60 additional recipes**, or **98 total** with the complete existing 38-recipe family. Exact-scenario deduplication verified all 98 are distinct. These are diagnostic loadouts, not a claim that a normal player owns those exact subsets. The complete reviewable proposal is saved as `TestResults/tower-floor6-partial-gear-proposed-family-20260929.json`, SHA **`e71b8bdda74e400d331ee639ffb4305cfa095ede3793d52e3d26464a24491219`**, with status `ProposedNotAdmitted`. All 38 original recipe objects are unchanged; only the healer equipment changes in the 60 additions. No outcomes or seeds are assigned.

Freeze the complete family and its family-size-adjusted gates before combat. A 128-seed screen would require at most **12,544 fights**; a separate 184-seed confirmation would require at most **18,032 fights**. Verify runtime/output admission from appropriate evidence first. Require every retained recipe’s adjusted upper bound at most 50%, and a qualifying partial-Restoration recipe for each of at least two actual compositions; retain the usual lower bound of 10%. A screen does not establish acceptance. Independently confirm the complete frozen family, including all unsuccessful variants, before claiming reduced specialization requirements. This assessment allocates none of those fights or seeds.

If partial investment qualifies, floor 6 may already support a less demanding healer loadout without a guardian edit. If it does not, use the closest partial variants for the supported composition search, preserving every known Restorer winner as a ceiling check. Only then consider a separately diagnosed ability change. The alternative baseline-search parent ranking is saved in the evidence, but is not the primary recommendation after inspecting the role-specific gear definitions.

## Current catalog and continuation prerequisite

The complete floor-6 definition and captured combat settings still match. The only changed ability records since its confirmation are Kharad’s Crushing Verdict and Seal of Ascension. The historical Tower file also contains older settings for other floors. The newer floor-5 acceptance, application receipt and five recorded runtime-assembly hashes were authenticated. Current Tower SHA **`aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`**; ability catalog SHA **`fe9f03f9f6b35e49d8dc589199d690c930a781afb5335bd2736598a091c368db`**.

**Current-runtime floor-6 parity was not run.** The study owner still deliberately rejects an ability-candidate source as an implicit baseline and rejects mismatched full catalogs during current-content refresh. Before the proposed experiment, implement explicit admission using the accepted floor-5 confirmation and application receipt: verify the declared ability deltas, current catalog/runtime identity, unchanged target-floor definition/settings, and exact retained recipes. Preserve historical hashes and rejected-candidate guards. Produce a new qualified source and verify all **9,728 historical floor-6 inputs plus 38 complete replays** against the current build before transferring those observations as current baseline evidence. Do not erase candidate provenance or simply bypass the catalog comparison. This prerequisite was identified, not implemented, in the read-only assessment.

## Evidence and verification

- Evidence: `TestResults/tower-floor6-gear-assessment-evidence-20260929.json`, SHA **`9a93aa742ff07331432b0d040af7e3f94179124b74397cfb5ede62d241c65259`**.
- Original floor-6 manifest: **`c628d02858d310977e778410be51d2f32c4cb71e35391dc216156a7d0604e924`**; reconstructed audit: **`b531ceb571e0410b2171f07709971495beb24724d91daddf0ee945c5464c5120`**. **9,834 archive members** and all **9,728 outcomes** verified.
- Frozen assessment protocol: `TestResults/tower-floor6-gear-assessment-protocol-20260929.md`, SHA **`38da20984919548774422306608b46300f43efa539edfc98b005ea79aacfeacc`**. Script: `TestResults/tower-floor6-gear-assessment-20260929.py`, SHA **`7032c0688877405e3265d7cfdfb128c696f2981602746d3a1837cd893b254d4a`**.
- Ran the assessment with the configured Python runtime and `-B -X utf8`. Recomputed all original bounds and counted actual compositions, paired seeds, recorded deaths and gear changes. Authenticated current source/runtime pins and the earlier **97 backend passes / four opt-in skips**; those tests were reused, not rerun. No maintained code or gameplay changed, so no new backend suite was needed.
- New study fights **0**, diagnostic/application replays **0**, new seeds **0**. Exclusion union remains **911,919**. No active study remains. Native qualification, search, screening and confirmation were intentionally not run in this assessment; no required command is blocked.
- Publication verification: `TestResults/tower-floor6-gear-assessment-publication-check-20260929.json`; current files, evidence bindings, document links and `git diff --check` pass. Updated this report, the floor-5 continuation notice, handoff, status notice and both harness guides. No configuration, migration, dependency, database or deployment changes.
