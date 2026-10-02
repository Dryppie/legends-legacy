# Registries

The game's canonical lists — one place for every name, code, colour and mark the interface shows, so no screen types them again. Components take these lists as data; when the game adds a rarity, a channel or a currency, it is added here first.

## Rules

**Must**
- Take names, codes and colours from these registries; never retype a list inside a component or screen.
- Show the rarity code with every rarity colour (Decision D-006), and name the damage type with every damage number — in the line ("124 bleed") or in a Tag. Several rarity and damage hues are confusable for colour-blind players (Foundations · Colour · Colour-vision checks).
- Keep the rarity and damage hues as they are: players have learned them (D-016).
- Follow the Nobility mark's display rules wherever a name can carry it (Decision D-007).
- Give every entry in the resource, attribute, condition, damage type and equipment slot registries an **icon slot** — the icon's name, or "none yet" — and a **fallback**, which shows until the icon exists. The fallback is the entry's word, never a placeholder icon (Foundations · Iconography · Registry icon slots).

**Should**
- Add a new entry here, with its token and its icon slot, before any screen uses it.
- Keep each registry's order as the game's own order (rarity from Common to Legacy).
- Keep the rarity and damage colours to their registries.

**Never**
- Give an entry a second name or a second code.

## Rarity

| Rarity | Code | Token |
| --- | --- | --- |
| Common | C | `rarity-common` |
| Uncommon | UC | `rarity-uncommon` |
| Rare | R | `rarity-rare` |
| Epic | E | `rarity-epic` |
| Unique | U | `rarity-unique` |
| Legendary | L | `rarity-legendary` |
| Legacy | LG | `rarity-legacy` |

The rarity hues are the game's existing ones, kept as the palette primitives `hue-rarity-*`; each `rarity-*` token aliases the hue of the same name (Foundations · Colour). Rarity also always shows its code: ItemSlot sets it in the corner, ItemLink names the rarity in its tooltip, and rarity Tags are outlined in their colour. Screen readers hear the rarity by name — "Epic", never "E" — in ItemLink, ItemSlot and ListRow (Foundations · Accessibility). The codes are exported as `LG_RARITY_CODES`.

## Damage types

| Type | Token | Icon | Fallback | Note |
| --- | --- | --- | --- | --- |
| Physical | `damage-physical` | `damage-physical` (none yet) | The word after the number: "124 physical" | |
| Magical | `damage-magical` | `damage-magical` (none yet) | "124 magical" | Keep its icon apart from Shadow's: the hues are confusable |
| Bleed | `damage-bleed` | `damage-bleed` (none yet) | "124 bleed" | Below 4.5:1 on `surface` and `surface-raised` (3.88–4.43:1): set bleed numbers on `ground` or `folio`, or bold at 19px and up (Foundations · Colour · Contrast) |
| Burn | `damage-burn` | `damage-burn` (none yet) | "124 burn" | Keep its icon apart from Poison's |
| Poison | `damage-poison` | `damage-poison` (none yet) | "124 poison" | |
| Shadow | `damage-shadow` | `damage-shadow` (none yet) | "124 shadow" | |
| None | `damage-none` | No icon, by design | The number alone | Untyped or true damage |

The damage hues are the game's existing ones, kept as the palette primitives `hue-damage-*`; each `damage-*` token aliases the hue of the same name (Foundations · Colour). A damage icon sits before the type's word, in the type's colour (`currentColor`), and the Poison, Burn and Bleed conditions reuse it. In combat the damage type owns hue (D-017): nothing else there takes a colour of its own, and every damage number names its type — Burn and Poison, and Shadow and Magical, are confusable under red–green colour blindness.

## Chronicle channels

| Channel | Token | Aliases | Hue | Notes |
| --- | --- | --- | --- | --- |
| General | `channel-general` | `bone-100` | Neutral | |
| Trade | `channel-trade` | `amber-300` | Amber | |
| Help | `channel-help` | `bone-100` | Neutral | Neutral since D-016; the tag word tells it from General |
| Guild | `channel-guild` | `verdigris-300` | Verdigris | |
| Whispers | `channel-whisper` | `orchid-100` | Orchid | Pale orchid since D-016, clear of Epic. Whispers to and from you: "From Kaelen" / "To Kaelen" |
| Raid | `channel-raid` | `ember-300` | Ember | |
| Invites | `channel-invites` | `azure-200` | Azure | Guild, party and raid invites |
| System | `channel-system` | `bone-300` | Neutral | Set in lore italic, without an author |
| Loot | `channel-loot` | `bone-300` | Neutral | Neutral since D-016: the item links carry the rarity hues. What you found; replaces the separate Loot History box (Decision D-005) |

