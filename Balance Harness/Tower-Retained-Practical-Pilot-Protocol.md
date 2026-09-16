# Practical retained-composition pilot: proposed protocol

**Proposed next step: one practical search with the verified fixed-order comparator, followed by independent confirmation against both admitted teams.** The question is whether this run finds a useful new team for current floor 5. It is not another block-search variant, a comparison of algorithms, independent rediscovery, or a global-optimality claim. The [machine-readable proposal](../TestResults/balance/tower-retained-practical-pilot-planning-20260916/proposal.json) is awaiting authorization. No new balance values, executable study definition or combat run has been created.

The [standalone parity checks](Tower-Retained-Standalone-Review.md) and [native preparation verification](Tower-Retained-Native-Verification-Review.md) passed. That makes the existing comparator the smallest supported search to try; it does not establish its current performance. The failed block proposal stays closed. Its 891 unused values and 6,912 possible fights do not transfer to this pilot.

## Fixed target and search

Use the current content/settings and standalone executable identity captured by the passing native verification, after checking that their source/content hashes still match. The target is Kharad/floor 5: ten level-40 characters, five Essences each, Standard tier-1/rank-2 fixed equipment, neutral identities, one equipment context and no contributed damage or styles. All characters and subgroup placements remain searchable under existing legality checks. The eligible pool is the admitted 85 Essences /82 families; `OwnedCopies=null` means sufficient copies are assumed. This is conditional legal-team finding, not an acquisition or player-inventory claim.

Supply exactly the two admitted references: `team-040e60d3dbc5c127321653c47ed3a9d3` and `team-49f6979895354870c89362d4abf214bb`. Preserve their canonical recipes and ancestry hashes. Discard historical fitness. Each is scored as a start inside the 64-candidate budget. Ability order stays ordinal and outcome-independent throughout.

Use `retained-composition-v1`, one method and **one construction root**, 64 complete evaluated parties and at most 256 emitted proposals, including rejected/duplicate opportunities. Keep the existing fresh schedule, parent retention, neighborhood scan and mutation operators. No ratios, operators, catalogue restrictions or scoring rules change.

Keep the existing four-party shortlist: best two plus two diversity representatives under the production `Retain(...).Skip(4).Take(2)` rule, with distinct ranked fallback and final discovery-rank order. This is not simply the top four. Use the existing zero-win selection policy to choose one finalist: selection wins first; only zero-win ties use mean guardian health, then frozen discovery rank and stable ID. Positive-win ties retain the existing behavior. The finalist can equal a start; do not force novelty by discarding an anchor after observing results.

## Stage budget and fresh values

| Stage | Fixed maximum | Fights |
| --- | --- | ---: |
| Discovery, including both starts | 64 teams ×8 values | 512 |
| Selection | 4 frozen nominees ×32 separate values | 128 |
| Confirmation | 1 frozen finalist +2 anchors, each ×256 new values | 768 |
| **Total** | No diagnostics or replays | **1,408** |

The allocation request is **297 fresh values**: one construction root, eight discovery, 32 selection and 256 confirmation. All are mutually disjoint and excluded against the full live reservation/history union, currently 483,046. Within each stage, teams share the panel for paired evaluation. No confirmation result enters construction, nomination or selection. Do not reuse either admission package's historical schedules. No master or candidate balance value is chosen or derived in this planning step.

Both anchors are always in the frozen confirmation family. If the finalist is identical to an anchor, existing exact-recipe merging saves 256 fights while retaining both origins; it cannot fund extra proposals or samples. A returned anchor fails the novelty requirement. All 297 values remain excluded even when execution stops or merging leaves capacity unused.

The generic study state machine confirms after completed discovery/selection; it has no outcome-based futility gate. This protocol therefore does not promise one. Even zero selection wins lead to the fixed confirmation family, providing current anchor measurements within the stated maximum. Incomplete discovery, cancellation, changed inputs, invalid artifacts or resource exhaustion stop the run and cannot establish improvement. No retry, resume, second root, sample extension or renamed follow-up is automatic.

## Decision fixed before allocation

Call the result **demonstrated improvement in this pilot** only if execution and reconstruction are complete, the finalist differs from both anchors, its adjusted confirmation lower bound is at least 10%, and it beats **each** anchor by at least five observed percentage points with a positive adjusted paired lower bound. Otherwise report **improvement not demonstrated**, distinguishing returning an anchor, a worse point estimate and insufficient precision. Missing evidence is an integrity failure. No outcome changes the gate or triggers more fights.

Use a fixed family of **seven Bernoulli quantities**: the finalist and two anchor win probabilities, and the gained/lost probabilities for both paired contrasts. For each quantity use existing `TowerBalanceEvaluator.Wilson(count, 256, 7)`. For a contrast, the conservative interval is `[gain.Lower - loss.Upper, gain.Upper - loss.Lower]`, multiplied by 100 for percentage points. This applies the existing approximate Bonferroni-Wilson machinery to a fixed family; it is not exact finite-sample coverage. Keep the family size seven if an exact recipe merges; that case already fails novelty.

The standard study's paired intervals are approximate pointwise 95% descriptions and its balance report enforces a 50% ceiling. Neither is silently reinterpreted as this pilot's improvement gate. A separate pinned summary computes the declared seven-component decision from saved outcomes; the existing study reports remain unchanged. Keep any team above 50% visible and report its balance implication separately. Boss health and survival are descriptive, not replacement endpoints. One construction root cannot establish search reliability or superiority to another algorithm; 256 confirmations can leave a useful five-point gain unresolved.

