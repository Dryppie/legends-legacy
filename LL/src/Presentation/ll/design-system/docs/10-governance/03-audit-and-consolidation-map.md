# Governance · Audit & consolidation map

An audit of all 34 components and the two showcase compositions, made on 30 September 2026 against their code and styles at the time, their pages, the Principles, the Anti-generic guardrails and D-012. Nothing was changed in this step; the revision queue at the end is the plan.

The verdicts — **keep**, **revise**, **merge**, **retire** — are audit calls, not lifecycle statuses: every part stays Draft until it ships (Governance · Statuses).

**Result:** 18 keep, 14 revise, 2 merge, 0 retire; both showcases revise.

## Component map

| Component | Purpose | Verdict | Issues |
| --- | --- | --- | --- |
| GameShell | The screen frame: rail, top bar, stage, Folio, Chronicle | revise | KeyHints float over the stage's foot, on top of Page content on information screens. The `hintbar-height` token is unused. Breakpoints (960, 1536px) and shell z-layers (1–4) are raw. |
| NavRail | The main navigation | revise | Locked items stayed in the tab order (`aria-disabled` plus `pointer-events: none`), against Standards · States. *Settled the other way: locked items stay in reach of pointer and keyboard, say "Locked" and give their condition in the reason tip (D-087).* The current item is marked in gilt while selection elsewhere is arcana — settled by D-015: the current item is the current location, gilt's job, not a selection. The 13px current item and 11px group labels are off the type scale (fixed: `nav-active` in Barlow and `label`, D-030, D-031). |
| TopBar | Who you are and what you carry | revise | Room for a name, an eyebrow and a few CurrencyPills only; the game's 15+ resources have nowhere to go. The title is Marcellus 22/28, a size no type style defines (now `name-header`, D-031). The menu button has focus but no hover state. |
| Page | The information screen frame | keep | Clean since D-012. `maxWidth` now defaults to `page-max` (D-053), and the Page is a layout region (D-050). |
| PageHeader | The information screen heading | keep | Its hex icon is the one allowed icon beside a heading. *The hexagon is now a gilt diamond — the current location — so it no longer reads the Sigil's tokens (D-065).* The 52px icon and eyebrow tracking are raw. |
| Stage | The scene backdrop | keep | Art-led screens only. The art treatment (sepia, saturate, brightness, blur) is raw. |
| Banner | The headline identity block | revise | Two edges at once — a `line` border and an inset gilt frame at 28% (Excessive borders). *Now one edge: the double gilt frame, as on the Folio (D-061).* In ScreenOverview it holds two display-size numbers. The art treatment and veil stops are raw. |
| Folio | The detail panel | revise | No way to step its `title-xl` down when a LevelPlate is present (one display-size element per screen). No empty state for "nothing selected". Frame inset, corner size and opacities are raw. *Its film grain sat straight behind its body text; removed (D-072).* |
| Panel | The content box | keep | Radius is a raw 2px; the 30px header and 0.14em tracking are raw. |
| Ledger | The labelled value list | revise | Every value was gilt, so on a Ledger-heavy screen gilt marked nothing (values are `ink` since D-018). Needs a compact two-up variant with deltas to absorb StatTile. It owns the only accessible tooltip in the system, which should become the shared Tooltip. Row height, 2px radius and the 300px tooltip width are raw. |
| StatTile | The compact stat | merged → Ledger (D-137) | A boxed label and value — the equal-tile rows the guardrails warn against — duplicating the Ledger row. Its up-delta was text in `arcana-glow` (since D-026 it is a Delta coloured by polarity), which Foundations · Colour forbids. Its `line` inset edge framed every tile (now gone: the `tile` fill carries it, D-059). |
| StatFigure | The headline number | revise | The 64px default is off the type scale and a display-size element; beside a LevelPlate it must be `sm`. Its explanation is a native `title`, which keyboard users never see. |
| LevelPlate | The level display | keep | Three dead declarations: a 96px `ink` numeral overridden by the 84px gilt one. The vertical kicker's tracking is raw. |
| Meter | The progress bar | keep | Raw 1px radius on the bar (now square-ended, D-068); 4, 6 and 10px track heights are raw. |
| Track | The milestone track | keep | Step labels are native `title` tooltips; the 1.5px node border is raw (now `border-emphasis`, D-059). |
| Sigil | The hex stat badge | keep | Art-led screens only. The 30px label and `calc(size × 0.42)` value are off the scale. Its `filter` transition, a leftover of D-012, is gone: it scales over `duration-fast` (D-075). |
| Constellation | The stat star chart | keep | Art-led stages only — as the Overview it told the player nothing (D-008). Ring and node opacities are raw. *Its node diamonds are now ticks across their ring (D-065).* |
| Emblem | The attribute sign | keep | Decorative and within the ornament budget. Five raw opacities. |
| ItemSlot | The item frame | revise | Image-first: a 112px square built around its picture, with the name as a caption that truncates ("Ember Wolf Ess…"). With neither image nor icon it styles itself as empty, even when it has a name. The rarity code is 10px (now 11px `code`, D-031) and its tooltip native; it is a button with no hover state. *Hover is now the `surface-raised` wash, and it takes the state model: locked, unavailable, not owned, undiscovered, a word state and favourite (D-087).* |
| ItemLink | An item named in text | keep | Rarity and meta sit only in a native `title`. |
| LoadoutSlot | An Essence loadout slot | revise | A bordered box around an ItemSlot frame, inside a Panel: three enclosure levels (its padding now steps down to the inner inset, D-039; the enclosure remains). Ability labels are 10px (now 12px `label`, D-031). It is a list row in all but name. A slot that opens now has a `line-strong` edge and the `surface-raised` hover instead of `gilt-soft` (D-062). *Attuned is a `neutral` Tag and an open slot shows the empty frame (D-087).* |
| EntryList | The browsable name list | merge → ListRow `scene` | A list row with a scene look (24px Marcellus, star marker, fading ends; the star is gone, D-065). Every workbench screen needs the same row without the scene look. Its current item was gilt while other selections are arcana. *Now `ink` with the `arcana-glow` bar, the hover is `surface-raised`, and locked entries take focus but not selection (D-087).* |
| TabStrip | The tabs | keep | No disabled tab. A dead 20px size sits under the 17px primary tab (removed, D-031). |
| Tag | The status label | keep | 11px capitals and a 20px height are off the scale (the capitals are now `label` 12px, D-031; the height remains). Badge soup is a usage risk, not a defect. *A row's one Tag is now chosen by Standards · States · Combining states, and `state` gives every state its word (D-086).* |
| Button | The command button | revise | No loading state for actions that wait on the server (place an order, claim a reward). The 16px Marcellus label and 42 and 32px heights are raw (the label is now Barlow `body` and `body-compact`, D-030; the heights are now the control heights 44, 40 and 32px, D-038). Key caps are `radius-full` while the docs say `radius-sm`. *Pending is now a state, with a progressive label in reserved room, and blocked Buttons give their reason (D-087).* |
| SearchField | Search with suggestions | keep | No hover and no error state (the search failed). Its page calls it pill-shaped, but the field uses `radius-md`. *Now `radius-control`, like the Search button beside it, and the page says so (D-064, D-068).* |
| Presence | The online status | keep | The last-seen time sits in a native `title`; the dot uses 999px instead of `radius-full`. *Now `radius-circle`, the system's one circle (D-065, D-068).* |
| JourneyCard | The next-step guide | revise | Used the Folio surface, `shadow-panel` and a gilt frame at 25% — a second Folio on a screen that has none, and a frame past the ornament budget; now a Level 1 surface with no shadow or frame (D-055), and no edge (D-059). Its phase is styled as an eyebrow on a block the guardrails do not allow one. 11px and 20px text are off the scale (now `label` and `title-sm`, D-031). |
| KeyHints | The keyboard shortcut hints | revise | Floats over the stage on every screen that passes hints, including over Page content; in a mouse-first game it should appear only where real shortcuts exist. Its `hintbar-height` token is unused. |
| Chronicle | Chat and the game log | revise | No state for loading history, a failed send, a lost connection, or a muted or cooling-down composer. Text in four raw sizes (11, 12, 14 and 15px); the 50px time gutter is raw. |
| CurrencyPill | The currency amount | revise | Built and registered for two currencies with art, Cinders and Soulstones; the game has more than 15 resources and no registry for them. The 17px amount is off the numeral scale (now `numeral-row`, D-031), and the full figure sits in a native `title`. |
| Heading | The titles | keep | `section` is 22/28, which no type style defines; every size is a hand-typed copy of a style. |
| Icon | The game's icon set | keep | Fifteen sidebar icons and the Nobility crown — none yet for actions, resources, attributes, conditions, damage types, slots or statuses. Foundations · Iconography now sets the standard, the scale and the taxonomy, and its inventory lists 99 icons to draw (D-080). |
| SectionRule | The dividers | keep | The 26px band height and tracking are raw. |
| ScreenOverview | Showcase of ArchetypeInformation | revise | Three enclosure levels in the Essence Loadout; two display-size numbers in the Banner. 38 lines of preview-only CSS (27 `pv-` classes) hold parts no component provides: the profile meta list, the Nobility mark, the perks grid. Named before the `Archetype<Name>` rule. |
| ScreenArchive | Showcase of ArchetypeArchive | revise | The Folio's `title-xl` name plus a LevelPlate numeral: two display-size elements. A StatTile grid in the Folio. 18 lines of preview-only CSS; named before the rule. |

