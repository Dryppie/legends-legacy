# Dynamic Threat and Tanking System

Status: Proposed design; not yet implemented.

## Purpose

Threat should let a deliberately built tank control ordinary enemy attention while
still allowing damage, healing, defensive actions, abilities, Taunt, Stealth, and
encounter mechanics to interact with that control in tunable ways.

The system must support all of the following:

- Heavy Armor has a clear tank identity without gaining a fourth recipe attribute.
- Threat is tracked independently for each enemy.
- Threat scales naturally from Region 1 through Region 10 and beyond.
- Tanks can reliably hold ordinary attacks when performing their role correctly.
- Exceptional damage or healing can matter, but the balance coefficients determine
  how strongly it matters.
- Taunt is reliable and never merely increases a random chance.
- Bosses can deliberately bypass the tank through explicitly authored selectors.
- Targeting is stable and does not bounce randomly between party members.
- The complete behavior is measurable through the canonical combat analyzer.

## Current behavior and its limitations

The current runtime gives every `RuntimeCombatant` a global base Threat of 100.
`CurrentTarget` uses a new weighted-random roll for every target selection. Taunt
adds a configurable flat 100 Threat to the candidate while it is active.

In a four-character party, equal base Threat gives every character approximately
25% of ordinary attacks. If the tank has the current +100 Taunt bonus, the weights
are 200 for the tank and 100 for each of the other three characters. The tank is
therefore selected only:

```text
200 / (200 + 100 + 100 + 100) = 40%
```

This is not reliable tanking. The current system also has these limitations:

- Damage does not generate Threat.
- Effective healing does not generate Threat.
- Barrier creation or absorption does not generate Threat.
- Heavy Armor's `Threat` tag has no runtime effect.
- Threat is global rather than specific to the enemy whose attention is being
  controlled.
- Target selection has no persistent target lock or switching threshold.
- Flat authored Threat values become progressively less meaningful if other
  Threat sources scale with combat output.

## Core model

Every combatant that can select an enemy owns a Threat table for its living
opponents.

For example:

```text
Ancient Golem's Threat table

Heavy tank       4,800
Damage dealer    3,200
Healer           1,400
Balanced build   1,100
```

The table belongs to the Ancient Golem. A second enemy owns a separate table.
Damaging the Ancient Golem must not automatically create damage Threat on the
second enemy, while party-wide support actions may create Threat on both enemies.

Conceptually:

```text
Threat[observing combatant, candidate target]
```

The observing combatant also stores its current Threat-selected target. Threat
tables and current targets exist only for the duration of a combat simulation and
do not require database persistence.

## Threat generation formula

Every qualifying combat event first produces Base Threat. The acting character's
Threat Generation multiplier is then applied once:

```text
Generated Threat = max(0, Base Threat * Threat Generation Multiplier)
```

Threat Generation is multiplicative around a baseline of `1.0`:

```text
Threat Generation Multiplier = max(0, 1 + Threat Generation Bonus / 100)
```

The initial recommended event coefficients are:

| Event                                |       Base Threat | Recipient table       |
| ------------------------------------ | ----------------: | --------------------- |
| Final damage delivered               |   `damage * 1.00` | The damaged enemy     |
| Effective health restored            |  `healing * 0.50` | Every living enemy    |
| Barrier damage absorbed              | `absorbed * 0.50` | Every living enemy    |
| Health regeneration restored         | `restored * 0.50` | Every living enemy    |
| Lifesteal health restored            | `restored * 0.50` | Every living enemy    |
| Overhealing                          |               `0` | None                  |
| Barrier granted but never consumed   |               `0` | None                  |
| Avoided or mitigated incoming damage |     `0` initially | None                  |
| Applying a normal condition          |     `0` initially | None unless authored  |
| Explicit Threat effect               |          Authored | Defined by the effect |

These are calibration defaults, not immutable constants. Each coefficient must
be represented in one versioned options object and measured through canonical
party simulations before activation.

### Damage Threat

Damage Threat uses damage that actually reaches Health or Barrier after defensive
resolution:

```text
Delivered Damage = Final Health Damage + Barrier Absorbed
Damage Threat = Delivered Damage * Damage Threat Coefficient
```

It does not use pre-mitigation damage. Dodge, Block, Armor, Resistance, Guard,
Damage Reduction, and similar mechanics can reduce the Threat generated by the
attacker because they reduce the delivered result.

