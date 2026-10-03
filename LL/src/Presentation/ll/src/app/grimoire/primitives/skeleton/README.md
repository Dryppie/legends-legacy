# Skeleton

The still blocks of the Loading state.

**Status:** Draft — its look is Proposed (D-145), for review

What a region shows while its content is fetched for the first time: blocks in the shape of what is coming, so the layout is in place when it arrives (Standards · States · Data · Loading). Usually inside a Region state, which gives the region its busy state and its words.

**Use:**

```html
<lg-region-state state="loading" heading="Loading members…">
  <lg-skeleton shape="rows" count="5" />
</lg-region-state>
```

**Provide:** `shape` — `text` (the default: lines of the region's text, the last of several shorter), `rows` (rows at the region's row height, for a list or a table) or `block` (a thumbnail, a portrait, a tile); `count` (lines, 1 by default, or rows, 3); `width`; and a block's `height` (the thumbnail's size, `--lg-thumb`, by default).

- **Still blocks** of `surface-raised` at `radius-control`. Nothing moves: no shimmer and no pulse, which Foundations · Motion forbids.
- **They wait 300ms** before they show, so a quick load never flashes them.
- **Rows follow the density:** in a Compact region they are Compact rows.
- **For the eye only** (`aria-hidden`): screen readers hear the region's `aria-busy` and, after a second, what is loading.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Loading | Fill: still `surface-raised` blocks, after 300ms | None of its own: the Region state's | Nothing of its own |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | None: `aria-hidden` |
| Keyboard | Not focusable |
| Focus | — |
| Announced | The region's busy state and words, not the blocks |
| Hover and tap | Nothing |
| Target size | — |
| Text scaling | Lines and rows are rem, from the type and density tokens |
| Colour | The blocks are a fill, not a message |
| Motion | None: they appear once, after 300ms |

## Related components

- Region state — the busy region and its words
- List, Table — what rows stand in for
