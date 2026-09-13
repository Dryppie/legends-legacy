# Search hypothesis assessment: distinct elite loadouts

Completed **13 September 2026**. One source-grounded hypothesis is selected **for implementation planning only**: prefer distinct per-character loadouts in the four-member elite parent pool, grouping only within-character Essence-order variants. The [separate implementation plan](Tower-Loadout-Diversity-Plan.md) specifies proposed opt-in `independent-loadout-diversity-v11`. **No C# implementation, combat, constructor call, new seed or replay occurred.**

This follows the [completed v10 diagnosis](Tower-Stagger-Reservation-Diagnosis-Review.md). V10 still has reliability **Fail 0/3**, all twelve generated finalists **0/256**, strongest control **125/256 (48.83%)**, and ordinary/joint family **Inconclusive / Inconclusive**. Retained nominal control capacity did not establish competitive discovery. This assessment does not change those conclusions or erase earlier ceiling breaches.

## Source rationale and frozen rule

The [current generation kernel](../LL/tools/BalanceHarness/TowerBossGeneration.cs) builds its elite beam with `Rank(measurements).Take(BeamSize)`, where `BeamSize` is four. Ordered recipe IDs remain distinct, and the [order mutation](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs) changes ordering without necessarily changing which Essences each character carries. Multiple order variants can therefore occupy elite positions. The existing four-member exploration pool seeks different capability patterns first, then other ordered recipes; it is selected with one-in-four probability when available. This proposal does not claim that exploration is absent.

The candidate rule was written into the [protocol](../TestResults/balance/tower-loadout-diversity-assessment-20260913/protocol.json) **before inspecting historical parent-pool exposure**. Rank all current-arm measurements by the unchanged combat rank. Scan that order, keeping the first representative of each signature until four members are selected. If fewer signatures exist, append remaining ordered recipes in rank order until the same capacity is filled. The signature retains numeric party-slot identity and the multiset of Essence IDs on each character, sorting IDs ordinally within each slot. It ignores only Essence ordering within a character; it does not merge characters, equipment assignments, Essence variants or quantities.

Grouping is solely a parent-selection preference. All ordered recipes retain their distinct cache IDs and combat results. The best-ranked recipe stays first; the representative of a loadout can change as stronger orderings are evaluated. The [combat engine](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs) iterates ready abilities in their stored order, so grouped recipes must never be treated as combat-equivalent. Construction, mutation portfolio, objective, final ranking and primary selection remain unchanged. No provider IDs, reference counts, timing deadline, fitted distance threshold or new weights define the rule.

## Saved independent-prefix assessment

The audit read all **576 evaluated independent recipes** in the six saved v10 discovery arms. At each recorded proposal with parents, it compared pools using **only evaluations already completed before that proposal**. Rejected proposals remain part of that decision history. It also recorded six final pools. There were **335 parent-decision snapshots**, with **79** showing duplicate elite loadout signatures while other signatures were available.

| Saved arm | Restart | Parent decisions | Decisions with changed pool membership | Final distinct loadouts among four: existing / proposed |
| --- | --- | ---: | ---: | --- |
| `compatible-defense-joint` | `-76005071` | 57 | 18 | 3 / 4 |
| `stagger-reservation-joint` | `-76005071` | 55 | 17 | 3 / 4 |
| `compatible-defense-joint` | `-129820605` | 56 | 30 | 4 / 4 |
| `stagger-reservation-joint` | `-129820605` | 55 | 3 | 3 / 4 |
| `compatible-defense-joint` | `-127588879` | 54 | 6 | 4 / 4 |
| `stagger-reservation-joint` | `-127588879` | 58 | 5 | 4 / 4 |

For the three **v10 new arms**, this is **25/168 parent decisions** (17, 3 and 5). Their existing beams had three distinct loadouts at these decisions; the candidate admits the saved fifth-ranked recipe. Across the three comparator arms, 54/167 decisions change, sometimes admitting rank six. These are repeated observations within six histories, not independent samples or an efficacy rate. No new confidence interval is calculated.