## States supported today

✓ supported · ✗ missing where the part needs it · — not applicable

| Component | Default | Hover | Focus | Selected | Disabled | Locked | Loading | Error |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| GameShell | ✓ | — | — | — | — | — | — | — |
| NavRail | ✓ | ✓ | ✓ | ✓ current | — | ✓ focusable, with its reason (D-087) | — | — |
| TopBar | ✓ | ✗ menu | ✓ | — | — | — | — | — |
| Page | ✓ | — | — | — | — | — | — | — |
| PageHeader | ✓ | — | — | — | — | — | — | — |
| Stage | ✓ | — | — | — | — | — | — | — |
| Banner | ✓ | — | — | — | — | — | — | — |
| Folio | ✓ | — | — | — | — | — | ✗ | — |
| Panel | ✓ | — | — | — | — | — | — | — |
| Ledger | ✓ | ✓ explained rows | ✓ | — | — | — | — | — |
| StatTile | ✓ | — | — | — | — | — | — | — |
| StatFigure | ✓ | native title only | ✗ | — | — | — | — | — |
| LevelPlate | ✓ | — | — | — | — | — | — | — |
| Meter | ✓ | — | — | — | — | — | — | — |
| Track | ✓ | native title only | — | ✓ current | — | — | — | — |
| Sigil | ✓ | ✓ | ✓ | ✓ | — | ✓ focusable, with its reason (D-087) | — | — |
| Constellation | ✓ | ✓ via Sigil | ✓ via Sigil | ✓ | — | ✓ via Sigil | — | — |
| Emblem | ✓ | — | — | — | — | — | — | — |
| ItemSlot | ✓ | ✓ (D-087) | ✓ | ✓ | ✓ blocked, with its reason | ✓ | — | — |
| ItemLink | ✓ | ✓ | ✓ | — | — | — | — | — |
| LoadoutSlot | ✓ | ✓ | ✓ | ✗ | — | ✓ focusable when it opens something | — | — |
| EntryList | ✓ | ✓ | ✓ | ✓ | — | ✓ focusable, with its reason (D-087) | — | — |
| TabStrip | ✓ | ✓ | ✓ | ✓ | ✗ | — | — | — |
| Tag | ✓ | — | — | — | — | — | — | — |
| Button | ✓ | ✓ | ✓ | — | ✓ rare; blocked states with a reason (D-087) | ✓ | ✓ pending | — |
| SearchField | ✓ | ✗ | ✓ | ✓ option | — | — | ✓ | ✗ |
| Presence | ✓ | native title only | — | — | — | — | — | — |
| JourneyCard | ✓ | — | — | — | — | — | — | — |
| KeyHints | ✓ | — | — | — | — | — | — | — |
| Chronicle | ✓ | ✓ | ✓ | ✓ channel | ✗ composer | — | ✗ | ✗ |
| CurrencyPill | ✓ | ✓ | ✓ | — | — | — | — | — |
| Heading | ✓ | — | — | — | — | — | — | — |
| Icon | ✓ | — | — | — | — | — | — | — |
| SectionRule | ✓ | — | — | — | — | — | — | — |

