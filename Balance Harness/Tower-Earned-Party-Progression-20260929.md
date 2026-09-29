# Earned Tower parties and floor-one diagnostic — 29 September 2026

Continuation: [early Tower clears and supply unlocks](Tower-Early-Unlock-Progression-20260929.md) now evaluates these earned 72-hour parties chronologically. Fifteen of 32 paths clear floor 3 and gain Epic supply eligibility; no new equipment or measured player acquisition time is credited. The findings below remain the preceding preparation/floor-one evidence.

The actual inventories from the [growing-activity study](Tower-Growing-Activity-Progression-20260929.md) now pass production Tower preparation. A fresh bounded floor-one diagnostic finds wins before every member buys seven supply items. This does **not** establish normal-player acquisition time, a minimum equipment requirement, balance acceptance, or earned progression to floors 10 and 11.

## Evidence

| Personal idle-cadence checkpoint | Earned-growth floor-one wins | Fixed-growth control wins | Supply items across each five-person party |
| --- | ---: | ---: | ---: |
| 2,160 encounters / 6 hours | 0/128 | 0/128 | 0 |
| 8,640 encounters / 24 hours | 0/128 | 0/128 | 0–7 |
| 25,920 encounters / 72 hours | **87/128** | **58/128** | 7–34 |
| Terminal inventory, by 86,400 encounters / 240 hours | 128/128 | 128/128 | 35 |

Each cell contains eight overlapping alternative parties, each tested on 16 seeds. They are four rotations of the same personal owner pool under two alternative idle-outcome assumptions, not 128 independent players. The fixed and grown arms share the same fresh seed panel within each outcome/rotation/checkpoint. At the 72-hour checkpoint, 58 pairs win in both arms, 29 win only in the grown arm, and 41 lose in both; no pair wins only in the fixed arm.

At that checkpoint, all eight grown parties have at least one win, ranging **2/16–16/16**, at levels **34–36**. In the perfect-idle rotation 3, the guardian owns seven supply pieces and the other four members own zero supply pieces; the party wins **10/16** using retained ordinary gear and earned training. Its members have 7, 7, 6, 6 and 6 equipped item slots. In the four-of-five counterpart, the same purchase-count pattern wins 2/16. These examples refute using seven successful purchases **per character** as a universal entry requirement; they do not prove a reliable minimum loadout.

No checkpoint was sampled between 24 and 72 hours. The printed hours count assumed idle activity at the production ten-second cadence. Each member's actual model timestamp adds its recorded dungeon combat seconds; a party waits for its latest selected member. At the 72-hour checkpoint the eight grown assembly times are about **72.409–73.208 model hours**. They exclude player attendance, navigation, noncombat delays and group coordination. **Measured player acquisition-time samples: zero.** The terminal panel stops already-completed histories at their earlier terminal state, without awarding waiting-time XP or rewards.

The 16/16 terminal panels also do not justify retuning the boss: the small fixed panel is a progression diagnostic, not the supported search/confirmation workflow or a representative-player acceptance sample. No historical Tower outcome was transferred to these new parties.

## Preparation, ownership and later dependencies

The new [harness adapter](../LL/tools/BalanceHarness/TowerEarnedPartyStudy.cs) reconstructs **256 personal checkpoints** from the 64 pinned histories (16 distinct owners, two policies and two alternative idle outcomes). It verifies each earned inventory prefix, rejects foreign or duplicate items, applies the existing deterministic inventory selection, and preserves the checkpoint's level, attributes, ordered Essence recipe, training and item identities. It does not rematerialize a hypothetical reference build or improve equipment to match a floor budget. The inventory policy scores materialized stats; it is not a combat optimization search.

It prepares **224 parties / 1,600 participant references** using `WorldTowerPartyRules`, `SnapshotCombatantBuilder`, `CombatPreparationPipeline` and `WorldTowerCombatRuntimeFactory`. Sixty-four preparations cover floor 1 at the four checkpoints. The remaining 160 inspect terminal inventories at floors 2–11 under an explicit hypothetical server-unlock condition. No later-floor fight or unlock is claimed.

Each party uses distinct personal owners, assumes one eligible account per owner, and assigns unique slots in groups of five. Account existence, moderation restrictions, power-rating availability, active expedition locks, server unlocks and concurrent attempts require live state and were **not queried or certified**. Production `WorldTowerService` has no character-level or Essence-count join threshold; the declared level/Essence budgets are evaluation assumptions, not those account/server entry checks.

The [roster fixture](../LL/tools/BalanceHarness/Fixtures/tower-earned-party.json) keeps guardian/restorer/striker/striker/controller order in the first two cells. The pool has only four distinct striker owners. A third cell therefore uses the remaining guardian/restorer/controller/guardian/restorer owners. This is a declared alternative composition, not a reproduction of the historical three balanced cells that would require six striker owners. Rotations overlap and cannot be treated as independently acquired expeditions.

