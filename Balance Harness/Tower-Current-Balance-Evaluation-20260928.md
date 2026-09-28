# Tower evaluation after the attribute and gear changes

The offline Tower harness now selects **attribute rules 18, equipment release 4 and `healing-v1`**, matching the repository's game configuration. Previous Tower construction used the implicit release-2 catalog and no healing override. Search policy, progression budgets and game configuration were not changed by this work.

The completed unchanged search selected an existing reference. On the fresh held-out panel, the original benchmark won **117/128**, both existing controls **128/128**, and the generated finalists **128/128** and **124/128**. The best generated team matches the strongest known references in clear rate; this run does not show generation surpassing them. Keep the supported algorithm. Floor 3 is now close to a ceiling; the next useful quality case is a separately declared floor-15 baseline with a viable existing reference, fixed gear and fresh measurements. Do not reopen algorithm tuning based on this single floor-3 run.

## Supported search and held-out results

The search used the original `affinity-creation-with-benchmark-validation-v1` policy, 17 proposals, **528 fights** and its unchanged 60-pair validation gate. The selected challenger was existing reference 3 (`d17448333a43…`), with **7 gained wins and 0 lost wins** in validation. Its status remains `ChallengerNeedsConfirmation`; this evaluation does not promote a production team.

Before held-out combat, both generated finalists and all three references were frozen. They then received the same 128 separate seeds; held-out results did not change the selected team.

| Team | Held-out wins | Gained / lost versus benchmark | Mean boss health remaining |
| --- | --- | --- | --- |
| Original benchmark `68a156b5380e…` | 117/128 | 0 / 0 | 0.3041% |
| Existing control 2 `5168053b9469…` | 128/128 | 11 / 0 | 0.0000% |
| Selected existing control 3 `d17448333a43…` | 128/128 | 11 / 0 | 0.0000% |
| Generated finalist `00258e358b4a…` | 128/128 | 11 / 0 | 0.0000% |
| Generated finalist `11649c167a2b…` | 124/128 | 11 / 4 | 0.0764% |

The evaluation completed **1,168 fights in 62.58 seconds**; the owned process finished in 64.84 seconds and drained all eight processes. It retained 124,111,185 bytes across 1,383 authenticated files. Limits remained 840 fixture seconds, 900 owner seconds and 1 GiB. **237 new values** (one generation root, 108 search seeds and 128 held-out seeds) were disjoint from **832,176 historical exclusions**; the union is now 832,413. No allocated run was retried or extended. Including the two reference screens, scientific work totaled **1,744 fights**; engineering-test combats are separate.

Native verification reconstructed the search and all 640 held-out inputs/results; all three opted-in fixture tests passed. An independent Python readback authenticated every retained file, recomputed the allocation/rejection journal, verified search/held-out separation and recomputed all 640 held-out wins, health means and paired gained/lost counts. It also checked exact executable equality with the final screen and identical aggregate outcomes between the two screens. No new fights were run during that audit.

Search inputs use the existing canonical Essence-ID ordering; original ordered recipes are retained separately. This remains a composition search, not a permutation search. All canonical references were measured again. The improvement over September 25's failed case spans changed gameplay/content/configuration and seeds; it is not evidence that the search algorithm itself improved.

Completed archive: `TestResults/balance/tower-current-search-final-20260928/`; owner and independent readback: `TestResults/tower-current-search-owner-final-20260928/`. Result SHA-256: `173893031989c2d3c6814e44022a2738c64e2416d08894fa16024a23391f0381`. Archive manifest SHA-256: `e37732ac694b66887e19fbe260093878362684f9002ee79a67fe132f22bd8398`.

## Implementation

- `TowerBundle.cs`, `OfflineContent.cs` and `TowerBattleRunner.cs` capture the independent rules/equipment/profile selection, use it for production preparation and persist it in normal Tower inputs. Invalid rules/catalog combinations fail before combat.
- Tower loadout, compact, discovery, racing, verification and replay callers use the same captured settings. Settings writers retain the selection while copying only the non-secret combat configuration. The selection participates in settings/input hashes and cache identity.
- `ContentSnapshotContract.cs` introduces snapshot schema 4, retaining the registry, releases 3/4 and healing override. Historical schemas 1–3 and absent-selection serialization remain supported. Historical replay still requires its original assemblies.
- `TowerBossInventory.cs` builds the effective ability inventory with the healing override and hashes that file. Affinity, generation, racing and proposal validators authenticate the additional source. Generation and selection algorithms are unchanged.
- `BalanceHarnessTowerBalanceSelectionTests.cs` covers release selection, production catalog equality, historical hashes, loadout replay, profile tampering and a complete synthetic 528-observation search. Existing persisted-production Tower parity tests now exercise the current configured selection. Historical fixture tests use their explicit old content allowlist.
- The two analysis launchers reuse existing archive/process/seed machinery. The floor-3 fixture authenticates a completed current screen and its exact executable/content/selection, then validates fresh mechanics before allocating seeds. An explicitly supplied latest history ledger must match the full native registry scan.

