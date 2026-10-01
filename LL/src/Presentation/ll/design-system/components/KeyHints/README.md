# KeyHints

The keyboard shortcut hints.

**Status:** Draft

The row of keyboard shortcuts at the stage's foot: "Back (Esc) · Select (↵)".

**Provide:** `hints` (`[{ label, key }]`). The GameShell pins it bottom-right via its `hints` slot and hides it on touch-sized screens.

- Labels are `ink-muted` 12px; keys are `Key` caps (`line-strong` edge, `surface` fill, `radius-control`: a key cap is a small rectangle, like a key).
- Three or four hints; only keys that actually work on this screen.
