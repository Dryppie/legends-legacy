# Standards

Standards are what every part must do, whichever layer it sits in: how it shows its states, where it sits in the hierarchy, what it discloses on demand, how it works without art, and how it speaks. Each part below follows the section template. States has a page of its own, Standards · States, and how several states meet on one thing has another, Standards · State combinations.

## States

Every part shows its state in the same way everywhere, by a channel other than colour and in the same words. The full model — six families, each state's meaning, channel, tokens, words and announcement, and which parts take which states — is in Standards · States (D-086). Which marks show when several states meet, how many and where, is in Standards · State combinations (D-094).

**Must**
- Show `focus-ring` on every interactive element, over every other state. Never remove it.
- Give every blocked thing its reason, and keep it focusable: `aria-disabled` with the reason as its description (Standards · States · The reason tip).
- Use the words Standards · States gives each state.

**Never**
- Show a state by colour alone, or with a glow (D-012).

Related components: every interactive component; Standards · States lists them.

## Information Hierarchy

What the player reads first, second and third. The next decision leads (Principles · Decisions first). Detail and collection screens have one subject; workbench screens may hold several, each ordered the same way (D-010).

**Must**
- Keep one Folio per screen; it explains the one thing the player has selected (Decision D-004).
- Keep the currency and level in the TopBar and nowhere else (see the note in Shell).
- Use one `solid` button per screen, for the committing action.

**Should**
- On information screens, lead with what the player needs to decide next (the JourneyCard), then who they are (one Banner with the headline figures), then the detail (Panels and Ledgers).
- Use one `folio` heading per screen; `screen` headings for screen titles and `section` headings for groups.
- Keep to one display-size element (56px and up) per screen. Where a LevelPlate is present, its numeral is that element: titles step down to `title-lg` and headline numbers use StatFigure `sm`. Use one LevelPlate per detail view and one Banner per screen (Principles · Anti-generic guardrails, D-011).
- Use primary tabs for what the player is browsing (Creatures, Regions) and secondary tabs for how it is filtered (All, Discovered).

**Never**
- Use three levels of tabs.
- Put navigation in the Folio.
- Nest Panels, or pad a container fully inside another: an inner surface steps down one level, and a list inside a Panel uses the Panel's padding (Foundations · Space & Density).

| Heading level | Size | Use |
| --- | --- | --- |
| `folio` | `title-xl`, 56px | The selected thing's name in the Folio — once per screen, never in a list |
| `screen` | `title-lg`, 36px | Screen titles and the name in a Banner |
| `section` | `title-md`, 24px | Sections inside a screen: Combat Attributes, Order book |
| `subsection` | `title-sm`, 20px | Sub-sections inside a section: Offense, Recent trades |

Headings are `ink` and eyebrows are `ink-muted`. The one headline figure and effect magnitudes inside descriptions are `gilt` (Foundations · Colour · Allocation).

| Do | Don't |
| --- | --- |
| Journey, then Combat Profile, then Combat Attributes on the Overview. | Attributes first because they are the longest block. |
| One `solid` "Enter dungeon" button. | "Enter dungeon" and "Buy potions" both `solid`. |

Related components: Folio, Heading, StatFigure, LevelPlate, Banner, JourneyCard, TabStrip, TopBar, Button, Panel.

## Progressive Disclosure

Every value is on the screen; its explanation is one hover, focus or click away, next to the value it explains.

**Must**
- Show the value itself on the screen; disclose only its explanation, breakdown or exact form.
- Open anything that opens on hover on keyboard focus too, and pin it on tap; it stays while hovered and closes on Escape (Foundations · Accessibility · Hover, focus and tap).

**Should**
- Explain a Ledger row with its `description` (and `tipMeta` as a footnote in `ink`), shown on hover and keyboard focus.
- Put a StatFigure's full explanation in its `title`.
- Put an abbreviated amount's full figure in the tooltip (CurrencyPill `short`).
- Reveal secondary detail in place with a `link` Button that sets `aria-expanded` — "Show perks" / "Hide perks" on the Combat Profile.
- Let selection disclose detail: choosing an entry, a sigil or an item fills the Folio.
- Let the Chronicle collapse to its strip or ticker rather than disappear.

