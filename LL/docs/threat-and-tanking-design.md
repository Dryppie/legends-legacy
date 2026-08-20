# Threat & Tanking — Design

**Status:** Design proposal (not implemented)
**Author:** Design pass, 2026-08-18
**Goal:** make tank builds able to actually perform a tank's job
**Scope:** `FastCombatEngine` targeting, threat state, and the ally-protection primitives
**Related:** `docs/threat-ability-authoring.md` (content authoring companion),
`docs/raid-system-design.md` (three-wing raids), World Tower (parties capped at 5)

> **Note:** parts of this design have since been implemented — `AbilitySpec.ThreatValue` /
> `ThreatMultiplier`, `AbilityThreatRules` / `AbilityThreatTuning`, `AbilityEffectOperation.ModifyThreat`
> and `GrantCover`, and `StandardConditionType.Taunt` / `Mark` / `Cover` all now exist, with four
> abilities authoring explicit threat values. Sections 1–2 describe the pre-implementation baseline and
> are retained as the diagnosis; §6.1 of the authoring companion covers what still needs fixing.

---

## 0. TL;DR

Tanking does not work today, and it isn't one bug — it's **four independent failures**, each of
which alone is enough to break the role:

1. **Threat never changes.** It is a static `100f` for every combatant, for the whole fight. There is
   no accrual from damage, healing, protecting, or being hit. Tanking is not an *activity* the engine
   can observe.
2. **Proportional roulette caps how much attention anyone can hold.** Target selection is
   threat-proportional random. Your share is `threat_you / Σthreat`. Taunt adds a flat `+100`, so a
   taunting tank against four allies gets `200/600` = **33%**. There is no amount of tank-ness that
   fixes this, because the maths is linear.
3. **You cannot take a hit for an ally.** There is no damage redirection, no intercept, no cover, no
   damage sharing. `ApplyDamage` takes one target with no substitution hook. Even at 100% aggro a
   tank has no way to protect anyone.
4. **Taunt is backwards in content.** Both abilities that apply Taunt apply it to *enemies*. No
   ability in the game applies Taunt to self. There is literally no tank tool to press.

The recommendation is a **three-layer fix**, because layers 1–2 handle single-target attention and
layer 3 handles what threat fundamentally can't (AoE, multi-target, bad luck):

| Layer | What it does | Cost |
|---|---|---|
| **1. Ability-generated threat** | Abilities carry an authored `threatValue`; activating them generates threat. Threat becomes a property of the **loadout you built** | Medium |
| **2. Attention curve** | Replace linear roulette weight with `(threat / median)^E`, so threat has real dynamic range | Small |
| **3. Cover** | A new primitive: redirect a % of an ally's incoming damage to the guardian, through the guardian's own mitigation | Medium |

Plus one **hard taunt** (forced target, short, on cooldown) as the on-demand tool, and a fix for the
accidental summon-dilution exploit that is currently the game's most powerful tanking mechanic.

One framing principle drives every choice below: **this is an auto-battler.** Nobody presses a taunt
button mid-fight. The player's only levers are the **abilities they equip** and the **gear they
wear** — so those are the only things that may determine threat, and this design uses only the first
of them (see §3.1.5 for why gear deliberately stays out of it). Any threat model whose output
depends on realized combat outcomes (damage dealt, damage taken, healing done) is a race the player
cannot steer, and it cannot be predicted or displayed before the fight. Threat here is therefore
generated **per ability activation, from an authored value**, never as a coefficient on a realized
damage number.

That constraint is not a compromise — it's the reason this design works. Because threat is a pure
function of the loadout, the game can compute and *show* a build's **threat per second** on the
character sheet, and the raid Battle Plan can predict the aggro split before a shot is fired.

---

## 1. Verified current behaviour

Everything in this section is quoted or derived from the code as it exists. It is the baseline any
proposal has to move.

### 1.1 Threat state

`RuntimeCombatant` (`Combat/Engine/AbilityRuntime.cs`):

```csharp
public const float BaseThreat = 100f;
private float _threat;
// ctor:
_threat = BaseThreat;
// accessor:
public float Threat => Math.Max(0, _threat);
public void AdjustThreat(float amount) => _threat += amount;
```

A **single global scalar per combatant.** No threat table, no per-attacker or per-target dimension.

`AdjustThreat` has exactly **two call sites in the whole solution**: the `ModifyThreat` effect
handler (`FastCombatEngine.cs:1140`) and the timed-modifier revert (`:2901`). `_threat` is read in
exactly **one** place: `GetEffectiveThreat` (`:3860`).

No decay. No per-tick recalculation. No reset logic. It resets between encounters only because fresh
`RuntimeCombatant` objects are built per encounter.

> Latent bug: `_threat` is unclamped but the getter floors at 0, so a large negative `ModifyThreat`
> leaves the backing field negative and its expiry revert silently refunds more than was ever visible.

### 1.2 Target selection

```csharp
private double GetEffectiveThreat(RuntimeCombatant combatant)
{
    if (combatant.HasCondition(StandardConditionType.Stealth))
        return 1d;

    var threat = combatant.Threat;
    if (combatant.HasCondition(StandardConditionType.Taunt))
        threat += _tauntThreatBonus;

    return threat;
}
```

`SelectFirstEnemy` — **misnamed, it is a roulette wheel**, and it is the primary targeting path
(called from `TickBasicAttack` and `FillTargets`): sum effective threat over living enemies,
`roll = _random.NextDouble() * totalThreat`, walk subtracting until negative. Exactly proportional.
No stickiness, no hysteresis, no memory.

**Re-rolled every single basic attack.** An ability activation rolls once and pins that target across
the effects of that activation (`SelectLockedEnemy`), but nothing persists across attacks or ticks.
With a boss and N equal-threat friendlies, the boss's attack stream is an i.i.d. uniform sample.

`TauntThreatBonus` defaults to `100f` and **no caller ever sets it** —
`CombatEngineExecutor.ExecuteCoreAsync` omits it from its named-argument list. So in production Taunt
always exactly doubles a combatant's weight.

Other selectors, all **threat-blind**:

- `SelectRandomEnemy` — uniform.
- `FillRandomTargets` — uniform top-k, used by `RandomAlly`, `TwoRandomEnemies`, `ThreeRandomEnemies`.
- `FillFilteredTargets` — **list-order scan**; `TwoEnemies` / `ThreeEnemies` take the *first* 2 or 3
  in list order. Not random, not threat-aware. A boss's multi-target ability hits the same slots
  every time.
