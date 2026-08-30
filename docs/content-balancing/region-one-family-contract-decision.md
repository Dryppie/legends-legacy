# Region 1 Affected-Family Contract Decision

Status: **Author review required; no new contract is configured**  
Evidence baseline: **balance schema 46, reliability analyzer v18, population policy v3**  
Scope: **Regeneration and DistributedAttrition only**

## Purpose

The Region 1 reliability audit confirms physical diagnostic recovery for Regeneration and DistributedAttrition across three populations, but it does not confirm either mechanic's original named-family premise. This decision separates the author-owned encounter intention from the already-complete empirical audit. It does not authorize another threshold search, change production content, or promote either composite fault above `Inconclusive`.

The required decision for each mechanic is one of:

1. approve an absolute physical family-response contract and supply its authored limits;
2. explicitly declare that no affected-family contract applies, with that exception recorded in the expansion decision; or
3. retain `InsufficientEvidence` and keep the Region 2+ expansion gate closed.

Reinstating a named-family winner from the existing population results is not supported.

## Evidence that is already settled

| Mechanic | Confirmed diagnostic | Rejected family shortcut | Current result |
| --- | --- | --- | --- |
| Regeneration | The detached ability-healing fault raises Guardian self-sustain by `1.44×`, `1.52×`, and `1.50×`; the system recovers Regeneration on all three populations | “SingleTargetSpecialist must be the strongest family.” The outcome leader changes between SingleTargetSpecialist, IntendedBalanced, and Defensive; generic damage and net-margin rankings also reverse | Diagnostic `Confirmed`; family `InsufficientEvidence` |
| DistributedAttrition | Non-primary damage rises by at least `1.15×`; the exact injected effect deals positive damage with five-target breadth; PartyAttrition is recovered on all three populations | “Defensive must outperform IntendedBalanced.” Censor-aware survival, sustain, prevented damage, and the preregistered burden-mitigation cohort reverse between populations | Diagnostic `Confirmed`; family `InsufficientEvidence` |

The evidence rules out selecting another family or threshold by inspecting the same measurements. A new contract must express authored encounter intent in physical units and then survive independent replication.

## Recommended Regeneration contract shape

Use an absolute **Guardian depletion feasibility** contract. Do not require a named family to win.

For every tested roster, retain these physical measurements:

- Guardian damage taken per second;
- Guardian passive regeneration, ability healing, and total self-sustain per second;
- net Guardian depletion per second (`damage taken - self-sustain`);
- projected depletion time from the observed net rate;
- actual clear, duration, deaths, and remaining health; and
- the roster's SingleTargetSustained capability distribution as explanatory evidence only.

The author must decide:

- the encounter's acceptable Guardian-depletion or completion-time window;
- whether a positive reserve above the minimum depletion rate is required;
- whether any named party family is intended to satisfy or fail that window; and
- whether the rule is a release requirement or a diagnostic warning.

Recommended acceptance semantics after those limits are authored:

1. the healing injection must continue to meet the existing physical recovery gate;
2. projected depletion time must worsen coherently as the injected healing dose rises;
3. each family is classified by the share of valid unique rosters meeting the absolute window, rather than by which family happens to rank first;
4. the authored expected-family disposition must hold on every protocol-compatible population; and
5. failure, reversal, incomplete family material, or a missing limit returns `InsufficientEvidence` or `Fail` without threshold revision.

This design treats regeneration as an authored damage-versus-sustain check. It avoids redefining the requirement when the generated population changes.

## Recommended DistributedAttrition contract shape

Use an absolute **party survival envelope**. Do not require Defensive to beat IntendedBalanced.

For every tested roster, retain these physical measurements:

- non-primary damage taken per second;
- exact injected-effect DPS, hit count, activation waves, and target breadth;
- effective party sustain and prevented damage;
- average friendly health-deficit ratio;
- first-death event rate and censor-aware restricted mean first-death-free ticks;
- deaths and remaining health; and
- AttritionResilience and PartySustain capability distributions as explanatory evidence only.

The author must decide:

- the minimum acceptable first-death-free window;
- any maximum acceptable average health burden;
- whether clearing with deaths is acceptable for the encounter;
- whether any named party family is intended to satisfy or fail the envelope; and
- whether the rule is a release requirement or a diagnostic warning.

Recommended acceptance semantics after those limits are authored:

1. the existing direct injected-damage and PartyAttrition recovery gates must pass first;
2. burden must worsen coherently as the injected distributed-damage dose rises;
3. each family is classified by the share of valid unique rosters meeting the absolute survival envelope;
4. the authored expected-family disposition must hold on every protocol-compatible population; and
5. a saturated probe, incomplete family material, population reversal, or missing physical limit cannot produce a pass.

This design lets an encounter require broad-party survival without assuming that the code-owned `Defensive` label is a universal proxy for attrition resilience.

## Evidence and replication policy

After the author supplies limits, validation must use one preregistered schema-46 protocol across at least three distinct master populations. The complete upstream population descriptor must match exactly. Release review should use at least the documented release-candidate family budget of 15 valid unique rosters per family and 10 common seeds per roster unless a separately approved policy replaces it.

The analyzer must continue to report diagnostic recovery separately from the family contract. A diagnostic pass cannot compensate for a missing or failed family premise, and a family failure cannot erase proof that the injected mechanic physically reached combat.

## Recommended author decision

Approve the two absolute contract shapes above, but do not configure numeric limits or named-family expectations until the intended encounter duration, acceptable death policy, and desired specialist behavior are explicitly authored for the affected floors.

Until that approval and those limits exist:

- keep both family verdicts at `InsufficientEvidence`;
- keep both composite reliability results at `Inconclusive`;
- do not run another empirical threshold search from the rejected proxies; and
- keep the Region 2+ expansion gate closed.

## Approval checklist

- [ ] Regeneration completion/depletion window approved.
- [ ] Regeneration reserve and death policy approved.
- [ ] Regeneration expected-family disposition approved or explicitly marked not applicable.
- [ ] DistributedAttrition first-death-free window approved.
- [ ] DistributedAttrition health-burden and death policy approved.
- [ ] DistributedAttrition expected-family disposition approved or explicitly marked not applicable.
- [ ] Release versus diagnostic authority approved for both contracts.
- [ ] One schema-46 population protocol preregistered for the confirmation panel.
