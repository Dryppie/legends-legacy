# Practical Tower: eight-recipe confirmation adapter

22 September 2026. Target: offline BalanceHarness. **Historical implementation status: ImplementedVerifiedNeedsNativeAdmission.** The separate **`tower-practical-fixed-family-confirmation-v1`** controller implements the [frozen six-challenger/two-reference plan](Tower-Practical-Fixed-Family-Confirmation-Plan.md): **5,500 shared trials per recipe /44,000 fights**, family 32, and every qualifying candidate reported in frozen order. It does not select a new primary.

**Execution follow-up:** the [admitted 44,000-fight run and both audits](Tower-Practical-Fixed-Family-Confirmation-Execution.md) completed successfully. Exact candidate `96b94357…` alone qualified against both references (76.15% versus 70.64% /64.16%). This confirms that fixed recipe for the captured cohort; no new primary or search default was selected. The study is closed and exclusions total **550,367**. The implementation/fixture results below are historical evidence for the producing controller.

## Public contract

```text
tower-fixed-family-confirmation-check <request.json>
tower-fixed-family-confirmation-run <request.json>
tower-fixed-family-confirmation-verify <completed-output>
```

`check` authenticates the request/definition, producing identity, settings/content, complete history and prior receipts, then prepares the eight native parties without combat or reservation. It returns `ContractValidNoReservation` and explicitly leaves resource feasibility unestablished. `run` owns the registry/output leases and watched worker through admission, the single entropy batch, combat, both audits and publication. `verify` reconstructs a completed archive without combat or allocation using its producing runtime. The worker command is an internal owned-process route. There is no resume, replay, replacement batch, recovery or sample-extension command.

The definition uses `TowerFixedFamilyDefinition`: version, eight ordered `TowerFixedFamily` entries, captured content hashes, settings hash, producing execution hash and complete sorted unique exclusions. Each entry contains role, party ID, reference IDs and exact seed-free scenario. Candidate roles have empty reference-ID lists; the last two entries use `PrimaryReference` and `OtherReference` with their catalogue reference IDs. Native canonical team hash is **`6cec8956d42a175044731021d9395ade5eca8275986892bfa94a248b54cc5d8d`**. Changing a recipe, actor, ability order, role, position, ID or reference binding fails before sampling. Current gameplay must not replace the captured cohort during admission.

The request uses `TowerFixedFamilyRequest`: version, absolute content/definition/registry/output paths, definition hash, required history hashes, cumulative `maximumSeconds`/`maximumBytes`, three phase limits, explicit `priorSeconds`/`priorBytes`, and **required `priorCharges`**. Existing Pending recovery paths and receipt hashes remain supported only for their authenticated historical contracts. Output must be a new directory directly beneath the complete history registry, with no input overlap or linked ancestors.

Each prior-charge entry contains `scope`, `seconds`, `bytes`, `receiptPath` and `receiptHash`. Scopes and normalized receipt paths must be unique; amounts must be finite and positive, and their sums must equal the request's prior totals. Missing cost fields, empty charges, default-zero history, duplicate receipts, changed receipt bytes and overlapping paths fail. Receipt bytes and the declared ledger are retained in the archive and checked again before entropy and during archive verification. These checks bind the **declared reconciliation**; they do not infer the meaning of arbitrary older receipt schemas or prove that differently named scopes cannot overlap. The subsequent admission review must reconcile all applicable allowances without double counting and freeze the final numeric cumulative caps.

The plan JSON remains a historical planning artifact, not a native request. Its exact recipes can be mapped into this typed definition; producing runtime, complete history, receipt ledger and final paths must be supplied by the separately bounded admission package.

## Statistical and publication behavior

Victory counts as a win; draw/defeat are non-wins. Invalid, faulted or missing trials make the whole attempt incomplete. The native and independent auditors both reconstruct **eight rates /twelve signed contrasts**, using the shared panel identity and order.

Every candidate needs an adjusted Wilson lower win-rate bound at least 10% (**621 wins**), at least five observed points against **each** reference (**275 net gains**), and positive paired lower bounds against both. All intervals use family **32**. Candidate and reference IDs are explicit on every contrast, so twelve comparisons cannot be mistaken for one candidate's two controls.

