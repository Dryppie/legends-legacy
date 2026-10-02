# Foundations · Iconography

Icons label things; they never replace a word, except for a few common actions. Grimoire's icons are one set, drawn the way the game's fifteen sidebar icons are drawn: a 24-unit grid, a 1.6 stroke, round caps and joins, in `currentColor`. This page sets that construction as the standard for every future icon. It also sets the size scale, the taxonomy the game needs, the label and colour rules, and the icon slot every registry entry carries. It ends with an inventory of what exists and what is needed. The new icons are not drawn yet; that is a separate task, and the inventory is its brief.

## Rules

**Must**
- Draw every icon to the construction standard below, and show it through `Icon`.
- Show line icons at 16, 20 or 24px. 12px is for solid inline markers only (Size scale, below).
- Put a label with every icon. An icon may stand alone only for a common action, and then it has a tooltip and an accessible name (Icons and labels, below).
- Colour icons with `currentColor` only, from the text they sit with. Never colour one by rarity, and never draw a line icon in more than one colour (Colour, below).
- Give every registry entry — resources, attributes, conditions, damage types and equipment slots — an icon slot with a documented fallback (Registry icon slots, below).
- Name an icon for what it means, not what it shows: `sell`, not `coin-arrow`. One icon, one meaning.

**Should**
- Use the full-colour currency art (Coins for Cinders, Diamonds for Soulstones) at display size, and each resource's line icon inline (Resource, below).
- Mark attributes in the Folio with Emblems (star polygons), each with its own point count (Registries · Emblem point counts), and with their line icon in rows.
- Set the Logo — gold-only — on `ground`, `surface` or `folio`.
- Use only the glyphs in Registries · Glyphs.

**Never**
- Use emoji, or a text character in place of an icon: ×, ★, ▸ and ▾ become `close`, `favourite` and `expand`.
- Use the asset SVGs, with their baked gold gradient, inside a component.
- Enclose an icon in a shape from the vocabulary — a circle, a square, a diamond or a hexagon — since each means something of its own (Foundations · Shape).
- Recolour the currency art or the ornaments, except the CornerOrnament mask.
- Add icons of your own to the NavRail; use `Icon` names so every item matches.

## Construction

The standard is the sidebar set's own geometry. Every new icon follows it, so the set reads as one hand.

