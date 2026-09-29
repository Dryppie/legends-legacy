# Post-unlock funded-entry qualification — 29 September 2026

The next dungeon-entry boundary is now qualified from actual personal checkpoint resources and earned Tower server results. Of **512 owner/server combinations**, **192** hold a matching sigil and satisfy the existing equipment-coverage policy. **92** of those combinations have Epic supply eligibility after an earned floor-3 clear. These are conditional entry preparations: **zero new dungeons, equipment awards, combat seeds or measured player samples**.

This continues the [early Tower unlock study](Tower-Early-Unlock-Progression-20260929.md). All sixteen modeled personal owners are considered on every one of its 32 server paths, including nonparticipants and the seventeen paths stopped before floor 3. Overlapping parties and alternative idle outcomes are retained; these counts are not independent players or population probabilities.

## Funding and entry findings

| Earned server state | Can enter under declared policy | No remaining sigil | Held by equipment-coverage policy | Total combinations |
| --- | ---: | ---: | ---: | ---: |
| Before floor-3 clear: Rare eligibility | 100 | 61 | 111 | 272 |
| Floor 3 cleared: Epic eligibility | 92 | 51 | 97 | 240 |
| Total | 192 | 112 | 208 | 512 |

On Epic-eligible servers, 43 ready combinations belong to Tower participants and **49 to nonparticipants**. Native dungeon access checks each owner's remaining stock independently. A server unlock does not supply a sigil or move personally bound items between owners. Conversely, joining the clear is not a condition of supply eligibility.

There are 32 personal checkpoint alternatives, representing sixteen owner identities under two prior idle-outcome assumptions:

| Prior idle assumption | Ready personal alternatives | No sigil | Coverage hold |
| --- | ---: | ---: | ---: |
| Perfect victories | 8 | 2 | 6 |
| Four victories in five | 4 | 5 | 7 |

All twelve ready personal alternatives previously reached the seven-supply stop. That stop belonged to the earlier study; it is **not** a gameplay restriction and is deliberately absent from this next-entry policy. The continued policy uses complete slot coverage, then Mines first when a personally held Mines sigil exists, otherwise Catacombs. Coverage includes either a two-handed weapon or a legal one-handed/off-hand pair. The 208 coverage holds are a declared conservative policy, **not a production entry rule or a proved inability to win**. Seven combinations on Epic servers are Tower participants held by this policy despite their party's successful clear.

The qualification found an accounting hazard and guards it explicitly. The archived source ledger records sigils granted by assembly, while the separate dungeon checkpoint records stock remaining after spending. Adding both would credit spent resources again. For each family and owner, the new ledger proves:

`quest stock + idle drops + assembled sigils − paid attempts = remaining checkpoint stock`

It reconciles **18 assembled-sigil grant units across the 32 alternative histories**, replaces those receipt balances with the actual remaining sigil stock, and preserves every other source item. Claimed ordinary-dungeon blueprints are added from the paid-run prefixes rather than lost during reconstruction. Fragments range up to nine, below the production ten-fragment assembly cost; this snapshot cannot fund another assembly without additional earnings. Existing caches remain unopened. Previously excluded currency sources are not minted or spent here; recorded source currency remains intact.

## What is preserved and verified

The [new harness](../LL/tools/BalanceHarness/TowerUpgradeEntryStudy.cs) exports 32 exact 25,920-encounter personal checkpoints. It retains character and Essence XP, ordered Essence identities, exact owned and equipped items, source currency/items, claims, observed offers, dungeon objective events, blueprint items and blueprint progress. It replays **123 historical mastery awards through the native mastery service**, verifies their original receipts and rejects duplicate awards. This is reward-state reconstruction from unchanged pre-branch runs, not a fresh combat result or a transfer of later outcomes to changed gear.

For each of the 512 combinations, the harness executes production combat preparation and both regional grade-I `DungeonAccessPolicy` checks: **1,024 native access evaluations**. Entry costs come from production dungeon definitions; both currently require one matching sigil. The prospective supply comes from the production catalog's level/region priority, released floors and that path's actual cleared floors. A copied inventory preview shows the one-entry debit; the real checkpoint stock is not mutated. All prepared gear and Essence identities remain unchanged across server alternatives. No prospective chest is opened or counted as owned.

The modeled next-entry start is the later of personal checkpoint availability and the server path's finalization time. The gap grants no idle activity or rewards. Clocks remain conditional and cannot establish farming or player attendance time.

The archival offer/claim/event evidence is retained, but **live prophecy runtime state has not been reconstructed**. Earlier exports do not include complete mutable active-instance and weekly-progress objects. The output states `prophecyRuntimeRehydrated: false`. It must not be deserialized as a complete running journey with fresh/default prophecy state: that could duplicate claims, lose progress or change later eligibility. This qualification therefore stops before a reward-producing run. No combat reservations are warranted until that continuation state and changed preparations pass their own checks.

## Verification and reproduction

New local files are the harness, [assumption fixture](../LL/tools/BalanceHarness/Fixtures/tower-upgrade-entry.json), [focused tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessUpgradeEntryTests.cs), [bounded owner](analysis/run-tower-upgrade-entry.py), [independent auditor](analysis/verify-tower-upgrade-entry.py), and this report. The handoff, previous early-unlock report and supply implementation report have continuation updates. Production source, boss content, gear curve, supported search, dependencies and deployment configuration are unchanged.