## Reference screen

All recipes, budgets, content, executable hashes and 32 paired historical seeds were frozen before combat. Three distinct compositions were selected on each of floors 3, 7 and 15. Equipment, identities and Essence order were retained. Floor 3 uses the previously corrected distinct third reference. The predeclared screening rule admits a floor when **reference 1** wins 4–28 of 32; it does not switch to a better control after seeing the results.

| Floor | Reference | Wins / 32 | Mean boss health remaining |
| --- | --- | --- | --- |
| 3 | Preselected | 28 | 0.34% |
| 3 | Control 2 | 32 | 0.00% |
| 3 | Control 3 | 32 | 0.00% |
| 7 | Preselected | 32 | 0.00% |
| 7 | Control 2 | 32 | 0.00% |
| 7 | Control 3 | 32 | 0.00% |
| 15 | Preselected | 2 | 15.22% |
| 15 | Control 2 | 11 | 6.61% |
| 15 | Control 3 | 11 | 12.96% |

Floor 3 qualifies for a follow-up; its controls are near the ceiling. Floor 7 offers no observed win-rate headroom. Floor 15 warrants a separately declared baseline using a viable reference; the stronger existing controls are not generated-search improvements. Existing progression budgets remain hypotheses, not approved population balance targets. Gear specialization was not searched.

The final screen ran **288 fights in 28.02 seconds**. An earlier 288-fight screen gave identical aggregate results for every team. That earlier run retained its first completed cell after correcting PascalCase/camelCase comparison in the Python verifier, without rerunning that cell. The screen was repeated after fixing remaining mechanics-source validators and incorporating a concurrent tooltip-only Domain change, so the final screen and evaluation can require identical executable hashes. Total screening work: **576 fights**, all on reused historical seeds. Both screens are retained; no favorable screen was selected from differing outcomes.

## Engineering verification and operational notes

- `build/run-tests.ps1` passed **377 checks** spanning Tower preparation/replay/compact/loadout compatibility, historical readers, content accounting, healing, Tenacity, set bonuses and attribute registration/combat.
- After the remaining racing/proposal source checks were fixed, **91 focused checks passed**, with the opt-in scientific operation skipped. This includes the full synthetic search using the profile hash, plus rejection when that hash changes.
- NuGet configuration access was initially blocked by the sandbox; the backend build/test command succeeded with approved access. Existing analyzer warnings remain.
- Four scientific preflight attempts stopped before allocation/combat: historical graph schema drift, an outdated exclusion ledger, an assembly mismatch caused by a concurrent tooltip edit, and an additional old source-list assumption. Each failure remains retained with zero search attempts/reservations. No failed allocation was discarded or redrawn.
- The isolated no-build runtime copy initially lacked MSBuild restore metadata and performed no tests; copying that metadata allowed 18 focused tests to execute and pass. That intermediate runtime was not used for a completed search. Successful test claims above refer to actual reported test counts.
- No migrations, production configuration changes, deployments or shared database operations. The concurrent `AttributeCatalog.cs` tooltip edits were preserved and are not authored by this work.

Final reference artifacts: `TestResults/tower-current-balance-screen-final-20260928/`. Its freeze SHA-256 is `3a9ff519cae5053f9da268dc422ea8acb94ab9b39a18f12ea6158a43be8b2fd5`. Original screen: `TestResults/tower-current-balance-screen-20260928/`. Test logs are `TestResults/tower-current-balance-verification-final-20260928.log` and `TestResults/tower-current-native-profile-verification-20260928.log`.

Commands use a new output directory and the already tested isolated build; the second command performs fresh allocation and combat:

```powershell
python -B -X utf8 'Balance Harness/analysis/screen-current-tower-balance.py' --previous TestResults/affinity-progression-screen-20260925 --runtime TestResults/tower-current-balance-build-20260928/bin/BalanceHarness/release --settings TestResults/tower-current-balance-settings-20260928.json --output TestResults/tower-current-balance-screen-final-20260928
python -B -X utf8 'Balance Harness/analysis/run-affinity-floor-evaluation.py' --package TestResults/tower-current-search-owner-final-20260928 --output TestResults/balance/tower-current-search-final-20260928 --artifacts TestResults/tower-current-balance-build-20260928 --screen TestResults/tower-current-balance-screen-final-20260928/result.json --master 2026092803 --history TestResults/balance/tower-affinity-floor3-baseline-20260925/seed-ledger.json
```

The sanitized settings file contains only `AttributeRedesign.LiveVersion`, `EquipmentBalance.LiveVersion`, `Combat.AbilityBalanceProfile`, `Combat.ThreatAndTanking`, `Combat.IdleProgression.EncounterCadenceSeconds` and `WorldTower.CombatTicksPerFrame`. These scripts require the pinned local historical evidence; a clean checkout alone does not contain it.
