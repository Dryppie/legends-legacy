# Foundations · Accessibility

Every player can read, reach and understand everything in Legend's Legacy, at the text size, font and motion they chose. The target is **WCAG 2.2 level AA**. The game already has a reading-font setting, a reading-size setting, reduced-motion handling, visible focus rings and focus-trapped dialogs. The system honours every one of them, and none may regress. This section sets what AA means here: text scaling, the keyboard model, target sizes, hover content, live regions, and names and numbers.

## Rules

**Must**
- Meet WCAG 2.2 AA on every screen: contrast, target size, focus, keyboard, text scaling (the table below).
- Size in rem everything that sets or holds text: type, line heights, spacing, density, layout and component boxes. The whole interface then follows the reading-size setting and the browser's own text size.
- Honour the game's settings: reading font (`data-reading-font="readable"` or `"system"`), reading size (`data-reading-font-size="large"` or `"extra-large"`) and the operating system's reduced-motion setting.
- Show `focus-ring` on every interactive element and never remove it. Keep it clear of anything that overlays the page, such as the TopBar. In a forced-colours theme it falls back to a system outline.
- Make every action work from the keyboard, in reading order, with one tab stop per list, tab set or grid (the keyboard model below).
- Give every target at least 24 × 24px (`target-min`), and primary actions 32px (`target-primary`).
- Make anything that shows on hover also show on keyboard focus and pin on tap. Nothing exists only on hover.
- Announce errors, claim results and completed activities, through the throttled announcer. Never announce every combat event.
- Name every icon-only control, read abbreviated numbers in full ("12.5k" is read as "12,480 Cinders"), and announce rarity by its name.
- Carry every colour meaning a second way (below).

**Should**
- Test every new screen at 100%, 115% and 130% text, with the keyboard alone, and with a screen reader, before it ships (Testing, below).
- Give compound units a spoken form where a screen reader would stumble: "84 HP/5s" as "84 health per 5 seconds".
- Let players remap or turn off single-key hotkeys, and never fire them while a text field has focus (WCAG 2.1.4).

**Never**
- Set a text size, line height, gap or box that holds text in px.
- Remove or restyle away the focus ring, or clip it with `overflow: hidden`.
- Put information only in a hover, a colour or a native `title`.
- Move focus with a status message, or make a live region of a stream: combat, timers, ticking currency.
- Rely on red versus green. `success` has no hue at all: it is `ink` with ✓ and its word (D-016). Red Bleed and green Poison are told apart by the type's name.

## What AA means here

