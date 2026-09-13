# V10 stagger-reservation implementation review

Completed **13 September 2026**. The opt-in `independent-stagger-reservation-v10` policy is implemented and verified, with ordered methods `["compatible-defense-joint", "stagger-reservation-joint"]`. **243 relevant tests pass. All six archived v9 arms and all three v10 comparator arms reproduce exactly.** The constructor probe used 288 synthetic new-arm measurement callbacks, zero campaign fights and no experimental seed allocation. Integration tests execute correctness-fixture battles separately; this increment is not a scientific combat pilot.

The [pre-implementation contract](../TestResults/balance/tower-stagger-reservation-implementation-20260913/implementation-plan.md), [frozen implementation/probe protocol](../TestResults/balance/tower-stagger-reservation-implementation-20260913/protocol.json), [producing inputs](../TestResults/balance/tower-stagger-reservation-implementation-20260913/probe-inputs.json), [probe receipt](../TestResults/balance/tower-stagger-reservation-implementation-20260913/probe-verification.json) and [final integrity receipt](../TestResults/balance/tower-stagger-reservation-implementation-20260913/final-verification.json) retain the specification, bounds and checks. The [active plan](Tower-Stagger-Reservation-Plan.md) and [handoff](Tower-Coverage-Replication-Plan.md) describe the next separately frozen pilot.

## Isolated behavior and design

After unchanged uniform recurring-control provider selection during guided fresh construction, the new arm derives `T` from the target's existing `CalculateThreshold(requiredPartySize, 0)` and positive `P` from exactly one supported direct Stun/Freeze route. If enabled stagger permits a first break and `ceil(T/P)` is attainable, one count draw samples uniformly from that minimum through the party size. Safe widened integer arithmetic avoids addition overflow. Every other category keeps its previous count algorithm.

Absent/disabled stagger, a nonpositive maximum-break limit, missing/unknown/nested/multiple routes, nonpositive chance or power, no selected trigger, and unattainable capacity retain the original zero-through-party-size draw. Providers are not removed or resampled, counts are not clamped, and failed placements are not repaired. Existing content validation remains in place.

Optional metadata retains the boss definition, first threshold, complete authored ability/effect definitions, selected/default triggers, predicates and use limits, authored chance, separate runtime control gate, evidence keys, nominal power/minimum and fallback reason. Conditional providers remain eligible. No timing cutoff, expected-probability margin, control-team count, new objective or fitness term is introduced. The metadata builder accepts only reference-free generation inputs and the typed inventory; the reporting command is not a generator dependency.

New-arm guided proposals retain structured `reservations`: selected provider/evidence, threshold/power/minimum, requested and satisfied placements, and fallback reason. Old policies and comparator proposals omit new fields. Uniform routes and mutations do not invent provider nominations. Shared copies retain the existing `Add` behavior and can satisfy another category without consuming another copy. Requested counts do not imply additional occupied slots or accepted stagger applications.

The one-in-eight uniform route, empty-pool fallback, core-insertion skip, filler and ordering rules, ownership/family legality, mutations, operator schedule, parent selection, exploration/beam, ranking and reference boundary remain unchanged. The opt-in version/method and fresh-operator provenance checks extend the existing contracts; no defaults change.

## Files changed in this increment

| Files | Purpose |
| --- | --- |
| [TowerStaggerReservation.cs](../LL/tools/BalanceHarness/TowerStaggerReservation.cs) | Typed source-derived metadata, first-threshold count derivation, fallbacks and metadata validation. |
| [TowerBossPartyGenerator.cs](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs), [TowerPartyCoverage.cs](../LL/tools/BalanceHarness/TowerPartyCoverage.cs) | Optional metadata/trace, guarded v10 construction and the single count branch. |
| [TowerBossGeneration.cs](../LL/tools/BalanceHarness/TowerBossGeneration.cs), [TowerBossDiscoveryContract.cs](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs) | Opt-in method pair, immutable trace propagation and provenance checks. |
| [BalanceHarnessTowerStaggerReservationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerStaggerReservationTests.cs) | 25 deterministic fixtures for metadata, count bounds, fallbacks, ownership/overlaps, legality, reference isolation and comparator parity. |
| [BalanceHarnessTowerBossDiscoveryRunTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBossDiscoveryRunTests.cs), [BalanceHarnessTowerBulkTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBulkTests.cs) | V10 ordinary/compact archive, reference invariance, reconstruction, export and interruption/resume cases. |
| [Harness README](../LL/tools/BalanceHarness/README.md), active plans and handoff | Opt-in usage, completed implementation status and the remaining pilot boundary. |

