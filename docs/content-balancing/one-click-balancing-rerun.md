# One-click combat balancing rerun

## Outcome

The Admin Dashboard World Tower campaign is now the guarded one-click entry point for rerunning balance evidence after Essence, ability, equipment, profile-generation, or Tower-content changes.

One click can:

1. Capture separate discovery and Tower-qualification/materialization fingerprints.
2. Discover Essence teams as real five-character parties with the canonical Guardian, Restorer, Striker, Striker, and Controller roles and role-specific equipment attributes.
3. Reuse completed discovery audits only when their request and actual role-aware discovery-build fingerprint are unchanged.
4. Qualify each eligible finalist against the exact target Tower floors with a bounded ten-sample production-runtime pass before choosing profile families.
5. Rebuild the 13 exact Tower profile scenarios when target equipment, Tower, guardian, or materialization inputs changed.
6. Reuse an already validated profile catalog only when both discovery and qualification/materialization inputs are identical.
7. Validate the candidate catalog in memory without modifying the approved source-controlled catalog.
8. Run a candidate smoke test through the production World Tower runtime.
9. Run deterministic candidate certification with common seeds.
10. Persist the campaign, audit reports, qualified catalog, catalog validation, smoke report, and certification report as one evidence bundle.
11. Unlock certified-candidate export only when all automated gates pass.

The flow never changes Tower recommendations and never promotes a catalog automatically. Promotion remains a deliberate source-control decision.

## Dependency decision

| Changed input | Automated action |
| --- | --- |
| Essence definitions, including rarity, tags, bonuses, ascension, evolution, or ability mapping | Run the five discovery audits, rebuild profiles, smoke, and certify |
| Abilities, statuses, summons, audit algorithm, simulator contract, combat rules, or discovery equipment output | Run the five discovery audits, rebuild profiles, smoke, and certify |
| Target equipment/profile output only | Reuse compatible audits, rebuild profiles, smoke, and certify |
| Tower floors, guardians, creature abilities, guardian Essence loot, region scaling, or recommendations | Reuse compatible discovery audits, requalify and rebuild profiles, then smoke and certify |
| No balance inputs changed | Reuse compatible audits and profiles, then smoke and certify |

Audit requests must also match exactly. Changing seeds, pool size, battle counts, finalist count, or discovery context prevents reuse of the affected audit even when the content fingerprint is unchanged.

## Fingerprints

The discovery fingerprint contains:

- Complete authored abilities, statuses, and summons.
- Complete Essence definitions rather than only Essence-to-ability IDs.
- Audit and simulator algorithm versions.
- Combat-rule, preparation, and canonical-roster versions.
- Deterministic projections of the Guardian, Restorer, Striker, Striker, and Controller discovery builds, including base attributes, equipment identity, generated modifiers, tempering state, and rating.

The materialization fingerprint contains:

- The same complete combat content.
- Profile schema and generator versions.
- Power Rating, combat-rule, preparation, and canonical-roster versions.
- Deterministic projections of every role/build used by every requested Tower scenario.
- Exact requested Tower definitions, guardian combat definitions, guardian-native abilities, guardian Essence loot mappings, and region creature-scaling content.
- The bounded Tower-qualification contract and sample count.

Fingerprints are checked again before every new audit and before catalog generation. A process restart after edited content therefore fails the old run closed instead of mixing old and new evidence. Retry is refused when either fingerprint changed; a new one-click run must be created so the dependency planner can make a safe reuse decision.

## Candidate verification

Before profile families are selected, every evidence-eligible audit finalist receives a bounded context qualification. For each exact floor assigned to the scenario, the qualifier:

- Materializes the finalist with the scenario's exact equipment tier, rarity, quality, roster size, party numbers, and canonical roles.
- Uses the production snapshot preparation pipeline, World Tower runtime factory, guardian scaling, stagger rules, cooldown policy, and playback executor.
- Runs ten deterministic samples from a persisted seed manifest.
- Records wins, losses, draws/timeouts, duration, runtime flags, and seed-manifest provenance in each selected party's catalog evidence.

Meta and Budget selection prioritize the best worst-floor and average Tower results. Typical selection targets the scenario's intended success band. Weak-but-Legal selects the lowest qualified result. PvP audit score remains a deterministic secondary signal. Role-specialist and no-Essence controls do not claim qualification earned by a differently composed party.

Candidate shadow and certification accept a validated in-memory catalog and record `Candidate` plus the campaign identity in report provenance. They do not read or write the approved catalog.

Default campaign verification is:

- 10 samples per team/cohort for smoke coverage.
- 100 samples per team/cohort for certification.
- Exact floors 1–15, exact roster sizes, and Expanded portfolios.
- Shared deterministic seed manifests for canonical and profile cohorts.
- Existing confidence, monotonicity, spread, timeout, and scenario gates.

A structurally completed campaign can still be promotion-blocked. `isPromotionReady` is true only when the catalog is valid, candidate smoke completed, certification completed, and certification passed.

## Expected runtime

The expensive global search remains five role-aware five-character discovery audits. With the default 24 finalists and floors 1–15, preselection qualification adds about `24 × 15 × 10 = 3,600` production Tower battles across the 13 scenario generations. It does not repeat global Essence discovery for every floor.

After catalog generation, the default Expanded portfolio has ten teams per exact floor. Smoke uses ten samples and certification uses 100 samples per selected team/cohort. These gates intentionally test the much smaller selected population rather than every discovered combination.

## Stored artifacts

Each schema-4 campaign directory contains:

- `campaign.json`
- `audits/*.json`
- `catalog.json`
- `catalog-validation.json`
- `candidate-smoke.json`
- `candidate-certification.json`

The evidence export includes every artifact. Reused audits identify their source campaign and original content hash, and a reused catalog identifies its source campaign. A reused report is normalized to the current catalog content hash only after the separate discovery fingerprint proves that its battle inputs are unchanged.

## Operator procedure

1. Make the Essence, equipment, Tower, guardian, ability, or scaling change and restart the Admin API so it loads the current content.
2. Open Combat Diagnostics.
3. Leave candidate verification enabled and select the desired sample counts.
4. Select **Run Complete Balancing Flow**.
5. Wait for `Completed`.
6. Review certification issues and the exported evidence.
7. Export the certified candidate only when the dashboard reports **Ready for human promotion review**.
8. Commit the reviewed JSON through normal source control. No database or deployment action is performed by this tool.

Fingerprint-contract-v2 and older campaigns remain historical evidence. Because role-aware discovery and Tower-context qualification changed the evidence meaning, the first run under fingerprint contract v3 must regenerate discovery and build a new catalog.
