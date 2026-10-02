# Heading

The titles.

**Status:** Draft

Titles in engraved Marcellus, one per level of the ramp.

**Provide:** the title as content, the level as the value of `lgHeading` (`folio` — `title-xl`, 56px; `screen` — `title-lg`, 36px; `section` — `title-md`, 24px; `subsection` — `title-sm`, 20px), as in `<h1 lgHeading="screen">`, and optional `sub` — lighter leading words ("Ember" Wolf), set in `ink-muted`.

- Headings are `ink`, and eyebrows `ink-muted` (D-015). Gilt is kept for its four jobs (Foundations · Colour · Allocation); the TopBar's and PageHeader's gilt eyebrows are known exceptions.
- One `folio` heading per screen, and never inside a list.
- Levels map to elements: `folio` is h2, `screen` h1, `section` h3 and `subsection` h4; to override, put `lgHeading` on another element (h1–h6).
