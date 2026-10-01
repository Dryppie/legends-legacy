# Principles · Anti-generic guardrails

As the system grows it drifts toward two defaults: the generic SaaS dashboard and the stereotypical AI-made game UI. An audit of the current game found the drift already under way:

- a card around every data group;
- containers nested three and four deep;
- the same heading → subtitle → grid-of-tiles rhythm, block after block;
- rows of equal-sized stat tiles;
- one progress card for mechanically different systems — Achievements, Soulstones, the Essence Codex;
- one accent colour carrying too many meanings.

These guardrails turn the Principles into limits. Every rule here is a *must*.

## Risks and rules

| Risk | What it looks like | Rule |
| --- | --- | --- |
| Excessive cards | Every data group in its own bordered box | Group data with a heading, a SectionRule band or space first. A Panel holds secondary content that must stand apart — lore, pending loot, a biography — never a group of values a Ledger title already names. |
| Container nesting | A box in a box in a box | At most **two enclosure levels** inside a region. The shell's regions — rail, stage, Folio, Chronicle — are the frame and don't count; any container with its own fill or border does. A Panel, Banner or JourneyCard is level one; a slot, tile, input or wash inside it is level two; nothing goes deeper. |
| Stat tile grids | A row of equal tiles as a screen's summary | Primary stats go in a Ledger or a StatFigure. StatTiles hold secondary stats in a stat column, such as the Folio's: two-up, six to eight per group. |
| Heading stacks and repeated eyebrows | Eyebrow, title and subtitle on every block; heading → subtitle → grid down the page | Eyebrows orient, so only four places carry one: the TopBar (a level or region), the PageHeader (its section), the Folio (the selected thing's kind) and the Banner (its subject). A block gets a title, and a subtitle only when it says what the title cannot. Consecutive groups take the form of their content — Ledger, list, Track, table — not one repeated rhythm. |
| Rounded rectangles and pills | Every surface a soft rounded box; every button, input and badge a pill | Square and engraved (Foundations · Shape): regions square, containers and rows at `radius-container` (2px), controls, tags and slots at `radius-control` (4px), only floating surfaces at `radius-float` (8px). Nothing is a pill, and the one circle is the Presence dot. |
| Gradients | Gradient fills, buttons, rails and headers | Gradients only as **dark fades over pictures**: the Stage and Banner veils, the TopBar's fade over the stage, list ends fading into the scene. Never on a surface, fill, button, border or text (D-012). |
| Glassmorphism | Frosted, translucent panels | No backdrop blur and no translucent panel over content. A Panel in flow lets the frame's backdrop through, unblurred (D-102); floating surfaces are opaque — `surface-raised` with `shadow-float`, or the Chronicle drawer's `surface-solid` — and the `scrim` is flat. Blur belongs to Stage and Banner art alone (Foundations · Surfaces & Layering). |
| Glow | Glowing cards, numbers, icons, rarities and states | **No halo anywhere** (D-012). Four states are lit, and flat (Foundations · Ornament · Glow): selected is a 2px `arcana-glow` ring, edge or bar; ready is the `arcana-glow` diamond; focus is the solid `focus-ring`; the solid Button brightens on hover. Nothing loops, nothing decorative is lit, and rarity never glows. A colour named "glow" is a flat fill, never a halo. |
| Excessive borders | A line around every tile, row and group | Space first, a divider second, a full border only for an interactive edge (`line-strong`) or a bounded object — an item slot, an opponent's card. A Panel is told apart by its fill, not a frame. Rows take separators or zebra, never both, and never inside a bordered container (Foundations · Lines). |
| Fantasy ornamentation | Filigree on every surface | Per screen: one ornamented framed surface (a Folio or a Banner), two ornament rules, grain only over art, and nothing in tables, lists, inputs, toasts, menus, dense Panels or everyday dialogs (Foundations · Ornament). Anything past it is cut. |
| Hero artwork without a subject | A painted strip atop a page that shows nothing in particular | Art shows the screen's own subject or place — the region, the dungeon, the Bazaar — behind content about it. A picture that could head any page is removed. On information screens, art appears only inside the Banner. |
| Oversized typography | Big type as decoration; several giant numbers | **One display-size element per screen.** Display size is 56px and up: the `folio` Heading (`title-xl`), `level-numeral`, a default StatFigure. Where a LevelPlate is present its numeral is the one: titles step down to `title-lg`, headline numbers use StatFigure `sm`. |
| Excessive whitespace | Airy bands holding one value each | Gaps come from the space scale: `space-6` to `space-8` between groups; `space-10` and up only for stage insets, the rail's top padding, the space above a screen title and the room around a Constellation. Space separates groups; it never stands in for content. |
| Mobile-app layouts on desktop | One centred column of stacked cards, a bottom tab bar, a menu button at full width | Desktop-first: at 960px and wider, use the shell's columns and multi-column content (the ledger grid's four columns). The rail drawer, the menu button and the bottom dock exist only under 960px. |
| One accent for many meanings | The same colour for brand, selection, links, values and warnings | Each hue family has one job per context, set out in Foundations · Colour · Allocation (D-015). `gilt` keeps four jobs and takes no new ones; `arcana` means ready, new, actionable or selected; a new job for any hue needs a Decision Log entry. |

