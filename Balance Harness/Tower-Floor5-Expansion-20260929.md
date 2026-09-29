# Earned floor-5 party expansion — 29 September 2026

All fifteen eligible servers can assemble and natively admit ten distinct earned owners for floor 5. **None clears in the bounded panel: 0/60 actual attempts and 0/60 supplied Epic controls.** These owners remain level 34–36 with four level-4/5 Essences. Completing equipment alone does not solve these cases. The next dependency is earned character/Essence progression, not boss retuning; this panel does not establish that a fifth Essence or level 40 alone will solve combat.

## Party growth and native admission

The input is the [earned-return continuation](Tower-Earned-Return-20260929.md). Fifteen paths with floor 4 cleared expand to the first ten seats of the existing `tower-earned-party.json` roster at their original rotation. The original five owners keep their seats, and five additional owners form the second five-person party. There are **75 newly participating owner references**, not newly created or independent players. All gear remains personally owned, with the strongest retained selection; no full-set bill or inventory transfer is introduced.

All sixteen owners per eligible server retain their native personal runtime, equipment, source stock, XP, mastery, prophecy instances and blueprint pity. Their native runtime is restored and checked before production preparation. Seventeen earlier stopped servers receive no new activity and remain pinned unchanged by file/hash reference. Across both groups, all 512 owner/server alternatives remain available.

The isolated server restoration now supports a larger population while retaining historical five-person rallies. It preserves the prior Tower floor states, tokens, titles, unlocks and failures across the intervening acquisition phase. New participants receive **zero retroactive Tower rewards**. Synthetic native regression tests verify both this and correct future rewards to newcomers on a subsequent victory.

Each expanded party passes actual `CreateRally`, application, acceptance and ordered-party assignment calls. Native `WorldTowerPartyRules` verifies slots 1–10 in two parties. Snapshots contain exact earned characters. Rating availability is explicitly modeled at raw rating **1** so these checks do not treat a power recommendation as an entry restriction. Separate accounts and absence of external active expeditions remain model assumptions; no live accounts or ratings were inspected. Admission probes run on disposable isolated servers, then combat restores the qualified initial state afresh. They do not retain active rallies or award anything.

## Fifth-Essence dependency

The production slot rule is `clamp(level / 10 + 1, 1, 10)` using integer division. Level 39 has four slots; level 40 has five. The authored floor-5 level-40/five-Essence budget is an evaluation declaration, not a Tower entry gate or earned grant.

For all **240 personal states on eligible servers**, the native Essence service accepts the existing four-Essence loadout. It rejects a fifth slot at the actual level and, in a separate disposable level-40 probe, rejects the unowned fifth Essence. Those are **720 native loadout decisions**. The hypothetical level is never retained. Production XP costs show **448,487–677,717 additional character XP** is needed to reach level 40 across those states. No activity duration is inferred from that deficit.

The source audit exports all **82 production creature Essence tables**, variant weights, slot thresholds and resonance/focus constants. The export's acquisition rule describes the **creature-drop channel**; quest and other direct item rewards are separate possible sources, not excluded from the economy. Creature drops require eligible creature-specific draws, focus and resonance state. Production failed eligible kills add one resonance point; the relative chance bonus reaches its 1% cap at 12,000 failures. Focus multiplies base drop chance by three and has a separate 1.2 spawn multiplier. These are rules, not assumed focused encounters or earned items.

The preceding four-Essence histories did **not** record ordinary Essence-drop draws, creature resonance or an additional unbound Essence inventory. We cannot retroactively award a fifth Essence or claim that historical resonance was zero. Native absorption consumes an owned unbound item; a loadout also requires distinct creature sources. Acquisition/absorption can precede level 40, but fifth-slot attunement cannot. A future model must explicitly qualify a funded quest/drop source, preserve or bound missing historical state, and train the newly earned Essence. Current fixed four-Essence runtime helpers also need a tested extension before retaining a fifth. No Essence, level, training, ascension, sigil or idle activity was granted here.

## Bounded combat

The actual ten-person party is paired with a supplied control at the **same actual character and Essence growth**. Controls add the seven floor-4 Epic/Fine/rank-3 candidates per member while retaining stronger owned items. They are not earned sets or level-40/five-Essence benchmarks and never finalize rewards.

The fixed rule is one floor only, four actual attempts or first actual victory, shared per-attempt seeds across both arms, and an exact first-attempt replay of each arm. All fifteen parties reach the four-attempt cap. **60 actual fights + 60 controls + 30 replays = 150 fights**, exactly the declared cap; no extension or search followed the failures.

| Arm | Victories / attempts | Engine duration range | Guardian health remaining |
| --- | ---: | ---: | ---: |
| Earned | 0 / 60 | 38–54 seconds | 87.21–92.39% |
| Supplied Epic, actual growth | 0 / 60 | 56–70.9 seconds | 65.97–78.31% |

All outcomes are defeats, not timeouts. Gear improves damage and survival but does not yield a clear. These are bounded conditional model alternatives sharing personal histories, not an independent population estimate. There are **zero measured player acquisition-time samples**; engine durations exclude attendance, resource generation and coordination.