| Requirement | Here | WCAG 2.2 |
| --- | --- | --- |
| **Text contrast** | 4.5:1 for text under 24px (under 18.66px bold), 3:1 above, on `ground`, `surface`, `surface-raised` and `folio`. `ink` measures 12.74–15.32:1 and `ink-muted` 6.89–8.28:1; on a Panel's translucent `surface` the text is also measured over the frame's backdrop (D-102). Two tokens carry rules: `ink-disabled` is for plain disabled controls and the names of locked things only, since inactive controls are exempt — the word "Locked" and the unlock condition beside a locked name are `ink-muted` and must pass — and `damage-bleed` goes on `ground`, `folio` or large bold text. Every ratio is in Foundations · Colour · Contrast. | 1.4.3 |
| **UI edge contrast** | 3:1 for anything a player must see to use a control or tell its state: input and button outlines, tab separators, the selected ring, meter fills against their track. `line-strong` measures 3.56–4.23:1; `line` is for decorative hairlines only. | 1.4.11 |
| **Target size** | At least 24 × 24px (`target-min`), or enough space that a 24px circle round the target touches no other. Primary actions get 32px (`target-primary`). Links inside a sentence are exempt. | 2.5.8 |
| **Focus visible** | The `focus-ring`: a 2px `ground` gap, then a solid 2px `focus` ring at 11.14–13.22:1. It shows on every side, even on a Compact row. No sticky part may cover the focused element: the Page scrolls focus clear of the TopBar, and a drawer or dialog makes the rest inert. | 2.4.7, 2.4.11 |
| **Keyboard** | Everything works from the keyboard, in reading order. There are no traps except a dialog's deliberate focus trap, which Escape leaves. A "Skip to content" link is the first tab stop. | 2.1.1, 2.1.2, 2.4.1, 2.4.3 |
| **Text resize and reflow** | Text scales to 130% through the reading-size setting and to 200% with browser zoom, without loss. At 320px wide (400% zoom) the page never scrolls sideways; strips of tabs, chips and currencies scroll within themselves. The stage, a Constellation and data tables are two-dimensional content and are exempt. | 1.4.4, 1.4.10 |
| **Text spacing** | Boxes that hold text size to it (rem, min-heights), so a player's own line or letter spacing clips nothing. | 1.4.12 |
| **Hover and focus content** | Dismissible with Escape, hoverable without closing, and open until the pointer or focus leaves. It also opens on focus and pins on tap. | 1.4.13 |
| **Status messages** | Announced through a live region without moving focus, and throttled. | 4.1.3 |
| **Name, role, value** | Every control has a name, a role and its state (`aria-pressed`, `aria-selected`, `aria-expanded`). | 4.1.2 |
| **Use of colour** | Never the only carrier of meaning. | 1.4.1 |
| **Motion** | Nothing loops or runs past five seconds, except the two loops Foundations · Motion allows: an indeterminate progress indicator while its wait lasts, with its label, and live combat playback, the activity itself. Reduced motion ends every transition at once. Live updates never move the control under the pointer. | 2.2.2 |

## The game's settings, honoured

| Setting | Values | How the system honours it |
| --- | --- | --- |
| Reading font | Game default · Readable sans · System | `data-reading-font="readable"` maps every role to Atkinson Hyperlegible (`font-readable`). `"system"` maps every role to the platform's UI font (`font-system`); Grimoire did not honour this before. |
| Reading size | Default · Large · Extra large | `data-reading-font-size="large"` or `"extra-large"` sets the root to 115% or 130%, and everything in rem follows (Text scaling, below). |
| Reduced motion | The operating system's setting | Under `prefers-reduced-motion: reduce`, every transition and animation in the system ends at once and scrolling is instant, matching the game's own rule. `data-motion="reduced"` on the root does the same, for an in-game setting. Each motion category's alternative — usually an instant change, with the live mark's brief colour change — is in Foundations · Motion. |
| Focus rings | Always on | `focus-ring` on every interactive element, never removed. In forced colours, a system outline. |
| Focus-trapped dialogs | Always on | The game's dialogs (its `appDialogFocus` directive) set `role="dialog"` and `aria-modal`, trap focus, focus the first control, close on Escape and return focus to the opener. Grimoire's future Dialog keeps all of it. The GameShell rail drawer follows the same model: it takes focus, makes the rest inert, closes on Escape and returns focus. |

## Text scaling

Grimoire's type, spacing, density and layout tokens are in **rem**, and so is every component box that holds text. One rem is the root's font size:

- 16px at Default, or whatever the player's browser asks for;
- 18.4px at Large (115%);
- 20.8px at Extra large (130%).

Pixel figures in these docs are at Default. The only lengths left in px are hairlines, borders and the focus ring's 2px width (1–3px), radii, shadows and ornament frames: lines that do not need to grow.

**Breakpoints are rem as well**, so the shell reflows sooner as text grows and the same window holds fewer, wider columns. The rail becomes a drawer under 60rem, and the Chronicle takes its own column from 96rem. That is 960px and 1536px at Default, 1104px and 1766px at Large, and 1248px and 1997px at Extra large.

**Tested steps** (GameShell with a Folio and the Docked Chronicle):

