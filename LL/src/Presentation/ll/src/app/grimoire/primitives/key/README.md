# Key

The key cap.

**Status:** Draft

A keyboard key drawn as a small cap — Esc, E, ↵ — after the words for what it does: "Back (Esc)". KeyHints, a Button's `hotkey` and the Chronicle's send key draw their own; `kbd[lgKey]` sets one anywhere else a key is named, such as running text.

**Provide:** the key's name as content of a `kbd`: `<kbd lgKey>Esc</kbd>`. The host is the key (D-143).

- **A small rectangle, like a key** (Foundations · Shape): a `border-hairline` `line-strong` edge on a `surface` fill at `radius-control` (4px), the corner of Buttons, inputs, tabs and Tags. It was a pill (D-064).
- **Set in `code`** (Foundations · Typography): Barlow 700 capitals in `ink`, 11px with `tracking-code`. Key caps and rarity codes are the only text under 12px. Its line height is `leading-mark`, so the cap adds no height to its line.
- **Sized by its text:** at least 1.375rem square (22px at the default text size), it widens for a word or a symbol (Esc, Shift, ↵) and never gets narrower than square.
- **A mark, not a line** (Foundations · Lines): its edge sits outside the decision ladder.
- **In a Button** the cap follows the label: on `solid` it takes the label's colour with no fill, and on a blocked Button its edge drops to `line`.
- **Only keys that work.** Show a key only where it acts on this screen. The cap only names it; you wire the key. Single-key hotkeys never fire while a text field has focus, and players can remap or turn them off (Foundations · Accessibility).

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `kbd` element with no role of its own, read as its text |
| Keyboard | Not focusable. The screen wires the key it names; a Button's `hotkey` also sets `aria-keyshortcuts` |
| Focus | Nothing |
| Announced | Its text, with the words around it |
| Hover and tap | Nothing |
| Target size | Not a target |
| Text scaling | Sized in rem, so it grows with the reading-size setting |
| Colour | `ink` on `surface`; the key's name carries it |
| Motion | Nothing |

## Related components

- KeyHints — the keyboard shortcut hints
- Button — the command button
