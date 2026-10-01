# Foundations · Layout

The grid every screen is built on, and how content arranges itself inside it. The key fact: the stage is much narrower than the screen. At 1920px, the NavRail (`rail-width`, 224px), the Folio (`folio-width`, 360px) and the docked Chronicle (`chronicle-width`, 384px) leave a 952px stage. After the Page's padding, 888px is left for content. So content arranges itself by the width of the region it sits in, never by the window. Shell covers where each shell region goes. LayoutSpecimen shows the rules on an inventory and LayoutGridSpecimen on the Character Overview.

## Rules

**Must**
- Lay content out by the width of its region, using the four content tiers below as container queries on `lg-region`. Never switch a content layout on the window (`@media`) or on the shell's width.
- Build on the shell grid: NavRail (`rail-width`), the stage (fluid) and the Folio (`folio-width`), with `topbar-height` across the stage and KeyHints (`hintbar-height`) at its foot, capped at `shell-max`.
- Give the shell a real height (`100vh` by default). The stage, Folio and Chronicle scroll inside it; the page never does.
- Size columns, breakpoints, gutters and minimum widths in rem, so layouts reflow sooner as the reading-size setting grows the text (Foundations · Accessibility · Text scaling). Nothing makes the page scroll sideways, down to 320px wide.
- Start every section on the Page's one left edge (Alignment and rhythm, below).
- Hold prose to the reading width: about 68 characters (`--lg-measure`).
- Keep every value when a layout narrows. Hide a table column only by its priority, and keep its values one step away, in the inspector or the row's detail.
- Check each layout at all four tiers, and at 115% and 130% text.

**Should**
- Design each screen at Medium first. 1920px with a Folio and the docked Chronicle, the most common setup, gives 888px of content, which is Medium. Then check Wide, Narrow and Stacked.
- Use the standard track layouts (below) before inventing one.
- Give a screen one inspector. Page screens (the inventory, the Cinder Bazaar, guild members) inspect in the content, with a list and inspector. Stage screens (an archive, the world map) use the Folio.
- Keep a Constellation's coordinates in pixels that match the stage size you design for. It keeps its aspect ratio and scales to its container's width.

**Never**
- Stretch prose, a Ledger or a form to fill a wide region.
- Run text, a Ledger grid or a table full bleed.
- Let anything scroll sideways except a data table or a strip of tabs, chips or currencies, and then only inside its own region.
- Let the document scroll behind the shell, or stretch the shell past `shell-max`. Beyond it the `ground-deep` letterbox shows.

## The stage is narrower than the screen

The stage is what the shell leaves: the screen, less the rail, the Folio and the Chronicle's column. A Page then pads it by `section-md` (32px) on each side. Each cell below gives the stage's width in px and the content tier its Page reaches (the stage less 64px). These are measured on GameShell at the default text size.

| Screen | Folio, docked open | Folio, docked collapsed | Folio, floating | No Folio, docked open | No Folio, docked collapsed | No Folio, floating |
| --- | --- | --- | --- | --- | --- | --- |
| 1,280 | 696 · Narrow | 696 · Narrow | 696 · Narrow | 672 · Narrow | 1,008 · Medium | 1,056 · Medium |
| 1,440 | 856 · Medium | 856 · Medium | 856 · Medium | 832 · Medium | 1,168 · Wide | 1,216 · Wide |
| 1,536 | 568 · Stacked | 904 · Medium | 952 · Medium | 928 · Medium | 1,264 · Wide | 1,312 · Wide |
| 1,920 | 952 · Medium | 1,288 · Wide | 1,336 · Wide | 1,312 · Wide | 1,648 · Wide | 1,696 · Wide |
| 2,560 | 952 · Medium | 1,288 · Wide | 1,336 · Wide | 1,312 · Wide | 1,648 · Wide | 1,696 · Wide |