The showcases inherit their parts' states; the Overview's player search shows loading and "No matching players" but no error. Across the system, **loading exists once (SearchField) and error nowhere**; disabled exists only on Button and Sigil. *Since D-086 the full model is Standards · States, with a matrix of which component categories take which states; this table records what is built. Button now has pending, and blocked Buttons, Sigils, ItemSlots, EntryList entries and NavRail items keep focus and give their reason (D-087). Plain disabled is now only Button's.*

## Five ways to show a number

| Form | Shows | Justified when | Not when | Verdict |
| --- | --- | --- | --- | --- |
| Ledger row | A labelled value with leaders, an optional sub-line and an explanation | The default for any value with a label: attributes, prices, fees, members' figures — on every kind of screen | — | keep: the default |
| StatFigure | One headline number with its label and caption | The single number a screen is about (Combat Rating); `sm` beside a LevelPlate | A second display-size figure; anything in a list | keep, limited |
| LevelPlate | A level, its progress and up to two side stats | The first thing on a character or creature detail, where the level is identity | Levels in lists — write "Lv. 42" in the row | keep |
| Sigil | A value of up to three characters in a hex, placed on the stage | Art-led screens where position carries meaning, or a stat the player raises or selects | Workbench and information screens; values over three characters | keep, art-led only |
| StatTile | A boxed label and value, two-up | Nothing a compact two-up Ledger cannot do without the box | — | merge into Ledger |

