# Explicit affinity creation and generation coverage

**Subsequent implementation — 23 September 2026:** The [owned creation controller and both audits are implemented](Tower-Affinity-Creation-Study-Implementation.md), following the separately qualified v3 native contract. The generation-only boundary below records the earlier stage. Final runtime qualification and creation-specific resource/history admission remain before any scientific execution.

23 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The opt-in affinity-creation operator and zero-fight coverage export are implemented.** In the captured development context, the new policy generated nine distinct recipes across nine owners. All nine differed from the control, with no common recipe between the two batches. Eight creations added one missing endpoint; one replaced two slots. This establishes legal, distinct generation, not stronger combat performance.

This implements the next step from the [saved-stage diagnosis](Tower-Proposal-Affinity-Stage-Review.md). The earlier preservation pilot remains closed with `AbandonThisConfiguration`. Its archives, admissions, outcome measurements and permanent exclusions were not changed.

## Operator and contract

`TowerProposalPolicies.BenchmarkAffinityCreation(ids)` creates a `tower-proposal-policy-v3` policy with nine/eight `affinity-create` slots, benchmark-parent tickets and explicit, sorted `createdDamageAffinityIds`. The existing commands accept it through `tower-proposal-export-v3`. The caller selects the authored routes; the implementation does not automatically select the entire affinity inventory or infer empirical synergy.

The operator rotates owners using the existing deterministic, per-parent/operator schedule. For each scheduled owner it:

1. Groups selected routes by their unordered essence pair. Multiple routes or directions for the same pair contribute one sampling choice, while every route ID remains in the evidence.
2. Excludes pairs already active on that owner. When one endpoint is present, adds only its missing partner. When neither is present, places both through a two-slot replacement on that same owner.
3. Enumerates minimal replacement options that respect the allowed pool, one essence per family, slot count, whole-party owned-copy limits and explicitly protected parent assignments.
4. Chooses uniformly among eligible pairs, then uniformly among that pair's legal minimal edits. Constructs and validates the resulting party through the existing canonical legality function.
5. Retains rejection records, including already-active, no-legal-placement and duplicate-recipe attempts. It never substitutes an unrelated mutation when creation is impossible.

The policy accepts one to 32 selected routes. The existing scope supports up to ten essence slots, so option enumeration is bounded by at most **32 ×45** lightweight placement checks per parent/owner and cached within that generator. These checks inspect families and copy counts without constructing hypothetical parties. An eligible attempt constructs one party; an ineligible attempt constructs none. The existing 128-attempt wave bound and duplicate rejection still apply. Cancellation is checked during enumeration.

Only the scheduled owner's essence assignments change. Actor identity, equipment, subgroup placement, other owners and overall slot count remain fixed. Selected creation routes and preservation are independent: `preservedDamageAffinityIds` and `preserveParentInteractions` can constrain removals; the factory leaves both forms of preservation off. Creating one pair does not imply preserving every other pair unless the caller explicitly requests it.

This version is deliberately **generation-only**. Existing v1/v2 racing and native study adapters reject a v3 policy before invoking an evaluator. No new combat controller, allocation or admission is implied by a successful export. This prevents a new generator from silently entering a previously admitted comparison contract.

## Exported evidence

The existing three-file export remains `request.json`, `batches.json` and `files.json`, with a 64-MiB aggregate limit, create-new output behavior, external manifest pin and deterministic reconstruction verification.

Each v3 arm includes `affinityCreationCoverage` with deduplicated pairs and their full route IDs, parent/owner opportunities, already-active pairs, missing and protected essences, every legal removal/addition option, accepted owner coverage, attempt counts and rejection counts. Pair/owner counts are explicitly separate from unique owners.

Each actual creation also records `affinityCreation`: the target pair, target route IDs, removed and added essences and every newly activated selected route on the changed owner. The ordinary proposal fields retain attempt order, scheduled/changed owners, realized replacement distance, party ID and rejection. Full seed-free team scenarios remain available for inspection.

The new fields serialize only when present. Existing v1/v2 JSON identities, random streams, defaults and golden trajectories remain compatible. Both an original v1 export and a distinct v2 preservation export were reconstructed successfully by the revised binary. The public preset-list command retains its original result; the v3 factory requires an explicit inventory-derived route selection.

## Captured development preview

The [request provenance](../TestResults/affinity-creation-preview-preparation-20260923/provenance.json) authenticates the prior pilot's frozen context and policy selection against scientific manifest `f0d4d22531f4c46426c28224102d193a91c0d20318377b71b95e2fe19f4acc33`. The preview reuses that context's historical **proposal seed 1738542254**. It draws no new entropy, reserves no values and runs no combat.

Four complete arms were exported: legacy v1, benchmark single-edit control v2, benchmark preservation v2 and benchmark affinity creation v3. The creation arm uses the same three reviewed Poison routes as the earlier preservation arm, resolving to two distinct essence pairs.

| Captured creation coverage | Result |
| --- | ---: |
| Selected routes / distinct essence pairs | 3 / 2 |
| Already-active pair/owner combinations | 2, both on owner 8 |
| Eligible pair/owner combinations | 18 |
| Eligible owners | 1–7, 9 and 10 |
| Legal minimal replacement options | 84 |
| Attempts / accepted recipes | 10 / 9 |
| Rejections | 1: affinities already active on owner 8 |
| Accepted owner coverage | 9 owners |
| One-slot / two-slot replacements | 8 / 1 |
| Changed positions against control | 9 / 9 |
| Recipes shared with control | 0 |
| New fights / new values | 0 / 0 |

