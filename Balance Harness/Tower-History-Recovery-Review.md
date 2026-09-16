# Complete history scan and abandoned reservation recovery

16 September 2026. **VerifiedZeroCombat**. 45 backend tests passed. Complete history membership, counts, recovery transcript and the 482,911-value union verified.

Sequential reference scan: **41.5581 s**. Candidate scans: **17.5043 s / 16.3488 s**, a first-scan ratio of **2.37x**. Complete candidate history validation: **38.2906 s**, against 150 s.

The scanner uses four bounded directory workers while retaining complete enumeration, queued-directory attribute checks, exact subtree exclusion, the directory cap and cancellation/error propagation. All workers stop before return; no membership cache or archive pruning was introduced.

The separate recovery receipt binds the exact failed study inventory and complete allocation transcript. An explicitly pinned optional launcher mapping can read its 45 values as permanent exclusions. Default Pending rejection and the failed study's no-retry/no-launch rules remain. The original Pending file and all sealed source bytes are unchanged.

Changed files: TowerHistoryRegistry.cs, TowerRefinementComparisonLaunch.cs, new TowerRefinementReservationRecovery.cs, scanner tests, new recovery tests, this protocol/review and six active handoffs. No gameplay, configuration, migrations or deployment changes. No new seeds, combat, preparations or replays.

Reproducible commands, per-phase deadlines, logs, TRX, native timing/CPU/allocation/memory traces, source hashes and preservation checks are in the [evidence package](../TestResults/balance/tower-history-recovery-20260916/completion.json). The exact invocation is `python -B TestResults/balance/tower-history-recovery-20260916/workflow.py <phase>` for the frozen ordered phases; the completed directory rejects retries. Backend tests use `build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/balance/tower-history-recovery-20260916/tests` with the four-class filter recorded in `control/tests-command.json`. See the [protocol](Tower-History-Recovery-Protocol.md).

Limitations: one sequential-first observation, uncontrolled cache state and machine load; these results do not establish cold-start throughput or complete binding cost. No new balance or combat-strength evidence. All 482,911 values remain reserved, including V3's 45, the earlier failure's 40 and V19's separate 512 unused values and 253 recipes. V19 reliability Unresolved; later Fail 1/3, deep recovery 0/3; adoption Hold.

Next: use the verification outcome to decide whether a new, separately frozen comparison is ready. This scope authorizes no fresh allocation or combat and never permits retrying the failed study.
