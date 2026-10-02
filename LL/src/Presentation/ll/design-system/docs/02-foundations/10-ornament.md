# Foundations · Ornament

Ornament marks the one special surface on a screen and the break between lore and effects, and nothing else. It never carries data, never takes the place of a value, and never reaches a table, a list or a control. The fantasy comes from type, lore and art; decoration is kept on a budget, counted per screen.

## Rules

**Must**
- Keep every screen within the budget below: one ornamented framed surface (a Folio or a Banner, not both), at most two ornament rules, and film grain only over art.
- Keep ornament out of the forbidden zones: tables, lists, inputs, toasts, menus, dense Panels, and dialogs other than major commitment dialogs.
- Draw light flat. Only four states are lit — selected, ready, keyboard focus and the solid Button's hover — and none of them is a halo (Glow, below).
- Put body text on a contrast surface wherever there is texture or art behind it (Texture, below).
- Hide ornament from assistive technology (`aria-hidden`): frames, corners, the ornament rule's lattice, Emblems, grain and veils.

**Should**
- Count the devices on every new screen and compare them with the budget before it ships (Audit, below).
- Spend the budget where the player's attention should go: the Folio on a stage screen, the Banner on an information screen.
- Tag any decorative element a screen adds outside the components with `data-ornament` (`frame`, `rule`, `corner`, `texture`), so the audit finds it (Audit, below).

**Never**
- Use parchment, leather, wood, metal, stone or any other material texture.
- Let anything glow as a halo, loop, decorate, or signal rarity.
- Let ornament replace a value or push one off the screen (D-008).

## The devices

| Device | What it is | Purpose | Allowed | Status |
| --- | --- | --- | --- | --- |
| **CornerOrnament** | The filigree corner asset (Ornaments/CornerOrnament.svg), masked in `gilt` and flipped for each corner | Brand: marks the screen's one special surface | The four corners of the Folio or the Banner (`cornerSrc`) | In use |
| **Divider** | The game's engraved flourish (Ornaments/Divider.svg), a bronze gradient with a centre diamond, 253 × 7 | Brand, as the game drew it before Grimoire | Only in place of the ornament rule's lattice, where it counts as one of the screen's two rules. No component draws it; prefer SectionRule `ornament`, which follows the theme | Asset only |
| **Diamond-chain rule** | SectionRule `ornament`: hollow diamonds in a `gilt` lattice | Brand: the break between lore and effects | Once per surface, twice per screen (D-061). The Folio draws its own | In use |
| **Double gilt inset frame** | Two `gilt` hairlines, the inner one 4px inside the outer | Brand: the special surface's one edge | The Folio or the Banner, never both on one screen (D-061) | In use |
| **Film grain** | Fractal noise at `--lg-grain-opacity` (0.14), blended over | Photographic: settles art so it reads as one picture | Over art only: the Stage's and the Banner's veils, and only when they have an image. Not the Folio (D-072) | In use |
| **Vignette** | The veil's radial darkening to `ground-deep`, and its fades | Frames art, and darkens it where text sits | The Stage and the Banner, only when they have an image | In use |
| **`glow-selected`** | Was a verdigris halo round the selected thing | — | Nowhere. Removed by D-012; selection is the flat 2px `arcana-glow` ring, bar or edge | Retired |
| **`glow-gilt`** | Was a gilt halo on brand marks and the solid Button | — | Nowhere. Removed by D-012; gilt never glows, and the solid Button brightens on hover | Retired |
| **`shadow-text-art`** | A dark text shadow (`#0b0806`, 14px and 3px) | Legibility, not decoration: lifts a label off art | Labels set over Stage or Banner art: Sigil labels, stage captions, the Banner's text. Never in a light colour | In use |
| **Emblem** | A star polygon in `gilt` strokes | A sign — an attribute, school or region — not an ornament (Foundations · Shape) | Crowning a Folio, one per Folio; it goes with the Folio's budget | In use |

The fades that are not the veil — the TopBar's fade over the stage and the EntryList's fading ends — are part of the art's frame too, and follow the gradient guardrail: dark fades over pictures only (D-012).

## The budget