After the merge: a labelled value is a Ledger row; the one number is a StatFigure; a level is a LevelPlate; a value with a place on the scene is a Sigil.

## Four containers and their boundaries

| Container | What it is | Holds | Never holds | Per screen |
| --- | --- | --- | --- | --- |
| Stage | The scene behind an art-led screen — a region's ground, not a box | The subject over its art: a list, a Constellation, a lore Panel | Boxed data groups; information or workbench content | One: it is the region |
| Folio | The detail region for the selection | The one selected thing: title, lore, effects, stats, one or two actions | Navigation, Panels, a second selection, a Banner beside it (D-070) | At most one (D-004) |
| Banner | The identity block of an information screen | Who or what the page is about, with at most one display-size figure | Section headers, promotions, dividers, a Folio beside it (D-070) | At most one, information screens only |
| Panel | A plain box for secondary content, told apart by its fill (no border, D-059) | Lore, pending loot, a biography, a loadout | Primary data (a Ledger on the ground does that), another Panel | As needed, within two enclosure levels |

Boundary problems found:

- **JourneyCard was a fifth, undeclared container.** It borrowed the Folio's surface and shadow and the Banner's gilt frame. It now sits at Panel level, Level 1: on `surface`, with no frame and no shadow (D-055).
- **LoadoutSlot is a container inside a Panel,** with a framed slot inside it — three levels.
- **Page is not a container** but the frame of an information screen; it holds the others and stays that way.

## EntryList and a general list row

**Yes — EntryList should become a variant of a general list row.** The game's lists — inventory, Bazaar listings and orders, guild members, achievements, loadouts — need one row: a leading mark (icon, slot, rank or presence), a primary name (rarity-coloured for items), a meta line, and trailing values, tags or actions. It needs hover, focus, selected, locked, new and disabled states and a compact density.

Build **ListRow** (a plain name, in Components · Lists & labels). EntryList becomes its `scene` variant — the 24px display names, the star marker and the fading ends — for art-led lists only. LoadoutSlot is rebuilt as a ListRow with a leading ItemSlot, which also removes its third enclosure level.

## ItemSlot and text-first items

ItemSlot is image-first: a square frame built around its picture, a rarity edge and a corner code, and the name as a caption beneath. Under D-003 that fails three ways: long names truncate, a slot with no image looks empty, and the code at 10px is the smallest text in the system.

Split it by job:

- **Text-first item (the default).** A ListRow for the item: the rarity-coloured name in full, its code, meta and quantity, with an Icon in the leading place until there is art. For inventory, the Bazaar and rewards.
- **Slot (positional).** The square frame, only where position is the meaning — equipment slots, loadouts. Empty means "no item", never "no image"; names wrap to two lines; the code is 11px or larger.

## CurrencyPill and the game's resources

CurrencyPill takes any name, amount and icon, but it is documented, registered and drawn for two currencies with art: Cinders and Soulstones. Registries · Resources now lists nine (D-085), each with a planned line icon for inline use (D-083); the Icon set has none of them yet but Soulstones, and the TopBar has no room for them.

- **Registries · Resources** exists (D-085): every resource with what it is, its art and line icon, its fallback and where it appears. Still to add: number format, and the materials that are items.
- Generalise the pill into **ResourceAmount**: the line icon, a numeral and the resource's name, with the exact figure on hover and focus through the shared Tooltip. Inline in rows and prices, text-first when there is no icon. The icon never replaces the name (D-083).
- CurrencyPill stays as ResourceAmount's TopBar variant for the currencies the TopBar shows — today Cinders and Soulstones. The rest open from the TopBar in a resources list, never as more pills.

## KeyHints in a mouse-first game

**No permanent bar.** Legend's Legacy is played with a mouse in a browser; a hints strip on every screen spends a corner of the stage — and covers Page content — on keys most players never press.

- Show shortcuts where they act: a Button's `hotkey` already draws its key cap and sets `aria-keyshortcuts`.
- Show KeyHints only on screens with real, screen-specific shortcuts — stepping through an archive, combat actions — three or four at most, and never over content.
- A keyboard help sheet, opened by one key, can list the rest. Remove `hintbar-height` or make the shell reserve it.