| Screen | 100% (body 15px) | 115% (body 17.25px) | 130% (body 19.5px) |
| --- | --- | --- | --- |
| 1920 × 1080 | Rail 256, stage 920, Folio 360, the Chronicle in its own 384px column | Rail 294, stage 770, Folio 414, the Chronicle in its own column | Rail 333, stage 1119, Folio 468; the Chronicle moves under the Folio |
| 1440 × 900 | The Chronicle under the Folio; stage 824 | The Chronicle under the Folio; stage 731 | Stage 639; the Folio scrolls |
| 1280 × 720 | Stage 664; the Folio scrolls | Stage 571; the Folio scrolls | Stage 479; the Folio scrolls |
| 1024 × 768 | Stage 408, every column kept | The rail becomes a drawer, the Folio stacks under the stage, and chat docks at the bottom | As at 115% |

**What reflows at larger text:**

- Content follows the four content tiers of its region: Wide from 68rem, Medium from 44rem, Narrow from 32rem, Stacked below (Foundations · Layout). The ledger grid drops from four Ledgers a row to two below Wide and to one when Stacked, and never makes a Ledger narrower than `ledger-min`.
- A list and its inspector go to one column under 44rem; the inspector then replaces the list, with Back.
- The Banner goes to one column below Wide; the JourneyCard moves its next unlock under the main block, and PageHeader actions wrap, below Medium.
- Data tables hide columns by priority, then scroll inside their own region with the name held.
- The TopBar's currency names give way to their art under 40rem. The full name is still read out and shown in the tooltip.
- The TopBar's centre Track drops its end labels under 16rem.
- ListRow meta truncates, and Tags and Buttons grow with their text.
- The docked Chronicle under the Folio never takes more than 45% of the screen's height.

**What scrolls, and only inside its own region:**

- vertically: the Page, the Folio, the Chronicle log and a scene List;
- sideways: primary Tabs, the Chronicle's channel tabs and, at 320px, the TopBar's currency strip.

The page itself never scrolls sideways, and nothing is clipped.

**Settings labels.** The reading-size options read Default, Large (115%) and Extra large (130%). They used to read "14px", "16px" and "18px", which stopped being true when the base became 16px (D-096).