Each arm still evaluated 90 or 91 distinct unordered loadouts among its 96 ordered recipes. The issue is temporary elite occupation, not a claim that the entire search generated only a few loadouts. Most recorded parent decisions have four distinct elite loadouts already. There is no evidence here that changing these remaining decisions will find winners.

The [pool snapshots](../TestResults/balance/tower-loadout-diversity-assessment-20260913/pool-snapshots.json) retain original/proposed recipe and proposal IDs, available signature counts, displaced IDs and admitted recipes' original discovery ranks and fitness. The [assessment](../TestResults/balance/tower-loadout-diversity-assessment-20260913/assessment.json) summarizes all arms. The procedure never samples parents or produces children. After the first changed selection pool, later saved prefixes are **not a counterfactual v11 history**; their measurements would not necessarily exist in a new run.

## Decision and limitations

The declared decision criterion is satisfied: source establishes a narrowly defined mechanism, deterministic checks preserve beam capacity and the best-ranked member, and saved independent search shows duplicate-signature occupation with alternatives available. This supports an isolated implementation comparison. It is not an estimated win-rate improvement or the diagnosed cause of persistent search failure.

The [decision record](../TestResults/balance/tower-loadout-diversity-assessment-20260913/decision.json) retains the counterarguments: order variants can be useful; existing exploration may already cover the alternatives; admitted recipes can be weaker; these signatures do not represent every kind of strategic diversity; and construction or the limited budget may remain the main constraint. Changing the beam also changes the input to the unchanged exploration selection and the recombination candidate union. Those are explicit consequences, not additional tuned mechanisms.

No alternative hypothesis was assessed or selected. In particular, this does not introduce recovery quotas, a conditional-control filter, extra stagger margins, mutation repair, a larger beam, a new objective, order-insensitive cache reuse or a diversity-based replacement of validation primaries. All saved controls remain outside independent generation and outside the assessment algorithm. Receipts containing historical reference evidence were hashed only for preservation.

## Verification and preservation

The [abstract checks](../TestResults/balance/tower-loadout-diversity-assessment-20260913/abstract-verification.json) exhaustively enumerate **87,381 ranked group-label sequences** of lengths zero through eight over four abstract groups. A separate first-occurrence oracle checks capacity, uniqueness, rank-one retention, maximal available group coverage, fallback and deterministic output. Twelve order permutations plus four slot/multiplicity/delimiter checks verify the signature definition. These are abstract selector checks, not production implementation tests or sampled parties.

Preparation verified **18 preceding packages**, **76 sealed reviews**, **2,237 C# sources**, five assemblies, 16 content files and two catalogs. The [final receipt](../TestResults/balance/tower-loadout-diversity-assessment-20260913/final-verification.json) additionally checks every archived prefix through an independent grouping oracle, Markdown links/anchors, the frozen plan snapshot and `git diff --check`. The all-array [ledger](../TestResults/balance/tower-loadout-diversity-assessment-20260913/seed-ledger.json) is byte-identical and remains **471,925 seeds**. Prior **243/243** implementation test results are hash-verified; backend tests were not rerun because source and assemblies are unchanged. No required command remains blocked.

Machine assessment took **0.27 seconds**, below the frozen 120-second limit; storage stays below 64 MiB. Preparation, documentation and final verification are timed separately. Changed files are this review, the new implementation plan, active status/handoff Markdown and the README, plus this new local assessment package. All historical reviews and sealed artifacts remain untouched.

Next implement and verify the isolated opt-in selector under the [plan](Tower-Loadout-Diversity-Plan.md). Any constructor parity/probe work needs explicit bounds before execution, and a subsequent combat pilot needs a separate frozen protocol. This assessment allocates neither. Kharad remains **Health 3.04881408 / Power 3.85370128**, with the same fixed gear, untrained/unevolved Essence budget and hypothetical ownership. No migration, configuration change, deployment, default/catalog promotion or floor expansion is included.
