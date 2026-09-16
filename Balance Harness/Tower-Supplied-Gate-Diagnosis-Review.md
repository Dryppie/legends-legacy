# Supplied-search gate diagnosis and native admission

16 September 2026. Target: offline `LL/tools/BalanceHarness`. Follow-up to the user's instruction to proceed after the stopped comparison.

**The failed fixture does not establish that diversity retention is ineffective.** Its landscape permits direct improvement without a weaker parent, and its lineage predicate can also credit a weaker donor that contributes nothing to the objective. The original **52/53** result remains failed and sealed; this follow-up neither reruns it nor changes its gate. Separately, the settings-copy defect is repaired in a new isolated preparation helper: **seven regression cases passed and both canonical historical anchors passed native current-gameplay admission**. No search variant, production seed allocation, combat or replay was performed.

## Why the gate cannot isolate the benefit of diversity

The [design review](Tower-Team-Search-Design-Review.md), deceptive-family row, called for a weaker distant B family whose useful child is not available through the competing A family's local moves. The [executed fixture](../TestResults/balance/tower-supplied-comparison-20260916/candidate/LL/tests/EssenceSystem.Tests/BalanceHarnessSuppliedComparisonTests.cs) instead rewards the presence of `e10` and `e11` on owner 1: neither scores 6, one scores 5, both score 9. Owner 2 is irrelevant to fitness. There are 12 distinct families, four slots per owner and no copy limit.

An independent [exhaustive enumeration](../TestResults/balance/tower-supplied-gate-diagnosis-20260916/landscape-audit.json) covers all **495** single-owner loadouts, representing **245,025** two-owner parties. It calls neither the harness nor its search kernel.

| Score | Loadouts on owner 1 | Optimal targets requiring exactly two replacements | Exactly three replacements |
| --- | ---: | ---: | ---: |
| 6: neither target | 210 | 6 from every loadout | 24 from every loadout |
| 5: one target | 240 | 21 from every loadout | 21 from every loadout |
| 9: both targets | 45 | Already optimal | Already optimal |

For example, `{e00,e01,e02,e03}` → `{e00,e01,e10,e11}` is a legal two-Essence replacement scoring 6 → 9. It preserves the other owner and satisfies the production operator's exact replacement-count and family rules. Thus **every** score-6 loadout has a direct route to an optimum supported by the coordinated operator. A score-5 parent is unnecessary. Independent fresh construction can also draw an optimum; 45/495 is its marginal probability on owner 1 under this fixture's uniform, unconstrained sampler. These are support/counting results, not observations of the failed run's actual ancestry.

The predicate has a second attribution problem. It counts an optimal child's score-5 parent whenever that parent appears in the retained population and four score-6 measurements already exist. It does not require the child to improve upon all parents or the weaker parent's contribution to affect the objective. A [legal donor counterexample](../TestResults/balance/tower-supplied-gate-diagnosis-20260916/lineage-counterexample.json) keeps an already optimal primary owner's loadout and imports only the irrelevant owner 2 from a score-5 donor. The child still scores 9 and the old predicate would credit the donor if its other preconditions hold. This counterexample is a static construction, not a claim about an observed proposal.

The saved TRX establishes that block search improved after the initial batch in roots 17 and 47 and that the old lineage count was zero. It retains no full proposal/population report for that toy. Consequently the actual reason for zero witnesses—retention, parent selection, operator opportunities, direct discovery or another path—**cannot be reconstructed from the saved observations**. Reusing the same roots to search for a passing assertion would not resolve this evidence gap.

The appropriate interpretation is narrower than rejection of the algorithm: the fixture's lineage requirement did not identify the intended mechanism. This does not retroactively pass the closed proposal, validate combat strength, or justify silently bypassing its stop.

## Preparation repair and current-anchor result

The [new helper](../TestResults/balance/tower-supplied-gate-diagnosis-20260916/PreparationAdmission.cs) uses explicit dictionary keys for `Combat`, `ThreatAndTanking`, `IdleProgression`, `EncounterCadenceSeconds`, `WorldTower` and `CombatTicksPerFrame`, following existing harness snapshot writers. The previous anonymous object was serialized with camel-case property names, which the case-sensitive settings reader rejects. The production reader and live configuration were not changed. Only the selected non-secret combat settings are copied, and existing output files cannot be overwritten.