**Never**
- Hide the only copy of a value behind hover.
- Put an explanation only a mouse can reach.

| Do | Don't |
| --- | --- |
| "Crit Chance · 9%" with the formula in a tooltip on hover and focus. | "Crit Chance" with "9%" only in the tooltip. |
| "12.5k" with 12,480 in the tooltip. | "12.5k" with no exact figure anywhere. |
| "Show perks" expanding the perk list under the Nobility line. | A separate Nobility screen for the same list. |

Related components: Ledger, StatFigure, CurrencyPill, Button, Folio, ItemLink, Chronicle.

## Art-optional

Art is atmosphere, never content, so every screen and part must be complete without it. Equipment and Essences are presented text-first until an art strategy is decided (Decision D-003).

**Must**
- Carry every fact as text: names, rarity codes, stats, abilities.
- Present equipment and Essences text-first: the name in its rarity colour, the rarity code, and the meta line. Images are optional.
- Keep a layout intact when an image is missing or still loading.

**Should**
- Fall back to the game's Icon set, not a placeholder picture; LoadoutSlot falls back to the Essences icon.
- Leave Stage and Banner without `image` when no art fits; their veil and ground stand on their own.

**Never**
- Make art the only carrier of information.
- Fill a slot with stand-in art to make it look finished while the art strategy is open.

**Known gap.** ItemSlot styles itself as empty (a dashed frame) when it has neither `image` nor `icon`. Until it has a text-first variant, pass an `icon` for text-first items.

| Do | Don't |
| --- | --- |
| "Ember Fang" in `rarity-epic`, code E, "One-handed · 42–51" underneath. | A painted sword with the name only in the tooltip. |
| An Essence slot with the Essences icon, its name and its two abilities. | An empty frame waiting for Essence art. |

Related components: ItemSlot, LoadoutSlot, ItemLink, Folio, Stage, Banner.

## Content

How the game speaks. Keep the tone clear, readable, slightly arcane, never noisy.

**Must**
- Speak to the player as "you". Instructions are short imperatives: "Retreat to secure your pending loot." "Advance room by room."
- Name things with the game's own nouns, capitalised as proper nouns: Cinders, Soulstones, Essences, Legacy Ascension, the World Tower, the Cinder Bazaar, Shenic.
- Keep two registers, two faces. **Mechanics** in `body` (Barlow): exact, numeric, no flourish — "+12% damage from equipment". **Lore** in `lore` (EB Garamond italic): one or two sentences of atmosphere — "Temples older than the roads that lead to them." Never mix them in one line; never put numbers in lore.
- Write labels in sentence case ("View breakdown"). Navigation, tabs, section bands, labels and tags are set in capitals by CSS — don't type them in capitals.
- Keep anything set in tracked capitals — tabs, rail items, section bands, Panel heads, column heads, Tags — to three words or fewer. A longer label is sentence case (Foundations · Typography).

**Should**
- Label buttons with a verb or verb phrase ("Level up", "Enter dungeon").
- Keep Tags to one to three words, and let them state facts.
- Give KeyHints three or four hints, only for keys that work on this screen.
- Take journey copy straight from the game's player-journey guidance.
- Link item names, not sentences.

**Never**
- Use emoji, exclamation marks or "Awesome!". Victory is stated, not cheered: "Defeated", "Floor cleared".
- Use a Tag as a button.

Number formatting is in Foundations · Numerals.

| Do | Don't |
| --- | --- |
| "Floor cleared" | "Floor cleared! Awesome!" |
| "Retreat to secure your pending loot." | "The player may wish to consider retreating." |
| "You found [Ember Fang]." | "[You found Ember Fang.]" as one link |
| "Cinders" | "coins" or "gold" |

Related components: Button, Tag, KeyHints, JourneyCard, ItemLink, Chronicle, Folio.