`TowerFixedFamilyResult` exposes `candidateIds`, `qualifyingPartyIds`, all rates/contrasts, recommendations and controls. A complete verified family with any qualifier returns **`StrongerFixedCandidatesConfirmed` /`RecommendFixedCandidates`** and recommends all qualifiers in catalogue order. Both references remain controls. Zero qualifiers return **`StrengthNotDemonstrated` /`Hold`** and retain both supplied reference recommendations. An incomplete attempt returns failed integrity, `IncompleteEvidence`, no new recommendations and preserved failure evidence. No estimated-best or single-primary field is introduced. Balance remains `NotAssessed`; neither candidate/candidate superiority nor reliable search discovery is established.

Publication requires matching native reconstruction and independent saved-report counts. JSON and Markdown report every rate, every comparison and all qualification outcomes. Seed-free exports preserve equipment, actor identities, subgroup mapping and Essence-copy requirements. Exact inventories and a final closeout receipt bind the archive; failed publication remains failed even if an earlier output file or closeout was written.

## Sampling, limits and compatibility

The reservation path freezes recipes and authenticated inputs before durable Pending/entropy intent/start. One fill supplies **44,000 bytes /11,000 signed little-endian words**. It excludes complete history and duplicate words, takes the first **5,500 eligible distinct values**, and permanently reserves every eligible unused tail value. Admission rejects history above **989,000**, leaving room under the million-value limit. Cancellation after the draw is deferred until the batch and entropy-completion record are durable. A shortfall stops without refill; unresolved entropy leaves blocking Pending state. Legacy recovery commands cannot reinterpret this version.

Each team uses **five 1,000-value slices and one 500-value slice**, giving **48 exact transport recipes**. Only `Scenario.Seeds` changes. The 1,000-value native input cap stays in place, and no intermediate statistical decision occurs. Every successful run has exactly **44,000 started/completed attempts**, without cache reuse. The readers reject missing, duplicate, extra, reordered or altered trial/report/chunk bindings.

Additional native ceilings are **6,000 seconds /3 GiB**, on top of explicit prior charges. Smaller admitted limits are permitted only within the same phase caps and reserved headroom; they do not change sample size or allow partial success.

| Allocation | Maximum seconds | Maximum growth |
| --- | ---: | ---: |
| Admission/reservation | 240 | 256 MiB |
| Combat/reports | 3,600 | 2,304 MiB |
| Both audits/publication | 1,800 | 256 MiB |
| Reserved closeout/cleanup | 60 | 16 MiB |
| Enclosing setup/headroom | 300 | 240 MiB |

Phase limits cannot transfer. The monitor separately checks unphased time/storage against enclosing headroom, and archive verification reconstructs that accounting. Closeout/cleanup allowance is withheld from ordinary work; successful publication also fits inside the audit and ordinary-work limits. The enclosing launcher must additionally account for its own external setup/logs/cleanup before admission, within the same proposed overall envelope. Smaller native ceilings can reserve that outer allowance. A killed owner/worker, resource exhaustion, changed history or integrity failure terminates the attempt and retains evidence; existing output is refusal.

The new five-file controller follows the existing fixed-team workflow while using the shared battle/archive, history, storage, attempt-journal and process primitives. Its protocol-specific records, counts, statistics, limits and result schema are separate. **All five old `TowerFixedTeamConfirmation` files remain unchanged**, preserving the three-team/family-seven command and historical archive semantics. Public dispatch and the separate test executable add only the new command family.

| Implementation area | Source |
| --- | --- |
| Frozen contract, twelve contrasts and all-qualifier export | [TowerFixedFamilyConfirmation.cs](../LL/tools/BalanceHarness/TowerFixedFamilyConfirmation.cs) |
| Admission, cumulative/phase limits and operation | [Run](../LL/tools/BalanceHarness/TowerFixedFamilyConfirmationRun.cs) |
| Single batch, retained prior receipts and durable journals | [Reservation](../LL/tools/BalanceHarness/TowerFixedFamilyConfirmationReservation.cs) |
| Owned worker, publication, closeout and public commands | [Worker](../LL/tools/BalanceHarness/TowerFixedFamilyConfirmationWorker.cs) |
| Native reconstruction and independent saved-outcome counts | [Archive](../LL/tools/BalanceHarness/TowerFixedFamilyConfirmationArchive.cs) |
| Fixtures | [Scientific/archive cases](../LL/tests/EssenceSystem.Tests/BalanceHarnessFixedFamilyConfirmationTests.cs), [process cases](../LL/tests/EssenceSystem.Tests/BalanceHarnessFixedFamilyProcessTests.cs), [literal fixture host](../LL/tests/BalanceHarness.ProcessFixture/FixedFamilyFixtureHost.cs) |

