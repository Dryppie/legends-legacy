# Costed Tower entry sources — 29 September 2026

## Finding

Production prophecy progress, claims and fragment assembly now feed a reproducible **personal entry-resource ledger** alongside the [retained dungeon-loot histories](Tower-Dungeon-Loot-Progression-20260929.md). This is a seed-free affordability projection. It does not assign additional dungeon wins or establish player acquisition time.

Two declared scenarios use the same idle victories as those histories. The named Common daily kill prophecy is assumed offered and accepted at each UTC day start. The second scenario also assumes the weekly kill prophecy is offered and accepted on Monday. Both claim immediately, retain all rewards, leave caches unopened and assemble whole Goblin Mines sigils at the existing checkpoints. Offers are **conditions**, not measured frequencies or guarantees of the production offer selector.

| Supplied activity horizon | Conditional offers | Claimed fragments, including Revelation milestones | Extra Mines sigils | Fragments left |
| --- | --- | ---: | ---: | ---: |
| 72 cadence hours | Common daily, with or without weekly kills | 8 | 0 | 8 |
| 240 cadence hours | Common daily | 34 | 3 | 4 |
| 240 cadence hours | Common daily plus completed weekly kills | 42 | 4 | 2 |

The fixture maps one encounter to ten seconds continuously from Monday 28 September at 00:00 UTC. This makes the last horizon ten supplied calendar days. It does **not** estimate how long an actual player takes. Perfect and four-in-five assumed victories meet the same claims by these coarse checkpoints. Daily and weekly objectives share activity; their kills are not billed twice or added to the existing idle farming hours.

Twelve archived histories end at 72 cadence hours; twenty end at 240. Across 32 histories, daily-only accounting retains 60 additional sigils, while daily-plus-weekly retains 80. These are alternative schedules, not additive income. Sigils remain unspent in this source-capacity ledger. New stock is combined with the archived stock only until the first changed entry decision; subsequent historical combat outcomes are not transferred.

## Where the entry policy changes

Both alternatives produce the same four first divergences, all at the 240-hour checkpoint:

| Historical identity / idle assumption | Next attempt ordinal (zero-based) | Archived source | Funded source | Historical supplies at the old endpoint |
| --- | ---: | --- | --- | ---: |
| controller / path 3 / four-in-five | 5 | Catacombs | Mines | 6 |
| controller / path 3 / perfect | 6 | Catacombs | Mines | 7 |
| guardian / path 2 / four-in-five | 6 | Catacombs | Mines | 7 |
| striker / path 0 / four-in-five | 3 | Catacombs | Mines | 5 |

This is **eight projections of four underlying histories**, not eight independent players. All eight retained entry loadouts passed production preparation with exact equipment-descriptor parity. No equipment was granted by the source model. Stronger owned gear, supplies, ordinary dungeon items, blueprints and actor identities remain in the original personal archive.

The two previously unfinished histories now have funded alternative entries under these conditions. That removes their arithmetic shortage relative to a seven-supply target under hypothetical future successes; it does not prove they complete. The first difference occurs before their old stopping points, so even their later old outcomes cannot be reused as outcomes of these alternatives. The other 28 histories per scenario reach their historical stopping point without a source-decision change, though some retain extra unused stock. No earlier checkpoint changes.

## Production sources and costs

The new [adapter](../LL/tools/BalanceHarness/TowerEntrySources.cs) uses `ProphecyService.TrackProgressAsync`, native claims and weekly milestones, `ProphecyRewardResolver`, `DungeonSigilAssemblyService` and `DungeonAccessPolicy`. Repository and inventory boundaries remain in memory. There is no database or API startup. Catalogs, objective targets, XP reward curves, item metadata and assembly rules are read from production files.

- The selected Common daily requires **300 creature defeats** and grants **2 fragments** and one Prophetic Favor. One eligible creature defeat per assumed idle victory is a separate supplied condition; the archived reward-only histories do not measure killed-creature counts. Defeats grant zero kill credit.
- The selected Uncommon weekly requires **35,000 defeats**, granting **8 fragments**, two Favor and a Greater Prophecy Cache. The first weekly completes after encounter 35,000 in the perfect scenario or 43,749 in four-in-five. The second week is incomplete at the ten-day endpoint.
- Weekly Revelation milestones at **3/5/7 Favor** grant **2/4/6 fragments** plus caches. The next week's Favor starts fresh. Claims are counted once; 676 successful claims across the alternative ledgers each passed a native duplicate-claim rejection check.
- Assembly consumes **10 owned fragments per sigil**. Failed affordability checks preserve stock, quantities stay integral, and the native region-1 access rules apply. The fixed source choice remains Mines first, then Catacombs.
- Both one-time quest sigils are already included in the original history: `quest.shenic.crystal_currents` grants the Catacombs sigil and `quest.shenic.between_day_and_night` grants the Mines sigil. The independent source inventory retains their production prerequisites/objectives and gives them **zero additional credit**.

