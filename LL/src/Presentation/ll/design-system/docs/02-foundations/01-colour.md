# Foundations · Colour

Colour comes in three tiers — palette primitives hold the raw values, semantic roles say what a colour is for, domain roles carry the game's own lists — and each hue family has one job per context. Components read roles and their own component tokens, never a primitive or a hex. There is one theme, Grimoire (Decision D-001). Registries lists the game's entries; Foundations · Lines covers the two line colours.

## Rules

**Must**
- Read a Tier 2 role, a Tier 3 domain role or the component's own component token — never a Tier 1 primitive, never a raw value.
- Alias every Tier 2 and Tier 3 token to one Tier 1 primitive. Only Tier 1 holds raw values.
- Give each hue family one job per context, as the allocation table sets out. A new use takes a job the table already lists, or the table changes with a Decision Log entry (D-015).
- Keep `gilt` to its four jobs: brand and current location, the one committing action, the screen's one headline figure, and effect magnitudes inside descriptions.
- Use `arcana` only for ready, new, actionable or selected.
- Set ordinary data values — Ledger values, table cells — in `ink`.
- Let each context's owner hold the hue: rarity in item contexts, the damage type in combat, the channel in chat — and in chat only on tags and speaker names. Everything else there is a word, a glyph or a shape (D-017).
- Keep the rarity and damage hues as they are: players have learned them. Resolve a collision by moving the other token or by requiring a word, glyph or form (D-016).
- Keep each status to its meaning: `danger` for loss, destruction and failure; `warning` for attention — a reversible risk, a shortfall, something expiring soon; `success` for a confirmed outcome only; `info` for neutral notices. "Ready" and "claimable" are `arcana` (D-025).
- Pair every status colour with a word or an icon. `success` has no hue: it is `ink` with ✓ and its word.
- Judge every change by the player's benefit, never by the sign of the number: a cooldown from 8s to 6.8s is `delta-better` (D-023).
- Give every delta its glyph and its sign — ▲ +12%, ▼ −1.2s, ±0 — so its meaning never depends on colour, and name every effect with its own word.
- Put soft status backgrounds (`danger-soft`, and any added later) only behind inline alerts (D-025).
- Show the rarity code with every rarity colour (D-006), and name the damage type with every damage number.
- Print the numbers next to every meter bar.
- Use `ink-disabled` only for plain disabled controls and the names of locked things, a locked name always with a lock or the word "Locked" (Standards · States).
- Measure every new text or edge token on the four grounds and against its cluster, and add it to the contrast and collision tables below, failures included.

**Should**
- Use `surface-raised` as the hover wash; `gilt-soft` takes no new uses.
- Recolour a role by pointing it at another primitive, not by editing the primitive: a primitive moves every token that aliases it.
- Alias a component token to a role when it means the same thing (`on-sigil` is `ink`), and to a primitive only when no role fits.

**Never**
- Fill with `gilt`, except the one `solid` button per screen, with `on-gilt` text.
- Set text in `arcana-glow` — it is for flat fills only.
- Colour Chronicle message text by channel — it stays `ink`.
- Put two colours closer than ΔE00 10 side by side in the same form (Collisions).
- Alias a role to another role; alias the primitive. (The one exception is a deprecated token, below.)
- Put a status wash on a row, list item, card or Tag.
- Colour a worse delta or a harmful effect as `danger`: a lower stat is not a loss.
- Read a deprecated token (`condition-beneficial`, `condition-harmful`).
- Read another component's component token.

## The three tiers

| Tier | Holds | Named for | Example | Read by |
| --- | --- | --- | --- | --- |
| 1 · Palette primitives | The raw values: nine ramps and the fourteen game hues | The colour | `umber-900`, `hue-rarity-epic` | Tier 2, Tier 3 and component tokens only |
| 2 · Semantic roles | What a colour does anywhere in the interface | The role | `ground`, `ink-muted`, `gilt` | Every component |
| 3 · Domain roles | The game's own lists, and polarity | The registry and its entry | `rarity-epic`, `channel-loot`, `meter-hp` | Every component, through Registries |
| Component tokens | One component's own colours | The component or its part | `sigil-fill`, `on-tile` | That component only |

A value travels one way: `rarity-epic` → `hue-rarity-epic` → `#e879f9`; `gilt` → `brass-300` → `#dcb872`; `on-sigil` → `ink` → `bone-100` → `#f0e6d2`.

The token viewer groups colours by name stem, so its first group, which it labels "Palette", holds the bare role names — `ground`, `ink`, `gilt`. That group is Tier 2, not Tier 1. Every token's usage line begins with its tier.

## Allocation

An earlier audit of the game found one accent colour carrying too many meanings, and Grimoire had repeated it: `gilt` marked group labels, eyebrows, effect values, Ledger values, the level numeral, the active nav marker, selected rules and the solid button. A colour that means everything marks nothing. So each hue family — a ramp and the tokens that alias it — does one job per context (D-015).

**The contexts.** **Shell** — the frame: TopBar, NavRail, Stage, the brass furniture of Folio and Banner. **Controls** — buttons, tabs, inputs. **Data** — Ledgers, figures, tables and notices on information screens. **Descriptions** — effect lines, perks, abilities, lore. **Meters** — every Meter and Track fill. **Items** — ItemSlot, ItemLink, LoadoutSlot, loot and market rows, an item's Folio. **Combat** — damage numbers and combat results. **Chat** — the Chronicle.

| Hue family | Shell | Controls | Data | Descriptions | Meters | Items | Combat | Chat |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| **Bone** — `ink`, `ink-muted`, `ink-disabled`, `success` | Text and labels | Labels | Every value and label; success as ✓ and its word; unchanged deltas (±0) | Text | The printed numbers | Everything but the item's rarity and a comparison's deltas | Everything but damage types and conditions | Message text; General, Help, System and Loot |
| **Brass** — `gilt`, `meter-xp` | Brand and current location | The one committing action | The one headline figure | Effect magnitudes | EXP and level progress | The brand frame only | — | — |
| **Verdigris** — `arcana`, `arcana-soft`, `arcana-glow`, `channel-guild` | Ready and new (rail badges) | Selected (tab bar, ring, edge) | Ready, claimable, new, actionable, selected | — | — | — (selection is a shape) | — | Guild |
| **Ember** — `danger`, `danger-soft`, `meter-hp`, `channel-raid` | Loss and failure | The destructive action | Loss, destruction, failure | — | HP | — (a word) | — (a word) | Raid |
| **Amber** — `warning`, `channel-trade` | Attention | — | Reversible risk, shortfalls, expiring soon | — | — | — (a word) | — (a word) | Trade |
| **Azure** — `info`, `meter-sp`, `channel-invites` | Notices | — | Notices | — | SP | — (a word) | — (a word) | Invites |
| **Orchid** — `channel-whisper` | — | — | — | — | — | — | — | Whispers |
| **Lichen** — `delta-better`, `effect-beneficial` | — | — | Better deltas; beneficial effects | Better, in upgrade previews | — | A comparison's better deltas | Beneficial conditions | — |
| **Madder** — `delta-worse`, `effect-harmful` | — | — | Worse deltas; harmful effects | Worse, in upgrade previews | — | A comparison's worse deltas | Harmful conditions | — |
| **Rarity hues** — `rarity-*` | — | — | — | — | — | The item's rarity: name, edge, tag and code | — | Item links, in [brackets] |
| **Damage hues** — `damage-*` | — | — | — | — | — | — | The damage type, always named | — |

Slate is structure — grounds, surfaces and lines — in every context: the game's own cool near-black, which Martin chose over a warm brown (D-102). Umber keeps the marks that stay warm: `line-strong`, `on-gilt` and `changed`. Three marks sit outside the table because they may appear anywhere: `focus` (`brass-200`), the keyboard focus ring, always a ring outside a `ground` gap and never a fill; `changed` (`umber-650`), the live-update mark, a flat wash behind a value that changed by itself, in any context — it says that the value changed, never whether that helps, so it takes no hue (Foundations · Motion); and the component tokens of Sigil and StatTile, which belong to their component.

### Gilt's four jobs

1. **Brand and current location** — the logo; the grimoire's brass furniture within the ornament budget (the Folio's and Banner's frames, corner ornaments, the ornament rule's lattice, Emblems); the game's one heraldic mark, the Nobility crown (D-007, D-066); and where you are: the active NavRail item's bar and wash (D-118), the current screen's icon in the PageHeader. The NavRail's icons are gilt at rest too (D-105): brand furniture of the frame, not a location mark; the bar, the wash and the weight say where you are.
2. **The one committing action** — the `solid` button's fill, with `on-gilt` text, once per screen. It keeps its fill in every context: a filled button is a shape, not a hue label.
3. **The screen's one headline figure** — the StatFigure, or the level numeral when the level is what the screen is about. One per screen, like every display-size element (Principles · Anti-generic guardrails).
4. **Effect magnitudes inside descriptions** — the "+12%" in "+12% damage from equipment". Inside an item context magnitudes are `ink`, because rarity owns the hue there.