The production slot counts grow and shrink: 5 on floors 1–4, 10 on floor 5, 5 on floors 6–7, 10 on floors 8–9, 15 on floor 10, and 10 on floor 11. Selecting a smaller roster never removes an absent owner's inventory. Across the sixteen policy/outcome/rotation alternatives, floor 11 retains the first ten floor-10 members and all **1,132 equipped item references** byte-for-byte. These references overlap across alternatives; they are not 1,132 independently earned items.

The terminal grown histories still contain only **level 35–45 characters, four unascended Essences each and tier-1 equipment** (227 equipped item references across the 32 alternative personal histories). Consequently:

| Declared evaluation budget | Remaining earned-history gap |
| --- | --- |
| Floors 5–9: level 40, five Essences | Fifth Essence acquisition is unmodeled; 14/32 terminal history states are still below level 40. |
| Floor 10: level 50, six Essences, Legendary/Masterpiece/rank-5 tier 2 | Every terminal state lacks 5–15 character levels, two Essences and earned tier-2 supply gear. |
| Floor 11: level 60, seven Essences, repeating Rare/Standard/rank-2 tier-2 reference | Every terminal state lacks 15–25 levels and three Essences. Tier-2 gear and server progression remain unearned. Stronger already-owned gear must still be retained. |

These are differences from the authored budget, not proof that any Essence count is universally necessary. The older [floor-10/11 carried-gear study](Tower-Carried-Equipment-Checked-Application-20260928.md) remains a separate authored-inventory result. Today's retention check carries actual tier-1 gear; it does not pretend Legendary tier-2 gear was earned. The user's repeating gear curve and `affinity-creation-with-benchmark-validation-v1` remain unchanged.

Next, model chronological Tower clears and **server-wide** unlocks through the early floors, then use the native supply milestone rules after floors 3, 6 and 9. Extend personal XP and actual fifth/sixth/seventh Essence acquisition, including any selected cache rewards, without retroactively assigning later gear, recipes or outcomes. Additional owners are needed if the historical three-cell role composition is required. Freeze and audit those changed preparations before allocating further combat. Do not grant floor-10/11 budgets, assume unearned server clears, require full supply sets for entry, or retune bosses to hide acquisition gaps.

## Reproduction and verification

The [bounded owner](analysis/run-tower-earned-party.py) has separate `prepare` and `combat` modes. Combat requires a pinned, independently audited preparation archive. Both run opt-in backend tests via `build/run-tests.ps1`, freeze source/content/test runtime hashes, use fresh directories with no resume, cap output at 256 MiB and logs at 1 MiB, and retain process-tree receipts. Qualification uses no combat or allocated seeds. Combat performs exactly **1,024 trials plus 64 full native playback replays = 1,088 fights**; all replays match. There is no search, extension or boss change.

| Artifact | SHA-256 |
| --- | --- |
| [Preparation manifest](../TestResults/tower-earned-party-study-20260929/files.json) | `d96b80e062b07a14712ace1686857b8c6b377845af683dcf0b69146dd3d731f2` |
| [Preparation result](../TestResults/tower-earned-party-study-20260929/result.json) | `003265b539e8dac66c1920ca5cd4ba8c2f76f2d7bf296f55e28762765a2b80a0` |
| [Combat manifest](../TestResults/tower-earned-combat-study-20260929/files.json) | `83ec1b78d6b53ffde69f7cfe839644e6ce56d39c5cbfd50db4e7d2d4888b783a` |
| [Combat result](../TestResults/tower-earned-combat-study-20260929/result.json) | `272f8825a799b6a7bd99e2d5ff0b322e9ed54478310ecfd9b41f2322c21b18d4` |
| [Combat seed ledger](../TestResults/tower-earned-combat-owner-20260929/seed-ledger.json) | `a07cc558bc333dfe291de556e0679a7a3c060fc027498f111b08990b416ed723` |

All **512** newly reserved seeds were consumed; the historical exclusion union is now **877,447**. Keep every earlier consumed and unconsumed reservation, including the growing-activity study's 5,824 unused values. Reusing the same new seed across the two declared arms and for replay is deliberate pairing, not another independent sample.

The [preparation audit](../TestResults/tower-earned-party-owner-20260929/independent-audit.json) verifies 1,938 frozen input hashes, all personal prefixes, 224 layouts/preparations, the activity clock, independent inventory selection and retention. The [combat audit](../TestResults/tower-earned-combat-owner-20260929/independent-audit.json) verifies 2,168 input hashes, all trials, identities, outcome/duration accounting, replay equality and reservation exclusion. [Derived comparisons](../TestResults/tower-earned-combat-owner-20260929/analysis.json) retain the per-party purchase and win counts.

The original preparation verifier stopped on three verifier representation errors: equipment enum order, creature GUID versus ability-profile ID, and native Single scaling conversion. The [explicit verifier amendment](../TestResults/tower-earned-party-owner-20260929/verifier-amendment-v1.json) preserves the failed logs, original frozen verifier and result. Its amended verifier SHA is `52b7ae69888dc1737f9e8920e32bfe68197be5799f17fffe8e7e643c4dbeb611`; the original was `1e88480a9d3a2c365e592d9dc5e533328493bd2b821d0d30320ff16af909c2b3`. The corrected audit was frozen and passed before combat. No preparation or combat was rerun or reinterpreted to change an outcome.

