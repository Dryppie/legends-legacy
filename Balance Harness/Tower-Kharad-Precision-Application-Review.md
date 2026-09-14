# Kharad +16% checked local application — 13 September 2026

**Applied locally and verified.** Kharad now uses **Health 3.5366243328 / Power 4.4702934848**, the candidate accepted by the [complete 17,821-recipe precision decision](Tower-Kharad-Precision-Resolution-Review.md). All **four complete local detailed reports** match their archived candidate replays. **Eight diagnostic fights** completed in **19.94 seconds**, with zero retries, and **89 relevant backend tests passed**. No new acceptance sample or seed reservation was created.

## Exact change and accepted scope

Only two values in the primary game's [Tower definitions](../LL/src/API/API.LL/Data/world-tower/tower-floors.json) changed:

| Floor-5 field | Previous | Applied | Factor |
| --- | ---: | ---: | ---: |
| `guardianScaling.health` | 3.04881408 | **3.5366243328** | 1.16 |
| `guardianScaling.offense` (Power) | 3.85370128 | **4.4702934848** | 1.16 |

The common factor preserves the Health/Power ratio. Every other value and local line ending remains unchanged. **All 16 Tower content files match the archived accepted candidate byte for byte**, including the floor file. The original 15 CRLF endings are preserved. No combat/search source, generator defaults, retained catalog, gear, Essence or other-floor value changed.

The source composite assessment remains **Pass**: the refreshed team won **339/1,000 (33.90%)**, adjusted **30.27%–37.73%**; all 17,821 final upper bounds are ≤50%, with a maximum of **49.31503%**, and 18 teams have supported ≥10% viability. Its original .025/.0125/.0125 uncertainty allocation and source evidence remain unchanged. The original 489,168-fight full-family report stays Inconclusive at its earlier allocation. These are scoped approximate bounds, with no lifetime repeated-study or unseen-build guarantee.

The budget remains ten level-40, tier-1, rank-2 Standard characters with five level-1 unascended/unevolved Essences, exact fixed gear, no styles or contributions, and all 80 eligible Essences under hypothetical ownership. Application does not establish practical acquisition or strongest-player readiness.

## Frozen diagnostics and result

The [application plan](Tower-Kharad-Precision-Application-Plan.md) and [protocol](../TestResults/balance/tower-kharad-application-20260913/protocol.json) froze before any combat or content change. Protocol SHA-256: `2287a49d23f7a369898c8026c85efecacc81396dd7a5e9fe83e4c073006c16cd`. The [preparation receipt](../TestResults/balance/tower-kharad-application-20260913/preparation-verification.json) verified **88,350 source artifacts** across the sealed full-family and precision study/work packages, the accepted decision, candidate content, producing identities, source files and catalogs.

Four existing observations were selected for parity: the fresh team's first Victory and first Defeat, the highest observed-rate retained second-stage team, and the previous candidate leader. Both retained teams use the first original second-stage seed. These outcome-aware choices exercise deterministic branches; they provide no new estimate of win rate.

| Case | Saved recipe | Existing seed | Outcome | Full local report |
| --- | --- | ---: | --- | --- |
| precision-victory | `team-db3598435c3754b58e2b6d9c10ff7be2` | -549489645 | Victory | Match, including 10,052 events |
| precision-defeat | `team-db3598435c3754b58e2b6d9c10ff7be2` | -291534997 | Defeat | Match, including 8,251 events |
| retained-leader | `team-83da44857d911e2966ae36379a458153` | -943260780 | Victory | Match, including 9,328 events |
| prior-leader | `team-a7e5de669c4a17287d84060e8ab6359b` | -943260780 | Victory | Match, including 9,467 events |

The [selected recipes and source identities](../TestResults/balance/tower-kharad-application-20260913/selected-cases.json) remain saved. The two retained cases replayed with their original full-family harness; the two refreshed cases and all local comparisons used the original precision harness. Both identities have identical gameplay assembly hashes, runtime, OS, architecture and settings. No producing harness was rebuilt.