In chat the channel owns hue (D-017), and only on its tag and its speaker names — never the message text, which stays `ink`, nor the active tab, the unread count or a mention. Each hue family holds one channel; General, Help, System and Loot are neutral and are told apart by the tag word, which every line carries in the All feed. Item links keep their rarity colour inside their brackets. Each channel aliases a palette primitive directly (Foundations · Colour), so recolouring a role leaves chat as it is. An `all` channel shows the merged feed of visible channels.

## Effects and conditions

The game's standard conditions (`StandardConditionType`), in its order. Polarity follows the game's own rule (`ConditionResistanceRules`): the seventeen conditions that resistance and Tenacity act on are harmful, and the other eleven are beneficial.

| Condition | Polarity | Token | Icon | Fallback | Note |
| --- | --- | --- | --- | --- | --- |
| Haste | Beneficial | `effect-beneficial` | `haste` (none yet) | The Tag: its name, time and rounded frame |  |
| Slow | Harmful | `effect-harmful` | `slow` (none yet) | The Tag: its name, time and cut-corner frame |  |
| Empower | Beneficial | `effect-beneficial` | `empower` (none yet) | The Tag: its name, time and rounded frame |  |
| Weaken | Harmful | `effect-harmful` | `weaken` (none yet) | The Tag: its name, time and cut-corner frame |  |
| Vulnerable | Harmful | `effect-harmful` | `vulnerable` (none yet) | The Tag: its name, time and cut-corner frame |  |
| Wound | Harmful | `effect-harmful` | `wound` (none yet) | The Tag: its name, time and cut-corner frame |  |
| Recovery | Beneficial | `effect-beneficial` | `recovery` (none yet) | The Tag: its name, time and rounded frame |  |
| Decay | Harmful | `effect-harmful` | `decay` (none yet) | The Tag: its name, time and cut-corner frame |  |
| Renewal | Beneficial | `effect-beneficial` | `renewal` (none yet) | The Tag: its name, time and rounded frame |  |
| Guard | Beneficial | `effect-beneficial` | `guard` (none yet) | The Tag: its name, time and rounded frame |  |
| Ward | Beneficial | `effect-beneficial` | `ward` (none yet) | The Tag: its name, time and rounded frame |  |
| Unstoppable | Beneficial | `effect-beneficial` | `unstoppable` (none yet) | The Tag: its name, time and rounded frame |  |
| Poison | Harmful | `effect-harmful` | `damage-poison` (none yet) | The Tag: its name, time and cut-corner frame | The damage type's icon |
| Burn | Harmful | `effect-harmful` | `damage-burn` (none yet) | The Tag: its name, time and cut-corner frame | The damage type's icon |
| Bleed | Harmful | `effect-harmful` | `damage-bleed` (none yet) | The Tag: its name, time and cut-corner frame | The damage type's icon |
| Stun | Harmful | `effect-harmful` | `stun` (none yet) | The Tag: its name, time and cut-corner frame |  |
| Taunt | Beneficial | `effect-beneficial` | `taunt` (none yet) | The Tag: its name, time and rounded frame | An attention effect the bearer chose |
| Stealth | Beneficial | `effect-beneficial` | `stealth` (none yet) | The Tag: its name, time and rounded frame | An attention effect the bearer chose |
| Chill | Harmful | `effect-harmful` | `chill` (none yet) | The Tag: its name, time and cut-corner frame |  |
| Freeze | Harmful | `effect-harmful` | `freeze` (none yet) | The Tag: its name, time and cut-corner frame |  |
| Corrosion | Harmful | `effect-harmful` | `corrosion` (none yet) | The Tag: its name, time and cut-corner frame |  |
| Doom | Harmful | `effect-harmful` | `doom` (none yet) | The Tag: its name, time and cut-corner frame |  |
| Thorns | Beneficial | `effect-beneficial` | `thorns` (none yet) | The Tag: its name, time and rounded frame |  |
| Mark | Harmful | `effect-harmful` | `mark` (none yet) | The Tag: its name, time and cut-corner frame |  |
| Cover | Beneficial | `effect-beneficial` | `cover` (none yet) | The Tag: its name, time and rounded frame | An attention effect the bearer chose |
| Silence | Harmful | `effect-harmful` | `silence` (none yet) | The Tag: its name, time and cut-corner frame |  |
| Soaked | Harmful | `effect-harmful` | `soaked` (none yet) | The Tag: its name, time and cut-corner frame |  |
| Exposed | Harmful | `effect-harmful` | `exposed` (none yet) | The Tag: its name, time and cut-corner frame |  |

