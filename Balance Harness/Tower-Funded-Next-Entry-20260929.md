# Native continuation and funded next entries — 29 September 2026

The earned 72-hour checkpoint histories now resume with their complete native personal runtime. One bounded next-entry wave then spends actual remaining sigils, resolves fresh dungeons, claims production rewards and retains all previously owned equipment. No production balance or content was changed.

## Evidence and limits

The twelve ready personal alternatives use twelve fresh combat panels, each paired across sixteen model-server alternatives. These are **twelve distinct personal combat cases**, not 192 independent samples or 192 players. All 512 owner/server combinations remain represented, including 112 without sigils and 208 held by the previously declared equipment-coverage policy. Coverage is a conservative policy, not a production entry restriction.

| Earned server eligibility | Paid entries | Completions | Paid failures | Supply equipment opened | Earned chests retained |
|---|---:|---:|---:|---:|---:|
| Rare | 100 | 84 | 16 | 0 | 84 |
| Epic | 92 | 76 | 16 | 76 | 0 |
| All alternatives | 192 | 160 | 32 | 76 | 84 |

Distinct personal cases complete **10/12**: 6/8 perfect-idle alternatives and 4/4 four-in-five alternatives. The two failures are `striker--1--perfect` and `striker--2--perfect`, both Catacombs Vigor attrition. Both also fail their explicitly supplied full-Epic controls. Each loses its paid sigil and 11,000 pending XP. The corresponding 32 server alternatives lose 352,000 pending XP; no failed run grants supply equipment or claimed XP. Better gear reduces combat duration in these controls but does not remove this route's Vigor failure. No boss was retuned.

Successful runs take **250.2–662.9 engine combat seconds** in this panel. Those seconds exclude player interaction and attendance. The historical ten-second idle cadence, declared victories, one-creature idle encounters, bonuses/retention assumptions and source exclusions remain conditions. Zero player acquisition-time samples were measured. Neither the previous seven-award study stop nor this additional clear establishes a farming-time forecast or a minimum required set.

The fixed choice rule uses the existing recipe and purchase order. It opens the first item that improves the existing materialized-stat-budget inventory score; otherwise it retains the bound chest. This is the existing deterministic inventory policy, not a new combat build search. All 84 earned Rare chests remain unopened because their recipe choices do not improve the retained gear. The 76 Epic openings all improve the selected loadout, including **45 nonparticipant** and **31 participant** alternatives. Ordinary drops independently improve eight Rare-eligible loadouts.

Across alternatives, the wave retains **96 ordinary equipment items**, **32 blueprint items** and **76 selected Epic items**. The ordinary rewards correspond to six distinct personal drops and two distinct personal blueprint awards, repeated across the paired server alternatives. No item is donated, discarded or downgraded. Final loadouts change in 84 combinations; all **192 final production preparations** pass. Characters remain at levels 35–36 with their earned four-Essence training state. After the wave, 128 entered combinations still have funding and 64 have exhausted it; the study executes no second entry.

## Runtime restoration and checkpoint correction

The reconstruction replays **829,440 historical idle events** and the recorded reward/event lifecycle of **123 historical dungeon runs** through the native growth, prophecy, assembly and mastery services. It performs no historical combat. Every recorded entry-before/entry-after state, claim, dungeon event, checkpoint balance, character/Essence state and mastery receipt must match before continuation.

The first qualification correctly failed on a real cutoff defect. The preceding entry qualification used `timestamp <= checkpoint time` for day observations. In thirteen histories, the next idle action generated offers at exactly the terminal checkpoint's time. They were future observations despite sharing that timestamp. The corrected cutoff excludes those **13 day observations**; it preserves all XP, claims, resources, equipment, mastery, prepared stats and entry decisions. Claims and dungeon events may legitimately occur at a completed run's terminal time, so their inclusive cutoff remains. A focused regression distinguishes these cases.

