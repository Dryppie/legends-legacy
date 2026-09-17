# Practical World Tower search: evaluation allocation proposal

17 September 2026. Target: the offline `LL/tools/BalanceHarness` team-search workflow.

**Status: implemented and tested; the paired comparison missed its endpoint. Keep the incumbent policy as default.** The [comparison result](Tower-Practical-Search-Allocation-Comparison.md) records a -0.73-point mean difference across three paired restarts and closes this version's evaluation without promotion or extra samples. The [implementation specification](Tower-Practical-Search-Evaluation-Allocation-Implementation.md) defines the concrete rounds, sampling, costs and integration. The original recommendation below records its rationale and limits.

## Recommendation

Test better allocation of fights during search, using the confirmed strong team as a starting point. Keep the existing mutation operators initially so the comparison can isolate whether the evaluation change helps.

The objective is stronger independently evaluated output within the same practical fight budget. Improving a supplied team and discovering a strong team from scratch remain separate objectives.

## What the current evidence supports

The [supplied search implementation](../LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs) already supports single, double, cross-character and whole-character changes, plus recombination. The practical method mostly chooses parents from its four highest-ranked discovery teams, with explicit access to supplied anchors. Recent runs evaluated each candidate on eight discovery fights.

A lucky early score can therefore influence many later proposals. This is a plausible weakness, not an established explanation of the search's failures. The [hypothesis review](Tower-Practical-Hypothesis-Qualification.md) documents the small-panel rankings and their limits.

The [fixed-team confirmation](Tower-Practical-Fixed-Team-Confirmation-Execution-Review.md) established one useful result: candidate `399bc776…` won 3,932/5,500 trials, or 71.49%, versus 63.84% and 63.07% for the two reference teams. It differs from anchor 040e by two Essence assignments on one character. This supports using that exact team as practical starting knowledge; it does not establish reliable search improvement or a causal explanation for the benefit of either individual substitution.

## Proposed search behavior

### 1. Start from the strongest known team

Give the existing method and proposed method the same declared starting teams, including the confirmed candidate, under identical gameplay assumptions. Evaluate those teams afresh within the new comparison. Historical confirmation scores must not become new discovery measurements.

Keep ability ordering fixed and preserve gameplay legality, equipment and progression assumptions. This experiment would measure practical improvement from supplied knowledge, not independent discovery.

### 2. Make candidates earn influence on subsequent generations

Generate a small batch using the existing operators. Screen it cheaply, then give promising candidates more fights before making them dominant parents. Compare surviving candidates and incumbent parents on matching trial panels, and use fresh training panels between rounds. Final evaluation remains separate and untouched.

The principle resembles sequential halving: concentrate a fixed evaluation budget on promising alternatives. [Karnin, Koren and Somekh's research](https://proceedings.mlr.press/v28/karnin13.html) provides a precedent for fixed-budget identification. Its guarantees do not automatically transfer to this adaptive team search.

The original proposal left batch sizes, sampling stages, elimination rules and parent replacement rules open. The linked implementation now specifies them and compares parents on one common fresh promotion panel. Simply sorting mixed sample counts by observed win rate would not address the concern.

### 3. Preserve coordinated exploration

Keep double edits, whole-character changes and recombination. A combination can help even when its individual substitutions do not. Retain exploration opportunities rather than requiring every intermediate team to beat the incumbent.

Initially preserve the existing operator mix. Changing sampling, operators and diversity together would obscure which change caused the result. This proposal does not reopen the closed block-search or archive-model experiments.

### 4. Measure output quality per budget

Compare the unchanged method and proposed method across several search restarts with identical starting information and equal total fight budgets. Count parent reevaluation and incumbent evaluation against those budgets.

Measure independently evaluated final team strength, improvement frequency, actual fights and elapsed time. More accurate parent evaluation costs fights and may reduce search breadth; it must earn that tradeoff.

Freeze each output before opening its final evaluation results. Define the useful improvement threshold, comparison family, evaluation precision, resource ceilings and stopping rules before execution. Report every planned restart, including unsuccessful or incomplete runs.

## Why this remains a hypothesis

Extra feedback has already failed in an earlier experiment. [V14](../LL/tools/BalanceHarness/TowerGenerationFeedback.cs) periodically retested selected candidates and did not establish reliable improvement. The proposed distinction is frequent parent selection within a strong supplied-team neighborhood, not merely adding samples. That distinction needs a concrete design and evidence; it is not itself proof of benefit.

The [recent selection diagnostic](Tower-Practical-Selection-Diagnostic-Execution-Review.md) did not demonstrate a final-selection mistake. Another finalist-selector rewrite is therefore a weak priority. That diagnostic did not establish whether early parent selection discarded useful search paths.

Existing evidence does not establish that measurement quality is the dominant bottleneck. Proposal quality and search breadth may still matter more. A failed comparison must be allowed to reject this proposed remedy.

## Next deliverable and stopping decision

The versioned allocation change is implemented using the existing legality, execution, accounting, audit and export components. The subsequent [three-pair comparison](Tower-Practical-Search-Allocation-Comparison.md) specifies precision, success and operational choices and records the measured decision separately.

Promote the change only if the comparison supports stronger outputs at the same cost, or comparable outputs at a prospectively defined lower cost. If it misses its declared endpoint, close this proposal rather than cycling through sampling ratios or adding more confirmation after seeing the result. Failure would not prove that every adaptive allocation method is ineffective.

The original document added a recommendation only. Its implementation checkpoint changed the offline harness and tests without allocating scientific values or running a quality experiment. The subsequent comparison owns its real allocation and combat separately. Neither step changes gameplay content, migrates a database or deploys a service.
