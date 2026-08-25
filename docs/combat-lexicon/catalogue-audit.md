# Catalogue Audit

## Scope

Reviewed the current domain engine and models, compiler/catalogue validation, `abilities.json` and `statuses.json`, Angular combat vocabulary, existing design documents, and ability/attribute combat tests.

## Historical findings and implementation outcome

The bullets below preserve the gaps found before the typed standard-condition implementation. Those gaps are now resolved for `ApplyCondition` and the standard damage/healing pipelines unless a bullet explicitly concerns legacy `ApplyStatus` content, frontend presentation, or another still-separate subsystem.

- The executable engine is data-driven through `AbilitySpec`, `StatusSpec`, `FastCombatEngine`, and runtime wrappers.
- `EssenceTagCatalog` accepts a wider vocabulary than the engine executes. Accepted tags are not implementation evidence.
- The Angular targeting enum reflects an older vocabulary and does not mirror `AbilityTargetSelector`.
- `docs/ability-system.md` and `docs/new-essence-ability-system-plan.md` describe older classes/selectors and must not be treated as current runtime authority.
- `docs/attack-speed-implementation-analysis.md` contains a stale “current state”; attack speed now changes basic-attack progress.
- `ArmorDamageReductionConstants.cs` contains a formula that conflicts with the formula actually called by the engine.
- Freeze is authored as `Control.Freeze`, but only `Control.Stun` blocks actions.
- Freeze and Stun applications are deterministic in the current runtime; canonical behavior requires an 80% base landing roll and exact X-second successful duration.
- Cleanse removes beneficial and harmful statuses alike.
- `OnInterval` is owner-scoped and published from tick zero using the trigger internal cooldown as its cadence; several other accepted trigger tags still have no runtime event.
- Reactive triggers are synchronous and lack a discovered recursion guard.
- Barrier is a single unbounded pool with no 2.5× MaxHealth cap, source tracking, or applied/absorbed/broken events.
- Bleed, Burn, and Poison currently rely on status-ID grouping, authored stack caps, and authored schedules; the canonical model requires uncapped independent 1% Power stacks with fixed schedules of Burn 1s/4s, Bleed 2s/8s, and Poison 2s/12s.
- Current Cold stacks are inert and may be permanent; canonical Chill requires a 20-stack cap, -1% Attack Speed per stack, shared 10-second refresh, and cleanse-all removal.
- Flat defence modification exists, but canonical Corrosion requires a shared 50-stack percentage reduction to Armor and Resistance, a refreshed 12-second duration, and cleanse-all removal.
- No delayed-condition primitive distinguishes natural trigger from removal; canonical Doom requires independent 15-second stacks, snapshotted X% Power damage, and single-stack Cleanse.
- Power can be modified by authored flat effects, but canonical Empower and Weaken require fixed ±20% percentage modifiers, Unique 10-second instances, and refresh-on-reapplication.
- Attack Speed modification exists, but canonical Haste and Slow require fixed ±25% multipliers, Unique 10-second instances, refresh-on-reapplication, and an independent Chill multiplier.
- Guard-tagged content currently uses timed general Damage Reduction; canonical Guard requires uncapped permanent charges, 25% direct-hit reduction, consumption filtering, and immunity to removal.
- The runtime has no harmful-condition classification or interception hook; canonical Ward requires uncapped permanent charges that cancel whole applications after immunity/landing checks.
- Authored reactive damage exists, but canonical Thorns requires independent timed percentage stacks, summed reflection from direct Health damage, partial expiration, and non-recursive Reflected Damage metadata.
- The authored vulnerability status is a one-hit trigger; canonical Vulnerable requires permanent uncapped stacks, 25% additive direct-hit amplification per stack, and Cleanse-all removal.
- The frontend does not currently generate canonical `Condition(X)` hover explanations or aggregate identical simultaneous stack ticks for display.
- No target-side Healing Received layer exists; canonical Wound and Recovery require independent timers with only one fixed ±30% modifier effective for each condition.
- Health Regeneration can be modified as a flat attribute, but canonical Decay and Renewal require independent timers with only one fixed ±30% amount modifier effective for each condition.
- The current target selector uses encounter order and Taunt priority; the canonical Threat-Weighted Enemy selector requires a Threat value and weighted roll.
- Taunt tags the taunting combatant and bypasses ordinary priority rather than modifying Threat weight.
- Canonical Taunt uses X as duration but still needs a fixed Threat bonus; canonical Stealth requires a final effective-Threat override of exactly 1 while underlying modifiers continue updating.

## Condition totals

| Status                | Count |
| --------------------- | ----: |
| Implemented           |    28 |
| Partially Implemented |     0 |
| Proposed              |     0 |
| Deprecated            |     0 |
| Unknown               |     0 |

## Evidence locations

- `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityRuntime.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`
- `LL/src/Core/Domain/Models/Attributes/AttributeCombatRules.cs`
- `LL/src/Core/Domain/Models/Damages/` and `LL/src/Core/Domain/Models/Attributes/`
- `LL/src/API/API.LL/Data/combat/` (authored combat JSON)
- `LL/tests/EssenceSystem.Tests/AbilitySystemTests.cs`
- `LL/tests/EssenceSystem.Tests/AttributeCombatSystemTests.cs`

## Implemented architecture

The runtime now exposes typed `ApplyCondition`, `HasCondition`, and `ConditionStacksAtLeast` authoring primitives. `RuntimeCondition` supports shared intensity, charges, Unique refresh, and independent timers. Damage, healing, regeneration, Barrier, Threat selection, control prevention, Cleanse, and Dispel consume the shared condition state.

Legacy `ApplyStatus` remains supported for bespoke authored mechanics. Standard Burn, Bleed,
Poison, Chill, Freeze, Stun, Empower, Weaken, Vulnerable, Taunt, Decay, Thorns, and Soaked content has been
migrated to typed `ApplyCondition`; obsolete duplicate definitions were removed from
`statuses.json`.
