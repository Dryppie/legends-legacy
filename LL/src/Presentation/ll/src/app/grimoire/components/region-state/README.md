# Region state

What a region shows in place of its content.

**Status:** Draft — its look is Proposed (D-145), for review

A Panel, a List, a table or a whole screen with nothing to show yet: it is loading, it has nothing in it, a filter hid everything, or it failed to load (Standards · States · Data). The Region state says which, in the words of Standards · States, and offers the way forward.

**Use:**

```html
<lg-region-state state="empty" heading="No listings yet.">
  List an item to sell it.
  <lg-region-state-actions><button lgButton size="sm" (click)="list()">List an item</button></lg-region-state-actions>
</lg-region-state>

<lg-region-state state="loading" heading="Loading members…"><lg-skeleton shape="rows" count="5" /></lg-region-state>
```

**Provide:** `state` — `loading`, `empty`, `no-results` or `error`; `heading`, the sentence ("No listings yet.", "No matching players", "Couldn't load the roster.", "Loading members…"); the next step's words as content, or while loading the Skeleton in the shape of what is coming; and the way forward in `<lg-region-state-actions>`: "List an item", "Clear filters", "Try again". Import `LG_REGION_STATE` for both.

- **Empty is not no results.** Empty means nothing exists yet, so it offers a way to make something; no results means a filter or a search hid everything, so it offers a way to undo it.
- **Error** is the ✕ and its sentence in `danger`, with a way to try again. A failed save the player made is not a Region state: it is a toast or the field's error, said at once.
- **Loading** is `aria-busy`: its still blocks show after 300ms, and its words after a second, in room kept for them, said once then ("Loading members").
- The sentence and next step are `body-compact` in `ink-muted`; the actions follow them, wrapping under them when there is no room.
- **No fill or inset of its own:** it stands in its region's surface and inset (a Panel's, a List's), so nothing is padded twice. A screen that shows it straight on the Page gives the host a surface and an inset, as the Character Overview does.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Loading | Fill: still blocks after 300ms; Text after a second | "Loading members…" | The region is busy; after a second, "Loading members" (polite) |
| Empty | Text: `ink-muted`, with a next step | "No listings yet. List an item to sell it." | The sentence (a status) |
| No results | Text: `ink-muted`, with "Clear filters" | "No matching players" | The sentence |
| Error | Text and icon: ✕ and the sentence in `danger`, with an action | "Couldn't load the roster." · Try again | The sentence (polite: the region failed to load) |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | `status`; `aria-busy` while loading |
| Keyboard | Only its actions |
| Focus | Its actions' `focus-ring` |
| Announced | A load past a second says what is loading, once |
| Hover and tap | Nothing of its own |
| Target size | Its actions' |
| Text scaling | The sentence wraps; the actions wrap under it |
| Colour | Error is also the ✕ and its words |
| Motion | None; the words wait a second, they do not move |

## Related components

- Skeleton — the still blocks
- Notice — a lasting message at the head of a region, not in place of its content
- Toast — the outcome of something the player did