- `SelectExtremumTarget` — argmax/argmin on health; ties go to earliest in list order.
- `SelectThreatWeightedEnemy` — **dead code.** One grep hit: its own declaration.

### 1.3 Nothing generates threat

Definitively confirmed. `ApplyDamage`, `ApplyHeal`, `GrantBarrier`, `ApplyStatus`, `ApplyCondition`
— none reference threat. Dealing damage, healing, shielding, killing, dying, blocking and reflecting
all leave threat untouched.

Threat is **static 100 for everyone**, unless an ability explicitly runs `ModifyThreat`, or the
combatant has Taunt (+100) or Stealth (hard floor to 1).

### 1.4 `ModifyThreat` in content: one ability out of 168

Flat additive, negatives allowed, permanent if `durationTicks == 0`, reverted on expiry otherwise.

```json
{ "id": "ability.creature.hobgoblin.threatening_presence", "kind": "Passive",
  "effects": [ { "operation": "ModifyThreat", "target": "Self", "baseValue": 50 }, … ] }
```

Permanent +50 self-threat on a *hostile* creature. That is the entire content footprint.

### 1.5 Taunt in content: two abilities, both pointed the wrong way

Taunt is a `StandardConditionType` (not a status), unique, no stacks, duration = authored
`baseValue` seconds × 10. Not in `IsHarmfulCondition`, so it is treated as beneficial: **not blocked
by Ward, and strippable by `Dispel`.** Three engine references total: apply, id-mapping, and
`GetEffectiveThreat`. **No forced-target semantics anywhere.**

```json
// transparent_slime: "Gain Guard(4) and Taunt a random enemy for 11 seconds."
{ "operation": "ApplyCondition", "target": "RandomEnemy", "baseValue": 11, "condition": "Taunt" }

// flame_imp: on applying Burn, 20% chance to Taunt the event target
{ "operation": "ApplyCondition", "target": "EventTarget", "baseValue": 15, "condition": "Taunt", "chancePercent": 20 }
```

Both apply Taunt to an **enemy**, which raises *that enemy's* weight in whoever is hostile to it.
That is a focus-fire mark, not a tank tool. **Zero abilities apply Taunt to self.**

### 1.6 Ally-protection primitives: almost none

| Primitive | Can it protect an ally? |
|---|---|
| `GrantBarrier` | **Yes** — `AllAllies`, `HighestMaxHealthAlly` used in content. Cap `MaxHealth × 2.5`. FIFO consumption with source attribution. Cannot be redirected; only absorbs on its holder. |
| `Thorns` | Partly — `bramble_shield` grants Thorns to `HighestMaxHealthAlly`. Retaliation, not protection. |
| `ModifyDamageTaken` | Engine-generically could target allies; **all 5 content uses are Self**. |
| `Guard` (25% cut per charge), `Ward` (negate next harmful condition) | Self only in all content. Charge-based, no duration, dispel-immune. |
| Block (50% cut, cap 60% chance), Dodge (cap 40%), `DamageReduction` (cap 40%) | Self only, by nature. |
| **Damage redirection / intercept / cover / sharing** | **Does not exist.** No hook in `ApplyDamage` can substitute a recipient. |

So "the tank takes the hit instead" is **not expressible today**. The only way a tank currently
reduces an ally's damage is by occupying a slot in the roulette (dilution) or pre-loading barrier.

### 1.7 No Threat attribute

`AttributeType` runs Power(0)…AttackSpeed(19), with 17 and 18 retired. There is **no `Threat`
entry**, and none in `AttributeCatalog` or `EquipmentStatBudgetCatalog`. Threat is not gearable, not
budgetable, not displayable, and cannot appear in Combat Rating.

### 1.8 Where multi-friendly combat actually happens

| Mode | Friendlies **per combat** | Notes |
|---|---|---|
| **World Tower** | **≤5** | `requiredSlots` ∈ {1, 3, 5, 10, 15} across 10 floors, but **parties are being capped at 5** — a 15-slot floor is 3 parties of 5, each resolving as its own combat. So the engine never sees more than 5 friendlies. |
| **Raids** (designed, not built) | **≤5** per wing | `docs/raid-system-design.md` — three wings, three separate battles |
| **PvP tournaments** | up to **3 v 3** | `CombatMode.Pvp` |
| Idle / dungeons | **1** (+ summons) | Threat only matters via summons |

> **This 5-per-combat cap is the most important constraint in the whole document**, and it is very
> good news. It means the attention curve only ever has to work against **at most 4 other allies**.
> Threat design for a 15-body free-for-all is a genuinely hard tuning problem (see the "vs 14" column
> in §3.2); threat design for a 5-body party is a solved one. It also makes **one tank per party** the
> natural composition — a 15-slot Tower floor wants three tanks, one per party, which is a legible
> rule players can internalise.
>
> If Tower instead resolves all 15 friendlies in a *single* combat, everything below still works but
> `AttentionExponent` needs to be ~4.0 rather than 2.5, and `MaxWeightRatio` needs raising. Worth
> confirming which it is before tuning.

### 1.9 The accidental tanking mechanic: summon dilution

Summons are ordinary `RuntimeCombatant`s built with the same constructor, so **every summon gets
`_threat = 100`** — identical weight to the player who summoned it.

`niCopy`: `maxActive: 9`, permanent, `canBasicAttack: false`, MaxHealth 10% of owner, **Armor ×1.0
and Resistance ×1.0 inherited**, summoned nine-at-once by `ability.creature.ni.ninefold`.

Nine extra bodies at full threat weight drops the owner to **10% of incoming attacks**. That is a
90% effective damage reduction, undesigned, and it is currently the strongest defensive mechanic in
the game. `kharadIronPillar`, `kharadAetherPillar`, `venomSpawn` (maxActive 5) and
`morrowmawBroodling` (unlimited) all do smaller versions of the same thing.

**This must be addressed by any threat change**, and arguably should be fixed regardless.

---

## 2. Why tanking fails — the arithmetic

The dilution ceiling is worth writing out, because it's the reason "just buff taunt" won't work.

With linear proportional roulette, a tank's share of attacks against `N` allies at threat 100 each:

```
share = T_tank / (T_tank + 100N)
```

| Tank threat | vs 2 allies | vs 4 allies | vs 14 allies (Tower F-15) |
|---|---|---|---|
| 100 (no taunt) | 33% | 20% | 6.7% |
| 200 (**today, taunted**) | **50%** | **33%** | **12.5%** |
| 400 | 67% | 50% | 22% |
| 1000 | 83% | 71% | 42% |
| 1600 | 89% | 80% | 53% |

