# World Tower Floor Bosses: Floors 11–20

> Status: Floors 11–15 are implemented. Floors 16–20 remain concept lore and encounter-design reference.

## The Stolen Hours

Floors 1–10 form the outer reaches of the World Tower. Defeating the Mad King reveals a path into an older, forgotten court: a realm caught within a single dying moment.

Its sovereign tried to stop the Tower's clock and prevent a foretold defeat. The inhabitants survived, but only as prisoners of an hour that could never end. Every guardian in this band represents something the court sacrificed to escape the passage of time: names, freedom, balance, memory, life, and ultimately the future itself.

The proposed encounter cadence follows the established Tower structure:

- Floors 11–14: Standard guardians
- Floor 15: Warden
- Floors 16–19: Standard guardians
- Floor 20: Sovereign

Floors 11–15 begin a substantially steeper progression band. Floor 11 is balanced around a full ten-character roster averaging Tier 2 Epic equipment of Exceptional quality, with seven equipped Essences per character. Its recommended Power Rating is 280. Floors 12–14 retain ten-character rosters and rise to 300, 325, and 350 recommended Power Rating; Floor 15 expands to fifteen characters and 400 recommended Power Rating.

## Floor 11 — The Hushed Archive

### Serevin, the Name-Eater

Serevin is a robed, faceless archivist who erases warriors from history. The shelves of the Hushed Archive contain biographies with their names scratched away, leaving only accounts of deaths no one remembers.

#### Encounter concept

- **Black Annotation (8s):** Erases one removable buff from every enemy, including summons. Serevin gains 1 Ink for each buff actually erased.
- **Redacted (15s):** Deals 250% Magical Damage to a random enemy and attempts to Silence them for 15 seconds. The random target ignores Taunt, and Ward can negate the Silence.
- **Spilling Ink (17s):** Deals 50% Magical Damage per Ink to two distinct random enemies, then consumes all Ink. Its random targets ignore Taunt.
- **Unwritten Law:** Ink caps at 15. Each stack grants 5% of Serevin's initial Power but removes 2% of his initial Armor and Resistance. Staggering Serevin halves his Ink, rounded down.

#### Encounter identity

An anti-buff guardian whose growing damage also exposes him to retaliation. Disciplined buff use limits Ink, Ward protects a key active ability from Redacted, and well-timed Staggers cut both his stored burst and Power bonus while restoring part of his defenses.

## Floor 12 — The Hanging Tempest

### Volgrin, the Shackled Storm

Volgrin is a lightning titan suspended from the Tower's ceiling by colossal iron chains. The forgotten court bound the living storm to power its motionless kingdom, but every broken chain returns a piece of Volgrin's fury.

#### Encounter concept

- **Chainbound Bolt (9s):** Deals 220% Magical Damage to Volgrin's current target.
- **Shackled Arc (14s):** Deals 110% Magical Damage and applies Chill(1) to two distinct random enemies.
- **Rattling Sky (18s):** Deals 75% Magical Damage to all enemies, plus 35% for each Broken Chain.
- **The Three Chains:** Volgrin begins combat with 45% more initial Armor and Resistance but 30 less Attack Speed. At 75%, 50%, and 25% Health, a chain permanently breaks, removing 15% of his initial Armor and Resistance and granting 15 Attack Speed.
- **Final Thunder:** With all three chains broken, each Basic Attack deals 35% Magical Damage to exactly two other random enemies.

#### Encounter identity

A reverse-enrage encounter. Damaging Volgrin makes him easier to kill but increasingly dangerous, creating a steadily mounting defensive and healing check.

## Floor 13 — The Drowned Orrery

### Nhalia, the Moondrowned

Nhalia was the court astrologer who first witnessed the sovereign's defeat written in the heavens. She drowned the prophecy beneath an artificial sea, but the dead moon above her chamber continues to turn.

#### Encounter concept

