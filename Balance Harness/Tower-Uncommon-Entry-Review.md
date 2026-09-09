# Tower floor-1 Uncommon equipment budget — 9 September 2026

**Verification follow-up:** the [subsequent curve increment](Tower-Curve-Review.md) ran all 70 existing Tower tests successfully, including this catalog's independent normal-Tower parity and materialization checks, then passed 79 tests with new curve coverage. The temporary compilation block described below is resolved; its original attempts and evidence remain historical. No earlier measurement bundle was changed.

The user specifies that floor 1 should be beatable with **four Essences per character, Uncommon equipment, Standard or Fine quality, and reinforcement rank 1–2**. This replaces the earlier Common-equipment assumption for the intended entry point. The prior Common investigations remain valid historical measurements and are preserved.

## Scope and controlled budgets

`Fixtures/tower-entry-uncommon.json` adds `tower-entry-uncommon-v1` with four combinations: `standard-rank-1`, `standard-rank-2`, `fine-rank-1`, `fine-rank-2`. Every equipped item uses its actual production `.rarity.uncommon` definition. Quality and reinforcement use the normal equipment evaluator; no synthetic stat multiplier or boss change is applied. All seven items per character use the cell's quality/rank.

Level 30 and tier 1 remain modeling assumptions. The four level-1, unascended, unevolved Essences and ordered Guardian/Restorer/two Strikers/Controller cell are unchanged. Actual RequiredSlots fill every floor. Rolls remain baseline, with no styles, scouting or contributions. The existing six-slot level-50 Common Standard tier-2 rank-3 checkpoint is included separately; the user has not supplied a replacement floor-10 gear budget.

The protocol was recorded before running: 20 discovery trials per party/floor at seed 1337 on **all 15 released floors** (1,500 battles), then 100 trials for all five parties on floors 1 and 10 at reserved master seed 20260909 (1,000 battles). Every combination is confirmed without selecting winners after discovery. The two seed schedules do not overlap.

## Results

All **2,500 battles completed**. Floor-1 results:

| Uncommon equipment | Discovery wins / 20 | Confirmation wins / 100 | Descriptive 95% Wilson interval |
| --- | --- | --- | --- |
| Standard, rank 1 | 13 | 50 | 40.38–59.62% |
| Standard, rank 2 | 16 | 89 | 81.37–93.75% |
| Fine, rank 1 | 20 | 100 | 96.30–100% |
| Fine, rank 2 | 20 | 100 | 96.30–100% |

These fixed builds demonstrate that floor 1 is beatable within the user's intended budget. The rarity clarification materially changes the conclusion from the earlier Common-gear checks. No floor-1 nerf is supported as a necessary action by this measurement. This is not a claim that every four-Essence composition wins, a mixed-quality guarantee, or an approved numerical balance gate. The starter 50–90% band is not applied.

The six-slot party won **100/100** floor-10 confirmation trials. All four level-30 entry parties lost all 100 floor-10 trials. The full discovery report retains every released floor, including losses beyond the intended entry point. No intermediate slot/floor curve is inferred automatically.

## Verification and evidence

Measurement used the retained Tower-entry-ranks executable and its frozen content/settings because concurrent Combat Styles edits initially prevented a current-tree test build. The matching executable and frozen source are copied into `TestResults/balance/tower-entry-uncommon-20260909/`; earlier evidence is untouched. **Nine detailed replays matched**: first floor-1 trial for every discovery and confirmation combination, plus the six-slot floor-10 confirmation checkpoint.

A subsequently compiled current harness also completed **75 smoke battles**, one matching discovery seed for every floor/party. All 75 pairs matched prepared participants, full gameplay summary, success, guardian health and displayed duration against the retained-build measurement. This checks cross-build repeatability for those trials; it is not independent normal-Tower parity or a full repeat of all 2,500 fights. Both executables are retained separately.

The required test command was attempted three times:

```powershell
./build/run-tests.ps1 -Configuration TowerUncommonVerification -Filter 'FullyQualifiedName~BalanceHarnessTower'
```

**New tests remain unexecuted.** The first build failed on missing concurrent Reprisal definitions; after those definitions appeared, compilation reached the test project and failed in unrelated `CombatStyleReprisalEngineTests.cs` (`AbilityEffectSpec[]` target-typed `new()` errors); a third attempt hit the same issue after further concurrent edits. Those files were not changed by this work. The added tests cover resolved rarity/quality/rank and exact Essence budget, all-floor legality, seed separation, repeated results/replay, dashboard discovery, and independent normal Tower parity at the two budget extremes. Rerun the command after the concurrent test compilation issue is resolved. The previous 66 passing Tower tests belong to the earlier increment and are not reported as a fresh pass here.

Saved discovery/confirmation reports are under `TestResults/balance/tower-dashboard-20260909/browser-runs/tower-entry-uncommon-20260909-{discovery,confirmation}/run/`. Protocol, retained/current executables, frozen source, logs, nine replays, current-build smoke/parity, summary and checksums are under `TestResults/balance/tower-entry-uncommon-20260909/`. Tower Lab exposes the new catalog and both saved reports; it serves the measurement executable so these runs can be replayed.

## Remaining scope and changed files

The next balance work can use this explicit Uncommon entry budget. Broader Essence compositions, mixed Standard/Fine sets, actual acquisition timing and intended win-rate/pacing policy remain unmeasured. The current build/test compilation issue must be cleared before the new automated checks can be counted as passing. Phase 2 integration stays deferred.

Changes are limited to the new catalog, three Tower test classes, this review, the README and plan. No production data or combat coefficients, migrations, production configuration, deployments or accepted baselines are changed.

```powershell
dotnet TestResults/balance/tower-entry-uncommon-20260909/executable/BalanceHarness.dll tower-benchmark --catalog LL/tools/BalanceHarness/Fixtures/tower-entry-uncommon.json --content-root TestResults/balance/tower-entry-uncommon-20260909/frozen-source --output TestResults/balance/tower-entry-uncommon-new
```