Gilt no longer marks group labels and eyebrows (`ink-muted`), Ledger and other data values (`ink`), selection (`arcana-glow`), hover (`surface-raised`), links (`ink`, underlined), status labels (the `neutral` Tag) or mentions (a neutral wash and an `ink` edge). `meter-xp` shares gilt's brass as the Meters job, and the Track's progress fill should read it too.

### Arcana's one meaning

`arcana`, `arcana-soft` and `arcana-glow` mean **ready, new, actionable or selected**, and nothing else: the "+ New" Tag, "Ready", "Claimable", "Essence ready", rail badges, the ready diamond; the active-tab bar and the selected ring, edge or bar. Being online, a positive delta, a plain count, a link and "attuned" are not arcana. In item, combat and chat contexts arcana takes no hue, because another family owns it there. A reward ready to claim is arcana; once the claim is confirmed it is success.

### Not yet on the allocation

These components still break the allocation. They are listed rather than hidden and are queued in Governance · Audit & consolidation map (revision item 9). Ledger, Folio, Tag, NavRail and Chronicle follow it (D-018 to D-022), StatTile did from D-026 until it merged into Ledger (D-137), and EntryList and LoadoutSlot since D-087.

| Component | Off the allocation | Moves to |
| --- | --- | --- |
| Button | Hover turns the edge `gilt`; the `link` variant is `gilt` text | Hover: a `line-strong` edge or the `surface-raised` wash; links: `ink`, underlined |
| TopBar | The eyebrow and the menu icon are `gilt` | `ink-muted` |
| PageHeader | The eyebrow is `gilt` | `ink-muted` (its icon stays gilt: current location) |
| JourneyCard | The phase eyebrow, the key glyph and the "Recommended now" `gilt-soft` wash | `ink-muted` eyebrow; the key as brand ornament; a neutral wash |
| SectionRule | The ornament rule's label is `gilt` | `ink-muted` (the lattice stays: brand) |
| Constellation | The strong ring and the node ticks are `gilt` | To decide: brand ornament, or `ink` |
| LevelPlate | The numeral is always `gilt` | Gilt only when the level is the screen's headline figure; otherwise `ink` |
| Track | The `gilt` tone reads `gilt`; the `arcana` tone colours progress | `meter-xp` for progress; `arcana` only for a ready step |
| SearchField | The highlighted suggestion is marked by the `gilt-soft` wash alone | `surface-raised` and a bar (also a contrast breach, below) |
| ItemSlot | Selection is an `arcana-glow` ring — the same form as an Uncommon edge, ΔE00 13.4 away | A shape: an offset ring or a corner mark |
| Presence | "Online" is `arcana` with an `arcana-glow` dot | A filled `ink` dot and the word |
| TabStrip | Tab counts are `arcana` | `ink-muted`, unless the count means new |

## Context ownership

In three contexts one set of hues belongs to the game's own list, and players read it at a glance. There that list owns hue, and everything else speaks in words, glyphs and shapes (D-017).

| Context | Owns hue | Where | Everything else |
| --- | --- | --- | --- |
| **Items** | Rarity: `rarity-*`, always with its code | ItemSlot, ItemLink, LoadoutSlot, loot and market rows, inventory, an item's Folio (`rarity` set) | A comparison of the player's stats inside an item view is a data block, so its deltas keep their polarity colours (D-024). Everything else is words and glyphs in `ink`: "Equipped", "Cannot equip", "Sold out" as `neutral` Tags or text; effect magnitudes in `ink`; selection as a shape |
| **Combat** | Damage numbers: the damage type (`damage-*`), always named. Conditions: polarity (`effect-beneficial`, `effect-harmful`), always named (D-024) | Damage numbers, conditions on a combatant, combat logs, combat results | "Victory" and "Defeated" as words in `ink`; HP and SP bars keep their fills, because a labelled bar is a different form from a number |
| **Chat** | The channel: `channel-*`, on tags and speaker names only | The Chronicle | Message text in `ink`; the active tab as an `ink` bar; unread counts `ink` on `surface-raised`; a mention as a neutral wash with an `ink` edge and bold `@you`; item links keep their rarity inside [brackets] |

Controls keep their colours in every context — the `solid` button's fill, the destructive button, the focus ring — because they are shapes, not hue labels. The one exception is selection in an item grid, which shares its form with a rarity edge and so must become a shape of its own.

## Feedback and polarity

Feedback says how something turned out; polarity says whether a change or an effect works for the player or against them. Legend's Legacy shows both constantly — equipment comparison, Soulstone upgrades, Essence Ascension previews, Empower against Weaken, Haste against Slow (D-023, D-025).

### What each status means

| Role | Means | In Legend's Legacy | Not this |
| --- | --- | --- | --- |
| `danger` | Loss, destruction, failure: it has happened, or it cannot be undone | Pending Loot lost; "Abandon run" (your Pending Loot is lost); a failed save; "Defeated"; destroying an item | A shortfall or a risk that can still be avoided — `warning` |
| `warning` | Attention: a reversible risk, insufficient resources, something expiring soon | "12 Soulstones short"; "Your Pending Loot is at risk — retreat to secure it"; "Nobility expires in 2 days" | Something already lost — `danger` |
| `success` | A confirmed outcome, and only that | "Upgrade succeeded", "Saved", "Floor cleared", "Victory" — `ink` with ✓ | "Ready", "Claimable", "Available" — `arcana` |
| `info` | A neutral notice | "The World Tower resets in 2 hours" | Anything the player must act on |
| `arcana` | Ready, claimable, new, actionable or selected | "Claimable", "Essence ready", "+ New quest" | A confirmed outcome — `success` |

The test between the two: if it can still be avoided or undone, it is a warning; once it has happened, or cannot be undone, it is danger. A destructive command is danger before it happens, because pressing it is the loss — "Abandon run" is a `danger` Button, and "Your Pending Loot is at risk" beside it is a warning.

### Polarity follows the player

`delta-better` and `delta-worse` judge a change by whether it helps the player, never by the sign of the number. A cooldown that goes from 8s to 6.8s falls, and it is better. The game supplies the judgement for each stat; components never guess it from the sign, and a change the game does not judge stays `delta-neutral`.

| Stat | Better when it | Example | Shown as |
| --- | --- | --- | --- |
| Power, Armor, Crit Chance, Attack Speed, Ability Haste | Rises | Power 142 → 154 | ▲ +12, better |
| Cooldowns and cast times | Falls | Cinder Bite 8s → 6.8s | ▼ −1.2s, better |
| Costs — SP, Cinders, Soulstones | Falls | SP Cost 14 → 12 | ▼ −2, better |
| Damage Taken | Falls | 14% → 12% | ▼ −2%, better |
| Threat | Depends on the role | 3.4 → 3.9 | ▲ +0.5, neutral unless the game knows the role |

Effects follow the same rule. `effect-beneficial` marks Empower and Haste, `effect-harmful` marks Weaken and Slow — by what they do to the player, whatever their numbers. `delta-better` and `effect-beneficial` share lichen, and `delta-worse` and `effect-harmful` share madder, because each pair means the same thing: good, or bad, for the player.

### Every delta carries a glyph and a sign

| Change | Shown as | Colour |
| --- | --- | --- |
| Up, and better | ▲ +12% | `delta-better` |
| Down, and better | ▼ −1.2s | `delta-better` |
| Up, and worse | ▲ +2% | `delta-worse` |
| Down, and worse | ▼ −0.5% | `delta-worse` |
| Unchanged | ±0 | `delta-neutral` |
| Up or down, not judged | ▲ +0.5 | `delta-neutral` |

- The glyph and the sign say which way the number moved, the colour says whether that helps, and screen readers hear both in words: "−1.2s, better". The meaning never depends on colour.
- The signs are true signs: + and − (U+2212), never a hyphen.
- Unchanged is ±0, with no glyph: a hollow diamond would read as a milestone still to come (D-067; Foundations · Shape).
- Delta draws this for every part — a Ledger row's change, comparison rows in a Ledger. Registries · Glyphs lists the glyphs; PatternFeedback shows them in use.

### Soft backgrounds

- Soft status backgrounds — `danger-soft`, and any warning, success or info wash added later — go behind **inline alerts** only: a one- or two-line message inside a Panel or the Folio, next to what it is about. No component draws an inline alert yet; it is queued with the other workbench parts (Governance · Audit & consolidation map, revision item 12).
- Never on rows, list items, cards or Tags. Status Tags are outlined (D-027).
- `arcana-soft` is not a status background. It stays on the `new` Tag and the rail's badges — small marks — and never covers a row, list item or card either.

## Collisions

**How it is measured.** Perceptual distance is CIEDE2000 (ΔE00) between the token values in sRGB; about 1 is the smallest difference most people can see, side by side. ΔL is the gap in OKLab lightness, ×100. Small text is read mostly through lightness, so two colours with the same lightness and chroma look alike at 11–14px even when ΔE00 says they differ.

**Thresholds and verdicts.** Below 10, two colours read as one. From 10 to 20 they are close: fine in large fields, unreliable in small text. From 20 they are distinct. Each pair gets one verdict for whether it may appear side by side:

- **Yes** — distinct enough (ΔE00 20 or more).
- **Words** — only when a word, glyph or code also tells them apart, as the rules already require.
- **Forms** — only in different forms: a bar beside a number, a fill beside text, a display-size figure beside a Tag.
- **Never** — they never share a context; context ownership keeps them apart.

**What was chosen.** The fourteen game hues fill almost every hue a dark ground can carry: searching the whole gamut for a new `success` found nothing at 20 or more from its neighbours except greyed mauves, and the same for `warning`. So Grimoire moves as few tokens as it can and settles the rest by ownership, words and forms (D-016):

- **`success` gives up its hue.** It was near-identical to `arcana`: ΔE00 12.9, but the same lightness (ΔL 0.3) and chroma, so at 12px the two read as one colour, and under tritanopia they are 2.0 apart. `success` is now `ink` with ✓ and its word — calm, like every finished thing — and `arcana` alone means ready, new, actionable or selected.
- **`channel-whisper` moves to pale orchid** (`orchid-100`, `#f6cbf1`). The old `#e39be2` was ΔE00 9.4 from `rarity-epic`, and whispers and Epic item links meet in the same chat lines. It is now 20.7 away and 14.3 lighter.
- **Chat keeps one hue per family.** `channel-loot` was `gilt`'s brass and ΔE00 6.5 from `channel-trade`; it is now neutral, because the item links in a loot line carry the colour. `channel-help` and `channel-invites` were both azure; Help is now neutral like General, and Invites takes `azure-200`, 22.4 from `channel-guild` instead of 12.9.
- **`info` stays.** Its near neighbours, `rarity-rare` (6.8) and `damage-shadow` (8.2), live in contexts where notices are words.
- **Words and glyphs do the rest.** The rarity code with every rarity colour, the damage type named with every damage number, a word or icon with every status colour, brackets around every item link, a tag word on every line in the All feed, and printed numbers with every meter.
- `orchid-300` and `azure-300` are no longer aliased and are removed.
- **Polarity takes two new ramps, lichen and madder** (D-023). The search was for a pair far apart from each other for every kind of colour vision, and from their same-form neighbours — `ink`, `gilt`, `arcana`, `danger`, `warning`, `info`. It found a pale lichen for better (`#b5ea99`) and a madder rose for worse (`#d671a7`), 65.6 apart, and at least 31.0 apart for every dichromacy. Their nearest learned neighbours — Poison and Uncommon for lichen, Legacy and Epic for madder — sit 13.7 to 14.5 away and never share a form: a delta always has a glyph and a sign, and a condition is a named Tag.

### Cool

`arcana` `#6fcab9`, `success` `#f0e6d2` (was `#74c6d6`), `info` `#9ccaf0`, `rarity-uncommon` `#41f1b6`, `rarity-rare` `#7cb7ff`, `damage-shadow` `#69b6dd`, `meter-sp` `#4f9fd6`.

| Pair | ΔE00 | ΔL | Side by side | How they are told apart |
| --- | ---: | ---: | --- | --- |
| `arcana` · `success` | 12.9 → 26.0 | 15.0 | Yes | Moved: `success` has no hue now |
| `arcana` · `info` | 22.4 | 4.2 | Yes | — |
| `arcana` · `rarity-uncommon` | 13.4 | 7.9 | Never | Arcana takes no hue in item contexts; ItemSlot's selection ring is queued to become a shape |
| `arcana` · `rarity-rare` | 28.5 | 1.0 | Yes | — |
| `arcana` · `damage-shadow` | 21.1 | 3.6 | Yes | — |
| `arcana` · `meter-sp` | 26.8 | 10.2 | Yes | — |
| `success` · `info` | 11.8 → 27.5 | 10.8 | Yes | Moved |
| `success` · `rarity-uncommon` | 25.6 → 27.1 | 7.1 | Yes | — |
| `success` · `rarity-rare` | 17.1 → 33.7 | 16.0 | Yes | Moved |
| `success` · `damage-shadow` | 9.0 → 31.4 | 18.6 | Yes | Moved; they never shared a context anyway |
| `success` · `meter-sp` | 16.2 → 37.4 | 25.2 | Yes | — |
| `info` · `rarity-uncommon` | 33.5 | 3.7 | Yes | — |
| `info` · `rarity-rare` | **6.8** | 5.2 | Never | Notices are words in item contexts; in chat, Invites shares info's value and Rare links are bracketed |
| `info` · `damage-shadow` | **8.2** | 7.8 | Never | In combat, notices are words |
| `info` · `meter-sp` | 13.5 | 14.4 | Forms | A notice with its word; SP is a bar labelled SP with its numbers |
| `rarity-uncommon` · `rarity-rare` | 40.4 | 8.9 | Yes | — |
| `rarity-uncommon` · `damage-shadow` | 34.3 | 11.5 | Yes | — |
| `rarity-uncommon` · `meter-sp` | 40.2 | 18.1 | Yes | — |
| `rarity-rare` · `damage-shadow` | **8.7** | 2.6 | Never | Items and combat never share a context; both hues are learned and stay |
| `rarity-rare` · `meter-sp` | **9.4** | 9.3 | Forms | An item name with its code beside a labelled bar |
| `damage-shadow` · `meter-sp` | **7.8** | 6.6 | Forms | In combat: a labelled SP bar beside a number that names its type |

### Warm

`gilt` `#dcb872`, `warning` `#f0b35a`, `rarity-unique` `#facc15`, `rarity-legendary` `#fb923c`, `damage-burn` `#ef8a3c`, `channel-loot` `#bba98c` (was `#dcb872`), `channel-trade` `#f0b35a`.

| Pair | ΔE00 | ΔL | Side by side | How they are told apart |
| --- | ---: | ---: | --- | --- |
| `gilt` · `warning` | **6.5** | 0.7 | Forms | Near-identical. Gilt appears only as ornament, the solid button's fill, the display-size headline figure and magnitudes in descriptions; warning only as a Tag or a line with its word, never in descriptions |
| `gilt` · `rarity-unique` | 12.6 | 6.2 | Forms | In item contexts gilt is only the brand frame and the solid button, never text |
| `gilt` · `rarity-legendary` | 17.2 | 4.1 | Forms | As above |
| `gilt` · `damage-burn` | 18.0 | 7.0 | Never | Gilt has no job in combat |
| `gilt` · `channel-loot` | 0.0 → 11.1 | 5.6 | Never | Moved: Loot is neutral; gilt has no job in chat |
| `gilt` · `channel-trade` | **6.5** | 0.7 | Never | Gilt has no job in chat |
| `warning` · `rarity-unique` | 12.4 | 5.4 | Never | In item contexts warnings are words |
| `warning` · `rarity-legendary` | 12.1 | 4.9 | Never | In item contexts warnings are words |
| `warning` · `damage-burn` | 13.6 | 7.7 | Never | In combat warnings are words |
| `warning` · `channel-loot` | 6.5 → 15.4 | 6.4 | Never | Moved; warning has no job in chat |
| `warning` · `channel-trade` | **0.0** | 0.0 | Never | One primitive, two contexts: warning in data, Trade in chat |
| `rarity-unique` · `rarity-legendary` | 24.0 | 10.3 | Yes | — |
| `rarity-unique` · `damage-burn` | 25.6 | 13.1 | Yes | — |
| `rarity-unique` · `channel-loot` | 12.6 → 22.4 | 11.8 | Yes | Moved: Loot is neutral; its item links carry the hue |
| `rarity-unique` · `channel-trade` | 12.4 | 5.4 | Words | Trade names and tags beside Unique links: the link is bracketed, the tag is a word, the name ends in a colon |
| `rarity-legendary` · `damage-burn` | **2.8** | 2.9 | Never | The same orange. Items and combat never share a context; both hues are learned and stay |
| `rarity-legendary` · `channel-loot` | 17.2 → 20.1 | 1.5 | Yes | Moved |
| `rarity-legendary` · `channel-trade` | 12.1 | 4.9 | Words | As Unique and Trade |
| `damage-burn` · `channel-loot` | 18.0 → 19.7 | 1.4 | Never | Combat and chat never share a line |
| `damage-burn` · `channel-trade` | 13.6 | 7.7 | Never | Combat and chat never share a line |
| `channel-loot` · `channel-trade` | 6.5 → 15.4 | 6.4 | Words | Moved: Loot is neutral. In the All feed every line carries its tag word |

### Red and pink

`danger` `#f2837a`, `rarity-legacy` `#fb7185`, `damage-bleed` `#d94d5c`, `meter-hp` `#d0443f`, `channel-whisper` `#f6cbf1` (was `#e39be2`), `rarity-epic` `#e879f9`.