The final receipt compares this increment against its initial working-tree hashes, preserving earlier uncommitted work. Only six existing C# files changed and two were added; all other source files, 73 sealed reviews and 15 preceding evidence packages remain intact. Source snapshots and producing binaries are retained in the implementation package.

## Verification and bounded constructor accounting

- `dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore` passed. Its five warnings are pre-existing in unrelated tests.
- `build/run-tests.ps1 -NoBuild -Filter <15 relevant BalanceHarness Tower classes>` passed **243/243** tests, including all previous 196 v9 checks, 20 coverage-diagnostics checks, 25 new fixtures and two archive cases. The exact invocation and full results are saved in the package's [command record](../TestResults/balance/tower-stagger-reservation-implementation-20260913/verification-commands.json) and [TRX](../TestResults/balance/tower-stagger-reservation-implementation-20260913/regression-tests.trx).
- The initial 25-test run had two test-harness failures: the RNG audit also counted shuffle draws, and a reference fixture targeted the wrong floor. Both fixtures were corrected; the subsequent 25/25 run and full 243/243 regression run pass. All development logs/TRX remain saved.
- The local source-free driver's first restore was blocked by sandbox access to the user's NuGet configuration. A scoped approved restore succeeded; driver build passed without warnings. No verification command remains blocked.
- The preallocated probe reused v9's three excluded generation seeds. It reconstructed all six v9 arms with **576** saved callbacks, then ran three v10 comparators with **288** saved callbacks and three new arms with **288 synthetic all-zero callbacks**. All **1,176 proposal dispatches** remain saved, including rejections/duplicates. Each arm reached 96 evaluations within its 2,048-proposal cap. No combat executor or held-out outcome was used by the probe.
- The probe completed in **1.072 seconds**, retaining approximately **31.1 MiB** before its receipt, within 600 seconds and 256 MiB. These are software-probe measurements, not performance claims about combat. Build/test/documentation/final-verification timing is separate.

The [saved new-arm constructor output](../TestResults/balance/tower-stagger-reservation-implementation-20260913/v10-constructor-probe.json) records **575 category nominations**, including **115 control nominations**. The [metadata](../TestResults/balance/tower-stagger-reservation-implementation-20260913/v10-metadata.json) preserves all 80 Essences and unchanged 71 augmented/69 compatible features. Current first-threshold capacity is 250:

| Provider ability | Authored power | Derived minimum | Observed requested range in the synthetic probe |
| --- | ---: | ---: | --- |
| Fae's Charm | 40 | 7 | 7–10 |
| Feral Pounce | 50 | 5 | 5–10 |
| Drag Beneath | 35 | 8 | 8–10 |
| Brutal Charge | 25 | 10 | 10 |

All 115 control nominations were satisfied in this hypothetical-ownership probe; scarce-ownership fixtures separately verify unsatisfied requests. Brutal Charge still requires target health below 30%, and all four routes remain subject to their authored/runtime gates. These construction observations do not establish earlier breaks, survival gains, win rates or near-optimality. Synthetic measurements and recipes must not nominate pilot finalists.

## Remaining scientific boundary

The newest seed ledger is byte-identical to the preceding ledger: exclude the union of every array, **471,656 distinct seeds**, including unused and constructor-only reservations. No pilot schedules have been allocated. A fresh v10 comparison still needs its own executable protocol, producing hashes and disjoint schedules before combat, with the proposed maximum **9,224 battles**, 600-second measured phases, 1 GiB package/512 MiB campaign caps, zero retries and no extensions.

Retain rank-one primaries before validation, exploratory rank-two candidates, all six controls outside generation and the original fixed anchor. The unchanged two-of-three reliability gate, adjusted rate/paired comparisons, observed >50% breach rule and per-cohort viable-party requirement remain binding. Software correctness does not change v9's **Fail 0/3** reliability or **Inconclusive** family assessment. Do not pool closed studies, tune the boss, promote defaults/catalogs or expand floors on this evidence.

Kharad remains **Health 3.04881408 / Power 3.85370128**, with the same ten-character fixed-gear, five-slot, untrained/unevolved Essence budget. Gameplay assemblies, all 16 content files and both retained-build catalogs are unchanged. There are **no migrations, configuration changes or deployments**. The offline harness must be rebuilt to use the opt-in policy; application deployment is unnecessary.