**Migrating the game: done (D-096).** The game set its root to 14px, and its legacy styles assume that base. Its root now follows Grimoire — 100% (16px, or the browser's own size), 115% and 130% — and the build multiplies every legacy rem by 0.875, so legacy screens keep their size at Default and come out slightly larger than the old 16 and 18px at Large and Extra large, never smaller. Media queries keep their rem: there it is the browser's 16px, not the root's.

## The keyboard model

**Tab order follows reading order,** which is DOM order:

1. "Skip to content"
2. The NavRail
3. The TopBar
4. The stage or Page
5. The Folio
6. The Chronicle

Never use a positive `tabindex`. Hidden and off-screen things are out of the order, and anything behind an open drawer or dialog is `inert`. **Blocked things stay in it** — unavailable, locked, restricted, short of a cost, cooling down: they are `aria-disabled`, not `disabled`, their reason is their description, and Enter or Space shows the reason in the reason tip and announces it (Standards · States, D-087). Only a plain disabled control, whose limit is obvious beside it, leaves the order.

**One tab stop per widget (roving tabindex).** Each list, tab set or grid is a single tab stop — the selected item, or else the first. The arrow keys move within it:

| Widget | Keys | What moves |
| --- | --- | --- |
| Tabs, the Chronicle's channels | Left, Right (wrapping); Home and End in Tabs | Focus and selection together: the tab opens as it takes focus |
| A selectable List (the scene list) | Up, Down (wrapping), Home, End, a name's first letters | Focus and selection together; a blocked row takes focus but not selection |
| A List of things to act on | Up, Down, Home, End, a name's first letters; Right into a row's trailing region and Left back to the row | Focus only; Enter or Space presses the row's action |
| Ledger (rows that explain themselves) | Up, Down, Home, End | Focus, with the explanation opening on each row |
| Constellation | Arrow keys, Home, End | Focus between Sigils, locked ones included; Enter or Space selects, or shows a locked Sigil's condition |
| Grids (the future inventory grid, the Table) | Arrow keys in two dimensions; Home and End within a row; Ctrl + Home and Ctrl + End to the first and last cell; Page Up and Page Down by a screenful | Focus only |

**Escape closes the topmost layer and stops there.** Each layer stops the key from reaching the next, in the layer stack's order (Foundations · Surfaces & Layering · The modal stack):

1. a drag in progress, which is cancelled;
2. the guided tour;
3. a popover detached from a dialog (the Guild Vault's hover card);
4. a confirmation, with focus back to the button that opened it;
5. a dialog, with focus back to its opener;
6. a hover card, tooltip, suggestion list or menu on the page (a Ledger explanation, SearchField);
7. the rail drawer, with focus back to the menu button.

The CDK overlay keeps this order for everything on it, the latest opened first, and the tip hears Escape before anything (D-134). The rail drawer and the Objective's tracker, which aren't overlays, leave an Escape something above them already took alone.

Only when no layer is open may a screen use Escape for Back, and its KeyHints then say so.

**Focus moves with the player, never away from them:**

- Opening a layer puts focus in it.
- Closing a layer returns focus to the control that opened it.
- Removing a row moves focus to the next row.
- A status message never moves focus.

## Target sizes

| Part | Target |
| --- | --- |
| Buttons | 44, 40 or 32px high by density, never less than 32px |
| Inputs, primary tabs | The control height: 44, 40 or 32px |
| Secondary tabs | 24–32px |
| A link Button standing alone | At least 24px (`target-min`) |
| A ListRow | Its action covers the whole row (32–48px) |
| Sigils | 40, 52 or 68px |
| Chronicle toggles and the send button | At least 24px |
| An ItemLink inside a sentence | Exempt as inline text; give it a separate target where it is the only way in |

Primary actions — the `solid` Button, the committing action of a dialog, Buy in an order book — get at least 32px (`target-primary`), whatever the density.

## Hover, focus and tap

Nothing exists only on hover. Every hover card or tooltip:

- opens on hover and on keyboard focus;
- pins open on click or tap, and closes on a second tap or a tap elsewhere;
- stays open while the pointer is over it;
- closes on Escape, or when the pointer or focus leaves (unless it is pinned).

The tip works this way: `lgTooltip`, a Ledger row's explanation and the reason tip on blocked controls (Standards · States · The reason tip) are one float for the page, on the CDK overlay, and it sits on top of whatever opened it, so Escape closes it first (D-134). Its words are also the element's description. A short CurrencyPill is a button that toggles to the full figure. StatFigure, ItemLink, Track and the ItemSlot code still use a native `title`; each moves to `lgTooltip` in its plan step. What they hold is also on screen or read out in the meantime.

## Live regions in a realtime game

The game changes every second, and a screen reader can speak only one thing at a time. So announce outcomes, not streams.

| Announce | Politeness | Example |
| --- | --- | --- |
| Errors and failed actions | Assertive | "Not enough Soulstones: short by 12" |
| Claim results | Polite | "Claimed 120 Cinders and Soul Prism" |
| Completed activities | Polite | "Floor 3 cleared", "Upgrade complete", "Order filled: 3 Dire Wolf Essence" |
| Being outbid, loot landing, whispers and mentions (when the player chooses) | Polite | "Outbid on Crown of Cinders" |

**Never announce:**

- every combat event or damage number;
- a countdown;
- currency or HP ticking;
- every line of a busy chat.

Summarise a fight when it ends ("Victory. 3 enemies defeated, 120 Cinders"). The combat log stays readable on demand: it is focusable, not live.

**One announcer, throttled.** `LgAnnouncer.announce(text, { key, assertive })` speaks through the CDK's `LiveAnnouncer`, one hidden live region for the whole app (D-134):

- polite messages go out one at a time, at most one every 1.5 seconds;
- a message with the same `key` replaces the one still waiting, so ten loot drops become one line;
- the same text is not repeated within 5 seconds;
- at most three messages wait;
- `assertive` is for errors only.

Components add no live regions of their own. The one exception is the Chronicle log, where `announce` chooses: `all` (the default, today's behaviour), `mentions` (only lines that mention you and whispers to you) or `off`.

## Names and numbers

- **Icon-only controls name their action:** "Open navigation", "Collapse chat", "Send", "Drag chat". Decorative icons are `aria-hidden`; a standalone meaningful Icon takes a `title`.
- **Abbreviated numbers are read in full.** CurrencyPill shows "12.5k" and screen readers hear "12,480 Cinders". Anything that uses `lgFormatShort` pairs it with the full figure in visually hidden text (`.lg-sr`), and shows the full figure on focus, click or tap as well as hover: a short CurrencyPill is a button that toggles between 12.5k and 12,480.
- **Changes and counts are read as words.** Delta says "+12, better"; a ListRow says "Quantity 3"; a Meter says "3,120 of 4,150".
- **Rarity is announced by name.** ItemLink, ItemSlot and ListRow say "Epic" wherever they show the code E, and rarity Tags spell the rarity out.

## Colour never carries meaning alone

Every colour has a second carrier, and colour-blind players need it (Foundations · Colour · Colour-vision checks):

- rarity: its code, with the name announced;
- damage: the type's name with every number;
- status colours: a word or icon;
- success: ✓ and its word;
- deltas: ▲ or ▼ and a sign, or ±0;
- conditions: the effect's name;
- meters: the numbers beside the bar;
- selection: a shape, and a weight or face change;
- Presence: the words, not the dot.

## Tokens used

| Token | Role here |
| --- | --- |
| `focus`, `focus-ring` | The focus ring colour and its two-ring shadow |
| `line-strong` | Every edge a player must see (3:1) |
| `ink`, `ink-muted` | Text that holds 4.5:1 on every ground |
| `ink-disabled` | Plain disabled controls and the names of locked things only, a locked name always with the word or a lock |
| `target-min`, `target-primary` | 24px minimum target; 32px for primary actions |
| `text-*`, `leading-*`, `space-*`, semantic spacing, `density`, `layout` tokens | All in rem, so they follow the reading-size setting |
| `font-readable`, `font-system` | The reading-font setting |
| `topbar-height` | How far the Page scrolls focus clear of the TopBar |

## Testing

Before a screen ships:

1. Use the keyboard alone: every action reachable, in order, with a visible ring and one stop per widget; Escape as above.
2. Run a screen reader, NVDA with Firefox or Chrome, and VoiceOver with Safari: names, states, rarity, full numbers, and announcements that neither flood nor stay silent.
3. Check text at 100%, 115% and 130%, at 1280 × 720 and 1920 × 1080; and at 200% browser zoom.
4. Check at 320px wide: no sideways page scroll.
5. Turn on reduced motion and a forced-colours theme.
6. Check contrast for any new token (Foundations · Colour · Contrast).

## Do and don't

| Do | Don't |
| --- | --- |
| Set a Tag's height in rem, so its text never overflows at 130%. | Fix it at 20px. |
| Let the Ledger's explanation open on focus, pin on tap and close on Escape. | Show it only on hover. |
| Announce "Floor 3 cleared" once. | Announce every hit in the fight. |
| Read "12.5k" as "12,480 Cinders". | Let a screen reader say "twelve point five k". |
| Make the inventory list one tab stop, with arrows inside. | Make players tab through 60 rows. |
| Close a suggestion list on Escape and stop there. | Let the same Escape also close the drawer behind it. |
| "Rare" and the code R beside a blue item name. | A blue name as the only sign of rarity. |
| A locked entry that says "Locked", takes focus and gives its unlock condition. | A greyed entry that Tab skips, or that takes focus and says nothing. |

## Related components

- GameShell — the screen frame
- Ledger — the labelled value list
- List — the list and its rows, and the scene name list
- Tabs — the tabs
- SearchField — search with suggestions
- Button — the command button
- CurrencyPill — the currency amount
- Chronicle — chat and the game log
- Constellation — the stat star chart
- NavRail — the main navigation
- KeyHints — the keyboard shortcut hints
