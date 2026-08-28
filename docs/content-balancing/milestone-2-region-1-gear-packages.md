# Milestone 2 Region 1 Gear Packages

Milestone 2 adds deterministic, equipment-only Gear Packages for the first World Tower progression band.

## Region 1 Anchors

The implemented design intent is:

| Anchor | Package | Equipment |
| --- | --- | --- |
| World Tower Region 1 Floor 1 | `T1_Rare_Exceptional_Balanced` | Tier 1, Rare, Exceptional, Balanced |
| World Tower Region 1 Floor 10 | `T1_Epic_Exceptional_Balanced` | Tier 1, Epic, Exceptional, Balanced |

Each package contains the seven canonical combat equipment slots. Offensive and defensive variants are deferred until benchmark evidence shows that they represent meaningful equipment variance.

## Production Construction Path

`GearPackageFactory` resolves each definition against `CanonicalEquipmentBuildFactory`. This reuses:

- authored production recipes and item bases;
- deterministic production stat-budget rolls;
- production tempering mechanics to reach the requested rarity;
- production attribute projection;
- the current Combat Rating calculation and display conversion.

Gear Packages contain no Essences. This keeps equipment power separate from Essence profile and representative-build power; Milestone 8 combines those independent inputs when measuring power anchors.

## Report Contract

Every balance run now includes the packages in `summary.json`, summarizes them in `summary.md`, and writes their complete snapshots to `gear-packages.json` under both `latest` and the immutable history directory.

Each snapshot records:

- progression anchor and package definition;
- canonical character level;
- equipment and Combat Rating algorithm versions;
- displayed and raw Combat Rating breakdowns;
- projected combat attributes;
- every equipment item, recipe, slot, and deterministic modifier.

With the production content current when this milestone was implemented, the smoke report produced displayed Combat Ratings of 164 for Floor 1 and 171 for Floor 10. These are measured outputs, not hard-coded acceptance values, and may change when production equipment or rating rules change.

## Verification Boundary

Automated coverage verifies:

- the exact Tier 1 Rare Exceptional and Tier 1 Epic Exceptional anchor definitions;
- seven matching production items per package;
- deterministic snapshots across repeated runs;
- a higher raw Combat Rating for the Epic end package than the Rare start package;
- JSON and Markdown report integration, including immutable historical package output.

This milestone does not define the World Tower power curve by itself. Milestone 3 supplies legal 4-slot and 6-slot Essence builds for these anchors plus a 5-slot intermediate sample; Milestones 8 and 9 now measure and interpolate the complete Region 1 band.