- **Docked, 1,536px and wider:** the Chronicle takes its own 384px column. Collapsed, it becomes a 48px strip.
- **Docked, under 1,536px:** with a Folio, the Chronicle shares the Folio's column, so opening or collapsing it doesn't change the stage. Without a Folio, it takes a 384px right-hand column, or a 48px strip when collapsed.
- **Floating:** the stage keeps its full width, but the drawer covers its bottom-right corner, 352px wide (384px from 1,536px) by 448px when open. Content under the drawer must stay reachable by scrolling. KeyHints move to the bottom left.
- **2,560px** is letterboxed. The shell stops at `shell-max`, 1,920px at the default text size, so the stage is as at 1,920px, with 320px of `ground-deep` on each side.
- **The 1,536px step.** At 1,536px the docked Chronicle takes its own column. With a Folio, the stage drops from 856px at 1,440px to 568px: narrower than at 1,280px, and Stacked. D-054 proposes that, with a Folio, the Chronicle take its own column only from 120rem. Until that is decided, a screen with a Folio must work Stacked at 1,536px.

**At larger text** the rail, Folio and Chronicle widen too, so the stage shrinks in px and the tiers (in rem) come sooner.

| Screen, text | Folio, docked open | Folio, docked collapsed | Folio, floating | No Folio, docked open | No Folio, docked collapsed | No Folio, floating |
| --- | --- | --- | --- | --- | --- | --- |
| 1,280, Large | 608 · Stacked | 608 · Stacked | 608 · Stacked | 581 · Stacked | 967 · Medium | 1,022 · Medium |
| 1,440, Large | 768 · Narrow | 768 · Narrow | 768 · Narrow | 741 · Narrow | 1,127 · Medium | 1,182 · Medium |
| 1,536, Large | 864 · Narrow | 864 · Narrow | 864 · Narrow | 837 · Narrow | 1,223 · Medium | 1,278 · Medium |
| 1,920, Large | 807 · Narrow | 1,193 · Medium | 1,248 · Medium | 1,221 · Medium | 1,607 · Wide | 1,662 · Wide |
| 2,560, Large | 1,095 · Medium | 1,481 · Wide | 1,536 · Wide | 1,509 · Wide | 1,895 · Wide | 1,950 · Wide |
| 1,280, Extra large | 521 · Stacked | 521 · Stacked | 521 · Stacked | 490 · Stacked | 926 · Narrow | 989 · Narrow |
| 1,440, Extra large | 681 · Stacked | 681 · Stacked | 681 · Stacked | 650 · Stacked | 1,086 · Medium | 1,149 · Medium |
| 1,536, Extra large | 777 · Narrow | 777 · Narrow | 777 · Narrow | 746 · Stacked | 1,182 · Medium | 1,245 · Medium |
| 1,920, Extra large | 1,161 · Medium | 1,161 · Medium | 1,161 · Medium | 1,130 · Medium | 1,566 · Wide | 1,629 · Wide |
| 2,560, Extra large | 1,238 · Medium | 1,674 · Wide | 1,737 · Wide | 1,706 · Wide | 2,142 · Wide | 2,205 · Wide |

At Extra large, 1,920px is below the Chronicle's own-column breakpoint (96rem, 1,997px), so the Chronicle stays under the Folio. The stage is wider than at Large as a result.

## Shell breakpoints

These widths are the shell's container width, in rem; the px figures are at the default text size. At Large (115%) and Extra large (130%) each breakpoint comes 15% or 30% sooner. The drawer takes over under 1,104px or 1,248px, and the Chronicle's own column needs 1,766px or 1,997px.

| Width | Rail | Folio | Chronicle, Docked | KeyHints |
| --- | --- | --- | --- | --- |
| 96rem (1,536px) and wider | Column | Column | Its own column at the far right | Stage foot |
| 60–96rem (960–1,535px) | Column | Column | Under the Folio, sharing its column, at most 45% of the screen's height | Stage foot |
| Under 60rem (960px) | Off-canvas drawer over `scrim` | Stacks under the stage | Bottom dock (both layouts) | Hidden |