To reach a credible 80% on a five-person wing a tank needs **16× base threat**. Taunt gives 2×.
And on a 15-slot Tower floor, even 1600 threat only reaches 53%.

There are two ways out: make threat's dynamic range enormous (fragile, hard to author, hard to
display), or **change the curve so moderate threat ratios produce large attention shares**. The
second is much cheaper and is what §3 proposes.

---

## 3. The model

Three layers. Layers 1 and 2 together decide **who the boss attacks**; layer 3 decides **what happens
to the damage that lands elsewhere anyway**. All three are needed: threat alone cannot address AoE,
and Cover alone would make aggro irrelevant.

- **Layer 1** — abilities generate threat, so tanking is a loadout decision.
- **Layer 2** — an attention curve turns a modest threat ratio into a decisive share of attacks.
- **Layer 3** — Cover lets a guardian absorb a capped share of allies' incoming damage.

The core of layers 1–2 keeps the engine's existing threat-proportional roulette rather than replacing
it with a deterministic "highest threat is targeted" rule. That is deliberate: an argmax would make one
point of threat flip 100% of attacks, so every non-tank composition becomes "whoever is marginally
highest eats everything and dies," and all fight-to-fight variety disappears — which matters in a game
where the player watches a replay rather than playing. Roulette with an amplified weight curve gives
tanks reliability without that cliff. Determinism is used in exactly one place, where it *is* correct:
the hard taunt (§4.3).

### 3.1 Layer 1 — Threat is generated by abilities

Threat is produced by **ability activations**. Every ability carries an authored threat value; when it
fires, its owner gains that much threat. Nothing else generates threat.

This makes threat a **property of the kit you assembled**. A tank isn't someone who happens to get
hit a lot — it's someone who equipped the Essences that grant high-threat abilities, and wears gear
that amplifies them. Both are decisions the player actually makes, and both are visible on the
character sheet before combat starts.

#### 3.1.1 The schema addition

Add a threat value to the ability schema. Ability-level is the right granularity — per-effect would
be finer but nothing needs that resolution, and it would make the number harder to display:

```csharp
// AbilitySpec
public int? ThreatValue { get; init; }        // null → use the derived default (§3.1.3)
public float ThreatMultiplier { get; init; } = 1f;   // optional per-ability scaling hook
```

```json
{
  "id": "ability.essence.guardian.provocation",
  "kind": "Active",
  "name": "Provocation",
  "description": "Generate 400 Threat and gain Guard(3).",
  "cooldownTicks": 120,
  "threatValue": 400,
  "effects": [
    { "id": "…guard", "operation": "ApplyCondition", "target": "Self",
      "baseValue": 3, "condition": "Guard" }
  ]
}
```

Note what that ability *does*: it generates threat and grants Guard. The threat is not a side effect
of damage — it **is** the effect. That is a tank ability, and it did not previously exist as an
expressible thing.

`threatValue` is also authorable as a **negative** number, which gives the design threat-shedding for
free: a rogue-flavoured ability with `"threatValue": -250` is a genuine "drop aggro" tool, and it
composes with the existing `Stealth` hard floor.

#### 3.1.2 Generation on activation, plus triggers

Threat is granted at the moment of activation:

- **Active abilities** — on activation, once, regardless of how many targets or effects resolve.
- **Passive abilities with triggers** — on each trigger firing, subject to that trigger's existing
  `InternalCooldownTicks` / `EveryNthOccurrence` gating. This is legitimate and expressive: a passive
  reading `OnDamaged → +25 Threat` is still an *authored value on an ability the player chose to
  equip*. What it must never be is `threat += damageTaken × coefficient`.
- **Basic attacks** — a flat `BasicAttackThreatValue` (config, suggested **8**) per swing, so a build
  with a thin ability set still generates a baseline. Without this, threat becomes purely a function
  of how many abilities you happen to have unlocked.

The distinction that matters, stated once and enforced everywhere: **the threat number is authored on
the ability. It is never derived from a realized combat outcome.** A trigger may *gate* threat; a
damage figure may never *scale* it.

#### 3.1.3 Default values, so 168 abilities don't need hand-authoring

Requiring an explicit `threatValue` on all 168 existing abilities is a bad trade. Derive a default
from the ability's authored shape, and let `threatValue` override it for deliberate outliers only.

**The derivation is a function-band model.** Threat is assigned by *what category of thing the ability
does* — read from `effects[].operation`, `effects[].target` and `effects[].condition`, which are
enum-backed and consistently populated. Author a target **threat-per-second** per band, and multiply
by the ability's cooldown to get the stored per-activation value:

```
threatValue = BandTps × (cooldownTicks / TicksPerSecond)
```

| Band | TPS |
|---|---|
| Protective — self (`Guard`, `Ward`, `Renewal`, negative `ModifyDamageTaken`, defensive `ModifyAttribute` on Self) | **5.0** |
| Protective — ally (`GrantCover`, `GrantBarrier` to an ally, ally damage reduction) | **5.0** |
| Retaliation (`Thorns`, reactive damage-on-being-hit) | **3.5** |
| Support — ally (`Heal` to an ally, healing/regen amplification, ally buffs) | **3.5** |
| Control — hard (`Stun`, `Freeze` on enemies) | **2.5** |
| Control — soft / debuff (`Slow`, `Weaken`, `Vulnerable`, `Chill`, `Corrosion`, `Wound`, `Decay`, `Doom`) | **2.0** |
| Damage | **1.5** |
| Sustain — self (`Heal`/`GrantBarrier` on Self) | **1.5** |
| Utility | **0.5** |

Three properties make this the right shape:

1. **Protective and controlling actions generate the most threat; damage the least.** The fiction is
   "the boss attacks what obstructs it" — and mechanically this is what lets a defensive *build* tank
   without needing one bespoke high-threat ability. An earlier draft of this table gave self-buffs and
   defensive conditions **zero**, which would have forced every tank onto the only two essences that
   author a large `threatValue`. That was a mistake; this table replaces it.
2. **Normalising by cooldown removes the "shortest cooldown wins" attractor.** Flat per-activation
   threat would make `lumo_wisp.soothing_glow` (80 ticks, the shortest in the defensive set, against a
   median of 130) roughly twice the threat source of an equivalent 160-tick ability, and every tank
   would slot it.
3. **It reads authored magnitudes, not realized damage**, so it stays a pure function of the loadout
   and remains predictable and displayable (§7).