The completed reconstruction restores **377 prophecy instances**: 78 claimed, 37 accepted, 96 offered and 166 declined; weekly offers cover Essence XP, dungeon completions, room clears and kills. It retains full objective/reward JSON, progress, periods, acceptance/completion/claim timestamps, weekly favor/milestone flags and reroll history. **100 native duplicate claim/milestone checks** preserve both resources and growth. Native behavior after a current-day action and a UTC day rollover matches an uninterrupted journey on detached, discarded probes. Probe rewards are not continuation stock.

Full runtime snapshots also retain character XP, current Essence XP/order/ownership, mastery and its last-awarded-run guard, blueprint pity, currencies, unopened caches and exact owned gear. Source grant receipts are reconciled once with the already qualified remaining sigils. Waiting to the earned server's entry time advances a separate clock offset and grants no idle rewards. All **512** restored branch preparations and access checks match the preceding entry qualification.

Only database row GUIDs and wall-clock mastery metadata are normalized for reproducibility. Gameplay timestamps, identities, balances and progression are preserved. The native supply service freezes its decision using the real entry snapshot and earned cleared-floor boundary. The native reward claimer issues a bound chest; duplicate completion/claim checks pass before the declared selection policy opens it. Ordinary equipment and blueprint pity also use native acquisition/claiming and replay guards. No new prophecy reward was earned in this particular next-entry wave.

## Immutable study evidence

| Stage | Output | Manifest SHA-256 |
|---|---|---|
| Corrected runtime qualification | `TestResults/tower-runtime-corrected-study-20260929` | `981c2adf6ad621f0060d03df409969fd69e91cabc6d196c8b94fa60b6892a76b` |
| Funded next entries | `TestResults/tower-next-entry-study-20260929` | `c389b202dd311c78d2e86ca52b06c5607a0e179fc70c2d9efe0be430fd6d8142` |

Runtime result pin: `0856d45f67a8f5cd83bdc2027da171bc3afc9855872b702fc338e8b70b8931cc`. Next-entry result pin: `f55345b0ec0a3d3ca328065cd2c9f08496e822cb8cec7c11af7d68bd3c6b79ed`.

- [Runtime owner/audit](../TestResults/tower-runtime-corrected-owner-20260929/independent-audit.json): 2,294 input hashes; 17.25 seconds; 300-second process/240-second native limits; zero combat/seeds.
- [Next-entry owner/audit](../TestResults/tower-next-entry-owner-20260929/independent-audit.json): 2,412 hashes, 192 paid attempts, twelve supplied controls, twelve exact full-run replays, **2,214 room combats/preparations**, and all reward, XP, pity, ownership and post-run preparation checks. Completed in 24.016 seconds; 17,637,187 output bytes; 900-second process/840-second native, 13,824-fight and 256-MiB bounds. No combat retry or sample extension occurred. The audit passes without amendment.
- [Negative auditor checks](../TestResults/tower-next-entry-owner-20260929/auditor-negative-checks.json) reject duplicate spendable sigils, lost owned gear, unfunded entry, invented XP, reset prophecy state and unearned assembly.
- [New reservation ledger](../TestResults/tower-next-entry-owner-20260929/seed-ledger.json), SHA `d003a2699163a079f49db8eecf55b527be9fa704b994741ab75a43ac8176d901`: 780 reserved, 135 used, **645 unconsumed remain excluded**. The complete exclusion union is **878,611**, preserving every earlier unused reservation.

The original failed runtime owner, partial output, frozen runtime and failure log remain at `TestResults/tower-runtime-owner-20260929` and `TestResults/tower-runtime-study-20260929`. Correction used separate owner/output/build directories; none were overwritten. The corrected runtime's original verifier appended a second timezone suffix to an already UTC `ObservedDay`; its failed log and frozen verifier remain preserved. The [separate auditor-only amendment](../TestResults/tower-runtime-corrected-owner-20260929/audit-amendment.json) fixes that parsing error and passes without changing runtime or results. Reproduce that audit with its pinned `audit-amendment/verify-tower-runtime.py`.

## Changed files and verification

