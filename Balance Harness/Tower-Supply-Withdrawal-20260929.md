# Withdraw unrequested Tower supply rewards — 29 September 2026

The user rejected guaranteed selectable equipment supplies as an unnecessary change to the equipment grind. The expected repeating gear curve specifies a progression target; it does not authorize awarding that exact, already-reinforced gear after seven or eight successful dungeon clears. The previous implementation made an unsupported economy decision.

## Correction

The normal `EquipmentAcquisitionService` no longer accepts or calls `ITowerEquipmentSupplyService`. The game service collection no longer registers the supply issuer or supply catalog. `TowerSupplyAcquisitionEnabled` now defaults to false; even explicitly setting the legacy flag true cannot restore the removed game integration. Existing ordinary equipment rolls, blueprint rewards, entry costs, reinforcement and dismantling rules remain unchanged.

The isolated catalog, helper types, item definitions and generic selection UI support remain in the checkout for historical tooling and compatibility. They are not an approved gameplay feature or a rollout recommendation. No fresh normal dungeon completion issues these chests, and the normal service provider does not expose Tower chest selection metadata. No compensating reward increase, replacement guarantee or boss retuning was introduced by this correction.

Production changes are limited to [EquipmentAcquisitionService.cs](../LL/src/Infrastructure/Service/Services.LL/Items/EquipmentAcquisitionService.cs), the supply-only registration hunk in [DependencyInjection.cs](../LL/src/Infrastructure/Service/Services.LL/DependencyInjection.cs), and the legacy option default in [IStarterEquipmentService.cs](../LL/src/Core/Application/Interfaces/Services/LL/Items/IStarterEquipmentService.cs). Concurrent registration changes for LiveOps, analytics and combat styles remain intact.

## Interpretation of previous studies

Frozen studies, builds, seed reservations and audits remain historical evidence for their captured inputs. None is rewritten or rerun. However, supply-funded equipment, dungeon successes, XP, mastery, quest completions, Tower unlocks and core projections cannot be transferred to ordinary player progression. This includes the later level-40/five-Essence parties and their floor-5 results. Their states developed through an economy the user did not approve.

The core-source audit still demonstrates native reward/ascension boundaries under its recorded conditions; its 120/240 funding result is not a forecast for the ordinary economy. Floor-10/11 supplied benchmark comparisons remain conditional combat evidence. They do not demonstrate that ordinary players can obtain those loadouts at the intended point.

Fixed-budget Tower calibration can continue with the user's declared equipment assumptions, clearly labeled as benchmark loadouts. This withdrawal does not require another dungeon or acquisition study before the separate Tower-only pass. It does not alter concurrent calibration files, search settings or guardian content.

Any future claim about acquisition pace must use existing ordinary drops, quest/protected rewards, blueprints, reinforcement costs, resource sources and failures. Reuse a historical prefix only if it ends before the first supply-dependent effect and its inventory, activity, claims and clocks reconcile. Removing chest items from the latest inventory is insufficient because earlier outcomes and downstream rewards may also change. Preserve the repeating gear curve, personally owned stronger gear and supported search. Do not introduce a replacement guaranteed source without the user's design decision.

## Verification

Regression checks cover eight normal completions with and without ordinary equipment drops and with the legacy supply flag both false and true. They verify no chest or supply marker, ordinary equipment still awarded when its configured roll succeeds, and no duplication on retry. Service registration checks reject the issuer/catalog under either flag. Direct historical-adapter tests require explicit opt-in; the default adapter awards nothing.

The old supply-backed acquisition entry point rejects the default configuration. Two full supply-funded projection unit scenarios are explicitly skipped as withdrawn; their bodies and frozen originals remain preserved. Lower-level ownership/carry checks remain covered. Current continuation tests verify that disabled supply claims preserve existing equipment and add no rewards. Native roster restoration now preserves the recorded supply-processed flag instead of forcing it true. First-clear tests retain real Tower rewards without expecting newly unlocked supplies.

All backend execution uses `build/run-tests.ps1` with a fresh `TestResults/tower-supply-withdrawal-build-20260929` directory. The initial sandboxed build could not read the local NuGet.Config; the wrapper was retried with authorized access. Preliminary failures exposed old supply-enabled expectations and a hard-coded restoration flag; those logs/TRX remain preserved.

The corrected build and regression run passed with **867 passed, 30 skipped, 0 failed (897 total)**. The skips comprise 28 existing opt-in study cases and the two withdrawn supply-funded projection scenarios. The authoritative output is [the isolated regression log](../TestResults/tower-supply-withdrawal-regression-fixed-20260929.log); the wrapper finished with exit code 0. Concurrent Tower work replaced the wrapper's shared `TestResults/tests/tests.trx` before both preliminary and final copies. Those unrelated one-test copies are retained under `concurrent-shared-preliminary-results.trx` and `concurrent-shared-results.trx` in the withdrawal build directory; neither is evidence for this regression run. No repeat run was needed after all relevant checks passed. The scope receipt is [final-checks.json](../TestResults/tower-supply-withdrawal-build-20260929/final-checks.json). `git diff --check` passed; no required verification remains blocked.

No schema migration, API startup, seeding, shared-database access, commit or deployment was performed. The source-level legacy-option default changed; no environment configuration file changed. The earlier supply rollout instructions are superseded by this withdrawal.
