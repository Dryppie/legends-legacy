# Presence

The online status.

**Status:** Draft

Online status for another player: a filled dot and "Online", or a hollow dot and when they were last seen.

**Provide:** `online`, `lastSeen` (already formatted: "3 h ago") and `compact` to drop the "Last seen" prefix in tables.

- Online is `arcana` with a filled `arcana-glow` dot; offline is `ink-muted`. The words carry the meaning, not the dot.
- **The dot is the system's one circle** (`radius-circle`): a circle means presence, and nothing else is drawn round — not a count badge, an icon button, a bullet or a portrait (Foundations · Shape).