## Workbench and art-led screens

| Component | Workbench — Bazaar, guild members, inventory | Art-led — Creature Archive |
| --- | --- | --- |
| GameShell, NavRail, TopBar, Chronicle | ✓ | ✓ |
| Page, PageHeader | ✓ | — (uses Stage) |
| Stage | ✗ | ✓ |
| Banner | — (information screens only) | — |
| Folio | — the inspector sits in the content, beside the list (D-052) | ✓ `align="start"` |
| Panel | ✓ | ✓ lore |
| Ledger | ✓ the core part | ✓ compact, in the Folio |
| StatTile | ✗ (merged into Ledger, D-137) | ✗ (merged into Ledger, D-137) |
| StatFigure | ✓ at most one | ✓ `sm` |
| LevelPlate | ✗ | ✓ in the Folio |
| Meter, Track | ✓ | ✓ |
| Sigil, Constellation, Emblem | ✗ | ✓ |
| ItemSlot | ✓ once text-first; slot for equipment | ✓ |
| ItemLink, Tag, TabStrip, SearchField, Button, Heading, Icon | ✓ | ✓ |
| SectionRule | ✓ band and hairline | ✓ ornament in the Folio |
| LoadoutSlot | ✓ once rebuilt on ListRow | — |
| EntryList | ✗ use ListRow | ✓ `scene` |
| Presence | ✓ guild members | — |
| JourneyCard | ✗ Overview only | ✗ |
| KeyHints | only with real shortcuts | ✓ stepping through entries |
| CurrencyPill | ✓ as ResourceAmount in prices | — |

