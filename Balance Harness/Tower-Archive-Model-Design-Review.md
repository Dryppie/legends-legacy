# Archive-guided ranking: a different search hypothesis

**Design and corpus inventory complete — prototype not executed.** Test whether a small nonlinear model can learn from compatible, previously evaluated complete parties and rank new compositions more usefully than simple archive lookup. This changes **how candidate evaluations are chosen**, rather than adding another mutation label or hand-picking another two-slot party. The first useful step is a fixed offline ranking gate on existing data, with **zero combat and zero new balance values**. Do not integrate it into search unless that gate passes.

This is a new proposal following the request for a different search direction. The [previous assessment](Tower-Team-Search-Design-Review.md), failed block-search and Web Weaver packages remain closed. Neither incumbent is displaced; no stronger party has been identified. The model is a hypothesis about evaluation efficiency, not an established optimization result.

## Evidence available to test it

The [inventory](../TestResults/balance/tower-archive-model-design-20260916/inventory.json) checks two sealed practical pilots. Each contains **64 complete discovery parties ×8 shared discovery trials**, for **128 rows /1,024 existing fights**. There are **126 distinct recipes**: only the two supplied anchors repeat across studies. The content hashes, settings, equipment contexts, budget, allowed pool, party size and ownership assumption match. The two historical discovery seed panels are disjoint.

Their complete execution identities **do not match**. Comparing their preserved executable inventories finds one difference: `BalanceHarness.dll`, reflecting distinct harness policy versions. Other recorded executable files match, including gameplay assemblies. This difference is explicitly retained in the corpus provenance; it is not silently discarded or treated as full runtime parity. Both original independent audits passed. A future importer must require this exact reviewed pair of manifests, rather than broadly allowing arbitrary harness-version differences.

Only discovery rows enter [the exported corpus](../TestResults/balance/tower-archive-model-design-20260916/corpus.json). Selection, confirmation, behavior/telemetry and historical health-based rankings are excluded as learning inputs. No old score is presented as fresh evidence. The same historical observations can inform a future proposal, but cannot also confirm that proposal's strength.

The [fixed folds](../TestResults/balance/tower-archive-model-design-20260916/folds.json) train on one pilot and test on the other, then reverse. Both shared anchors are excluded from both roles, leaving **62 training /62 test recipes per direction**. By complete-party replacement distance, **45 and 41** test recipes respectively differ from every training recipe by at least four per-slot Essence replacements. These counts are descriptive corpus properties, not additional independent trials.

There are only **two adaptive construction roots**, many descendants share supplied ancestors, and each study reuses one eight-value discovery panel across its recipes. Thus a row split or 1,024-fight count must not be advertised as lineage-independent replication. The offline result concerns ranking inside these two logged candidate pools. Unlogged compositions have no observed labels; their counterfactual strength cannot be scored from this archive.

## Minimum model, with no new dependency

Use one isolated Python prototype with the available **NumPy** runtime. No production C# changes, game-service dependency, package installation, training service or general model framework is needed. The model predicts discovery win fraction from composition alone.

Use **850 slot-by-Essence flags** (ten fixed slots ×85 allowed IDs) and **170 subgroup-by-Essence counts** (two subgroups ×85 IDs). IDs and columns have one fixed ordinal ordering. Owner flags distinguish where an Essence is placed; subgroup counts expose repeated support within a subgroup. They do not establish the strength of that support. Fixed owner equipment and subgroup assignment are part of this cohort contract. A differently equipped cohort requires a new representation/admission decision.

Normalize each feature block to unit L2 length, concatenate them, and divide by the square root of two. For normalized vectors `x,z`, freeze the kernel **K(x,z) = ((1 + dot(x,z))/2)^3**. Fit centered kernel ridge regression with **lambda =1**, target `wins/8`, and clip predicted scores to `[0,1]`. The solve is only **62×62**. There is no model randomness and no hyperparameter search. The exact formula and constraints are frozen in [the offline proposal](../TestResults/balance/tower-archive-model-design-20260916/offline-proposal.json).

The cubic kernel lets the score depend on combinations of input features, rather than assigning a universal additive value to each Essence. It does not represent every possible high-order party interaction, guarantee extrapolation or produce calibrated probabilities. Sixty-two training rows in a large composition space can still be inadequate. This is why the first test asks whether the model provides any useful ranking signal on held-out logged recipes before using it to direct new evaluations.

**Forbidden predictors:** wins, guardian health, telemetry, survival, seed, study ID, generation root, proposal order/rank, operator, ancestry, selection and confirmation results. Outcomes appear only as training targets or held-out scoring labels. In particular, runtime telemetry would be unavailable for an unevaluated candidate and would make this proposal circular.

## One fixed offline comparison

| Method | Information and prediction |
|---|---|
| Training-mean baseline | One constant score equal to the training rows' mean win fraction. |
| Five-nearest-recipe baseline | Mean win fraction of five training recipes with smallest total per-slot replacement distance; distance ties use ordinal recipe ID. |
| Cubic-kernel model | Same training recipes and outcomes; one fixed normalized representation and regularized nonlinear fit. |

All methods rank the same 62 test recipes per direction. Prediction ties use ordinal recipe ID. Freeze the model configuration and both sets of predictions before scoring; fitting processes receive training labels and label-free test features only. The scoring phase reads test labels afterward. This reduces accidental leakage; it does not turn already available historical data into a prospective blind combat experiment.