An effect's polarity is what it does to the one it is on, not the sign of its numbers: Slow lowers a number and is harmful; a shorter cooldown is better. Effects on the player show as `beneficial` and `harmful` Tags with their name and time left ("Weaken 6s", the time in the Tag's `value`), and the frame says the polarity too: a beneficial Tag keeps its rounded corners, and a harmful one has its corners cut (Foundations · Iconography · Condition). In combat they are the only hue beside the damage types (D-024). Add each new condition here with its polarity and icon slot before any screen shows it.

## Resources

Currencies and the materials spent like them. Each has a line icon for inline use and a fallback; Cinders and Soulstones also have full-colour art for display (Foundations · Iconography · Resource).

| Resource | What it is | Art | Line icon | Fallback | Shown in |
| --- | --- | --- | --- | --- | --- |
| Cinders | The soft currency | `Currency/Cinders.webp` | `cinders` (none yet) | The word: "1,250 Cinders"; the art at display size | CurrencyPill in the TopBar and shop headers; cost lists |
| Soulstones | The premium currency | `Currency/Soulstones.webp` | `soulstones`, the sidebar's gem | "40 Soulstones"; the art at display size | CurrencyPill in the TopBar and shop headers; cost lists |
| Experience | EXP, from combat and quests | — | `experience` (none yet) | "240 EXP" | Rewards; the XP Meter keeps its label |
| Essence Dust | From shattering spare Essences | — | `essence-dust` (none yet) | "120 Essence Dust" | The cost of levelling an Essence |
| Glory | The Colosseum's currency | — | `glory` (none yet) | "300 Glory" | Colosseum rewards and its shop |
| Guild Supplies | The guild's resource | — | `guild-supplies` (none yet) | "80 Guild Supplies" | The guild |
| Fate Echo | A guild shop reward | — | `fate-echo` (none yet) | "1 Fate Echo" | The guild shop |
| Sigil Fragments | A material, given as a reward | — | `sigil-fragments` (none yet) | "6 Sigil Fragments" | Rewards and the guild shop |
| Tower Tokens | The World Tower's currency | — | `tower-tokens` (none yet) | "12 Tower Tokens" | The World Tower |
| Signets | Redeemed for Nobility, and traded between players | — | `signets` (none yet) | "3 Signets" | Nobility; Signet trading, where Signets held by an open trade are Reserved and the rest unreserved (Standards · States · Ownership and use) |

The line icon goes before the resource's word, at 16px in `currentColor`, and never replaces it. The art is for display size only — the CurrencyPill, shop headers, reward reveals — and is never shrunk into a cost line.

## Attributes

Every attribute the game defines (`AttributeType`), by the label the interface shows. A rating and its percentage share one entry and one icon. The Emblem marks an attribute at display size in the Folio; the line icon marks it in rows (Foundations · Iconography · Attribute and stat).

