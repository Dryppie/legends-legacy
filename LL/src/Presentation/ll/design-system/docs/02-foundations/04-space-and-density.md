# Foundations · Space & Density

Spacing is the game's 4px scale, used through semantic tokens that name the job. Every spacing and density token is in rem, so spacing grows with the reading-size setting; the pixel figures here are at the default text size (Foundations · Accessibility). Density is how tightly a region packs its rows, in three modes. Legend's Legacy players spend hours on inventory, rankings and trading screens, and information density is preferred over decorative layouts (D-008). So Compact is a deliberate, legible mode, never just a smaller one.

## Rules

**Must**
- Space with the semantic tokens: `inset-*`, `stack-*`, `inline-*` and `section-*`. Use a raw `space-*` step only where no role fits, such as an optical offset. Never invent a gap.
- Give every region one density, Comfortable, Standard or Compact, with `data-density` on its container or a component's `density` input. Standard is the default.
- Keep 12px as the smallest text in every density; only rarity codes and key caps use 11px. Keep the full focus ring:
  - a focused row rises above its neighbours;
  - a scrolling list keeps `space-1` of inline padding, so the ring is never clipped.
- Step down one level inside another surface: two nested containers never both apply full padding. A container inside a Banner or a JourneyCard takes the inner inset, and an inner surface in the Folio takes the Folio's; the Folio and dialogs hold no Panel (Foundations · Surfaces & Layering).
- Put a list or table inside a Panel on the Panel's own padding. Make the Panel `flush`; its rows then take the Panel's inset and line up with its title.
- Keep a row at its density's height. A thumbnail fills the row with 4px above and below it, and the hairline between rows sits inside the row.

**Should**
- Choose the density from the job, not the screen. A Workbench screen has Compact tables beside a Standard order form and a Comfortable inspector.
- Keep Compact to one line per row: name, code, meta and values in aligned columns. Secondary detail goes to the inspector (or the Folio on a stage screen) or a tooltip, not a second line.
- Reach for a denser mode before cutting content.
- Lay Ledgers out in the ledger grid (`lg-ledgergrid`): four a row in a Wide region, two at Medium and Narrow, one Stacked (Foundations · Layout).
- Keep a two-up Ledger to six to eight rows; more wants a full Ledger or a table.

**Never**
- Mix densities inside one list or table.
- Make text smaller, drop a focus ring or hide a value to make a region denser.
- Pad a nested container fully, or give a list inside a Panel padding of its own.
- Trade a value for whitespace or ornament.

## The scale

| Token | Value | Typical job |
| --- | --- | --- |
| `space-1` | 4px | Icon to label, key cap padding |
| `space-2` | 8px | Inside tags and small controls |
| `space-3` | 12px | Tiles and inner surfaces |
| `space-4` | 16px | Panel padding |
| `space-5` | 20px | The TopBar's side padding, secondary tab gaps |
| `space-6` | 24px | Comfortable containers, between Panels |
| `space-8` | 32px | The Page's side padding, between Folio blocks |
| `space-10` | 40px | Stage inset from the rail and Folio |
| `space-12` | 48px | Above screen titles, the rail's top padding |
| `space-16` | 64px | Stage breathing room around a Constellation |

## Semantic spacing

Each semantic token equals one step of the scale. The stylesheet aliases it to that step (`--lg-inset-lg: var(--lg-space-4)`), so a change to the scale carries through. `tokens.json` lists the same values for the viewer; its format has no aliases for lengths. Change a scale step, never an alias.

| Kind | Tokens | Aliases | Job |
| --- | --- | --- | --- |
| **Inset**: padding inside a container | `inset-xs` 4 · `inset-sm` 8 · `inset-md` 12 · `inset-lg` 16 · `inset-xl` 24 | `space-1` · `space-2` · `space-3` · `space-4` · `space-6` | Badges; tags; tiles and inner surfaces; a Panel; a Comfortable container |
| **Stack**: vertical gap | `stack-xs` 4 · `stack-sm` 8 · `stack-md` 12 · `stack-lg` 16 | `space-1` · `space-2` · `space-3` · `space-4` | Label to value; rows of a group; related blocks; groups inside a container |
| **Inline**: horizontal gap | `inline-xs` 4 · `inline-sm` 8 · `inline-md` 12 · `inline-lg` 16 | `space-1` · `space-2` · `space-3` · `space-4` | Icon to label; tags in a row; controls in a toolbar; control groups |
| **Section**: between groups | `section-sm` 24 · `section-md` 32 · `section-lg` 48 | `space-6` · `space-8` · `space-12` | Between Panels on a Page; the Page's side padding and major regions; above a screen title |

## Density modes

