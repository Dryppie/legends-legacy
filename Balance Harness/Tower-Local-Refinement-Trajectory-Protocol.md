# Saved V4 refinement and nomination diagnosis

16 September 2026. Offline BalanceHarness evidence analysis only, following the completed 288-fight local-refinement comparison. No game or search implementation changes.

## Frozen workload

Read the sealed `tower-local-refinement-comparison-study-20260916` and execution package, whose seal is `ebc687cbceee5e5f8e1d42ecf4d2bd7291eb830dfaa384a32638ed1c448a6668`. The readiness seal is `3707b364da43548335928192945dd9b7907fcd6d6ba0e520a8d9ad32707df054`. Retain those packages unchanged. New evidence goes only to `TestResults/balance/tower-local-refinement-trajectory-20260916`; publish this protocol, a review and the six active Markdown handoffs.

Start from the final comparison publication receipt: **3,874.8272723556192 diagnostic seconds / 4,598,743,362 bytes**. Existing cumulative ceilings remain **4,260 seconds / 4,613,734,400 bytes**. This scope permits **60 diagnostic seconds / 1 MiB** within those ceilings, including failures and publication. Charge ten seconds conservatively for initial instruction, schema and source inspection. Setup has five seconds, analysis thirty-five, publication ten; all share the sixty-second ceiling and publication includes one second reserved for sealing. Script/report drafting is excluded. Zero fresh values, generated proposals, preparations, fights, combat replays, retries or history-registry scans. Stop dependent diagnostics at the first failed assertion and preserve the failure.

Freeze scripts, the exact input manifests, producing-source hashes, prior resource receipt and protocol before analysis. Snapshot the dirty checkout; preserve unrelated work. Do not run any old execution command or change any old cap.

## Exact checks and outputs

1. Verify exact membership and hashes of the sealed readiness, execution and 556-file study packages. Verify relevant source pins against the captured producing sources. Read the existing 288 records: 64 baseline discovery, 64 V4 discovery, 32 selection and 128 confirmation. Check receipts, seed schedules, canonical recipes, outcomes and 288 durable attempt charges.
2. Recompute all 32 discovery measurements and the complete discovery ranking: wins descending, boss health ascending, survival descending, victory duration ascending, ordinal ID. Reconstruct both top-two nominations and selection decisions (wins descending, original discovery rank, ID). Report whether descriptive selection-health ordering agrees, without choosing new finalists or assuming results for unconfirmed teams.
3. Trace all sixteen V4 proposals. For all nine local edits verify the best completed parent, exactly one changed slot and one removed/added Essence, fixed ordinal order, trace agreement, novelty and bounded construction checks. Report same-seed parent/child health and win changes, incumbent changes, per-seed signs and skipped construction options. Check that the seven fresh V4 recipes equal the baseline's first seven. Distinguish successful local steps from whether they survive later fresh improvements or reach nominations.
4. Cross-check raw discovery health aggregates and all nine paired differences through a separate Decimal sum/count pass. Run the twelve existing pure-reader fixtures plus four local-trace fixtures (valid edit; multiple-slot rejection; multiple-Essence rejection; inconsistent skip accounting rejection). No backend build/test rerun: the unchanged implementation retains the previously verified 69 tests through `build/run-tests.ps1`.
5. Persist every candidate/edit, the independent arithmetic checks, resource usage, preservation receipts and a readable diagnosis. Explain the observed allocation tradeoff and nomination outcomes. Any recommendation is limited to this single adaptive trajectory; four reused discovery seeds do not establish general strength or an Essence's causal effect. Do not implement a changed policy or run more combat in this scope.

All **483,001 reservations** remain excluded, including V19's 512 unused values and all prior failed allocations. V19 retains all 253 recipes and no confirmation; reliability Unresolved, later Fail 1/3, deep recovery 0/3 and adoption Hold. Ability order stays fixed. No Kharad tuning, gameplay/content changes, migrations, configuration changes or deployment.

After freezing, execute `workflow.py setup`, `workflow.py analyze`, and `workflow.py publish` once each using the bundled Python with `-B`. A failed phase permits failure publication only, never retry. Exact commands and exit evidence are retained in the new package.
