# Damage Types

| Stable ID              | Runtime type | Status      | Scaling and defence                                        | Critical/periodic rules                                          |
| ---------------------- | ------------ | ----------- | ---------------------------------------------------------- | ---------------------------------------------------------------- |
| `damage-type.physical` | `Physical`   | Implemented | Effect-selected scaling; Armor and Armor Penetration.      | Category, not type, controls defaults.                           |
| `damage-type.bleed`    | `Bleed`      | Implemented | Per-stack Power snapshot; Armor and Armor Penetration.     | Canonically periodic, uncapped independent stacks.               |
| `damage-type.magical`  | `Magical`    | Implemented | Effect-selected scaling; Resistance and Magic Penetration. | Category, not type, controls defaults.                           |
| `damage-type.burn`     | `Burn`       | Implemented | Per-stack Power snapshot; Resistance and Magic Penetration. | Canonically periodic, uncapped independent stacks.              |
| `damage-type.poison`   | `Poison`     | Implemented | Per-stack Power snapshot; Resistance and Magic Penetration. | Canonically periodic, uncapped independent stacks.              |
| `damage-type.untyped`  | `None`       | Implemented | Bypasses typed defence.                                    | Still affected by eligible block, Damage Reduction, and Barrier. |

Damage type and [damage category](damage-categories.md) are independent axes.

Fire, Frost/Cold, Shadow/Dark, Necrotic, Holy, Nature, Arcane, and Lightning appear or are accepted as tags, not separate mitigation channels. “True Damage” is not a runtime type; `None` bypasses Armor/Resistance but is not fully unmodifiable true damage.

Current authored formulas select a base value and optional scaling attribute/coefficient per effect. No damage type has an inherent scaling statistic. Direct/periodic category and `CritEligibility` determine critical defaults.

Implementation: `LL/src/Core/Domain/Models/Damages/DamageType.cs`, `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`, `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`, and `LL/src/API/API.LL/Data/combat/`.
