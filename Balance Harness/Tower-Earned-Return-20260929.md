# Earned return to the World Tower — 29 September 2026

The funded acquisition wave now has a qualified, chronological Tower follow-up. All **15 model servers entering floor 4 clear within four attempts**, thirteen on their first attempt. Of seventeen previously stopped servers, one advances from floor 1 and one from floor 2; fifteen remain stopped. Production bosses, the repeating equipment bands and the supported search were unchanged.

## Retained state and controls

This continues the [funded next-entry wave](Tower-Funded-Next-Entry-20260929.md), corrected native-runtime qualification and [early Tower histories](Tower-Early-Unlock-Progression-20260929.md). All 32 server paths retain sixteen personal owners: 512 owner/server alternatives, including 192 paid entrants and 320 held combinations. These alternatives share personal histories and are not 512 independent player observations.

Entrants retain exact post-wave characters, all personally owned equipment, native source/growth/mastery/prophecy runtime and ordinary-loot blueprint pity. Held owners retain corrected runtime and stock. The bridge round-trips each runtime through native restoration, then prepares the original five-person roster through production Tower preparation. Selection keeps stronger owned equipment using the existing deterministic score and tie break. Inventories are never pooled, reset or donated. Of 160 participant loadouts, **35 changed**. The other 49 of the wave's 84 changed loadouts belong to nonparticipants and remain available for party growth.

The declared assembly barrier waits until all sixteen independently modeled owners finish their already-funded wave. Waiting grants no idle activity, XP, new offers or items. Each server keeps its cleared/unlocked floors, scouting progress, historical attempts, tokens, titles and old completion-time chest decisions. Restoring persisted failures matters because native finalization counts them toward the weekly scouting cap. Unit tests verify that a fifth failure cannot reset that cap and that restoring a victory keeps its rewards.

Qualification prepared all 32 legal next floors and three arms per server, **480 friendly combatants**:

- Actual earned post-wave party.
- Matched pre-wave party, with its exact earlier growth and equipment.
- Supplied diagnostic party at actual growth, adding seven floor-4 Epic/Fine/rank-3 candidates per member while retaining stronger owned gear.

Controls do not consume stock, finalize attempts or earn rewards. Owner IDs, positions and ordered Essence recipes stay fixed. The comparison tests the combined acquisition wave; it does not isolate gear from earned XP/training.

## Bounded findings

Each server attempted **one** next legal floor, stopping on first actual victory or four attempts. All three arms shared each fresh attempt seed. The first attempt of every arm was replayed exactly. No search, sample extension or rerun occurred.

| Next floor | Servers | Servers clearing | Actual wins / attempts | Pre-wave wins / attempts | Supplied wins / attempts |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 4 | 1 | 1 / 14 | 1 / 14 | 14 / 14 |
| 2 | 10 | 1 | 1 / 39 | 1 / 39 | 39 / 39 |
| 3 | 3 | 0 | 0 / 12 | 0 / 12 | 12 / 12 |
| 4 | 15 | 15 | 15 / 18 | 12 / 18 | 18 / 18 |

Three actual floor-4 victories pair with pre-wave defeats: `four-of-five--1--3`, `perfect--0--2` and `perfect--1--1`, each on new attempt zero. There are no reverse outcome pairs. Actual and pre-wave outcomes match on every floor-1–3 pair. Supplied controls win all 83 attempts, but those sets are unearned: this shows combat headroom, not solved acquisition.

Stopping depends on the actual arm, so these fractions are conditional diagnostics, not unbiased or reliable-clear estimates. Server paths share owners and preceding histories. Thirteen first-attempt floor-4 wins and fifteen eventual clears do not establish a population success rate.

Execution used **83 actual attempts, 166 controls and 96 exact replays: 345 fights**. Actual engine durations span 33–120.1 seconds, totaling 6,629.8 seconds across alternatives. They exclude player attendance, resource generation and coordination. There are **zero measured acquisition-time samples**. Seven awards remain a historical study stop, not an entry requirement or production cap.

Native finalization rejects premature and duplicate completion. Actual victories add 36,780 Tower tokens and 85 title unlocks across alternatives and unlock the next floors. They add no equipment or personal XP. Terminal highest-cleared distribution: three at 0, ten at 1, four at 2 and fifteen at 4. All 512 personal runtimes and 18,812 owned item references remain exported, including nonparticipants and failed dungeon entrants. The personal pool remains level 34–36 with four Essences.

## Evidence and verification

| Stage | Output | Manifest SHA-256 |
| --- | --- | --- |
| Preparation | `TestResults/tower-return-preparation-study-20260929` | `4a775ba52a633e9571d5429712575c9f9aa9f9f8003025d7ca88ea9e7ec315b7` |
| Combat | `TestResults/tower-return-combat-study-20260929` | `9b9cff072ac42afe75c17485015bc792f34bae5580f5cf789e40bf3fafc2e6be` |