| Attribute | `AttributeType` | Icon | Emblem points | Fallback |
| --- | --- | --- | --- | --- |
| Power | `Power` | `power` (none yet) | 6 | The label alone; the Emblem in the Folio |
| Max Health | `MaxHealth` | `max-health` (none yet) | — | The label alone |
| Armor | `Armor`, `ArmorRating` | `armor` (none yet) | 8 | The label alone; the Emblem in the Folio |
| Resistance | `Resistance`, `ResistanceRating` | `resistance` (none yet) | 7 | The label alone; the Emblem in the Folio |
| Crit Chance | `CritChance` | `crit-chance` (none yet) | 5 | The label alone; the Emblem in the Folio |
| Crit Damage | `CritDamage` | `crit-damage` (none yet) | — | The label alone |
| Armor Penetration | `ArmorPenetration` | `armor-penetration` (none yet) | — | The label alone |
| Magic Penetration | `MagicPenetration` | `magic-penetration` (none yet) | — | The label alone |
| Dodge | `DodgeChance` | `dodge` (none yet) | 9 | The label alone; the Emblem in the Folio |
| Block | `BlockChance` | `block` (none yet) | — | The label alone |
| Damage Reduction | `DamageReduction` | `damage-reduction` (none yet) | — | The label alone |
| Healing Power | `HealingPowerPercent` | `healing-power` (none yet) | — | The label alone |
| Health Regen | `HealthRegeneration` | `health-regen` (none yet) | — | The label alone |
| Life Steal | `LifeSteal` | `life-steal` (none yet) | — | The label alone |
| Cooldown Reduction | `Cooldown` | `cooldown-reduction` (none yet) | — | The label alone |
| Status Resistance | `StatusResistance` | `status-resistance` (none yet) | — | The label alone |
| Crowd Control Resistance | `CrowdControlResistance` | `cc-resistance` (none yet) | — | The label alone |
| Threat | `Threat` | `threat` (none yet) | — | The label alone |
| Attack Speed | `AttackSpeed` | `attack-speed` (none yet) | — | The label alone |
| Ability Haste | `AbilityHaste` | `ability-haste` (none yet) | — | The label alone |
| Tenacity | `Tenacity` | `tenacity` (none yet) | — | The label alone |
| Restoration | `Restoration` | `restoration` (none yet) | — | The label alone |

## Equipment slots

The eight slots of `EquipmentSlotType`, in the game's order. An empty slot shows its icon at 24px in `ink-muted`, with the slot's name as its label and accessible name; a filled slot shows its item.

| Slot | `EquipmentSlotType` | Takes | Icon | Fallback |
| --- | --- | --- | --- | --- |
| Head | `Head` | Head pieces | `slot-head` (none yet) | "Head" as the empty slot's label |
| Relic | `Relic` | Relics | `slot-relic` (none yet) | "Relic" as the empty slot's label |
| Chest | `Chest` | Chest pieces | `slot-chest` (none yet) | "Chest" as the empty slot's label |
| Necklace | `Necklace` | Necklaces | `slot-necklace` (none yet) | "Necklace" as the empty slot's label |
| Legs | `Legs` | Leg pieces | `slot-legs` (none yet) | "Legs" as the empty slot's label |
| Ring | `Ring` | Rings | `slot-ring` (none yet) | "Ring" as the empty slot's label |
| Main hand | `MainHand` | One-handed and two-handed weapons | `slot-main-hand` (none yet) | "Main hand" as the empty slot's label |
| Off hand | `OffHand` | Off-hand items | `slot-off-hand` (none yet) | "Off hand" as the empty slot's label |

The game's slot art (`assets/icons/equipment-slots`) is filled and gold-gradient, on mixed grids, and is redrawn to the standard; its belt and tool drawings are not slots.

## Meters

| Meter | Tone | Token | Label |
| --- | --- | --- | --- |
| Health | `hp` | `meter-hp` | HP |
| Stamina or mana | `sp` | `meter-sp` | SP |
| Experience and level progress | `xp` | `meter-xp` (the same brass as `gilt`) | EXP |

Every meter sits on `meter-track` and prints its value beside the bar.

## Emblem point counts

| Attribute | Points |
| --- | --- |
| Crit Chance | 5 |
| Power | 6 |
| Resistance | 7 |
| Armor | 8 |
| Dodge | 9 |

Emblems take 5 to 12 points. Give each attribute, school or region its own count so the emblem becomes its sign, and add the count here when you assign it.

## Glyphs

