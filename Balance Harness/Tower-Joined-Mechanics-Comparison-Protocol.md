# Joined-mechanics comparison: frozen bounded pilot

15 September 2026. Authorized preparation following the joined-mechanics implementation. Target: offline BalanceHarness, captured-v19 gameplay assemblies and the unchanged captured guardian Health/Power +10% candidate. No gameplay tuning, order search, historical archive modification, deployment or old-cap increase. A separate explicit exception for **45 new values and at most 512 fights** is required before allocation/execution; the previous pilot's authorization is exhausted.

## Exact comparison

Two independent policies: baseline `independent-composition-only-v1` / `composition-only-joint`, candidate `independent-joined-mechanics-v1` / `joined-mechanics-joint`. Both receive **44 distinct evaluated teams**, at most **704 proposals**, FreshEvery 4, initial 11 fresh teams, one shared generation-root value. Each policy retains its own defined random stream; this compares the policies as implemented, not identical random draws or an isolated causal effect of joining. Neither gets control recipes/outcomes in generation. Run baseline discovery before candidate discovery; timing is descriptive, with no randomized performance claim.

Use identical captured eligible pool (80 Essences), equipment, attributes, neutral actor/Essence identities, UTC timestamp, floor 5, ten characters forming two five-player parties, five Essence slots each, level 40/tier 1/rank 2, and unrestricted per-Essence owned copies. Fixed ordinal Essence ID order everywhere. Use identical scenario identity and four paired discovery seeds for both policies. No prior combat results are pooled into this comparison.

| Phase | Teams | Fights per team | Maximum fights |
| --- | ---: | ---: | ---: |
| Baseline discovery | 44 | 4 | 176 |
| Joined discovery | 44 | 4 | 176 |
| Screen | Top 2 per policy, up to 4 distinct recipes | 8 shared | 32 |
| Confirmation | Top 1 per policy plus 2 fixed controls, up to 4 recipes | 32 shared | 128 |
| Total | | | **512** |

Per-policy discovery ranking uses the existing fitness and ordinal identity tie-break. Freeze both top-two lists before screening. Rank within each policy by screen wins descending, then that policy's discovery rank, then ordinal identity. Freeze both winners before confirmation. Deduplicate identical full recipes only across screen/confirmation members with matching complete context, retain every policy/rank/control origin, and do not refill freed seats. Discovery overlaps are evaluated independently and charged twice within the 352-fight bound. Any incomplete discovery stops the pipeline.

The two controls are `team-040e60d3dbc5c127321653c47ed3a9d3` and `team-49f6979895354870c89362d4abf214bb`, with fixed ordinal Essence order. They remain outside both searches and the screen. Shared confirmations use the same seeds and complete non-Essence context across all measured recipes.

## Schedules, accounting and interpretation

Proposed new allocation: **1 generation + 4 discovery + 8 screen + 32 confirmation = 45 values**, mutually disjoint and outside the complete current reservation union of **482,371**. Successful binding would retain **482,416** reservations. Refresh the registry and refuse binding if it changed. Preserve all historical reservations, including unused values. Journal every allocation attempt durably and retain pending reservations on failure. Never use preflight fixture labels for combat.

Primary contrast: joined finalist minus baseline finalist on 32 paired confirmations. Four secondary contrasts compare each finalist with each control. Retain all five, including zero/negative results. Use existing Wilson bounds: per-recipe rates with family factor 8; each of the two discordance proportions in each of five differences with factor 20. This reserves up to half of total alpha for four rates and half for ten discordance bounds. For the same deduplicated recipe the difference is exactly zero. Draws are non-wins. Report broad uncertainty; do not infer reliability, general search improvement or optimality from one shallow restart. No reselection after confirmation.

Hard caps: **512 charged combat attempts including all repeated measurements, 1,800 seconds of diagnostic workload, 4 GiB total new output, zero retries/resumes/replays**. Preparation consumes this new comparison's time/output allowance. Preserve its charges at execution. Allow at most 900 seconds for native combat execution, with the outer remaining-time limit able to stop sooner; reserve time for archive verification. Use the existing compact campaigns, incremental storage accounting and durable attempt journal. Verify native discovery/balance archives without combat and independently recount the final evidence. Keep all recipes, proposals, construction traces, selections, failures and performance timings.

## Exact preparation checks before approval

Freeze scripts/source/test hashes before each once-only diagnostic. Use the sealed joined-mechanics implementation source and captured gameplay DLLs. No full dirty gameplay build. Compile the comparison driver and isolated tests, then invoke `build/run-tests.ps1` with the existing composition/joined tests and the new comparison tests. Tests use synthetic outcomes and a combat-entry guard; no allocator is invoked. Test exact per-policy/global costs, schedule disjointness, shared inputs, separate nominations, screen tie-breaks, deduplication/origin retention, paired intervals and durable interruption charging.

One native preflight call refreshes the existing reservation registry, uses 45 already-reserved labels only in explicitly non-runnable fixtures, validates both definitions/content, checks identical generation inputs except policy/method, and prepares the two fixed controls without combat. Save 66 interval fixtures (all 0..32 wins with factors 8 and 20), execution/gameplay identity and participant descriptions. No real-content search or candidate fights during preflight. One independent audit verifies the ledger union, exact definitions, shared scenario/context, preparation outputs, intervals and passing tests. Preparation diagnostic cap 300 seconds, within the 1,800-second total; new preparation output at most 1 GiB, study at most 3 GiB. Compilation is separately measured engineering work, with 300-second command limits. No diagnostic retries: preserve and report any failure instead of silently rerunning.

Finish the runnable controller, freeze the preparation package and update active Markdown before requesting the specific allocation/execution exception. No allocation, bound study or combat is authorized by this protocol alone. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption Hold remain unchanged.