> **Implementation prerequisite.** Magnitude for `Damage` / `Heal` / `GrantBarrier` lives in
> `scalingCoefficient`, **not** `baseValue` — measured across `abilities.json`, `baseValue` is `0` for
> all 12 Heal effects, all 6 GrantBarrier effects, and 82 of 83 Damage effects. Any derivation reading
> `baseValue` for those operations returns zero for ~160 of 168 abilities. Read `baseValue` only for
> `ApplyCondition`, `GrantCover` and the `Modify*` family.

**Full authoring guidance — per-band signals, the 37 tank-capable essences, passive/trigger handling,
diversity mechanisms, and re-anchoring the four already-authored values — is in
`docs/threat-ability-authoring.md`.**

#### 3.1.4 Decay, and why threat is a rolling value

Threat decays exponentially toward base each tick:

```
On activation:  threat_i += ThreatValue(ability) × ThreatMultiplier_i
Each tick:      threat_i = Base + (threat_i - Base) × (1 - DecayPerTick)

Base            = 100
ThreatHalfLife  = 15 s   →  DecayPerTick = 1 - 0.5^(1/150) ≈ 0.00461
```

Decay rather than a plain running total, for three concrete reasons:

1. **Sustained threat converges to a stable equilibrium** proportional to threat-per-second:
   `threat_eq = Base + TPS / λ`, where `λ = ln2 / halfLife ≈ 0.0462/s`. So the *TPS ratio between
   builds* directly determines the aggro split — which is exactly the legible quantity we want.
2. **Burst-threat abilities keep mattering.** Under a plain cumulative total, a +400 spike is huge at
   t=10s and irrelevant at t=300s. Under decay it is always worth the same.
3. **It bounds the number**, so it can be displayed without becoming a seven-digit score.

The cost is one multiply per combatant per tick — or, better, compute it lazily on read from
`lastUpdatedTick` so idle combatants cost nothing.

> **Simpler variant if decay proves annoying:** a plain cumulative total with no decay. The aggro
> ratio then converges to the TPS ratio over time anyway, and it's marginally cheaper. It loses
> property 2, which matters for any burst-threat tank kit, so decay is recommended.

#### 3.1.5 Threat is deliberately NOT a gear stat

**Recommendation: do not add a `Threat` attribute. Threat comes only from abilities.**

`ThreatMultiplier_i` stays in the formula as a hook (default `1.0`) so per-ability or per-Essence
scaling is possible later, but no equipment stat feeds it. Four reasons, in order of importance:

**1. Threat has a saturating payoff, which makes it a bad gear stat.** Every other defensive stat is
monotonically useful — +5% Armor is +5% useful whether you have 0 or 5000 Armor. Threat is not: once
you hold ~85% of your party's aggro, further threat does *nothing*, and below the threshold where you
hold aggro at all it does *everything*. A stat whose value curve is a step function is miserable to
gear for and miserable to balance.

Worse, the codebase already has to handle this problem. `CombatRatingCalculator` values capped
attributes "only to their useful cap" — but **threat's useful cap depends on the other four people in
your party**, which is unknowable at the moment gear is equipped or Combat Rating is computed. There
is no correct number to put in `EquipmentStatBudgetCatalog`.

**2. It dissolves the armour-class problem entirely.** If threat isn't on gear, it cannot be
concentrated on plate, and there is no mechanism by which tanking requires heavy armour. A dodge tank,
an armour tank and a barrier tank all generate *identical* threat from the same Essences. That is the
outcome you want, achieved by removing a system rather than by carefully balancing one.

**3. It keeps the model legible.** Threat-per-second becomes a function of exactly one input — the
abilities you equipped. That is a short, comprehensible list the UI can show without qualification
(§7). Adding gear multiplies the input set and reintroduces "why is my threat different today?"

**4. It keeps the stat budget from growing.** The budget already carries Power, MaxHealth, Armor,
Resistance, DodgeChance, BlockChance, DamageReduction, HealthRegeneration, StatusResistance and
CrowdControlResistance. Every added stat dilutes the others and adds a calibration axis to
`EquipmentStatBudgetCatalog`. Not adding one is a real win.

#### 3.1.5a Then where does the tank's gear progression live?

In **survivability** — and that is a complete, satisfying gear identity on its own.

Split the two questions cleanly:

| Question | Decided by |
|---|---|
| **Who does the boss hit?** | Abilities (threat) |
| **Do you survive being hit?** | Gear (mitigation, avoidance, health, regen) |

A tank's gear job is to survive the aggro their abilities attract. That framing makes every armour
class a legitimate tank, differentiated by *how* it survives rather than by *whether* it can hold
aggro:

| Tank flavour | Survives via | Natural gear |
|---|---|---|
| **Armour tank** | Flat mitigation, Block | Plate — high Armor, BlockChance |
| **Dodge tank** | Avoidance, high uptime of not-being-hit | Leather — DodgeChance, AttackSpeed |
| **Ward tank** | Barrier throughput, Resistance, regen | Cloth/mail — Resistance, HealthRegeneration, Spirit |

All three slot the same Guardian Essence and hold the same ~80% of party aggro. They differ entirely
in what happens next. This is strictly better than making them compete on a threat stat, because it
turns "which armour class tanks" from a balance problem into a **build-variety feature**.

> Note the dodge tank has a genuine, interesting weakness that falls out of the existing engine for
> free: `DodgeChance` caps at 40% and dodge only applies to melee/ranged direct damage, so a dodge
> tank is excellent against basic-attack bosses and poor against ability-heavy ones. That is real
> counterplay, authored by the encounter, with no new systems.

#### 3.1.5b If you want a threat progression axis anyway

There is a good one available that doesn't touch gear: **Essence level.**

```
ThreatMultiplier_i = 1 + (essenceLevel × ThreatPerEssenceLevel)
```

Threat then grows by investing in the Essence that grants the tank abilities — which is already a
progression system with existing sinks (Soul Dust, cores, catalysts). It keeps threat ability-sourced,
gives tanks something to chase, and cannot be gained by swapping armour class.

Use this **only if** playtesting shows tanks need a per-player threat dial. The §3.2 curve normalises
against the party median, so absolute threat scale is irrelevant to the aggro split — a party of
level-10s and a party of level-60s behave identically. The one case where it matters is *mixed-power*
parties, which is better handled by `levelRequirement` gating than by a stat.

> One real effect to be aware of regardless: Essence slots unlock at levels 10 / 30 / 50 (2 / 4 / 6
> slots). More slots means more abilities means more total threat-per-second, so a 2-slot character
> genuinely cannot out-threat a 6-slot one. That is a reasonable, existing progression axis — but it
> means a low-level tank in a high-level party will lose aggro, and content gating should prevent that
> combination rather than the threat system trying to compensate for it.