Per screen, with every dialog open over it counted as part of the screen:

| Device | Budget | Notes |
| --- | --- | --- |
| Ornamented framed surface (double gilt frame and CornerOrnaments) | **1**: a Folio or a Banner, not both | A stage screen spends it on the Folio; an information screen on the Banner. Page screens put their inspector in the content, not a Folio (D-052), so the two rarely meet |
| Ornament rule (the diamond chain, or the Divider in its place) | **2**, and 1 per surface | The Folio's own between lore and effects, and one more at most: in a major commitment dialog, or in the Banner |
| Film grain | **Over art only**, at its set strength | The Stage and the Banner, when they have an image |
| Vignette | **Over art only** | The same two, with the same condition |
| `shadow-text-art` | **On labels over art only** | Never on text on a fill |
| Emblem | **1**, in the Folio | |
| Lit marks | **Only the four lit states** | Selected, ready, keyboard focus, the solid Button's hover (Glow, below) |
| Halos and loops | **0** | The two loops Foundations · Motion allows — an indeterminate progress indicator and live combat playback — are not ornament, and the audit leaves them out |

**Major commitment dialogs.** A dialog that spends something scarce or permanent and marks a milestone — Legacy Ascension, redeeming Nobility, attuning a Legendary Essence, founding a guild — is a major commitment. Set `data-commitment="major"` on it. It may carry one ornament rule, between what the player gives and what they get. It never takes the gilt frame or the corners, which stay with the Folio and the Banner. A destructive confirmation — Abandon, Leave guild, Sell, Release — is never a major commitment, however large: a warning stays plain.

## Forbidden zones

No frame, corner, ornament rule, grain, vignette or texture goes inside these, on any screen:

| Zone | Why |
| --- | --- |
| **Tables** | Rows are read across; ornament breaks the columns |
| **Lists** — List, ListRow, EntryList, the Chronicle's log | Ornament between rows reads as a group break that is not there |
| **Inputs** — fields, selects, the chat composer | An input is a control; its edge is `line-strong`, nothing more |
| **Toasts** | They arrive unasked; decoration makes them shout |
| **Menus** — suggestion lists, context menus, pickers | Options are scanned, not admired |
| **Dense Panels** — a Panel holding a List, a table or a Ledger, a `flush` Panel, or any Panel in Compact density | Density needs every pixel for data |
| **Dialogs**, other than major commitment dialogs | Everyday dialogs and every destructive confirmation stay plain |

Buttons, tiles, slots and Tags carry no ornament either: they have their own shapes and edges (Foundations · Shape, Lines).

## Glow

In Grimoire, light is drawn flat. Four states are lit, each with its own mark, and nothing else is:

| State | The lit mark | Token |
| --- | --- | --- |
| **Selected** | A 2px ring, bar or edge — `ink` in item rows and chat — with the `surface-raised` wash | `arcana-glow`, `border-emphasis` |
| **Ready or claimable** | The diamond on a Sigil; the `new` Tag | `arcana-glow`, `arcana` |
| **Keyboard focus** | The focus ring: a `ground` gap and a solid 2px ring | `focus-ring` |
| **Primary-button hover** | The solid Button brightens by 6% | — |

- **Never a halo.** No blurred coloured shadow, bloom, outer glow or glowing text, on anything (D-012). `glow-selected` and `glow-gilt` stay retired.
- **Never loops.** A lit mark appears and stays; it never pulses, breathes, shimmers or sweeps. Motion follows Foundations · Motion and stops under reduced motion.
- **Never decorates.** No frame, title, numeral, icon, rule or Emblem is lit.
- **Never signals rarity.** Rarity is carried by the slot's edge, the name's colour and the rarity code (Registries · Rarity); a Legendary item looks like any other with a different colour and code. A lit mark on an item means it is selected, nothing else.

## Texture

