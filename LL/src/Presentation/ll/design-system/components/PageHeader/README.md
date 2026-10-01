# PageHeader

The information screen heading.

**Status:** Draft

The heading row of an information screen: the section's icon in a gilt diamond, eyebrow, title, one-line summary and the screen's actions.

**Provide:** `title`, `eyebrow` (the sidebar group: Character, World, City), `summary`, `icon` (an `Icon` name) and actions as children (SearchField, Buttons).

- The section's icon is 24px (`icon-lg`), the drawing's own size.
- **The section mark is a diamond:** a `border-hairline` `gilt` outline on `surface`, with the section's icon in `gilt`. The current section is the current location, the diamond's meaning and gilt's. It was a verdigris hexagon drawn with the Sigil's tokens, but a hexagon holds a value (D-065, Foundations · Shape).
- Title is `title-lg`; eyebrow is `label` in `gilt`; summary is `body` in `ink-muted`; a `line` rule closes it.
- **In the game's frame it is one row** (D-120): inside a GameShell the eyebrow and summary are not shown — the rail's current item already names the section and says what the screen is for — the mark is 2.25rem with an 18px icon, the title is `title-md`, and the rule sits `space-3` below. Keep passing `eyebrow` and `summary`: outside the frame they still show.
- Actions stay on one line and wrap under the title when the region is below Medium (44rem, Foundations · Layout). The summary keeps to the 68ch reading width.