| Property | Standard |
| --- | --- |
| **Grid** | 24 × 24 units, `viewBox="0 0 24 24"`. One unit is 1px at 24px |
| **Live area** | 20 × 20 inside 2 units of padding (2–22). A stroke may reach 1 unit from the edge, never past it |
| **Keylines** | So shapes look the same size: a square 18 × 18 (3–21), a circle 20 across, an upright rectangle 16 × 20, a wide one 20 × 16 |
| **Stroke** | 1.6 units, `stroke="currentColor"`, `fill="none"`. One weight throughout: no hairlines and no bold strokes inside an icon |
| **Caps and joins** | Round, `stroke-linecap="round"`, `stroke-linejoin="round"` |
| **Corners** | Drawn corners round with the join; a deliberate radius on a drawn rectangle is 1–2 units |
| **Fill** | None, except dots of 2 units or less (the world map's pins) and the solid inline markers, which are drawn filled so they hold at 12px |
| **Alignment** | Straight strokes and circle centres on whole units, so they sit cleanly at 24px and blur least at 20 and 16 |
| **Detail** | Gaps of at least 2 units, and no more than three or four strokes inside the outline, so the drawing survives at 16px |
| **Colour** | `currentColor` only. No gradient, no second colour, no opacity inside the drawing |
| **File** | An entry in `icons.json`, the one source of the set: both editions of `Icon` draw from it, through code `scripts/build-icons.mjs` writes (D-125). The name is the meaning, in lowercase with hyphens |

Shapes inside an icon are linework and carry no meaning of their own (Foundations · Shape · Drawings keep their own lines). An icon's outline must not be a vocabulary shape around a glyph — no "i" in a circle, no check in a square — because a mark that sits beside content follows the vocabulary.

Two sidebar icons stray from the standard and are queued in the inventory: `combat-styles` is drawn at 1.75, and `quest-journal` on a 22-unit view box, so it renders about 9% larger with a heavier line.

## Size scale

| Size | Token | For |
| --- | --- | --- |
| **24px** | `icon-lg` | The drawing's own size, for display: the PageHeader's section mark, an empty ItemSlot's slot icon, a JourneyCard's unlock, a reveal's heading |
| **20px** | `icon-md` | The default: NavRail items, Comfortable and Standard rows and controls, the TopBar's menu |
| **16px** | `icon-sm` | Inline with text, and Compact rows and controls: cost lists, Ledger and ListRow labels, `sm` Buttons, a condition's icon inside its 20px Tag, the search field, the Chronicle's controls. The smallest line icon |
| **12px** | `icon-marker` | Inline markers only: the Nobility crown beside a body-size name, a locked or favourite marker on a slot. Solid, never a line icon |

Density follows the scale: Comfortable and Standard rows and controls take 20px (`icon-comfortable`, `icon-standard`), Compact takes 16px (`icon-compact`). Standard was 18px, off the scale, and moved to 20 (D-081). The sizes are in rem, so icons grow with the reading-size setting (Foundations · Accessibility).

`Icon` takes `size` 12, 16, 20 or 24 and logs a console warning for any other size, or for a line icon under 16px.

**Optical adjustments for small sizes.** One drawing serves 24, 20 and 16; the rules above make it hold:

- **At 24px** the stroke is 1.6px: the drawing as drawn.
- **At 20px** the stroke renders at 1.33px. Nothing changes.
- **At 16px** the stroke renders at 1.07px and the gaps shrink by a third. This is why gaps stay at 2 units or more and detail stays sparse. Check every new icon at 16px before it ships; one that does not read there is simplified, not thickened.
- **Below 16px** a 1.6 stroke falls under a pixel and the drawing turns to fuzz, so no line icon is shown smaller.
- **At 12px** only markers appear, drawn for the size: filled silhouettes with no feature under 2 units (1px), and no interior lines. `nobility` is the model: a filled crown that holds at 12px.
- **Round and pointed shapes overshoot.** A circle runs to the edge of the live area and a point slightly past the keyline, so they don't look smaller than a square beside them.

## Icons and labels

**An icon accompanies a label.** The word carries the meaning; the icon helps the eye find it. A row, a Button, a Tag, a cost line and a NavRail item always show their word next to their icon.

**An icon may stand alone only for a common action** that players meet on every screen and that has no other reading: `close`, `back`, `expand` and collapse, `menu`, `search`, `filter`, `sort`, `refresh`, `copy`, `link`, and the drag grip. Every one of them:

- has an accessible name — `aria-label`, or `title` on `Icon` — that says the action, not the picture: "Close", not "Cross";
- has a tooltip with the same words, on hover and on keyboard focus;
- keeps the 24px minimum target, at 32px or more in a row (Foundations · Accessibility).

The one navigation exception is the compact NavRail, which the player collapses themselves: each icon keeps its section's name as its tooltip and accessible name, and the full rail is one click away.

Everything else keeps its word, even in a dense row: equip, unequip, sell, buy, claim, lock, unlock, favourite, whisper and invite are commitments or changes of state, and a misread icon costs the player something.

**Markers are not buttons.** A 12px marker shows a state beside content — Nobility on a name, locked or favourite on a slot. It is an image with an accessible name ("Noble", "Locked", "Favourite") and a tooltip, and the action that sets it is a labelled Button elsewhere.

**A missing icon leaves no hole.** `Icon` draws nothing for a name it doesn't know, and the label beside it is the fallback.

## Colour

- **`currentColor` only.** An icon takes the colour of the text it labels: `ink-muted` beside a muted label, `ink` beside an active one, `gilt` for the current location (the PageHeader's mark) and for the NavRail's icons, which are gold at rest as the game's sidebar has always drawn them (D-105), `arcana` beside a ready or new word, a status colour beside its status word, a damage type's colour beside its damage number, `effect-beneficial` or `effect-harmful` inside a condition's Tag.
- **No rarity-coloured icons.** Rarity is carried by the slot's edge, the name's colour and the rarity code (Registries · Rarity). An icon beside a rarity-coloured name is set in `ink-muted`, never inherited from the name.
- **No multicolour line icons.** A line icon is one colour, with no gradient and no second tone. The only multicolour images are art — the currency art, the Logo, the ornaments and the combat banners — and art is never an icon: it is shown at display size and never inline at 16px.
- **No glow** on any icon (Foundations · Ornament · Glow).

## Taxonomy

| Category | What it marks | Rule |
| --- | --- | --- |
| **Navigation** | A section of the game, in the NavRail and the PageHeader | The fifteen sidebar icons. One per section; never reused for another meaning |
| **Action** | Something the player does | A verb's icon, with its word, except the common actions above |
| **Attribute and stat** | An attribute in a row, a comparison or a tooltip | A line icon in rows; the Emblem (a star polygon) marks the attribute at display size in the Folio |
| **Resource** | A currency or material in a cost, a reward or a balance | The line icon inline; Cinders and Soulstones keep their art at display size |
| **Condition** | A beneficial or harmful effect on a combatant | Inside its Tag, framed by polarity |
| **Damage type** | A damage number's type | Beside the number, in the type's colour, with its word |
| **Equipment slot** | One of the eight slots, empty | In the empty ItemSlot, with the slot's name |
| **Entity type** | What a combatant or a name is: a player, a creature, a boss | Beside the name, with the type in words where it matters |
| **Status** | Success, warning, danger, info | Beside the status word, in an alert or a toast |
| **Social** | Whisper, guild, mention, party | Beside the name or the channel word |

### Navigation

The existing fifteen, one per section of the sidebar: Character (`overview`, `inventory`, `essences`, `combat-styles`, `achievements`, `soulstones`), World (`world-map`, `legacy-ascension`, `quest-journal`, `prophecies`), City (`guild`, `colosseum`, `cinder-bazaar`, `leaderboard`) and System (`settings`). A section's icon means that section and its subject, so `soulstones` is also the Soulstones resource and `guild` is also the guild in social contexts. A new section gets a new icon; an existing icon never takes a second meaning. `nobility`, the filled crown, is the one marker in the set.

### Action

Equip, unequip, sell, buy, claim, lock, unlock, favourite, filter, sort, refresh, close, back, expand, copy, link, whisper and invite. Collapse is `expand` turned over, and the Chronicle's taller and shorter controls are one `resize` icon turned over.

- **Favourite is a bookmark ribbon** (D-082). The game marks favourites with ★, but a star polygon is an Emblem's sign. The ribbon is the grimoire's own marker and clashes with no shape in the vocabulary. At 12px, on a slot's corner, it is a solid ribbon.
- **Expand is a chevron**, never a filled triangle: ▸ ▾ are the shape of the delta glyphs ▲ ▼ (Foundations · Shape).
- **Lock and unlock are padlocks**, closed and open. The JourneyCard's key means "the next unlock" — progression — and stays a different icon.
- **Sort uses open arrows**, not triangles, for the same reason as expand.
- Equip and unequip draw an arrow into and out of a slot outline; the slot inside the drawing is linework, not the square of the vocabulary.

### Attribute and stat

Every attribute the game defines (`AttributeType`) gets a line icon for rows, comparisons and tooltips: Ledger labels, the item comparison's deltas, an Essence's effect lines. The Emblem stays the attribute's mark at display size in the Folio, where its point count is the sign (Registries · Emblem point counts); at 16px a five- and a six-point star cannot be told apart, so rows take the line icon. An attribute's rating and its percentage share one icon: Armor and Armor Rating, Resistance and Resistance Rating.

Keep attribute icons apart from the slots they resemble: Armor is not the Chest slot's cuirass, and Block is not the Off-hand slot's shield.

### Resource

**The decision (D-083):** every resource has a line icon for inline use — cost lists, Ledgers, ListRows, Tags, reward lines and running text — at 16px in `currentColor`, before the amount's word: "Upgrade cost ····· 40 [icon] Soulstones". The full-colour art stays for display: the Coins (Cinders) and the Diamonds (Soulstones) in the CurrencyPill, shop headers and reward reveals, at the CurrencyPill's size and up. Art is never set inline at 16px, where its colours turn to a smudge and it outshouts the number. The other seven resources have no art; their line icon is their only mark.

- The line icon never replaces the resource's name: "40 Soulstones", with or without the icon, never "40 [icon]".
- `soulstones`, the sidebar's cut gem, is the Soulstones line icon: one subject, one icon.
- In a list of several costs, every line takes its icon, or none does.

### Condition

A condition is always a Tag — its icon at 16px before its name, then its time or stacks — never an icon alone. The frame says its polarity (D-084):

| Polarity | Frame | Colour | Screen readers |
| --- | --- | --- | --- |
| **Beneficial** | The Tag's rectangle with `radius-control` corners | `effect-beneficial` | "…, beneficial" |
| **Harmful** | The same rectangle with its corners cut | `effect-harmful` | "…, harmful" |

The cut corners keep the two apart in greyscale and for colour-blind players, where lichen and madder alone would not. The chamfer was considered for Buttons and turned down (D-064), so it carries no other meaning. Polarity follows the game's own rule (`ConditionResistanceRules`): the seventeen conditions that resistance and Tenacity act on are harmful, and the other eleven are beneficial. Taunt, Stealth and Cover are attention effects the bearer chose, so they frame as beneficial.

A condition that deals a damage type — Poison, Burn, Bleed — reuses that type's icon inside its frame: the icon says what it is, and the frame says it's a condition.

### Damage type

Each of the seven types has an icon, set beside the damage number in the type's colour (`currentColor`), with the type's word: "124 [icon] bleed" (Registries · Damage types). The icon gives the confusable pairs — Burn and Poison, Shadow and Magical — a shape, in addition to their word, for colour-blind players. None (untyped or true damage) takes no icon.

### Equipment slot

The eight slots of `EquipmentSlotType`, in the game's order: Head, Relic, Chest, Necklace, Legs, Ring, Main hand and Off hand. An empty ItemSlot shows its slot's icon at 24px in `ink-muted`, with the slot's name as its label and accessible name. A filled slot shows the item, never the slot icon. The game's own slot art (`assets/icons/equipment-slots`) is filled, gold-gradient and drawn on 512- and 24-unit grids, so all eight are redrawn to the standard. Its belt and tool drawings are not equipment slots and are left out.

### Entity type

What a combatant or a name is: a player character, a creature, an elite, a boss (with mini-bosses), a raid boss, an NPC or a summon — the game's `Entity` kinds and `BossRank`. The icon sits before the name in combat targets, the Creature Archive and raid rosters, and the rank is also said in words ("Boss") where it changes what the player does. A boss is never marked with a crown, which is Nobility's alone.

### Status

Success, warning, danger and info, beside their word in an alert, a toast or a notice, in the status colour (`currentColor`). Their enclosures follow the vocabulary: **success** is a check and **info** an "i", both open, with no ring; **warning** is "!" in an outlined triangle; **danger** is × in a cut-corner frame, the sign of harm the harmful condition also takes. The warning triangle is outlined and carries its "!", so it can't be read as the solid delta ▲. The `success` Tag's ✓ stays a glyph.

### Social

Whisper, guild, mention and party. `whisper` is the same icon as the whisper action; `guild` is the sidebar's guild icon. A mention's icon marks mention filters and notices; in chat text the mention stays the bold word "@you". Party is a raid or dungeon group, with "invite" as its action. Channel hues stay on the channel's tag and speaker names; a social icon takes the colour of its text.

## Registry icon slots

Every entry in the resource, attribute, condition, damage type and equipment slot registries has an **icon slot**: the icon's name, or "none yet" while it is undrawn, and a **fallback**, which is what shows until the icon exists or when it fails to load. The registries carry both columns (Registries).

| Registry | Icon slot | Fallback |
| --- | --- | --- |
| **Resources** | A line icon for each; art for Cinders and Soulstones | The resource's name alone, after the amount: "40 Soulstones". At display size, the art where it exists |
| **Attributes** | A line icon for each; an Emblem point count where assigned | The attribute's label alone. In the Folio, its Emblem |
| **Conditions** | A line icon for each, or its damage type's | The Tag alone: its name, time and polarity frame |
| **Damage types** | A line icon for each but None | The type's word after the number, in its colour: "124 bleed" |
| **Equipment slots** | A line icon for each of the eight | The slot's name as the empty slot's label: "Head" |

- **The fallback is the word that is always there.** A label is shown with every registry icon anyway, so a missing icon costs nothing but the icon.
- **Never a placeholder.** No question mark, no blank box, no generic dot, and never another entry's icon: a blank square reads as an item slot, and a borrowed icon tells a lie.
- **Add the slot with the entry.** A new resource, attribute, condition, damage type or slot is added to its registry with its icon slot filled in — a name or "none yet" — before any screen shows it.

## Inventory

What exists today, and what is needed, by category. Names are the icons' planned names in the set.

**Today:** **Set** — in `Icon`, to the standard. **Set, off-standard** — in `Icon` but straying from it. **Inline** — drawn inside a component to the standard, but not in the set yet. **Game** — in the game's own assets, off the standard (filled, gradients, other grids): to redraw. **Glyph** — a text character today: to replace. **Art** — a full-colour picture, kept as art. **Needed** — nothing exists.

**Priority:** **P1** — on screens and registry entries the game ships now: draw first. **P2** — on screens that exist, where the word carries it for now. **P3** — a review of an existing icon, or for later.

### Navigation

| Icon | Shows | Today | Priority | Note |
| --- | --- | --- | --- | --- |
| `overview` | Character Overview | Set | P2 | Its shield competes with the Off-hand slot and Block. Redraw as the character's bust |
| `inventory` | Inventory | Set | — | |
| `essences` | Essences | Set | — | |
| `combat-styles` | Combat Styles | Set, off-standard | P2 | Drawn at 1.75; a ring with a stroke, it reads as an alert at 16px. Redraw at 1.6 without the ring |
| `achievements` | Achievements | Set | — | |
| `soulstones` | Soulstones, and the Soulstones resource | Set | — | Also the resource's line icon |
| `world-map` | World Map | Set | — | |
| `legacy-ascension` | World Tower | Set | P3 | A tower; check it against `guild` at 16px |
| `quest-journal` | Quests | Set, off-standard | P2 | On a 22-unit view box: renders 9% large. Redraw on 24 |
| `prophecies` | Prophecies | Set | P3 | Its four-point sparkle is close to the banned sparkle trope (Principles · Anti-generic guardrails); review |
| `guild` | Guild, and the guild in social contexts | Set | — | |
| `colosseum` | Colosseum | Set | — | |
| `cinder-bazaar` | Cinder Bazaar | Set | — | |
| `leaderboard` | Leaderboard | Set | P1 | A crown: the crown is Nobility's alone (Registries · Nobility mark). Redraw as a podium |
| `settings` | Settings | Set | P3 | Reads as a sun at 16px; review |
| `nobility` | Active Nobility (marker) | Set | — | The model for solid 12px markers |

### Action

| Icon | Shows | Today | Priority | Note |
| --- | --- | --- | --- | --- |
| `equip` | Equip | Needed | P1 | An arrow into a slot outline |
| `unequip` | Unequip | Needed | P1 | An arrow out of a slot outline |
| `sell` | Sell | Needed | P1 | A coin leaving |
| `buy` | Buy | Needed | P1 | A coin arriving |
| `claim` | Claim a reward | Needed | P1 | A reward coming to hand |
| `lock` | Lock an item, or Locked (marker) | Set (D-113) | P1 | A closed padlock: a solid 12px marker, drawn first for the NavRail's locked destinations |
| `unlock` | Unlock an item | Needed | P1 | An open padlock |
| `favourite` | Favourite, or Favourite (marker) | Glyph ★ | P1 | A bookmark ribbon (D-082); a solid 12px marker on slots |
| `filter` | Filter | Game | P1 | A funnel. The game's `settings/filters.svg` has a baked gradient |
| `sort` | Sort | Needed | P1 | Open arrows, up and down, never triangles |
| `refresh` | Refresh | Needed | P2 | An open arc with an arrowhead |
| `close` | Close | Glyph × | P1 | Used twenty times in the game as × |
| `back` | Back | Game | P1 | The game's `Back.svg` (28 × 21) and `Left.svg` / `Right.svg` (16 × 38) are off the grid |
| `expand` | Expand, and collapse turned over | Inline | P1 | The Chronicle's chevrons; the game uses ▸ ▾ |
| `copy` | Copy | Needed | P2 | Two offset rectangles |
| `link` | Link an item into chat | Needed | P2 | A chain link |
| `whisper` | Whisper | Needed | P1 | Shared with Social |
| `invite` | Invite to a guild, party or raid | Needed | P2 | A figure with a plus |
| `menu` | Open the navigation | Inline | P1 | The TopBar's menu button |
| `search` | Search | Inline | P1 | The SearchField's glass |
| `grip` | Drag to move | Inline | P2 | The floating Chronicle's grip |
| `resize` | Make taller, and shorter turned over | Inline | P2 | The floating Chronicle's height toggle |
| `unlock-next` | The next unlock (progression) | Inline | P2 | The JourneyCard's key; not the padlock |

### Attribute and stat

| Icon | Shows | Today | Priority | Note |
| --- | --- | --- | --- | --- |
| `power` | Power | Needed | P1 | Emblem: 6 points |
| `max-health` | Max Health | Needed | P1 | |
| `armor` | Armor and Armor Rating | Needed | P1 | Emblem: 8 points. Not the Chest slot's cuirass |
| `resistance` | Resistance and Resistance Rating | Needed | P1 | Emblem: 7 points |
| `crit-chance` | Crit Chance | Needed | P1 | Emblem: 5 points |
| `crit-damage` | Crit Damage | Needed | P1 | |
| `attack-speed` | Attack Speed | Needed | P1 | |
| `dodge` | Dodge | Needed | P1 | Emblem: 9 points |
| `block` | Block | Needed | P1 | Not the Off-hand slot's shield |
| `armor-penetration` | Armor Penetration | Needed | P2 | |
| `magic-penetration` | Magic Penetration | Needed | P2 | |
| `damage-reduction` | Damage Reduction | Needed | P2 | |
| `healing-power` | Healing Power | Needed | P2 | |
| `health-regen` | Health Regen | Needed | P2 | |
| `life-steal` | Life Steal | Needed | P2 | |
| `cooldown-reduction` | Cooldown Reduction | Needed | P2 | |
| `ability-haste` | Ability Haste | Needed | P2 | Not the Haste condition |
| `tenacity` | Tenacity | Needed | P2 | |
| `restoration` | Restoration | Needed | P2 | |
| `status-resistance` | Status Resistance | Needed | P2 | |
| `cc-resistance` | Crowd Control Resistance | Needed | P2 | |
| `threat` | Threat | Needed | P2 | |

### Resource

| Icon | Shows | Today | Priority | Note |
| --- | --- | --- | --- | --- |
| `cinders` | Cinders | Art (Coins) | P1 | Line icon for cost lists; the art stays for display |
| `soulstones` | Soulstones | Set; Art (Diamonds) | — | The sidebar's gem is the line icon |
| `essence-dust` | Essence Dust | Needed | P1 | Shown as a cost when levelling an Essence |
| `glory` | Glory (Colosseum) | Game | P1 | A laurel; the game's podium wreath is filled, on a 798-unit grid |
| `experience` | Experience (EXP) | Needed | P2 | Rewards; the Meter keeps its EXP label |
| `guild-supplies` | Guild Supplies | Needed | P2 | |
| `fate-echo` | Fate Echo | Needed | P2 | |
| `sigil-fragments` | Sigil Fragments | Needed | P2 | Never a hexagon: a hexagon is the Sigil's value (Foundations · Shape) |
| `tower-tokens` | Tower Tokens (World Tower) | Needed | P2 | |
| `signets` | Signets (Nobility) | Needed | P2 | Never the crown: the crown is the Nobility mark itself (Registries · Nobility mark) |

### Condition

| Icon | Shows | Polarity | Today | Priority | Note |
| --- | --- | --- | --- | --- | --- |
| `haste` | Haste | Beneficial | Needed | P1 | |
| `empower` | Empower | Beneficial | Needed | P1 | |
| `slow` | Slow | Harmful | Needed | P1 | |
| `weaken` | Weaken | Harmful | Needed | P1 | |
| `stun` | Stun | Harmful | Needed | P1 | |
| `freeze` | Freeze | Harmful | Needed | P1 | |
| `damage-poison` | Poison | Harmful | Needed | P1 | The damage type's icon |
| `damage-burn` | Burn | Harmful | Needed | P1 | The damage type's icon |
| `damage-bleed` | Bleed | Harmful | Needed | P1 | The damage type's icon |
| `vulnerable` | Vulnerable | Harmful | Needed | P2 | |
| `wound` | Wound | Harmful | Needed | P2 | |
| `decay` | Decay | Harmful | Needed | P2 | |
| `chill` | Chill | Harmful | Needed | P2 | |
| `corrosion` | Corrosion | Harmful | Needed | P2 | |
| `doom` | Doom | Harmful | Needed | P2 | |
| `mark` | Mark | Harmful | Needed | P2 | |
| `silence` | Silence | Harmful | Needed | P2 | |
| `soaked` | Soaked | Harmful | Needed | P2 | |
| `exposed` | Exposed | Harmful | Needed | P2 | |
| `recovery` | Recovery | Beneficial | Needed | P2 | |
| `renewal` | Renewal | Beneficial | Needed | P2 | |
| `guard` | Guard | Beneficial | Needed | P2 | |
| `ward` | Ward | Beneficial | Needed | P2 | |
| `unstoppable` | Unstoppable | Beneficial | Needed | P2 | |
| `taunt` | Taunt | Beneficial | Needed | P2 | An attention effect |
| `stealth` | Stealth | Beneficial | Needed | P2 | An attention effect |
| `thorns` | Thorns | Beneficial | Needed | P2 | |
| `cover` | Cover | Beneficial | Needed | P2 | An attention effect |

### Damage type

| Icon | Shows | Today | Priority | Note |
| --- | --- | --- | --- | --- |
| `damage-physical` | Physical | Needed | P1 | |
| `damage-magical` | Magical | Needed | P1 | Keep apart from Shadow: the hues are confusable |
| `damage-bleed` | Bleed | Needed | P1 | Also the Bleed condition |
| `damage-burn` | Burn | Needed | P1 | Also the Burn condition; keep apart from Poison |
| `damage-poison` | Poison | Needed | P1 | Also the Poison condition |
| `damage-shadow` | Shadow | Needed | P1 | Not the moon of `prophecies` |
| — | None (untyped or true) | — | — | No icon: the number and its word |

### Equipment slot

| Icon | Shows | Today | Priority | Note |
| --- | --- | --- | --- | --- |
| `slot-head` | Head | Game | P1 | All eight game drawings are filled, gold-gradient, on 512- or 24-unit grids |
| `slot-relic` | Relic | Game | P1 | |
| `slot-chest` | Chest | Game | P1 | Not `armor` |
| `slot-necklace` | Necklace | Game | P1 | |
| `slot-legs` | Legs | Game | P1 | |
| `slot-ring` | Ring | Game | P1 | |
| `slot-main-hand` | Main hand | Game | P1 | One-handed and two-handed weapons |
| `slot-off-hand` | Off hand | Game | P1 | Not `block` |

### Entity type

| Icon | Shows | Today | Priority | Note |
| --- | --- | --- | --- | --- |
| `player` | A player character | Needed | P2 | |
| `creature` | A creature | Needed | P2 | |
| `elite` | An elite creature | Needed | P2 | |
| `boss` | A boss or mini-boss | Needed | P2 | Never a crown |
| `raid-boss` | A raid boss | Needed | P2 | |
| `npc` | A non-player character | Needed | P2 | |
| `summon` | A summoned ally | Needed | P2 | |

### Status

| Icon | Shows | Today | Priority | Note |
| --- | --- | --- | --- | --- |
| `success` | Success | Game | P1 | A check, open. The game's toast icons are on a 32-unit grid with fixed fills |
| `warning` | Warning | Game | P1 | "!" in an outlined triangle |
| `danger` | Danger | Game | P1 | × in a cut-corner frame |
| `info` | Info | Game | P1 | "i", open. The game's `Info.svg` is on a 17-unit grid |

### Social

| Icon | Shows | Today | Priority | Note |
| --- | --- | --- | --- | --- |
| `whisper` | Whisper | Needed | P1 | The same icon as the action |
| `guild` | Guild | Set | — | The sidebar's icon |
| `mention` | A mention | Needed | P2 | Filters and notices; in chat the mention stays "@you" |
| `party` | A party | Needed | P2 | |

**Totals.** 122 icons across the ten categories. 16 are in the set, two of them off the standard; 6 are drawn inline in components and move into the set as they are; 15 are in the game's own assets and are redrawn; 2 are glyphs today; Cinders has only its art; and 82 are new. That makes **100 to draw — 49 at P1 and 51 at P2** — plus the 6 to move (3 at P1) and 4 of the sidebar set to redraw (the Leaderboard at P1). Conditions that deal a damage type share its icon, and Soulstones and Guild share the sidebar's, so they are counted once.

## Tokens used

| Token | Role here |
| --- | --- |
| `icon-marker`, `icon-sm`, `icon-md`, `icon-lg` | The size scale |
| `icon-comfortable`, `icon-standard`, `icon-compact` | Icon size by density |
| `ink`, `ink-muted` | Icons beside ordinary and muted labels |
| `gilt` | The current location's icon only |
| `arcana` | Icons beside a ready or new word |
| `effect-beneficial`, `effect-harmful` | Icons inside a condition's Tag |
| `damage-*`, `success`, `warning`, `danger`, `info` | Icons beside their own word |
| `radius-control` | The beneficial condition frame |

The icon names are listed in Registries · Icon names.

## Do and don't

| Do | Don't |
| --- | --- |
| "Guild" with the guild icon beside it. | The guild icon alone in the rail. |
| A close button with the `close` icon, named "Close", with a tooltip. | A × character in a corner. |
| "40 [line icon] Soulstones" in a cost list. | The Diamonds art shrunk to 16px in a cost line. |
| An Epic item's name in `rarity-epic` with its icon in `ink-muted`. | The icon turned pink with the name. |
| "WEAKEN 6s" in a cut-corner Tag with its icon. | The Weaken icon alone, red. |
| The Head slot's icon and "Head" in an empty slot. | A question mark in a slot whose icon isn't drawn yet. |
| A bookmark ribbon for a favourite. | A star, which is an Emblem's sign. |
| `Icon name="essences"` in `ink-muted`. | The Essences asset SVG with its baked gradient inside a button. |

## Related components

- Icon — the game's icon set
- NavRail — the main navigation
- Button — the command button
- Tag — the status label
- ItemSlot — the item frame
- CurrencyPill — the currency amount
- Ledger — the labelled value list
- PageHeader — the information screen heading
- Emblem — the attribute sign