| Pair | ΔE00 | ΔL | Side by side | How they are told apart |
| --- | ---: | ---: | --- | --- |
| `danger` · `rarity-legacy` | **8.1** | 1.1 | Never | Losses are words in item contexts; in chat, Raid shares danger's value and Legacy links are bracketed |
| `danger` · `damage-bleed` | 13.8 | 11.6 | Never | In combat, outcomes are words |
| `danger` · `meter-hp` | 16.5 | 14.6 | Forms | A danger word beside the labelled HP bar |
| `danger` · `channel-whisper` | 25.7 → 26.1 | 16.0 | Yes | — |
| `danger` · `rarity-epic` | 30.3 | 1.7 | Yes | — |
| `rarity-legacy` · `damage-bleed` | 11.3 | 10.5 | Never | Items and combat never share a context |
| `rarity-legacy` · `meter-hp` | 17.2 | 13.5 | Forms | Item text beside a labelled bar |
| `rarity-legacy` · `channel-whisper` | 21.4 → 24.9 | 17.1 | Yes | Moved |
| `rarity-legacy` · `rarity-epic` | 24.6 | 2.9 | Yes | — |
| `damage-bleed` · `meter-hp` | **7.4** | 3.0 | Forms | In combat: the HP bar with its label and numbers beside bleed numbers that name their type. Bleed is learned; HP's red stays |
| `damage-bleed` · `channel-whisper` | 28.8 → 33.9 | 27.6 | Yes | — |
| `damage-bleed` · `rarity-epic` | 30.2 | 13.3 | Yes | — |
| `meter-hp` · `channel-whisper` | 35.0 → 39.1 | 30.6 | Yes | — |
| `meter-hp` · `rarity-epic` | 36.6 | 16.3 | Yes | — |
| `channel-whisper` · `rarity-epic` | 9.4 → 20.7 | 14.3 | Yes | Moved to pale orchid; Epic links in chat are bracketed as well |

### Polarity

`delta-better` and `effect-beneficial` `#b5ea99`; `delta-worse` and `effect-harmful` `#d671a7`; `delta-neutral` `#bba98c`, the same as `ink-muted`.

| Pair | ΔE00 | ΔL | Side by side | How they are told apart |
| --- | ---: | ---: | --- | --- |
| `delta-better` · `delta-worse` | 65.6 | 20.2 | Yes | Distinct for every kind of colour vision (below) |
| `delta-better` · `delta-neutral` | 24.3 | 13.8 | Yes | — |
| `delta-worse` · `delta-neutral` | 35.0 | 6.3 | Yes | — |
| `delta-better` · `ink` | 20.4 | 4.7 | Yes | Lighter lichen against parchment; the glyph and sign mark the delta anyway |
| `delta-better` · `gilt` | 24.1 | 8.2 | Yes | — |
| `delta-better` · `arcana` | 19.9 | 10.3 | Forms | A delta is a glyph and a signed number; arcana is a Tag, a badge or a bar |
| `delta-better` · `arcana-glow` | 17.7 | 2.1 | Forms | As above |
| `delta-better` · `rarity-uncommon` | 14.3 | 2.4 | Forms | In an item view: a delta in a comparison row beside the item's name or edge |
| `effect-beneficial` · `damage-poison` | 13.7 | 15.7 | Forms | In combat: a named condition Tag beside a damage number that names its type |
| `delta-worse` · `danger` | 19.9 | 5.1 | Forms | Danger is always a word; a delta always a glyph and a sign — and they never mean the same thing |
| `delta-worse` · `warning` | 46.5 | 12.7 | Yes | — |
| `delta-worse` · `rarity-legacy` | 13.7 | 4.0 | Forms | In an item view: a delta beside the item's name or edge |
| `delta-worse` · `rarity-epic` | 14.5 | 6.8 | Forms | As above |
| `effect-harmful` · `damage-bleed` | 17.8 | 6.5 | Forms | In combat: a named condition Tag beside a bleed number |
| `effect-harmful` · `meter-hp` | 24.6 | 9.5 | Yes | — |
| `delta-neutral` · `ink-muted` | **0.0** | 0.0 | Words | The same value by design: an unchanged delta is quiet, and ±0 marks it |

ΔE00 below 10 is in bold. "Moved" marks a pair changed by D-016; the arrow shows the distance before and after.

### Chat channels

In chat the channels own hue, and item links bring the rarity hues into the same lines, so each channel is measured against every rarity hue.

| Channel | Token | Value | Nearest rarity hue | ΔE00 | Told apart by |
| --- | --- | --- | --- | ---: | --- |
| General | `channel-general` | `#f0e6d2` (neutral) | `rarity-common` | 11.7 | The tag word; links are bracketed |
| Trade | `channel-trade` | `#f0b35a` | `rarity-legendary` | 12.1 | The tag word; links are bracketed |
| Help | `channel-help` | `#f0e6d2` (neutral) | `rarity-common` | 11.7 | The tag word; links are bracketed |
| Guild | `channel-guild` | `#6fcab9` | `rarity-uncommon` | 13.4 | The tag word; links are bracketed |
| Whispers | `channel-whisper` | `#f6cbf1` | `rarity-common` | 17.9 | The tag word; links are bracketed |
| Raid | `channel-raid` | `#f2837a` | `rarity-legacy` | 8.1 | The tag word; links are bracketed |
| Invites | `channel-invites` | `#9ccaf0` | `rarity-rare` | 6.8 | The tag word; links are bracketed |
| System | `channel-system` | `#bba98c` (neutral) | `rarity-common` | 18.1 | The tag word; links are bracketed |
| Loot | `channel-loot` | `#bba98c` (neutral) | `rarity-common` | 18.1 | The tag word; links are bracketed |

## Colour-vision checks

Each key pair simulated for the three dichromacies — protanopia and deuteranopia (red–green) and tritanopia (blue–yellow) — with the Machado, Oliveira and Fernandes (2009) model at full severity, in linear sRGB, then measured in ΔE00. Below 10, in bold, the pair is confusable for that player, and something other than hue must tell it apart.

| Pair | Normal | Protanopia | Deuteranopia | Tritanopia | What tells them apart |
| --- | ---: | ---: | ---: | ---: | --- |
| `danger` · `success` | 54.0 → 30.2 | 26.2 → 21.5 | 33.2 → 17.5 | 59.6 → 27.7 | Moved; success also carries ✓ and its word |
| `arcana` · `success` | 12.9 → 26.0 | **14.3 → 9.9** | 10.8 → 17.1 | 2.0 → 35.2 | Moved; under protanopia still close, so success always carries ✓ and its word |
| `arcana` · `danger` | 51.9 | 14.9 | 20.9 | 60.4 | Distinct |
| `danger` · `warning` | 28.0 | 17.4 | 11.1 | 10.8 | Close under deuteranopia and tritanopia: both always carry their word |
| `gilt` · `warning` | **6.5** | **4.1** | **4.3** | **5.2** | The same for everyone: kept to different forms |
| `gilt` · `rarity-unique` | 12.6 | 12.1 | 11.8 | **4.4** | Kept to different forms in item contexts |
| `channel-whisper` · `rarity-epic` | 9.4 → 20.7 | 8.9 → 20.4 | 8.4 → 18.3 | 6.3 → 16.7 | Moved; Epic links are bracketed |
| `channel-invites` · `channel-guild` | 12.9 → 22.4 | 14.3 → 19.8 | 10.8 → 14.3 | **2.0 → 7.1** | Moved; close under tritanopia, so the tag word decides |
| `channel-raid` · `channel-guild` | 51.9 | 14.9 | 20.9 | 60.4 | Close under protanopia: the tag word decides |
| `channel-raid` · `channel-trade` | 28.0 | 17.4 | 11.1 | 10.8 | Close under tritanopia: the tag word decides |
| `rarity-uncommon` · `rarity-legacy` | 74.2 | 25.8 | 11.8 | 72.4 | Green against red: the rarity code decides |
| `rarity-common` · `rarity-uncommon` | 27.5 | 18.2 | 11.6 | 24.9 | The rarity code decides |
| `rarity-rare` · `rarity-epic` | 33.4 | **9.5** | **1.4** | 59.2 | Nearly one colour under deuteranopia: the rarity code decides |
| `rarity-unique` · `rarity-legendary` | 24.0 | 13.5 | **9.2** | 14.7 | Confusable under deuteranopia: the rarity code decides |
| `rarity-legendary` · `rarity-legacy` | 29.0 | 22.9 | 14.7 | **6.1** | Confusable under tritanopia: the rarity code decides |
| `rarity-epic` · `rarity-legacy` | 24.6 | 30.1 | 39.7 | 13.0 | Distinct |
| `damage-bleed` · `damage-poison` | 64.6 | 29.5 | 12.2 | 54.9 | Red against green: the type's name decides |
| `damage-burn` · `damage-poison` | 43.1 | **7.0** | **3.9** | 48.4 | Confusable under protanopia and deuteranopia: the type's name decides |
| `damage-bleed` · `damage-burn` | 27.9 | 24.0 | 15.4 | 12.6 | The type's name decides |
| `damage-shadow` · `damage-magical` | 22.5 | 12.8 | **9.0** | 22.1 | Confusable under deuteranopia: the type's name decides |
| `damage-physical` · `damage-none` | 25.6 | 25.3 | 25.3 | 24.0 | Distinct |
| `meter-hp` · `meter-sp` | 50.1 | 43.5 | 46.7 | 62.1 | Distinct; bars are labelled anyway |
| `meter-hp` · `meter-xp` | 38.6 | 30.7 | 19.7 | 28.0 | Distinct |
| `delta-better` · `delta-worse` | 65.6 | 44.2 | 31.0 | 51.0 | Distinct for everyone — and the glyph and sign carry the change |
| `delta-better` · `arcana` | 19.9 | 18.9 | 23.6 | 13.0 | Close under tritanopia: kept to different forms |
| `delta-better` · `rarity-uncommon` | 14.3 | **6.4** | 10.4 | 13.7 | Confusable under protanopia: a delta has its glyph and sign |
| `delta-worse` · `danger` | 19.9 | 27.6 | 25.2 | **6.4** | Confusable under tritanopia: danger is always a word |
| `delta-worse` · `rarity-legacy` | 13.7 | 18.2 | 21.9 | **5.8** | Confusable under tritanopia: different forms |
| `effect-beneficial` · `damage-poison` | 13.7 | 13.8 | 13.8 | 12.8 | Close for everyone: a named condition beside a named damage type |
| `effect-harmful` · `damage-bleed` | 17.8 | 25.4 | 24.8 | 11.9 | The names decide |

