# Milestone 3 Random Essence Builds

Milestone 3 adds deterministic random sampling of legal multi-Essence character builds. The generator constructs complete production characters and intentionally performs no optimization.

## Profiles

Each run generates ten builds per profile by default:

| Profile | Essence slots | Character level | Reference gear |
| --- | ---: | ---: | --- |
| `E4_RANDOM` | 4 | 30 | `T1_Rare_Exceptional_Balanced` |
| `E5_RANDOM` | 5 | 40 | `T1_Rare_Exceptional_Balanced` |
| `E6_RANDOM` | 6 | 50 | `T1_Epic_Exceptional_Balanced` |

The reference gear preserves the Region 1 anchors: Floor 1 uses Tier 1 Rare Exceptional gear, while Floor 10 uses Tier 1 Epic Exceptional gear. The five-slot profile is an intermediate sample rather than a new progression anchor.

Use `--build-count` to change the number generated for each profile:

```powershell
dotnet run --project LL/tools/LegendsLegacy.Balance -- --seed 8471 --build-count 25
```

The accepted range is 1 through 1,000 builds per profile.

## Legality and Reproducibility

The generator reads the production Essence catalog and enforces the same fundamental construction constraints as canonical production builds:

- every selected Essence definition exists;
- an Essence definition cannot appear twice in a loadout;
- only one variant from a source monster may appear in a loadout;
- the character level unlocks at least the requested number of Essence slots.

Candidate selection is seeded and stable for identical production content, seed, and build count. Builds within a profile have unique ordered Essence signatures. The generated character is materialized through `CanonicalEquipmentBuildFactory`, so equipment, tempering, attributes, Essence tags, and Combat Rating follow production code paths.

## Report Contract

Every balance run includes the generated snapshots in `summary.json`, adds a profile table to `summary.md`, and writes full snapshots to `essence-builds.json` under both `latest` and the immutable history directory.

Each snapshot records:

- build and profile IDs;
- slot count and derived generation seed;
- Essence ID, display name, rarity, and source monster for every selection;
- reference Gear Package, character level, unlocked slots, and Combat Rating.

## Current Interpretation

With the current production content and seed `8471`, a three-build-per-profile smoke run produced displayed CR ranges of 187–187 for `E4_RANDOM`, 196–196 for `E5_RANDOM`, and 213–213 for `E6_RANDOM`. These are measured results, not acceptance constants.

The current Combat Rating algorithm does not score Essence ability performance, so a zero-width CR spread inside a profile is expected. Milestone 4 evaluates these builds with the PvE benchmark suite, and Milestone 6 uses that measured performance as optimizer fitness.

Random builds remain part of the benchmark population after scoring and optimization. They establish the unbiased baseline used by Combat Rating analysis and seed the broader evaluated population from which representative builds are selected.

## Verification Boundary

Automated coverage verifies:

- repeatability for identical seeds;
- all three slot-count profiles and their character construction;
- unique definitions and source monsters within each build;
- unique builds within each profile;
- sufficient production slot unlocks;
- JSON and Markdown report integration, including immutable historical Essence output.