- **Drowned Constellation (11s):** Deals 100% Magical Damage to all enemies. During High Tide, each target also gains 1 Soaked.
- **Moonfall (14s):** Deals 240% Magical Damage, plus 20% per Soaked, to the enemy carrying the most Soaked. Its selection ignores Taunt and includes summons.
- **Turning of the Dead Moon (20s):** Nhalia begins in High Tide and alternates tides whenever this ability is used. High Tide grants 50% of her initial Armor and makes direct damaging attacks apply 1 Soaked. Low Tide grants 50% of her initial Resistance; entering it deals 40% Magical Damage per Soaked to every affected enemy and consumes all Soaked.
- **Gravitational Undertow:** Whenever an enemy receives effective healing, Nhalia heals for 20% of the amount actually restored. Overhealing contributes nothing.
- **Soaked:** A harmful condition with a shared maximum of 10 stacks. Ward blocks an application and Cleanse removes it.

#### Encounter identity

An alternating-phase guardian that rewards parties with a balance of Physical and Magical damage rather than a single dominant damage type. Healing discipline limits Undertow, while Cleanse and Ward control how much pressure is stored for Moonfall and the next Low Tide.

## Floor 14 — The Unlit Forge

### Caldris, Smith of the Fallen Star

Caldris is a blackened giant encased in armor forged from a dead star. He created the chains, seals, and anchors that hold the stolen hour together. His forge has no flame; its metal is shaped by pressure, darkness, and rage.

#### Encounter concept

- **The Unlit Forge:** Caldris begins combat with 4 permanent **Star-Iron Plates**. Each plate grants 8% Damage Reduction.
- Staggering Caldris shatters one plate. Each Shattered Plate grants 10% Power and 8 Attack Speed.
- **Meteor Hammer:** Deal 210% Physical Damage to the current target, plus 35% for each Shattered Plate. Cooldown: 10 seconds.
- **Blackstar Quake:** Deal 100% Physical Damage to all enemies. Cooldown: 16 seconds.
- **Reforge:** Restore one shattered plate and remove its corresponding offensive stack. Cooldown: 20 seconds; being staggered resets the cooldown.
- Active abilities begin combat on cooldown. Staggers remain possible after four total breaks so restored plates can be shattered again.

#### Encounter identity

A guardian built around the Tower's stagger system. Breaking Caldris is necessary to defeat him, but every successful break accelerates the encounter.

## Floor 15 — The Warden's Measure

### Serath, the Second Warden

Serath is a six-armed judge carrying chains, blades, and a perfectly balanced scale. Where Kharad guards the act of ascension, Serath judges whether an entire company deserves to ascend. To Serath, dependence upon a single champion is proof of collective weakness.

#### Encounter concept

- **The Living Scale:** Beginning ten seconds into combat and every fifteen seconds thereafter, Serath compares the highest and lowest Health percentages among living non-summoned challengers. A difference of twelve percentage points or less gives Serath **Doubt**, causing him to take 15% increased damage until the next measurement. A wider difference gives him **Conviction**, granting 15% Power and 15 Attack Speed. If fewer than two eligible challengers remain, Serath automatically gains Conviction.
- **Weight of Flesh:** Deals 220% Physical Damage to the living non-summoned challenger with the highest current Health percentage. It ignores Taunt, and ties are resolved randomly.
- **Weight of Spirit:** Deals 190% Magical Damage to the living non-summoned challenger with the lowest current Health percentage and applies Wound for eight seconds. It ignores Taunt, and ties are resolved randomly. Ward can block the Wound.
- **Sixfold Sentence:** Serath strikes three random enemies for 55% Physical Damage, then independently selects three random enemies for 55% Magical Damage. Summons can be selected, and the same challenger may be judged by both halves.

#### Encounter identity

A coordination Warden who judges the party's Health distribution. Keeping the company close together exposes Serath through Doubt, while uneven damage, deaths, or neglected allies strengthen him through Conviction. His paired single-target attacks pull against that goal by pressuring both ends of the party's Health range.

## Floor 16 — The Choir of Empty Masks

### Myriel, Cantor of the Hollow Court

Myriel conducts nine porcelain masks containing the final voices of the court. The singers surrendered their faces and bodies so their last hymn could continue for as long as the sovereign's frozen hour endured.

#### Encounter concept