The prototype has exactly **two model fits**, one per direction. It must retain all predictions and failures. Compute Bernoulli Brier score as the mean squared error between a recipe's predicted score and each of its eight actual held-out win/loss outcomes, giving each recipe equal weight. Do not substitute boss health when win prediction is weak.

The frozen engineering gate requires **all** of the following in **both** directions:

1. Input, representation, legality, split, numerical and leakage checks pass. Each test set and its distance-at-least-four subset contain at least eight recipes.
2. The model's Brier score is no worse than **each** baseline, both over the full test set and over the distance-at-least-four subset.
3. The model's top eight test recipes produce **strictly more observed discovery wins** across their 64 historical fights than each baseline's top eight. A tie fails.

This is a deliberately small engineering filter, not a statistical-significance claim. The eight selected recipes still share the same seed panel, and the two folds are not independent gameplay replications. A pass is named `PassedOfflineRankingGate`; it establishes neither search superiority nor stronger teams. A failed quality gate ends this model proposal without trying another kernel, lambda, split, target, model family or extra sample. Missing or invalid evidence cannot pass.

Meaningful no-combat fixtures should exercise placement-sensitive coordinated interactions despite equal expedition-wide counts; same-family illegality and canonical-order invariance; exact-recipe leakage between folds; rejection of outcome-derived columns; frozen predictions unchanged by subsequent held-out-label changes; and a flat all-zero outcome landscape that cannot manufacture a top-eight advantage. Keep literal tiny fixtures separate from the archived ranking result. They test the mechanism and failure handling, not combat strength.

For an independent audit, reconstruct features/distances/kernel entries from saved JSON, verify the solved coefficients against their regularized normal equations, then reproduce clipped predictions, both baseline rankings, Brier scores, top-eight sets and the complete gate. The identity regularizer makes the linear system nonsingular; this permits a residual-based arithmetic check without trusting a second model implementation. Use fixed numerical tolerances and preserve every artifact. No simulator or native preparation is invoked.

## Connection to actual search—only after the offline gate

A passing prototype would justify designing a candidate-selection experiment: generate bounded legal complete-party candidates with fixed-order, coordinated operators, and use the model to prioritize which receive scarce combat evaluations. Keep an independent exploration allocation and both incumbents eligible. An unconstrained maximization of the model score would invite exploitation of prediction errors.

That later comparison must give the model and comparator equivalent archived starting information, combat budgets, candidate/proposal ceilings and measured computation limits. An evaluation-prioritization comparison should first hold its legal proposal pool fixed so a changed generator cannot receive the model's credit. The saved-pool offline test does not execute this adaptive search or resolve performance outside its logged support. Any live comparison needs its own full frozen protocol, new independent outcomes and stopping rule. No such combat allowance is requested here.

## Concrete next implementation scope and budget

The [reviewable proposal](../TestResults/balance/tower-archive-model-design-20260916/offline-proposal.json) specifies one isolated prototype, no-combat fixtures, two model fits, predictions, saved-arithmetic audit and publication, capped at **30 seconds /3 MiB**: ten seconds /1 MiB each for input/fixture verification, fitting/prediction/audit, and publication/preservation. Pin the available NumPy/Python versions and reuse inputs in place. No dependency download, copied runtime, model retry, C# build, harness run, native preparation, seed derivation or combat is included. Any later backend tests must use `build/run-tests.ps1`; this prototype adds no backend code or backend test invocation.

This design step conservatively charges **15 engineering/diagnostic seconds**, with a **2 MiB ceiling**, leaving **4.525880 engineering seconds**. The requested next scope therefore needs an explicit once-only **30-second transfer from unused run capacity to engineering**, with **zero byte transfer** and no increase to overall caps. Proposed time caps are engineering **1,545**, run **1,755**, audit **300** seconds; current caps remain unchanged until approval. The receipt reconciles available bytes and carried run use. Merely having unused run time does not extend the engineering cap.

**Approval requested for that concrete 30-second /3-MiB offline prototype and its 30-second transfer—not for combat or a new search run.** A pass would require a later design decision before search integration. A failure closes this model proposal.

## Work completed and preservation

The inventory and publication checks passed: both source studies' sealed manifests and independent-audit records, exact compatibility fields and executable differences, 128 rows and their trial links, fixed-order ten-by-five recipes, disjoint historical discovery panels, fold overlap exclusion, all known history hashes, current **483,732**-value exclusion union, unrelated dirty files, local links, whitespace and resource arithmetic. The [completion receipt](../TestResults/balance/tower-archive-model-design-20260916/completion.json) and [preservation record](../TestResults/balance/tower-archive-model-design-20260916/preservation.json) retain the result. **No model was fitted**, and the offline quality gate remains unexecuted.

Changed files are this design, its corpus inventory/folds/pins/proposal/accounting package and current notices in the strategy handoff and original design review. No production source, gameplay content, migration, persistent configuration or deployment changed. No builds, tests, native preparation, live scans, new seeds, fights or replays ran. All prior experiments remain closed; adoption **Hold**, V19 **Unresolved**, with **253 required recipes /512 unused confirmation values** preserved.
