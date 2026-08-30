# Assisted Encounter Calibration

Balance schema 24 adds an opt-in, non-mutating assisted layer to the existing bounded encounter calibrator. Enable it with `--assisted-calibration`; `--assisted-calibration-simulations <number>` controls trials per sensitivity and holdout evaluation, while zero inherits `--tower-simulations`.

The original shared health/offense binary search remains unchanged as a diagnostic baseline and continues to feed existing downstream reports. Assisted proposals are separate review evidence and do not replace that baseline or write content.

## Evidence gate

The first implementation deliberately recognizes only mappings supported by the current physical telemetry:

| Dominant observed failure mode | Temporary parameter group |
| --- | --- |
| `PrimaryTargetCollapse` | Guardian offense |
| `PartyAttrition` | Guardian offense |
| `BossSustainDominance` | Guardian regeneration |

The dominant mode must represent at least 60% of non-success observed failure modes. Mixed observations and `AddPressure`, `PriorityObjectiveUnmet`, `ControlWindowUnmet`, `CleanseDemandUnmet`, or `Other` return `Review`; those mechanics do not identify an interchangeable scalar knob. Too-easy results also return `Review`, because successful trials alone do not establish which part of encounter identity should become harder.

## Probe and holdout flow

For a supported hard encounter, the calibrator:

1. evaluates adjustment factors 0.85 and 0.70 for exactly one parameter group using the original run seed;
2. requires at least a five-percentage-point improvement in absolute clear-rate error;
3. derives a separate deterministic holdout seed;
4. evaluates both authored factor 1.0 and the selected candidate on that same holdout seed;
5. accepts a proposal only if the candidate materially improves the paired holdout and lands in the target clear-rate window;
6. reports a bounded factor range around the selected grid cell and marks it as requiring human approval.

Every other Guardian scaling factor remains 1.0 during a single-group probe. The evaluator clones the floor in memory and can temporarily vary health, offense, defense, resistance, or regeneration, but the current evidence gate selects only offense or regeneration.

## Report contract and safety

Each floor records the assisted verdict, evidence disposition, dominant observed mode/share, sensitivity and holdout trace, identity check, bounded proposals, and a human-readable recommendation. Verdicts are `Disabled`, `KeepAuthored`, `Proposal`, or `Review`.

No production JSON, database state, player state, authored `RequiredSlots`, API, or UI is changed. There is no migration or deployment implication. Later Phase 5 work will add author-owned identity bounds, duration and party-family constraints, discrete mechanic grids, and carefully bounded multi-group interaction handling.
