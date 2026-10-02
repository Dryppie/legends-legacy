# Components

The interface parts, in two lists. **Components** are generic: they have no game vocabulary of their own and take the game's words and numbers as content, so the same Ledger shows attributes, a combat summary or a price list. **Game Components** carry the game's own vocabulary — levels, sigils, attributes, items, Essences, currencies, the player's journey — and are built from Components, Foundations and Registries. The frame is in Shell.

## Rules

**Must**
- Read Tier 2 and Tier 3 roles and their own component tokens — never a palette primitive or a raw value (Foundations · Colour).
- Read type from the ramp — the `text-*`, `leading-*` and `tracking-*` tokens — never a pixel size, line height or letter-spacing (Foundations · Typography).
- Take row heights, paddings, gaps, icon and control sizes from the density variables (`--lg-row`, `--lg-cell-*`, `--lg-gap`, `--lg-inset*`, `--lg-icon`, `--lg-control`), so a region's `data-density` reaches every part inside it (Foundations · Space & Density).
- Use the `lg-` class prefix (Angular selectors `lg-*`), so components live beside today's `ll-` classes while screens migrate.
- Have a README beside its code that opens with a plain subtitle and a status (Governance · Templates), a showcase entry with a story for each state and variant it supports (D-129), and a spec that pins its behaviour through its harness (D-130).
- Meet every Standard: states, hierarchy, disclosure, art-optional, content. Show every state by the channel and words in Standards · States, list the states it has in its README's Supported states, and keep a blocked control focusable with its reason (D-087).
- Meet Foundations · Accessibility: rem sizes, the keyboard model, 24px targets, hover content that opens on focus and pins on tap, named controls, and Accessibility notes in the README.

**Should**
- Keep evocative names with their plain subtitle; give new components plain descriptive names.
- Format numbers with the shared helpers (`LG_FORMAT`, `lgFormatNumber`, …) or take them already formatted; never with a formatter of their own (Foundations · Numerals).
- Reflow on their container's width, not the window's.

**Never**
- Hard-code a game list — take it from Registries.
- Copy a component under a new name to change one detail; add a documented variant instead.

## Components

Every component starts at Draft (Governance defines the statuses). They are grouped as "Components · <family>"; the README's index gives each one's folder in `src/app/grimoire/`.

| Component | Plain subtitle | Family | Status |
| --- | --- | --- | --- |
| Button | the command button | Actions & input | Draft |
| SearchField | search with suggestions | Actions & input | Draft |
| TabStrip | the tabs | Actions & input | Draft |
| Heading | the titles | Type & ornament | Draft |
| Icon | the game's icon set | Type & ornament | Draft |
| SectionRule | the dividers | Type & ornament | Draft |
| Key | the key cap | Type & ornament | Draft |
| Ledger | the labelled value list | Data | Draft |
| Meter | the progress bar | Data | Draft |
| StatTile | the compact stat | Data | Draft |
| Delta | the stat change | Data | Draft |
| StatFigure | the headline number | Data | Draft |
| Track | the milestone track | Data | Draft |
| Num | a number with its unit | Data | Draft |
| Panel | the content box | Containers | Draft |
| Banner | the headline block | Containers | Draft |
| PageHeader | the information screen heading | Containers | Draft |
| Notice | the persistent notice | Containers | Draft |
| EntryList | the browsable name list | Lists & labels | Draft |
| ListRow | the list row | Lists & labels | Draft |
| Tag | the status label | Lists & labels | Draft |
| Presence | the online status | Lists & labels | Draft |

## Game Components

They are grouped as "Game Components · <family>".

| Component | Plain subtitle | Family | Status |
| --- | --- | --- | --- |
| LevelPlate | the level display | Character | Draft |
| ProfileIdentity | who a player is | Character | Draft |
| Sigil | the hex stat badge | Character | Draft |
| Constellation | the stat star chart | Character | Draft |
| Emblem | the attribute sign | Character | Draft |
| JourneyCard | the next-step guide | Character | Draft |
| ItemSlot | the item frame | Items & economy | Draft |
| ItemLink | an item named in text | Items & economy | Draft |
| LoadoutSlot | an Essence loadout slot | Items & economy | Draft |
| CurrencyPill | the currency amount | Items & economy | Draft |

### Rules for Game Components

**Must**
- Take names, codes and colours from Registries: the rarity code always accompanies the rarity colour (Decision D-006).
- Work text-first: equipment and Essences show their name, rarity code and meta as text, and art is optional (Decision D-003, Standards · Art-optional).
- Map onto one game concept, using the game's own nouns and labels.

**Should**
- Compose generic Components rather than redrawing them — LoadoutSlot uses ItemSlot and Tag; LevelPlate uses a Meter.
- Keep to the counts the game shows: one LevelPlate per detail view, Sigil values of up to three characters, five to seven Constellation items.

**Never**
- Invent a rarity, channel, currency or mark inside a component.
- Show someone else's journey: the JourneyCard is for the player's own profile only.

## Tokens used

All of them, through the Foundations; no component defines a colour, size, radius or shadow of its own. Game Components lean on these:

| Token | Role here |
| --- | --- |
| `rarity-*` | Item names, slot edges and rarity tags |
| `sigil-fill`, `sigil-edge`, `on-sigil` | Sigils |
| `gilt` | Emblem strokes and frames (brand), the headline figure — never values or selection (D-015) |
| `arcana`, `arcana-glow` | Ready, new, actionable and selected states — not in item contexts, where rarity owns hue (D-017) |
| `meter-*` | Level and essence progress |
| `delta-*`, `effect-*` | Comparisons, upgrade previews and conditions, through Delta and Tag |

## Do and don't

| Do | Don't |
| --- | --- |
| A Ledger for Combat Attributes and another for Market fees. | A new "AttributeList" that is a Ledger with a different name. |
| A Panel for secondary content and the Folio for the selection. | Panels inside Panels. |
| A `quiet` Button for Cancel. | A new grey button colour. |
| An ItemSlot with the item's name, its code and its meta, and an icon until there is art. | An ItemSlot that only shows a picture. |
| The JourneyCard on your own Overview. | The JourneyCard while viewing Maren's profile. |
| A StatTile for "1,284". | A Sigil stretched to fit "1,284". |

## Related sections

- Registries — the lists Game Components show
- Patterns — compositions of these parts
- Standards · Art-optional — text-first presentation
- Governance · Audit & consolidation map — what to keep, revise, merge or retire
- Governance — naming, statuses and the component README template
