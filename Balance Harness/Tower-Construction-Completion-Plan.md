# Fresh-construction completion experiment

Status: **complete** — see the [implementation and result](Tower-Construction-Completion-Review.md). The [original plan](../TestResults/balance/tower-construction-completion-20260913/implementation-plan.md) and [executable protocol](../TestResults/balance/tower-construction-completion-20260913/protocol.json) remain sealed. The specification below records the completed v7 change and its precombat requirements; it is not pending work. The later v8 and `independent-compatible-defense-v9` experiments are also complete. Use the [current handoff](Tower-Coverage-Replication-Plan.md) for active status and the latest seed exclusions.

## Implemented change

V7 added opt-in `independent-completion-v7` with exactly `["collective-joint", "completion-joint"]`. Keep v6's `collective-joint` comparator byte/behavior compatible. In the new arm's guided fresh construction, remove only the `random.Next(2) == 0` skip so each character attempts the existing compatible-core insertion. Preserve the preceding coverage phase, shuffled character order, legal-core enumeration/selection, fixed budget, inventory/family checks, weighted fill, final ordering, refinement operators, ranking, beam/exploration and schedules. Keep the one-in-eight uniform route and empty-feature behavior intact. If no compatible core exists, retain the current ordinary fill; do not overwrite earlier coverage or repair by changing unrelated assignments.

This rule follows a general constructor operation: use an available structural combination during guided construction. It specifies no Essence ID, saved copy count or winning recipe template. Do not increase combat budgets, core weights or mutation frequency as part of this change. Different arm names keep separate deterministic RNG streams; the comparison assesses search policies and is not an identical-parent causal ablation.

The experiment tested whether fewer unnecessarily skipped compatible insertions would yield more useful fresh starting parties. The diagnosis found only 5.23% blocked characters but 1,052 skipped legal insertions on incomplete characters. It also found three fully core-complete fresh parties with no wins, so structural completion alone did not establish efficacy. The subsequent v7 comparison failed its reliability gate.

## Precombat verification requirements — completed

1. Preserve all five sealed prior experiments and this diagnostic package/review; use a new output directory. Verify current source/content/execution, both catalogs and all six controls.
2. Verify unchanged v6 behavior with deterministic tests and separate zero-combat reconstruction of its six saved arms. No historical measurement may enter the fresh generator.
3. Verify new construction determinism, complete legal parties, missing/empty features, uniform reachability, scarce ownership, family conflicts and immutable input boundaries across the existing floor/slot budgets. Verify restricted provenance, reference removal/reordering invariance, and compact reconstruction/resume parity. Trace that every eligible guided character receives an insertion attempt; do not assert that all parties must win or all characters must obtain a core.
4. Generate fresh seeds excluding every array of the [then-current construction-diagnosis ledger](../TestResults/balance/tower-construction-diagnosis-20260913/seed-ledger.json), including 256 constructor-only seeds and all previous unused reservations. Freeze a complete executable protocol and producing hashes before combat.

## Frozen allocation and decision

Keep the same bounded policy comparison: two methods, three fresh generation restarts per method, 96 evaluated complete parties per arm, eight shared discovery seeds and at most 2,048 proposals per arm. Freeze the top two recipes per arm by discovery ranking plus all six saved controls before 256 disjoint validation seeds. Preserve exact duplicates' source associations; retain all distinct recipes and all observed ceiling breaches.

| Phase | Maximum fights |
| --- | ---: |
| Discovery: 2 × 3 × 96 × 8 | 4,608 |
| Validation: at most 18 × 256 | 4,608 |
| Fixed diagnostics: 4 old parity + 3 new primaries + original anchor | 8 |
| **Total, zero retry reserve** | **9,224** |

Caps: 600 seconds for execution, 1 GiB for the package, 512 MiB per campaign, no automatic resume or optional extension. These limits were frozen in the linked executable protocol and the experiment completed within them. They allocate no follow-up fights.

Use the unchanged exploratory reliability gate: at least two of three discovery-selected new primaries need adjusted rate lower ≥10%, paired lower improvement over their same-restart v6 primary >0, and paired lower difference from original anchor `team-1abe76ca1891d97a91d484f0a3662048` ≥−10 pp. Other controls and secondary finalists cannot replace the anchor or primary after outcomes. Joint nominal alpha .05 splits .025 across the complete final rate family and .025 across six paired comparisons, with component alpha .025/12 for discordance bounds. Keep approximate coverage, no historical pooling and no lifetime repeated-study guarantee explicit.

The ordinary 10–50% evaluator remains separate. Any observed >50% is a ceiling breach; uncertainty must not turn it into acceptance. Preserve the previous 131/256 breach, separate 479/1,000 inconclusive confirmation and all original scopes. A new small comparison is not a complete Tower-family acceptance, near-optimality claim or resolution of practical acquisition.

Keep Kharad **3.04881408 / 3.85370128**, the exact floor-5 budget and all six controls fixed. No boss retuning, default/catalog promotion or floors 6–11 follow automatically from this experiment. Save every evaluated/final recipe, ancestry, fresh outcomes, durable accounting, reconstructions and fixed diagnostic parity reports.

## Completed result and next boundary

**Construction completion pilot — 13 September 2026:** [opt-in `independent-completion-v7`](Tower-Construction-Completion-Review.md) completed 9,224 fights in 247.82 seconds of measured phases, with 172 passing tests. Reliability is **Fail (0/3; 2 required)**; 0 of 12 generated finalists recorded a held-out win. Six controls measured 19.92–53.12%; the 18-cell assessment is **Fail** (strongest joint upper 62.78%). Every guided character received an insertion attempt in 128 exact constructor checks. Recipes, ancestry and evidence are saved. Historical 131/256 breach and 479/1,000 Inconclusive confirmation remain separate; no pooled acceptance, retuning, catalog/default promotion or new floors.

The [final receipt](../TestResults/balance/tower-construction-completion-20260913/final-verification.json) seals v7. Its [ledger](../TestResults/balance/tower-construction-completion-20260913/seed-ledger.json) is historical. The later [completion diagnosis](Tower-Completion-Diagnosis-Review.md) and [v8 attribute-defense pilot](Tower-Attribute-Defense-Review.md) are also complete; v8 retained v6's insertion behavior and failed at 0/3. Future preparation must exclude every array of the latest pilot ledger (**472,194 distinct seeds**) linked from the [current handoff](Tower-Coverage-Replication-Plan.md#seed-exclusions-and-evidence-preservation). The later [v9](Tower-Protection-Compatibility-Review.md) and [v10](Tower-Stagger-Reservation-Review.md) pilots are complete. The [zero-combat v10 diagnosis](Tower-Stagger-Reservation-Diagnosis-Review.md) is complete. The later [elite-loadout assessment](Tower-Loadout-Diversity-Assessment-Review.md) selects one [parent-diversity proposal](Tower-Loadout-Diversity-Plan.md) for implementation planning. The [v11 implementation](Tower-Loadout-Diversity-Implementation-Review.md), its [separately frozen pilot](Tower-Loadout-Diversity-Review.md) and its [completed zero-combat v11 diagnosis](Tower-Loadout-Diversity-Diagnosis-Review.md) are complete; no further combat campaign is allocated. No new combat campaign is allocated.