Direct, periodic, reflected, and stored damage can all generate Threat. The
combatant credited as the damage source receives the Threat. Self-damage does not
generate Threat against allies or enemies.

### Healing Threat

Healing Threat uses only Health actually restored:

```text
Effective Healing = Health After - Health Before
Healing Threat = Effective Healing * Healing Threat Coefficient
```

Overhealing generates nothing. Healing Threat is added independently to every
living opponent's table because every active enemy can observe party recovery.
It is not divided by the number of enemies. Dividing would make a healer less
noticeable to each individual enemy merely because more enemies entered combat.

Self-healing, regeneration, and lifesteal use the same effective-restoration rule.
Their coefficients remain independently configurable if calibration shows that
one source needs different behavior.

### Barrier Threat

Granting Barrier does not immediately generate Threat. The Barrier source gains
Threat when that Barrier actually absorbs damage:

```text
Barrier Threat = Absorbed Contribution * Barrier Threat Coefficient
```

The existing barrier contribution records already preserve the source of each
absorbed contribution. This allows a support character to receive credit for a
Barrier placed on the tank and allows a self-shielding tank to receive its own
Threat.

Unused and expired Barrier generates no Threat.

### Defensive-action Threat

Avoidance and mitigation do not generate Threat in the initial version. Armor,
Resistance, Dodge, Block, Guard, and Damage Reduction already let the tank survive
the attention it holds; automatically converting all prevented damage into Threat
could cause a positive feedback loop in which being attacked makes aggro
effectively impossible to lose.

If simulations show that low-damage tanks cannot sustain adequate Threat, a
separate defensive coefficient may be introduced for one or more of:

- blocked damage;
- Barrier absorbed from the tank's own Barrier;
- damage prevented by Guard;
- successful Dodge events;
- explicitly authored defensive abilities.

Such a coefficient must be added deliberately and measured. It must not be hidden
inside the damage formula.

## Heavy Armor and Threat Generation

Heavy Armor retains its three recipe attributes:

- Armor;
- Maximum Health;
- Resistance.

Threat Generation is armor-family behavior rather than a fourth budgeted recipe
attribute. The initial recommendation is:

| Equipped Heavy pieces | Threat Generation Bonus | Final multiplier |
| --------------------: | ----------------------: | ---------------: |
|                     0 |                     +0% |             x1.0 |
|                     1 |                   +100% |             x2.0 |
|                     2 |                   +200% |             x3.0 |
|                     3 |                   +300% |             x4.0 |

Each equipped Heavy Head, Chest, or Legs recipe contributes +100 percentage
points. A complete Heavy set therefore generates four times the Threat produced
by its qualifying combat actions.

This modifier does not consume equipment stat budget and is not rolled, tempered,
or upgraded. It derives from the equipped recipe family's declared `Heavy` role.
It must be visible in the character overview:

```text
Threat Generation: x4.0
Heavy Armor Presence: +300%
```

The x4.0 value is a calibration starting point. It is intended to let a tank with
substantially lower damage remain ahead of a dedicated damage character while
leaving room for poor Threat generation, deaths, Threat reductions, and encounter
mechanics to matter.

## Opening Threat

Every candidate begins with a small opening Threat value so enemies can select a
valid target before the first combat event. The opening value uses the candidate's
Threat Generation multiplier:

```text
Opening Threat = Base Opening Threat * Threat Generation Multiplier
```

With a recommended Base Opening Threat of 100, a full-Heavy tank begins at 400
while an ordinary character begins at 100. This gives the tank opening attention
without creating tier-dependent authored values.

Opening Threat is only an initial targeting anchor. Combat-generated Threat is
expected to become the dominant value as the encounter continues.

If the intended design ultimately requires a completely inactive tank to retain
aggro indefinitely against active damage dealers, that is a separate policy. It
should be represented as a visible passive Threat pulse or a minimum Heavy
Presence floor rather than being concealed inside damage Threat.

## Target selection and aggro stability

Threat-aware targeting is deterministic. Weighted random selection is removed
from `CurrentTarget`.

An observing combatant follows this order:

1. Use a valid forced target created by Taunt.
2. Remove dead or otherwise invalid candidates.
3. If there is no current target, select the candidate with the highest effective
   Threat.
4. Retain the current target while no competitor exceeds the switching threshold.
5. Switch when a competitor's effective Threat exceeds the current target by the
   configured threshold.