| | Comfortable | Standard | Compact |
| --- | --- | --- | --- |
| **For** | Identity and detail views, the Folio, dialogs | The default: panels and forms | Tables, inventory lists, rankings, guild members, combat logs, Bazaar order books |
| **Row height** (minimum, one-line row) | `row-comfortable` 48px | `row-standard` 40px | `row-compact` 32px |
| **Cell padding** (vertical / horizontal) | 12 / 16px (`stack-md` / `inset-lg`) | 8 / 12px (`stack-sm` / `inset-md`) | 4 / 8px (`stack-xs` / `inset-sm`) |
| **Gap between elements** | 16px (`inline-lg`) | 12px (`inline-md`) | 8px (`inline-sm`) |
| **Container inset, and inner surface** | 24px, inner 16px | 16px, inner 12px | 12px, inner 8px |
| **Row text** | `body`, 15 / 20 | `body-compact`, 14 / 18 | `body-compact`, 14 / 18 |
| **Numbers in rows** | `numeral-row`, 18 | `numeral-row`, 18 | `numeral-compact`, 14 |
| **Names in rows** | `name-row`, 17 (EntryList: `name-header`, 24) | `name-row`, 17 (EntryList: `name-header`, 24) | `name-row`, 17 |
| **Meta line** (type, level, rank) | Its own line under the name | Beside the name, truncating | Beside the name, truncating |
| **Icon** | `icon-comfortable` 20px | `icon-standard` 20px | `icon-compact` 16px |
| **Row thumbnail** | `thumb-comfortable` 40px | `thumb-standard` 32px | `thumb-compact` 24px |
| **Control height** (Button, input, primary tab) | `control-comfortable` 44px | `control-standard` 40px | `control-compact` 32px |

**How a mode is set:**

- `data-density="compact"` on any container sets the mode for everything inside it, and a component's `density` input does the same on the component itself.
- The nearest setting wins, so a Compact table can sit inside a Standard Panel.
- The Folio and a list's inspector (`lg-split__inspector`, Foundations · Layout) are always Comfortable; mark a region inside either `data-density` to change it.
- Components read only the `--lg-row`, `--lg-cell-*`, `--lg-gap`, `--lg-inset*`, `--lg-control`, `--lg-icon` and `--lg-thumb` variables that the mode sets. A mode is one attribute, with no per-component overrides.

## Making Compact legible

Compact is where players spend their hours, so it keeps everything that makes a row readable and removes only air:

- **Text never shrinks below the floor.** Row text is 14px, captions and meta 12px, names 17px Marcellus, numbers 14px Barlow Condensed in tabular figures. Only the rarity code is 11px.
- **One line, aligned columns.** The rows of a List share their columns (thumbnail, name, quantity, value, action) through subgrid, so every value lines up and the eye scans down a column rather than along a row.
- **Hairlines, not boxes.** A `line` hairline separates rows; a wide list or table read across many columns may take zebra rows on `row-stripe` instead, never both (Foundations · Lines). There are no cards; hover takes the `surface-raised` wash, and selection is a 2px `ink` bar plus that wash.
- **The whole focus ring.** The 2px gap and 2px ring show on every side of a focused row: the row rises above its neighbours, and scrolling lists keep 4px of inline padding.
- **Controls stay reachable.** Compact controls are 32px high, above the 24px minimum target size (WCAG 2.2). A row's action is a `sm` Button in the trailing column.
- **Compact is for scanning many similar rows.** A form, a dialog or an item's detail is never Compact.

## Nesting: the step-down rule

A container inside another surface takes the inner inset of the density it sits in, one step less than a container standing on its own:

| Region | A container on its own | A container inside another surface |
| --- | --- | --- |
| Comfortable | 24px | 16px |
| Standard | 16px | 12px |
| Compact | 12px | 8px |

- **An inner surface in the Folio** (a LoadoutSlot, a well) pads 16px, the Comfortable inner inset, not 24px. The Folio holds no Panel.
- **A LoadoutSlot in a Panel** pads 12px, the inner inset, and follows its region; the Panel keeps its 16px.
- **A List or table in a Panel:** the Panel is `flush` (no body padding), and each row pads to the Panel's inset, 16px in Standard. The list's names line up with the Panel's title, and the row's hover wash runs to the Panel's edge.
- **Panels never nest** (Standards · Information Hierarchy). Three levels of enclosure means the inner one should be a row, not a box.

## Which components support which densities

| Component | Comfortable | Standard | Compact | What changes |
| --- | --- | --- | --- | --- |
| Ledger | Yes | Yes | Yes | Row height and padding, label text, values (`numeral-compact` in Compact) |
| ListRow and List | Yes | Yes | Yes | Row, thumbnail, gap, meta on its own line in Comfortable, values |
| EntryList | Yes | Yes | Yes | Row height; names `name-header` 24px, or `name-row` 17px in Compact |
| TabStrip | Yes | Yes | Yes | Primary tab height (the control height) and padding; secondary tab height |
| Button | Yes | Yes | Yes | Height, padding, label size and icon. Without a `size` it follows its region; `size="md"` pins Standard, `size="sm"` pins Compact |
| SearchField and inputs | Yes | Yes | Yes | Control height |
| Panel | Yes | Yes | Yes | Body and head inset; one step less when nested; `flush` for a list or table |
| LoadoutSlot | Follows its region | Follows its region | Follows its region | Inner inset only |
| Folio | Always | — | — | Comfortable by definition; everything inside it is Comfortable unless marked |
| Chronicle | — | — | Always | The combat and chat log is Compact, and the Compact lists setting leaves it alone |
| Meter, Sigil, LevelPlate, StatFigure, Banner, JourneyCard, Tag, CurrencyPill, KeyHints, TopBar, NavRail | One size | One size | One size | Fixed. NavRail's `compact` means collapsed to icons, not a density |
| ItemSlot | Sizes | Sizes | Sizes | `sm`, `md` and `wide` are sizes for positional slots, not densities |