- **No material textures.** No parchment, paper, leather, wood, metal, stone or cloth, and no embossing, bevels, stitching, torn edges or wax seals. Surfaces are flat fills (Foundations · Surfaces & Layering).
- **Grain is photographic, not a material.** It settles art into one picture, so it sits only over art, at its set strength.
- **No texture directly behind body text without a contrast surface.** Body text sits on a fill — a Panel (its translucent `surface` holds 4.5:1 over the frame's backdrop, D-102), the Folio, a dialog — or on a veil that holds it at 4.5:1 over the brightest point of the art. The Banner's veil does: across its left 60%, where the text sits, it is at least 66% `ground-deep`, which keeps `ink` at 9.9:1 even over a white pixel of art (12.3:1 at the 42% mark). At its thinnest (40%, the right edge, under the headline figures) `ink` holds 5.8:1 and `gilt` 3.8:1, enough for figures that size. A label over Stage art takes `shadow-text-art`; a paragraph over art goes in a Panel.
- **So the Folio has no grain** (D-072). It bears no art, and its lore, effects and Ledgers sat straight on the texture.

## Audit

A quick check for any screen, before it ships and whenever it changes:

1. **Take the screen at its widest,** with its busiest content and any dialog it can open.
2. **Count the devices:** ornamented framed surfaces, ornament rules, corner sets, surfaces with grain (and whether each bears art), vignettes, anything tagged `data-ornament`, and anything that is lit or moves by itself.
3. **Check the zones:** is any device inside a table, list, input, toast, menu, dense Panel or everyday dialog?
4. **Compare with the budget.** Any count over, any device in a forbidden zone, any glow outside the four lit states, any texture: cut it.

The components check part of this themselves. On a screen inside a GameShell they warn in the console when a Folio and a Banner meet, when a third ornament rule appears, or when an ornament rule lands in a forbidden zone (`lgCheckFramed` and `lgCheckOrnamentRule`, against `LG_ORNAMENT_BUDGET` and `lgForbiddenZone`). Nothing checks grain, halos, loops or ornament painted into an image, so the eye check still stands.

The Creature Archive, counted within the budget and over it:

| Creature Archive | Within the budget | Over-decorated |
| --- | --- | --- |
| Framed surfaces | 1: the Folio, with its corners | 2: a Banner over the stage as well |
| Ornament rules | 0 | 3: two in the Folio instead of bands, one in a Panel |
| Corner sets | 1, on the Folio | 3: the Folio, the Banner and a Panel |
| Film grain | 1, over the Stage's art | 3: the Stage, the Banner, and the Folio behind its text |
| Textures | 0 | 1: parchment behind the drops list |
| Halos | 0 | 3: the selected creature, the Rare Essence slot, the level numeral |
| Loops | 0 | 1: the pulsing "Essence ready" count |

## Tokens used

| Token | Role here |
| --- | --- |
| `gilt` | The frame, the corners, the ornament rule's lattice, Emblem strokes — brand ornament, gilt's first job (Foundations · Colour · Allocation) |
| `ground-deep` | The veil's vignette and the Banner's contrast veil |
| `shadow-text-art` | Labels over art |
| `arcana-glow`, `focus-ring`, `border-emphasis` | The flat lit marks |
| `folio`, `shadow-panel` | The Folio the frame sits on |

The grain's strength is the component variable `--lg-grain-opacity` (0.14) and the frame's `--lg-frame-opacity` (0.32), set once for the theme. Assets: the Ornaments group — `CornerOrnament.svg` and `Divider.svg`.

## Do and don't

| Do | Don't |
| --- | --- |
| One Folio with engraved corners on the Creature Archive. | A Banner over the stage as well as the Folio. |
| A `band` SectionRule over the Folio's stat groups. | The ornament rule above every group. |
| Grain over the Stage's art. | Grain behind the Folio's lore and effects. |
| A flat 2px ring on the selected Essence slot. | A blue halo round a Rare slot. |
| The "Essence ready" count, still. | The count pulsing to draw the eye. |
| One ornament rule in the Legacy Ascension dialog. | An ornament rule in the Sell confirmation. |
| A lore paragraph in a Panel over the stage. | Lore set straight on the art, or on parchment. |

## Related components

- Folio — the detail panel
- Banner — the headline block
- SectionRule — the dividers
- Button — the command button
- ItemSlot — the item frame
- Stage — the scene behind the screen
- Emblem — the attribute sign
- Sigil — the hex stat badge