The initial switching rule is:

```text
Required Threat to pull aggro = Current Target Threat * 1.10
```

The universal 110% threshold is appropriate while combat has no spatial model.
If range or positioning later becomes meaningful, different melee and ranged
thresholds can be considered.

When two candidates have equal effective Threat, the current valid target wins.
If there is no current target, encounter insertion order provides the deterministic
tie-break.

There is no passive Threat decay in the initial version. Death, combat end,
explicit Threat effects, Taunt, and Stealth provide the necessary state changes.

## Taunt

Taunt is a reliable forced-target mechanic, not a flat weighted bonus.

When a candidate successfully gains Taunt, each affected opposing combatant sets
that candidate's Threat to at least 10% above its current leader:

```text
Taunter Threat = max(
    Existing Taunter Threat,
    Highest Threat * 1.10)
```

The opposing combatant then treats the taunter as its forced target for the
authored duration.

Taunt follows these rules:

- The next eligible `CurrentTarget` action targets the taunter.
- `CurrentTarget` remains forced for the complete Taunt duration.
- Reapplication refreshes the duration and recalculates the minimum lead.
- The most recently applied valid Taunt wins when multiple Taunts overlap.
- When Taunt expires, ordinary Threat selection resumes.
- The taunter retains the Threat established by Taunt and must continue generating
  Threat to remain ahead.
- Taunt does not affect explicitly authored random, area, health-based, summon,
  or other special selectors.

Recommended player-facing wording:

> Forces enemies using normal targeting to attack you for X seconds and places
> you above their current highest Threat.

The current flat `TauntThreatBonus` option becomes obsolete under this model and
is replaced by `TauntThreatLeadPercent`, initially 10%.

## Stealth

Stealth is evaluated after ordinary Threat modifiers.

While a candidate is Stealthed:

- its underlying Threat table values continue changing;
- it is not selected by ordinary Threat targeting while another visible valid
  candidate exists;
- any existing ordinary target lock on it is released;
- explicit selectors that are documented as ignoring Stealth may still target it;
- when Stealth expires, its current underlying Threat immediately becomes active.

This is clearer than assigning an arbitrary effective Threat of exactly 1, while
still preserving accumulated Threat underneath the temporary protection state.
If compatibility requires the existing value-1 rule, the final target-selection
behavior must still guarantee that a visible candidate is preferred.

## Target selectors

Threat applies only to selectors that represent ordinary hostile attention.

| Selector                   | Uses Threat? | Notes                                             |
| -------------------------- | ------------ | ------------------------------------------------- |
| `CurrentTarget`            | Yes          | Uses target lock, Taunt, and switching threshold. |
| `RandomEnemy`              | No           | Explicit random encounter mechanic.               |
| `LowestHealthEnemy`        | No           | Explicit execution or pressure mechanic.          |
| `HighestHealthEnemy`       | No           | Explicit authored mechanic.                       |
| `LowestCurrentHealthEnemy` | No           | Explicit authored mechanic.                       |
| `HighestMaxHealthEnemy`    | No           | Explicit authored mechanic.                       |
| `AllEnemies`               | No           | Area effect.                                      |
| `TwoEnemies`               | No           | Multi-target effect.                              |
| Event-locked targets       | No reroll    | Preserve the event's resolved target.             |

Basic attacks use `CurrentTarget`. Active abilities whose primary selector is
`CurrentTarget` use the same locked target. Every effect in one active ability
activation that refers to `CurrentTarget` must resolve against the activation's
locked primary target unless that target becomes invalid during resolution.

Bosses and creatures should use explicit non-Threat selectors whenever the
encounter is intended to pressure non-tanks. Party damage must be authored as a
mechanic rather than emerging from random failure of the aggro system.

## Explicit Threat effects

The existing `ModifyThreat` operation cannot remain an unbounded flat global
candidate value as the primary authoring tool. Threat now belongs to an observing
combatant's table and most ordinary Threat scales with combat output.

The runtime should support explicit operations with unambiguous scope:

- add Threat on the affected enemy's table;
- add Threat on every living enemy's table;
- multiply future Threat Generation for a duration;
- reduce current Threat by a percentage;
- reset current Threat;
- match the current leader;
- move above the current leader by a percentage;
- force a target through Taunt.

Fixed additive Threat remains permissible for tightly controlled effects, but
content should prefer percentage-of-current-leader or generated-Threat modifiers
when the effect must remain meaningful across open-ended tiers.