- Each mask represents a different voice, such as Fury, Sorrow, Hunger, or Silence.
- Living masks add notes to **The Hollow Hymn**, an increasingly powerful periodic attack.
- Destroying a mask weakens the Hymn but empowers the surviving masks.
- **Grand Crescendo:** Detonates every active voice, with each contributing a different effect.
- Staggering Myriel interrupts the current Hymn and briefly silences the masks.

#### Encounter identity

A controlled summon encounter. Unlike other swarm guardians, Myriel's summons collectively construct a modular ultimate rather than functioning primarily as independent attackers.

## Floor 17 — The Garden of Last Breath

### Orrun, the Pale Gardener

Orrun is a skeletal tree-creature who cultivates flowers from the remains of failed challengers. The garden preserves every final breath ever drawn within the Tower, feeding life back into the court without allowing anything to be truly born.

#### Encounter concept

- **Grave Seed:** Healing a seeded character also nourishes the seed.
- At sufficient nourishment, the seed blooms, damaging its host and healing Orrun.
- **Harvest the Living:** Consumes all active blooms to empower an area attack.
- **Winter's Mercy:** Converts Orrun's excess healing into Barrier.
- Barriers prevent Grave Seeds from being nourished, offering an alternative to conventional healing.

#### Encounter identity

An anti-sustain guardian that rewards barriers, healing control, cleansing, and calculated recovery rather than maximum healing output.

## Floor 18 — The Clock Without Hands

### Vaska, the Unwound Knight

Vaska was the sovereign's undefeated champion. When she finally suffered a mortal wound, the court removed the moment of her death from history. She now fights as a broken clockwork knight, forever returning to the last instant in which she was whole.

#### Encounter concept

- Vaska periodically records her current Health and conditions.
- Several seconds later, **Rewind** returns her to the recorded Health value.
- Negative conditions remain after the rewind, allowing damage-over-time strategies to retain value.
- Rewind has a long cast that can be stopped by staggering Vaska.
- Each successful Rewind grants Vaska a permanent stack of **Déjà Vu**, increasing her offense.

#### Encounter identity

A timing and stagger guardian. Burst damage is wasted unless the party can interrupt the subsequent Rewind, while sustained condition damage offers an alternative strategy.

## Floor 19 — The Stair That Remembers

### Anathema, Echo of the Fallen

Anathema is not a living guardian. It is the Tower's memory of every creature defeated upon its stairs, forced into a single shifting form. It greets ascending champions with movements they have already learned to fear.

#### Encounter concept

At successive Health thresholds, Anathema assumes fixed echoes of earlier guardians:

- **Gatekeeper Echo:** Gains defensive seals that shatter as Health is lost.
- **Bloodwing Echo:** Spreads Bleed and hunts weakened enemies.
- **Mirrorbound Echo:** Briefly reflects direct damage.
- **Mad King Echo:** Enters a final lifesteal berserk.

These echoes are compressed and recombined versions of the original mechanics rather than complete repetitions of the earlier encounters.

#### Encounter identity

The final examination for the first two Tower bands. Players benefit from recognizing and preparing for mechanics introduced throughout floors 1–10.

## Floor 20 — The Throne of the Last Hour

### Aevum, Sovereign of the Last Hour

Aevum rules the court imprisoned within the stolen hour. After witnessing a prophecy of inevitable defeat, the sovereign stopped the Tower's clock. The kingdom escaped its ending, but it also surrendered every possibility of change, growth, and renewal.

#### Encounter concept

The encounter unfolds across three phases:

1. **The Past:** Aevum summons fragments of defeated guardians and accumulates defensive Hour Seals.
2. **The Present:** Aevum records damage dealt and healing received, then repeats portions of both against the party.
3. **The Future:** A visible countdown begins. When it reaches zero, **The Last Hour** deals catastrophic damage.

During the final phase, Aevum attempts **Sovereign Rewind**, which would restore the encounter to the beginning of that phase. Destroying the summoned Hour Anchors or staggering Aevum enough times prevents the rewind.

#### Encounter identity

A capstone encounter combining summons, stagger pressure, escalating danger, and a hard finale. Its phases bring the narrative of the Stolen Hours to a conclusion without relying exclusively on overwhelming statistics.
