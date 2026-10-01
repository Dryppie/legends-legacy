# Foundations · Typography

Four families, each with one job, and a ramp of 27 styles organised by role. The ramp is sized for a data-dense RPG: it covers order books, attribute tables and loadouts as well as titles and lore. Every size, line height and letter-spacing in the component styles reads a token from this ramp. How numbers are formatted is in Foundations · Numerals. TypeRamp shows every style, and TypeSpecimen shows the styles in use on a Cinder Bazaar screen and a Combat Attributes table.

## Rules

**Must**
- Use the ramp. Components read the `text-*`, `leading-*` and `tracking-*` tokens, never a pixel value. The only relative sizes are marks sized to the text around them, such as a Tag's ✓ and a Delta's triangle, and the Sigil numeral, which scales with its hex.
- Set anything a player reads at 12px or larger. Only rarity codes and key caps may use 11px (`code`), and always in bold capitals.
- Set Marcellus at 15px or larger. Anything smaller is Barlow.
- Set numbers in Barlow Condensed (`numeral-stat`, `numeral-row`, `numeral-compact`, `sigil-numeral`) or Barlow, in tabular lining figures. Marcellus sets only two numbers: the StatFigure value and the LevelPlate level (Foundations · Numerals, D-034).
- Give a dense row its row line height and running text its prose line height (see Line heights below).
- Set mechanics in `body` or `body-compact` (Barlow) and lore in `lore` or `lore-sm` (EB Garamond italic). Never mix them in one line.
- Honour the reading-font setting: `data-reading-font="readable"` on the root maps every role to Atkinson Hyperlegible, and `"system"` to the platform's UI font (`font-system`).
- Honour the reading-size setting: every size and line height is in rem, so `data-reading-font-size` (115% or 130%) scales the whole ramp (Foundations · Accessibility · Text scaling).

**Should**
- Choose the style by role, not by size. A player's name in a header is `name-header`, even though `title-md` is the same size.
- Use `body-compact` for tables, dense lists and the Chronicle, and `body` for running text in panels and the Folio.
- Emphasise key nouns in lore with weight 500 in `ink`, not with colour.
- Write labels in sentence case ("View breakdown"). CSS sets the capitals for navigation, tabs, section bands, labels and tags.

**Never**
- Use `title-xl` or `level-numeral` more than once on a screen, or inside a list.
- Set a number in Marcellus other than the StatFigure value and the LevelPlate level.
- Set a label of more than three words in tracked uppercase. Set a longer label in sentence case, in `body-compact-strong` or `caption-strong`.
- Type capitals by hand.
- Put numbers in lore.
- Invent a size, line height or letter-spacing.

## Families

| Family token | Face | Job | Sizes in the ramp |
| --- | --- | --- | --- |
| `display` | Marcellus | Engraved Roman capitals for titles, entity names, primary tabs, the one headline figure and the level | 17px and up; never below 15px |
| `lore` | EB Garamond italic | The narrator | 16 and 18px |
| `ui` | Barlow | Everything the player operates: body, labels, captions, buttons, navigation | 11–15px |
| `numeral` | Barlow Condensed | Every number that is not the headline figure or the level | 14–22px |
| `readable` | Atkinson Hyperlegible | The reading-font setting: Readable sans | 16px |
| `system` | The platform's UI font (`system-ui`) | The reading-font setting: System | Every size |

## The ramp

The ramp has ten groups, organised by role. The tokens are in rem; sizes and line heights here are in px at the default text size (1rem = 16px), and grow 15% or 30% with the reading-size setting. "Row" is the line height inside a dense row where it differs from the prose line height. Titles and `name-header` take `tracking-display`, and capitals take `tracking-caps` unless noted.

**Display**

| Style | Face | Size / line | Used for |
| --- | --- | --- | --- |
| `level-numeral` | Marcellus in `gilt`, `tracking-hero` | 84 / 76 | The LevelPlate's level. Once per screen, never in a list. |
| `title-xl` | Marcellus | 56 / 60 | The Folio title: the one thing the screen is about. Once per screen, never in a list. |
| `title-lg` | Marcellus | 36 / 40 | Screen titles (PageHeader, the `screen` Heading), the Banner name, the small StatFigure |

