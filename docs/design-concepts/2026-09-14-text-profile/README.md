# LegendsLegacy — full attributes and text-only Essences

Two revised static character-overview concepts, created with the built-in image generation tool on 14 September 2026.

The character portrait, landscape art, and Essence portraits are removed. Their space now carries the full set of 19 attributes used by the actual overview, grouped as Offense, Defense, Recovery, and Utility. Essence slots show names, levels, active/passive ability names, and the Conduit source as text. Small equipment and navigation icons remain.

The established charcoal-and-gold theme and grouped left sidebar remain in both chat variants. Compact identity, Combat Rating, health, and XP occupy the top; aligned attribute rows occupy the main body; the current build sits alongside.

## Right-side chat

![Full attributes and text Essences with right chat](C:/repos/Legends-Legacy/legends-legacy/docs/design-concepts/2026-09-14-text-profile/01-right-chat.png)

## Bottom chat

![Full attributes and text Essences with bottom chat](C:/repos/Legends-Legacy/legends-legacy/docs/design-concepts/2026-09-14-text-profile/02-bottom-chat.png)

## Attribute coverage

Names, grouping and units were checked against the frontend overview and backend attribute catalog. Values are illustrative, not a live character's data.

| Group | Attributes displayed |
| --- | --- |
| Offense | Power; Attack Speed; Crit Chance; Crit Damage; Armor Penetration; Magic Penetration |
| Defense | Max Health; Physical Damage Reduction; Magical Damage Reduction; Dodge; Block; Damage Reduction |
| Recovery | Healing Power; Health Regen; Life Steal |
| Utility | Cooldown Reduction; Status Resistance; Crowd Control Resistance; Threat |

Attack Speed is a percentage bonus, mitigation values are percentages, Health Regen is HP/5s, and the overview's Threat value is an estimate from attuned Essence abilities in threat/s.

Source references: [overview groups](../../../LL/src/Presentation/ll/src/app/features/game/character/character-overview/character-overview.component.ts), [attribute labels and units](../../../LL/src/Core/Domain/Models/Attributes/AttributeCatalog.cs).

## Files and verification

Added two PNGs, this visual index, and [PROMPTS.md](PROMPTS.md). Previous concepts and generated originals remain intact.

Both images were visually checked for all 19 attribute rows, legible long labels, absence of player/Essence artwork, sidebar preservation, chat placement, three text-only Essence entries, and eight equipment icons. Both decoded successfully, and SHA-256 hashes verified that saved copies match their generated originals. Markdown whitespace checks passed. The built-in image tool was used; no CLI fallback.

These are static visual concepts with sample data and fictional chat. No game source code, configuration, migrations, or deployment changed. Application tests/builds were not run because no executable behavior changed. No required verification was blocked.

