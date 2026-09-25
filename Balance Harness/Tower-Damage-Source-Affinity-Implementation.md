# Damage-source affinity and opt-in preservation

**Later implementation:** The [native proposal and paired-comparison adapters plus prospective plan](Tower-Proposal-Affinity-Native-Implementation.md) are complete, with 163 tests passing. The planned two-arm pilot has a 21,888-fight ceiling; no campaign or reservation has started. The owned held-out study controller and independent audit are next.

23 September 2026. The offline Balance Harness now implements the separately versioned affinity and preservation rule from the [captured-runtime scope review](Tower-Benchmark-Interaction-Scope-Review.md). It preserves selected producer/modifier pairs on their original owner without changing the original interaction tags or legacy replay.

**The policy produces distinct candidates, but its first-wave effect is small in the captured context:** it changed five of 108 candidate positions across all twelve recorded pilot roots. Seven complete first waves remained identical to the benchmark-only control. These are generation checks, not evidence of stronger teams. No native preparation, campaign fight, entropy draw or reservation occurred.

## Contracts and extraction

| Contract | Version | New behavior |
| --- | --- | --- |
| Affinity analysis | `tower-damage-source-affinity-v1` | Separate effect-level Poison-source hypotheses and excluded routes |
| Proposal policy | `tower-proposal-policy-v2` | Explicit `preservedDamageAffinityIds` selection |
| Candidate export | `tower-proposal-export-v2` | Full `damageAffinityInventory` evidence, derived affinity report and protected parent paths |
| Injected evaluator | `tower-proposal-racing-v2` | Binds inventory and selected affinities through both waves of the unchanged 528-trial kernel |

The analyzer derives hypotheses from the inventory's typed ability, effect, status and trigger definitions. Version 1 covers Poison applied by direct active effects to the current target, owner Basic Attack passives to the event target, or Basic Attack effects in one self-applied status layer. It joins those routes with an unconditional, positive, constant, self-targeted passive Poison damage modifier. Different same-family essence variants are excluded as illegal pair placements.

Each affinity records producer and modifier essence IDs, damage type, source scope and both complete node routes. Its ID binds the version and route identities; the report additionally hashes the entire inventory. Effect/trigger nodes must match their containing definitions. Conditional producer guards remain visible in the supplied definitions and do not imply successful activation.

Summon proxies, summoned producers, other trigger events, other status recipients, nested statuses and other damage types are outside this first version. Unsupported relevant routes are excluded; unresolved or inconsistent required nodes fail validation. The captured catalog yields six supported paths and fifteen excluded routes. Three supported paths are co-located in the benchmark: Royal Venom and the two Spiderling paths, each paired with Viper's Potent Toxins. They represent two essence pairs and protect the union of three essence IDs on owner 8.

Variant essences can share a base family's passive. The analyzer uses the essence's declared equipped ability roots; `OwningEssenceId` on a shared ability is not the combatant receiving or producing damage. No essence-specific production rules were added.

## Policy behavior and compatibility

`BenchmarkDamageEdits(ids)` supplies the benchmark-parent, nine/eight single-edit schedules with explicit sorted affinity IDs. Empty selection is a valid v2 control. The caller chooses the hypotheses; the generator does not protect every discovered affinity automatically. Selected IDs must exist in the derived report and use allowed essences. Inventory source hashes, essence roots and original interactions must match the frozen mechanics.

Protection activates only where both endpoints already exist on the same primary parent owner. Mutation preserves their union, and recombination cannot remove protected primary-parent assignments. A fresh proposal has no parent to preserve. Old interaction preservation remains an independent flag. Exhaustion retains all rejection attempts and never silently releases a protected slot; an incomplete first wave dispatches no evaluator calls.

Parent-specific protected assignments are cached within a generator. This avoids repeatedly traversing all selected paths during bounded construction retries and does not change random draws. The original 128-attempt/32-construction-check limits, family/copy legality, owner scheduling, equipment and slot layout remain in force.

The new fields serialize only when present. Version 1 rejects v2 selections/inventory; version 2 requires the corresponding evidence. Legacy presets retain their original serialized shape, random streams, recipe identities and defaults. A v2 export may contain unchanged v1 arms for direct comparison. Policy, request and evaluator identities bind the new selection and inventory; reconstruction derives the affinities again instead of trusting exported annotations.

These are source-based hypotheses. Inventory declarations are not independent authentication of gameplay content or executable compatibility. A future native admission must reproduce the inventory from its authenticated content and bind the reviewed runtime. Prepared conversions, progression, rounding, competing effects, application success and uptime can alter or eliminate the expected benefit.

## Concrete captured-context previews

The first preview reused historical proposal root `1738542254`. It protected all three reviewed paths, but the control's owner-8 edit happened to remove Pack Howler, an unprotected endpoint. Consequently the new and control first waves were identical. This result is retained and the export explicitly reports the equivalent arms.

The follow-up declared **all twelve recorded pilot roots** before generating further batches. It freezes the first physical generation context and varies only the proposal root. Historical episode IDs and combat panels are not transplanted into a new study. The complete root set, pinned source plan files and every batch are retained.

| Historical root index | Changed candidate position |
| --- | --- |
| 2 | 6 |
| 4 | 8 |
| 5 | 6 |
| 6 | 9 |
| 11 | 4 |