**Section titles**

| Style | Face | Size / line | Used for |
| --- | --- | --- | --- |
| `title-md` | Marcellus | 24 / 30 | Sections inside a screen, such as Combat Attributes and Order book (the `section` Heading) |
| `title-sm` | Marcellus | 20 / 26 | Sub-sections, tooltip titles and the small Sigil's label (the `subsection` Heading) |

**Entity names**: items, creatures, Essences and players

| Style | Face | Size / line | Used for |
| --- | --- | --- | --- |
| `name-header` | Marcellus | 24 / 30 | A name that leads: the TopBar, the item on sale in the Bazaar, a creature's header, EntryList entries |
| `name-row` | Marcellus | 17 / 22 | A name in a row: a Bazaar seller, a recent trade, a LoadoutSlot's Essence, a party member. An item takes its rarity colour and its code. |

**Body**

| Style | Face | Size / line | Used for |
| --- | --- | --- | --- |
| `body` | Barlow | 15 / 22 · row 20 | Mechanics and running text; the md Button label (at 600) |
| `body-strong` | Barlow 600 | 15 / 22 · row 20 | Emphasis in running text |
| `body-compact` | Barlow | 14 / 20 · row 18 | Tables, dense lists, the Chronicle, inputs, Ledger labels |
| `body-compact-strong` | Barlow 600 | 14 / 20 · row 18 | Emphasis in dense text, the sm Button label, names under an item slot, Meter labels |

**Lore**

| Style | Face | Size / line | Used for |
| --- | --- | --- | --- |
| `lore` | EB Garamond italic | 18 / 28 | Folio descriptions, quest intros, item and creature lore |
| `lore-sm` | EB Garamond italic | 16 / 24 · row 20 | Lore in rows, tooltips and panels, and the Chronicle's system and loot lines |

**Labels and captions**

| Style | Face | Size / line | Used for |
| --- | --- | --- | --- |
| `label` | Barlow 600, capitals | 12 / 16 | Group labels, eyebrows, Panel heads, table column heads and Tags. Three words at most. |
| `caption` | Barlow | 12 / 16 | Sub-lines, timestamps, expiry times and meter text. The floor for anything a player reads. |
| `caption-strong` | Barlow 600 | 12 / 16 | Counts in badges, footnotes, Presence |
| `code` | Barlow 700, capitals, `tracking-code` | 11 / 12 | Rarity codes and key caps only |

**Controls**

| Style | Face | Size / line | Used for |
| --- | --- | --- | --- |
| `tab` | Marcellus, capitals | 17 / 24 | Primary TabStrip labels. Secondary tabs use `label`. |
| `nav` | Barlow 500, capitals, `tracking-nav` | 13 / 18 | NavRail items |
| `nav-active` | Barlow 600, capitals, `tracking-nav` | 13 / 18 | The current NavRail item, beside the gilt diamond |

**Numerals**

| Style | Face | Size / line | Used for |
| --- | --- | --- | --- |
| `numeral-headline` | Marcellus in `gilt`, `tracking-headline` | 64 / 64 | The screen's one headline figure (StatFigure). Never in a table. |
| `numeral-stat` | Barlow Condensed 700 | 22 / 24 | StatTile values |
| `numeral-row` | Barlow Condensed 600 | 18 / 20 | Ledger values, Meter values, side stats, currency amounts |
| `numeral-compact` | Barlow Condensed 600 | 14 / 18 | Table cells and dense rows: prices, quantities and totals in the order book, ItemSlot quantities, the StatTile suffix |

**Component**

| Style | Face | Size / line | Used for |
| --- | --- | --- | --- |
| `sigil-label` | Marcellus | 30 / 34 | Constellation labels beside a Sigil, over stage art |
| `sigil-numeral` | Barlow Condensed 700 | 22 / 24 at the md Sigil | The number inside a Sigil, in tabular figures (D-034). It is 0.42 of the hex, so it has no size token. |

**Readable**

