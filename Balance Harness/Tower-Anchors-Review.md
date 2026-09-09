# Tower progression anchors — 9 September 2026

The user's stated direction is **floor 1 starting at about four Essence slots per character**, and **floor 10 being doable at about six**. This is a qualitative progression requirement. It does not change expedition size: floor 1 still requires five characters, floor 10 fifteen. No numerical clear-rate policy, intended gear budget or interpolation for other floors was supplied.

## Conditional checkpoint model

The separate `tower-anchors-v1` catalog measures these checkpoints at the first slot-unlock levels:

| Party | Intended checkpoint | Character level | Equipped Essences | Equipment assumption |
| --- | --- | --- | --- | --- |
| entry-4 | Floor 1 | 30 | 4 | Seven Standard tier-1 rank-0 items |
| floor-10-6 | Floor 10 | 50 | 6 | Seven Standard tier-2 rank-3 items |

`EssenceSlotProgression` unlocks four slots at level 30 and six at level 50. Tier 2 is legal at level 50. The entry gear is an unreinforced full-set hypothesis; the six-slot gear reuses the existing mid preset. These level/gear choices are modeling assumptions, not additional user-approved budgets. All Essences are level 1, unascended and unevolved. Baseline rolls, fixed Guardian/Restorer/two Strikers/Controller cells, no styles/scouting/contributions and fresh fights match the existing workflow.

The fixed catalog runs both parties on every released floor for context: 15 floors × two parties × 20 trials = **600 battles**, master seed 1337. Only floor 1 with `entry-4` and floor 10 with `floor-10-6` are the stated checkpoints. The original level-90 slot-count experiment remains unchanged and useful for controlled loadout comparisons, but cannot establish entry-level viability.

## Measured checkpoint results

The full browser-driven run completed **600/600 trials**. Both first-trial checkpoint replays matched saved preparation, combat and outcome.

| Checkpoint | Wins / trials | Descriptive 95% Wilson clear interval | Mean duration | Mean guardian health remaining |
| --- | --- | --- | --- | --- |
| Floor 1, four Essences, level 30, tier 1 rank 0 | 0/20 | 0–16.11% | 157.91 s | 28.1435% |
| Floor 10, six Essences, level 50, tier 2 rank 3 | 20/20 | 83.89–100% | 117.475 s | 0% |

The entry build did not demonstrate viability at the requested floor-1 anchor. The floor-10 observation is consistent with “doable at around six slots” under the assumed mid gear, but is not an approved population target or proof of a true 100% win rate. This narrows the next investigation to the floor-1 entry build/budget and encounter; it does not identify which coefficient or build change is appropriate. No boss was changed and no baseline or numerical target was promoted.

Saved full run: `TestResults/balance/tower-dashboard-20260909/browser-runs/dashboard-20260909-111522-ea99476958ee45b3a7742923490dc824/run/`. The bundle retains all-floor Markdown/JSON reports and frozen content/inputs/execution identity. Matching executable, test log/TRX, checkpoint summary, two replay JSON/log pairs, verification and hashes are in `TestResults/balance/tower-anchors-20260909/`.

```powershell
dotnet TestResults/balance/tower-anchors-20260909/executable/BalanceHarness.dll tower-dashboard --port 5093 --content-root LL/src/API/API.LL --runs-root TestResults/balance/tower-dashboard-20260909/browser-runs
```

## Verification and limitations

The anchor tests verify all-floor coverage, legal preparation, exact first unlock levels and independent persisted normal Tower preparation/playback/outcome parity for both intended checkpoints. Dashboard discovery verifies both parties. `./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessTower'` passed **62 tests**, zero failures/skips, with five existing compiler/analyzer warnings. The full backend suite was not rerun for this fixture/test increment; no verification command remains blocked.

Measurement success is complete verified execution, not meeting an invented win-rate threshold. Small repeated-seed samples do not approve difficulty for all compositions or certify acquisition, ascension, styles or a continuous climb. The starter 50–90% band is not applied. No boss tuning is part of this checkpoint measurement. Phase 2 integration remains deferred.

## Files and use

`Fixtures/tower-anchors.json` adds eight profiles and two parties. The three Tower test classes add unlock-level/materialization checks, independent checkpoint parity and dashboard discovery. The README and plan record the user's qualitative anchors separately from the assumed gear. Existing catalogs/evidence and unrelated working-tree changes remain preserved. No combat implementation, production content, schema, configuration, database migration or deployment changes are introduced.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-benchmark --catalog LL/tools/BalanceHarness/Fixtures/tower-anchors.json --output TestResults/balance/tower-anchors-new
```
