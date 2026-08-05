# Standard Condition Ability Authoring

Abilities reference canonical conditions through the strongly typed `ApplyCondition` operation. JSON enum values are serialized as names.

## Apply a condition

```json
{
  "id": "effect.poison",
  "operation": "ApplyCondition",
  "condition": "Poison",
  "target": "CurrentTarget",
  "baseValue": 3
}
```

`baseValue` is the player-facing X in `Condition(X)`:

- stack count for Bleed, Burn, Poison, Chill, Corrosion, and Vulnerable;
- seconds for Freeze, Stun, Taunt, Stealth, Unstoppable, Wound, Recovery, Decay, and Renewal;
- charges for Guard and Ward;
- Power percentage for Doom;
- reflected-damage percentage for Thorns.

Empower, Weaken, Haste, and Slow use their fixed canonical magnitude and duration, so they do not require `baseValue`.

Thorns is the one standard condition whose X is not its duration. Supply its duration separately in ticks:

```json
{
  "id": "effect.thorns",
  "operation": "ApplyCondition",
  "condition": "Thorns",
  "target": "Self",
  "baseValue": 20,
  "durationTicks": 80
}
```

For a condition applied repeatedly by one effect, `durationTicks` and `intervalTicks` describe the
application schedule; the condition still uses its canonical duration. This is useful for effects
such as “apply Burn(1) every second for 10 seconds.” Thorns cannot use this form because its
`durationTicks` belongs to the Thorns application itself.

## Query a condition

Effects and triggers can use `HasCondition` or `ConditionStacksAtLeast`:

```json
{
  "type": "ConditionStacksAtLeast",
  "subject": "Target",
  "condition": "Poison",
  "value": 3
}
```

## Cleanse and Dispel

`Cleanse` removes harmful standard conditions; `Dispel` removes beneficial standard conditions. Set `condition` for targeted removal or omit it to process all eligible conditions according to each condition's removal contract.

```json
{
  "id": "effect.cleanse-wound",
  "operation": "Cleanse",
  "condition": "Wound",
  "target": "Self"
}
```

Guard and Ward cannot be removed. Independent Wound, Decay, Doom, Recovery, Renewal, and Thorns applications use deterministic single-stack removal. Conditions whose contract says Cleanse removes the whole condition remove all their stacks.

## Related standard operations

- `GrantBarrier` uses the capped, source-tracked Barrier pool.
- `lifeStealPercentage` adds effect-specific Lifesteal to eligible direct damage.
- `ModifyThreat` adds or subtracts Threat weight and may have a duration.
- `ModifyRegenerationRate` adjusts Regeneration progress by percentage points.
- `ModifyRegenerationInterval` adjusts the default 50-tick interval; negative values make Regeneration faster.
- `ModifyAttribute` with `HealthRegeneration` changes the amount restored per trigger.

Timed modifier operations apply immediately and reverse their own value at expiration.

## Runtime references

- Authoring model: `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`
- Validation and compilation: `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityCatalog.cs` and `AbilityCompiler.cs`
- Runtime state: `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityRuntime.cs`
- Resolution: `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`
- Executable examples: `LL/tests/EssenceSystem.Tests/StandardConditionSystemTests.cs`