Candidate replays consumed **16.47 seconds** for four fights; local comparisons consumed **3.46 seconds** for four fights. All eight starts completed and the durable journal is exact. The aggregate diagnostic cap was **600 seconds**, with **512 MiB** for the application package and zero retries/resume/extensions. Preparation, builds, tests, documentation and no-combat integrity reconstruction are outside diagnostic-phase time/fight accounting. The audit measured **147,688,071 bytes** before final documentation and receipt; the final receipt records the final package size.

The [application receipt](../TestResults/balance/tower-kharad-application-20260913/application-receipt.json) links the successful pre-application report receipts to the exact two-value change. The [local comparison receipt](../TestResults/balance/tower-kharad-application-20260913/after-receipt.json) records canonical report hashes. The [independent audit](../TestResults/balance/tower-kharad-application-20260913/audit.json) separately compared every complete JSON report, including all 11 participants, outcomes, durations and event logs, and verified the exact content diff, journal, candidate equality and test receipt.

## Verification and preserved evidence

The two small diagnostic helper builds completed with **zero warnings and errors** using captured dependencies, an empty-source NuGet configuration and temporary APPDATA. No repository dependency changed. Existing backend test binaries were used because only content changed:

```powershell
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~EssenceSystem.Tests.WorldTowerTests'
```

All **89 tests passed**, zero failed or skipped, covering the production Tower preparation/playback path, persisted parity, content definitions, multiple floors and invalid inputs. The [TRX receipt](../TestResults/balance/tower-kharad-application-20260913/tests.trx) is retained. Fixture test combats are separate from the eight application diagnostics. No mirror test was added for two literal values.

The application verifier also reconstructed the **complete 17,821-recipe source precision Pass**, both original source stages and the fresh archive, in **423.20 seconds**, under a guard forbidding combat. It checked all four application report pairs and attempt accounting. Run from the repository root without changing the sealed package:

```powershell
dotnet TestResults/balance/tower-kharad-application-20260913/driver/precision/bin/Release/net10.0/Driver.dll verify
```

The older top-level study helpers intentionally check their original workspace content fingerprints; those checks now recognize that this local floor file changed. They are preserved unchanged. The application verifier uses the captured precision API and archived source content while explicitly checking the approved local after-content hashes. This reconstructs the original decision without weakening its evidence checks or rewriting historical packages. Keep all referenced sealed packages alongside this application package. Never rerun its diagnostic or apply phases.

The [final verification receipt](../TestResults/balance/tower-kharad-application-20260913/final-verification.json) records the protected inputs, complete source-package hashes, changed-file set, current documentation, byte-for-byte candidate equality and manifest. `git -c core.safecrlf=false diff --check` and local Markdown links passed. No required command remains blocked.

Changed repository scope: the Tower definitions JSON, six active handoff/discovery/policy/harness guides, and this new application plan/review. The ignored application directory retains the protocol, original/proposed bytes, helpers, reports, scripts, logs and receipts. Preexisting unrelated working-tree changes and historical reviews remain preserved.

## Runtime implications and next work

No service was deployed or restarted, and there are no migrations or shared-database changes. The Tower definition provider loads the catalog at construction and is registered as a singleton, so an already running service must reload/recreate it or restart before using these values. This review confirms the local content file and its production-path parity.

The authoritative [476,054-reservation precision ledger](../TestResults/balance/tower-kharad-precision-20260913/seed-ledger.json) is unchanged. The eight diagnostics reused three distinct recorded seeds; exclude every array of that ledger for future fresh studies. Saved recipes remain external references, without catalog promotion or independent-generation ancestry.

**Independent-search reliability at the applied setting remains Fail 0/3.** The earlier Pass 2/3 belongs to the previous lower setting. Diagnose the applied-setting independent-search failure (0/3) from the saved discovery, primary-selection and validation evidence. Distinguish weak party construction from ranking or screening failures, then freeze one targeted bounded comparison. No further fights are allocated by this handoff. Keep the applied content and fixed budget constant during that diagnosis. Practical ownership, unsearched recipes, strongest-player readiness and floors 6–11 remain open.
