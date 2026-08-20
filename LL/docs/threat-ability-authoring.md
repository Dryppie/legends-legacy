# Threat — Ability Authoring Guide

**Status:** Design proposal (content-authoring companion)
**Author:** Design pass, 2026-08-18
**Companion to:** `docs/threat-and-tanking-design.md` (the systems design)
**Question this answers:** which abilities generate threat, and how to do it without every tank
converging on the same 2–3 abilities.

---

## 0. The answer in one paragraph

**Do not author threat per ability. Author it per _function band_, and derive the value from
`operation` + `target` + `condition`.** A band is a category of thing an ability does — "shields
itself," "protects an ally," "locks an enemy down," "deals damage" — and every ability in a band
generates comparable threat _per second_. Then a tank is not someone who slotted Provocation; a tank
is anyone whose loadout is **made of protective and controlling abilities**, whichever ones those
happen to be. Because **37 of 63 essences already own at least one protective, defensive, retaliatory
or control ability**, that single decision converts a 2-essence bottleneck into 37 viable starting
points.

The inversion that makes it work: **defensive and protective actions generate the _most_ threat, and
damage generates the _least_.** The current draft in the systems doc has this backwards.

---

## 1. Correcting the systems doc

`threat-and-tanking-design.md` §3.1.3 currently proposes:

| Ability composition                                             | Derived default              |
| --------------------------------------------------------------- | ---------------------------- |
| Damage operations                                               | `2.0 × Σ authored baseValue` |
| `Heal` / `GrantBarrier`                                         | `2.4 × Σ authored baseValue` |
| Harmful `ApplyStatus` / `ApplyCondition` / control              | flat `40`                    |
| **Self-buffs, `ModifyAttribute` on Self, defensive conditions** | **`0`**                      |
| Everything else                                                 | flat `20`                    |

That table is the exact cause of the problem being asked about. Two fatal properties:

1. **Defensive abilities generate zero threat.** A build made entirely of Guard, Ward, Thorns and
   mitigation passives generates _nothing_, so tanking becomes impossible without slotting a bespoke
   high-threat ability. Only two essences currently carry one (`transparent_slime` at 400,
   `hobgoblin` at 600) — so all tanks would be forced onto those two. Homogenisation by construction.
2. **Damage generates the most threat** (via the `2.0 ×` coefficient), which means the best tank is a
   damage build. That's backwards from the intended fantasy and from every other design decision in
   the doc.

There is also an implementation bug that makes the table inert regardless (§6.1): it reads
`effect.BaseValue`, which is **0 for essentially every Damage / Heal / GrantBarrier effect in the
game** — magnitude lives in `scalingCoefficient`.

Replace it with §3 below.

---

## 2. Why bands, and not tags

The obvious mechanism would be `AbilitySpec.Tags` — but the tag vocabulary is not fit for this:

- **58 distinct tags**, in one flat namespace, mixing damage type (`Physical`, `Magical`), delivery
  (`Melee`, `Ranged`, `Area`), function (`Defensive`, `Barrier`, `Control`, `Healing`) and one-off
  flavour (`Cinder`, `Pillar`, `NinefoldCopy`, `Spore`, `Seal`).
- **`Defensive` (22 uses) misses obvious defensive abilities.** `blue_slime.protective_slime` — an
  AoE ally barrier — is tagged `Barrier`/`Area` with no `Defensive`. `wood_nymph.bramble_shield` is
  `Barrier`/`Buff`/`Retaliation`, no `Defensive`. `gnoll_shaman.totemic_ward` is
  `Summon`/`Barrier`/`Support`.
- **`Passive` appears as a tag on 17 abilities** despite `kind: "Passive"` existing on 73 — applied to
  23% of passives. Unreliable.
- **Two competing vocabularies are in flight.** `statuses.json` uses a dotted namespace
  (`Status.Buff`, `Defense.Armor`, `Pattern.HealthThreshold`, `Element.Fire`), and two of those have
  leaked onto abilities via `garran.the_first_gate`.
