# Combat Style System — Design

**Status:** Design proposal (not implemented)  
**Author:** Design pass, 2026-08-19  
**Feature name (player-facing):** **Combat Style**  
**Question this answers:** how players shape the behaviour of a build without making one Essence,
one item set, or one mandatory tank stance define the role  
**Related:** `docs/threat-and-tanking-design.md`, `docs/threat-ability-authoring.md`

---

## 0. Executive summary

A character build has three independent knobs:

| Build axis       | Player question              | Owns                                                       |
| ---------------- | ---------------------------- | ---------------------------------------------------------- |
| **Essences**     | What can I do?               | Abilities, triggers, summons, and the kit's core mechanics |
| **Gear**         | How strong and durable am I? | Attributes, equipment effects, and numeric stat expression |
| **Combat Style** | How does my build behave?    | Priorities, conditional rules, conversions, and trade-offs |

Combat Style is a compact, connected passive web inspired by _Path of Exile_, but sized for this
game rather than copied from it. The initial web should contain roughly **20–30 nodes**, while a
character receives enough points to take approximately **8–10 nodes**. Players can cross between
regions of the web instead of selecting a class.

Most nodes should be conditional or mechanical. Gear already owns raw numbers, so Combat Style
should not become another equipment sheet disguised as a skill tree. A good node says “barriers
placed on allies also protect you” or “control effects generate more threat”; a weak node says
“+15 Power.”

**Battle Stances are part of Combat Style, not a fourth build system.** They are optional,
mutually-exclusive keystone nodes that make a large trade-off. A character may use no Stance at all.
No Stance is universally “the tank stance,” and baseline protective abilities must generate enough
threat to tank without one.

This gives players several legitimate tank identities: an ally-protecting Warden, a self-mitigating
Bulwark, a reactive Retaliator, a control-oriented Disruptor, or a hybrid built across those paths.
The same web also supports damage, healing, summoning, and utility builds.

---

## 1. The design decision

The system should answer a problem that Essences and gear cannot solve cleanly on their own:

> Two characters can equip different abilities and comparable gear, yet still choose different
> combat priorities and relationships between those abilities.

Essences must remain the primary source of capability. A Style node may enhance Cover, but it does
not grant Cover to a build that owns no Cover effect. Gear must remain the primary source of raw
attributes. A Style may reward blocking, but it should not be the main way a character obtains Armor
or Block Chance.

Combat Style sits between those systems. It changes how capabilities interact and which behaviours
the build emphasizes.

### 1.1 Why this is not a class system

The player does not choose “Tank,” “Healer,” or “Damage Dealer.” They spend points on a connected
web. Regions establish readable themes, while bridges allow hybrids. A build's role emerges from its
Essences, gear, and chosen path together.

This matters especially for tanking. A mandatory tank class or a universal “generate +100% threat”
node would collapse the intended build variety. The system instead offers several conditional ways
to increase threat, each attached to behaviour the player has already chosen.

### 1.2 Why Battle Stance is not a separate system

“Battle Stance” sounds like one mode a character toggles. “Combat Style” describes the permanent
build layer. Both names can be useful if their relationship is precise:

- **Combat Style** is the full passive web and its saved allocation.
- **Stance** is an optional, mutually-exclusive keystone within that web.
- A build may allocate no Stance and remain fully viable.
- Stances are selected before combat and never toggled by the simulator mid-fight.

This preserves three build axes instead of accidentally creating a fourth.

---

## 2. Goals and non-goals

### 2.1 Goals

1. Give players a third, legible source of build authorship.
2. Support multiple identities within the same combat role.
3. Make threat generation something a player can deliberately tune.
4. Preserve Essences as the source of abilities and gear as the source of attributes.
5. Make hybrid paths useful rather than treating every non-specialized build as a mistake.
6. Keep the first version small enough for one developer to author, test, and balance.
7. Make allocations deterministic, inspectable, and suitable for auto-resolved combat.
8. Use the same rules in ordinary PvE, World Bosses, Rifts, raids, and Tournament Grounds.

### 2.2 Non-goals