**Missing for workbench screens:** a Table (sortable columns, aligned numerals, sticky header), ListRow (built, D-040), Select, NumberField or quantity stepper, Checkbox and Toggle (Settings' "Display Nobility" is a checkbox today), a shared Tooltip, Dialog and confirm (Sell, Abandon, Leave guild), pagination or a virtualised list, empty and error blocks, and a loading skeleton. The system has no parts yet for the Bazaar, the guild's member list or the inventory beyond Ledgers and Panels.

## Hard-coded values

Values in the component styles that are not tokens, grouped by kind. The Angular edition's styles carry the same values.

### Type sizes

*A snapshot from before D-029 to D-031, kept as the audit found it. Every size, line height and letter-spacing below now reads a token from the ramp in Foundations · Typography; its Size map says where each one went. The only relative sizes left are the Tag and Delta glyphs and the Sigil numeral. Weights are still typed as part of each style.*

The file has **19 distinct font sizes** (18 in effect — 96px is overridden) against **17 type styles**, which use only 11 sizes. No component reads a type style: every size is typed by hand, even where it matches one.

| Size | On the scale? | Used by |
| --- | --- | --- |
| 10px | No | ItemSlot code; LoadoutSlot ability labels |
| 11px | No — 13 uses, a de facto micro label | Chronicle prefix, tag and unread count; CurrencyPill name; EntryList lock; JourneyCard label; key cap; LoadoutSlot slot; NavRail badge and group label; StatTile delta; TabStrip count; Tag |
| 12px | Yes (`label`, `caption`) — 25 uses with eight different trackings | Eyebrows, captions, Ledger titles and sub-lines, key hints, Chronicle channel and time, and more |
| 13px | Yes (`nav`) | link Button, CurrencyPill, Ledger tip text, locked LoadoutSlot name, NavRail current item, ItemSlot name and quantity, StatTile label |
| 14px | No | small Button, Chronicle text and input, input field, Ledger label, LevelPlate side stats, Meter label, SearchField option, StatTile suffix |
| 15px | Yes (`body`) | root text, Folio effects, Chronicle system lines |
| 16px | Yes (`lore-sm`), but used for Marcellus | Button label |
| 17px | Yes (`tab`) | primary tab, CurrencyPill amount, LoadoutSlot name |
| 18px | Yes (`lore`, `numeral-sm`) | Folio lore, Ledger value, Ledger tip title, LevelPlate side value, Meter value |
| 20px | No | JourneyCard unlock, small Sigil label; a dead primary-tab size |
| 22px | Yes (`sigil-numeral`, `numeral-md`), but used for headings at 22/28 | `section` Heading, StatTile value, TopBar title |
| 24px | No | EntryList names |
| 30px | Yes (`sigil-label`) | Sigil label |
| 36px | Yes (`title-lg`) | `screen` Heading, small StatFigure, large Sigil label |
| 56px | Yes (`title-xl`) | `folio` Heading |
| 64px | No | StatFigure value |
| 84px | Yes (`level-numeral`) | LevelPlate numeral |
| 96px | No — dead | LevelPlate numeral, overridden |
| `calc(size × 0.42)` | No | Sigil value |

The showcase previews add 9, 11, 12, 13, 14 and 16px of their own.

### Other type metrics

- **Line heights:** 17 distinct values — 1, 16, 17, 18, 19, 20, 22, 24, 26, 28, 32, 34, 40, 60, 64, 76 and 80px — none tied to a style.
- **Letter-spacing:** 14 raw values from −0.02em to 0.24em; uppercase labels alone use seven (0.1, 0.12, 0.14, 0.16, 0.18, 0.22, 0.24em).
- **Weights:** 400, 500, 600 and 700 typed directly, 36 of them 600.

### Radii

- `2px` in 11 rules — Panel, Ledger row, SectionRule band, link Button, JourneyCard objective, Chronicle line, the Chronicle channel and ItemLink focus rings, and ItemSlot's code, quantity and empty frame. No token is 2px (`radius-sm` is 4px).
- `1px` on the Meter bar's track and fill.
- `999px` on the Presence dot, instead of `radius-full`.
- `0` (the floating Chronicle in the bottom dock) and `inherit` (the Meter fill) are resets, not values.

*Now every radius reads a role token — `radius-container`, `radius-control`, `radius-float` or `radius-circle` — and none of the raw values remain; Meters are square-ended (D-068).*

### Sizes and spacing

- **Control heights:** nine, none tokens — 20 (Tag), 22 (key cap), 30 (rail item, Panel header, CurrencyPill), 32 (small Button, Chronicle toggle), 34 (Chronicle input, Ledger row), 38 (input), 40 (primary tab, EntryList item, Chronicle header, compact rail item) and 42px (Button). *Since D-038 the Button, input, primary tab, Ledger row and EntryList item read the density tokens (`control-*`, `row-*`); Tag, key cap, rail item, Panel header and CurrencyPill are still fixed.*
- **Component sizes:** ItemSlot 112, 64 and 176px; the tooltip 300px; the TopBar centre 180–420px; Page 1280px; the rail drawer 300px; Folio corners 44px and Banner corners 36px; the PageHeader icon 52px; the JourneyCard aside 220px. *Since D-044 these, the control heights and the spacing values are rem; only 1–3px lines, radii and shadows stay px.*
- **Spacing off the space scale:** 22 raw values, mostly 1–6px optical offsets (6px 19 times, 2px 11 times, 4px 7 times). The larger ones: the Chronicle's 50px time gutter and −44px offset, SearchField's 36px input padding, EntryList's 40px fade, the −12px art bleed, the Folio frame's 10px inset and the Banner and JourneyCard frames' 8px.

### Everything else

- **Opacity:** eleven raw values (0 to 1, including 0.25, 0.28, 0.45, 0.5, 0.6, 0.7, 0.8, 0.85, 0.9); Emblem alone uses five.
- **Motion:** 140, 160, 180, 220 and 400ms, `ease` and one cubic-bezier, now five duration and three easing tokens in `tokens.json` · `motion` (D-074). Every transition reads them and moves only `transform` and `opacity`; Sigil's `filter` transition and the colour transitions on Button, NavRail and EntryList are gone (D-075).
- **Z-index:** the shell's layers 1, 2, 3, 4, −1 and −2; overlays use the `z-*` tokens. *Now every z-index reads a `z-*` token; only art inside an isolated Stage or Banner keeps −1 and −2 (D-056).*
- **Breakpoints:** container widths 1536, 1100, 960, 900, 700, 520 and 120px. *Now rem (96, 68.75, 60, 56.25, 43.75, 32.5 and 7.5rem), so they follow the reading-size setting (D-044). Content layouts now read four content tiers of their region: 68, 44 and 32rem (D-050); the Banner, JourneyCard and PageHeader moved onto them.*
- **Colour:** one raw `#000`, in EntryList's fade mask (harmless — masks read alpha), and eight `color-mix` tints outside the tokens: `gilt` 30%, `ground` 72%, `ground-deep` 35, 40, 60, 70 and 78%, `surface-raised` 30%.
- **Filters:** two different art treatments (Stage and Banner), and `brightness(1.06)` on a hovered solid Button.
- **Borders:** 1.5px on the Track node and Presence dot beside the 1px norm. *Now every line reads `border-hairline` or `border-emphasis`: the Track diamonds are 2px, the Presence dot 1px, and the 3px ListRow and EntryList bars 2px (D-059).*
- **Dead declarations:** seven, left over from the two-theme days and overridden later — the LevelPlate numeral (three), the primary tab size, the Chronicle and Chronicle input backgrounds, and the Meter track's edge. The LevelPlate and primary-tab four are gone (D-031), and the Meter track's edge (D-059).

## Where gilt is used

*A snapshot from before D-015, kept as the audit found it. D-015 keeps gilt to four jobs; the Ledger, Folio, Tag, NavRail and Chronicle uses below are gone (D-018 to D-022), and every remaining use off the allocation is listed in Foundations · Colour · Not yet on the allocation.*

52 declarations in the component styles (the counts below), two token aliases and a few showcase uses. Nine meanings:

| Meaning | Where | Uses |
| --- | --- | --- |
| Structure: eyebrows and group labels | NavRail group labels; Folio, TopBar and PageHeader eyebrows; JourneyCard phase; Ledger titles; SectionRule ornament label; PageHeader icon; TopBar menu icon; collapsed Chronicle label | 10 |
| Value emphasis | Ledger values and tip footnote; Folio effect values; StatFigure value; LevelPlate numeral (and Achievement Points in the Overview showcase) | 5 |
| Selected or current | NavRail current icon and diamond; EntryList current name and star; Track progress in its gilt tone (fill, done and current steps); Constellation strong ring and nodes | 11 |
| Hover | Button edge; the `gilt-soft` wash on rail items, EntryList rows, Ledger rows, LoadoutSlot and the Chronicle toggle; SearchField's highlighted option | 7 |
| The committing action | Solid Button fill and edge, with `on-gilt` text | 3 |
| Navigation inside a panel | Link Button text (and the Overview showcase's link underline on hover) | 1 |
| Attention | Chronicle mention: wash, edge and `@you` | 3 |
| Ornament | Folio frame (two lines) and corners; Banner frame; SectionRule lattice; Emblem; the Chronicle's top rule (the JourneyCard frame is gone, D-055) | 7 |
| Status labels | Tag `gilt` tone, text and wash (the guild tag); JourneyCard objective wash ("Recommended now") and key glyph | 4 |

Token aliases: `meter-xp` (experience fills) and `channel-loot` (loot lines) both take `gilt`. The Overview showcase also sets the Nobility ◆ in gilt (now the crown, D-066).

**Conflicts:**

- **"Selected" has two colours.** Gilt in NavRail, EntryList, Track and Constellation; arcana in TabStrip, Sigil and ItemSlot. Standards · States says arcana. *EntryList moved (D-087); NavRail's gilt is the current location, not selection (D-015).*
- **`gilt-soft` means four things:** hover, the selected row, a mention and "Recommended now".
- **Every Ledger value is gilt,** so on a Ledger-heavy screen the colour marks nothing (Calm until it matters).
- **Near neighbours.** `focus` (#f5d48f) and `rarity-unique` (#facc15) sit beside `gilt` (#dcb872): a focused gilt value, or a Unique item beside a gilt number, reads as the same signal.

**Recommendation** (D-015 took a different split: brand and current location, the committing action, the headline figure and effect magnitudes, with eyebrows and group labels moving to `ink-muted`): gilt keeps three jobs — the brand's structure (eyebrows, group labels, frames within the ornament budget), the committing action, and emphasis for the one value that matters (effect values, the headline number, the level). Selection moves to arcana everywhere; hover washes move to a neutral `surface-raised`; links move to `ink` with an underline; Ledger values move to `ink`, with gilt only for effect values; mentions and "Recommended now" get their own wash.

## Revision queue

In order. Each item that changes a rule needs a Decision Log entry first (marked **decision**).

**Priority 1 — rule breaches and inconsistencies (small, and they unblock the rest)**

1. **One colour for "selected".** Settled by D-015: selection is `arcana-glow`, and the NavRail's active item is the current location, which keeps gilt. Still to move: Track and Constellation (EntryList moved, D-087).
2. **A shared Tooltip,** built from the Ledger's, replacing the native `title` in StatFigure, ItemLink, CurrencyPill, Track, Presence and the ItemSlot code — explanations must open on keyboard focus too. The Ledger's now pins on tap, stays while hovered and closes on Escape (D-046); the shared Tooltip takes that behaviour.
3. **Fix the breaches:** StatTile's `arcana-glow` text (done, D-026), NavRail's focusable locked items (settled the other way: blocked things stay focusable and give their reason, D-087), ItemSlot's empty-when-no-image logic, all 10px text (done, D-031), and the colour breaches flagged in Foundations · Colour — SearchField's highlighted suggestion shown by the `gilt-soft` wash alone (1.16:1), PageHeader's hex reading the Sigil's component tokens (done: now a gilt diamond, D-065), and TabStrip's active tab reading StatTile's `tile`.
4. **Type onto tokens.** Map the 18 live sizes onto the scale; decide whether an 11px micro label and a 14px compact body become styles or fold into 12 and 15px; drop 10, 20, 24 and 64px and the dead 96px; components read styles, not pixels. **Decision** on which styles to add. Done: the ramp (D-029) adds `body-compact` at 14px and folds the 11px micro label into 12px, keeping 11px only for `code`; 20, 24 and 64px became `title-sm`, `title-md`/`name-header` and `numeral-headline`; every component reads the tokens (D-031).
5. **Radii and dead code.** Radii done: four role tokens replace the size names and every raw radius (D-068). Still to do: remove the seven dead declarations (five removed, D-031, D-059). Sigil's `filter` transition is gone (D-075).

**Priority 2 — consolidation**

6. **ListRow,** with EntryList as its `scene` variant and LoadoutSlot rebuilt on it (removes the third enclosure level). **Decision.** ListRow and List exist, with the three densities (D-040); EntryList and LoadoutSlot are not yet rebuilt on them.
7. **Compact two-up Ledger with deltas** — Delta (D-026) is ready for its rows; StatTile merges into it; ScreenArchive's Folio stats move over. **Done (D-137):** `columns="2"` and a row's `delta`; StatTile is deleted.
8. **ItemSlot text-first:** the item row by default, the square slot only for positional use, names that wrap, a code of 11px or larger (the code is now `code`, 11px, D-031; names still truncate).
9. **Gilt down to four jobs, and every hue family to one job per context** (D-015 to D-017). Done in Ledger, Folio, Tag, NavRail and Chronicle (D-018 to D-022), StatTile (D-026), and EntryList and LoadoutSlot (D-087). The rest — Button, TopBar, PageHeader, JourneyCard, SectionRule, Constellation, LevelPlate, Track, SearchField, ItemSlot, Presence, TabStrip and the showcases — are listed with their target in Foundations · Colour · Not yet on the allocation.
10. **Resources:** Registries · Resources and ResourceAmount; CurrencyPill becomes its TopBar variant, with the other resources behind it. **Decision.**
11. **KeyHints opt-in:** only on screens with real shortcuts, never over content; drop or use `hintbar-height`. **Decision.**

**Priority 3 — workbench readiness and the showcases**

12. **Workbench parts:** Table and ListRow first (ListRow done, D-040; the Table reads the density values when it is built, and builds on `lg-tablewrap`'s column priorities, held first column and minimum width, D-051) (they unlock the Bazaar, guild members and inventory), then Select, NumberField, Checkbox and Toggle, Dialog and confirm (on the layer stack, D-057), a Toast and the guided tour, an inline alert (the only place soft status washes may go, D-025), empty and error blocks, a loading skeleton and pagination.
13. **Missing states:** Button loading (done: `pending`, D-087); Chronicle history loading, failed send, lost connection and cooldown; SearchField error; Folio empty and loading; TabStrip blocked states (ItemSlot done, D-087); ItemSlot hover (done, D-087) and TopBar menu hover. Each one takes its words and channel from Standards · States (D-086).
14. **Container boundaries:** JourneyCard onto `surface` with no frame or shadow (done, D-055), and no edge (D-059); Banner to one edge (done, D-061); the Folio title steps down beside a LevelPlate; StatFigure `sm` beside a LevelPlate.
15. **Remaining raw values as tokens:** two control heights (32 and 40px), a few opacity steps, the shell's z-layers. Control heights done: `control-*` 44, 40 and 32px (D-038); z-layers done (D-056).
16. **The showcases:** fix their known exceptions (nesting, display type), move their preview-only CSS into components (the profile meta list, the Nobility mark, the perks grid), and rename them as ArchetypeInformation and ArchetypeArchive showcases.
17. **Doc fixes:** SearchField's "pill-shaped", and the key-cap radius wording in Foundations · Shape. Done (D-064, D-068): key caps are `radius-control` rectangles.
18. **The 1,536px step:** with a Folio, the docked Chronicle's own column drops the stage from 856px (at 1,440px) to 568px. D-054 proposes the column only from 120rem when a Folio is shown. **Decision.**
