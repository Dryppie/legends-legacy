# Principles

Legend's Legacy is played for hours at a time: reading numbers, comparing builds, running systems that feed each other. Every screen must be dense enough to work in and still read as the grimoire. These eight principles settle that tension. When two pull apart, the priority order decides; the checklist applies them to any new component or screen.

## 1. Decisions first

**Lead with what the player must decide next.**

- **In practice:** the first thing read on a screen is the next choice — the objective, the action that commits, what is ready to claim. Reference and history follow.
- **Resolves:** completeness against focus. Everything stays on the screen, ordered by decision rather than by size or by system.
- **Example:** the Overview opens with the JourneyCard — "Recommended now" and one `solid` action — before the Combat Profile and the four attribute Ledgers.

## 2. Truth over atmosphere

**What the game knows, the player can see: every value visible, labelled and explainable.**

- **In practice:** no value is hidden, rounded away or dimmed for the look. Each has a plain label and an explanation one hover or focus away; an abbreviation keeps its exact figure a hover away.
- **Resolves:** mood against legibility. Atmosphere lives in the frame — ground, gilt, lore — never in place of a number.
- **Example:** each Combat Attribute row shows its value, its equipment rating beneath ("240 Armor Rating") and its formula on hover and focus. The constellation Overview was dropped because it hid them (D-008).

## 3. Dense, not crowded

**Density comes from alignment, type and rhythm, not from more boxes.**

- **In practice:** align values on shared columns, set them in the numeral face, join labels to values with leaders, keep the 4px rhythm. Group with a heading or a SectionRule band before reaching for a Panel; never nest Panels.
- **Resolves:** more information against more clutter. A screen holds more by having fewer frames.
- **Example:** the four Combat Attribute Ledgers share one grid — muted labels, gilt numerals, dotted leaders — dozens of values and not one extra border.

## 4. One meaning per signal

**In any one context, a colour, shape or position means one thing.**

- **In practice:** keep each signal to its job — rarity colours mean rarity and carry their code, `danger` means loss or risk, the crown means Nobility, the Folio's column holds the selected thing. Something new gets its own signal; an old one is never overloaded. Every colour meaning is also carried by a word, code or number.
- **Resolves:** expressiveness against learnability. A small vocabulary, learned once, still reads at a glance hours later.
- **Example:** the crown marks active Nobility and nothing else, so a player scanning the guild's member list knows exactly what it says (Registries · Nobility mark).

## 5. Mechanics give identity

**The system draws the screen: a dungeon route, a Tower ascent or an order book shapes it more than ornament does.**

- **In practice:** take a screen's structure from its mechanic — a route or an ascent is a Track, an order book is aligned columns of orders, attributes are Ledgers. Ornament and art come after, at the edges.
- **Resolves:** distinctiveness against consistency. Screens differ because their systems differ, while every part stays shared.
- **Example:** a World Tower climb reads as a Track of floors with the current floor ringed — the same Track the TopBar's centre carries while the climb is under way.

## 6. Art enhances, never carries

**Every screen works without illustration.**

- **In practice:** names, codes and numbers carry every fact. Art is optional in the Stage, the Banner and every slot, and text over art takes `shadow-text-art`. Remove every image and only mood is lost.
- **Resolves:** atmosphere against what can be drawn. The game looks finished before it has an art pipeline.
- **Example:** Essences are text-first until there is an art strategy (D-003): a loadout slot shows the Essences icon, the Essence's name in its rarity colour, its code and its two abilities.

## 7. Calm until it matters

**Attention is a budget. Spend it on risk, rewards and required action.**

- **In practice:** a resting screen is quiet — ink, muted labels, no motion. Emphasis — a gilt fill, arcana badges, `danger`, motion — is kept for what is at risk, what was won and what the player must do. One `solid` button per screen.
- **Resolves:** liveliness against fatigue over long sessions. If everything shouts, nothing is heard.
- **Example:** loot arrives as one quiet line in the Chronicle's Loot channel, not a pop-up (D-005); only a line that mentions you takes the gilt wash.

## 8. Built to grow

**A new system is a registry entry, not a new visual language.**

- **In practice:** a new rarity, channel, currency, damage type or mark goes into Registries first, with its token. A new screen starts from an archetype; a new part is built from existing ones.
- **Resolves:** novelty against coherence. The game keeps adding systems; the grimoire stays one book.
- **Example:** a new chat channel is one row in Registries · Chronicle channels and one `channel-*` token aliasing a palette primitive (Foundations · Colour) — the Chronicle needs no new styles.

## When principles conflict

The higher rule wins.

1. **Truth over atmosphere.** A value is never hidden or dimmed for the look.
2. **Decisions over completeness.** When everything cannot lead, the next decision leads and the rest follows — lower on the screen, never removed.
3. **Clarity over expressiveness.** One meaning per signal beats a richer visual vocabulary.
4. **Coherence over novelty.** A registry entry and existing parts beat a new component or style.
5. **Calm over emphasis.** When two things compete for attention, risk, rewards and required action win; the other goes quiet.
6. **Density over decoration.** A value beats a frame, an ornament or empty space; alignment beats a box.
7. **Mechanics over ornament.** The system's own shape beats a decorative layout.
8. **Art last.** Art fills what is left, and is the first thing cut when space or clarity runs short.

## One screen, one subject

The Shell's rule — the stage shows one subject, the Folio explains the selected thing — is *Decisions first* on a screen about one thing (D-010).

- **Applies to detail and collection screens:** a character on the Overview, a creature, the Creature Archive and every other archive. One subject on the stage, at most one selection in the Folio (D-004).
- **Does not apply to dense workbench screens:** the Cinder Bazaar, with listings, buy orders and your own orders side by side; the guild's member list, with facts and actions on every row. There the player works across many things at once — *Decisions first* orders the actions and *Dense, not crowded* lays out the rest. The rest of the Shell still holds: nothing else floats, and there is one Folio at most.

## Review checklist

Run it on every new component or screen, then the checklist in Principles · Anti-generic guardrails.

1. What must the player decide here — and is it the first thing they read?
2. Is every value the game knows here visible, labelled, and explained on hover and focus?
3. Is density made by alignment, the numeral face and rhythm — is there any box a heading or rule could replace?
4. Does each colour, shape and position mean one thing here, and is every colour meaning also a word, code or number?
5. Does the screen take its shape from its mechanic rather than from decoration?
6. With every image removed, is anything lost but mood?
7. Is emphasis spent only on risk, rewards and required action — with one `solid` button at most?
8. Is everything new a registry entry, or built from existing parts?
9. Detail, collection or workbench — and is it laid out as one?