Purchases are inspected but unfunded in these histories:

| Source | Price | Fragments | Weekly purchase limit | Additional gate |
| --- | ---: | ---: | ---: | --- |
| Guild Sigil Fragment Case | 200 Guild Favor | 10 | 2 | Guild membership; Market Office level 2 |
| Guild Sigilwright's Cache | 350 Guild Favor | 30 | 1 | Guild membership; Market Office level 3 |
| Champion Market strongbox | 140 Glory | 20 | 2 | Eligible arena character; Bronze rank |

The ledger contains no earned Guild Favor or Glory, funded guild construction/missions, arena participation or tournament placements. Therefore affordable purchases and purchase grants are zero. A price is not an acquisition schedule. Prophetic Favor and Fate Echo cannot be substituted for either purchase currency. Purchase limits are constraints, not free weekly income. No shop purchase service is executed or claimed to have been exercised by this projection.

Unopened cache contents also contribute zero spendable fragments. The auditor exactly enumerates the production weighted rolls as distributions, separately from owned stock:

| Cache | Fragment range | Mean fragments per opening | Probability of zero fragments |
| --- | --- | ---: | ---: |
| Greater Prophecy | 0–18 | 3.15 | 34.3% |
| Small Revelation | 0–4 | 1.60 | 16% |
| Greater Revelation | 0–18 | 4.80 | 27.4625% |
| Perfect Week | 0–32 | 8.20 | 17.850625% |

Those means are distribution summaries, never fractional sigils or deterministic inventory grants. The ten-day daily-only schedule owns two Small, one Greater and one Perfect Week Revelation cache. The weekly alternative additionally owns one Greater Prophecy Cache. All remain unopened.

## Explicit limits

This extension keeps level 30, existing gear selection, full-slot readiness, quest ordering, purchase targets, Essence state and mastery fixed. Native prophecy XP, Soulstones and Fate Echo are recorded without applying progression or buying improvements: ten-day totals are 33,520 XP / 36 Soulstones / 145 Fate Echo for daily-only, or 54,470 / 44 / 179 with weekly kills. Native Cinder rewards are zero for these selected profiles. Idle XP and earlier activity remain excluded. The in-memory leveling boundary deliberately records rewards without materializing a new level; these are not complete character-state simulations.

Daily availability, acceptance at reset, continuous activity, kill counts, immediate claims and starting weekday are assumptions. Production offer probabilities, interrupted play, other eligible objectives, cache opening identities, paid rerolls, leveling, guild and arena income remain unresolved. There are **zero measured player samples**, zero new fights and zero combat seed reservations. No old combat sample was extended. Seven selected supplies remain distinct from a minimally useful Tower loadout.

## Verification and evidence

Added [source adapter](../LL/tools/BalanceHarness/TowerEntrySources.cs), [projection runner](../LL/tools/BalanceHarness/TowerEntrySourceStudy.cs), [fixture](../LL/tools/BalanceHarness/Fixtures/tower-entry-sources.json), [focused tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessEntrySourceTests.cs), [bounded owner](analysis/run-tower-entry-sources.py) and [independent verifier](analysis/verify-tower-entry-sources.py). Existing combat-study source and historical outputs were left unchanged. Updated this report, the handoff and the supply implementation report, with a forward link in the loot report.

The required backend wrapper passed **456 tests, with eight intentional opt-in skips**. The separately owned export passed one test. New tests cover insufficient funds, incomplete claims, retry protection, owner isolation, acceptance/expiry boundaries, one offer per period, weekly reset, shared objective activity, whole-fragment spending and unspent caches. No frontend changes required frontend tests. Initial sandbox NuGet-config access failed; the same wrapper succeeded with escalation. Two adapter compile errors were corrected before the passing tests; no required command remains blocked.

The independent audit checked **1,894 input hashes, 64 personal source projections, 676 claims and eight production-prepared entry states**. It independently reconstructs kill thresholds, calendar periods, native reward quantities, weekly Favor resets, cache ownership, XP/currency totals, integral assembly debits and the first changed source decision. It also audits quest duplication and unfunded purchases. It does not verify offer likelihood, combat outcomes or player pace.

Frozen evidence in ignored `TestResults`:

- [Request](../TestResults/tower-entry-sources-owner-20260929/request.json), [source map](../TestResults/tower-entry-sources-owner-20260929/source-map.json), [declaration](../TestResults/tower-entry-sources-owner-20260929/declaration.json), [passing independent audit](../TestResults/tower-entry-sources-owner-20260929/independent-audit.json).
- [Output manifest](../TestResults/tower-entry-sources-study-20260929/files.json), trusted SHA-256 **`72e3feb994eda1542724cf0aa8b0c903597c1477a024f74bce029825a836b3d6`**.
- [Result](../TestResults/tower-entry-sources-study-20260929/result.json), SHA-256 **`fddb1afe4ebafd2e5d15482e308fc1b4c7b9a159e83f88beb335ef3cab45b2c2`**.
- [Process receipt](../TestResults/tower-entry-sources-owner-20260929/process.json): 3.391 seconds, exit zero, no remaining children, 772,005 output bytes. Limits were 180 process seconds, 120 native seconds, 32 MiB output and 1 MiB log.
- [Regression TRX](../TestResults/tower-entry-sources-build-20260929/regression-tests.trx), [regression log](../TestResults/tower-entry-sources-regression-20260929.log), [owned export TRX](../TestResults/tower-entry-sources-owner-20260929/export-tests.trx), [export log](../TestResults/tower-entry-sources-owner-20260929/export.log).

Commands executed; preserve these completed output directories:

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-entry-sources-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessEntrySourceTests|FullyQualifiedName~Prophecy|FullyQualifiedName~SigilFragment|FullyQualifiedName~DungeonSigilAssembly'
./build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/tower-entry-sources-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessEntrySourceTests|FullyQualifiedName~BalanceHarnessDungeonLootTests|FullyQualifiedName~BalanceHarnessEntryReadinessTests|FullyQualifiedName~BalanceHarnessSourcePolicyTests|FullyQualifiedName~BalanceHarnessActivityTests|FullyQualifiedName~BalanceHarnessBootstrapTests|FullyQualifiedName~BalanceHarnessDungeonAcquisitionTests|FullyQualifiedName~BalanceHarnessAcquisitionStudyTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~TowerEquipmentSupplyTests|FullyQualifiedName~Dungeon|FullyQualifiedName~Prophecy|FullyQualifiedName~SigilFragment'
$env:PYTHONDONTWRITEBYTECODE = '1'
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python 'Balance Harness/analysis/run-tower-entry-sources.py' --owner TestResults/tower-entry-sources-owner-20260929 --output TestResults/tower-entry-sources-study-20260929 --artifacts TestResults/tower-entry-sources-build-20260929
& $python 'TestResults/tower-entry-sources-owner-20260929/inputs/Balance Harness/analysis/verify-tower-entry-sources.py' --owner TestResults/tower-entry-sources-owner-20260929 --manifest-pin 72e3feb994eda1542724cf0aa8b0c903597c1477a024f74bce029825a836b3d6 --receipt TestResults/tower-entry-sources-owner-20260929/independent-audit.json
```

The owner checked all 375 previously pinned production/content sources before freezing current inputs and runtime. The historical dungeon-loot manifest stays `3b4f4870dfc8e10f9b0056788eb006b394ad7618dca963cba1d48d204a453fe7`; its seed ledger stays `c48c5fafa3b176febbf862841425f6fcbffdfd016032ad1ad02f0d459d52f1c9`. The exclusion union remains **864,455**; all unused reservations remain excluded.

No production source, configuration, dependency, migration, API startup, shared database, seeding or deployment changed. No boss adjustment, equipment-curve change or search change occurred. Historical archives and unrelated concurrent LiveOps/analytics work remain preserved; nothing was staged or committed.

## Next work

Replace the guaranteed-offer conditions with production offer generation and an explicit visible choice/acceptance schedule. Include actual objective eligibility and progression events; preserve one daily choice, UTC expiry, weekly reset and no retroactive progress. Decide whether to include unopened caches or additional funded activities as separate declared alternatives. Carry their currency and XP consequences explicitly. Once a concrete source policy is frozen, evaluate its changed entry routes through a fresh bounded combat comparison using the historical exclusions; never reuse the outcomes after the first differences above. No boss retuning or supply-cadence change follows from this affordability result.

## Subsequent completion — native offers

The [native prophecy offer continuation](Tower-Native-Prophecy-Progression-20260929.md) completes production generation, daily choice and acceptance, plus a fresh bounded comparison. It preserves this report's conditional three/four-sigil calculations as historical scenarios. Actual generated offers under the fixed choice policy assemble one to three sigils at the archived ten-day endpoints; five first entries change and prepare successfully. Fresh four-in-five completion improves **15/16 → 16/16**, while perfect completion stays **16/16**. No existing completion reaches an earlier checkpoint, and normal-player pace remains unmeasured. Original outputs and pins above are unchanged; latest reservations and next work are recorded in the handoff.
