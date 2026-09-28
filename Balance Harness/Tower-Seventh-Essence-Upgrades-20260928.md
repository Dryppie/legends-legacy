# Controlled seventh-Essence upgrades — 28 September 2026

**Follow-up completed:** the [expanded floor-11 calibration](Tower-Floor11-Expanded-Calibration-20260928.md) retained all 114 additions, every original control and both level-matched controls across six settings. The repeated Shadow Imp upgrade wins 20/32 at factor 2 and 2/32 at 2.5, while all lower-budget controls win zero at both. No setting qualifies; a separately frozen finer grid inside 2–2.5 is the next recommendation. The earlier Blackjaw representatives were ties at the screening setting, not universal leaders.

**The missing controlled upgrades substantially outperform the earlier seven-Essence reference family in this historical-seed screen.** At the captured +50% Health/Power floor-11 setting, both strong six-Essence parties reach **32/32** after leveling to 60 and adding a legal seventh Essence. Leveling alone also matters. The earlier seven-Essence family's maximum at this setting was 11/32; it did not represent these stronger compositions.

**Next: recalibrate against the expanded seven-Essence family, retaining the original controls and both level-60 six-Essence controls.** Keep the supported search policy and the user's repeating equipment curve. These results do not justify a new search algorithm, an artificial minimum Essence count, or a mechanics change. No boss setting is selected or applied.

## Controlled results

All entries use the same ten party positions, resistance-and-health gear, tier 2, Rare / Standard / rank 2 equipment, and 32 historical seeds. Floor-11 guardian Health is **4.35** and Power **5.58**, the prior calibration's factor-1.5 content copy. The first six Essences retain their exact order. Existing character, item and Essence instance IDs stay fixed.

| Source party | Six Essences, level 50 | Six Essences, level 60 | Best uniform seventh addition, level 60 | Additions scoring 32/32 |
| --- | ---: | ---: | ---: | ---: |
| Repeated, `floor11-six-1` | 16/32 | 27/32 | 32/32 | 30/57 |
| Alternating, `floor11-six-2` | 11/32 | 17/32 | 32/32 | 9/57 |

The level-only change gains 11 wins and loses none on the repeated party. On the alternating party it gains eight and loses two, a net improvement of six. This is why the level-matched controls are necessary: comparing level-50 six-Essence teams directly with level-60 seven-Essence teams would combine two changes.

`essence.blackjaw_spider` is the deterministic representative for both source parties. Representatives are ordered by wins, lower mean remaining guardian health, then ordinal Essence ID. Every perfect result has zero remaining guardian health, so Blackjaw wins the alphabetical tie. **It is not uniquely best. All 39 co-leaders are preserved.**

| Blackjaw addition | Gains / losses against level-60 six | Gains / losses against level-50 six | Mean fight duration |
| --- | ---: | ---: | ---: |
| Repeated party | 5 / 0 | 16 / 0 | 70.09 s |
| Alternating party | 15 / 0 | 21 / 0 | 74.17 s |

Durations include all 32 fights. The corresponding level-60 six-Essence means are 93.55 s and 98.69 s. Paired gains and losses describe these sampled seeds; they are not independent confirmation or a significance test after searching many additions.

Composition still matters. For example, the ordinary `essence.spider_queen` addition wins zero fights on either source. The distinct `essence.spider_queen_royal_venom` definition is an alternating-party co-leader. There are two draws in the whole screen, both for the alternating party with `essence.undead`, which wins 30/32. Draws count as nonwins.

## Scope and retained evidence

The catalog contains 85 definitions. A uniform addition must come from a source-monster family unused by every member of the source party. Each source admits **57** definitions. The screen appends the same legal definition to all ten characters, yielding **114 upgraded parties**. It does not rearrange existing Essences, mix different additions across party positions, optimize gear, or run the supported 528-fight search.

The frozen schedule contains **7,296 fights**:

- All 112 original floor-11 team/gear combinations, including every earlier seven-, six- and four-Essence control: 3,584 exact input and full-report replays against the captured factor-1.5 calibration.
- Two level-60 six-Essence controls and all 114 additions: 3,712 diagnostic fights on the same historical seeds.

All 228 combinations were prepared before combat. There were zero retries and no fresh seeds. The [seed-free handoff](../TestResults/tower-seventh-upgrade-20260928/handoff.json) contains the 39 co-leaders and both level-60 six-Essence controls, explicitly marked `DiagnosticCandidatesNotConfirmed`. The full [cell definitions](../TestResults/tower-seventh-upgrade-20260928/cells.json) and [results](../TestResults/tower-seventh-upgrade-20260928/result.json) retain all 114 additions, including weaker candidates.

## Implementation decisions

Target: the primary game's detached reference builder and offline Balance Harness.

