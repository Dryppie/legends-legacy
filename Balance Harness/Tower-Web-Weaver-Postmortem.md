# Web Weaver party: saved-evidence postmortem

**16 September 2026 — the extra attacks appeared, but the useful Seal timing did not improve.** The combined replacement increased the two changed characters' whole-fight damage, yet the party emitted more first-three-Seal waves and won fewer fights. The next useful tool change is a small, opt-in record of damage consumed by each Seal barrier before its pulse deadlines. That would make a future party hypothesis assessable; it is not evidence that another replacement will work.

This review reads the completed [64-pair comparison](Tower-Coordinated-Party-Comparison-Execution-Review.md). It introduces no new combat, values, builds, backend tests, replay, search variant or gameplay changes. The frozen package remains **MechanismNotDemonstrated** and closed. Adoption remains **Hold**. The analysis below is descriptive and post hoc; it does not replace the seven failed prospective gates or identify either replacement's individual causal effect.

## What the saved reports establish

The comparison changed `040e` slots 3 and 10 together, replacing Poisonous Rat and Cinder Beetle with Web Weaver Spider. All figures below compare the same 64 values, `040e` to the frozen candidate. Counts and damage are sums over complete fights, including losses.

| Changed slot | Basic Attack uses | Total character damage | Damage attributed to boss target |
|---|---:|---:|---:|
| 3 | 4,296 → 4,951 (+15.25%) | 211,818 → 215,864 (+1.91%) | 192,357 → 195,517 |
| 10 | 3,612 → 4,020 (+11.30%) | 147,410 → 158,863 (+7.77%) | 128,618 → 138,812 |

Slot 3 gained Basic Attack, Royal Venom and Toxic Opportunity damage, while losing 35,348 Toxic Bite damage and adding 20,050 Weaver's Grasp damage. Slot 10 lost 18,643 combined Burning Mandibles/Molten Shell damage and added 23,025 Weaver's Grasp damage. Royal Cocoon's recorded healing was unchanged; Vile Feast's increased. The original retained-healing intent therefore was not invalidated by an accidentally removed healing ability.

The two slots gained **15,499** total damage, while the other eight collectively lost **18,641**. Whole-party damage fell **3,142** (1,805,932 → 1,802,790); boss-target damage fell **874** (1,454,182 → 1,453,308). These totals depend on fight length, targets, barrier exposure and outcomes. They cannot tell us whether a character displaced another character's useful damage or caused its death. The reports also do not separate natural from forced basic attacks or provide Haste uptime. More attacks are consistent with the proposed mechanism, without isolating its cause.

The more informative observation is where the first three Seals crossed pulse boundaries. Each began at ticks 160, 320 and 480 respectively in every inspected fight. All 384 opportunities had a recorded end; none was missing or censored.

| Seal ordinal | Mean elapsed ticks, control → candidate | Total emitted waves, control → candidate | Actual breaks after at most one wave |
|---|---:|---:|---:|
| First | 31.921875 → 29 | 64 → 64 | 64 → 64 |
| Second | 43.796875 → 42.625 | 132 → 130 | 0 → 0 |
| Third | 48.34375 → 53.34375 | 127 → 145 | 25 → 18 |

One tick is 0.1 seconds. The first Seal broke about **0.292 seconds sooner**, but both parties still received its first pulse at offset +19 and avoided its second at +39. The second Seal never broke before elapsed tick 40 in either party: even the quickest examples missed the second-pulse boundary. The third ended half a second later on average and emitted **18 additional waves**; both parties had 61 breaks and three timeouts there. Its mean includes those timeouts and is not a break-only mean.

The second Seal saved only two waves across all 64 pairs. The net first-three total rose **323 → 339**, reproducing the original primary improvement of **-0.25 waves per fight**. Thus missing-opportunity penalties do not explain this failure. No friendly had died before the first or second Seal; two fights per party had a prior death before the third. This locates the timing problem but does not establish that the third Seal caused later deaths. Candidate victories remained 41/64 versus 44/64 and 47/64 for the anchors.

Evidence: [paired character rows](../TestResults/balance/tower-web-weaver-postmortem-20260916/paired-character-evidence.json), [paired Seal rows](../TestResults/balance/tower-web-weaver-postmortem-20260916/paired-seal-evidence.json), and [descriptive summary](../TestResults/balance/tower-web-weaver-postmortem-20260916/summary.json). First-death ownership in that summary allows ties; its counts are not mutually exclusive probabilities. First-death-or-fight-end times are censored and must not be interpreted as uncensored lifetimes.

## Smallest useful next implementation