Existing authored content must be audited. In particular:

- creature `Threatening Presence` currently adds a flat 50 Threat;
- the power benchmark uses an extremely large flat Threat value to pin a target;
- Taunt documentation currently describes a flat configurable +100 bonus;
- Threat-weighted targeting documentation currently specifies random selection.

The benchmark should receive an explicit target-lock mechanism instead of relying
on a billion-point Threat modifier.

## Summons

Summons are independent combatants and initially own their own generated Threat.
Damage and healing produced by a summon are credited to that summon unless the
summon definition explicitly redirects Threat to its owner.

This permits authored tank summons and disposable decoys. A future summon option
may declare one of:

- `Independent`: summon owns its Threat;
- `TransferToOwner`: all generated Threat is credited to the owner;
- `SharedWithOwner`: a configurable portion is transferred.

The initial implementation should retain independent Threat unless existing
content proves that owner transfer is required.

When a summon expires or dies, it is removed from every opponent's eligible target
set and target selection immediately resolves a replacement.

## Death, removal, and combat lifecycle

- Threat tables are initialized at combat start.
- Newly summoned combatants receive opening Threat entries in opposing tables.
- Dead or expired combatants remain in no eligible target set.
- An observer whose current target dies retargets before its next eligible action.
- Revived combatants re-enter with their retained or reset Threat according to the
  eventual revival mechanic's explicit rule.
- Combat end discards every Threat table and target lock.
- Checkpoint and resumed simulations must serialize Threat tables, target locks,
  forced targets, and remaining Taunt state to preserve deterministic parity.

## Configuration

All global defaults belong to one versioned options object. A representative
shape is:

```csharp
public sealed record ThreatSystemOptions(
    int ModelVersion = 2,
    double BaseOpeningThreat = 100,
    double DamageThreatCoefficient = 1.00,
    double HealingThreatCoefficient = 0.50,
    double RegenerationThreatCoefficient = 0.50,
    double LifestealThreatCoefficient = 0.50,
    double BarrierAbsorptionThreatCoefficient = 0.50,
    double AggroSwitchThreshold = 1.10,
    double TauntThreatLeadPercent = 10,
    double ThreatGenerationBonusPerHeavyPiece = 100);
```

These values must not be scattered through combat resolution. Changing them
changes encounter behavior and requires a new balance report. Production
activation should reference a declared Threat model version in the same manner as
the equipment balance version.

## Combat telemetry

Threat must be observable even if the complete table is not always shown to
players.

The engine should record:

- Threat generated by source category;
- Threat generated by each combatant;
- current target changes;
- previous and new target;
- reason for each target change;
- current leader and lead percentage;
- Taunt applications, refreshes, expirations, and resisted or invalid attempts;
- time each party member spent as the ordinary target;
- number of ordinary single-target attacks received by each member;
- attacks that bypassed Threat because of an explicit selector.

Suggested target-change reasons:

```text
InitialSelection
ThreatExceeded
Taunted
TauntExpired
TargetDied
TargetStealthed
TargetInvalidated
SummonedTargetEntered
ExplicitSelector
```

The character overview should display the final Threat Generation multiplier.
Developer diagnostics should expose the full per-enemy table and its event-source
breakdown.

## Canonical analyzer contract

The existing TTK/TTD analyzer measures whether the tank survives pressure. Threat
verification must additionally measure whether the tank receives the intended
pressure in a real four-role party.

The canonical party contains:

- one full-Heavy defensive build;
- one sustain build;
- one balanced build;
- one offense build.

At Tiers 1, 5, 10, 20, 50, and 100, the analyzer runs single-target, multi-enemy,
Taunt, Stealth, and scripted-bypass scenarios over the same production combat
engine.

Initial activation gates:

- After the opening second, the defensive build receives at least 95% of ordinary
  `CurrentTarget` attacks in the canonical single-target party scenario.
- Taunt redirects the next eligible ordinary action in 100% of valid cases.
- Taunt remains effective for its exact authored duration.
- A competitor that exceeds 110% of current-target Threat pulls aggro on the next
  ordinary target resolution.
- A competitor below the switching threshold never causes target oscillation.
- Healing creates measurable Threat but does not overtake a correctly functioning
  canonical tank under expected throughput.