- `deliveryTags` and `effectTags` are declared and **never populated**.

By contrast, `effects[].operation`, `effects[].target` and `effects[].condition` are **enum-backed,
consistently populated, and validated at startup**. Derive from those. Keep `tags` as a
human-readable hint and an optional override hook only.

> If tags are ever cleaned up into a single namespaced vocabulary, revisit — a `Function.Protective`
> tag would be a cleaner key than inferring from operations. But do not block threat on that cleanup.

---

## 3. The band model

### 3.1 Author a target TPS, derive the per-activation value

The unit that matters is **threat per second**, not threat per activation. Author the band's TPS
target; multiply by the ability's cooldown to get the stored value:

```
threatValue = BandTps × (cooldownTicks / TicksPerSecond)
```

**This normalisation is essential and is the second mechanism against homogenisation.** Without it,
short-cooldown abilities are strictly better threat sources and everyone converges on them. Concretely:
`lumo_wisp.soothing_glow` has the shortest cooldown in the defensive set at **80 ticks**, against a
median of 130 and a max of 450. Flat per-activation threat would make it roughly **2× the threat
source** of an equivalent 160-tick ability, and every tank would slot it. Normalising by cooldown makes
an 8-second heal and a 17-second heal identical in TPS, so the choice between them is about _texture_,
not power.

For **passives** (`cooldownTicks` is 0 for all 73 of them), derive the effective period from the
trigger instead:

| Passive shape                                                  | Effective period                                                                                                   |
| -------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `triggers[].internalCooldownTicks` set                         | use that value                                                                                                     |
| `OnInterval` with an interval                                  | use the interval                                                                                                   |
| `everyNthOccurrence: N` on an attack-ish trigger               | `N × BasicAttackIntervalTicks`                                                                                     |
| `OnCombatStart` one-shot, or `uses: 1`                         | flat `BandTps × 20` (a 20-second nominal amortisation)                                                             |
| unbounded reactive trigger (`OnDamaged`, `OnHit`) with no gate | flat `BandTps × 4`, and **add an `internalCooldownTicks`** — an ungated reactive threat source is a balance hazard |

### 3.2 The bands

Calibrated so that a fully-committed tank lands near **30–35 TPS** and a pure damage build near
**9–12 TPS**, which is the 2.5–3× ratio the attention curve needs for ~75–83% aggro
(`threat-and-tanking-design.md` §3.1.6, §3.2).

| Band                        | Signal (operation / target / condition)                                                                                                                                                                                                                                                                                           | TPS                    |
| --------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------- |
| **Protective — self**       | `ApplyCondition` on Self with `Guard`, `Ward`, `Renewal`, `Recovery`, `Unstoppable`; `ModifyDamageTaken` negative on Self; `ModifyDamageTakenFromCondition` negative; defensive `ModifyAttribute`/`ModifyAttributePercentOfInitial` on Self (`Armor`, `Resistance`, `MaxHealth`, `DamageReduction`, `BlockChance`, `DodgeChance`) | **5.0**                |
| **Protective — ally**       | `GrantCover` any target; `GrantBarrier` to an ally selector; `ModifyDamageTaken` negative on an ally selector                                                                                                                                                                                                                     | **5.0**                |
| **Retaliation**             | `ApplyCondition` with `Thorns`; reactive damage-on-being-hit patterns (`OnDamaged`/`OnAttacked` → `Damage`)                                                                                                                                                                                                                       | **3.5**                |
| **Support — ally**          | `Heal` to an ally selector; `ModifyHealingReceived` positive on allies; `ModifyRegenerationRate` positive on allies; beneficial `ApplyCondition` on allies (`Empower`, `Haste`)                                                                                                                                                   | **3.5**                |
| **Control — hard**          | `ApplyCondition` with `Stun` or `Freeze` on an enemy                                                                                                                                                                                                                                                                              | **2.5**                |
| **Control — soft / debuff** | `ApplyCondition` with `Slow`, `Weaken`, `Vulnerable`, `Chill`, `Corrosion`, `Wound`, `Decay`, `Doom`; `ModifyHealingReceived`/`ModifyRegenerationRate` negative on enemies; `ModifyDamageDealt` negative on enemies                                                                                                               | **2.0**                |
| **Damage**                  | `Damage` to any enemy selector                                                                                                                                                                                                                                                                                                    | **1.5**                |
| **Sustain — self**          | `Heal` to Self; `GrantBarrier` to Self; positive `ModifyRegenerationRate` on Self                                                                                                                                                                                                                                                 | **1.5**                |
| **Utility**                 | anything else — `Cleanse`, `Dispel`, `RemoveStatus`, `RestoreResource`, `Summon`, movement/flavour                                                                                                                                                                                                                                | **0.5**                |
| **Threat-negative**         | `Stealth` on Self; explicit authored negatives                                                                                                                                                                                                                                                                                    | **authored, negative** |

