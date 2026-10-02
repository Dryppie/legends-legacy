# Folio

The detail panel.

**Status:** Draft

The right-hand detail panel: an emblem, a title, a paragraph of lore, effect lines and one action — the answer to "what is this thing I selected?".

**Provide:** `title` and any of an `emblem` slot (an Emblem, or an ItemSlot for an item), `eyebrow`, `rarity` (when it explains an item), `lore` (wrap key nouns in `<b>`, in a `lgSlot="lore"` child; the `lore` input is plain text), `effects` (`[{ value, text }]`), an `actions` slot (one Button, two at most), a `footer` slot (a Track), content for stat stacks, and `cornerSrc` (the CornerOrnament asset URL) for the engraved corners.

- **Level 3** (Foundations · Surfaces & Layering): `folio` with `shadow-panel` and a double inset frame in `gilt`. It has no film grain: it bears no art, and its lore and effects would sit straight on the texture (D-072). The frame and corners are brand ornament — gilt's first job — and carry no meaning. The double gilt frame is its one edge, and belongs to the Folio and the Banner only (Foundations · Lines). Its fill is not lighter than a hover wash: its height reads from the shadow and the frame.
- **Its layer is `z-folio`,** above the stage and the TopBar, so its shadow falls on them. The floating Chronicle (`z-chat-float`) and every popover pass over it.
- The eyebrow is a `label` in `ink-muted` (D-019). Lore is `lore` style in `ink-muted`, bold words in `ink`. Effect magnitudes are `gilt`, the rest `ink`.
- **An item's Folio** (`rarity` set) is an item context, where rarity owns hue (Foundations · Colour · Context ownership): the title takes the rarity colour, a rarity Tag names the rarity under it, and effect magnitudes turn `ink`. The one committing action keeps its `solid` gilt button — a control shape, not a hue label.
- `align="start"` left-aligns the content for stat-heavy details (the creature archive).
- **The Folio is Comfortable** (Foundations · Space & Density): Ledgers inside it get 48px rows, Buttons 44px. Mark a region inside it `data-density` only for a long list, which is Standard.
- **One enclosed level inside it:** tiles, slots, inputs and washes, never a Panel (Principles · Anti-generic guardrails).
- **Its ornament budget** (Foundations · Ornament): the Folio is the screen's one ornamented framed surface — its frame, its four CornerOrnaments (`cornerSrc`) and one Emblem — so a screen with a Folio has no Banner. It draws the SectionRule ornament between lore and effects itself and holds no other: that is one of the screen's two ornament rules. Group its stats with space, a `band` or a `hairline`. Its Ledgers keep their dotted leaders, with no box around them, and the footer's hairline sets off the Track (Foundations · Lines).
- **Nothing in it is lit** but the selection and focus, drawn flat: no glowing title, numeral or item, and no halo for rarity. A Folio beside a Banner inside one GameShell logs a console warning.
- **The Folio is a region** (`lg-region`) of 18.5rem inside, always the Stacked tier: grids in it hold one Ledger and two StatTiles a row (Foundations · Layout).
- One Folio per screen, and it is the screen's only inspector: stage screens use it, Page screens put their inspector in the content (D-052). Don't put navigation in it.
