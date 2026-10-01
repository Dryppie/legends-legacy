# Panel

The content box.

**Status:** Draft

A plain box with a small-caps header, for secondary content that sits on the stage or in a column: lore, pending loot, a biography, a list.

**Provide:** `title`, `children`, optionally `titleAlign="end"`, an `aside` (a Tag or count) in the header, `flush` and `density`.

- **Level 1** (Foundations · Surfaces & Layering): `surface` with 2px corners, and no shadow or texture. It sits on Level 0 — the Page, or content on the stage — one level up. Rows inside it wash to Level 2 (`surface-raised`) on hover and selection; tiles, slots and inputs inside it are its one enclosed level, and nothing goes deeper.
- **No border** (Foundations · Lines). A Panel is a region, not a bounded object, so its fill and the space around it set it apart. Its one line is the head's separation hairline, which divides the title from the body. Don't add a border, a gilt frame or an inset line.
- **Padding follows the density:** 24px Comfortable, 16px Standard, 12px Compact, on the body and the head alike. Inside another surface — a Banner or a JourneyCard — it steps down one level (16, 12 or 8px), so two containers never both pad fully (Foundations · Space & Density).
- **`flush`** drops the body padding for a List or a table: its rows run edge to edge and pad to the Panel's inset, so their names line up with the title and the hover wash reaches the edge. A list inside a Panel never adds padding of its own. Because the Panel has no border, the List's separators never meet a container edge; give the List one rhythm — separators, zebra or spacing — never two.
- **Group inside it with space and a heading first,** then a `band` or `hairline` SectionRule. Never the ornament: that is for lore and effects in a Folio.
- Use a Panel for content, a Folio for the selection. Don't nest Panels, and don't put one in the Folio or a dialog: those group with headings and SectionRules.
- It floats nowhere and has no layer of its own: it paints with the content (`z-content`).