Target the optional offline diagnostics used by **BalanceHarness**, at the existing engine observation boundary. [FastCombatEngine.ApplyDamage](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs) already receives the attacker, target, damage type/delivery, nullable effect and reporting labels. Its call to `ConsumeBarrierWithSources` returns each actually consumed contribution. [AbilityRuntime](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityRuntime.cs) preserves contribution source, effect, activation and application order. The current [mechanic collector](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatMechanicDiagnostics.cs) records starts, ends and linked periodic applications, but no intermediate consumption. Its barrier `SourceId` denotes the barrier provider, not the attacker. A break event's amount is the final consumed piece, not the full consumption history.

Add an explicitly enabled damage-consumption observation immediately after that consumption call, before callbacks or other event dispatch. Copy all returned contributions in their recorded order at that point, including fractional amounts before integer log rounding. Retain attacker ID, defender ID, barrier provider ID, barrier effect/activation/application order, tick, precise consumed amount, damage type/delivery, nullable attacking effect ID and an optional reporting label. Preserve list order for same-tick observations. Do not infer a canonical ability ID from a display name. Periodic or otherwise unidentified abilities need an explicit unknown-ability bucket while retaining their known attacker.

Use the existing generic filter and bounded collector; add no targeting, random draws, mitigation calculations or combat-event subscribers. The disabled path must not enumerate or allocate extra records. A new trace schema must be explicit; sealed schema-1 files stay unchanged. An exhausted event budget makes attribution incomplete and ineligible for conclusions, rather than silently dropping inconvenient hits.

Report consumption by attacker and ability where known, split at actual pulse events in engine order. Reconcile each matched barrier's accepted amount with consumption and its recorded remainder. Exclude health overflow and other barrier contributions from that Seal's total. A critical limit is that damage is not the only consumer: ability costs, combat styles and negative barrier adjustments can call `ConsumeBarrier` elsewhere. A damage-only observer must expose unexplained residuals and reject complete attribution when those paths apply; it must not relabel them as attack damage. Incomplete/censored activations likewise remain explicitly unreconciled unless a remaining-amount observation supports closure.

Proposed zero-combat fixtures should cover two-hit accounting, overlapping barriers, fractional consumption, health overflow, nullable periodic ability identity, guarded/redirected damage, non-damage residual rejection, and both same-tick pulse/break orders. Include event-cap failure and disabled/enabled ordinary-report parity on synthetic engine fixtures. Any future backend verification uses `build/run-tests.ps1`. These checks establish accounting and non-interference, not team strength. No such implementation or fixture execution occurs in this review.

## Decision and stopping rule

Do not derive a slot-10-only variant from these post hoc totals, retry the Haste package with more values, or assume pillar damage is a cheaper solution. Neither the causal contribution of one swap nor useful damage inside Seal windows is available in the current reports. Keep both compatible anchors available as practical starting recipes; this small diagnostic does not supersede their earlier matched practical evaluation or establish which anchor is stronger.

The observer is worth implementing only if it stays small, optional and behavior-preserving. If contribution totals cannot be reconciled or complete traces cannot fit a fixed cap, stop and report that limitation. After verification, any new diagnostic would require its own prospective scope and resource accounting with fresh values; this review authorizes none. Do not replay sealed fights to fill the gap. Before another party comparison, require a concrete account of which existing contribution can move which pulse deadline without removing needed survival support. Freeze the complete legal party and outcome-independent ability order, retain independent evaluation and a stopping rule, and abandon it if the prospective mechanism and outcome gates fail. Attribution can improve hypothesis quality; it cannot guarantee a stronger party.

## Preservation and accounting

The [completion receipt](../TestResults/balance/tower-web-weaver-postmortem-20260916/completion.json) carries the prior balances forward and charges this saved-evidence step under its fixed **10-second /2-MiB** local ceiling. No caps or transfers increase. All **483,720** exclusions remain; V19 retains 253 required recipes and 512 unused confirmation values, with reliability **Unresolved**. The diagnostic and pilot remain **Closed**.

Changed files are this review, one current notice in [the strategy handoff](Tower-Search-Strategy-Reset.md), and the isolated postmortem evidence package. Only one existing handoff is updated to keep publication within the fixed byte allowance. Saved-report arithmetic, sealed hashes, input pins, dirty-file preservation, new links and scoped whitespace are checked during publication. Builds, backend tests, benchmarks and combat are intentionally unrun for this analysis-only scope. Exploratory reads of two guessed source/evidence paths found no file; subsequent source discovery and manifest-driven evidence access resolved them, leaving no required check blocked. There are no gameplay, content, configuration, migration or deployment changes.
