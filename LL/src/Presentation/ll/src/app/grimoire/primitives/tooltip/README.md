# Tooltip

A short explanation beside the thing under the pointer or focus.

**Status:** Draft

**Use:** `<button lgButton="quiet" lgTooltip="Sort by rarity, then by name">Sort</button>`.

**Provide:** `lgTooltip`, the words: a string, `{ title, text, meta, kind }` for an explanation, or an `ng-template` with `lgTooltipDescription` for what screen readers hear. Optional `lgTooltipPlace` (`below`, the default, or `end`) and `lgTooltipPin`, which lets a press pin it open.

The tooltip is the tip: one float for the page, on the CDK overlay, that a tooltip, a Ledger row's explanation and a blocked control's reason (Standards · States · The reason tip) all use (D-134). Showing it for one element moves it from any other. A part that is its own tooltip's element, as a Ledger row is, drives it with `LgTooltipController`, the directive's behaviour without the directive (D-137).

## When to use

- A short explanation of something already on the page: what a sort order does, what an abbreviation stands for, what a stat means (`kind: 'explanation'`, as Ledger rows use it).
- A control whose own label isn't visible, such as an icon-only button: its tooltip says the same words as its `aria-label`.

## When not to use

- As the only home of something that matters. Put it on the page too, or in the element's name (Foundations · Accessibility · Content on hover or focus).
- For why a control can't be used: that is the reason tip, through `lgBlocked` (Standards · States).
- For anything interactive inside it. A tooltip holds words; a popover holds controls.

## Anatomy

1. **The float** — a Level 2 floating surface: `surface-raised`, a `line-strong` edge, `radius-float`, `shadow-float`, at most 18rem wide, with `space-2` by `space-3` padding.
2. **Title** (optional) — `body-compact` at weight 600 in `ink`; for an explanation, the display face at `title-sm`.
3. **Word** (optional) — a state's word in `label` capitals, `ink-muted`. The reason tip uses it.
4. **Text** — `body-compact` in `ink`, one line per line break; `ink-muted` in an explanation, `warning` for a shortfall.
5. **Footnote** (optional) — `caption` at weight 600 in `ink`.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Hidden | — | — | The words, as the element's description |
| Shown | The float beside its element | Its words | Nothing more: it was already the description |
| Pinned | The float stays when the pointer leaves | Its words | Nothing more |

## Content rules

- One or two short sentences. Sentence case, no terminal punctuation on a fragment.
- Say what something means or does, not what it is called again.
- Numbers formatted by Foundations · Numerals.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | The float is `aria-hidden`: its words are the element's description (`aria-describedby`), through the CDK's `AriaDescriber` |
| Keyboard | Shows on keyboard focus (`:focus-visible`) and closes when focus moves on. The element must be focusable to reach keyboard users |
| Focus | Never takes focus |
| Announced | Its words, as the element's description, when the element is focused |
| Hover and tap | Shows on hover, stays while the pointer is on it, closes 150ms after the pointer leaves (WCAG 1.4.13). With `lgTooltipPin` a tap pins it and a second tap closes it. A press elsewhere and Escape close it; Escape reaches it before anything else on the page |
| Target size | Its element's |
| Text scaling | rem throughout; it wraps at 18rem |
| Colour | The warning text is also said in words ("Short by 250 Cinders") |
| Motion | Fades and rises in over `duration-fast` on `ease-enter`, out on `ease-exit`. At once under reduced motion |

## Related components

- Button — the command button, whose blocked state uses the same tip
- Ledger — the labelled value list, whose rows explain themselves with it
