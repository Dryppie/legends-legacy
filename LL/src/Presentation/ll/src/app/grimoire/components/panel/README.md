# Panel

The content box.

**Status:** Draft

A plain box with a small-caps header, for secondary content that sits on the stage or in a column: lore, pending loot, a biography, a list.

**Use:**

```html
<lg-panel>
  <lg-panel-header>
    <lg-panel-title>Combat Style</lg-panel-title>
    <a lgButton="link" routerLink="/game/character/combat-styles">Manage</a>
  </lg-panel-header>
  …
</lg-panel>
```

**Provide:** the content, and a header: `<lg-panel-header>` holding `<lg-panel-title>` and any extras after it (a Tag, a count, a link Button), which sit at the end. Optional `align="end"` on the header sets the title at the end; `flush` and `density` on the Panel. Import `LG_PANEL` for all three.

- **The Panel is its own box** (D-136): `lg-panel` is the element that fills and pads, so a layout places and sizes it directly. With a title it is a region named by it (`role="region"`, `aria-labelledby` the title); without a header it is the body alone and no landmark.
- **Paragraphs:** the Panel doesn't style what you put in it. For several paragraphs, wrap them in `.lg-prose`, which spaces them `stack-md` apart.

- **Level 1** (Foundations · Surfaces & Layering): `surface` with 2px corners, and no shadow or texture. `surface` is the game's panel material, a cool near-black at 72% that lets the frame's backdrop through, never blurred (D-102). It sits on Level 0 — the Page, or content on the stage — one level up. Rows inside it wash to Level 2 (`surface-raised`) on hover and selection; tiles, slots and inputs inside it are its one enclosed level, and nothing goes deeper.
- **No border** (Foundations · Lines). A Panel is a region, not a bounded object, so its fill and the space around it set it apart. Its one line is the head's separation hairline, which divides the title from the body. Don't add a border, a gilt frame or an inset line.
- **Padding follows the density:** 24px Comfortable, 16px Standard, 12px Compact, on the body and the head alike. Inside another surface — a Banner or a JourneyCard — it steps down one level (16, 12 or 8px), so two containers never both pad fully (Foundations · Space & Density).
- **`flush`** drops the body padding for a List or a table: its rows run edge to edge and pad to the Panel's inset, so their names line up with the title and the hover wash reaches the edge. A list inside a Panel never adds padding of its own. Because the Panel has no border, the List's separators never meet a container edge; give the List one rhythm — separators, zebra or spacing — never two.
- **Group inside it with space and a heading first,** then a `band` or `hairline` SectionRule. Never the ornament: that is for lore and effects in a Folio.
- Use a Panel for content, a Folio for the selection. Don't nest Panels, and don't put one in the Folio or a dialog: those group with headings and SectionRules.
- It floats nowhere and has no layer of its own: it paints with the content (`z-content`).