- Reproducing the size or complexity of _Path of Exile's_ passive tree.
- Adding active abilities that compete with Essences.
- Replacing gear progression with permanent flat-stat bonuses.
- Declaring a tank before combat or bypassing threat with a party role selector.
- Requiring every group, including a 3v3 tournament team, to designate a tank.
- Allowing mid-combat allocation changes or stance switching.
- Shipping hundreds of bespoke node scripts that cannot be validated centrally.

---

## 3. Player-facing model

### 3.1 Vocabulary

| Term             | Meaning                                              |
| ---------------- | ---------------------------------------------------- |
| **Style Point**  | One point spent to allocate a connected node         |
| **Small node**   | A modest, focused modifier that helps form a path    |
| **Notable**      | A stronger conditional effect or interaction change  |
| **Keystone**     | A build-defining rule with a real cost or constraint |
| **Stance**       | A mutually-exclusive kind of Keystone; optional      |
| **Style preset** | A named, saved allocation and its selected Stance    |

### 3.2 Initial size

The first version should target:

- **20–30 total nodes**;
- **8–10 spendable Style Points** at full initial progression;
- one central, neutral starting area;
- four to six themed regions;
- multiple bridges between neighbouring regions;
- no dead-end tax nodes whose only purpose is consuming a point.

This is large enough to produce meaningful paths but small enough that every node can be understood
without a third-party planner.

### 3.3 Connectivity

Players begin at the central hub and may allocate any connected node they can afford. The web must
not assign a starting point by class, species, weapon, or Essence. A region signals a concentration
of mechanics, not a locked archetype.

Cross-region bridges are important. For example:

- Guardianship ↔ Restoration enables a protector/healer.
- Bastion ↔ Retaliation enables a block-and-counter tank.
- Control ↔ Assault enables a debuff damage build.
- Invocation ↔ Guardianship enables defensive summons.

The exact visual layout may evolve, but every initial region should have at least two viable exits.

---

## 4. Thematic regions

These are authoring families, not classes. A node may sit on a bridge and carry two families.

| Region           | Behaviour it emphasizes                           | Example mechanics                                                    |
| ---------------- | ------------------------------------------------- | -------------------------------------------------------------------- |
| **Assault**      | Direct pressure and offensive sequencing          | critical trade-offs, damage-over-time conversion, execute conditions |
| **Bastion**      | Self-protection and endurance                     | Guard, Ward, Block, mitigation, defensive cooldown loops             |
| **Guardianship** | Protecting allies and controlling enemy attention | Cover, ally barriers, redirected damage, protective threat           |
| **Restoration**  | Healing and recovery priorities                   | overhealing, low-health ally triggers, regeneration, cleansing       |
| **Control**      | Disruption and debuff leverage                    | Stun, Freeze, Slow, Weaken, cooldown manipulation, control threat    |
| **Invocation**   | Summons, resources, and indirect effects          | summon inheritance, resource conversion, trigger cadence             |

Not every region needs the same number of nodes in the first release. The web should reflect the
actual mechanic vocabulary supported by the engine and current Essence catalogue.

### 4.1 Example node shapes

Good Combat Style effects include:

- Barriers granted to allies also grant the caster a smaller barrier.
- A portion of overhealing becomes a barrier.
- Blocking advances the cooldown of the character's next defensive ability.
- Hard control generates additional threat while soft control lasts longer.
- Retaliation effects generate more threat, but direct-damage threat is reduced.
- Guard lasts longer but mitigates less per hit.
- Cover redirects a smaller share from each ally but may protect one additional ally.
- Critical strikes deal less immediate damage but apply a stronger Bleed.
- Healing a low-health ally advances a recovery cooldown.
- Summons inherit more defensive attributes but less Power.

Poor Combat Style effects include:

- large unconditional Power, MaxHealth, Armor, or Resistance grants;
- granting an active ability that should belong to an Essence;
- unconditional “all threat +100%”;
- an effect that is always optimal for every build using a broad mechanic;
- a hidden modifier that cannot be explained in the node tooltip.

Small nodes may use limited numeric modifiers, but they should still point toward behaviour. “Barriers
you grant are 6% stronger” is acceptable connective tissue; “+6% to every attribute” is not.

---

## 5. Node taxonomy and power budget

