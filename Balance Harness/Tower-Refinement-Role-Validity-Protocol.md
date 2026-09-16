# Refinement role validity: frozen zero-combat verification

16 September 2026. Target: offline `LL/tools/BalanceHarness`. This follows the sealed refinement comparison's two `missing-team-roles` rejections. It does not authorize another comparison or seed allocation.

Implement opt-in `independent-discovery-refinement-roles-v2`, retaining v1 exactly. Distribution constructs a role-preserving subset of the sampled target slots; refinement selects a provider from one bounded pass over the sorted pool; whole-character replacement retains the removed owner's otherwise absent role providers. Final family, inventory, canonical-order and role checks remain mandatory. No extra proposals, random retries, combat evaluations or ability-order changes are allowed. An unchanged party is still a charged duplicate. The current comparison launcher continues to use v1; integration into a separately frozen future comparison is outside this diagnostic.

## Frozen execution

One package: `TestResults/balance/tower-refinement-role-validity-20260916`. Its setup manifest pins the scripts, protocol, candidate sources, test sources, previous executable assets and the three captured discovery inputs. Reuse the gameplay assemblies from `tower-refinement-storage-profile-20260916`; compile only the harness and isolated test assembly. Do not rebuild gameplay or overwrite ordinary binaries.

Run each phase once, in order, using the captured `workflow.py`: freeze (4 seconds), harness build (20), test build (12), tests (25), captured-data diagnostic (15), audit (8), publication (8). Total maximum **100 additional diagnostic seconds** and **20 MiB new retained output**, including preparation products, compiler intermediates and test results. Existing cumulative ceilings remain **3,600 seconds / 4 GiB**. Previous authority: `tower-refinement-comparison-closeout-20260916/completion.json` (3,317.506469455596 seconds). Charge every executed phase, including failures and publication. On first failure or limit, preserve evidence and skip all dependent execution; zero retries.

Exactly **20 backend tests** through `build/run-tests.ps1`: the existing 12 `BalanceHarnessDiscoveryRefinementTests` and 8 new `BalanceHarnessRefinementRoleTests`. The new tests cover actual distribution targets, alternative role providers, whole-character roles, shared inventory, two deterministic synthetic searches, the 16-attempt stop, cancellation charging and v2 input bounds. A combat-start guard rejects any engine entry.

The separate captured-data diagnostic performs:

1. One v1 search reconstruction using only the sealed discovery measurements, matched by party ID; require exact generation hash parity, 16 proposals and 14 evaluations. This is a saved-data reconstruction, not a combat replay.
2. Inspect exactly the two rejected proposals (indices 4 and 13), identify their absent roles, and exercise v2's relevant construction helper on those saved parent/edit inputs. Require roles, family legality, inventory legality and canonical order afterward. Persist before/after party hashes, missing roles and actual targets/provider change.
3. Two v2 searches with the captured input seed and clearly synthetic deterministic scores; reverse metadata for the second. Require identical result hashes, no missing-role rejection, complete valid ancestry and all ordinary final checks on evaluated parties. Report actual evaluation and rejection counts without assuming 16 distinct teams or a strength improvement.

Audit source/input pins, executable gameplay hashes, the failed study's sealed file inventory, unrelated dirty-file hashes and scoped whitespace. Publish a new review and update the six active handoffs. Preserve the original failed study, all **482,866 reservations**, its **40 unused values**, v19's separate **512 unused values** and **253 recipes**. No fresh seeds, fights, preparations, selection, confirmation, boss/content tuning, deployment or old-cap changes. Adoption remains Hold; no new reliability claim.

## Reproduction

From the repository root, use the bundled Python with `-B` and the package's `workflow.py`, passing the phases above one at a time. The package is once-only; its captured sources, commands and asset hashes describe reproduction in a new explicitly bounded directory. Never rerun a sealed package.
