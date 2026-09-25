# Captured benchmark interaction scopes

**Subsequent implementation:** The [separate damage-source affinity and v2 preservation policy](Tower-Damage-Source-Affinity-Implementation.md) are now implemented. All 134 relevant tests pass. A complete twelve-root generation review changes five of 108 first-wave candidate positions and retains seven identical batches; no combat efficacy claim or promotion follows.

23 September 2026. Target: the offline Balance Harness. The unresolved entries from the [proposal preview](Tower-Proposal-Policy-Implementation.md) have been inspected against the captured content and executable. **The three existing entries do not require a common owner. A different, owner-specific Poison damage modifier is missing from the interaction inventory.**

This closes the scope-review step. It does not implement or qualify a new preservation policy. The current benchmark-only presets still produce identical first waves, and the original mechanics remain unchanged.

## What the three entries mean

All three co-located entries are on owner 8 of benchmark `96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c`. They terminate at Piercing Fangs' conditional damage effect, which reads `HasCondition(Poison)` on its actual target.

| Producer path | Application scope and condition | Consumer requirement |
| --- | --- | --- |
| Royal Venom status | The buff holder's Basic Attack applies Poison to that attack's target; its trigger also checks `EventSourceIsSelf` | That enemy must still have Poison when Piercing Fangs checks it |
| Toxic Opportunity | The owner's Basic Attack applies Poison to its event target when that target has Slow | Same target/Poison requirement; Slow may itself come from an ally |
| Venom Web | Applies Poison to the caster's current target | Same target/Poison requirement |

The archived runtime's condition check accepts positive Poison from either the Viper owner or an ally. It rejects absent Poison, zero stacks and Poison on another enemy. Its Basic Attack trigger scope rejects another actor's attack, and Royal Venom's explicit guard does the same. These producer restrictions determine who applies Poison; they do **not** impose the same ownership requirement on Piercing Fangs' target check.

The two Spiderling entries are different producer paths, not duplicate observations. This is conditional structural compatibility: successful application, target alignment, survival, cleansing and timing remain relevant. No conclusion about realized uptime or win rate follows.

## The separate missing mechanism

Viper's passive **Potent Toxins** has an unconditional `ModifyDamageDealt`, `Self`, `Poison`, authored value `7`. The captured periodic-damage component uses the Poison condition's source for its outgoing damage modifier. In a fixed single-tick fixture with no defense, the result was:

| Modifier placement | Poison damage |
| --- | ---: |
| Absent | 100 |
| On the Poison source | 107 |
| On another ally | 100 |

This supplies a distinct structural hypothesis for keeping Viper with Royal Venom and/or Venomous Spiderling on the Poison source. There are **two essence-pair affinities through three producer paths** in the reviewed owner-8 build. Their consumer is Potent Toxins' outgoing Poison modifier, not Piercing Fangs' Poison predicate. The existing 128-entry interaction inventory contains none of these modifier edges.

The inventory currently joins status/condition/summon identifier producers and readers, plus selected emitted/listened-to events. It does not join Poison production with typed outgoing damage modifiers. Changing the compatibility tag on the existing three entries would therefore encode the wrong reason for co-location and obscure the actual coverage gap.

The 7 is an authored modifier used for the component fixture, not an estimate of the fully prepared team's gain. Progression, other modifiers, integer rounding, opportunity cost, Poison uptime and the rest of the party matter. Keeping all three owner-8 essences would be a deliberately restrictive proposal hypothesis, not a proven optimal constraint.

## Reproducible implementation and evidence

[`audit-tower-benchmark-interactions.ps1`](../LL/tools/BalanceHarness/Scripts/audit-tower-benchmark-interactions.ps1) is a narrow forensic audit. It:

- Requires external manifest pins, verifies every file and the exact inventory, rejects links and output inside an input archive, and refuses overwrite.
- Loads the captured gameplay DLLs in a fresh process and regenerates the inventory from captured content. Essence roots, source hashes and all 128 interactions must exactly match the preview.
- Requires the expected three paths and literal consumer/modifier predicates; unknown input shapes fail rather than receiving a generic classification.
- Runs **14 direct component checks** against the captured engine, then records their results, content definitions, assembly identities and method IL hashes. It never enters the battle loop, prepares a native team or allocates seeds.
- Rechecks both input archives before publishing a create-new, hashed audit package. Installed ASP.NET configuration helpers are identified separately in the evidence.