**What follows.** Rarity and damage pairs that fall below 10 cannot be fixed by recolouring, because players have learned the hues; the rarity code (D-006) and the damage type's name carry them. Status colours are never alone. After the move, `success` against `danger` stays above 17 for every player, and `channel-whisper` against `rarity-epic` above 16.

## Tokens used

Every colour token, tier by tier.

### Tier 1 · Palette primitives

Nine ramps, each named for its colour in the grimoire's own terms — slate, umber, bone, brass, verdigris, ember, amber, azure, orchid. Step numbers order a ramp by lightness — the lower the number, the lighter the colour — and are not evenly spaced: the values were inherited, so steps such as 825 and 875 sit where the existing colours fell. `-a<opacity>` marks a translucent version: `slate-950-a80` at 80%, `slate-900-a72` at 72%. `slate-100-a12` and `slate-100-a04` are light veils with no opaque step of their own.

| Primitive | Value | Aliased by |
| --- | --- | --- |
| `slate-950` | `#0b0b0f` | `ground-deep` |
| `slate-950-a80` | `#0b0b0fcc` | `scrim` |
| `slate-900` | `#101014` | `ground` |
| `slate-900-a72` | `#101014b8` | `surface` |
| `slate-875` | `#16161b` | `surface-solid` |
| `slate-850` | `#131318` | `folio` |
| `slate-800` | `#1b1b22` | `tile` |
| `slate-775` | `#22222a` | `surface-raised` |
| `slate-750` | `#2a2a33` | `meter-track` |
| `slate-100-a12` | `#f4f4f81f` | `line` |
| `slate-100-a04` | `#f4f4f80a` | `row-stripe` |
| `umber-875` | `#1a1109` | `on-gilt` |
| `umber-650` | `#46372a` | `changed` |
| `umber-450` | `#8c7358` | `line-strong` |
| `bone-100` | `#f0e6d2` | `ink`, `success`, `channel-general`, `channel-help` |
| `bone-300` | `#bba98c` | `ink-muted`, `channel-system`, `channel-loot`, `delta-neutral` |
| `bone-500` | `#7a6a56` | `ink-disabled` |
| `brass-200` | `#f5d48f` | `focus` |
| `brass-300` | `#dcb872` | `gilt`, `meter-xp` |
| `brass-900` | `#3a2b17` | `gilt-soft` |
| `verdigris-200` | `#86e6d2` | `arcana-glow` |
| `verdigris-250` | `#7fc9b8` | `sigil-edge` |
| `verdigris-300` | `#6fcab9` | `arcana`, `channel-guild` |
| `verdigris-800` | `#1d4b45` | `sigil-fill` |
| `verdigris-900` | `#163833` | `arcana-soft` |
| `ember-300` | `#f2837a` | `danger`, `channel-raid` |
| `ember-500` | `#d0443f` | `meter-hp` |
| `ember-900` | `#3d1715` | `danger-soft` |
| `amber-300` | `#f0b35a` | `warning`, `channel-trade` |
| `azure-200` | `#9ccaf0` | `info`, `channel-invites` |
| `azure-500` | `#4f9fd6` | `meter-sp` |
| `orchid-100` | `#f6cbf1` | `channel-whisper` |
| `lichen-200` | `#b5ea99` | `delta-better`, `effect-beneficial` |
| `madder-400` | `#d671a7` | `delta-worse`, `effect-harmful` |

The fourteen game hues are the game's existing rarity and damage colours. Players have learned them, so they never change (D-016). Each feeds exactly one Tier 3 role of the same name without `hue-`.

| Rarity hue | Value | Damage hue | Value |
| --- | --- | --- | --- |
| `hue-rarity-common` | `#d4d4d8` | `hue-damage-physical` | `#e6e2d9` |
| `hue-rarity-uncommon` | `#41f1b6` | `hue-damage-magical` | `#9d86ef` |
| `hue-rarity-rare` | `#7cb7ff` | `hue-damage-bleed` | `#d94d5c` |
| `hue-rarity-epic` | `#e879f9` | `hue-damage-burn` | `#ef8a3c` |
| `hue-rarity-unique` | `#facc15` | `hue-damage-poison` | `#82b94b` |
| `hue-rarity-legendary` | `#fb923c` | `hue-damage-shadow` | `#69b6dd` |
| `hue-rarity-legacy` | `#fb7185` | `hue-damage-none` | `#8d8991` |

### Tier 2 · Semantic roles

| Family | Role | Aliases | Value | For |
| --- | --- | --- | --- | --- |
| **Grounds** | `ground` | `slate-900` | `#101014` | Level 0: the page, behind everything; GameShell's stage behind a Page, and the docked Chronicle. |
|  | `ground-deep` | `slate-950` | `#0b0b0f` | Below Level 0: wells (inputs, item slots), the vignette, the letterbox behind stage art. |
|  | `surface` | `slate-900-a72` | `#101014b8` | Level 1: the rail, Panels, lists and the JourneyCard. The game's panel material: translucent over the frame's backdrop, never blurred (D-102). |
|  | `surface-solid` | `slate-875` | `#16161b` | Level 1 where something passes beneath it: sticky table headers and columns, the floating chat drawer, `lg-level-1--float`. About what `surface` shows over the backdrop. |
|  | `surface-raised` | `slate-775` | `#22222a` | Level 2: the hover, selected and mention washes; popovers, hover cards, menus and toasts. |
|  | `folio` | `slate-850` | `#131318` | Level 3: the Folio, dialog sheets, confirmations and the tour's coach mark. Darker than `surface-raised`: its height reads from `shadow-panel`. |
|  | `scrim` | `slate-950-a80` | `#0b0b0fcc` | Above the levels: the flat dimmer behind the rail drawer, dialogs, confirmations and the tour. Never blurred. |
| **Lines** | `line` | `slate-100-a12` | `#f4f4f81f` | Separation and decoration: row and group separators, bounded objects that are not controls. Never the only edge of a control (Foundations · Lines). |
|  | `line-strong` | `umber-450` | `#8c7358` | Interactive edges — inputs, outline buttons, clickable pills and slots, tab separators — floating surfaces and dotted Ledger leaders. |
|  | `row-stripe` | `slate-100-a04` | `#f4f4f80a` | The zebra rhythm: every second row of a wide List or table set to zebra, on Level 1 or in the Folio. Never with separators. |
| **Ink** | `ink` | `bone-100` | `#f0e6d2` | Text and every ordinary data value. |
|  | `ink-muted` | `bone-300` | `#bba98c` | Secondary text, labels, eyebrows, inactive items, captions. |
|  | `ink-disabled` | `bone-500` | `#7a6a56` | Plain disabled controls and the names of locked things only, a locked name always with a lock or "Locked". |
| **Gilt** | `gilt` | `brass-300` | `#dcb872` | The brand's brass, with four jobs: brand and current location, the one committing action, the screen's one headline figure, effect magnitudes inside descriptions. |
|  | `gilt-soft` | `brass-900` | `#3a2b17` | The former hover wash. No new uses; two components still read it (Not yet on the allocation). |
|  | `on-gilt` | `umber-875` | `#1a1109` | Text and icons on a `gilt` fill. |
| **Arcana** | `arcana` | `verdigris-300` | `#6fcab9` | Ready, claimable, new, actionable or selected, as text and icons: "+ New", "Claimable", rail badges, ready states. |
|  | `arcana-soft` | `verdigris-900` | `#163833` | The wash behind `arcana` text on small marks — the `new` Tag, rail badges. Never a row, list item or card. |
|  | `arcana-glow` | `verdigris-200` | `#86e6d2` | Flat fills for selected and ready: the active-tab bar, the selected ring or edge, the ready diamond. Never text, never a halo (the name predates D-012). |
| **Status** | `danger` | `ember-300` | `#f2837a` | Loss, destruction, failure — Pending Loot lost, a failed save, "Defeated". With a word or icon. |
|  | `danger-soft` | `ember-900` | `#3d1715` | The wash behind an inline alert about a loss or failure — inline alerts only. Nothing reads it now that Tags are outlined. |
|  | `success` | `bone-100` | `#f0e6d2` | A confirmed outcome only: `ink` with ✓ and the word. "Ready" and "Claimable" are arcana. No hue of its own (D-016). |
|  | `warning` | `amber-300` | `#f0b35a` | Attention: a reversible risk, insufficient resources, something expiring soon. With a word or icon; never in descriptions. |
|  | `info` | `azure-200` | `#9ccaf0` | Neutral notices, with a word or icon. |
| **Focus** | `focus` | `brass-200` | `#f5d48f` | The keyboard focus ring (Foundations · Accessibility). |
| **Live** | `changed` | `umber-650` | `#46372a` | The live-update mark: a flat wash behind a value that changed by itself — a Bazaar price, a roster status, a payout — on at once, held for `duration-reveal`, faded over `duration-slow` (Foundations · Motion). Behind `ink` and `ink-muted` values only; never a hover, selected, row or rarity wash. |