The future Table reads the same row, cell and control values.

## Page archetypes and their densities

| Archetype | Default | Regions that differ |
| --- | --- | --- |
| ArchetypeInformation: Overview, Settings, the guild's front page | Standard | The Folio is Comfortable |
| ArchetypeArchive: the Creature Archive | Standard: tabs and the EntryList | The Folio is Comfortable |
| ArchetypeWorkbench (future): the Cinder Bazaar, inventory, crafting | Compact: tables, lists and order books | The order form and filters are Standard; the inspector and dialogs are Comfortable |
| ArchetypeRanking (future): the Leaderboard, the Colosseum ladder | Compact | The player's own standing, above the table, is Standard |
| ArchetypeRoster (future): guild members, a party | Compact | The selected member's detail is Comfortable, in the inspector |
| ArchetypeDetail (future): one character, creature or item on its own screen | Comfortable | Long stat tables inside it are Standard |
| ArchetypeCombat (future): dungeon and Colosseum fights | Standard: actions, HP and SP | The combat log is Compact |
| Dialogs: confirm, sell, trade | Comfortable | A list inside a trade dialog is Standard |

## The "Compact lists" setting

**Recommendation: yes. Offer one on/off switch, "Compact lists", in Settings, on by default.**

- **What it changes.** When it is off, every region designed as Compact reads as Standard: inventory, rankings, guild members, Bazaar order books and trade lists get 40px rows, 12px cell padding, 40px controls and `numeral-row` numbers.
- **What it never changes.** It leaves Standard and Comfortable regions, the 12px text floor, focus rings, the Folio and the Chronicle alone. The Chronicle already has its own layout setting.
- **Why on by default.** These screens are designed Compact because players who live on them want more rows in view: 32px rows show a quarter more than 40px rows.
- **Why offer it at all.** It helps on touch screens, where 32px controls are reachable but tight. It helps players with low vision or on the reading-font setting, since Atkinson Hyperlegible is wider. And it helps anyone who finds dense lists tiring. Consider defaulting it to off on touch devices.
- **Why not three options.** Comfortable lists are not offered: a list of hundreds at 48px is a long scroll, and Comfortable is for detail views.
- **How it works.** The switch is stored with the player's other Settings, like Chat layout (D-002), and applied as `data-compact-lists="off"` on the root. The stylesheet already maps every Compact region to Standard under it, so no component needs work. It is one switch for the whole game, never a per-screen toggle, so every list behaves the same.

## Tokens used

| Token | Role here |
| --- | --- |
| `space-1` … `space-16` | The scale; every other spacing token aliases one step |
| `inset-*`, `stack-*`, `inline-*`, `section-*` | Semantic spacing (the table above) |
| `row-comfortable`, `row-standard`, `row-compact` | Row heights: 48, 40 and 32px |
| `control-comfortable`, `control-standard`, `control-compact` | Control heights: 44, 40 and 32px |
| `icon-comfortable`, `icon-standard`, `icon-compact` | Icon sizes: 20, 20 and 16px, on the icon scale (Foundations · Iconography · Size scale). Standard was 18px (D-081) |
| `thumb-comfortable`, `thumb-standard`, `thumb-compact` | Row thumbnails: 40, 32 and 24px |
| `text-body`, `text-body-compact`, `text-numeral-row`, `text-numeral-compact`, `text-name-row`, `text-name-header` | The row type each mode uses |
| `focus-ring` | Kept whole in every density |

## Do and don't

| Do | Don't |
| --- | --- |
| Put the inventory in a `flush` Panel with a Compact List, so the rows line up with the Panel's title. | Put a padded list inside a padded Panel: 16px, then 12px more. |
| Show an item's detail in a Comfortable inspector beside a Compact inventory. | Make the whole Bazaar Comfortable to give the inspector room. |
| Show 60 items at 32px rows, one line each, with values in a right-aligned column. | Show 60 cards with the value on a second line. |
| Pad an inner surface in the Folio 16px. | Pad it 24px inside the Folio's own padding, or put a Panel there. |
| Keep a Compact row's name at 17px and its meta at 12px. | Drop the meta to 10px to fit. |
| Let a focused Compact row rise over its neighbours, ring intact. | Clip the ring with `overflow: hidden` on a list. |
| Four Combat Attribute Ledgers side by side on a wide Page. | One Ledger per full-width Panel with large gaps between them. |
| `inset-lg` inside a Panel. | A one-off 22px padding. |

## Related components

- ListRow — the list row
- Ledger — the labelled value list
- EntryList — the browsable name list
- TabStrip — the tabs
- Button — the command button
- Panel — the content box
- Folio — the detail panel
- Chronicle — chat and the game log
- SearchField — search with suggestions