| Style | Face | Size / line | Used for |
| --- | --- | --- | --- |
| `body-readable` | Atkinson Hyperlegible | 16 / 24 | Body text under the reading-font setting |

## Line heights

There are two sets:

- **Prose** is text that wraps and is read as a paragraph: descriptions, lore, tooltips and summaries.
- **Rows** are text set on one line beside its neighbours: a Ledger row, an order-book row, a Chronicle line, a Button.

| Style | Prose | Row |
| --- | --- | --- |
| `body`, `body-strong` | `leading-body`, 22px | `leading-body-row`, 20px |
| `body-compact`, `body-compact-strong` | `leading-body-compact`, 20px | `leading-body-compact-row`, 18px |
| `lore-sm` | `leading-lore-sm`, 24px | `leading-lore-sm-row`, 20px |

Every other style sits on one line and has a single line height, `leading-<style>`. `leading-mark` (1) is for key caps, counts, badges and glyphs that must add no height.

## Marcellus or Barlow at small sizes

Marcellus runs below 17px in three places. They were evaluated against the 15px floor and for legibility in dense rows. Marcellus has one weight (400), low contrast and small counters. Below about 15px its letters close up, and it cannot be set in bold to hold its own beside Barlow numbers. Decision D-030.

| Part | Was | Now | Why |
| --- | --- | --- | --- |
| Button labels | Marcellus 16px (md), 14px (sm) | Barlow 600: `body` on the 20px row leading (md) and `body-compact` on 18px (sm), `tracking-button` | The sm label broke the 15px floor, and one face should serve both sizes. Buttons sit in dense rows beside Barlow Condensed numbers (the order book's Buy). At the same size, Barlow 600 is heavier and has a larger x-height. This follows Clarity over expressiveness (Principles). |
| EntryList items | Marcellus 24px | Stays Marcellus, at `name-header` 24 / 30 | It is a list of names — creatures, guild members, prophecies — over stage art, read one name at a time rather than scanned as data. At 24px Marcellus is legible and carries the scene's voice. The dense row it will merge into (ListRow in the Audit) uses `name-row` and Barlow. |
| The current NavRail item | Marcellus 13px | Barlow 600 at the `nav` size (`nav-active`) | 13px is below the Marcellus floor. The current item now changes weight instead of face, and the gilt diamond still marks it. |

## Size map

Every font size that was typed by hand in the component styles, and the style it reads now (D-031). The whole-pixel changes are visible: 10 and 11px text rises to 12px, apart from codes and key caps.

| Was | Now | Parts |
| --- | --- | --- |
| 10px | `code` (11) | ItemSlot rarity code |
| 10px | `label` (12) | LoadoutSlot ability labels |
| 11px | `code` | Key cap |
| 11px | `label` (12) | Tag, CurrencyPill name, EntryList lock, NavRail group label, Chronicle tag and prefix, JourneyCard label, LoadoutSlot slot number |
| 11px | `caption` (12) | StatTile delta, NavRail badge, TabStrip count, Chronicle unread count |
| 12px | `label` | Eyebrows (TopBar, PageHeader, Folio), Panel head, SectionRule label, Ledger title, StatFigure label, JourneyCard phase, LevelPlate kicker, Track end, secondary tabs, Chronicle channel and collapsed strip |
| 12px | `caption` | KeyHints, LevelPlate EXP text, SectionRule aside, ItemSlot meta, Chronicle time, SearchField note, StatFigure caption, Ledger sub-line and tip meta, LoadoutSlot abilities, Presence |
| 12px | `nav` (13) | NavRail items |
| 13px | `label` (12) | StatTile label |
| 13px | `body-compact` (14) | ItemSlot name, CurrencyPill, link Button, Ledger tip text, locked LoadoutSlot name |
| 13px | `numeral-compact` (14) | ItemSlot quantity |
| 13px | `nav-active` | The current NavRail item. Its size rule is gone; it reads `nav`. |
| 14px | `body-compact` | sm Button, Meter label, LevelPlate side stats, Chronicle text and input, input field, SearchField option, Ledger label |
| 14px | `numeral-compact` | StatTile suffix |
| 15px | `body` | Root text, Folio effects |
| 15px | `lore-sm` (16) | Chronicle system and loot lines |
| 16px | `body` (15) | md Button |
| 17px | `tab` | Primary tab (a dead 20px duplicate removed) |
| 17px | `name-row` | LoadoutSlot name |
| 17px | `numeral-row` (18) | CurrencyPill amount |
| 18px | `numeral-row` | Meter value, LevelPlate side value, Ledger value |
| 18px | `lore` | Folio lore |
| 18px | `title-sm` (20) | Ledger tooltip title |
| 20px | `title-sm` | Small Sigil label |
| 20px | `body-strong` (15) | JourneyCard next unlock: it carries a number, so it left Marcellus (D-034) |
| 22px | `title-md` (24) | `section` Heading |
| 22px | `name-header` (24) | TopBar name |
| 22px | `numeral-stat` | StatTile value |
| 24px | `name-header` | EntryList items |
| 30px | `sigil-label` | Sigil label |
| 36px | `title-lg` | `screen` Heading, small StatFigure value, large Sigil label |
| 56px | `title-xl` | `folio` Heading |
| 64px | `numeral-headline` | StatFigure value |
| 84px (and a dead 96px) | `level-numeral` | LevelPlate numeral |

Letter-spacing has also moved onto tokens:

- Capitals used seven trackings from 0.1em to 0.24em. They now use `tracking-caps` (0.14em), and NavRail uses `tracking-nav` (0.16em).
- Titles and header names use `tracking-display`.
- Every `line-height: 1` now reads `leading-mark`.

The previews read the same tokens.

## Tokens used

Each style's size and line height are `text-<style>` and `leading-<style>`. There are some exceptions:

- The `-strong` styles share their base style's tokens.
- `nav-active` shares `nav`'s tokens.
- `sigil-numeral` has no size token.

| Token | Value | Role here |
| --- | --- | --- |
| `text-*` | 11–84px, 22 tokens | Font sizes, one per style |
| `leading-*` | 12–76px, plus `leading-mark` (1) | Line heights, with `-row` variants for dense rows |
| `tracking-display` | 0.01em | Titles and header names in Marcellus |
| `tracking-hero` | −0.02em | `level-numeral` |
| `tracking-headline` | −0.01em | `numeral-headline` |
| `tracking-caps` | 0.14em | Tracked capitals: labels, tabs, Tags |
| `tracking-nav` | 0.16em | NavRail items |
| `tracking-code` | 0.06em | Rarity codes and key caps |
| `tracking-button` | 0.02em | Button labels |
| `font-display`, `font-lore`, `font-ui`, `font-numeral`, `font-readable` | The five faces | Families |

## Do and don't

| Do | Don't |
| --- | --- |
| Set an order-book price in `numeral-compact`, right-aligned. | Set the same price in Marcellus. |
| Head a column "Price each" in `label`. | Head it "Price per unit after fee" in tracked capitals. |
| Set a rarity code "UC" in `code`, 11px bold. | Set a timestamp at 11px. |
| Use one `title-xl` on the screen: the Folio's. | Set a list of Essences in `title-xl`. |
| Set a Buy button in Barlow 600. | Set a 13px Marcellus button label. |
| Set "+12% damage from equipment" in `body`. | Set the same line in lore italic. |
| Set "Temples older than the roads that lead to them." in `lore`. | Write "Temples, 300 years old, grant +5%.", a number in lore. |
| Type "View breakdown" and let CSS set tab capitals. | Type "VIEW BREAKDOWN". |

## Related components

- TypeRamp — every type style
- TypeSpecimen — the ramp in use
- Heading — the titles
- PageHeader — the screen title
- Button — the command button
- EntryList — the browsable name list
- NavRail — the main navigation
- Ledger — the labelled value list
- StatFigure — the headline number
- StatTile — the compact stat
- LevelPlate — the level display
- Folio — the detail panel
- Chronicle — chat and the game log
- TabStrip — the tabs
- Sigil — the hex stat badge