Result pins: `f1341db8c18e0b635bdff193d9aa4ae0ddd5960a9e48798a53e9d5ba50d3f74d` and `8a59418771c98fc2267877b2496ccffddcd087a5cf00ebc0389eaee658cc9a07`. Corresponding `*-owner-20260929` directories retain frozen source/data/runtime, declarations, requests, process receipts, independent audits and study TRX files. Both audits pass **without amendments**, checking 3,726 and 3,763 hashes. Fifteen in-memory corruption checks reject lost histories/gear/prophecy state, invented resources, roster changes, reward resets, changed ordinals and replay drift without modifying an archive.

Preparation ran for 16.453 seconds within a 300-second owner / 240-second native cap. Combat ran for 19.140 seconds within 900 / 840 seconds and a 480-fight cap. Both owners exited with zero active child processes. Outputs are 13,170,926 and 33,365,490 bytes, below 256 MiB; logs are capped at 1 MiB.

Latest ledger: `TestResults/tower-return-combat-owner-20260929/seed-ledger.json`, SHA **`0bd04b6d0061f3ed06bad4e3ec20e9f952bcc5d7afbe986f2f757ae0b76b4986`**. It reserves 128 fresh seeds, consumes 83 and retains **45 unused** values. The full exclusion union is **878,739**, including all 645 unused funded-wave reservations and every earlier reservation. Do not rerun these owners, rename them as retries or reuse unconsumed seeds.

Verification used `build/run-tests.ps1`: **594 relevant regression tests passed**, with **21 intentional opt-in skips**; both bounded study tests passed separately. The command and receipt are in `TestResults/tower-return-combat-owner-20260929/final-checks.json` and `TestResults/tower-return-build-20260929/regression-tests.trx`. The initial sandboxed build could not read the user's NuGet configuration; the same wrapper succeeded with approved filesystem access. No required verification remains unrun.

Re-audit completed outputs without combat using the following, with unused receipt paths:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONDONTWRITEBYTECODE = '1'
& $python 'Balance Harness/analysis/verify-tower-return.py' --owner TestResults/tower-return-preparation-owner-20260929 --manifest-pin 4a775ba52a633e9571d5429712575c9f9aa9f9f8003025d7ca88ea9e7ec315b7 --receipt TestResults/return-preparation-reaudit.json
& $python 'Balance Harness/analysis/verify-tower-return.py' --owner TestResults/tower-return-combat-owner-20260929 --manifest-pin 9b9cff072ac42afe75c17485015bc792f34bae5580f5cf789e40bf3fafc2e6be --receipt TestResults/return-combat-reaudit.json
```

`TestResults` is ignored and local. Preserve all completed directories. Closeout verifies historical manifest pins and all pre-existing uncommitted files, with only the scoped server-helper declaration and two continuation documents intentionally updated.

## Next continuation

Use combat `--continuation.json.gz` as the latest terminal state: actual final server plus all sixteen owners' runtime, equipment and pity. Matching qualification `--preparation.json.gz` contains historical attempts; concatenate them with new actual attempt receipts on the next restoration. New attempt files contain `battles.actual`, `battles.baseline` and `battles.supplied`; only actual outcomes are earned. Do not replay historical combat or finalize controls.

Fifteen floor-4 clears unlock **floor 5, requiring ten distinct owners**. Assemble the first ten members of the existing declared roster from these exports, retaining owned gear. Prepare actual levels/Essences before another combat admission. The authored level-40/five-Essence budget is an evaluation declaration, not a grant or automatically a production entry gate. Native eligibility and actual fifth-Essence acquisition/training remain separate work. The 128 still-funded dungeon combinations have no second-entry outcome; unopened surplus Rare chests are not missing equipment grants. Further activity must use current stock, advanced clocks and actual server unlocks.

Early stopped servers still need acquisition/progression diagnosis. This panel does not justify boss retuning, a new supply cadence or a farming-hours target. Continue resource/Essence/party growth toward floors 10–11, retaining stronger gear across the cycle. Keep `affinity-creation-with-benchmark-validation-v1` and the user-defined curve.

Files changed: `TowerReturnStudy.cs`, `tower-return.json`, `TowerReturnServer.cs`, `BalanceHarnessReturnTests.cs`, the partial declaration in `TowerUnlockServer.cs`, new owner/verifier scripts, this report and handoff/supply-report updates. No production source, dependencies, configuration, migrations, shared databases, API hosts or deployments changed. The supply feature's original future rollout requirements still apply.
