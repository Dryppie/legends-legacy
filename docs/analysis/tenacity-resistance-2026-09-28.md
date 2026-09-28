# Tenacity: chance to ignore harmful applications

The user requested replacing duration reduction with a chance to ignore the condition entirely. Under the selected attribute rules 18, each eligible application now rolls `clamp(Tenacity, 0, 80) / 100` using the seeded combat RNG. Fractional percentages are retained. A value of 20 means a 20% resist chance, not a shorter effect.

## Behavior

- Standard harmful conditions include Slow, Weaken, Wound, Decay, Poison, Burn, Bleed, Stun, Chill, Freeze, Corrosion, Doom, Mark, Silence, Exposed, Vulnerable and Soaked. Harmful custom statuses use the existing Debuff/Affliction/Control tag classification, including indefinite statuses.
- One roll covers the whole application and its stack count. Repeated applications roll separately. Rejected attempts neither add stacks nor refresh/replace existing effects, and do not publish `OnStatusApplied`.
- Accepted effects retain their full authored duration, potency and damage. Doom keeps its original countdown and full stored/expiry damage; Tenacity no longer reduces its damage after it lands.
- Beneficial conditions/statuses and the existing Taunt attention mechanic do not roll. Stun/Freeze against bosses continue to use Stagger. Existing intrinsic application chances, Unstoppable and Ward checks retain their ordering; Tenacity is checked after these defenses and boss-Stagger conversion. Ward therefore still consumes its charge before a Tenacity roll would be needed.
- `GuaranteedConditionApplication` continues to bypass application defenses, now including Tenacity. This preserves scripted effects such as Royal Cocoon's self-stun. Rules 17 retain their old status-duration behavior and do not roll Tenacity.

Equipment prices, weights and the 80% cap are unchanged. This is a mechanical change, not a new balance certification; earlier duration-based Tenacity simulations remain historical evidence and must not be described as validation of this version. Retain their original executables for reproduction.

## Changed files

- `AttributeRules`, `ConditionResistanceRules` and `FastCombatEngine`: shared probability/eligibility, seeded application checks, removal of current-rule duration and Doom-damage reductions.
- `AttributeCatalog` and `CompareEquipmentQuery`: updated tooltip and a percentage-based resist-chance comparison instead of a three-second duration example.
- `EventType`, the frontend combat-event enum and `EntityStats`: explicit `StatusEffectResisted` logs and `HarmfulApplicationsResisted` counts. The old duration-prevention JSON field remains readable for historical results and is not populated by new fights.
- `TenacityCombatTests`, `AttributeRedesignTests` and `EquipmentComparisonProjectorTests`: landed/resisted cases, custom statuses, refresh/stack/replace behavior, caps, fractional chances, guaranteed effects, boss Stagger, legacy rules and comparison output.
- Current implementation and rollout documentation link this revised mechanic.

## Deployment

No new database migration, item conversion or configuration change is needed for this follow-up. Build and restart the API, worker and any other combat hosts together, and serve the updated frontend. Attribute descriptions arrive through bootstrap, so clients need to refresh. Existing equipment retains its authored Tenacity amount. No local database records or external environments were changed by this implementation.

## Verification

The final affected regression run passed **712 tests**, with zero failures or skips:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~TenacityCombatTests|FullyQualifiedName~AttributeRedesignTests|FullyQualifiedName~AttributeCombatSystemTests|FullyQualifiedName~StandardConditionSystemTests|FullyQualifiedName~AbilitySystemTests|FullyQualifiedName~CombatStyle|FullyQualifiedName~EquipmentComparisonProjectorTests|FullyQualifiedName~CombatStats'
```

Log: `TestResults/tenacity-resistance-final-regression.log`. Coverage includes ignored/landed applications for every eligible standard condition, tagged statuses, preserved existing stacks and toggled states, guaranteed applications, boss Stagger, zero/capped/fractional Tenacity, independent repeated applications, legacy resistance and comparison percentages. The build reported 16 existing warnings and zero errors.

From `LL/src/Presentation/ll`, `npm.cmd exec -- tsc --noEmit --project tsconfig.app.json` passed (log: `TestResults/tenacity-frontend-typecheck.log`). `git diff --check` passed. No verification command was blocked. The frontend production build and a new balance simulation were not run; frontend code changes are limited to the event enum, while tooltip and comparison text come from the backend.
