# ProfileIdentity

Who a player is.

**Status:** Draft

## When to use

- At the head of a player's profile, in a Banner's body: the Character Overview's Combat Profile, and later a guild member's or a ranked player's profile. It shows the name with the Nobility crown, Presence when it is someone else, and a short list of facts — Guild, Essences attuned, Achievement Points, Nobility.

## When not to use

- A player named in a list or a line of text: a ListRow, or the name in the Chronicle. The headline numbers beside the identity (level, Combat Rating): a LevelPlate and a StatFigure in the Banner's aside.

## Anatomy

1. **Eyebrow** — "Combat Profile" on your own, "Viewing player" on someone else's; `label` style in `ink-muted`.
2. **Name** — a `screen` Heading (`h2` by default). Before it, the Nobility crown (`nobility` icon, 16px, `gilt`) for an active Noble (D-066); after it, Presence for another player.
3. **Facts** — a description list: labels in `ink-muted`, values in `ink` with tabular figures, in `body-compact`. A value may hold a text link (the Guild: `ink`, underlined), a Tag (the guild tag) or a link Button (Show perks). The facts wrap as the region narrows.

The eyebrow and the values follow the colour allocation (Foundations · Colour, D-015): on a profile the one `gilt` figure is the headline number beside the identity, so Achievement Points are `ink`.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | The name as a heading; the facts as a list | The name and each fact | The heading, then each label and value |
| Current (Noble) | Icon: the crown before the name, `gilt` | — | "Noble" |
| Another player | Presence after the name | "Online", or "Last seen 3 h ago" | The same |

The links and Buttons inside the facts keep their own states.

## Density variants

| Variant | Prop | Use in |
| --- | --- | --- |
| Default | | A Banner's body, at any density |

## Content rules

- The name is the player's name, or their equipped title's display name.
- Labels are nouns in sentence case: Guild, Essences, Achievement Points, Nobility. Values are formatted by Foundations · Numerals: "1,240", "2 / 3 attuned", "Expires 12 Oct 2026".
- Show a fact only when the game knows it; a player without a guild shows "None", not an empty value.
- Four or five facts at most. More detail belongs in Panels below the Banner.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | The name is a heading (`h2`, or `as`); the facts are a `dl`. The crown is an image named "Noble" |
| Keyboard | Only what the facts hold: a link or Button is a tab stop |
| Focus | `focus-ring` on the links and Buttons inside |
| Announced | The heading, then each fact as label and value. Presence is read as text |
| Hover and tap | The crown's title, "Active Nobility", repeats its name; nothing else |
| Target size | The links and Buttons inside: at least 24px tall |
| Text scaling | The name and the facts wrap; nothing truncates |
| Colour | Labels and values differ by position as well as tone; the crown is a shape |
| Motion | Nothing |

## Related components

- Banner — the headline identity block it sits in
- LevelPlate — the level display beside it
- StatFigure — the headline number beside it
- Presence — the online status