### 5.1 Small nodes

Small nodes establish a path and make its intent readable. They should usually contain one modifier
and avoid new runtime hooks.

Typical budget:

- 5–10% to one narrow, conditional quantity;
- 5–10% duration or magnitude for one operation family;
- a similarly small threat modifier tied to one function band;
- no general-purpose flat attributes except where needed for a tiny central neutral node.

### 5.2 Notables

Notables should change an interaction or create a conditional loop. They are the main reason to
enter a region. A Notable may combine a moderate modifier with one rule change, but its tooltip must
remain explainable in one or two sentences.

Examples:

- **Shared Shelter:** granting an ally a barrier grants you 25% of that barrier.
- **Hold the Line:** while Guarded, your Protective—self abilities generate 30% more threat.
- **Measured Response:** blocking advances one defensive cooldown by 5 ticks, at most once every
  20 ticks.
- **Cruel Opening:** critical damage is reduced by 15%, but critical hits increase Bleed potency by
  35%.

### 5.3 Keystones and Stances

Keystones make the largest trade-offs. A Stance is a Keystone with an additional rule: only one
Stance may be active in a preset.

Candidate Stances for the first or a later release:

| Stance        | Identity             | Benefit                                              | Cost                                       |
| ------------- | -------------------- | ---------------------------------------------------- | ------------------------------------------ |
| **Vanguard**  | self-mitigating tank | more threat from Guard, Ward, and Block interactions | less threat from ally healing and barriers |
| **Warden**    | ally-protecting tank | more threat from Cover and ally barriers             | reduced self-sustain magnitude             |
| **Reprisal**  | retaliation tank     | more retaliation threat and stronger counter effects | less threat from direct damage             |
| **Tactician** | control tank/support | more threat and duration from control and debuffs    | reduced direct-damage output               |

These are candidates, not a requirement to ship all four immediately. The system must also support
**no Stance**, which is the natural choice for flexible hybrids.

Stance trade-offs should alter relative priorities rather than simply adding more total power. If
every specialized build always takes a Stance, the no-Stance option has failed.

---

## 6. Threat integration

Combat Style is the player-facing threat knob, but it does not replace the ability threat model.

The authoring guide derives baseline threat from an ability's **function bands** and cadence.
Protective and controlling loadouts therefore tank before Style is considered. Style then rewards
the particular tank behaviour the player wants to emphasize.

For positive threat generated by an activation:

```text
final threat = derived band threat
             × ability threat multiplier
             × applicable Combat Style modifiers
```

Negative threat effects remain authored values and are **not** multiplied by positive threat
bonuses. Otherwise a threat-reduction build could accidentally make its threat loss grow in the
wrong direction.

### 6.1 What Style is allowed to modify

Style threat modifiers must select stable combat concepts, preferably the same enum-backed
`operation + target + condition` signals used by the threat band model. Examples:

- Protective—self threat;
- Protective—ally threat;
- Retaliation threat;
- Support—ally threat;
- hard-control or soft-control threat;
- direct-damage threat;
- threat while Guarded, Warded, or covering an ally.

Do not depend on free-form ability tags until that vocabulary is normalized and validated.

### 6.2 Threat budget

A committed Combat Style should generally move a build's sustainable positive threat by roughly
**25–50%**, with the exact result depending on whether the character actually performs the selected
behaviour. The equipped abilities and their cadence must remain the largest input.

This produces the desired hierarchy:

1. **Essences** determine which threat-generating actions exist.
2. **Combat Style** emphasizes some of those actions.
3. **Gear** helps the character survive or improve the underlying effects, without carrying a
   generic threat stat.

A Style allocation must never turn a pure damage loadout into the best tank merely by multiplying
its low damage-band threat.

### 6.3 Multiple valid tank identities

Comparable threat can come from different conditional sources:

| Tank identity    | Primary threat behaviour            | Typical survival plan                      |
| ---------------- | ----------------------------------- | ------------------------------------------ |
| **Protector**    | Cover and barriers on allies        | redirects damage and shares barriers       |
| **Bulwark**      | Guard, Ward, Block, self-mitigation | absorbs direct attention                   |
| **Retaliator**   | Thorns and reactive damage          | punishes attacks and recovers between hits |
| **Disruptor**    | Stun, Freeze, Slow, Weaken, debuffs | reduces enemy action quality               |
| **Support tank** | healing and protecting allies       | combines sustain with moderate mitigation  |