#### 3.1.6 Worked example

A reference five-person group, with threat-per-second derived purely from each loadout
(`Σ threatValue / cooldownSeconds`, plus basic attacks):

| Build | Threat sources | TPS | Equilibrium threat |
|---|---|---|---|
| **Tank** | Provocation 400/12s (33) + Cover 150/20s (7.5) + basics (2.7) | **43** | 100 + 43×21.65 = **1031** |

| DPS ×3 | damage abilities ≈ 7 + basics 2.7 | 10 | 100 + 217 = **317** |
| Healer | heal abilities ≈ 7.5 + basics 2.7 | 10 | 100 + 217 = **317** |

Median = 317, so the tank's ratio is `1031/317 = 3.25×`. Fed into the attention curve (§3.2) at
`E = 2.5`: weight `3.25^2.5 = 19.1` against four others at 1.0 →

**the tank takes 83% of incoming single-target attacks.**

And because threat ramps from base, aggro *establishes* over the opening seconds rather than being
instant — which is good drama and a genuine reason for the tank to have a strong opener:

| Time | Tank threat | DPS threat | Ratio | Tank's share |
|---|---|---|---|---|
| 0 s | 100 | 100 | 1.00× | 20% |
| 5 s | 292 | 145 | 2.01× | 59% |
| 15 s | 565 | 208 | 2.72× | 75% |
| 45 s | 934 | 292 | 3.20× | 82% |
| ∞ | 1031 | 317 | 3.25× | 83% |

Every number in that table is computable from the loadout before the fight starts. That is the
property outcome-based accrual could never have.

### 3.2 Layer 2 — The attention curve

Layer 1 gives threat a meaningful spread between builds. Layer 2 is what converts a modest threat
ratio into a decisive attention share — without it, a 3× threat tank still only draws 43% of a
five-person group's attacks (§2), and all the ability authoring in the world won't fix that.

Replace the linear weight with a normalised power curve:

```
median   = median(threat over living friendlies)     // median, not mean — robust to one outlier
ratio_i  = threat_i / max(1, median)
weight_i = clamp(ratio_i ^ AttentionExponent, MinWeight, MaxWeightRatio)

AttentionExponent = 2.5      // the master dial
MinWeight         = 0.05     // nobody is ever fully untargetable (Stealth overrides separately)
MaxWeightRatio    = 20       // nobody is ever a guaranteed target either
```

Then roulette exactly as today, on `weight_i` instead of raw threat.

**What that buys**, with all non-tanks at ratio 1.0. (Threat ratios here are the equilibrium ratios
produced by Layer 1 — a 2–3× ratio is what a dedicated tank kit generates, per §3.1.6.)

| Tank threat ratio | Weight (E=2.5) | Share vs 2 | vs 4 | vs 14 |
|---|---|---|---|---|
| 1.0× | 1.0 | 33% | 20% | 6.7% |
| 1.5× | 2.76 | 58% | 41% | 16% |
| 2.0× | 5.66 | 74% | 59% | 29% |
| 2.5× | 9.88 | 83% | 71% | 41% |
| 3.0× | 15.6 | 89% | 80% | 53% |
| 4.0× | 32.0 | 94% | 89% | 70% |

Compare the "vs 4" column to today's 33% ceiling. A tank at a **2–3× threat ratio** — which is what
one dedicated high-threat active in the loadout produces — holds **59–80%** of a five-person party's
incoming attacks.

**Because parties are capped at 5, the "vs 4" column is the only one that matters** (§1.8). The
"vs 14" column is retained only as a reference for what a single 15-body combat would require. That is a tank doing its job, while still letting one hit in four slip through so
healers and barriers stay relevant.

`AttentionExponent` should be **config, not a constant** — it is the single most valuable tuning dial
in the whole system. With the 5-per-combat cap, a single global value of ~2.5 serves every mode
(5-person parties, 5-person raid wings, 3v3 PvP), which is a considerable simplification over needing
per-content tuning.

### 3.3 Layer 3 — Cover

New effect operation and condition, because **threat can never solve AoE.** §1.2 established that
`AllEnemies`, `TwoEnemies` and `ThreeEnemies` selectors are threat-blind by construction — no threat
value, however large, changes who an `AllEnemies` ability hits. Against those, a tank holding 85% of
single-target aggro contributes nothing at all. Cover is also what makes tanking robust to bad luck:
layer 2 leaves roughly one hit in five landing on someone else, and Cover catches a share of it.

Threat makes tanking *legible*; Cover makes it *reliable*.

```
AbilityEffectOperation.GrantCover
  target:      an ally (or AllAllies)
  baseValue:   redirect percentage (e.g. 30)
  durationTicks / uses as normal
  → applies condition Cover { Percent, GuardianId, BudgetRemaining }
```

Resolution, inside `ApplyDamage` **after** the target's own mitigation and **before** barrier:

```
if target has Cover and guardian.IsAlive and guardian != target:
    redirected = min(damage × Cover.Percent/100, Cover.BudgetRemaining)
    damage    -= redirected
    Cover.BudgetRemaining -= redirected
    ApplyDamage(source → guardian, redirected, DamageDelivery.Redirected)
```

Rules that keep it from becoming a nightmare:

- **`DamageDelivery.Redirected`** — a new member alongside `Direct | Periodic | Reflected | Stored |
  Self`. No dodge, no block, no crit, does not trigger Thorns, and **cannot itself be re-covered**
  (hard loop guard: redirected damage never redirects again).
- **It does go through the guardian's own typed mitigation.** This is the entire point — the tank's
  Armor and Resistance apply to somebody else's hit. It is what makes a tank *valuable* rather than
  just *present*.
- **Budget cap**, e.g. `guardian.MaxHealth × 0.5` per Cover instance, so a tank cannot eat a whole
  wing's damage. This bound is what stops Cover from trivialising encounters.
- **Breaks on guardian death**, and optionally below a health threshold, which creates a genuine
  "the tank is going down" moment.
- **Attribution**: log redirected damage with the guardian as recipient and the original target as
  context. Note the existing `EntityStats` gotcha — barrier-absorbed damage is excluded from
  `DamageDone` because `Magnitude` is `healthBefore - target.Health`, so Cover needs its own explicit
  counters (`DamageRedirectedTo` / `DamageRedirectedAway`) rather than being inferred.