**584 regression tests passed, seventeen intentional study opt-in skips**, plus the passing owned qualification. Focused tests cover spent-sigil reconciliation, duplicated/future inventory rejection, stock after the old seven-award stop, unchanged gear/stock under previews, and coverage versus affordability. The [independent audit](../TestResults/tower-upgrade-entry-owner-20260929/independent-audit.json) passes without amendment and checks **2,223 frozen input hashes**, resource conservation, historical prefixes, mastery receipts, source items/blueprints, all 512 preparations and 1,024 access evaluations. [Derived personal tables](../TestResults/tower-upgrade-entry-owner-20260929/analysis.json) retain the exact per-owner reasons.

| Artifact | SHA-256 |
| --- | --- |
| [Qualification manifest](../TestResults/tower-upgrade-entry-study-20260929/files.json) | `4b0cbf561aa44dca4d9eaccc50dfac846399cf1c60f67560e9cf0bd30a37c315` |
| [Qualification result](../TestResults/tower-upgrade-entry-study-20260929/result.json) | `bad6c1f4ab5fbce35b9d8decdfca07d6897ea4ced19c9cc23ec72d2ce61a44e4` |
| Unchanged predecessor seed ledger | `4094923d5ca85548df97c30d00046eb26935279bed7c3dd03ecc060be9862dff` |

The owner freezes source/content/runtime and all used history members before invoking the repository test wrapper. It allows 300 process seconds, 240 native seconds, 256 MiB output and 1 MiB logs, with no combat or seed allocation. Actual completion: **5.219 process seconds**, **19,771,053 output bytes**, exit zero and zero active children; see the [process receipt](../TestResults/tower-upgrade-entry-owner-20260929/process.json). Process time is not player time. The exclusion union remains **877,831**, including all **236** unused early-unlock reservations and every earlier unused seed.

The first sandbox build could not read local NuGet configuration; the authorized wrapper build succeeded. A missing namespace import was corrected before freezing. No required check remains blocked. [Regression log](../TestResults/tower-upgrade-entry-regression-20260929.log), [regression TRX](../TestResults/tower-upgrade-entry-regression-20260929.trx), [owned-test TRX](../TestResults/tower-upgrade-entry-owner-20260929/study-tests.trx), frozen source maps and [closeout checks](../TestResults/tower-upgrade-entry-owner-20260929/final-checks.json) are retained locally.

Original commands; existing owner, output and receipt paths intentionally reject overwrites:

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-upgrade-entry-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessUpgradeEntryTests'
# Broader regression: previous early-unlock filter plus BalanceHarnessUpgradeEntryTests, saved in final-checks.json.
$env:PYTHONDONTWRITEBYTECODE='1'
$towerPython='C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $towerPython 'Balance Harness/analysis/run-tower-upgrade-entry.py' --owner TestResults/tower-upgrade-entry-owner-20260929 --output TestResults/tower-upgrade-entry-study-20260929 --artifacts TestResults/tower-upgrade-entry-build-20260929 --unlock-pin fce82fe32b7bfb224f9b6b4d6675a0fca72bc202c12aed54685d3735d698fc52
& $towerPython 'TestResults/tower-upgrade-entry-owner-20260929/inputs/Balance Harness/analysis/verify-tower-upgrade-entry.py' --owner TestResults/tower-upgrade-entry-owner-20260929 --manifest-pin 4b0cbf561aa44dca4d9eaccc50dfac846399cf1c60f67560e9cf0bd30a37c315 --receipt TestResults/tower-upgrade-entry-owner-20260929/independent-audit.json
```

## Next implementation

**Completed by the [native continuation and funded-entry follow-up](Tower-Funded-Next-Entry-20260929.md).** That reconstruction found thirteen next-activity day observations included solely because their timestamps equaled the checkpoint. The current `Restore` excludes those future offers; the original archive and audit remain unchanged. All original resource, claim, XP, gear, mastery, prepared-stat and entry-decision witnesses still match. Use the corrected full runtime archive for continuation, not the old inclusive `days` prefix. The following paragraph records the original next-step brief.

Reconstruct the full native journey/prophecy runtime through the exact historical checkpoint prefix, verify it against these exported XP, claim, offer, event, resource and mastery witnesses, and advance only its clock to the qualified entry start. Keep the reconciled spendable sigil stock separate from grant receipts. Then admit a bounded next-entry experiment with explicit handling of all server paths and personal coverage holds, debit the actual matching sigil, resolve a fresh dungeon and native rewards, and select any earned supply item against retained inventory. A successful Epic-eligible completion is still required; eligibility alone is not an item. Replay later preparation after XP and equipment change, using fresh seeds from the full predecessor union.

Further levels, actual fifth/sixth/seventh Essence acquisition, larger parties and tier-2 gear still require earned histories toward floors 10–11. Preserve the repeating curve and stronger gear across the transition. No boss retuning, API startup, real database connection, migration, seeding or deployment occurred. All 258 pre-existing modified/untracked files were unchanged before the three intended documentation updates, and historical archives and unrelated concurrent work remain preserved.
