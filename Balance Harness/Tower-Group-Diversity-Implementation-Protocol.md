# Group-diversity implementation and zero-combat verification

15 September 2026. Target: offline `LL/tools/BalanceHarness`. Implement the user's authorized next search step after the saved-trajectory audit. **Zero fights, combat preparations, fresh seeds or replays.** Controls, nominations, observed combat strength and Essence names do not guide selection.

## Frozen policy

Add opt-in `independent-group-diversity-v1` / `group-diversity-joint`. Keep the existing bounded joined catalogue, atomic group reservation, count-drift rejection, candidate/attempt limits, every-eighth uniform request, canonical ability order, mutation/ranking/nomination logic and durable combat/storage contracts. Existing policies and defaults retain their outputs.

Build a deterministic order of the entire catalogue: greedily maximize previously unseen authored evidence keys, then unseen source-core IDs, then unseen Essence IDs. Break ties by the unsigned stable seed of policy/version, generation label and group ID, then ordinal group ID. Canonicalize set-valued metadata. This is structural coverage, not estimated synergy strength. Compute/cache one order for the active generation label; no growing historical scans, outcome input or random-stream consumption.

Visit every group once before another variant sweep. With ten owners, sweep counts are **5, 1, 10, 5**; the last sweep uses a second filler draw. Deduplicate count anchors for one/two-owner inputs. After the complete set of count/filler sweeps, advance every anchor by one modulo the owner count so later cycles cover all counts. Preserve placement and baseline filler streams for the same group within a cycle, allowing nested count targets; use a distinct second filler label. Charge every fresh request, including rejected/duplicate requests. Reset fresh ordinals for each arm.

## Exact diagnostics

Freeze scripts, source, fixtures and inputs before execution. Capture dirty checkout source before editing and preserve unrelated work. Build an isolated reference wrapper against the sealed comparison executable, a candidate harness from copied changed source against the same captured gameplay DLLs, and isolated tests. Builds are separately measured, each capped at 300 seconds, with no restore/gameplay build.

One reference invocation: produce a 128-evaluation synthetic old-variation report hash and 19 complete construction requests each for old group/count and old variation, using the retained captured inputs/mechanics and label 17. No engine entry is permitted.

Run exactly **79 backend cases through `build/run-tests.ps1`**: the existing 63 composition/joined/group-count/variation cases plus 16 diversity cases covering novelty ordering, metadata permutation, complete breadth/count cycles for 1/2/10 owners, uniform/empty fallback, large/invalid ordinals, nested placement/fillers, ownership rollback, count drift, deterministic bounded search, cancellation, duplicate charging, arm reset, explicit policy metadata/order prohibition and the captured old-variation hash. Existing old-policy golden hashes remain required.

One captured-input candidate invocation constructs exactly 19 requests each for old group/count, old variation and diversity (57 requests), compares the two old-policy outputs with the reference's 38 requests, and saves the complete diversity order and two cycles of the 214-group schedule: 1,712 guided positions plus the intervening uniform positions. Save detailed elapsed time/trace, first-19 legal/rejected outcomes, requested/placed/final counts and prefix coverage for evidence/core/Essence/group sets across all three policies. No candidate fitness callback for captured gameplay inputs.

One independent Python audit reconstructs canonical greedy order and stable tie seeds, all schedule positions, recipes/IDs, legality, exact owner counts for successful guided requests, nested/count/filler metadata, reference hashes and prefix coverage. Report rejected constructions honestly; no retry or expansion to obtain a favourable result. The expected structural benefit is more distinct early groups than four-request variation bundles, not a guaranteed combat improvement or selection of a particular control combination.

## Limits and completion

At most **300 seconds new diagnostic work** including preservation, reference, tests, construction, independent audit and publication. Carry forward **541.019 seconds** and the complete retained output chain within **1,800 seconds / 4 GiB cumulative**. New package cap **512 MiB**. Zero retries. On a failure, retain evidence and stop dependent diagnostics; fixes require separately frozen verification rather than replaying this package.

Preserve all 22 preceding sealed packages before/after. Publish measured prefix coverage, construction/ordering timings, compatibility results, commands, limitations and six active Markdown handoffs. Preserve all **482,506 reservations**, v19's 253 recipes and unused 512 confirmation values, fixed ability order, unchanged gameplay, historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption Hold. No new combat study, fresh allocation, Kharad tuning, configuration/migration/deployment or 129,536-fight confirmation.