- **Threat**: redirected damage generates **no** threat. The `GrantCover` ability's own authored
  `threatValue` is the threat contribution — consistent with §3.1, and it keeps Cover's threat
  predictable instead of scaling with how much damage happened to land on allies.

Cover is also the primitive that makes a tank matter in the raid design's **Vanguard wing**, where a
Scourge's `AllEnemies` abilities would otherwise ignore tanking entirely.

---

## 4. The tank's toolkit

Layers 1–3 are the chassis. These are the authored abilities that let a build express tanking. All
are expressible with the existing effect vocabulary plus `GrantCover`.

### 4.1 Threat generation

All of these are authored `threatValue` on an ability, per §3.1 — the player equips them, and their
combined threat-per-second is what determines whether they tank.

| Tool | Shape |
|---|---|
| **Signature provocation** | Active, `threatValue: 400`, ~12 s cooldown, bundled with a defensive effect (Guard/barrier). The tank's core ability; ~33 TPS on its own |
| **Sustained presence** | Passive, `OnBasicAttack` trigger, `threatValue: 25` — low, constant, unconditional threat floor |
| **Threat on absorb** | Passive, `OnBarrierAbsorbed` / `OnDamaged` trigger with `InternalCooldownTicks`, `threatValue: 30` — "they resent what they can't kill." Trigger-gated, flat authored value, **not** scaled by damage |
| **Opener** | Active, `threatValue: 600`, long cooldown, `OnCombatStart`-friendly — compresses the aggro ramp in §3.1.6 so the tank establishes in ~5 s instead of ~15 s |
| **Threat shed** | Active, `threatValue: -250` — for non-tanks who pulled aggro they don't want; composes with `Stealth` |
| **Legacy hook** | `ModifyThreat { Self, +N }` still exists as an effect operation and remains useful for *conditional* threat inside an ability's effect list (e.g. only when below 50% health) |

### 4.2 Fix the Taunt/Mark confusion

Split the one overloaded condition into two, because the existing content clearly wants the *other* one:

- **`Taunt`** — applied to **self**, beneficial: hard forced-target magnet (§4.3).
- **`Mark`** (new) — applied to an **enemy**, harmful: raises that enemy's threat so your allies focus
  it. This is what `impish_flame` actually means today; migrate it to `Mark` and its behaviour is
  preserved rather than reinterpreted.

Also fix the classification: `Mark` belongs in `IsHarmfulCondition` (so Ward blocks it and Dispel
logic is coherent), whereas `Taunt`-on-self is beneficial and should arguably be **Dispel-exempt**
alongside Guard and Ward — otherwise a boss with a dispel deletes the tank role.

### 4.3 Hard taunt

The on-demand tool, and the one place a deterministic forced target is correct (§3):

```
Taunt (self, N seconds, long cooldown):
  While active, every hostile that selects a target from this combatant's team
  MUST select the taunter, unless the taunter has become invalid (dead, Stealth).
  Multiple simultaneous taunters → roulette among taunters only.
```

Short (3–5 s) and on a real cooldown (30–45 s) so it is a *moment*, not a permanent state. Duration
authored as `baseValue` seconds exactly as Taunt already works — so this is a semantics change to
`GetEffectiveThreat`/selection, not a schema change.

Because this bypasses the roulette entirely, implement it as a **pre-filter** on the candidate list
rather than as an enormous threat number. That keeps the weight curve's tuning intact and makes
"taunt is absolute" true rather than approximately true.

### 4.4 Protection

| Tool | Shape |
|---|---|
| **Cover** | `GrantCover { AllAllies, 25%, 15s }` on a cooldown — the signature tank active |
| **Focused cover** | `GrantCover { LowestHealthAlly, 50%, 8s }` — reactive, higher percentage, single target |
| **Ally barrier** | Existing `GrantBarrier { AllAllies }` — already works, already in content |
| **Ally mitigation** | `ModifyDamageTaken { AllAllies, -10% }` — engine already supports ally targeting; **no content uses it**. Free win, zero engine work. |
| **Retaliation** | Existing `Thorns` — `bramble_shield` already grants it to an ally |

Note §4.4 row three and four: **two ally-protection tools already work and simply have no content.**
Authoring a few of those is the cheapest possible partial improvement, available before any engine
change lands.

---

## 5. Secondary fixes worth bundling

These are all real problems the audit surfaced. Bundling them is efficient because they touch the
same code and the same test baselines (§6).

### 5.1 Summon threat weight (highest impact)

Summons inherit `BaseThreat = 100`, so `niCopy`'s nine permanent bodies reduce the owner to 10% of
incoming attacks — an undesigned 90% damage reduction, currently the strongest defensive mechanic in
the game.

Add `ThreatMultiplier` to `SummonAttributeSpec` (or a `threatWeight` field on `SummonSpec`), default
**0.25**, authored per summon. Decoy summons can then be *deliberately* good at soaking (author 1.0
for a dedicated taunt-totem) while a swarm of nine damage copies stops accidentally out-tanking every
tank in the game.

**This is the single most impactful line item in this document for existing balance**, and it is
worth doing whether or not the rest ships.

### 5.2 Threat-aware multi-target selection

`FillFilteredTargets` gives `TwoEnemies` / `ThreeEnemies` the **first** 2 or 3 in list order — not
random, not threat-aware. A boss's multi-target ability therefore hits the same slots every fight.
Replace with threat-weighted sampling **without replacement**, so a tank soaks a proportionate share
of multi-target abilities too.

`AllEnemies` correctly stays threat-blind — that is what makes Cover necessary.

### 5.3 Delete or fix `SelectThreatWeightedEnemy`

Dead code with one grep hit (its own declaration), duplicating `SelectFirstEnemy`'s algorithm with
LINQ allocations. Either delete it or make it the single shared implementation and have
`SelectFirstEnemy` call it. Do not leave two divergent copies of the targeting rule.

### 5.4 Rename `SelectFirstEnemy`

It is a roulette wheel, not a first-match. The name will mislead the next person to touch aggro.
`SelectWeightedEnemy` or `SelectAttentionTarget`.

### 5.5 Clamp `_threat`

Clamp the backing field, not just the getter, so a negative `ModifyThreat` cannot silently
over-refund on expiry.

### 5.6 Unrelated but found while auditing: the Resistance curve is dead

```csharp
public static float CalculateDefenseMitigation(float defense, float penetrationPercent = 0)
{
    var defenseRating = EquipmentStatBudgetCatalog.ConvertEffectiveValueToNormalizedRating(
        AttributeType.Armor, defense);          // ← hardcoded Armor
    …
    var effectiveDefensePercent = EquipmentStatBudgetCatalog
        .ConvertNormalizedRatingToEffectiveValue(AttributeType.Armor, netDefenseRating);  // ← again
```

