# Sunken Scalehold implementation

Implemented 14 September 2026 in the primary LL game service. This supersedes the original Elementalist summon-selection design in the [analysis](../analysis/sunken-scalehold-implementation-analysis.md).

## Final creatures and essences

| Creature | Active | Passive |
| --- | --- | --- |
| Lizardfolk Brute | **Skullcrusher**, 17s: 180% Physical Damage then Stun(2), or 270% if already stunned, without refreshing Stun. | **Brutal Follow-Up**: after any active ability, the next basic attack gets +75% damage. Repeated casts refresh one pending bonus; the basic attempt consumes it. |
| Lizardfolk Elementalist | **Converging Element**, 20s: exactly one equal-probability choice of Burn(40), Chill(5), or 180% Magical Damage. | **Elemental Bond**: one Lightning Elemental at combat start, permanent until death, with no respawn. |
| Lizardfolk Scout | **Find Weakness**, 16s: 120% Physical Damage, then Exposed. | **Keen Eye**: +25 percentage points Critical Damage against Stun only. |
| Lizardfolk Shaman | **Herb Mixture**, 14s: heal the ally with least absolute current Health for 210% Power, including self and summons. | **Cleansing Herbs**: each direct allied heal application cleanses one negative effect, including applications at full Health. HoTs, regeneration and lifesteal do not trigger it. |
| Lizardfolk Warrior | **Spearhead Assault**, 15s: 150% Physical Damage, or 220% when target Health is strictly above 70% before the hit. | **Battle Rhythm**: each basic adds 2 percentage points Attack Speed, capped at eight stacks. A damaging cast consumes the stacks for +2% damage per stack across its hits, repeats and periodic damage. Pure heals/buffs do not consume stacks; Converging Element's Chill outcome does not consume them. |

Lightning Elemental defaults were confirmed by the user: 10% of owner Max Health, 30% of owner Power, normal basic-attack speed, owner Critical Chance +20 percentage points, and inherited Critical Damage. It uses ranged magical basic attacks. Summon stats snapshot the owner; existing essence summon Health/Power ascension multipliers apply. Creature and essence abilities share definitions.

Exposed is now a standard harmful condition: +10 percentage points target-side Critical Chance for ten seconds, unique/refreshing, cleansable and prevented by Ward. It uses the current shared **100%** Critical Chance cap. It neither enables otherwise-ineligible critical hits nor affects healing.

## Shared implementation decisions

- Triggers can select exactly one effect using the seeded combat RNG and snapshot effect conditions before execution. These support random outcomes and mutually exclusive health/Stun branches without generating extra damage hits.
- A pre-damage-ability event captures the Warrior bonus before active-effect dispatch, independent of equipped essence order. The cast multiplier persists on scheduled damage effects, damaging conditions and authored statuses. It does not amplify unrelated reactive passives. Reaper collection of future DoT ticks preserves their original damage multiplier.
- Pending basic-attack modifiers can refresh by effect identity while existing additive modifiers retain their behavior.
- Direct heal application has a separate event. Limited cleanse first removes the oldest harmful standard-condition instance, then harmful authored statuses in insertion order. Shared conditions remove as one effect; independent timers remove one instance. Beneficial authored statuses are preserved. Existing unlimited Cleanse behavior is unchanged.
- New trigger/effect/condition numeric identities are appended. Compiler and creature/essence clone paths preserve new fields. Fixed stack counts, thresholds and cleanse limits do not scale with ascension.

## Content and presentation

Sunken Scalehold is `region_02_area_05`, entry level 70, difficulty/global step 15, Tier 2, gated by Tower Floor 10. Its five creatures use equal 0.2 spawn weights. Meran's existing recommended combat ratings remain **200 / 242 / 293 / 354**, with **428** for the new area.

The authored data includes five creature profiles, ten abilities, one summon, one stack status, five Common essences and unbound items, five loot tables, and a five-essence collection following neighboring collection rewards. Existing awakening metadata is reused. Ordinary Tier 2 equipment and regional Sigil acquisition include the new area. The admin essence source catalog and Angular world navigation include it.

Creature image keys are `lizardfolk_brute`, `lizardfolk_elementalist`, `lizardfolk_scout`, `lizardfolk_shaman`, `lizardfolk_warrior`; the summon uses `lightning_elemental`. No portrait binaries or remote asset uploads are part of this change.

## Verification and release

Regression coverage includes both conditional-hit branches, repeated priming, seeded random exclusivity, summon death/no respawn, Exposed refresh/prevention/critical cap, Stun-only critical damage, direct versus periodic healing, self/summon targeting, limited cleansing, Warrior repeats/DoTs and stack caps, ascension cloning, catalogue coverage, fresh/existing-world seeding and preserved area ratings.

Final verification: **449 backend tests passed** through `build/run-tests.ps1`, covering the combat, essence, seed, scaling, equipment acquisition, item catalog, tooltip and Reaper suites. The frontend's full **816-test suite passed**, followed by **18 focused region/description tests** after the final glossary correction. JSON/reference validation and `git diff --check` passed.

The initial sandboxed backend restore could not read the user NuGet configuration; the approved test-runner retry used the existing configuration successfully. The first frontend invocation's PowerShell npm wrapper swallowed the include flags, so it ran the full suite; the focused rerun used `npm.cmd`. No verification remains blocked. Builds reported existing unrelated warnings.

No EF schema migration, database application, service deployment or infrastructure changes were made. The release requires the usual backend catalog/runtime and frontend updates plus normal world-content seeding. Existing unrelated working-tree changes were preserved.