**578 regression tests passed; fourteen intentional opt-in study skips.** The two separately owned studies each passed their opt-in test. Focused tests reject shared/foreign items, future rewards, duplicate owners, mixed arms and premature assembly, and assert identity preservation through production preparation. The initial sandbox build could not read the user's NuGet configuration; the same repository wrapper completed with authorized access. Development compile/test-fixture errors were corrected before freezing either study. No required check remains blocked. Qualification completed in 11.953 process seconds with 56,963,758 output bytes; combat completed in 25.406 seconds with 62,951,084 bytes. Both exited zero, stayed within 300/600-second process and 240/540-second native limits, and left zero active children. Process time is not player time.

Commands (PowerShell; Python is the bundled runtime):

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-earned-party-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessEarnedPartyTests|FullyQualifiedName~BalanceHarnessGrowingActivityTests|FullyQualifiedName~BalanceHarnessGrowthTests|FullyQualifiedName~BalanceHarnessProphecyOfferTests|FullyQualifiedName~BalanceHarnessEntrySourceTests|FullyQualifiedName~BalanceHarnessDungeonLootTests|FullyQualifiedName~BalanceHarnessEntryReadinessTests|FullyQualifiedName~BalanceHarnessSourcePolicyTests|FullyQualifiedName~BalanceHarnessActivityTests|FullyQualifiedName~BalanceHarnessBootstrapTests|FullyQualifiedName~BalanceHarnessDungeonAcquisitionTests|FullyQualifiedName~BalanceHarnessAcquisitionStudyTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~TowerEquipmentSupplyTests|FullyQualifiedName~Dungeon|FullyQualifiedName~Prophecy|FullyQualifiedName~SigilFragment|FullyQualifiedName~CharacterExperienceProgressionTests|FullyQualifiedName~EssenceProgressionServiceTests|FullyQualifiedName~WorldTower'
$env:PYTHONDONTWRITEBYTECODE='1'
$towerPython='C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $towerPython 'Balance Harness/analysis/run-tower-earned-party.py' --mode prepare --owner TestResults/tower-earned-party-owner-20260929 --output TestResults/tower-earned-party-study-20260929 --artifacts TestResults/tower-earned-party-build-20260929
& $towerPython 'TestResults/tower-earned-party-owner-20260929/amended-verifier-v1/verify-tower-earned-party.py' --owner TestResults/tower-earned-party-owner-20260929 --manifest-pin d96b80e062b07a14712ace1686857b8c6b377845af683dcf0b69146dd3d731f2 --receipt TestResults/tower-earned-party-owner-20260929/independent-audit.json
& $towerPython 'Balance Harness/analysis/run-tower-earned-party.py' --mode combat --owner TestResults/tower-earned-combat-owner-20260929 --output TestResults/tower-earned-combat-study-20260929 --artifacts TestResults/tower-earned-party-build-20260929 --qualification-owner TestResults/tower-earned-party-owner-20260929 --qualification-pin d96b80e062b07a14712ace1686857b8c6b377845af683dcf0b69146dd3d731f2
& $towerPython 'TestResults/tower-earned-combat-owner-20260929/inputs/Balance Harness/analysis/verify-tower-earned-party.py' --owner TestResults/tower-earned-combat-owner-20260929 --manifest-pin 83ec1b78d6b53ffde69f7cfe839644e6ce56d39c5cbfd50db4e7d2d4888b783a --receipt TestResults/tower-earned-combat-owner-20260929/independent-audit.json
```

These are the original commands; their existing owner/output/receipt paths intentionally reject reruns. A subsequent [owner admission guard](../TestResults/tower-earned-combat-owner-20260929/owner-admission-hardening.json) also rejects a renamed combat rerun when this diagnostic has already reserved seeds. This prevents its closed predecessor union from silently omitting the current study's 512 reservations. The negative check creates no owner/output and allocates no seeds; the original frozen owner and results remain unchanged. A separately declared follow-up needs new directories **and an updated predecessor union**, not just another output name. [Regression log](../TestResults/tower-earned-party-regression-20260929.log), [regression TRX](../TestResults/tower-earned-party-regression-20260929.trx), [qualification process receipt](../TestResults/tower-earned-party-owner-20260929/process.json), [combat process receipt](../TestResults/tower-earned-combat-owner-20260929/process.json) and [closeout checks](../TestResults/tower-earned-combat-owner-20260929/final-checks.json) are local ignored evidence.

Changed files: the new harness adapter, harness-only roster fixture, [focused tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessEarnedPartyTests.cs), owner script and [independent verifier](analysis/verify-tower-earned-party.py); this report; and continuation links/verification status in the handoff, growing-activity report and supply implementation report. Existing production code, search, boss content, cadence, package dependencies and deployment configuration were unchanged. No migration, API startup, shared database access or deployment occurred. Original supply rollout requirements still apply. Historical archives and concurrent LiveOps/analytics edits remain preserved.
