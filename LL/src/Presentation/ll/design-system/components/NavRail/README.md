# NavRail

The main navigation.

**Status:** Draft

The game's primary navigation: the sidebar's real sections (Character, World, City, System) as a quiet column of spaced capitals, three words at most.

**Provide:** `sections` (`[{ label, items: [{ id, title, icon, badge?, badgeLabel?, locked?, reason?, ready? }] }]`), `activeId`, `onNavigate`, and a `header` (the Logo asset plus the wordmark). A locked item needs its `reason`: how it unlocks.

- Items are `nav` style in `ink-muted`; group labels are `label` style in `ink-muted` with a trailing `line` rule (D-021). Hover takes the neutral `surface-raised` wash.
- The active item is `nav-active` — Barlow 600, a weight change rather than a face change (D-030) — in `ink` with a small gilt diamond and a gilt icon: where you are is gilt's first job (brand and current location), and the diamond's (Foundations · Shape). Items are rows, so their hover wash takes `radius-container`.
- Badges (new items, quests ready) use `arcana` on `arcana-soft` — arcana's one meaning: ready, new or actionable — as small rectangles at `radius-control`, not circles: the circle means presence (Foundations · Shape). Give them a `badgeLabel` for screen readers. Something waiting with nothing to count takes `ready` instead: the `arcana-glow` diamond in the badge's place. An item's end holds one mark: Locked, else the badge, else the diamond (Standards · State combinations). In `compact` the badge or diamond sits on the square's top end corner.
- **Motion** (Foundations · Motion): hover fades the `surface-raised` wash in and out over `duration-fast` on `ease-standard` — a layer's opacity, not a background transition — and the title turns `ink` at once. The active item, its weight and its gilt diamond move at once: the current location never animates. As GameShell's drawer under 60rem, the rail slides in over `duration-base` on `ease-enter` and out over `duration-fast` on `ease-exit`, then hides. Under reduced motion all of it happens at once.
- Item icons are 20px (`icon-md`), in `ink-muted`, and `gilt` on the active item (Foundations · Iconography).
- `compact` shows icons only, for a collapsed desktop rail the player chose: each keeps its section's name as tooltip and accessible name. It is the one place a navigation icon stands alone.
- **Locked items stay in reach** (Standards · States, D-087). The title is `ink-disabled` with "Locked" where the badge goes (the 12px lock marker replaces the word once it is drawn); it stays in the Tab order (`aria-disabled`), takes no hover wash, and its unlock condition opens in the reason tip beside the rail on hover and focus. A click or Enter pins the condition and announces it, and never navigates. In `compact` the tip names the item too. It used to leave the pointer out (`pointer-events: none`) while staying in the Tab order — the gap the audit found.
- Don't add icons of your own; use `Icon` names so every item matches.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | Text: `nav` in `ink-muted` | The title | "Inventory, link" |
| Hover | Fill: the `surface-raised` wash; the title turns `ink` | — | Nothing |
| Focus-visible | Edge: `focus-ring` | — | Its name and state |
| Current | Marker: the gilt diamond; Typeface: `nav-active` (600) in `ink`; a `gilt` icon | — | "current page" |
| Unread, Ready | Marker: an `arcana` count badge; with nothing to count, the `arcana-glow` diamond | The count: "3" | The `badgeLabel`: "3 new items", or `ready`'s words: "Quest ready" |
| Locked | Text: `ink-disabled` and "Locked"; the reason tip beside the rail | "Locked", then "Unlocks at level 20" | "Colosseum, link, unavailable. Locked. Unlocks at level 20." |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `nav` named by `label` ("Game"); items are links named by their title, the current one `aria-current="page"`; a locked one `aria-disabled`, described by its condition |
| Keyboard | Tab moves through the items, locked ones included; Enter follows a link, or on a locked item shows its condition |
| Focus | `focus-ring` |
| Announced | The title, "current page", a badge's `badgeLabel`, a locked item's condition |
| Hover and tap | The wash; in `compact`, the title as a tooltip. A locked item opens its reason tip, which a tap pins and Escape closes |
| Target size | 30px rows; 40px squares in `compact` |
| Text scaling | Titles truncate; the rail scrolls |
| Colour | Current is also a diamond and a weight; a badge is also a number; locked is also its word |
| Motion | The wash fades over `duration-fast`; the drawer slides over `duration-base` in and `duration-fast` out; at once under reduced motion |