The new harness files are `TowerJourneyRuntime.cs`, `TowerJourneyReplay.cs`, `TowerRuntimeStudy.cs`, `TowerContinuationSupply.cs` and `TowerNextEntryStudy.cs`. The existing journey/growth/source classes expose detached restoration through partial classes; `TowerDungeonLoot.cs` restores personal blueprint pity, and `TowerUpgradeEntryStudy.cs` fixes the observation cutoff. Two harness-only fixtures freeze the runtime and next-entry contracts. `BalanceHarnessRuntimeTests.cs`, `BalanceHarnessNextEntryTests.cs` and the added upgrade-entry cutoff regression cover restoration, rollover, claim guards, funding, failed rewards and retained stronger gear. Five Python owner/auditor files add the bounded execution and independent reconstruction. This report, the handoff, supply report and entry-qualification correction document the result.

**592 regression tests passed; nineteen opt-in studies skipped**, with each completed owned qualification/study passing separately. Backend commands use `build/run-tests.ps1`. The initial sandbox build could not read the local NuGet configuration; the same wrapper succeeded with authorized escalation. Namespace build errors were corrected before admission. No relevant verification remains blocked. The exact regression filter and preservation checks are in [final-checks.json](../TestResults/tower-next-entry-owner-20260929/final-checks.json); the [TRX](../TestResults/tower-next-entry-build-20260929/regression-tests.trx) and build/failure logs remain available.

Original completed study commands (existing owner/output/receipt paths reject overwrites):

```powershell
$env:PYTHONDONTWRITEBYTECODE='1'
$towerPython='C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $towerPython 'Balance Harness/analysis/run-tower-runtime.py' --owner TestResults/tower-runtime-corrected-owner-20260929 --output TestResults/tower-runtime-corrected-study-20260929 --artifacts TestResults/tower-runtime-corrected-build-20260929 --entry-pin 4b0cbf561aa44dca4d9eaccc50dfac846399cf1c60f67560e9cf0bd30a37c315
& $towerPython 'Balance Harness/analysis/run-tower-next-entry.py' --owner TestResults/tower-next-entry-owner-20260929 --output TestResults/tower-next-entry-study-20260929 --artifacts TestResults/tower-next-entry-build-20260929 --qualification-owner TestResults/tower-runtime-corrected-owner-20260929 --qualification-pin 981c2adf6ad621f0060d03df409969fd69e91cabc6d196c8b94fa60b6892a76b
& $towerPython 'Balance Harness/analysis/verify-tower-next-entry.py' --owner TestResults/tower-next-entry-owner-20260929 --manifest-pin c389b202dd311c78d2e86ca52b06c5607a0e179fc70c2d9efe0be430fd6d8142 --receipt TestResults/tower-next-entry-owner-20260929/independent-audit.json
```

## Continuation

Prepare the next legal Tower parties from each server's actual post-wave inventories, clocks and existing roster. Owners held from entry retain the qualified original runtime; successful and failed entrants use their exported post-wave runtime and pity. Preserve stopped servers and nonparticipants. First qualify floor-4 entrants on the fifteen servers that earned floor 3, and next legal attempts on the remaining servers; do not grant a fresh server, a full Epic set or another owner's gear. Use a fresh bounded panel only after preparation and cohort admission, retaining supplied controls and failure evidence. Do not replay this wave or reinterpret repeated alternatives as independent evidence.

The 128 still-funded combinations may support a separately declared continuation, but the present one-entry contract supplies no second outcome. Further earned levels, additional Essence acquisition/training, larger parties and the tier-2/floor-10→11 transition remain necessary before validating the authored late-floor budgets. No farming-hours target is invented. Preserve stronger equipment across that transition and the supported `affinity-creation-with-benchmark-validation-v1` search. No cadence or boss tuning was made to conceal stock or Vigor problems.

All 264 starting uncommitted files were preserved; six intended harness/test files and three documentation files were updated. Historical manifests, unrelated LiveOps/analytics work and the Guardian pin remain unchanged. No production source, deployment configuration, dependency, migration, real database connection, API startup, seeding or deployment changed. No staging, commit, new chat, automation or delegation occurred. Original supply rollout requirements remain applicable.
