# Versioned proposal policies and deterministic batch export

**Subsequent implementation:** The [separate damage-source affinity and v2 preservation policy](Tower-Damage-Source-Affinity-Implementation.md) implement the finding from the [scope audit](Tower-Benchmark-Interaction-Scope-Review.md). The original tags and presets remain unchanged. The complete twelve-root generation review changes five of 108 first-wave positions and retains seven identical batches; a prospective comparison still needs a native adapter and a justified design/budget.

23 September 2026. The offline Balance Harness now supports explicit proposal policies and a deterministic, zero-combat export for comparing their first waves. A separate evaluator API runs each policy through the existing 528-trial racing kernel. The original adaptive v1 contract and defaults retain their behavior.

This implements the next step from the [recognition diagnosis](Tower-Frozen-Pool-Recognition-Execution.md). It does not demonstrate stronger teams or a better search policy. No native preparation, combat, fresh entropy draw or seed reservation was performed.

## Contracts and behavior

`TowerProposalPolicies.cs` introduces three separate version identifiers:

| Contract | Version | Purpose |
| --- | --- | --- |
| Proposal policy | `tower-proposal-policy-v1` | Explicit nine/eight operator schedules, parent tickets and parent-interaction preservation |
| Candidate export | `tower-proposal-export-v1` | One frozen context/root, two to four named policies, first-wave recipes and generation diagnostics |
| Experimental evaluator | `tower-proposal-racing-v1` | A policy plus the existing adaptive plan, evaluated through the unchanged racing kernel |

A policy declares `firstWave` (nine operators), `secondWave` (eight), `parentTickets` (one to sixteen tickets) and `preserveParentInteractions`. Repeated tickets determine the parent probabilities. Allowed operators are the existing single, coordinated, partial, recombination, guided-pair and fresh operators; parents are the benchmark, another reference or the preceding beam. A beam ticket in the first wave uses the existing reference fallback.

The policy version fixes the generation mechanism and limits: 128 attempts per wave, 32 construction checks per attempt, unchanged duplicate rejection, canonical owner-preserving recipes and existing copy/family constraints. Rejected attempts continue to advance the operator and owner schedules. Policy fields, order and name are hashed into the new plan identity. The existing deterministic streams remain keyed by proposal root and purpose; a policy name does not manufacture different random proposals. Exports therefore detect identical ordered candidate batches even when policy hashes differ.

Three presets are available. They are development hypotheses:

- **`legacy-v1`** contains the exact original schedules and the 2:1:1 benchmark/other-reference/beam ticket mixture. Its reserved name rejects altered contents.
- **`benchmark-single-v1`** proposes nine then eight single-assignment edits from the fixed benchmark.
- **`benchmark-preserving-single-v1`** uses the same schedule and parent choice while preserving declared, co-located parent interaction pairs.

The new contract accepts other explicit schedules within those fixed bounds. Each comparison must state which factors it changes. The two benchmark presets isolate the preservation flag; the legacy-versus-benchmark contrast changes the proposal distribution as a whole, not just one operator.

Preservation retains both endpoints on their original owner for interactions tagged `same-owner-or-explicit-recipient-required` in the supplied mechanics. Overlapping pairs protect the union of their endpoints. Protection applies to mutation and recombination; a fresh proposal has no parent to preserve. The implementation never silently relaxes protection to fill a batch. If construction or duplicate rejection exhausts the attempt budget, the result is incomplete and retains every proposal/rejection. An incomplete first wave dispatches no evaluator calls.

The other tag, `recipient-and-trigger-scope-unverified`, does not activate protection. Even the accepted tag is a structural hypothesis, not proof that the interaction functions or improves combat. Neither metadata nor game mechanics are changed by this implementation.

## Export and evaluation

The export context contains the captured scope, mechanics, exact benchmark identity and explicit proposal root. It accepts no outcomes or supplied beam. The first wave can therefore be constructed without inventing feedback. Later waves are generated only after completed feedback in the evaluator API.

Each exported arm contains its policy hash, completion status, full attempt log, accepted recipes, three controls and seed-free scenario transports. Logs retain requested/effective operators, primary/donor parents, scheduled/changed owners, construction checks, realized replacement distance, interaction choice, fallback and rejection. The export also records protected pairs in the actual parents and pairs of arms with identical ordered first-wave recipes. Existing scope/history fields are declarations copied from the context; export is not a fresh runtime or live-history admission.

The three-file archive contains `request.json`, `batches.json` and `files.json`, with a 64-MiB aggregate limit. Creation refuses an existing output. Verification requires the external manifest hash, checks the file inventory and bytes, regenerates every arm from the request, compares the result, and rechecks its inputs. There is no combat command in this interface.

Public commands:

```text
tower-proposal-policy-presets
tower-proposal-policy-check <request.json>
tower-proposal-policy-export <request.json> <new-output-directory>
tower-proposal-policy-verify <output-directory> <external-manifest-sha256>
```