Native finalization rejects premature/duplicate completion and preserves the weekly failure cap. All fifteen floor-5 scouting values end at **30**, rather than incorrectly granting a fourth increment. No floor-6 unlock, title, token or personal item is earned. The 8,915 owned item references on eligible branches remain intact, alongside the unchanged held paths. Personal clocks advance by actual Tower duration as waiting time, with no synthesized idle rewards or prophecy observations.

## Evidence and checks

| Stage | Output | Manifest SHA-256 |
| --- | --- | --- |
| Preparation | `TestResults/tower-expansion-preparation-study-20260929` | `b756d8027ae3b6e12258c6773c6eba0ca68b957f9bb976680f0e3e3352771e91` |
| Combat | `TestResults/tower-expansion-combat-study-20260929` | `81306b47b1aa870356dcdb43cc0fb7266449a031e2397a1debf781f19467a722` |

Result pins: `e95d514d3c732c6a5c6a786df74a7f7217e40a76d4b04460c74677732a7a647b` and `00d420d631796c2b97e5f6ec92d1d3715f41b95a2a5cf1a8d23a7fa412252876`. Corresponding `*-owner-20260929` directories preserve frozen source, production data, runtime, declaration, process, audit and TRX receipts. Audits passed **without amendments**, checking 2,981 and 3,002 input hashes. Nineteen in-memory corruption checks reject ownership, roster, old-reward, fifth-slot, XP, replay and timeline corruption without changing archives.

Preparation ran 15.156 seconds within 300 owner / 240 native seconds; combat ran 14.906 seconds within 900 / 840 seconds. Both exited with zero child processes. Outputs are 8,115,494 and 23,959,953 bytes, below 256 MiB caps; logs are capped at 1 MiB.

Latest ledger: `TestResults/tower-expansion-combat-owner-20260929/seed-ledger.json`, SHA **`7473b0172d868e81d872d8caffb0bdf33fab41986c4ae4cc79720e4ec27a8fa2`**. All 60 fresh seeds were consumed, bringing the union to **878,799**. Every older unused reservation remains excluded, including the return study's 45 and funded wave's 645. Preserve the completed panel; do not rerun it, rename it as a retry, or extend it.

Backend verification ran only through `build/run-tests.ps1`: **689 passed, 23 intentional opt-in skips**, plus both owned study tests. This includes prior acquisition/progression/Tower checks and native Essence service/archive tests. The first sandboxed build could not read local NuGet configuration; the same wrapper passed with approved filesystem access. No required check remains unrun. The regression command, historical/source preservation checks and results are recorded in `TestResults/tower-expansion-combat-owner-20260929/final-checks.json`; TRX is in `TestResults/tower-expansion-build-20260929/regression-tests.trx`.

Re-audit without combat, using unused receipt names:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONDONTWRITEBYTECODE = '1'
& $python 'Balance Harness/analysis/verify-tower-expansion.py' --owner TestResults/tower-expansion-preparation-owner-20260929 --manifest-pin b756d8027ae3b6e12258c6773c6eba0ca68b957f9bb976680f0e3e3352771e91 --receipt TestResults/expansion-preparation-reaudit.json
& $python 'Balance Harness/analysis/verify-tower-expansion.py' --owner TestResults/tower-expansion-combat-owner-20260929 --manifest-pin 81306b47b1aa870356dcdb43cc0fb7266449a031e2397a1debf781f19467a722 --receipt TestResults/expansion-combat-reaudit.json
```

## Next continuation and scope

For the fifteen expanded servers use the new combat `--continuation.json.gz` files: sixteen exact owner states, final Tower state and four failed floor-5 attempts. For the other seventeen use the original return continuations named in `result.held`. The expansion preparation's `continuation.historicalAttempts` preserves earlier actual history; append the new actual receipts on restoration. Historical roster sizes differ across phases: keep five-person old rallies and ten-person floor-5 rallies. The acquisition boundary remains explicit at `personalRefreshBefore`. Future restoration must not reset the floor-5 weekly failure cap.

Next qualify continuing earned XP and an actual additional Essence source, with unknown historical drop/resonance state explicitly bounded or declared. Preserve current source stock and native prophecy runtime; no second dungeon outcome exists for the 128 still-funded combinations. Extend runtime ownership/attunement beyond four only with native acquisition/training checks, then freeze changed parties before fresh combat. Do not add levels or a fifth Essence to actual parties merely to match the authored budget. Stronger owned equipment must remain carried into larger parties and eventually floor 10→11. Keep the repeating curve and `affinity-creation-with-benchmark-validation-v1` unchanged.

Changes are confined to `TowerExpansionStudy.cs`, `TowerEssenceEligibility.cs`, `tower-expansion.json`, expansion server/tests, the existing server restoration helpers, the new owner/auditor scripts and documentation. No production source, dependencies, configuration, migration, shared database, API host or deployment changed. Existing uncommitted work and historical evidence are preserved; original supply rollout requirements remain applicable.