## Components at risk

| Part | Limit |
| --- | --- |
| Banner | One per screen, for the headline identity block of an information screen only. Its art shows the subject's place, or it has none. Never a section header, a promotion or a divider. |
| Folio | One per screen (D-004), for the selected thing only. Its frame is the screen's engraved frame: nothing inside it is framed again, and no Panel goes in it. |
| Stage | Scene screens only; its art is the screen's place. Information and workbench screens use a Page. |
| StatTile | Secondary stats in a stat column, such as the Folio's — two-up, six to eight per group. Never a screen-wide row; primary stats go in a Ledger or a StatFigure. |
| Button | An engraved rectangle at `radius-control`, never a pill (D-064); CurrencyPills and key caps take the same corner, and Meters are square-ended (Foundations · Shape). One `solid` button per screen. |
| `arcana-glow` | A flat fill for the selected ring, edge or bar, the active-tab bar and the ready diamond — never a halo, a shadow or text. One selection per screen carries it. |

## Banned tropes

- An icon in a circle beside every heading. The PageHeader's gilt diamond, once per screen, is the only section mark.
- Feature-card triplets: three equal cards, each with an icon, a title and a line.
- Gradient text.
- Sparkle or wand icons.
- Badge soup: more than one Tag or badge on a row, or badges that repeat what the row already says. When several states apply, the first in the Tag order is the row's one Tag, and markers keep their fixed places (Standards · State combinations).
- Emoji.
- Cheerful copy: "Welcome back, hero!", "Awesome!".
- The same padding at every nesting level. Padding steps down as you go in: `space-6` in a Panel, `space-3` in a tile.
- Parchment or wood textures.

## The sameness test

**Two mechanically different systems must not share an identical structure without a stated reason.**

1. Write each system's mechanic in one line: what the player does, what changes, what they decide.
2. If the lines differ, the screens differ in structure — not only in labels and numbers.
3. If they share a structure anyway, write the reason on the screen's page or in the Decision Log. Two archives of collectible things sharing ArchetypeArchive is a good reason; "it was quicker" is not.

The audit's case: Achievements, Soulstones and the Essence Codex were drawn as one progress card — a title, a bar, a percentage. Under the test each starts from its own loop. A collection to complete, like the Codex, takes the archive's list and Folio; goals to claim become rows with their reward and a claim action; Soulstones take the shape of their own loop, not their neighbour's.

## Fantasy balance

The fantasy comes from three sources only:

- **Typography** — Marcellus capitals, the lore italic.
- **The lore register** — one or two sentences of atmosphere, never numbers (Standards · Content).
- **A small set of ornaments** within the budget — the Folio's engraved frame, the diamond-chain rule, Emblems, the hexagon and the diamond.

Never from skeuomorphic parchment, wood or metal. The film grain over the Stage's and the Banner's art is photographic, not a material, and stays at its set strength; nothing else carries it, and no texture sits behind body text without a contrast surface (Foundations · Ornament).

## Known exceptions

The current components break these guardrails in a few places. Each is either brought in line in a visual pass or recorded as an accepted exception in the Decision Log. The gradient and glow exceptions were removed (D-012).

| Where | What | Guardrail |
| --- | --- | --- |
| ScreenOverview | Panel → LoadoutSlot → ItemSlot frame: three enclosure levels in the Essence Loadout | Container nesting |
| ScreenOverview | The LevelPlate numeral and a default StatFigure together in the Banner | Oversized typography |
| ScreenArchive | The Folio's `title-xl` name and its LevelPlate numeral | Oversized typography |

## Review checklist

Answer every question for a new component or screen. Each *no* needs a fix or a stated reason.

1. Does every Panel hold secondary content that a heading or a rule could not separate?
2. Are there at most two enclosure levels inside each region?
3. Are primary stats in Ledgers or a StatFigure, with StatTiles only for secondary stats?
4. Do only the TopBar, the PageHeader, the Folio and the Banner carry eyebrows, with no heading → subtitle → grid repeated down the screen?
5. Does every radius come from its role, is nothing a pill, does each shape keep its one meaning (Foundations · Shape), and is every full border an interactive edge or a bounded object (Foundations · Lines)?
6. Are gradients only dark fades over pictures, with no frosted glass anywhere?
7. Is there no halo anywhere — only the four flat lit states, nothing looping but the two loops Foundations · Motion allows, and rarity carried by edge, colour and code?
8. Counted with the ornament audit, is the screen within the budget — one framed surface, two ornament rules, grain only over art, no ornament in a forbidden zone, no parchment, wood or metal?
9. Does every image show this screen's own subject or place?
10. Is there at most one display-size element?
11. At desktop width, does the screen use the shell's columns and multi-column content, with gaps from the space scale?
12. Does it pass the sameness test, and is it free of every banned trope?