Owner 10's two-slot edit replaces Cinder Beetle and Pack Howler with Spider Queen Royal Venom and Viper. The other eight edits add Viper to a build already containing the selected producer. Owner 8 receives no irrelevant fallback edit when both selected pairs are already active.

The [independent structural checker](../TestResults/affinity-creation-preview-preparation-20260923/verify.py) recounts all 84 legal options, accepted edits, canonical recipe identities, owner locality, route activation, coverage and zero-seed scenarios from the saved JSON. It does not import the generator. See its [verification result](../TestResults/affinity-creation-preview-preparation-20260923/verification.json).

The [export](../TestResults/affinity-creation-preview-20260923/batches.json) has external manifest SHA-256 **`db58d54f7a1c595e6b39995508643f16207941be9b772719ac039a9e1775ad39`** and request hash `fd9442d4bac565c590dbc00ea1856d806ca7e0ba7baf798a47bd69481138cb4b`. Public reconstruction passed for this export and the retained [v1](../TestResults/affinity-creation-preview-preparation-20260923/legacy-verify.log) and [v2](../TestResults/affinity-creation-preview-preparation-20260923/preservation-verify.log) exports.

## Verification and changed files

The repository test wrapper passed **135 tests**, including **13 new creation cases**. Coverage includes missing-one and missing-both endpoints, heterogeneous owners, deduplicated route weight, active-pair rejection, exhausted copies, family conflicts, protected assignments, both generation waves, deterministic replay and arm order, invalid version/evidence bindings, cancellation, refusal to enter combat adapters, create-new output and rehashed coverage tampering. Existing adaptive golden trajectories and preservation tests pass.

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessAffinityCreationTests|FullyQualifiedName~BalanceHarnessDamageAffinityTests|FullyQualifiedName~BalanceHarnessProposalPolicyTests|FullyQualifiedName~BalanceHarnessAdaptiveRacingTests|FullyQualifiedName~BalanceHarnessBatchRacingTests' -ArtifactsPath 'TestResults/affinity-creation-build-20260923'

dotnet TestResults/affinity-creation-build-20260923/bin/BalanceHarness/release/BalanceHarness.dll tower-proposal-policy-check TestResults/affinity-creation-preview-preparation-20260923/request.json
dotnet TestResults/affinity-creation-build-20260923/bin/BalanceHarness/release/BalanceHarness.dll tower-proposal-policy-verify TestResults/affinity-creation-preview-20260923 db58d54f7a1c595e6b39995508643f16207941be9b772719ac039a9e1775ad39
```

The public export command produced the retained directory once. Use a new output directory for another export; it refuses overwrites. The [final test log](../TestResults/affinity-creation-tests-complete-20260923.log) and [TRX](../TestResults/affinity-creation-tests-complete-20260923.trx) are retained. Producing PDB checks bind all four implementation files and the new test file to their compiled assemblies. Initial test failures came from fixture assumptions about identical owner builds and an out-of-range fixture copy count; corrected tests passed. The sandbox initially could not access NuGet configuration; the approved wrapper rerun succeeded. No required command remains blocked.

The [completion receipt](../TestResults/affinity-creation-preview-preparation-20260923/completion.json) binds the 135-test TRX, runtime file hashes, exact source snapshots and preview pin. Its retained verification directory has external manifest SHA-256 **`3ca0de9c98bae23d27fe6885acafd53015b7f9ca09482c2d4bf822c774f25f0a`**. The final harness DLL SHA-256 is `f7f629e4a9051e509b69bb82fd7e68b742ad2f5c70a623f0513e1a5cbef32c3b`.

Changed implementation files:

- [TowerAffinityCreation.cs](../LL/tools/BalanceHarness/TowerAffinityCreation.cs): pair deduplication, bounded legal options, structural creation and coverage.
- [TowerAdaptiveRacingGenerator.cs](../LL/tools/BalanceHarness/TowerAdaptiveRacingGenerator.cs): opt-in dispatch, lineage and first-wave coverage.
- [TowerAdaptiveRacing.cs](../LL/tools/BalanceHarness/TowerAdaptiveRacing.cs): optional creation metadata with unchanged legacy serialization.
- [TowerProposalPolicies.cs](../LL/tools/BalanceHarness/TowerProposalPolicies.cs): v3 factory, validation, export and explicit combat-adapter rejection.
- [BalanceHarnessAffinityCreationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityCreationTests.cs): regression coverage. Current-status documentation links point here.

This work is development implementation and verification, separate from scientific campaign accounting. It creates no new study or runtime admission. There are no migrations, application configuration changes, deployments or gameplay-default changes.

The next step is a separately versioned v3 racing/native contract and prospective comparison plan, with runtime qualification, bounded resource admission and fresh paired combat panels. Generation coverage now supports designing that comparison; combat quality remains unmeasured. Existing held-out values cannot qualify a revised mechanism, and the prior negative pilot is not reopened.