`TowerProposalPolicies.RunAsync` accepts an injected evaluator and fixes the allocation at 528 trials. It uses the same `TowerBatchRacing.RunCoreAsync` implementation for screening, continuation, diversity, nomination, positive incumbent ties and final selection. Both waves record their actual beam and feedback bindings. Snapshots are copied, policy/plan identities are distinct from legacy v1, and `ReconstructAsync` rejects changed policies, proposals, feedback or observations. Native execution of a new scientific comparison still requires its own versioned adapter, declared design and admission; this API is not a native campaign launcher.

## Captured-context preview

The public commands were exercised against the authenticated first development root of the completed adaptive pilot. This deliberately reuses that historical proposal context to check compatibility. It is not a fresh experimental root or a qualification study.

All three arms exported nine candidates in nine attempts, with zero fights and zero new reserved values. The legacy arm's complete first-wave batch exactly matches the saved pilot batch, including proposal metadata and recipes.

The preview also exposed a limitation before spending any combat budget: **the two benchmark-only presets generate identical recipes**. The captured mechanics contain 128 interaction entries, of which 20 carry the declared scope tag and 108 retain the unverified tag. The benchmark's co-located entries all have the unverified tag; no parent pair activates the conservative preservation rule. The preview explicitly records zero protected pairs and the identical arm pair. A differently named policy is not a distinct treatment here.

Evidence:

- [Concrete export request and provenance](../TestResults/proposal-policy-preview-preparation-20260923/provenance.json).
- [All policies, batches and exact seed-free teams](../TestResults/proposal-policy-preview-20260923/batches.json).
- [Preview summary and saved-pilot equality check](../TestResults/proposal-policy-preview-preparation-20260923/summary.json).
- [Public verification receipt](../TestResults/proposal-policy-preview-preparation-20260923/verify.log).

Export manifest SHA-256: **`114de3f89e54b649f5abae57ef9bdd41f84fe20c8bd84a17cf807c7036206458`**. Request identity: `ac31d9d6b4e957c1f2db8dd75ac1e918ad52291df89a6f5eea0ec8c45d1ea8b8`. Source pilot manifest: `f2327de7f9382f3a3ac3213a25cdb590e81e676ddd6e622fb91e3615ea8dd95a`.

The subsequent [scope review](Tower-Benchmark-Interaction-Scope-Review.md) is complete. It supports a separate owner-specific damage affinity, not relabeling the three existing target-condition entries. A distinct preservation arm still requires implementation and a new export. Alternatively, a comparison can use the two currently distinct proposal policies, but their strength remains untested and the earlier negative anchored-neighborhood evidence still applies. Do not spend a third arm's budget on the inactive preservation preset. A subsequent comparison needs fresh roots, matched combat budgets, a declared endpoint against R*, and independent evaluation; the preview creates none of those resources.

## Changed files and verification

- `LL/tools/BalanceHarness/TowerProposalPolicies.cs`: contracts, presets, validation, deterministic export/reconstruction, CLI and opt-in evaluator wrapper.
- `TowerAdaptiveRacingGenerator.cs`: configurable schedules and parent tickets, with opt-in structural preservation. Its ordinary constructor selects the exact legacy policy.
- `TowerBatchRacingContract.cs`: extracted the existing scope validation for reuse without requiring combat panels during export. The original panel validation remains in place.
- `Program.cs`: dispatches the new zero-combat command prefix.
- `LL/tests/EssenceSystem.Tests/BalanceHarnessProposalPolicyTests.cs`: compatibility, policy, export, feedback, exhaustion and tampering coverage.
- This report and status links in the harness guides and preceding diagnosis.

Two complete legacy fixture trajectories are checked against plan/report hashes obtained from the retained **pre-change binary**, not generated expectations from the revised implementation. The captured real-context preview independently checks the old first-wave batch. The broader test run covers the proposal contracts, existing adaptive generator/native adapter, racing kernel and adaptive comparison.

The final required-wrapper run passed **115 tests**, including **17 new proposal-policy cases**. See the [test log](../TestResults/proposal-policy-tests-verified-20260923.log), [retained TRX](../TestResults/proposal-policy-tests-verified-20260923.trx) and [verification summary](../TestResults/proposal-policy-verification-20260923.json). The public presets, check, export and pinned verification commands all passed on the captured development context. [Documentation links, scoped whitespace and the preview manifest pin](../TestResults/proposal-policy-doc-check-20260923.log) also passed. No required verification command remains blocked.

The initial sandbox build could not read the user's NuGet configuration. The approved wrapper rerun compiled after correcting a local variable name collision. An initial regression run passed 114/115 tests; its only failure was an invalid array cast in the new mutable-preset test. The test was corrected to mutate the array-backed preset. These attempts are retained in `TestResults/proposal-policy-tests-20260923.log`, `proposal-policy-tests-build-20260923.log` and `proposal-policy-tests-final-20260923.log`.

Verification command:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessProposalPolicyTests|FullyQualifiedName~BalanceHarnessAdaptiveRacingTests|FullyQualifiedName~BalanceHarnessBatchRacingTests|FullyQualifiedName~BalanceHarnessAdaptiveComparisonTests' -ArtifactsPath 'TestResults/proposal-policy-build-20260923'
```

There are no database migrations, application configuration changes or deployment steps. Existing admission/runtime packages, scientific archives, exclusions, recommendations and gameplay defaults remain untouched. The captured preview is generation-only and is not an efficacy result.
