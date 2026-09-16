# Archive-model offline execution — 16 September 2026

**FailedOfflineRankingGate.** The model failed the frozen offline ranking gate. This model proposal is closed: do not try another kernel, lambda, split, target, model family or sample extension under this proposal. No stronger party was established. Both incumbents remain eligible and adoption remains **Hold**.

The [approved design](Tower-Archive-Model-Design-Review.md) was executed once with **two fits**, one per frozen direction: 62 training and 62 test recipes. The two shared anchors were excluded from both roles. The distant subsets contain **45 and 41** recipes respectively. All observations are previously recorded discovery outcomes; **zero new fights, seeds, balance values, replays or native preparations** occurred.

## Fixed comparison

| Train → test | Method | Full Brier | Distant Brier | Top-eight wins /64 |
|---|---|---:|---:|---:|
| Retained → incumbent | Model | 0.175611711 | 0.131781052 | 39/64 |
| Retained → incumbent | TrainingMean | 0.227964848 | 0.168174879 | 14/64 |
| Retained → incumbent | FiveNearestRecipes | 0.173911290 | 0.145652778 | 31/64 |
| Incumbent → retained | Model | 0.150700762 | 0.100939916 | 34/64 |
| Incumbent → retained | TrainingMean | 0.181626398 | 0.129020460 | 13/64 |
| Incumbent → retained | FiveNearestRecipes | 0.195504032 | 0.159557927 | 22/64 |

Lower Bernoulli Brier is better. Each score averages squared error against every recorded binary outcome, with eight outcomes per recipe. Top-eight wins are summed across the selected recipes' 64 historical fights. These recipes share a seed panel, so those counts are not independent replicated evidence.

The gate requires model Brier no worse than **both** comparators over the full and distant sets in **both** directions, plus strictly more top-eight observed wins than **both** comparators in **both** directions. A top-eight tie fails. Exact comparisons used unrounded stored values, with no favorable tolerance.

- Retained → incumbent: `fullBrierVsFiveNearestRecipes` failed.

The complete [decision and selected recipe IDs](../TestResults/balance/tower-archive-model-offline-20260916/decision.json) preserve every metric and failed gate. No model, fold or sample adjustment followed the result.

## Implementation and validation

The isolated [prototype](../TestResults/balance/tower-archive-model-offline-20260916/model.py) uses 850 owner/Essence flags and 170 subgroup/Essence counts. Each block is normalized separately, then the joined vector is divided by sqrt(2). The kernel remains `((1 + dot(x,z))/2)^3`, ridge lambda remains 1, and the target remains discovery wins/8 centered on the training mean. Predictions are clipped to [0,1]. The comparators remain training mean and five nearest recipes by total per-slot Essence replacements. All ties use ordinal recipe IDs. NumPy is the only numerical dependency; no dependency was installed or copied.

The [fixtures](../TestResults/balance/tower-archive-model-offline-20260916/fixtures.json) passed, including placement sensitivity with equal expedition and subgroup counts, coordinated nonlinear interactions, canonical input ordering, same-source-family legality, invalid/duplicate IDs, missing slots, exact-recipe split leakage, forbidden test columns, unchanged predictions after held-out label changes, exact ties and failure on a flat landscape. Literal fixtures perform no model fits. Only the two approved archived fits ran.

Each fitting process opened only training rows with explicitly allowed target wins, a label-free test export and the captured Essence family map. Both models, coefficients, exports, source configuration and prediction sets were saved in the [prediction freeze](../TestResults/balance/tower-archive-model-offline-20260916/prediction-freeze.json) before the separate scoring process opened held-out labels. This prevents accidental workflow leakage; the historical labels were already available and this is not a prospectively blind experiment.

The [independent audit](../TestResults/balance/tower-archive-model-offline-20260916/independent-audit.json) passed using standard-library sparse owner/group arithmetic without importing prototype feature, model or scoring code. It reconstructed the regularized normal equations, clipped predictions, distances, baselines, all selected sets and binary-outcome Brier calculations. Maximum residual was **1.39e-15** against 1e-8; maximum prediction difference was **8.88e-16** against 1e-10. Ranking used the exact frozen finite-precision values after arithmetic verification, so near-ties were not silently rounded into ties.

Input checks verified the sealed design package, its source pins, discovery row/build/provenance/trial-ID reconstruction, all 128 legal recipes and the exact frozen folds. The approved pair still differs in **BalanceHarness.dll**; the original explicit pair of hashes was required. It was not treated as matching full execution identity or generalized into permission for arbitrary harness versions.

These are two adaptive logged pools with shared ancestry and one reused eight-value discovery panel each. Passing mechanical validation is separate from passing the quality gate. The result establishes no statistical significance, live search efficiency, superiority over either incumbent, or strength for unlogged parties.

## Budget and preservation

The approved once-only transfer moved **30 seconds and zero bytes from run to engineering**, leaving component time caps engineering **1,545**, run **1,755**, audit **300** seconds. Overall caps and every byte cap stayed unchanged. This scope conservatively charges its full **30 seconds /3 MiB**, split into three ten-second /1-MiB phase allowances including source/setup/output and preservation reserves. Owned Windows jobs bound subprocess work and reserve cleanup time; phase receipts record measured execution. Carried run/audit consumption is unchanged.

The [completion receipt](../TestResults/balance/tower-archive-model-offline-20260916/completion.json) leaves **4.525880 engineering seconds** and **1,164,024 engineering bytes**, with **1811.748608 overall diagnostic seconds** and **93,523,214 output bytes** remaining. No prior component consumption was re-added as a new delta.

The sealed planning and source packages, known history hashes, previously recorded unrelated dirty files and original handoff bodies are preserved. All **483,732 exclusions**, V19 **253 required recipes /512 unused confirmation values**, V19 **Unresolved**, prior pilots and diagnostics **Closed**, and adoption **Hold** remain in force.

Changed files are this execution review, the isolated Python/input/prediction/audit/accounting package, and current notices in `Tower-Search-Strategy-Reset.md` and `Tower-Team-Search-Design-Review.md`. The previous proposal remains preserved as historical evidence; its pending-approval wording has been superseded by this execution review.

Verification commands: the pinned Python runtime invoked `workflow.py input`, `workflow.py fit`, and `workflow.py publish`; publication checked document links, whitespace, hashes and accounting. Backend tests through `build/run-tests.ps1` were not run because this authorized scope adds no backend code and excludes backend builds/tests. No production source, gameplay content, migration, persistent configuration or deployment changed. No external environment was touched.
