# Tower four-slot entry investigation — 9 September 2026

The four-slot entry party can beat floor 1 with reinforcement, but wins remain uncommon in the tested budgets. This investigation does not establish that Garran needs a particular nerf or that the current recipe represents all four-slot parties. The user's qualitative progression anchors remain floor 1 at about four Essences per character and floor 10 at about six.

## Controlled experiment

`Fixtures/tower-entry-ranks.json` adds the `tower-entry-ranks-v1` catalog to Tower Lab. Four level-30 parties retain the anchor's exact four Essence selections, seven Standard tier-1 items, baseline rolls and Guardian/Restorer/two Strikers/Controller composition. Only equipment reinforcement rank changes: 0, 1, 2 or 3. Essences remain level 1, unascended and unevolved; styles, scouting and contributions are absent. The existing level-50 six-slot tier-2 rank-3 checkpoint is retained separately. Actual RequiredSlots determine each expedition's size.

The saved protocol fixes a 20-trial discovery schedule at master seed 1337 on **all 15 released floors**: 75 cells, **1,500/1,500 valid battles**. Before measurement, confirmation was reserved at master seed 8675309 with 100 trials per cell on floors 1 and 10. The rule selects the lowest positive rank with any floor-1 discovery win, retains rank 0 and the six-slot checkpoint, and makes no numerical balance acceptance decision. This selected rank 2; confirmation completed **600/600 battles**. Confirmation uses discovery's frozen content and selected settings with the same retained executable. Seed schedules do not overlap.

## Results

| Floor-1 equipment rank | Discovery wins / 20 | Mean fight duration | Mean Garran health left |
| --- | --- | --- | --- |
| 0 | 0 | 157.91 s | 28.14% |
| 1 | 0 | 167.81 s | 22.82% |
| 2 | 3 | 176.76 s | 15.05% |
| 3 | 5 | 183.79 s | 7.59% |

| Reserved-seed checkpoint | Wins / 100 | Descriptive 95% Wilson interval |
| --- | --- | --- |
| Floor 1, four Essences, rank 0 | 0 | 0–3.70% |
| Floor 1, four Essences, rank 2 | 10 | 5.52–17.44% |
| Floor 10, six Essences, tier 2 rank 3 | 100 | 96.30–100% |

The rank-0 and rank-2 four-slot parties both lost all 100 floor-10 confirmation trials. Their floor-1 losses are party defeats, not time limits. In the 100 floor-1 confirmation trials, the Restorer was the first casualty in 91 rank-0 fights and 92 rank-2 fights. Mean time to the first casualty increased from 90.77 to 108.90 seconds. This identifies support survival as a useful next controlled variable; it does not prove that the healer alone causes the losses. These counts are retained in `death-diagnostics.json`.

Next: compare the Restorer's equipment protection and a bounded set of legal four-Essence loadouts while holding level/rank fixed. If suitable entry builds still struggle, test floor-1 tuning overlays against the retained controls. Reinforcement alone has not established a broadly approachable entry point. Gear affordability, intended clear rate and composition coverage remain open; no production coefficient or accepted baseline is changed. The starter 50–90% target is not applied. Phase 2 integration stays deferred.

## Evidence and verification

Saved runs beneath `TestResults/balance/tower-dashboard-20260909/browser-runs/`:

- `tower-entry-ranks-20260909-discovery/run/`: full all-floor Markdown/JSON report, frozen inputs/content and battle records.
- `tower-entry-ranks-20260909-confirmation/run/`: reserved-seed report and records.

`TestResults/balance/tower-entry-ranks-20260909/` retains the executable, protocol, confirmation catalog/source, logs/TRX, diagnostics, summary, verification and checksums. **Eight detailed replays matched**: the first floor-1 trial at every discovery rank, the three stated confirmation checkpoints, and rank 2's first confirmation victory (`tower.0002`, selected after measurement to inspect a win).

```powershell
./build/run-tests.ps1 -Configuration TowerEntryVerification -Filter 'FullyQualifiedName~BalanceHarnessTower'
dotnet TestResults/balance/tower-entry-ranks-20260909/executable/BalanceHarness.dll tower-benchmark --catalog LL/tools/BalanceHarness/Fixtures/tower-entry-ranks.json --content-root LL/src/API/API.LL --seed 1337 --output TestResults/balance/tower-entry-ranks-new
```

**66 tests passed**, zero failed/skipped. The separate configuration avoided a Release DLL locked by another active test process; the initial sandbox attempt also lacked NuGet configuration access. Both obstacles were resolved. The clean isolated build emitted 33 existing warnings. No verification command remains blocked; the full backend suite was not rerun for this catalog/test change.

Tests establish rank-only recipe differences, all-floor legal materialization, preserved six-slot recipes, paired/disjoint seed schedules, repeated gameplay, replay and dashboard discovery. Added independent persisted normal Tower preparation/playback/outcome cases cover ranks 1 and 3. Existing cases retain parity for the original four- and six-slot anchors. Tower Lab serves the retained build for new evidence replay; older evidence remains replayable with its own retained executable.

Changed files are the new fixture, the three Tower test classes, this review, the harness README and plan. All original catalogs, accepted starter evidence and unrelated changes are preserved. No production combat/content, schema, migrations, production configuration or deployments are changed.