`AttributeType.Armor` is hardcoded in both conversions, so **Resistance is evaluated on Armor's
curve** (`halfCap 55`) and its own authored `halfCap 80` is never used. Every magic-mitigation number
in the game is therefore off-spec. Tanks care about this more than anyone. It is a one-line fix with
wide balance consequences — worth its own ticket and its own recalibration pass, not a quiet
bundling.

---

## 6. Determinism, migration and cost

This is the part that determines whether the work is a week or a month.

### 6.1 The `_random` constraint

`_random` is **one shared, order-sensitive stream**, seeded from `RandomSeed`. It serves target
roulette, dodge, block, crit, effect `ChancePercent`, and the Freeze/Stun resist roll. Only
`_magnitudeRandom` is separate (±20% variance, salted with `0x9E3779B9`).

Any change to the *number* or *order* of draws — adding a taunt pre-filter, switching to argmax
(which consumes zero draws), sampling without replacement — **shifts every downstream roll and
diverges every replay and every balance baseline.** Pinned to that stream:

- `AbilityBalanceSimulator`
- the `expectedLogs` assertions across **168 `ability-behaviors.json` fixtures**
- `DungeonPowerRecommendationCacheEntries` recommended-power values
- any stored Tower playback artifacts

### 6.2 Therefore: land targeting changes as ONE change

The mitigation is scheduling, not cleverness. **Every item in §3 and §5.2–6.5 should ship in a single
change** so the 168 fixtures and all power baselines are regenerated exactly once. Four separate
targeting PRs mean four full baseline regenerations and four windows where balance data is unreliable.

Additionally:

- **Add a third `Random` for targeting**, salted off `RandomSeed` like `_magnitudeRandom`. This keeps
  `_random`'s sequence byte-identical for fights where the selected target happens to be unchanged,
  which dramatically reduces fixture churn. It does not eliminate it — a changed target still
  perturbs later damage rolls indirectly — but it converts "everything diverges" into "only fights
  where aggro actually changed diverge."
- **Bump `CombatRulesVersion` 11 → 12.** That already invalidates cached power recommendations by
  design, which is exactly what's wanted.
- **Put every constant behind config**: `AttentionExponent`, `ThreatHalfLife`,
  `BasicAttackThreatValue`, the §3.1.3 default-derivation coefficients, `MinWeight`,
  `MaxWeightRatio`, taunt duration/cooldown defaults, Cover budget fraction, default summon
  `ThreatMultiplier`. Threat is going to need a dozen tuning passes; each one must not be a deploy.
  Individual ability `threatValue`s live in `abilities.json` and are already hot-authorable.
- **Feature-flag the whole thing** so the old linear behaviour is one switch away during rollout.

### 6.3 Performance

`SelectFirstEnemy` is deliberately allocation-free (two index loops, no LINQ) and runs once per basic
attack per combatant. `GetEffectiveThreat` already walks `Conditions` twice per candidate per pass
(`HasCondition` is a linear scan) — that's four scans per candidate per selection. Adding a median
computation and a `Math.Pow` per candidate per selection is affordable, but:

- Cache the median per selection pass, not per candidate.
- Consider caching `weight` on `RuntimeCombatant`, invalidated on threat change or condition change,
  rather than recomputing `Math.Pow` in the hot loop.
- Compute threat decay **lazily on read** from a `lastThreatUpdateTick`, not eagerly per combatant per
  tick — decay is a closed-form `× (1-d)^Δticks`, so idle combatants cost nothing.
- If `HasCondition` scans become the bottleneck, an indexed condition bitmask is the fix — not more
  list walks.

Tower already resolves 15-vs-1 at 6000 ticks and records `EngineDurationMilliseconds` /
`EngineAllocatedBytes`, so there is existing telemetry to check this against.

---

## 7. Legibility — non-negotiable in an auto-battler

The player never presses a button during combat. If they cannot *see* that tanking is working, it
does not matter that it is.

Ability-generated threat unlocks the thing outcome-based accrual could not: **threat per second is a
pure function of the loadout, so it can be computed and displayed without simulating anything.**

1. **Show Threat per second on the character sheet**, next to Armor and Resistance. It is
   `Σ (threatValue_i × ThreatMultiplier / cooldownSeconds_i) + basic-attack contribution` — arithmetic
   over the equipped kit, no simulation needed. This is the headline number and the whole reason to
   prefer this model.
2. **Show `threatValue` on every ability tooltip**, the way damage and cooldown already appear.
   "Generates 400 Threat" makes a tank ability legible as a tank ability at the moment the player is
   deciding whether to equip it.
3. **Show the attention split in combat results.** A per-participant "share of attacks taken" figure
   is derivable from the existing event log and is the clearest proof that a tank worked.
4. **Show redirected damage.** `DamageRedirectedTo` / `DamageRedirectedAway` in `EntityStats` gives a
   tank a number they can be proud of, which is the emotional payoff the role currently lacks entirely.
5. **Predict the aggro split in the raid Battle Plan preview** (`docs/raid-system-design.md` §5).
   Because equilibrium threat follows from TPS, the projected attention share per wing member can be
   shown **as arithmetic, before any simulation runs** — turning "who should tank the Vanguard" from
   guesswork into a readable number.

Without #3 and #4, a player has no way to distinguish "my tank held aggro" from "I got lucky," and
the role will feel broken even after it works.

---

## 8. Suggested sequencing

**Phase 0 — free wins, no engine change.**
Author the ally-protection abilities that already work: `ModifyDamageTaken { AllAllies, -X% }`,
more `GrantBarrier { AllAllies }`, ally `Thorns`. Author the first self-`ModifyThreat` passive using
the existing effect operation. Set `TauntThreatBonus` explicitly in `CombatEngineExecutor` instead of
relying on the default. This is content-only and immediately makes tanky builds contribute something.

**Phase 1 — the chassis (one change, one baseline regeneration).**
`threatValue` on `AbilitySpec` + activation/trigger generation + default derivation + decay (§3.1);
attention curve (§3.2); hard taunt (§4.3); `Mark`/`Taunt` split (§4.2); summon `ThreatMultiplier`
(§5.1); threat-aware multi-target (§5.2); dead-code and naming cleanup (§5.3–6.5); third `Random`;
`CombatRulesVersion` → 12; config + feature flag.