Under 30rem (480px, or 400% zoom on a 1,280px screen) the Page's side padding steps down to `inset-lg`.

## Content tiers

Content inside the stage answers to four tiers, read from the width of its region.

| Tier | Region width | Holds | Typical regions |
| --- | --- | --- | --- |
| Wide | 68rem (1,088px) and up (`content-wide`) | Four Ledgers a row. A list and inspector at 3 : 2. A table with every column. | 1,920px without a Folio (1,248px of content). 1,920px with a Folio and the Chronicle collapsed or floating. 1,440px and 1,536px without a Folio, the Chronicle collapsed or floating. |
| Medium | 44–68rem (704–1,087px) (`content-medium`) | Two Ledgers a row, or three for three groups. A list and inspector side by side, the inspector at 20rem or more. | 1,920px with a Folio and the docked Chronicle (888px). 1,440px, docked. 1,536px without a Folio, or with one and the Chronicle collapsed or floating. |
| Narrow | 32–44rem (512–703px) (`content-narrow`) | Two Ledgers a row. A list and inspector in one column. Tables without priorities 3 and 4. | 1,280px, docked. 1,440px and 1,536px at Large text. |
| Stacked | Under 32rem (512px) | One column | The Folio (18.5rem inside). An inspector. 1,536px with a Folio and the docked Chronicle. 1,280px at Large and Extra large text. |

**Regions.** A layout reads its tier from the nearest region:

- the Page (its content box);
- the Folio's content;
- each cell of a list and inspector, or of a comparison;
- anything marked `.lg-region`.

A Panel is not a region. A layout inside a Panel follows the region around the Panel, which is two insets wider; the grids' minimum widths absorb the difference. Mark a box `.lg-region` when a layout inside it must follow the box, such as a column of your own grid or a dialog. The region the pointer or focus is in paints above its neighbours, so a tooltip or suggestion list that overflows a cell is never covered.

**In code:** `@container lg-region (width >= 44rem) { … }`. The rules are written mobile-first, so a layout with no region around it takes its Stacked form. Container conditions cannot read custom properties, so the stylesheet repeats the `content-*` token values. The Angular edition sets `container: lg-region / inline-size` on the same regions.

## Track layouts

Each track layout has one behaviour per tier:

| Layout | Wide | Medium | Narrow | Stacked |
| --- | --- | --- | --- | --- |
| List and inspector (`lg-split`) | Side by side, 3 : 2 | Side by side, 3 : 2 until the inspector reaches 20rem, then the list gives way (about 52 : 48 at 44rem) | One column: the list; a row opens the inspector in its place, with Back | As Narrow |
| Ledger grid, four groups (`lg-ledgergrid`) | 4 a row | 2 | 2 | 1 |
| Ledger grid, three groups (`lg-ledgergrid--3`) | 3 | 3 from 49rem, 2 below | 2 | 1 |
| Ledger grid, two groups (`lg-ledgergrid--2`) | 2 | 2 | 2 | 1 |
| Stat grid (`lg-statgrid`) | Up to 6 tiles | 4 | 3 | 2 |
| Data table (`lg-tablewrap`) | Every column | Priority 4 hidden | Priorities 3 and 4 hidden | Priority 1 only; scrolls inside its wrapper if still too wide |
| Split comparison (`lg-compare`) | Two panes, 1 : 1 | 1 : 1 | 1 : 1 | One above the other, current first |
| Main and side column (`lg-aside`) | The main column, and a 20rem side column beside it | As Wide | One column: the main column, then the side | As Narrow |

Every grid has a floor. A Ledger never gets narrower than `ledger-min` (15rem), and a tile never narrower than `stat-min` (8.5rem). When a row can't hold the tier's count at that width, it holds one fewer. That keeps the three-group grid at two a row between 44 and 49rem, and keeps every label whole at 130%. Grids keep their empty tracks, so a short row keeps the columns of the grid above it.