| Glyph | Meaning |
| --- | --- |
| ▲ ▼ | A delta: the number rose or fell — always with its sign (+, −) and coloured by whether that helps the player (D-023) |
| ± | A delta: unchanged, as ±0 — no glyph beside it, since a hollow diamond is a milestone still to come (D-067) |
| ✓ | Success: won, completed — with its word, since `success` has no hue (D-016) |
| [ ] | An item named in text: ItemLink wraps the name, so an item never passes for a speaker or a channel |
| ↵ | Enter, on key caps |
| – | The list marker: an en dash in `ink-muted`, hung before each item. The ✦ is retired: at list size it reads as a diamond (Foundations · Shape) |
| − | Minus: a negative number or a fall, U+2212, never a hyphen (Foundations · Numerals) |
| – | A range: 12–18, an en dash with no spaces, always between two numbers |
| × | A multiplier or a stack: ×1.5, ×3 |
| / | A value out of a maximum: 3,120 / 4,150, spaced |
| — | Unknown, or does not apply, in any value cell; zero is 0 |

No emoji, and no ◆ or ◇: a diamond is a shape with its own meaning (Foundations · Shape). The Nobility mark is the crown icon, not a glyph. Three characters the game uses today are icons in Grimoire, not glyphs: × as close (`close`; × stays the multiplier), ★ as favourite (`favourite`, a bookmark ribbon) and ▸ ▾ as disclosure (`expand`, a chevron) (Foundations · Iconography).

## Nobility mark

The crown beside a name says that player's Nobility is active. It follows these display rules everywhere (Decision D-007). The crown replaced the ◆, which was the same shape and colour as the NavRail's current-location diamond (D-066).

- **Where:** before the character's name, wherever the name is shown as a character tag, and in the Combat Profile on the Overview.
- **When:** only while that character's Nobility is active — its expiry is after the server's time — and only if that player has **Display Nobility** switched on in Settings › Nobility. It disappears the moment Nobility expires, without a reload.
- **How it looks:** the `nobility` icon — a filled crown — in `gilt`, the game's one heraldic mark and part of gilt's brand job (D-015). It is a step smaller than the name it marks and centred on it: 12px (`icon-marker`) beside body-size names, 16px (`icon-sm`) beside a screen title. It is the one solid marker drawn for 12px (Foundations · Iconography · Size scale).
- **For assistive technology:** it is an image named "Noble", with the tooltip "Active Nobility".
- **Inside Nobility's own panel,** the status badge ("Noble" or "Inactive") carries the crown, hidden from assistive technology since the word says it. Perk lists use the ordinary list marker, the en dash.
- **Never:** show it for expired Nobility or for a player who switched Display Nobility off; colour it anything but `gilt`; use the crown for anything but Nobility; mark Nobility with a diamond.

## Icon names

`overview`, `inventory`, `essences`, `combat-styles`, `achievements`, `soulstones`, `world-map`, `legacy-ascension`, `quest-journal`, `prophecies`, `guild`, `colosseum`, `cinder-bazaar`, `leaderboard`, `settings` — the game's sidebar set. Two more are solid markers, not sidebar icons: `nobility`, the filled crown of the Nobility mark, and `lock`, the Locked marker (D-113). All seventeen are in `icons.json`, the one source of the set (D-125); `LG_ICON_NAMES` lists them.

Every other name in the registries above is planned and not drawn yet ("none yet"); Foundations · Iconography · Inventory lists them all with their priority. A name is the icon's meaning, in lowercase with hyphens; slot icons take `slot-` and damage icons `damage-`, matching their tokens. Until a name is drawn, `Icon` draws nothing for it and the entry's fallback shows.

## Legacy token map

Moved to Foundations · Colour · Migration map, beside the colour tiers it maps onto. It still covers every `--ll-*` token, colour and non-colour.

## Tokens used

`rarity-*`, `damage-*`, `channel-*`, `effect-beneficial`, `effect-harmful`, `meter-hp`, `meter-sp`, `meter-xp`, `meter-track`, `gilt` — each listed in its registry above.

## Do and don't

| Do | Don't |
| --- | --- |
| "Ember Fang" in `rarity-epic` with the code E. | "Ember Fang" in pink with no code. |
| Take the channel list from the registry when building a channel picker. | Hard-code "General, Trade, Help" in the picker. |
| Show the crown before Aldric's name while his Nobility is active and displayed. | Keep showing it after it expired, or when he hid it. |

## Related components

- ItemSlot — the item frame
- ItemLink — an item named in text
- Tag — the status label
- Chronicle — chat and the game log
- CurrencyPill — the currency amount
- Meter — the progress bar
- Emblem — the attribute sign
- Icon — the game's icon set