- [EquipmentReferenceBuildFactory.cs](../LL/src/Infrastructure/Service/Services.LL/PowerRatings/EquipmentReferenceBuildFactory.cs) adds the optional `IdentityProgression` reference-build field. Historical deterministic identity includes level and Essence count, so an ordinary upgrade would regenerate existing instance IDs. The new field anchors identity to the source progression while the actual level and equipped Essences continue to determine stats, abilities and legality. It cannot be combined with the older `IdentityEssenceIds` pin. Null fields remain omitted from serialization, preserving historical behavior.
- [TowerProgressionUpgrades.cs](../LL/tools/BalanceHarness/TowerProgressionUpgrades.cs) enumerates legal uniform additions and copies an existing six-Essence scenario into a level control or appended upgrade. It validates the prepared builds, asserts unchanged existing IDs and clears exported seeds. Gear, party positions and the original ordered loadouts remain intact.
- [BalanceHarnessProgressionUpgradeTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessProgressionUpgradeTests.cs) covers identity retention, source immutability, production preparation, conflicting pins and illegal additions. Its combat fixture is opt-in through `LL_PROGRESSION_UPGRADES`.
- [screen-seventh-essence-upgrades.py](analysis/screen-seventh-essence-upgrades.py) freezes the complete family, content, runtime and source hashes before owning the bounded run. [verify-seventh-essence-upgrades.py](analysis/verify-seventh-essence-upgrades.py) independently authenticates recipes, reports, paired contrasts, ties and the handoff.
- This record, the [prior calibration follow-up](Tower-Floor11-Linked-Calibration-20260928.md) and the [search guide](../LL/tools/BalanceHarness/AFFINITY-SEARCH.md) document the outcome and next step.

The new executable reproduces all 3,584 prior inputs and full reports before evaluating upgrades. The optional field changes detached reference construction only; no live gameplay rule, production balance setting, database schema or deployment configuration changes. Replaying the new pinned upgrades requires their captured executable.

## Next comparison and limits

The +50% setting now has many intended-budget teams above the 50% ceiling. It also leaves a level-60 six-Essence control at 27/32. Treat the earlier 11/32 seven-Essence maximum as a result for that old family, not the maximum of the expanded family.

The next calibration should retain all original controls, both level-matched controls and the measured upgrade family. Keep all 114 additions available; do not drop a strong measured recipe or choose a weaker reference to make a setting appear balanced. Freeze the next setting grid and family before combat, then use fresh confirmation seeds only after selecting a candidate under the existing acceptance policy. Factors between 1.5 and 2 are a useful starting interval from the earlier grid, but **these upgraded parties have not been tested at factor 2**; their upper difficulty boundary remains unknown.

This screen establishes neither a globally best composition nor search-algorithm improvement. It does not resolve acquisition feasibility, carried-over gear or every possible mixed seventh-slot party. The repeating floor equipment curve remains unchanged. No additional fights are queued by this report.

## Verification and reproduction

The focused regression run passed **49 tests**, with the opt-in combat fixture initially skipped. The subsequent owned run passed all three selected tests, including the combat fixture. The final incremental build had 16 existing warnings and zero errors. Python syntax/CLI and whitespace checks passed. No required command was blocked.

The native run took **236.42 seconds**; its owner took **238.95 seconds**, with all eight processes drained. The sealed study contains **211,717,538 bytes**, below the 2 GiB cap. Time limits were 840 seconds internally and 900 seconds for the owner.

The [independent audit](../TestResults/tower-seventh-upgrade-owner-20260928/independent-audit.json) passed on its first run, authenticating **7,629 files** and reconstructing all outcomes, contrasts, co-leaders, handoff teams and 3,584 baseline parity comparisons without more combat. No new teams are independently confirmed. The prior fresh-seed exclusion union remains 834,295.

Source calibration manifest: `feaae1ff33bfccd145c1cd733a91b8dc313f4b965f6d0b4394dffab3a4697c03`.
Study manifest: `c69c0624b45912926bfd3e8bfc02aaa6d1a3f5cf2b8c1ca1ed8e3b4e193f67e8`.
Result SHA-256: `4149df5f84f79a73cd34a5494a3e6351ea588f06f1384e469427589264aeb9bc`.

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-seventh-upgrade-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessProgressionUpgradeTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~CanonicalEquipmentBuildFactoryTests|FullyQualifiedName~EquipmentIntegrationTests'
python -B -X utf8 'Balance Harness/analysis/screen-seventh-essence-upgrades.py' --package 'TestResults/tower-seventh-upgrade-owner-20260928' --output 'TestResults/tower-seventh-upgrade-20260928' --artifacts 'TestResults/tower-seventh-upgrade-build-20260928'
python -B -X utf8 'Balance Harness/analysis/verify-seventh-essence-upgrades.py' --study 'TestResults/tower-seventh-upgrade-20260928' --owner 'TestResults/tower-seventh-upgrade-owner-20260928' --manifest-pin 'c69c0624b45912926bfd3e8bfc02aaa6d1a3f5cf2b8c1ca1ed8e3b4e193f67e8' --receipt 'TestResults/tower-seventh-upgrade-owner-20260928/independent-audit.json'
git diff --check
```

Python refers to the bundled runtime. Completed package/output directories cannot be reused; the owner does not resume or retry combat. Later read-only audits require a new receipt path. Captured evidence is local under `TestResults`, not supplied by a clean checkout. No migrations, external environment changes or deployments are required or performed.