The balance target is not identical threat on every combat tick. It is comparable sustained
attention over representative encounters, with different strengths against different bosses.

### 6.4 Taunt, Mark, Stealth, and explicit targeting

- **Taunt** remains a short forced-target tool. It is useful but does not define the tank role.
- **Mark** changes which enemy allies prefer and does not generate self-threat.
- **Stealth** and authored threat loss continue to reduce attention.
- Explicit target mechanics, area attacks, and boss scripts may bypass threat deliberately.
- Cover and other ally-protection mechanics remain valuable when threat cannot prevent damage.

Combat Style may enhance these mechanics conditionally, but it must not erase their distinct roles.

---

## 7. Game-mode behaviour

### 7.1 World Bosses, Rifts, and raids

Threat remains emergent from each submitted build. No party leader designates a tank. This permits:

- independently built characters to cooperate without pre-arranged roles;
- two tanks to compete for or exchange attention naturally;
- a backup tank to inherit attention when the primary tank is defeated;
- encounter mechanics to test different protection styles;
- the pre-combat UI to estimate threat contribution from the saved build.

Style is snapshotted with the rest of the submitted combat build. Later edits do not change a battle
that has already been submitted or begun.

### 7.2 Tournament Grounds (3v3)

Tournament teams do **not** select a required tank. They use the same threat rules as every other
combat mode. A player may submit:

- a conventional tank, support, and damage team;
- a control-heavy team with no dedicated tank;
- several durable hybrids;
- a high-pressure team that accepts less control over enemy attention.

The submitted lineup locks each character's Style preset for that match or tournament entry, using
the same snapshot boundary as Essences and gear. This avoids a tournament-only role system and makes
the build shown before resolution the build that actually fights.

---

## 8. Progression, presets, and respecs

### 8.1 Earning points

Style Points should come from deterministic character progression or explicit milestones. They must
not be random drops and should not consume equipment budget. The exact unlock levels should be set
against the final progression curve, but the initial ceiling should remain around 8–10 points.

The first point should arrive only after the player understands Essences and gear. Introducing all
three systems simultaneously would obscure what each one controls.

### 8.2 Respec policy

For the initial release, reallocating Combat Style should be **free outside combat and outside a
locked submission**. The system asks players to experiment with interactions that will need multiple
balance passes. A respec tax would discourage the behaviour the feature is intended to create.

If a cost is introduced later, it should remain low-friction and never depend on a rare consumable.

### 8.3 Presets and loadouts

Combat Style remains a distinct build axis even when the UI saves it with a loadout.

- Players can create named Style presets.
- An Essence/equipment loadout may reference a Style preset for one-click equipping.
- Editing a referenced Style preset must clearly state which loadouts use it.
- A loadout snapshot records the resolved allocation, not only a mutable preset identifier.
- Invalidated presets receive a free repair/reset after content or graph changes.

The player should be able to reuse one Style with several Essence loadouts or pair one Essence
loadout with different Styles.

---

## 9. UX requirements

The web must be understandable without an external build planner.

### 9.1 Web view

The UI should show:

- available and spent Style Points;
- allocated, available, locked, and previewed nodes;
- connection paths and the point cost of a previewed path;
- region labels and concise descriptions;
- the active Stance, if any;
- node search by supported mechanic, such as “Barrier,” “Threat,” or “Summon”;
- an explicit Apply action so exploratory clicks do not immediately mutate the build.

### 9.2 Tooltips

Every node tooltip should include:

1. the exact rule;
2. the condition under which it applies;
3. the trade-off, if any;
4. the affected abilities in the currently equipped Essence loadout, when practical;
5. a before/after preview for derived values such as estimated threat per second.

“Your protective abilities generate more threat” is insufficient if the UI can instead say which
equipped abilities qualify.

### 9.3 Character-sheet summary

The character sheet should summarize Style through a small number of derived descriptions rather
than listing every node again, for example:

```text
Combat Style: Warden
Emphasis: ally protection, barriers, Cover
Estimated sustained threat: 38.4/s (+31% from Style)
```

Threat estimates are advisory because target availability, conditions, and encounter scripts affect
realized behaviour.

---

## 10. Example builds

These examples demonstrate that no universal Essence or Stance defines tanking.

### 10.1 Wood Nymph protector

- **Essences:** Wood Nymph plus complementary healing or barrier effects.
- **Gear:** MaxHealth and mitigation, improving survival and any effects that scale from MaxHealth.
- **Style:** Guardianship/Restoration path; perhaps Warden.
- **Result:** protects allies with Cover and barriers, creates threat by performing that job, and
  trades some personal sustain for group protection.

### 10.2 Brown Slime bulwark

- **Essences:** Brown Slime plus self-defense and recovery.
- **Gear:** Armor, Block, MaxHealth, or Resistance according to encounter damage.
- **Style:** Bastion path; perhaps Vanguard.
- **Result:** holds attention through Guard and self-mitigation while absorbing repeated direct hits.

### 10.3 Cinder Beetle retaliator

- **Essences:** Cinder Beetle plus defensive triggers.
- **Gear:** durability sufficient to survive being attacked frequently.
- **Style:** Bastion/Retaliation bridge; perhaps Reprisal.
- **Result:** turns enemy attention into reactive pressure and threat rather than copying the
  protector's barrier loop.

### 10.4 Enchanted Fairy disruptor

- **Essences:** Enchanted Fairy plus debuffs and defensive utility.
- **Gear:** cooldown, sustain, and appropriate defensive attributes.
- **Style:** Control/Bastion bridge; perhaps Tactician.
- **Result:** tanks less reliably than a hard-taunt protector but suppresses enemy actions and earns
  attention through repeated control.

### 10.5 Non-tank hybrid

- **Essences:** healing and damage abilities.
- **Gear:** Power and recovery-oriented attributes.
- **Style:** Restoration/Assault bridge with no Stance.
- **Result:** heals allies under pressure while preserving useful damage, without accidentally
  becoming a full tank.

---

## 11. Data and runtime model

The exact types should follow existing Core conventions. Conceptually, the system needs:

```text
CombatStyleDefinition
  Id
  Version
  Nodes[]

CombatStyleNodeDefinition
  Id
  Name
  Description
  NodeType               // Small, Notable, Keystone, Stance
  PointCost
  Position
  ConnectedNodeIds[]
  Requirements[]
  Effects[]
  Families[]

CombatStyleEffectDefinition
  EffectType             // enum-backed modifier or rule hook
  Selector               // operation, target, condition, trigger, etc.
  Value
  LimitOrCooldown

CharacterCombatStylePreset
  Id
  CharacterId
  Name
  DefinitionVersion
  AllocatedNodeIds[]
  ActiveStanceNodeId?
```

### 11.1 Authoring constraints

- Node and effect identifiers are stable and unique.
- Runtime effect types are enum-backed and centrally handled.
- Selectors prefer validated operation, target, condition, and trigger enums.
- New rule hooks are added intentionally; arbitrary executable scripts are not stored in content.
- Every repeatable reactive effect has a limit, internal cooldown, or other deterministic gate.
- Effects compose through the existing modifier pipeline rather than mutating authored abilities.
- Definitions live in Core-owned content/configuration; Presentation only renders and submits choices.
- Combat snapshots store the resolved Style state and definition version.

### 11.2 Resolution order

A consistent order prevents hidden differences between preview and combat:

1. Load authored ability and Essence definitions.
2. Resolve character attributes from progression and gear.
3. Resolve equipped Essence scaling from the appropriate attributes.
4. Apply Combat Style rule modifications and conditional registrations.
5. Derive display values, including estimated threat.
6. Snapshot the fully resolved combat build.

Style must not rewrite source definitions or leak state between characters.

---

## 12. Validation and balance guardrails

### 12.1 Definition validation

Startup or content-build validation should reject:

- duplicate or missing node identifiers;
- connections to unknown nodes;
- asymmetric connections unless explicitly supported;
- nodes unreachable from the central hub;
- zero or negative point costs;
- more than one selected Stance;
- selectors referencing unsupported operations or conditions;
- reactive effects with no deterministic frequency bound;
- presets spending more points than the character owns;
- presets created against an incompatible definition version.

### 12.2 Balance guardrails

The balance suite should test representative complete builds rather than isolated node values only.
At minimum:

1. A baseline protective loadout can tank with no Stance.
2. Protector, Bulwark, Retaliator, and Disruptor builds reach comparable sustained threat bands.
3. No pure damage build becomes the strongest tank through Style alone.
4. No single node appears in every competitive build for a role.
5. Stance builds and viable no-Stance hybrids both exist.
6. Style contributes less raw attribute value than gear at comparable progression.
7. Reactive loops cannot trigger themselves indefinitely.
8. Previewed threat and simulated threat agree when all qualifying conditions are held constant.
9. Removing or changing a node invalidates presets safely and visibly.

The goal is not perfect equality. A Warden should outperform a Retaliator when allies need constant
Cover; the Retaliator should outperform when a boss attacks rapidly. Encounter-dependent strengths
are desirable as long as one path does not dominate the full encounter set.

### 12.3 Telemetry worth collecting

If production analytics are available later, track:

- allocation and respec rates per node;
- Stance and no-Stance usage;
- common Essence/Style combinations;
- threat share and survival by Style family;
- nodes present in an unusually high share of successful builds;
- nodes allocated but rarely activated in combat.

Telemetry informs balance changes; it must not silently alter player builds.

---

## 13. Initial content proposal

The smallest useful release is one complete web, not a partially implemented framework with
placeholder nodes.

Suggested content shape:

| Node type                   | Approximate count | Purpose                                           |
| --------------------------- | ----------------: | ------------------------------------------------- |
| Central/neutral small nodes |               2–3 | accessible entry choices and bridges              |
| Themed small nodes          |             10–14 | readable paths and narrow modifiers               |
| Notables                    |               7–9 | mechanical identity and cross-system interactions |
| Stances/Keystones           |               3–4 | optional build-defining trade-offs                |
| **Total**                   |         **22–30** | compact initial web                               |

At least half of the initial Notables should support non-tank builds. Threat is an important first
integration, not the sole purpose of Combat Style.

---

## 14. Delivery sequence

### Phase 1 — Foundation

- Add versioned definitions and validation.
- Add character presets and deterministic point progression.
- Add the connected-web allocation rules.
- Add a small enum-backed modifier vocabulary.
- Add snapshot support.
- Build the web UI, preview, apply, and free respec flow.

### Phase 2 — First complete web

- Author and ship the full 20–30-node initial web.
- Integrate function-band threat modifiers.
- Add several interaction Notables across protection, damage, healing, control, and summons.
- Add no more Stances than can be meaningfully differentiated and tested.
- Show affected equipped abilities and estimated threat changes.

### Phase 3 — Mode integration and tuning

- Attach Style snapshots to World Boss, Rift, raid, and Tournament submissions.
- Add representative automated balance scenarios.
- Tune against real encounter mixes, not a single training target.
- Expand node content only after the initial graph shows genuine build diversity.

---

## 15. Accepted design decisions

These decisions define the proposal and should not be reopened accidentally during implementation:

1. The three player build axes are **Essences, Gear, and Combat Style**.
2. Essences own abilities; gear owns most raw attributes; Style owns behaviour and trade-offs.
3. Battle Stances, if implemented, are optional mutually-exclusive nodes inside Combat Style.
4. The web is compact and connected, with approximately 20–30 initial nodes and 8–10 points.
5. Respecs are free outside combat and locked submissions for the initial release.
6. Threat remains the common targeting system; players do not designate a tank per encounter.
7. Baseline ability threat is sufficient for tanking without a particular Style or Stance.
8. Style provides several conditional threat paths instead of one universal threat multiplier.
9. Tournament Grounds uses the same build and threat rules as other combat modes.
10. The system is data-driven, validated, deterministic, and snapshot-safe.

The intended result is not “every tank chooses the tank tree.” It is that players can look at the
same encounter, choose different ways to protect a team, and have the combat engine recognize each
of those choices as tanking.