### List and inspector

```html
<div class="lg-split" data-view="list">            <!-- "inspector" once a row is opened -->
  <div class="lg-split__list"> … a Panel holding a List … </div>
  <section class="lg-split__inspector" aria-labelledby="item-name" tabindex="-1">
    <button class="lg-btn lg-btn--link lg-split__back">Back to Bags</button>
    …
  </section>
</div>
```

- **Ratio 3 : 2:** the list takes 60% and the inspector 40%. The inspector never gets narrower than `inspector-min` (20rem); the list gives way first. At 78rem the inspector is about 30rem; at 54rem, 21rem.
- **The split reads its own width**, since it is a container of its own (`lg-split`). A split inside a Panel follows the Panel.
- **In one column (under 44rem)** the list fills the region.
  - Opening a row sets `data-view="inspector"`: the inspector replaces the list, and its Back button shows.
  - Move focus into the inspector (its `section`, with `tabindex="-1"`) only when the list is hidden.
  - Back and Escape set `data-view="list"` and return focus to the row. Escape means Back only when no layer is open (Foundations · Accessibility · The keyboard model).
- **Each cell is a region,** so the inspector's content follows the inspector. At Wide the inspector is about 30rem, which is Stacked, so its stat grid holds two tiles a row.
- **The inspector is Comfortable,** like the Folio, whatever the list's density (Foundations · Space & Density).
- **The inspector's first view** is the selection's name, rarity, key numbers and actions. Longer detail goes below them.
- **One inspector per screen.** A screen with the Folio doesn't add one.

### Ledger grids

- **Four groups** (`lg-ledgergrid`) is the Character Overview's Offense, Defense, Recovery and Utility. It runs four a row, then 2 × 2, then one. Four groups never split three and one.
- **Three groups** (`lg-ledgergrid--3`) and **two groups** (`lg-ledgergrid--2`) are the other grids.
- **Two short Ledgers in a Wide region** go in the three-group grid, with its third column left empty. They then keep the width of the Ledgers around them, and their leaders stay short enough to follow.
- **Gaps:** `space-6` between rows of Ledgers, and the gutter, `section-md`, between columns.

### Stat grid

Tiles run 2, 3, 4 and then up to 6 a row, from Stacked to Wide. A tile never gets narrower than `stat-min`. A tile with a Delta needs more room, so in a Stacked region (the Folio) give it a one-word label. Six to eight tiles make a group; more want a Ledger.

### Data table

The table fills its region and hides columns by priority as the region narrows. Put `data-priority` on the `th` and every `td` of a column.

| Priority | Holds | Hidden in |
| --- | --- | --- |
| 1 | The row's name, the value the table exists for (the price in an order book, the rating in a ranking), and its one action | Never |
| 2 | What qualifies the row: quantity, level, owner or seller | Stacked |
| 3 | Supporting facts: time left, category, guild tag, last seen | Narrow and Stacked |
| 4 | Derived or repeated values: a total (quantity × price), an ID, the date added | Medium, Narrow and Stacked |

**Minimum widths by column:**

| Column | Minimum |
| --- | --- |
| A name (item, player, creature), with its rarity code | 12rem |
| A thumbnail | The row height, 1.5–3rem |
| A count or level (×12, Lv 142) | 4rem |
| A price, total or rating | 6rem |
| Short text: a status, time left, a category | 6rem |
| An action: one small Button | 6rem |

Set `--lg-table-min` on the wrapper to the sum of the priority-1 minimums. The Bazaar order book's Seller (12rem), Price each (6rem) and Buy (6rem) make 24rem.

**Horizontal scrolling is acceptable only when the priority-1 columns alone are wider than the region.**