Roots 1, 3, 7, 8, 9, 10 and 12 were unchanged. Every arm completed nine candidates in nine attempts. Every changed recipe differs only on owner 8; its control recipe breaks at least one selected affinity and its preserved counterpart retains all selected endpoints. Every other candidate position is identical. The legacy and benchmark-control arms in the first preview exactly match the preceding v1 export, including proposal metadata and scenario transports.

This verifies that the constraint is operative, while exposing its limited first-wave differentiation. It does not establish that preserving these slots is beneficial, that second-wave batches would remain identical, or that a third full-budget campaign arm is justified. Second-wave generation still requires actual evaluator feedback.

Evidence and external manifest SHA-256 pins:

- [First preview](../TestResults/damage-affinity-preview-20260923/batches.json): `3595d792881f9f78c3f3a44b75bd604699e367dd93d53afd63127e37d5469b18`.
- [All twelve roots and results](../TestResults/damage-affinity-root-sweep-20260923/summary.json): `154e57bcd416293ad92df1920a59d3b34b67a76eef091e7c396882833ba6cebb`.
- [Standalone distinct example, root 2](../TestResults/damage-affinity-distinct-preview-20260923/batches.json): `55360a2d759ba1b4f663f546c31561ecf8c84f937df93a06552f4b9c7c11d949`.
- [Independent recipe checks, changed IDs and verification receipt](../TestResults/damage-affinity-preview-preparation-20260923/verification.json).

The standalone root-2 export is the first differing root in the complete recorded sequence, provided for inspection after retaining all ties. It is not a root selected for a scientific comparison. Its request hash is `4f8a30324278bc2239492482450c19d3ce63403fdc8abaf1968ed928b2c2e2ef`.

## Usage and verification

The existing proposal commands support the new versions. One additional read-only command derives affinities:

```text
tower-proposal-policy-affinities <boss-profiles.json>
tower-proposal-policy-check <v2-request.json>
tower-proposal-policy-export <v2-request.json> <new-output-directory>
tower-proposal-policy-verify <output-directory> <external-manifest-sha256>
```

The [concrete v2 request](../TestResults/damage-affinity-distinct-preview-20260923/request.json) contains the unchanged legacy and benchmark-only arms plus the explicitly selected affinity arm. Export retains full inventory evidence, seed-free scenarios and actual protected paths. Its existing 64-MiB limit, create-new behavior, external pin, exact inventory and reconstruction checks remain enforced. These commands do not launch a native campaign.

The required backend wrapper passed **134 tests: 19 affinity cases and 115 existing proposal, adaptive racing, batch racing and comparison cases**. Coverage includes the actual catalog's three reviewed paths, shared passive roots, excluded source/recipient routes, missing/conflicting evidence, family legality, distinct and empty-selection controls, bounded exhaustion with zero evaluator calls, both-wave feedback, recombination protection, version/evidence rejection and rehashed archive tampering. The pre-change legacy trajectory goldens continue to pass.

```powershell
./build/run-tests.ps1 `
  -Filter 'FullyQualifiedName~BalanceHarnessDamageAffinityTests|FullyQualifiedName~BalanceHarnessProposalPolicyTests|FullyQualifiedName~BalanceHarnessAdaptiveRacingTests|FullyQualifiedName~BalanceHarnessBatchRacingTests|FullyQualifiedName~BalanceHarnessAdaptiveComparisonTests' `
  -ArtifactsPath 'TestResults/damage-affinity-build-20260923'
```

See the [final test log](../TestResults/damage-affinity-tests-final-20260923.log) and [retained TRX](../TestResults/damage-affinity-tests-verified-20260923.trx). The verified harness SHA-256 is `8da8759fa6d337b04cee466cd3a76136c055080690e336b121fe4a44a91aba3e`. Public verification passed for the new distinct preview, the identical first preview and the original v1 preview. Independent Python checks authenticated the generation archives and verified the owner/endpoint differences across all twelve roots.

The [completion receipt](../TestResults/damage-affinity-verification-20260923/completion.json) also checks the retained TRX, preview manifests, document links and scoped whitespace. The four implementation/test source files match the checksums in their producing portable symbols, and both PDB identities match their DLLs.

The first sandbox build could not read the user's NuGet configuration; the approved wrapper rerun built successfully. Its initial test pass was 133/134, exposing the shared-passive ownership assumption; that assumption was corrected. The first preview's intended distinctness assertion also correctly failed on identical recipes. The complete root review preserves that finding. No required command remains blocked; all attempts and successful results are retained.

## Changed files and next step

- `TowerDamageSourceAffinities.cs`: separate typed affinity analysis, evidence routes, exclusions and version.
- `TowerProposalPolicies.cs`: opt-in v2 contracts, explicit selection, export diagnostics, CLI analysis and evaluator/reconstruction bindings.
- `TowerAdaptiveRacingGenerator.cs`: selected-affinity protection alongside the original rule, with cached protected assignments.
- `BalanceHarnessDamageAffinityTests.cs`: nineteen new cases. Earlier test files and gameplay code remain unchanged.
- This report and status links in the earlier reports and harness guides; development evidence under `TestResults/damage-affinity-*`.

The next implementation is a versioned native comparison adapter and a prospective evaluation plan, using this exact policy contract and real second-wave feedback. The plan must account for the low observed first-wave differentiation before fixing arms and combat budget. Fresh proposal roots, common admitted combat panels, an endpoint against the fixed benchmark and independent evaluation are still required. No efficacy result or policy promotion is implied by this implementation.

There are no database migrations, application configuration changes or deployment steps. Existing exclusions, scientific archives, gameplay defaults and policy recommendations remain unchanged.