## Readiness before binding or combat

Source inspection found two concrete execution gaps. The native test resolves the standalone harness and gameplay DLLs from different directories, while `TowerBossStudy.RetainExecutable` expects a complete runtime beside the harness. Also, `TowerBossStudy.RunAsync` limits fights but its generic CLI does not provide durable external authorization, a once-only launch record or hard wall-clock/storage limits. Existing reservation adapters have fixed older schedules and cannot be relabeled as this 297-value request.

Prepare a small isolated launch package using the existing study and reservation/storage patterns; do not implement another search kernel. The readiness work must:

1. Assemble a complete runtime from the pinned standalone harness and matching dependencies, verify executable retention and reproduce native admission from that exact location. Preserve the producing identity or stop for review if it changes.
2. Bind the exact proposal hash, current source/content/settings, two anchors, stage counts and resource limits. Require a durable launch marker so the same request cannot run twice.
3. Validate a durable four-stage reservation adapter with literal synthetic labels, including collisions, interrupted/Pending states and mismatched authorization. Reserve actual values only after all readiness gates pass and the complete historical exclusions are reconciled. Preserve every returned value and unresolved derivation; never clear old Pending evidence.
4. Verify existing study dispatch, four-party nomination, selection, merged confirmation accounting, cancellation/exhaustion, a durable attempted-fight counter, and the fixed seven-component summary using fabricated outcomes. Run backend checks through `build/run-tests.ps1`. These are workflow checks, not new search-quality landscapes.
5. Bound the entire owned process tree and all new runtime/binding/study output. Treat scientific exit codes 0/1/3 as completed only when the actual study status is `Complete` and its matching reconstruction passes; `Incomplete`, `Invalid` or `Cancelled` cannot masquerade as a successful run. An above-50% team must not be discarded because the separate balance assessment fails.

Run once only after these gates pass. Retain complete recipes, provenance, shortlist, selection results, confirmation freeze, outcomes, attempted/completed counts, runtime and manifests. Use saved-evidence `TowerBossStudy.VerifyAsync` plus independent ledger/outcome/statistics reconstruction; no combat replay is reserved. Any failed readiness gate stops before fresh binding or combat. A changed policy or numerical envelope requires a revised prospective decision, not an in-place adjustment after outcomes.

## Requested authorization and resource envelope

The current overall limits remain **7,980 diagnostic seconds /5,804,916,736 bytes**. Request a **100-second /64-MiB transfer from unused run allowance to engineering**, making engineering **775 seconds /336 MiB**, run **2525 seconds /656 MiB**, and unchanged audit **300 seconds /32 MiB**. No overall increase is requested.

Within those component limits, authorize this pilot only up to:

| Work | Time cap | New-output cap |
| --- | ---: | ---: |
| Readiness, including static work, tests and failed attempts | 90 seconds | 64 MiB |
| Binding and one run, including runtime copies and cleanup | 1,200 seconds | 600 MiB |
| Saved-evidence verification and publication | 180 seconds | 16 MiB |

These are stopping ceilings, not runtime predictions. If the caps prevent completion, preserve partial evidence and report no demonstrated improvement. The older 280-fight result took 65.55 native seconds and roughly 81 MiB of study output, but it used a different version/cohort and archive path; it does not certify that this pilot fits. No benchmark or combat is run to choose the budget.

**Planning overrun:** the read-only preservation freeze measured **8.203 seconds**, exceeding this planning scope's five-second total ceiling and its remaining engineering balance. The [planning receipt](../TestResults/balance/tower-retained-practical-pilot-planning-20260916/completion.json) records the overrun, static/publication allowance and full cumulative charge. It is not hidden or retroactively passed by the proposed transfer. No build, test, fresh value or fight occurred. The requested 100 seconds covers the charged overrun and leaves room for the separately capped 90-second readiness work.

The approval requested is for readiness, then **at most 297 fresh values and 1,408 fights**, followed by the fixed verification, conditional on every gate above. It authorizes no block-search revival, gameplay tuning, V19 confirmation, deployment, retries or extra samples. All 483,046 reservations, V19's 253 required recipes and unused 512 values remain preserved. Adoption stays Hold; this search result alone cannot establish general Tower balance acceptance.

## Planning work and verification

This turn adds this protocol and a non-executable proposal/accounting package, and updates the README plus five handoff notices. No production or test-project code changes. Static verification checks the stage arithmetic, source pins, links, prior sealed native-verification package, unrelated dirty files, reservation union and scoped `git diff --check`. The preservation overrun is explicitly charged. No backend build/test or native preparation command was run because this is prospective planning; none is claimed as newly verified. There are no migrations, persistent configuration changes or deployment implications.

Source references: [study dispatch/stages](../LL/tools/BalanceHarness/TowerBossStudy.cs), [runtime retention and reconstruction](../LL/tools/BalanceHarness/TowerBossStudyArchive.cs), [shortlist and retained search](../LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs), [selection and confirmation merging](../LL/tools/BalanceHarness/TowerBossStudyPolicy.cs), [Wilson implementation](../LL/tools/BalanceHarness/TowerBalanceEvaluator.cs), [existing descriptive paired intervals](../LL/tools/BalanceHarness/PairedStatistics.cs), [durable reservation pattern](../LL/tools/BalanceHarness/TowerRefinementReservation.cs), and [older measured execution](Tower-Fresh-First-Comparison-Execution-Review.md).