**No `AttributeType` change, no `EquipmentStatBudgetCatalog` change, no Combat Rating change** — a
meaningful reduction in blast radius compared with adding a threat stat (§3.1.5).

**Phase 1.5 — the tank kit.**
Author the abilities in §4.1 onto one or two Essences, so there is a build that can actually tank.
The chassis without the kit produces no observable change — these should ship close together.

**Phase 2 — Cover.**
`GrantCover` operation, `Cover` condition, `DamageDelivery.Redirected`, budget and loop guards,
`EntityStats` counters, tank Cover abilities.

**Phase 3 — legibility and tuning.**
Threat on the character sheet, attention split and redirected damage in results, Battle Plan
integration, then the actual tuning passes on `AttentionExponent` per content type.

**Separate ticket, do not bundle:** the Resistance curve fix (§5.6) and its recalibration.

---

## 9. Open questions

1. ~~Does Threat belong in Combat Rating?~~ **Resolved:** there is no Threat attribute (§3.1.5), so
   Combat Rating, `EquipmentStatBudgetCatalog` and `AttributeType` are all untouched.
2. ~~Can `AttributeType` slots 17/18 be reused?~~ **Resolved / no longer needed** (§3.1.5).
3. **What is the intended tank share?** This document assumes ~75–85% on a five-person party as the
   target. 60% keeps healers busier; 95% makes the tank a hard requirement. This single number should
   be decided first, because `AttentionExponent` is derived from it.
4. ~~How should 10- and 15-slot Tower floors work?~~ **Resolved by the 5-per-party cap** (§1.8) — a
   15-slot floor is three 5-person combats, so one exponent serves every mode. **Still needs
   confirming:** that a 15-slot floor really does resolve as three separate combats rather than one
   15-friendly combat.
5. **Should hard taunt be absolute or a large weight?** Absolute (pre-filter) is cleaner and makes the
   tool feel real; a large weight is less disruptive to the `_random` stream. Recommendation:
   absolute, since the stream is being regenerated anyway in Phase 1.
6. **Does Cover stack from multiple guardians?** Simplest: no — highest percentage wins, one Cover per
   target. Additive stacking invites a two-tank comp that redirects ~100% of everything.
   Note this becomes more relevant with 3-party content, where each party has its own tank but a raid
   Scourge's `AllEnemies` abilities may cross wings — confirm whether Cover can ever apply across
   parties (recommendation: no, Cover is party-scoped).
7. **Should threat be visible mid-fight in playback?** The Tower already streams combat frames
   (`WorldTowerCombatFrameUpdated`). Showing a live threat bar would be genuinely satisfying, but it
   means adding threat to the frame payload and the checkpoint schema — currently threat appears in
   neither `CombatCheckpoint` nor `SimpleCombatEntity`.
8. **Decay or plain cumulative?** §3.1.4 recommends decay (half-life 15 s) because it keeps burst-threat
   abilities relevant and bounds the displayed number. Plain cumulative is slightly cheaper and
   converges to the same TPS ratio. Decide before authoring any burst-threat tank ability, since the
   answer changes whether such an ability is worth designing.
9. **Should `threatValue` scale with anything?** It is a flat authored integer in this proposal, and
   since §3.2 normalises against the party median, absolute scale is irrelevant to the aggro split.
   §3.1.5b offers Essence level as the progression axis **if** a per-player threat dial proves
   necessary. Default position: don't, until playtesting says otherwise.
10. **How do the 168 derived defaults land in practice?** The §3.1.3 band TPS values are a first guess.
    Before Phase 1 ships, run the derivation over `abilities.json` and review the per-essence TPS
    report — the goal is that no current build accidentally out-threats a committed tank loadout. See
    `docs/threat-ability-authoring.md` §6.4 and §8.
11. **How many essences can a player equip?** `IncreaseEssenceSlotAction.Execute` and
    `IncreaseEssenceReserveSlotAction.Execute` are both `Task.CompletedTask` — slot growth is not
    implemented. **This is the largest single input to whether threat produces build diversity**: at
    3–4 slots variety is guaranteed; at 8+ every tank slots every high-threat ability regardless of how
    the bands are authored. The 2 / 4 / 6 progression at levels 10 / 30 / 50 referenced elsewhere is the
    right shape. Confirm and implement before tuning bands.
12. **Should Taunt stay on one essence?** It is currently on exactly one (`essence.transparent_slime`).
    If a hard taunt is what separates tanking from not tanking, that essence becomes mandatory for every
    tank in the game — the exact homogenisation this design is trying to avoid. Recommendation: 4–5
    taunt carriers with different secondary payloads (authoring companion §5).

---

## Appendix — Current-state reference card

| Question | Answer (verified) |
|---|---|
| Threat storage | Single `float` per combatant, `BaseThreat = 100f` |
| Threat table (per-attacker) | Does not exist |
| Threat accrual from actions | **None** |
| Threat decay | None |
| Threat writes in engine | 2 (`ModifyThreat` handler, timed revert) |
| Threat reads in engine | 1 (`GetEffectiveThreat`) |
| Targeting algorithm | Proportional roulette (`SelectFirstEnemy`), re-rolled per basic attack |
| Target stickiness | None across attacks; within one ability activation only |
| `TauntThreatBonus` | Default `100f`, **never set by any caller** → Taunt always doubles weight |
| Taunt semantics | Threat bonus only. **No forced targeting.** Unique, no stacks, dispellable, not blocked by Ward |
| Abilities applying Taunt to self | **0** |
| Abilities applying Taunt to enemies | 2 |
| Abilities using `ModifyThreat` | **1** of 168 (a hostile creature, +50 self, permanent) |
| Stealth | Hard floor: effective threat = 1 |
| Damage redirection / intercept / cover | **Does not exist** |
| Ally-targetable protection that works today | `GrantBarrier`, `Thorns`, `ModifyDamageTaken` (last one has no content) |
| `Threat` attribute in catalog | **Does not exist** — and this design deliberately keeps it that way (§3.1.5) |
| Summon threat | Inherits `BaseThreat = 100`; `niCopy` ×9 → owner takes 10% of attacks |
| Multi-target selectors | `TwoEnemies`/`ThreeEnemies` take first-in-list-order; `*Random*` uniform; all threat-blind |
| Dead code | `SelectThreatWeightedEnemy` (never called) |
| Randomness | Single order-sensitive `_random`; `_magnitudeRandom` separate for ±20% variance |
| Baselines pinned to draw order | `AbilityBalanceSimulator`, 168 `ability-behaviors.json` fixtures, power recommendations |