The successful evidence is [audit.json](../TestResults/proposal-interaction-scope-review-02-20260923/audit.json), accompanied by the exact audit script and a two-file manifest. Its external manifest SHA-256 is **`c81c4c0b6e4fa4d147ba002f83194f89a047d69df225ee9ddac479aa89567c57`**.

Inputs:

- Captured admission manifest: `1c57b77e14fff65453a33bcc170b0eb7c010cda86c91b53855f7268dcbf19556`.
- Proposal preview manifest: `114de3f89e54b649f5abae57ef9bdd41f84fe20c8bd84a17cf807c7036206458`.
- Captured `Services.LL.dll`: `399f94c8f61e055da15731b1e069a36f3b94a0e29ec2f964d4e06c300948bbee`.

Current engine source helped locate the relevant components, but the admission contains no engine source/PDB snapshot and the current rebuilt DLL differs from the captured one. The runtime checks therefore use the pinned historical DLL itself. Method IL hashes locate the reviewed implementation; they are not a substitute for the full assembly pin or a claim of exhaustive path coverage.

The [independent verification receipt](../TestResults/proposal-interaction-scope-verification-20260923/verification.json) confirms all 14 checks, a second run with an identical archive manifest, and six expected rejections: wrong pin, overwrite, nested output, extra file, missing file and changed bytes. Both source archives were independently reverified afterward. The initial 12-check development package is retained separately; the `-02-` package above adds the two explicit Royal Venom guard checks and is the reviewed result.

The required backend wrapper additionally passed **18 tests** (17 proposal-policy regressions and the effect-recipient condition regression), using the previously built binaries because no C# changed. These are separate from the historical-runtime component checks. See the [log](../TestResults/proposal-interaction-scope-backend-tests-20260923.log) and [retained TRX](../TestResults/proposal-interaction-scope-backend-tests-20260923.trx). PowerShell parsing, scoped whitespace and document links passed. No required command remains blocked. During development, the audit rejected a mistyped external pin; a PowerShell string conversion and missing explicit shared-framework resolution were corrected before the successful packages were published.

```powershell
./build/run-tests.ps1 -NoBuild `
  -Filter 'FullyQualifiedName~BalanceHarnessProposalPolicyTests|FullyQualifiedName~Effect_condition_target_still_resolves_to_the_effect_recipient' `
  -ArtifactsPath 'TestResults/proposal-policy-build-20260923'
```

Reproduction (use a fresh output directory):

```powershell
./LL/tools/BalanceHarness/Scripts/audit-tower-benchmark-interactions.ps1 `
  -Capture 'TestResults/recognition-admission-repaired-20260923' `
  -CaptureManifestSha256 '1c57b77e14fff65453a33bcc170b0eb7c010cda86c91b53855f7268dcbf19556' `
  -Preview 'TestResults/proposal-policy-preview-20260923' `
  -PreviewManifestSha256 '114de3f89e54b649f5abae57ef9bdd41f84fe20c8bd84a17cf807c7036206458' `
  -Output '<new-audit-directory>'
```

## Next implementation

Add a **separate versioned damage-source affinity representation** and opt-in preservation policy. Derive affinity from executable producer routes and the modifier's damage type/recipient, retain effect-level evidence, and reject unresolved summon or proxy credit. Do not amend the old inventory tags or adaptive v1 replay behavior.

For this benchmark, an explicit experimental rule could preserve the two reviewed owner-8 affinities, protecting the union of their three essence endpoints. Require a deterministic export to demonstrate which candidates actually change and report rejected/exhausted proposals without silently weakening the rule. If the new arm still produces the same recipes, do not budget it as a distinct treatment. Shared-target Poison predicates should remain available as a different, less restrictive hypothesis.

Only after that generation review should a prospective comparison define fresh roots, matched budgets, its endpoint against the fixed benchmark and independent evaluation. The two already distinct legacy/benchmark-only arms remain an alternative; neither this review nor the earlier negative anchored-neighborhood evidence supports promoting them now.

Changed implementation: the audit script and this report; the proposal implementation report, state/gaps review, independent assessment and harness guides link the resolved finding. Verification artifacts are under `TestResults/proposal-interaction-scope-*`. There are **zero new campaign fights, native preparations or reserved values**, no migrations, configuration/default changes or deployment steps. No C# or gameplay implementation changed.