**Summing rule.** An ability's threat is the sum over its **distinct bands**, not over its effects —
otherwise a three-effect ability in one band triples its threat for free. `transparent_engulf` hits
_Protective—self_ (Guard) and _Protective—ally_ (Cover) → `(5.0 + 5.0) × 12s = 120`.

**Magnitude sensitivity.** Within a band, scale modestly by magnitude so a big shield out-threatens a
token one — but keep the exponent well under 1 so magnitude cannot dominate the band:

```
bandValue = BandTps × cooldownSeconds × clamp(magnitudeRatio, 0.7, 1.4)
magnitudeRatio = (thisAbilityMagnitude / bandMedianMagnitude) ^ 0.35
```

Use `scalingCoefficient` (× the scaling attribute) for Damage/Heal/GrantBarrier — **never
`baseValue`**, which is 0 for those (§6.1). Use `baseValue` for `ApplyCondition`, `GrantCover`,
`ModifyDamageTaken` and the `Modify*` family, where it _is_ the magnitude.

### 3.3 Deliberate zeroes

| Case                       | Threat                    | Why                                                                                                                                                                      |
| -------------------------- | ------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Mark` applied to an enemy | **0**                     | Mark raises _that enemy's_ threat so allies focus it. It must not also generate self-threat, or it becomes a stealth tank tool and the Mark/Taunt distinction collapses. |
| `Taunt` applied to Self    | **0 from the band model** | Taunt is a hard forced-target (systems doc §4.3). Giving it threat _as well_ double-counts. Let the ability's other effects carry its threat.                            |
| `Summon` operation         | **0**                     | Summons carry their own threat via `DefaultSummonThreatMultiplier = 0.25`. Charging the summoner too would double-count.                                                 |
| Self-damage effects        | **0**                     | `giant_spider.spider_crash` and similar shouldn't read as protective.                                                                                                    |
| `NonCombat`-tagged         | **0**                     | —                                                                                                                                                                        |

---

## 4. The diversity mechanisms

Six independent reasons the band model produces variety rather than a single optimal tank.

### 4.1 Breadth: 37 of 63 essences qualify

Every essence owning at least one ability in the Protective, Retaliation, Support or Control bands is
a viable tank component. That is **37 essences**, not 2. Grouped by what they contribute:

**Full tank identities (10)** — `transparent_slime` (Guard + Taunt + AoE Cover), `hobgoblin` and
`hobgoblin_brutal_charge` (threat passive + Vulnerable mitigation), `brown_slime` (Guard + stacking
Armor), `wood_nymph` (ally Thorns + Cover + barrier; Renewal + Ward), `skeleton` (Guard at combat
start), `thornback_boar` (Thorns 30s), `hollow_stag` (DamageReduction per HP lost), `giant_spider`
(+15% Physical Armor), `large_rat` (+10% MaxHealth).

**Protector / support (7)** — `blue_slime` (AoE barrier from _your_ MaxHealth + AoE heal),
`lumo_wisp` (ally heal, 80-tick cooldown + self barrier), `goblin_shaman` (channel heal + AoE
Recovery), `forest_spirit` (ally heal + heal amplification), `rainbow_slime` (barrier on every ability
use), `gnoll_shaman` (barrier totem), `treant_sapling` (largest self-heal + healing received).

**Mitigation / sustain passives (10)** — `blood_zombie`, `lumo_sentinel`, `smolder_rat`,
`poisonous_rat`, `vampire_bat`, `shadow_imp`, `crystal_wisp`, `moss_lizard` (also the threat-shed),
`rotfly_toad`, `ravenous_ghoul`.

**Retaliation / control (10)** — `cinder_beetle`, `green_slime`, `undead`, `illusion_fox`, `goblin`,
`enchanted_fairy` (the only AoE hard CC), `giant_worm`, `feral_ghoul`, `grave_hound`, `raven`
(plus `lost_soul` and `gnoll_pack_leader` as borderline).

**Breadth is fine; depth is not.** Only **2 essences apply Guard**, **1 applies Ward**, **1 applies
Taunt**, **4 grant ally barrier**, **4 hard-CC**. This is precisely why threat must key off the _band_
and never off a specific condition — a rule like "grants Guard → high threat" collapses 37 essences
back to 2. §5 addresses the depth problem separately.

### 4.2 Cooldown normalisation

Covered in §3.1. Removes the "shortest cooldown wins" attractor that would otherwise make
`lumo_wisp` mandatory.

### 4.3 Threat saturates, so "enough" beats "maximum"

This is the structural reason variety survives, and it's the same property that argued against making
threat a gear stat (systems doc §3.1.5). Once a tank holds ~85% of party aggro, **more threat does
nothing.** So a tank needs to clear a bar, not maximise a number — and many loadout combinations clear
the same bar.

Set the bar deliberately low: at `AttentionExponent = 2.5` against 4 allies, **~2.5× median threat
buys 71%** and 3× buys 80%. A build needs roughly _three_ protective-band abilities to get there, out
of six essence slots. **The remaining three slots are free**, which is where build identity lives:
extra mitigation, a heal, a debuff, or straight damage. That headroom is the design.

### 4.4 Four shapes of threat, equal in power

Because TPS is normalised, these are balanced against each other and differ only in _texture_ —
which interacts with the aggro ramp (systems doc §3.1.6):

| Shape          | Example                                                                                      | Feel                                                       |
| -------------- | -------------------------------------------------------------------------------------------- | ---------------------------------------------------------- |
| **Front-load** | `OnCombatStart` passive (`skeleton.calcium`, `thornback_boar.bristling_hide`)                | Instant aggro, no ramp — best in short fights              |
| **Sustained**  | short-cooldown active (`lumo_wisp` 80t)                                                      | Smooth, predictable, recovers fast from a threat-shed      |
| **Burst**      | long-cooldown active (`gnoll_shaman.totemic_ward` 180t)                                      | Spiky; aggro wobbles between casts                         |
| **Reactive**   | `OnDamaged` / `OnAttacked` passive (`cinder_beetle.molten_shell`, `brown_slime.layered_mud`) | Self-reinforcing once the boss commits; weak on the opener |

A player choosing front-load vs reactive is making a real decision with no correct answer. That's
diversity generated for free by the normalisation.

### 4.5 The derived default is the norm; authoring is the exception

If the derivation is right, **all 37 essences get sensible threat with zero hand-authoring**, and no
essence is "the threat essence." Reserve explicit `threatValue` for deliberate outliers only:

- The signature hard-taunt abilities.
- The threat-shed tools (`moss_lizard.moss_camouflage` at `-250`).
- Boss abilities where threat needs to be wrong on purpose.

**Target: fewer than 15 of 168 abilities carry an authored `threatValue`.** If that number climbs,
the bands are miscalibrated — fix the bands rather than authoring around them.

### 4.6 Re-anchor the four existing authored values

The four abilities that already author threat are **3–5× above** what the band model derives, and were
set before the bands existed:

| Ability                                | Authored | Band-derived | Bands hit                                                                        |
| -------------------------------------- | -------- | ------------ | -------------------------------------------------------------------------------- |
| `hobgoblin.threatening_presence`       | 600      | ~100         | Protective—self (`ModifyDamageTakenFromCondition −10`), `OnCombatStart` one-shot |
| `transparent_slime.transparent_engulf` | 400      | ~120         | Protective—self (Guard) + Protective—ally (Cover), 120t                          |
| `wood_nymph.bramble_shield`            | 150      | ~80          | Protective—ally (Cover + barrier) + Retaliation (Thorns), 160t                   |
| `moss_lizard.moss_camouflage`          | −250     | authored     | Threat-negative — keep as authored                                               |

Note the gap in the _other_ direction too: `AbilityThreatTuning` currently defaults
`HarmfulControlThreat = 40`, `OtherThreat = 20`, `BasicAttackThreatValue = 8`. Against authored values
of 400–600 that is a **10–50× spread with nothing in between** — the derived defaults are calibrated to
nothing. Re-anchor both ends onto the band scale in one pass: the authored values down, and the
control/other defaults expressed as band TPS rather than flat constants.

`threatening_presence` at 600 is the clearest symptom — it does almost nothing mechanically (a
conditional −10% damage taken) and is carrying 600 threat purely to signal "this is the tank essence."
Under the band model that signalling job is unnecessary.

---

## 5. Fixing the depth problem

Breadth (37 essences) is fine. **Depth is the real homogenisation risk**, because the _unique tools_
sit on single essences:

| Tool                           | Essences that have it                           | Risk                                                                                                               |
| ------------------------------ | ----------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| **Taunt** (hard forced-target) | **1** — `transparent_slime`                     | If hard taunt is what separates tanking from not tanking, this essence is **mandatory for every tank in the game** |
| `Cover`                        | 2 abilities — `transparent_slime`, `wood_nymph` | Cover is Layer 3 of the design; two sources is thin                                                                |
| `Guard`                        | 2 — `brown_slime`, `skeleton`                   | —                                                                                                                  |
| `Ward`                         | 1 — `wood_nymph`                                | —                                                                                                                  |
| Ally barrier                   | 4                                               | Acceptable                                                                                                         |
| Hard CC                        | 4 — and only `enchanted_fairy` is AoE           | —                                                                                                                  |

**Recommendation: spread Taunt across 4–5 essences, each pairing it with a different secondary
payload**, so the choice of taunt-carrier is a real choice:

| Concept                      | Taunt +                             | Serves                                         |
| ---------------------------- | ----------------------------------- | ---------------------------------------------- |
| existing `transparent_slime` | Guard + AoE Cover                   | The all-round protector                        |
| new                          | AoE damage                          | Aggressive tank who wants to contribute damage |
| new                          | ally heal or barrier                | Tank/healer hybrid                             |
| new                          | Vulnerable or Weaken on all enemies | Debuff tank who raises party damage            |
| new                          | Unstoppable + self-mitigation       | Anti-CC tank for control-heavy bosses          |

Same logic for Cover: 3–4 sources rather than 2. Guard: a third source.

**Do not solve this by making Taunt unnecessary.** A tank without a hard taunt should be _viable but
less reliable_ — holding ~75% via band threat rather than ~95% with a taunt window. That keeps taunt
valuable without making one essence compulsory.

### 5.1 Two blockers to fix first

- **`essence.hobgoblin` and `essence.hobgoblin_brutal_charge` are currently unobtainable.** The
  hobgoblin creature exists in `creatures.json` with a loot table, but **no area or dungeon in
  `regions.json` spawns it** — and `region_01_area_05` is missing from the area list entirely (areas
  jump 04 → 06), which is probably where it belonged. Both Rare essences, including the 600-threat
  tank passive, cannot be acquired. Fix the spawn before treating hobgoblin as part of the tank
  roster.
- **Essence slot growth is not implemented.** `IncreaseEssenceSlotAction.Execute` and
  `IncreaseEssenceReserveSlotAction.Execute` are both `Task.CompletedTask`. **The equippable slot count
  is the single largest input to whether 37 tank-capable essences produces diversity.** At 3–4 slots,
  players must make hard choices and variety is guaranteed. At 8+, everyone slots every high-threat
  ability and diversity evaporates regardless of authoring. The 2 / 4 / 6 progression (levels 10 / 30 / 50) referenced elsewhere in the docs is the right shape — **6 is a good ceiling**; confirm and
  implement it before tuning bands.

---

## 6. Implementation notes and bugs

### 6.1 `AbilityThreatRules.GetThreatValue` is inert against real content

It derives threat from `effect.BaseValue` summed over Damage / Heal / GrantBarrier effects. But in
`abilities.json`:

| Operation      | n   | `baseValue` min / median / max                                   |
| -------------- | --- | ---------------------------------------------------------------- |
| `Damage`       | 83  | 0 / **0** / 31 (only `ravenous_ghoul.draining_claws` is nonzero) |
| `Heal`         | 12  | 0 / **0** / 0                                                    |
| `GrantBarrier` | 6   | 0 / **0** / 0                                                    |

Magnitude lives in `scalingCoefficient` (× `scalingAttribute`): damage single-target median **1.4**,
AoE median **0.6**; heal median 0.35; barrier median 0.145. So for ~160 of 168 abilities the
damage/support branch returns 0 and execution falls through to the control/self-defensive/other
branches.

**Any threat derivation must read `scalingCoefficient` for those operations, and `baseValue` only for
`ApplyCondition` / `GrantCover` / the `Modify*` family.** This is a prerequisite, not a refinement.

### 6.2 Do not let ascension scale threat on hard CC

`EssenceProgressionConstants` scales per ascension tier: +12% damage, +10% heal/barrier, +8%
attribute, +6% status stacks, +5% duration (cap 15%), −5% active cooldown (cap 15%). `Taunt` currently
scales on the **attribute** growth rate (8%/tier), and `ScaleEffectDurationSeconds` **explicitly
refuses to scale hard-CC durations**.

Mirror that refusal for threat: **do not scale threat on Stun/Freeze abilities.** Only 4 essences
hard-CC; if their threat compounds with ascension they become mandatory tanks at high tier. Cooldown
reduction is a subtler version of the same problem — a −15% cooldown at tier 3 is a **+18% TPS**
increase across the board, so verify the band ratios still hold at max ascension, not just at tier 0.

### 6.3 Gate reactive threat triggers

`OnDamaged` (5 uses) and `OnAttacked` (1) are proven-working hooks with `EventWasCritical` /
`EventAttackTypeIs` conditions and `uses` caps. `OnHit` (3) and `OnInterval` (12, the most-exercised
path) also work. But note **`OnBarrierAbsorbed` is declared with zero content** — it is unproven, so
don't build a headline tank passive on it without testing that it fires.

Any reactive threat trigger needs `internalCooldownTicks`. An ungated `OnDamaged → threat` on a tank
being focused by a boss fires every hit and produces runaway threat.

### 6.4 Validation to add

At startup, alongside the existing ability validators:

- Warn when an ability's derived threat exceeds the band-derived value by more than 2× without an
  authored `threatValue` — catches accidental band stacking.
- Warn when a Damage/Heal/GrantBarrier effect has `baseValue != 0` **and** `scalingCoefficient != 0`
  — ambiguous magnitude.
- Fail when an authored `threatValue` is set on an ability with `kind: Passive` and no trigger and no
  `uses` cap — that's an unbounded threat source.
- Emit a build-time report of derived TPS per essence, so content authors can see the spread. This is
  the single most useful tool for keeping 37 essences in band.

---

## 7. Worked examples

Using `BandTps` from §3.2, `TicksPerSecond = 10`, magnitude modifier omitted for clarity.

| Ability                                                                    | cd      | Bands                   | Derived threat        | TPS  |
| -------------------------------------------------------------------------- | ------- | ----------------------- | --------------------- | ---- |
| `brown_slime.absorb_impact` (Guard 5)                                      | 120     | Protective—self         | `5.0 × 12` = **60**   | 5.0  |
| `transparent_slime.transparent_engulf` (Guard + Cover AoE; Taunt/Mark → 0) | 120     | Prot—self + Prot—ally   | `10.0 × 12` = **120** | 10.0 |
| `wood_nymph.bramble_shield` (ally Cover + barrier + Thorns)                | 160     | Prot—ally + Retaliation | `8.5 × 16` = **136**  | 8.5  |
| `lumo_wisp.soothing_glow` (heal lowest ally)                               | 80      | Support—ally            | `3.5 × 8` = **28**    | 3.5  |
| `blue_slime.sweet_water` (AoE heal)                                        | 140     | Support—ally            | `3.5 × 14` = **49**   | 3.5  |
| `giant_worm.drag_beneath` (damage + Stun 2)                                | 150     | Damage + Control—hard   | `4.0 × 15` = **60**   | 4.0  |
| generic damage active (coeff 1.4)                                          | 120     | Damage                  | `1.5 × 12` = **18**   | 1.5  |
| `thornback_boar.bristling_hide` (Thorns 10, combat start)                  | passive | Retaliation, one-shot   | `3.5 × 20` = **70**   | ~3.5 |
| `skeleton.calcium` (Guard 4, combat start)                                 | passive | Prot—self, one-shot     | `5.0 × 20` = **100**  | ~5.0 |
| `blue_slime.protective_slime` (AoE barrier, combat start)                  | passive | Prot—ally, one-shot     | `5.0 × 20` = **100**  | ~5.0 |

### Three builds, six essence slots each

| Build              | Composition                       | TPS (incl. 2.7 basic) | Ratio vs 12 TPS median | Aggro share vs 4 allies |
| ------------------ | --------------------------------- | --------------------- | ---------------------- | ----------------------- |
| **Committed tank** | 6 protective/retaliation essences | ~33                   | 2.75×                  | **76%**                 |
| **Hybrid tank**    | 3 protective + 3 damage           | ~22                   | 1.83×                  | **53%**                 |
| **Damage**         | 6 damage essences                 | ~12                   | 1.00×                  | 20%                     |

The committed tank tanks. The hybrid holds aggro about half the time — a real, playable off-tank. The
damage build doesn't tank. **And crucially, "6 protective essences" can be assembled from 37
candidates in an enormous number of ways**, every one of which reaches roughly the same TPS. That is
the diversity the question was asking for.

---

## 8. Checklist

**Before authoring any threat values:**

1. Fix `AbilityThreatRules` to read `scalingCoefficient` for Damage/Heal/GrantBarrier (§6.1).
2. Confirm and implement the equippable essence slot count; 6 is the recommended ceiling (§5.1).
3. Fix the hobgoblin spawn gap, or drop hobgoblin from the tank roster (§5.1).
4. Replace the `AbilityThreatTuning` flat constants with the band TPS table (§3.2).

**Then:**

5. Run the derivation over all 168 abilities and review the per-essence TPS report (§6.4).
6. Re-anchor the four existing authored `threatValue`s onto the band scale (§4.6).
7. Verify band ratios still hold at max ascension, accounting for the −15% cooldown (§6.2).
8. Author 3–4 additional Taunt carriers and 1–2 additional Cover/Guard sources (§5).
9. Add the validators (§6.4).

**Success criteria:**

- Fewer than 15 of 168 abilities carry an authored `threatValue`.
- At least 15 distinct 6-essence loadouts reach ≥30 TPS.
- No single essence appears in every viable tank loadout.
