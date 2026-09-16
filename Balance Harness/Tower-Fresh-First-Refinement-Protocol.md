# Fresh-first V5 allocation: implementation and zero-combat verification

16 September 2026. Target: offline `LL/tools/BalanceHarness`. Implements the authorized follow-up to the [V4 trajectory diagnosis](Tower-Local-Refinement-Trajectory-Review.md). No combat comparison is authorized.

## Frozen design

Add opt-in `independent-discovery-refinement-fresh-first-v5`. For a fixed budget N, spend the first `N - floor(N/4)` proposal positions on unchanged fresh construction; use the remaining positions for V4's single-Essence, single-slot edit of the best completed parent. The sixteen-proposal comparison therefore uses **12 fresh + at most 4 local edits**, compared with V4's seven fresh + nine edits. This 75/25 split is an exploratory engineering choice, not an optimum inferred from the observed winning recipe.

Require equal candidate and attempt caps for V5, from one through the existing maximum of sixteen. Rejections and duplicates consume positions. Without a completed parent, retain the controller's existing charged fresh fallback; no extra attempt or refill is granted. Candidate exhaustion and cancellation remain unchanged. Preserve V4's local-edit random namespace, fresh sequence, legality checks, metadata, ranking, nominations and selection. Ability order stays fixed; no gameplay or Kharad change. V1–V4 serialized behavior and all defaults stay unchanged.

Expose V5 through explicit comparison `tower-discovery-refinement-comparison-v4` and gate `tower-discovery-comparison-gate-v4`; retain policy-specific driver receipt and authorization validation. No allocator, live registry refresh or preparation service runs here.

## Resource and execution envelope

New evidence: `TestResults/balance/tower-fresh-first-refinement-20260916`. Start with the sealed trajectory receipt: **3,886.9672723556187 seconds / 4,599,617,720 bytes**. Existing cumulative ceilings remain **4,260 diagnostic seconds / 4,613,734,400 bytes**. This scope allows **180 diagnostic seconds / 13 MiB** including setup, compilation, tests, temporary fixtures, shared test-runner output, audits, failures and publication. Reserve 1 MiB for shared test-runner output. Charge twenty seconds for initial inspection plus measured checkout/source snapshotting; source/script drafting is excluded.

Freeze source/test bytes, this protocol, exact commands, dependency/reference hashes and the test filter before the first build. Phase maxima: freeze 15 s, harness build 30 s, test build 25 s, tests 75 s, audit 10 s, publication 10 s, all sharing the 180-second total. The owned process runner bounds and cleans up child processes. Run each phase once; on the first failure stop dependent diagnostics, retain evidence and publish the limitation. Zero diagnostic retries, fresh balance values, preparations, fights or combat replays. Literal labels and fabricated evaluator results are test fixtures only.

Build the isolated harness and test assembly against the already captured dependency bytes and cached restore assets, without rebuilding gameplay or copying another runtime. To fit the remaining output allowance, retain the newly built harness at its intermediate path and disable its redundant output copy; the test assembly's pinned resolver loads that exact DLL. Retain/count all fixture temporary output. Snapshot unrelated checkout bytes before editing and record concurrent changes without overwriting them.

## Exact verification

Run **85 backend facts** once through `build/run-tests.ps1 -NoBuild -ArtifactsPath <new evidence>/tests` with a frozen ten-class filter:

- The existing 68 discovery-refinement, role, novelty, comparison-version, gate and local-refinement facts, including maximum-neighbourhood exhaustion and archive/charge checks.
- Ten new allocation facts: opt-in/equal caps; schedule across N=1..16; no-parent charged fallback; exact baseline twelve-recipe prefix and four-edit tail; best completed parent and one canonical replacement; exhausted tail charged without refill; checkpoint failure/cancellation before the thirteenth evaluation; metadata-order determinism; unchanged V4 edit and random offset; cancellation before generation.
- Six new comparison-version facts: policy-only input differences; matching driver receipt; authorization mismatch before allocation; synthetic binding/reconstruction; incomplete discovery stops nominations; wrong policy/nominations rejected.
- One evidence-only parity fact: compare complete serialized V1, V2, V3 and V4 generation results between the sealed tested V4 DLL and the new DLL under identical literal-input synthetic evaluators. No engine or production reservation is invoked.

Persist TRX, timings, commands, complete generation fixture output, parity hashes, source/binary pins, attempt/archival checks and resource receipts. Audit the recorded twelve-fresh/four-local schedule and parity independently from the test assertions, recheck frozen inputs and unrelated changes, and run scoped `git diff --check`. No broader test rerun is scheduled after success.

Update a measured review and the six active Markdown handoffs. A successful implementation does not show stronger combat performance and is not permission to allocate a comparison. All **483,001 reservations** stay excluded, including prior failed allocations and V19's 512 unused values. V19 retains all 253 recipes and no confirmation; reliability Unresolved, later Fail 1/3, deep recovery 0/3 and adoption Hold. No configuration, migrations or deployment.