- Every enemy maintains an independent table in the multi-enemy scenario.
- Tank death produces a valid replacement target before the enemy's next action.
- Explicit random and area mechanics produce exactly the same target behavior with
  Threat enabled or disabled.
- Tank ordinary-target share varies by no more than 3 percentage points across
  tier checkpoints.
- Threat event totals reconcile with damage, healing, regeneration, lifesteal, and
  Barrier telemetry within rounding tolerance.

Calibration must also include adversarial scenarios:

- offense damage increased substantially above the canonical value;
- sustain healing increased substantially above the canonical value;
- tank damage reduced to zero;
- tank abilities disabled;
- one, two, and three Heavy pieces;
- two simultaneous tanks;
- multiple Taunts during an existing Taunt;
- summoned tanks and threat-producing summons;
- long encounters that expose numerical growth or precision problems.

The zero-damage tank scenario is diagnostic even though the initial dynamic model
does not promise indefinite passive aggro. Its result tells us whether opening
Threat, Heavy multipliers, Taunt cadence, and any future defensive Threat source
need adjustment.

## Player-facing rules

The complete internal table does not need to be explained in every tooltip, but
the following rules must be consistently visible:

1. Dealing damage, restoring Health, and having Barriers absorb damage generate
   Threat.
2. Heavy Armor multiplies the Threat you generate.
3. Enemies normally attack the character with the highest Threat.
4. Enemies do not switch for very small Threat differences.
5. Taunt forces ordinary attacks and moves the tank above the current Threat
   leader.
6. Some explicitly described enemy abilities ignore Threat.

Suggested glossary text:

> **Threat** determines whom enemies choose for ordinary attacks. Damage, effective
> healing, and absorbed Barriers generate Threat. Heavy Armor increases Threat
> generation. Taunt temporarily forces ordinary enemy targeting.

## Implementation sequence

### Phase 1: Runtime state

- Replace the combatant-global scalar Threat with per-observer Threat tables.
- Add persistent current-target and forced-target state.
- Include the new state in checkpoint/resume behavior.
- Preserve deterministic insertion-order tie-breaking.

### Phase 2: Event integration

- Generate damage Threat after final Health and Barrier delivery is known.
- Generate healing, regeneration, and lifesteal Threat from effective restoration.
- Credit Barrier absorption Threat to each contribution source.
- Add summon lifecycle entries and cleanup.

### Phase 3: Targeting

- Route basic attacks and `CurrentTarget` abilities through the persistent target
  resolver.
- Add the 110% switching threshold.
- Keep explicit selectors independent from Threat.
- Guarantee one activation-level target lock for multi-effect abilities.

### Phase 4: Heavy Armor and authored effects

- Derive Threat Generation from equipped Heavy recipe roles.
- Expose the multiplier in combat construction and character overview DTOs.
- Replace flat Taunt bonus behavior with forced targeting and leader matching.
- Audit and migrate existing `ModifyThreat` content.
- Replace the benchmark's billion-point Threat pin with an explicit diagnostic lock.

### Phase 5: Telemetry and analysis

- Add Threat source totals and target-change events to combat telemetry.
- Extend canonical party analysis with Threat-share and Taunt gates.
- Run smoke, development, and activation seed counts at every tier checkpoint.
- Calibrate coefficients without changing damage, healing, or equipment budget
  formulas merely to make Threat pass.

### Phase 6: Documentation and UI

- Update the combat lexicon's targeting, Taunt, Stealth, and formula references.
- Add the Threat Generation multiplier to the character overview.
- Add target and Taunt indicators to relevant combat presentation.
- Document explicit boss abilities that bypass Threat.

## Compatibility and deployment implications

The Threat table is runtime combat state and requires no EF Core migration. Heavy
Armor recipes already identify their role and contain Threat-related tags, but
runtime construction must use the canonical recipe role rather than treating a
free-form tag as an authoritative stat.

This change affects deterministic combat results. Saved or resumable combat
checkpoints require an explicit compatibility decision: either migrate the saved
runtime state, finish them under the old Threat model, or invalidate them during
the version transition. The selected behavior must be documented before release.

Existing balance baselines, combat snapshots, power-rating simulations, dungeon
simulations, World Tower simulations, and ability-system tests must be regenerated
or deliberately reconciled because target selection can change damage distribution
without changing total authored damage.

No production activation should occur until the canonical Threat gates, existing
TTK/TTD gates, deterministic replay tests, and encounter-specific regression tests
all pass under the same declared model version.