## Verification

The fixture suite covers exact plan/content/settings bindings; 48 native transport inputs and per-input limits; zero/one/multiple qualifiers; 620/621 viability, 274/275 net-gain and high-discordance boundaries; both-reference requirements; full literal report publication and dual audits; re-signed archive tampering; every entropy/publication interruption; collisions, duplicate words, unused tail and shortfall; required prior charges and retained receipts; process death, cancellation, registry leases, phase/enclosing/cumulative limits; and rejection by legacy recovery/commands. Its synthetic report callbacks never invoke the battle engine or production entropy provider.

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/fixed-family-implementation-build-20260922' -Filter 'FullyQualifiedName~BalanceHarnessFixedFamily'
./build/run-tests.ps1 -NoBuild -ArtifactsPath 'TestResults/fixed-family-implementation-build-20260922' -Filter 'FullyQualifiedName~BalanceHarnessFixedTeamConfirmationTests|FullyQualifiedName~BalanceHarnessPracticalSearchTests|FullyQualifiedName~BalanceHarnessPracticalPresetTests|FullyQualifiedName~BalanceHarnessSelectionDiagnosticTests'
./build/run-tests.ps1 -NoBuild -ArtifactsPath 'TestResults/fixed-family-implementation-build-20260922' -Filter 'FullyQualifiedName~BalanceHarnessSelectionDiagnosticTests.Complete_negative_and_incumbent_primary_are_valid_without_reselection'
```

The exact planning JSON and its 22 authenticated inputs also reproduce unchanged; public help lists both old and new command families. Final test results, resource observations and source/runtime hashes are recorded in the [implementation verification receipt](../TestResults/fixed-family-implementation-verification-20260922/summary.json) and its [exact file inventory](../TestResults/fixed-family-implementation-verification-20260922/files.json).

**All 73 new cases passed** in the complete focused run. This includes 96 first/last-value native input checks across 48 chunks, eight party preparations, the unchanged per-input seed cap, and two full 44,000-report workflows. The positive literal fixture published two qualifiers in catalogue order in **277.602 seconds /64,665,296 bytes**; the owned negative fixture published Hold in **332.445 seconds /64,669,715 bytes**. These closeout observations exclude later test-only mutation/re-verification/cleanup work. They measure synthetic report bookkeeping with minimal fixture history, not captured content/runtime copying, production history or gameplay costs. Full native resource feasibility remains unestablished.

The initial sandboxed build could not read the normal NuGet configuration. The same repository wrapper succeeded with the required filesystem access; the final build had no errors and ten existing warnings outside the changed files. The real history check verifies exact membership and unchanged hashes for all **227** registered ledger files, preserving **539,367** exclusions. Its first read-only filename filter omitted `prior-seed-ledger.json`; correcting that filter resolved the count without changing any ledger or allocating values.

The mixed compatibility run returned **166 passed /one failed** across the old fixed-team, practical-search, preset and diagnostic suites. The incumbent-mode diagnostic hit its unchanged 60-second confirmation-phase deadline while other classes were also running. An isolated rerun of that theory passed **both cases** in 49 seconds, without source or budget changes. The original failure and isolated result are both retained; timing contention is consistent with these observations, not independently proven. Across the final focused run, compatibility run and isolated rerun, **all 240 distinct cases have a passing result**. This does not claim that the mixed run was entirely green. No required command remains blocked.

Changed files are the five new controller parts, public dispatch, two new test files, the new fixture host and its dispatch, this implementation report and current Markdown pointers. No application configuration change, migration or deployment is required. No new scientific recommendation or gameplay resource-feasibility claim follows from these fixtures.