Status colours appear in shell, control and data contexts only; inside item, combat and chat contexts a status is a word in `ink`. The stacking order of the grounds is in Foundations · Surfaces & Layering.

### Tier 3 · Domain roles

The game's own lists — set out, with names and codes, in Registries — and the polarity roles.

| Family | Token | Aliases | Value | For | Same value as |
| --- | --- | --- | --- | --- | --- |
| **Meters** | `meter-track` | `slate-750` | `#2a2a33` | The empty part of every Meter and Track. | — |
|  | `meter-hp` | `ember-500` | `#d0443f` | Health (HP). | — |
|  | `meter-sp` | `azure-500` | `#4f9fd6` | Stamina or mana (SP). | — |
|  | `meter-xp` | `brass-300` | `#dcb872` | Experience (EXP) and level progress. | `gilt` |
| **Channels** | `channel-general` | `bone-100` | `#f0e6d2` | General — neutral. | `ink` |
|  | `channel-trade` | `amber-300` | `#f0b35a` | Trade. | `warning` |
|  | `channel-help` | `bone-100` | `#f0e6d2` | Help — neutral. | `ink` |
|  | `channel-guild` | `verdigris-300` | `#6fcab9` | Guild. | `arcana` |
|  | `channel-whisper` | `orchid-100` | `#f6cbf1` | Whispers to and from you. | — |
|  | `channel-raid` | `ember-300` | `#f2837a` | Raid. | `danger` |
|  | `channel-invites` | `azure-200` | `#9ccaf0` | Guild, party and raid invites. | `info` |
|  | `channel-system` | `bone-300` | `#bba98c` | System lines, in lore italic — neutral. | `ink-muted` |
|  | `channel-loot` | `bone-300` | `#bba98c` | Loot — what you found (D-005) — neutral. | `ink-muted` |
| **Polarity** | `delta-better` | `lichen-200` | `#b5ea99` | Better for the player, with ▲ or ▼ and a sign. | — |
|  | `delta-worse` | `madder-400` | `#d671a7` | Worse for the player, with ▲ or ▼ and a sign. | — |
|  | `delta-neutral` | `bone-300` | `#bba98c` | Unchanged (±0), or not judged. | `ink-muted` |
|  | `effect-beneficial` | `lichen-200` | `#b5ea99` | Beneficial effects and conditions, always named. | `delta-better` |
|  | `effect-harmful` | `madder-400` | `#d671a7` | Harmful effects and conditions, always named. | `delta-worse` |
| **Rarity** | `rarity-common` … `rarity-legacy` (7) | `hue-rarity-*` of the same name | Tier 1 table | Item names, slot edges, rarity Tags — always with the code (D-006). | — |
| **Damage** | `damage-physical` … `damage-none` (7) | `hue-damage-*` of the same name | Tier 1 table | Damage numbers and tags — always with the type's name. | — |

Channels and `meter-xp` alias primitives directly, so they do not move when a role changes. Several share a value with a role — `channel-trade` and `warning` are both `amber-300` — but in different contexts, so recolouring `warning` leaves Trade as it is. When a channel should follow a role, point both at the new primitive on purpose.

**Deprecated placeholders.** `condition-beneficial` and `condition-harmful` were reserved in D-014 and never read. D-023 names the roles `effect-beneficial` and `effect-harmful`, so the old names alias the new ones until they are removed (Deprecating a token).

### Component tokens

A component token belongs to one component and is read only there. It aliases a role when it means the same thing and a primitive when no role fits. When a second component needs it, that component takes a role instead, or the token is promoted to a role (Adding a token).

