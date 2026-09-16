# Controlled retention and scheduling result

16 September 2026. Target: offline `LL/tools/BalanceHarness`.

**The controlled fixture identifies an operator-scheduling defect.** With nine fixed eligible parents, each parent receives only one of the three block operators. A useful weaker team can survive diversity retention and be selected repeatedly without receiving the operator needed to improve it. With eight parents, the same prescribed landscape demonstrates the intended retention benefit across all three frozen label mappings.

The fixture and behavior-preserving extraction are complete: **20/20 current tests passed**, one baseline parity execution passed, and complete before/after search reports match for six arms and 144 synthetic evaluations per report. Passing tests confirm the nine-parent limitation; they do not certify search quality. No scheduler fix, combat, fresh balance values or historical experiment rerun occurred. The previous failed **52/53** gate remains failed and sealed.

## What the controlled evidence shows

The [frozen protocol](../TestResults/balance/tower-controlled-retention-20260916/protocol.md) supplies ten similar A teams scoring 6 and a distant non-anchor B scoring 5. Each team has one owner and four Essences from 20 distinct families. Two targets score 9. Every A needs four replacements to reach either target, whereas B can reach a target using the production two/three-Essence replacement operator. Ability order remains ordinal.

Both retention arms receive the same initial measurements and 30 mutation opportunities. The diverse arm uses production four-elite/four-diverse retention; the ablation keeps only four elites. Both use the same production block planner and proposal operator. The scored archive stays fixed, and sampler choices are prescribed before execution. Identity, reverse and affine label mappings all give the following results:

| Eligible parents in diverse arm | Diverse final score | Elite-only final score | Opportunities received by B |
| --- | ---: | ---: | --- |
| 8: supplied anchor already retained | 9 | 6 | All three operators; strict improvement at mutation 12 |
| 9: one additional supplied anchor | 6 | 6 | Character-block only at mutations 4, 13 and 22 |

The elite-only arm has four or five eligible parents because retention is the ablated feature. Equal budgets mean the same 30 charged opportunities, including rejected and duplicate proposals, rather than identical selected parents.

The [independent audit](../TestResults/balance/tower-controlled-retention-20260916/audit.json) checks all **12 complete traces / 360 opportunities**: initial recipes, measurements and provenance; retained and eligible populations; parent/donor identities and scores; operator ordinals and random choices; legal canonical children; rejection/duplicate accounting; and strict improvement over contributing parents. B is independently introduced as `fresh-legal`, never supplied as an anchor. Its successful child improves on its sole parent and the initial best.

These are conditional component results. Child outcomes do not feed back into retention. Fresh construction is omitted, and character/donor blocks reject this one-owner cohort. Real multi-owner search has additional routes to improvement and changing populations. The fixture therefore neither establishes a stochastic success rate nor shows that the old failed toy was caused by this scheduling defect.

## Why scheduling loses coverage

The current policy selects `parent = eligible[mutation % eligible.Count]` and `operator = operators[mutation % 3]`. When the ordered eligible list stays fixed at nine entries, a parent's visits differ by multiples of nine, so its operator index never changes. Eight and ten parents cycle through all three operators. With eight retained teams and up to two appended supplied anchors, all three counts can occur in production.

The extraction into `PlanBlockMutation` exposes this existing rule without changing it. The scalar parity test runs both actual search methods with two supplied starts and the prospectively fixed construction labels 17/31/47, 24 evaluations per arm and a 96-proposal ceiling. It compares the entire serialized result, including recipes, proposals, ancestry, fitness and shortlist. The preserved baseline DLL and rebuilt harness produce structurally identical reports. This establishes parity for those traces, not an exhaustive proof over every input.

The next implementation should **decouple parent visits from operator selection in a separately versioned policy**, preserving v1 reproduction. A bounded per-parent operator rotation is a candidate: every retained parent's successive visits should cycle through the three operators even when anchors or population membership change. Freeze the schedule and coverage assertions before running it; check eight/nine/ten-parent and changing-population cases, then rerun this controlled landscape under the new version while retaining v1's negative result. Do not change archive size or construction ratios to hide the coupling. An end-to-end quality gate and native admission remain separate prerequisites before reconsidering combat.

## Files and verification

- [TowerSuppliedCompositionSearch.cs](../LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs): extracts the existing planner; preserves version, random draws and search behavior.
- [BalanceHarnessControlledRetentionTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessControlledRetentionTests.cs): seven controlled integration/schedule cases with optional full trace export.
- [BalanceHarnessSuppliedSchedulingParityTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessSuppliedSchedulingParityTests.cs): full search report export and completion checks for before/after comparison.
- [README.md](../LL/tools/BalanceHarness/README.md), this review and the new evidence package: current handoff, frozen inputs, commands, receipts and preserved failure.

The isolated baseline tests, current harness and current tests restored from cached packages and built with zero warnings/errors. Gameplay dependencies were reused after verifying the previous package and all **1,337** pinned gameplay source files; gameplay was not rebuilt. Verification invoked `build/run-tests.ps1 -NoBuild -ArtifactsPath <package>/baseline` with the parity class filter, then the same wrapper with `<package>/current` and the parity, controlled-retention and existing supplied-composition class filters. Current coverage comprises 12 existing kernel cases, seven new controlled cases and one parity case. Exact commands and TRX files are in the package's `control` directory. The independent audit and scoped `git diff --check` also passed.

The harness build itself succeeded, but its wrapper stopped on the post-build storage check: captured fixture copies pushed usage above the frozen **48 MiB** local ceiling. Lossless ZIP recovery verified each captured fixture's original hash before removing its redundant uncompressed copy; executable inputs and all results were unchanged. No build or test needed a retry. The observed recovery peak was **52,370,170 bytes**, above the **50,331,648-byte** ceiling. This is a recorded resource violation, not a clean resource pass. The closure-only accounting amendment preserves the original workflow/pins, leaves the limit unchanged, and charges at least the observed high-water mark plus closure reserve against the approved cumulative allowance. Final retained output is below 48 MiB. All requested verification completed; none remains blocked.

The [completion receipt](../TestResults/balance/tower-controlled-retention-20260916/completion.json) records exact time, storage and remaining cumulative balances. Final preservation verifies both predecessor package manifests, frozen inputs, unrelated dirty files and the **483,046** authoritative reservations. The **891 authorized fresh values and up to 6,912 fights remain unused**. Current historical-anchor admission remains evidence for its recorded build; future policy binding needs its own matching identity and admission. Adoption remains Hold; V19 reliability remains Unresolved.

No gameplay content, persistent configuration, migration or deployment changed.
