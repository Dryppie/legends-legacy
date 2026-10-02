# Page Archetypes

Whole-screen templates. An archetype fixes a screen's shell configuration, what fills the stage, and the order its content comes in. Archetypes are named `Archetype<Name>`. The game's Character Overview is the worked example of ArchetypeInformation.

## Rules

**Must**
- Name an archetype `Archetype<Name>`.
- Say which stage content it uses — a Page or a Stage — and the order of its content.
- Keep to the Shell: one Folio at most, the chat layout from the player's setting, and one inspector — the Folio on a stage screen, or the list and inspector in a Page's content (D-052).
- Say which density each region uses (Foundations · Space & Density), and which track layout (Foundations · Layout).

**Should**
- Start every new screen from the closest archetype.
- Add a new archetype when a screen fits none of them, with that screen as its worked example.

**Never**
- Let a screen drop information to fit an archetype's look — information density is preferred over decorative layouts (Decision D-008).

## ArchetypeInformation

**For:** information screens — Overview, Settings, Leaderboard, Guild.
**Stage content:** a Page instead of a Stage: a PageHeader, then content on the frame's backdrop (D-101). Content art appears only inside a Banner.
**Order:** lead with what the player needs to decide next (the JourneyCard), then who they are (one Banner with the headline figures), then the detail (Panels and Ledgers). Show every value the game knows, label it plainly, and explain it on hover or focus — atmosphere never replaces information.
**Density:** Standard; the Folio, when there is one, is Comfortable.
**Layout:** a main and side column (`lg-aside`). The main column holds the profile and then the Ledger grid of attributes; the side column runs beside all of it — the Combat Style Panel first, on one row, then the Essence Loadout (D-121, D-123) — so nothing waits below the loadout (D-103). A long list in the side column takes its compact form — the loadout's slots are compact LoadoutSlots; the Banner's figures beside its identity at Wide. In the game's own frame, while screens migrate, the Page takes `flow` (D-097).
**Worked example:** the game's Character Overview (`src/app/features/game/character/character-overview-grimoire/`).
**Status:** Draft

## ArchetypeArchive

**For:** screens that browse a collection over a scene — the Creature Archive.
**Stage content:** a Stage with art; tabs and filters across the top (primary and secondary TabStrips), an EntryList over the scene, and a lore Panel.
**Order:** what you are browsing, how it is filtered, the list, then the selected thing's stats in the Folio (`align="start"`).
**Density:** Standard for the tabs and the EntryList; the Folio is Comfortable.
**Layout:** a Stage, with the Folio as the inspector.
**Status:** Draft

## Workbench screens

Dense screens where the player works across many things at once — the Cinder Bazaar, the guild's member list — have no archetype yet. "One screen, one subject" does not apply to them; *Decisions first* orders their actions and *Dense, not crowded* lays out the rest (Principles, D-010). The first one designed becomes their archetype.

## Densities for future archetypes

Each future archetype has a default density, set now so the first screens of each kind agree:

| Archetype | Default | Regions that differ |
| --- | --- | --- |
| ArchetypeWorkbench: the Cinder Bazaar, inventory, crafting | Compact: tables, lists and order books | The order form and filters are Standard; the inspector and dialogs are Comfortable |
| ArchetypeRanking: the Leaderboard, the Colosseum ladder | Compact | The player's own standing, above the table, is Standard |
| ArchetypeRoster: guild members, a party | Compact | The selected member's detail is Comfortable, in the inspector |
| ArchetypeDetail: one character, creature or item on its own screen | Comfortable | Long stat tables inside it are Standard |
| ArchetypeCombat: dungeon and Colosseum fights | Standard: actions, HP and SP | The combat log is Compact |

The player's Compact lists setting turns every Compact region Standard (Foundations · Space & Density).

## Layouts for future archetypes

Each future archetype also has a default track layout (Foundations · Layout):

| Archetype | Layout |
| --- | --- |
| ArchetypeWorkbench | A list and inspector; order books as data tables |
| ArchetypeRanking | A data table, with the player's own standing above it |
| ArchetypeRoster | A list and inspector |
| ArchetypeDetail | Ledger grids, and a split comparison against what the player has equipped |
| ArchetypeCombat | A Stage; the combat log lives in the Chronicle |

## Tokens used

None of their own: each archetype uses the Shell and the parts it names.

## Do and don't

| Do | Don't |
| --- | --- |
| Settings as an ArchetypeInformation screen: a Page, a PageHeader, Panels. | Settings over a painted Stage. |
| A new archive — Regions, Prophecies — as ArchetypeArchive. | A one-off layout for each archive. |
| Every Combat Attribute on the Overview, each explained on hover and focus. | A decorative layout that hides half of them (Decision D-008). |

## Related components

- Page — the information screen frame
- Stage — the scene backdrop
- PageHeader — the information screen heading
- JourneyCard — the next-step guide
- Banner — the headline block
- Panel — the content box
- Ledger — the labelled value list
- TabStrip — the tabs
- EntryList — the browsable name list
- Folio — the detail panel