| Token | Component | Aliases | Value | For | Also read by (known exception) |
| --- | --- | --- | --- | --- | --- |
| `sigil-fill` | Sigil | `verdigris-800` | `#1d4b45` | The hexagon badge's fill. | — |
| `sigil-edge` | Sigil | `verdigris-250` | `#7fc9b8` | The hexagon's outline. | — |
| `on-sigil` | Sigil | `ink` | `#f0e6d2` | Numerals inside the badge. | — |
| `tile` | TabStrip (StatTile's until D-137) | `slate-800` | `#1b1b22` | The active primary tab's fill. | — |
| `on-tile` | TabStrip (StatTile's until D-137) | `ink` | `#f0e6d2` | Text on the active primary tab. | — |
| `on-tile-muted` | None since D-137 (StatTile) | `ink-muted` | `#bba98c` | Labels and units on a tile. | — |

One reader broke the one-component rule and was flagged rather than hidden: TabStrip's active primary tab takes the tile fill. Since StatTile merged into Ledger (D-137) TabStrip is the tile tokens' only reader, so the next token pass can make them TabStrip's own and retire `on-tile-muted`. (PageHeader used to draw its own hex with the Sigil tokens; its section mark is now a `gilt` diamond, D-065.) It is queued in Governance · Audit & consolidation map (revision item 3), to be resolved by promoting the token to a role or giving the reader its own component token.

## Naming convention

| Tier | Pattern | Examples |
| --- | --- | --- |
| 1 · ramps | `<ramp>-<step>`, and `-a<opacity>` for a translucent version | `slate-900`, `bone-300`, `slate-950-a80` |
| 1 · game hues | `hue-<registry>-<entry>` | `hue-rarity-epic`, `hue-damage-bleed` |
| 2 | `<role>`, `<role>-<modifier>`, or `on-<role>` for text and icons on that role's fill | `surface-raised`, `ink-muted`, `on-gilt` |
| 3 | `<registry>-<entry>`, with the entry spelled as Registries spells it | `rarity-legendary`, `channel-whisper`, `meter-hp` |
| Component | `<part>`, `<part>-<modifier>`, or `on-<part>`; a new one starts with the component's name | `sigil-fill`, `on-tile-muted` |

- **Only Tier 1 is named for its colour.** A ramp's name is a material, pigment or substance of the grimoire's world (umber, bone, brass, verdigris, ember, amber, azure, orchid); a new ramp gets a name in the same spirit, not a bare `red` or `brown`.
- **Roles are named for their job, never their hue:** `ink-muted`, not `beige`; `danger`, not `red`. A primitive never names a role: `umber-900`, not `ground-900`.
- **Modifiers mean the same everywhere:** `-deep` and `-raised` step a ground down or up, `-strong` is the edge that must be seen, `-muted` and `-disabled` step ink back, `-soft` is the wash behind that colour's text, `on-` is what sits on that fill.
- **Names are lowercase with hyphens, unique across every token family** (the page shares one namespace for colour, shadow, spacing, radius, layout and z-index).
- **A step's number places it by lightness** (OKLab L) between its neighbours; take a free number between them rather than renumbering the ramp. `orchid-100` is the only orchid step left once `orchid-300` was removed.

## How aliases work

- A Tier 1 token holds a raw colour. Every other colour token holds an alias, `{brass-300}`, which the page compiles to `var(--lg-brass-300)`: `--lg-gilt` resolves in the browser to brass-300's value, so a component's `var(--lg-gilt)` never changes when the palette is reorganised.
- Chains are short: Tier 2 and Tier 3 alias a primitive directly (one step); a component token may alias a role (two steps). The page would follow up to 16; we keep it to two.
- An alias must name an existing token and cannot mix, tint or fade it. A translucent colour is a primitive of its own (`slate-950-a80`, for `scrim`).
- Changing a primitive moves every token that aliases it — its usage line lists them. To change one role, repoint that role.
- **Raw values outside Tier 1.** Shadow tokens cannot alias, so three repeat palette values by hand: `focus-ring` repeats `slate-900` (#101014) and `brass-200` (#f5d48f), and `shadow-text-art` repeats `slate-950` (#0b0b0f); `shadow-panel` and `shadow-float` use black, which has no primitive. Changing those primitives means editing those shadows too. EntryList's list-end mask uses `#000` as an alpha value; a mask is not a colour and takes no token.
- **Check:** no component names a primitive. Search the components' CSS (`src/app/grimoire/**/*.css`, all but the compiled `tokens/tokens.css`) for `var(--lg-umber-`, `--lg-bone-`, `--lg-brass-`, `--lg-verdigris-`, `--lg-ember-`, `--lg-amber-`, `--lg-azure-`, `--lg-orchid-` and `--lg-hue-`; today there are none. Raw values — a hex colour, a pixel font size, a shadow with its own colour — are checked automatically: `node design-system/scripts/check.mjs`.

## Adding a token

1. Look for a role that already says it. Most requests are an existing role in a new place.
2. Choose the tier: an entry in one of the game's lists is Tier 3, added to Registries first; a job any screen could have is Tier 2; one component's own colour is a component token.
3. Find the primitive. Reuse one whose value fits; otherwise add a step to the ramp whose hue fits, numbered by lightness between its neighbours. Start a new ramp only when no ramp's hue fits.
4. Add it to `tokens.json` inside its tier's block, as an alias, with a usage line that starts with its tier and says where it is used.
5. Give it one job in the allocation table, in a context where its hue family has no job yet — or change the table, with a Decision Log entry.
6. Measure it against every token of its cluster and add its rows to Collisions; a pair below ΔE00 20 that can sit side by side needs a verdict of Words or Forms, and one below 10 in the same form does not ship. Check the key pairs for colour vision.
7. Measure it on `ground`, `surface`, `surface-raised` and `folio`, and add its row to Contrast. A text token below 4.5:1 or an edge below 3:1 is listed as a failure with the rule for using it, or it does not ship.
8. If it adds or narrows a rule, log it in the Decision Log.

## Deprecating a token

1. Log a Decision Log entry naming the replacement.
2. Keep the token and alias it to its replacement — the one case where a token aliases a role — and start its usage line with "Deprecated (D-xxx) — use `<replacement>`."
3. Add it to the table below and move every reader to the replacement; the audit lists the readers.
4. Remove it, with a Decision Log entry, once no component, pattern or game screen reads it. A primitive is removed as soon as nothing aliases it.

| Deprecated token | Replacement | Decision | Remove when |
| --- | --- | --- | --- |
| `condition-beneficial` | `effect-beneficial` | D-023 | The next release: nothing reads it |
| `condition-harmful` | `effect-harmful` | D-023 | The next release: nothing reads it |

`gilt-soft` is not deprecated yet: it still has two readers and no single replacement (a hover wash and "Recommended now" need different answers). The `gilt` tone of Tag is a component variant, deprecated on Tag's page (D-020). Primitives are removed, not deprecated, once nothing aliases them — `orchid-300` and `azure-300` in D-016.

## Contrast

WCAG 2.2 contrast ratios, measured from the token values in sRGB and truncated to two decimals, so a ratio shown as 4.50 always passes. Text needs 4.5:1 (WCAG 1.4.3); an edge or other non-text mark a player must see needs 3:1 (WCAG 1.4.11). Large text — 24px regular or 19px bold and up — needs 3:1. The `surface` column is measured on `surface-solid`, about what the translucent `surface` shows over the frame's backdrop; text on a Panel in the game's frame was also measured over the real backdrop, at 5.9:1 or better (D-102). `line` and `row-stripe` are measured over each fill. `scrim` is translucent and not measured; deprecated tokens share their replacements' values. `success`, `channel-general` and `channel-help` share `ink`'s value and its ratios; `effect-beneficial` and `effect-harmful` share the deltas'; `delta-neutral` shares `ink-muted`'s.

### Failures

Every ratio below the threshold, flagged here and marked **fail** in the tables.

| Token | Fails | Measured | Why it ships, and the rule |
| --- | --- | --- | --- |
| `ink-disabled` | Text 4.5:1 on all four grounds | 3.02–3.63 | Plain disabled controls and the names of locked things only, a locked name always with a lock or the word "Locked" in `ink-muted`; WCAG exempts the text of inactive controls. It passes 3:1. |
| `damage-bleed` | Text 4.5:1 on `surface`, `surface-raised` | surface 4.43, surface-raised 3.88 | Kept from the game's palette. Set bleed numbers on `ground` (4.67) or `folio` (4.55), or as large text: bold at 19px and up. It passes 3:1 everywhere. |
| `line` | Edge 3:1 on all four grounds | 1.36–1.44 | Separation and decoration only — never the only edge of a control (Foundations · Lines). |
| `row-stripe` | Edge 3:1 on all four grounds | 1.08–1.10 over each fill | A rhythm, not a state and not an edge: it helps the eye along a wide row. Rows are also told apart by their text; use it on Level 1 or in the Folio, not on `ground` (Foundations · Lines). |
| `meter-track` | Edge 3:1 on all four grounds | 1.11–1.33 | The empty part is a fill. The `bar` Meter draws its full length with a `line-strong` edge (passes); the thin Meter and the Track rely on the numbers printed beside them. |
| `surface-raised`, `gilt-soft`, `arcana-soft`, `danger-soft`, `changed` | Edge 3:1 on the grounds | 1.00–1.66; `surface-raised` 1.14–1.20, `changed` 1.38–1.66 | Washes, never the only sign of a state: selection also takes a bar, ring or edge and a weight or face change (Standards · States); a mention also takes its `ink` edge and bold `@you`; hover is an extra cue, not a state a player must see; the live mark is an extra cue too, since the value itself shows the change. |
| `gilt-soft` in SearchField | Edge 3:1 on `surface-raised` | 1.15 | **Breach, not an exception:** the highlighted suggestion in SearchField's list is marked by the wash alone. Queued in Governance · Audit & consolidation map (revision item 3). |
| `tile`, `sigil-fill` | Edge 3:1 on all four grounds | 1.05–1.93 | Fills. The Sigil's shape is drawn by `sigil-edge` (passes); TabStrip's active tab also takes the `arcana-glow` bar. |

### Text tokens on the four grounds

| Token | Primitive | `ground` | `surface` | `surface-raised` | `folio` | Text 4.5:1 | Edge 3:1 |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| `ink` | `bone-100` | 15.32 | 14.55 | 12.74 | 14.95 | Pass | Pass |
| `ink-muted` | `bone-300` | 8.28 | 7.87 | 6.89 | 8.08 | Pass | Pass |
| `ink-disabled` | `bone-500` | 3.63 **fail** | 3.45 **fail** | 3.02 **fail** | 3.54 **fail** | **Fail** on all four | Pass |
| `gilt` | `brass-300` | 10.07 | 9.56 | 8.37 | 9.82 | Pass | Pass |
| `arcana` | `verdigris-300` | 9.78 | 9.29 | 8.13 | 9.54 | Pass | Pass |
| `danger` | `ember-300` | 7.50 | 7.12 | 6.24 | 7.32 | Pass | Pass |
| `warning` | `amber-300` | 10.21 | 9.70 | 8.49 | 9.96 | Pass | Pass |
| `info` | `azure-200` | 10.95 | 10.40 | 9.11 | 10.68 | Pass | Pass |
| `delta-better` | `lichen-200` | 13.73 | 13.04 | 11.42 | 13.39 | Pass | Pass |
| `delta-worse` | `madder-400` | 6.12 | 5.82 | 5.09 | 5.97 | Pass | Pass |
| `rarity-common` | `hue-rarity-common` | 12.84 | 12.19 | 10.68 | 12.52 | Pass | Pass |
| `rarity-uncommon` | `hue-rarity-uncommon` | 13.09 | 12.43 | 10.88 | 12.77 | Pass | Pass |
| `rarity-rare` | `hue-rarity-rare` | 9.10 | 8.64 | 7.57 | 8.88 | Pass | Pass |
| `rarity-epic` | `hue-rarity-epic` | 7.71 | 7.32 | 6.41 | 7.52 | Pass | Pass |
| `rarity-unique` | `hue-rarity-unique` | 12.39 | 11.77 | 10.30 | 12.09 | Pass | Pass |
| `rarity-legendary` | `hue-rarity-legendary` | 8.38 | 7.96 | 6.97 | 8.18 | Pass | Pass |
| `rarity-legacy` | `hue-rarity-legacy` | 7.05 | 6.69 | 5.86 | 6.88 | Pass | Pass |
| `damage-physical` | `hue-damage-physical` | 14.68 | 13.94 | 12.21 | 14.32 | Pass | Pass |
| `damage-magical` | `hue-damage-magical` | 6.40 | 6.08 | 5.32 | 6.25 | Pass | Pass |
| `damage-bleed` | `hue-damage-bleed` | 4.67 | 4.43 **fail** | 3.88 **fail** | 4.55 | **Fail** on `surface`, `surface-raised` | Pass |
| `damage-burn` | `hue-damage-burn` | 7.56 | 7.18 | 6.29 | 7.38 | Pass | Pass |
| `damage-poison` | `hue-damage-poison` | 8.12 | 7.71 | 6.75 | 7.92 | Pass | Pass |
| `damage-shadow` | `hue-damage-shadow` | 8.43 | 8.01 | 7.01 | 8.23 | Pass | Pass |
| `damage-none` | `hue-damage-none` | 5.53 | 5.25 | 4.60 | 5.39 | Pass | Pass |

### Other text-capable tokens

Channel colours set speaker names and tags; the component tokens carry text on their own fills.

| Token | Primitive | `ground` | `surface` | `surface-raised` | `folio` | Text 4.5:1 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| `channel-general` | `bone-100` | 15.32 | 14.55 | 12.74 | 14.95 | Pass |
| `channel-trade` | `amber-300` | 10.21 | 9.70 | 8.49 | 9.96 | Pass |
| `channel-help` | `bone-100` | 15.32 | 14.55 | 12.74 | 14.95 | Pass |
| `channel-guild` | `verdigris-300` | 9.78 | 9.29 | 8.13 | 9.54 | Pass |
| `channel-whisper` | `orchid-100` | 13.31 | 12.64 | 11.07 | 12.99 | Pass |
| `channel-raid` | `ember-300` | 7.50 | 7.12 | 6.24 | 7.32 | Pass |
| `channel-invites` | `azure-200` | 10.95 | 10.40 | 9.11 | 10.68 | Pass |
| `channel-system` | `bone-300` | 8.28 | 7.87 | 6.89 | 8.08 | Pass |
| `channel-loot` | `bone-300` | 8.28 | 7.87 | 6.89 | 8.08 | Pass |

| Text | On | Ratio | Text 4.5:1 |
| --- | --- | ---: | --- |
| `delta-better` | `tile` | 12.38 | Pass |
| `delta-worse` | `tile` | 5.52 | Pass |
| `on-gilt` | `gilt` | 9.87 | Pass |
| `on-sigil` | `sigil-fill` | 7.90 | Pass |
| `on-tile` | `tile` | 13.82 | Pass |
| `on-tile-muted` | `tile` | 7.47 | Pass |
| `ink` | `surface-raised` | 12.74 | Pass |
| `ink` | `gilt-soft` | 11.02 | Pass |
| `ink-muted` | `gilt-soft` | 5.96 | Pass |
| `gilt` | `gilt-soft` | 7.24 | Pass |
| `arcana` | `arcana-soft` | 6.57 | Pass |
| `ink` | `arcana-soft` | 10.29 | Pass |
| `danger` | `danger-soft` | 6.23 | Pass |
| `ink` | `danger-soft` | 12.73 | Pass |
| `ink` | `changed` | 9.21 | Pass |
| `ink-muted` | `changed` | 4.98 | Pass |

### Edges and marks (3:1)

| Token | Used as | `ground` | `surface` | `surface-raised` | `folio` | Edge 3:1 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| `line` | Separators and decoration | 1.36 **fail** | 1.39 **fail** | 1.44 **fail** | 1.37 **fail** | **Fail** on all four |
| `line-strong` | Interactive edges, floating edges, dotted leaders | 4.25 | 4.04 | 3.53 | 4.15 | Pass |
| `focus` | The focus ring | 13.28 | 12.61 | 11.04 | 12.95 | Pass |
| `arcana-glow` | The selected ring, edge or bar; the ready diamond | 12.89 | 12.24 | 10.72 | 12.57 | Pass |
| `gilt` | Brand frames; the current-location diamond and the rail's current bar | 10.07 | 9.56 | 8.37 | 9.82 | Pass |
| `ink` | The Chronicle's active-tab bar and mention edge | 15.32 | 14.55 | 12.74 | 14.95 | Pass |
| `sigil-edge` | The Sigil outline | 9.89 | 9.40 | 8.23 | 9.65 | Pass |
| `meter-hp` | Health fill | 4.14 | 3.93 | 3.44 | 4.03 | Pass |
| `meter-sp` | Stamina or mana fill | 6.56 | 6.23 | 5.45 | 6.40 | Pass |
| `meter-xp` | Experience fill | 10.07 | 9.56 | 8.37 | 9.82 | Pass |
| `meter-track` | The empty part of a meter | 1.33 **fail** | 1.26 **fail** | 1.11 **fail** | 1.30 **fail** | **Fail** on all four |
| `surface-raised` | Hover and mention wash | 1.20 **fail** | 1.14 **fail** | — | 1.17 **fail** | **Fail** on the other three |
| `gilt-soft` | Former hover wash | 1.39 **fail** | 1.32 **fail** | 1.15 **fail** | 1.35 **fail** | **Fail** on all four |
| `arcana-soft` | Wash behind arcana | 1.48 **fail** | 1.41 **fail** | 1.23 **fail** | 1.45 **fail** | **Fail** on all four |
| `danger-soft` | Wash behind danger | 1.20 **fail** | 1.14 **fail** | 1.00 **fail** | 1.17 **fail** | **Fail** on all four |
| `changed` | The live-update mark | 1.66 **fail** | 1.58 **fail** | 1.38 **fail** | 1.62 **fail** | **Fail** on all four |
| `tile` | The active tab's fill | 1.10 **fail** | 1.05 **fail** | 1.08 **fail** | 1.08 **fail** | **Fail** on all four |
| `sigil-fill` | Sigil fill | 1.93 **fail** | 1.84 **fail** | 1.61 **fail** | 1.89 **fail** | **Fail** on all four |

| Mark | Against | Ratio | Edge 3:1 |
| --- | --- | ---: | --- |
| `meter-hp` | `meter-track` | 3.10 | Pass |
| `meter-sp` | `meter-track` | 4.91 | Pass |
| `meter-xp` | `meter-track` | 7.54 | Pass |
| `sigil-edge` | `sigil-fill` | 5.10 | Pass |
| `arcana-glow` | `tile` | 11.62 | Pass |

Every text token that passes 4.5:1 also passes 3:1 as an edge, so rarity-coloured slot edges pass; on `ground-deep`, the darkest ground, every ratio is higher than on `ground`.

## Migration map: `--ll-*` to Grimoire

Components use the `lg-` class prefix so they can live beside today's `ll-` classes while screens migrate one at a time. Each old token maps to a Grimoire role — never to a primitive. The map also covers the non-colour families, so there is one map to follow.

| Old token | Grimoire role |
| --- | --- |
| `--ll-color-bg` | `ground` |
| `--ll-color-canvas` | `ground-deep` |
| `--ll-color-surface` | `surface` |
| `--ll-color-surface-elevated` | `surface-raised` |
| `--ll-color-surface-hover` / `-selected` | `gilt-soft` |
| `--ll-color-text` | `ink` |
| `--ll-color-text-secondary` / `-muted` | `ink-muted` |
| `--ll-color-text-disabled` | `ink-disabled` |
| `--ll-color-primary` | `gilt` |
| `--ll-color-on-primary` | `on-gilt` |
| `--ll-color-border` | `line` |
| `--ll-color-border-strong` / `--ll-color-control-border` | `line-strong` |
| `--ll-color-focus-ring` | `focus` |
| `--ll-rarity-*` | `rarity-*` |
| `--ll-damage-type-*` | `damage-*` |
| `--ll-space-*` | `space-*` |
| `--ll-radius-*` | `radius-*` |
| `--ll-z-*` | `z-*` |
| `--ll-font-display` | `display` |
| `--ll-font-body` | `ui` |

`--ll-color-primary` maps to `gilt`, but only for gilt's four jobs: an old screen that used the primary colour for labels, values or selection takes `ink-muted`, `ink` or `arcana-glow` as it migrates.

## Do and don't

| Do | Don't |
| --- | --- |
| Set Ledger values in `ink`, and the one headline figure in a gilt StatFigure. | Set every value on the screen in `gilt`. |
| Show the current screen with the gilt bar and wash in the NavRail (D-118). | Mark a selected list entry in `gilt` — selection is `arcana-glow`. |
| Put "+ New" in `arcana` on `arcana-soft`. | Colour "Online", a positive delta or a link in `arcana`. |
| Write "Completed ✓" in `ink`. | Colour a completed quest teal next to a ready one. |
| In an Epic item's Folio, set the title in `rarity-epic` and "+14 Power" in bold `ink`. | Set the item's effect magnitudes in `gilt` beside its rarity-coloured name. |
| In chat, colour only the tag and the speaker's name; mark a mention with the neutral wash and an `ink` edge. | Colour the message text, the tab bar or the mention by channel or in gilt. |
| Name the damage type with every damage number: "124 bleed". | Rely on red for bleed and green for poison. |
| `color: var(--lg-gilt)` in a component. | `color: var(--lg-brass-300)` or `#dcb872` in a component. |
| Point `warning` at another primitive to recolour warnings. | Edit `amber-300` to recolour warnings — Trade moves with it. |
| Give the one committing action on a screen the `solid` gilt button ("Enter dungeon"). | Put two `solid` buttons on one screen. |
| Print "240 / 300" beside a health bar. | Let the `meter-hp` fill carry the value alone. |
| Set a bleed number on `ground` or `folio`, or bold at 19px. | Set a 13px regular bleed number on `surface` (4.43:1). |
| Show a cooldown cut from 8s to 6.8s as ▼ −1.2s in `delta-better`. | Colour it `delta-worse` because the number fell. |
| Say "12 Soulstones short" as a `warning`. | Show a shortfall as `danger`. |
| Tag a reward that can be claimed in `arcana`; say "✓ Claimed" once it is. | Mark "Ready to claim" as `success`. |
| Name a harmful condition in a `harmful` Tag: "Weaken 6s", the time in `value`. | A madder dot with no name. |
| Outline a `danger` Tag, and keep `danger-soft` for an inline alert. | Wash a list row or a card in `danger-soft`. |

## Related components

- Delta — the stat change
- Ledger — the labelled value list
- Folio — the detail panel
- Tag — the status label
- NavRail — the main navigation
- Chronicle — chat and the game log
- Button — the command button
- Meter — the progress bar
- Sigil — the hex stat badge
- TabStrip — the tabs
- PageHeader — the information screen heading
- ItemSlot — the item frame
