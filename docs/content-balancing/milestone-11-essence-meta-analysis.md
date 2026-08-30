# Milestone 11 Essence Meta Analysis

Milestone 11 turns the optimizer's complete unique evaluated population into explainable Essence-usage and pairing evidence. It is diagnostic only: warnings identify investigation candidates and never change Essence content automatically.

## Evidence Boundary

The primary PvE evidence is every unique candidate evaluated by the Milestone 6 optimizer across E4, E5, and E6. The small retained optimizer list and representative library are not used as the statistical population because doing so would amplify selection bias.

The production Admin Essence Simulator contributes a deterministic Tier 1 Rare Balanced 1v1 screen. Its score, adjusted score delta, classification, and battle count are attached to each Essence as complementary PvP-style evidence. Simulator evidence does not replace PvE optimizer usage or determine warnings by itself.

## Percentile Usage

Candidates are ranked independently inside each E4/E5/E6 population by aggregate PvE benchmark score and stable build ID. A candidate's percentile is its zero-based position divided by `population size - 1`; a one-build population is P100.

For each production Essence, usage at P50, P75, P90, P95, and P99 is:

```text
eligible builds containing the Essence
÷
all builds at or above that percentile
```

The report also retains overall appearances, overall usage, mean performance with and without the Essence, and their difference. A build contains each Essence at most once because the generator already enforces unique source families and unique Essence IDs.

## Pairing and Synergy

Every unordered Essence pair observed in at least three evaluated builds is analyzed. For population mean `G`, single-Essence conditional means `A` and `B`, and pair conditional mean `AB`:

```text
expected pair score = A + B - G
synergy delta       = AB - expected pair score
```

This asks whether the pair performs above or below the additive uplift associated with its members. A delta of at least `+5.00` is `Strong`; at most `-5.00` is `Weak`; other eligible pairs are `Neutral`. This is correlation-based investigation evidence, not causal proof.

Each Essence also lists up to five common partners, ordered by co-appearance count, pair performance, and stable ID.

## Warnings

The default diagnostic thresholds are:

| Warning | Rule |
| --- | --- |
| Potentially mandatory | P95 usage is at least 80% |
| Underused | Overall usage is at most 2% |
| Suspicious synergy | Eligible pair has an absolute synergy delta of at least 5 points |

Synergy warnings are capped at the twenty largest absolute deltas to keep the report actionable. All eligible pair measurements remain available in JSON even when they do not receive a warning.

Thresholds and sampling limits are serialized with the report. The one-button CLI exposes `--meta-simulator-battles <number>` for the legacy random sample and `--meta-simulator-rounds-per-matchup <number>` for a balanced all-Essence singleton round robin. In round-robin mode the requested value is repeated for every unordered matchup, not treated as a global battle count.

## Report Contract

Balance schema version 11 introduced `essenceMetaAnalysis` in `summary.json` and `essence-meta-analysis.json` under both `latest` and immutable history; the current combined pipeline uses schema version 46. The Markdown report summarizes warnings, percentile usage, simulator evidence, and the strongest measured pair deltas.

## Initial Measured Result

The default seed-`8471` run analyzed 240 unique optimizer candidates and all 80 production Essences. Its percentile cohorts contained 120 P50+, 60 P75+, 24 P90+, 12 P95+, and 3 P99+ builds. It produced 248 pair measurements with at least three observations.

No Essence crossed the 80% P95 mandatory threshold. Four crossed the 2% underuse threshold:

- Hobgoblin Essence — Brutal Charge: 1.25% overall usage;
- Lumo Wisp Essence: 1.25%;
- Treant Sapling Essence: 1.67%;
- Vampire Bat Essence: 1.67%.

Fourteen pairs crossed the five-point absolute synergy threshold. The largest positive signal was Dire Wolf + Kobold Skirmisher at `+7.11` points across eight builds. The largest negative signal was Bark Golem + Bog Mite at `-7.19` points across three builds. The small counts, especially three-observation pairs, make these investigation leads rather than tuning conclusions.

The complementary simulator ran 2,000 deterministic Tier 1 Rare Balanced 1v1 battles. Its per-Essence samples remain below the production simulator's 1,000-battle classification floor, so classifications are `InsufficientData`; raw score and adjusted-delta evidence are still retained. Increase `--meta-simulator-battles` when a stronger PvP-side classification is required.

## Balanced coverage and discrimination result

The algorithm-v20 seed-`8471` pilot used 16 rounds for every one of the 3,160 unordered singleton matchups among 80 production Essences. It ran 50,560 battles and gave every Essence exactly 1,264 appearances, so coverage is no longer the limiting factor. Nevertheless, every Essence score was exactly `0.5000`; side alternation canceled the duel outcome and left no observable Essence separation. The meta analyzer now records distinct-score count and score range, requires a range of at least `0.02` once every Essence has classification coverage, emits `SimulatorNoDiscrimination` on failure, and exposes `NoDiscrimination` rather than `Healthy` for affected rows.

This result supersedes the earlier suggestion to solve the problem by increasing only `--meta-simulator-battles`. No singleton or pair balance conclusion may be drawn from the current duel endpoint even at high battle counts. The next measurement must use a neutral discriminatory endpoint, such as common-seed fixed-hostile PvE trials or an explicitly side-bias-controlled damage/survival score, before a factorial pair audit is sized.

## Verification Boundary

Automated coverage verifies percentile denominators, conditional performance, additive pair expectations, deterministic ordering, warning thresholds, CLI validation, simulator evidence attachment, and identical `latest`/history output.