- The table then scrolls inside `lg-tablewrap`, with its first column (the name) held in place.
- The wrapper is a focusable, named region (`tabindex="0"`, `role="region"`, `aria-label`), so the keyboard can scroll it.
- Strips of tabs, chips and currencies may also scroll within themselves.
- Nothing else scrolls sideways: not prose, forms, Ledgers, a list and inspector, or the page.

Under 32rem, prefer a List of ListRows (a name, one value, one action) over a table of three columns.

**Rows take one rhythm** (Foundations · Lines): a hairline between body rows by default, or every second row on `row-stripe` with `lg-tablewrap--zebra`, never both. One hairline closes the header and travels with it when it sticks; the table has no border around it.

### Main and side column

`lg-aside` sets a main column beside a narrow side column of supporting detail: the Character Overview's profile and Combat Style, with the Essence Loadout beside them (D-098).

```html
<div class="lg-aside">
  <div class="lg-aside__main"> … a Banner, Panels … </div>
  <aside class="lg-aside__side"> … a Panel … </aside>
</div>
```

- **From Medium (44rem)** the side column is `aside-width` (20rem) and the main column takes the rest. Below, they stack: the main column first.
- **Each column is a region**, so what sits in it follows its own width: in a 60rem Page the main column is about 38rem, so a Banner in it lays its figures under its identity.
- **Stack in order of use.** The main column holds what the screen is about; the side holds what supports it. Don't put the screen's primary action in the side column: on a narrow screen it ends up last.
- The layout reads its own width (`lg-aside`).

### Split comparison

`lg-compare` puts two things side by side: the equipped item and a candidate, two builds, or two players. The panes are 1 : 1 from 32rem. Below that they stack, current first: reading order is DOM order.

- Both panes carry the same rows in the same order, with "—" where a value is missing, so the rows pair up.
- The candidate shows each difference with a Delta, coloured by whether it helps (Foundations · Numerals).
- The comparison is its own container (`lg-compare`).

## Reading width and full bleed

- **Prose keeps to `--lg-measure`, 68ch** — about 68 characters however wide the region. Prose means descriptions, lore, quest text, help and summaries.
  - Use `.lg-prose`. PageHeader and JourneyCard summaries already hold the measure.
  - In a narrower region, prose fills the region.
  - In the readable font the same 68ch is wider in px, as it should be.
  - The measure lives in the stylesheet, because the token format has no `ch` unit.
- **A Page's content stops at `page-max`** (80rem, 1,280px) and centres.
- **Full bleed** (`.lg-bleed`) runs edge to edge of the stage, through the Page's cap and side padding. It is for art only: a Banner's art, a scene, a map. Text over bled art keeps to the content edges.
- **Tables, Ledger grids and forms never bleed.** A table wider than `page-max` scrolls inside its own region.

## Alignment and rhythm

- **One left edge.** Every section on a Page starts at the Page's content edge. That goes for headings, prose, a table, a Ledger grid, and the outer edge of a Panel or boxed section. Never indent a heading to line its text up with text inside a box: the box's edge is on the line.
- **One gutter.** Columns in every track layout are separated by `section-md`, so columns in stacked sections line up wherever their counts match.
- **Tops and first baselines align.** Side-by-side cells start at the same top edge. Cells that open with a heading use the same heading level, so their first baselines match.
- **A 4px vertical rhythm.** Every gap, padding, row height and control height is a multiple of 4px (`space-1`); hairline borders aside.
  - Sections stack in three steps: `section-sm` (24px) between Panels and blocks on a Page, `section-md` (32px) between major regions, and `section-lg` (48px) above a screen title.
  - A heading sits closer to what it heads (`stack-xs` to `stack-md`) than to the section before it.
- **Line heights are exempt.** They come from the type ramp and are even, but not all multiples of 4 (body is 22px). A block of text may end off the grid, but every section box starts and ends on it, so the next section is back in rhythm. Don't trim line boxes with negative margins to force a baseline grid.

## At larger reading sizes