The [seven tests](../TestResults/balance/tower-supplied-gate-diagnosis-20260916/PreparationAdmissionTests.cs) verify both JSON encodings, exact exported keys, overwrite protection, invalid cadence rejection and reproduction of the original failure. The failed helper remains untouched in its sealed package.

The [native admission receipt](../TestResults/balance/tower-supplied-gate-diagnosis-20260916/native-admission.json) admits:

- `team-040e60d3dbc5c127321653c47ed3a9d3`
- `team-49f6979895354870c89362d4abf214bb`

Each has ten level-40 characters with five Essences, tier 1/rank 2, targeting floor 5 with the fixed equipment, neutral identities and ordinal Essence order. The current eligible pool contains **85 Essences in 82 families**. Production admission checked all ten equipment templates and materialized both reference inputs. It reused an already reserved historical label only for preparation; it derived no new values and executed no battles. Historical ancestry and evidence hashes are retained.

The helper uses the previous step's full current-gameplay build after checking all **1,337** pinned gameplay source files still match and verifying the reused DLLs against the sealed package. Only the small helper/test assembly was newly compiled. The archived harness assembly contains the withdrawn candidate controller, which this helper never calls. The receipt records that exact execution identity. Admission demonstrates current compatibility and legality; it does **not** transfer historical win rates, validate a player's copy inventory, or authorize a combat run. The saved admitted definition retains historical schedule structure for admission and is not a fresh study request.

## Verification and next implementation boundary

The isolated helper restored from cached packages and built with **zero warnings/errors**. Backend verification used:

`build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/balance/tower-supplied-gate-diagnosis-20260916/tests -Filter "FullyQualifiedName~EssenceSystem.Tests.PreparationAdmissionTests"`

All **7/7** cases passed. One subsequent native admission invocation completed successfully. The independent enumeration and donor counterexample scripts passed their assertions. Exact commands, TRX, hashes, process-tree accounting and final audit are retained in the [new package](../TestResults/balance/tower-supplied-gate-diagnosis-20260916/completion.json). Final verification checks the entire previous package unchanged, unrelated dirty files, current input pins, the 483,046-value authoritative ledger and scoped `git diff --check`. No relevant command in this follow-up was blocked or failed. The stopped comparison, its tests and historical combat were not rerun.

The next useful implementation is a **controlled retention-and-scheduling fixture**, separate from the end-to-end search-quality test:

1. Supply a fixed scored population with more than four near-duplicate A parties and a distant weaker B; prescribe candidate opportunities before execution. For the operator being isolated, verify A cannot reach the designated child in those opportunities while B can. Keep alternative whole-character and fresh routes explicit; they have broad support and cannot honestly be described as globally unreachable.
2. Exercise production retention, parent selection and the chosen coordinated operator together. Require B to survive and receive a usable opportunity, and require its child to improve on every contributing parent. Compare with elite-only retention on the identical prescribed opportunities to test whether retention makes a difference. This is a conditional mechanism check, not a stochastic success-rate benchmark.
3. Save the complete population, parent, operator, rejection and score trace. Separately assess end-to-end best-found score/regret at equal budgets, allowing improvement through any legal route. Freeze label permutations and limits prospectively; do not adjust the old result into a pass.

This follow-up implements the preparation repair and completes the gate diagnosis; it does not implement that new scheduler fixture or reopen the comparison. The existing search kernel and selector are unchanged. No automatic new search ratio, seed sweep, archive-size variant or combat extension follows from these findings.

## Changed files and budget

The new preparation helper, regression cases, enumeration, counterexample, workflow and receipts are confined to `TestResults/balance/tower-supplied-gate-diagnosis-20260916`. The repository handoff in `LL/tools/BalanceHarness/README.md` and this review are updated. All old sealed studies and unrelated dirty files are preserved.

The scope was frozen at **120 diagnostic seconds / 32 MiB**, within the unused approved cumulative allowance and remaining engineering allocation. The final [completion receipt](../TestResults/balance/tower-supplied-gate-diagnosis-20260916/completion.json) includes exact charges and updated balances. All **483,046** reservations remain unchanged; the **891 authorized fresh values and 6,912 possible fights remain unused**. Adoption remains Hold and V19 reliability remains Unresolved.

There are no gameplay-content edits, migrations, persistent configuration changes or deployments.