Every tier, minimum and gutter is in rem. At Large and Extra large text a region reaches a smaller tier sooner, and every minimum grows with the text, which keeps labels whole. The specimens' three regions:

| Region | Default | Large (115%) | Extra large (130%) |
| --- | --- | --- | --- |
| 1,248px | 78rem · Wide: 4 Ledgers a row; list and inspector at 3 : 2 | 67.8rem · Medium: 2 a row; side by side | 60rem · Medium: 2 a row; side by side |
| 864px | 54rem · Medium: 2 a row; the inspector at its minimum | 47rem · Medium: 2 a row; side by side | 41.5rem · Narrow: 2 a row; one column |
| 507px | 31.7rem · Stacked: 1 a row; one column | 27.6rem · Stacked | 24.4rem · Stacked |

At every step, in both specimens, no label truncates and nothing scrolls sideways.

## Tokens used

| Token | Value | Role |
| --- | --- | --- |
| `rail-width` | 14rem (224px) | NavRail column |
| `folio-width` | 22.5rem (360px) | Folio column |
| `topbar-height` | 3.5rem (56px) | TopBar height; the Page pads its top by it, and scrolls focus clear of it |
| `hintbar-height` | 2.5rem (40px) | KeyHints strip at the stage's foot |
| `shell-max` | 120rem (1,920px) | Widest the shell grows |
| `page-max` | 80rem (1,280px) | Widest a Page's content grows |
| `content-wide`, `content-medium`, `content-narrow` | 68, 44 and 32rem | Where the Wide, Medium and Narrow tiers start |
| `ledger-min` | 15rem (240px) | The narrowest Ledger in a grid |
| `stat-min` | 8.5rem (136px) | The narrowest StatTile in a grid |
| `inspector-min` | 20rem (320px) | The narrowest inspector beside its list |
| `aside-width` | 20rem (320px) | The side column beside a main column (`lg-aside`) |
| `section-md` | 2rem (32px) | The gutter between columns (`--lg-gutter`) and the Page's side padding |
| `section-sm`, `section-lg` | 24 and 48px | Section spacing |
| `space-1` | 4px | The rhythm unit |
| `--lg-measure` (stylesheet) | 68ch | The reading width |

The Chronicle's own sizes (`chronicle-*`) are listed in Shell.

## Do and don't

| Do | Don't |
| --- | --- |
| Switch the Overview's attribute grid on its region: four a row at Wide, two at Medium and Narrow. | Switch it on the window, and squeeze four Ledgers into 888px on a 1,920px screen with a Folio. |
| Hide an order book's Total column first (priority 4). | Hide Price each, or the seller's name. |
| Let a 24rem order book scroll inside its wrapper at 130%, the seller's name held. | Let the page scroll sideways. |
| Keep a quest description at 68ch in a Wide region. | Run it 1,200px across. |
| Run a Banner's art edge to edge with `.lg-bleed`, and keep its text on the content edge. | Bleed a Ledger grid or a table. |
| Put the section heading, the Panel's edge and the table on one left edge. | Indent the heading to line up with the Panel's text. |
| Put the inventory's inspector in the content, and the archive's in the Folio. | Put an inspector beside the list and use the Folio as well. |
| Test the Stacked form at 1,536px with a Folio. | Assume a wider screen means a wider stage. |
| Let the stage, Folio and Chronicle scroll inside a full-height shell. | Let the whole browser page scroll. |

## Related components

- LayoutSpecimen — the list and inspector at three region widths
- LayoutGridSpecimen — the attribute grid at three region widths
- GameShell — the screen frame
- Page — the information screen frame
- Folio — the detail panel
- ListRow — the list row
- Ledger — the labelled value list
- StatTile — the compact stat
- PageHeader — the information screen heading
- Banner — the headline block
- JourneyCard — the next-step guide
- Stage — the scene backdrop
- NavRail — the main navigation
- Constellation — the stat star chart
