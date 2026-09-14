# Kharad search diagnosis at the applied setting

Completed 13 September 2026. Target: saved evidence from the offline BalanceHarness. **The evidence identifies loss from final ranking in two restarts, alongside weak or unresolved candidate quality.** The next selected change is an [independent finalist rescreen](Tower-Finalist-Rescreen-Plan.md): evaluate a fixed top-32 v13 shortlist on 64 fresh seeds before freezing its two finalists. Implementation and a separately frozen comparison remain next; neither ran during this diagnosis.

All **2,304 calibration discovery evaluations** were joined to their complete typed recipes and later full-family measurements: **2,016 distinct recipes**, with 288 repeated associations across arms. All seven source packages and **117,689 artifact hashes** verified. This work used **zero new fights, seeds or constructor calls**. Kharad remains locally applied at **Health 3.5366243328 / Power 4.4702934848**; the 17,821-recipe composite **Pass**, its 18 supported viable recipes, and independent-search **Fail 0/3** remain unchanged.

## Complete candidate trace

The [complete join](../TestResults/balance/tower-kharad-search-diagnosis-20260913/candidate-joins.json) retains every arm, generation seed, candidate ID, exact recipe hash, evaluation position, discovery rank/fitness, winning discovery seeds, operator, parents, original nomination and later measurements. Full recipes remain in the original calibration export and complete-family archive; no recipe was promoted to a catalog or independent-generation input.

| Method | Generation seed | Candidates with discovery wins | Candidates with later observed wins | Primary: calibration / later confirmation |
| --- | ---: | ---: | ---: | --- |
| Deeper coverage | 1144935365 | 0/384 | 0/384 | 0/512; 0/192 |
| V13 loadout composition | 1144935365 | 6/384 | 9/384 | 7/512; 0/192 |
| Deeper coverage | 528558651 | 0/384 | 0/384 | 0/512; 0/192 |
| V13 loadout composition | 528558651 | 11/384 | 22/384 | 18/512; 8/192 |
| Deeper coverage | 1031543897 | 0/384 | 0/384 | 0/512; 0/192 |
| V13 loadout composition | 1031543897 | 0/384 | 0/384 | 0/512; 0/192 |

Every discovery winner recorded **exactly 1/8**. In total, 2,287 evaluations had zero discovery wins. All 31 distinct later-positive recipes came from two v13 restarts. Full arm results, nomination details, operator counts and rank-cutoff counts are in [arm-summary.json](../TestResults/balance/tower-kharad-search-diagnosis-20260913/arm-summary.json).

The 2,016 distinct recipes have unequal final sample sizes: **1,973 use 24 trials**, and **43 use 192 trials**. Twelve original finalists additionally retain their separate 512-trial calibration results. The later design promoted every non-anchor with at least one first-stage win and directly confirmed the declared anchors. Therefore its 192-trial subset was selected, not a random sample of generated teams. Counts above describe saved observations; they do not pool samples or estimate an overall generator win rate.

## What final ranking lost

The unchanged ranking orders candidates by discovery win rate, then lower guardian health, higher survival, shorter victory duration and ordinal candidate ID. With six or eleven candidates tied at 1/8, the tie-breakers determined which two received calibration confirmation. The original nominations were reproduced exactly; there is no evidence here of an implementation error in that ordering.

The later complete-family confirmation exposes the following misses on **the same 192 seeds** within each row group:

| Restart | Discovery rank | Saved recipe | Wins / 192 |
| --- | ---: | --- | ---: |
| 1144935365 | 1, original primary | `team-940a5f6e458f9a4e87012b189b5c68b2` | 0 |
| 1144935365 | 2, original secondary | `team-445c39796a28df7b8ef763eea1f36a0a` | 23 |
| 1144935365 | 3 | `team-c53d90b1fd1fc04e09bacf2249e5c2cf` | 24 |
| 1144935365 | 4 | `team-67fb06e6cccf1368a1feb0c0ae5a052a` | 24 |
| 1144935365 | 8, zero discovery wins | `team-7a4d03f61015ad3aae6637ad7941aacf` | 22 |
| 528558651 | 1, original primary | `team-cbb3a3cfe4d9ab67bdf4e40c53ee5eac` | 8 |
| 528558651 | 2, original secondary | `team-fe4cf21e09c4c00ae33fc2adac101fa1` | 5 |
| 528558651 | 3 | `team-a432afc1f82790236b9a3b0a8bf4a4f1` | 29 |
| 528558651 | 5 | `team-9be7f494d6f52913c0d8be4cc411361c` | 36 |

These are **post-hoc descriptive comparisons**, not a newly certified superiority result. The later outcomes cannot retroactively replace frozen primaries or turn the historical reliability failure into a pass. Likewise, eight-seed ties and outcome changes show limited ranking resolution, without proving a particular number of added seeds would solve it.

Top 32 by the original discovery rank includes all nine later-positive candidates in the first v13 restart and 13 of 22 in the second, including the strongest observed candidates. The remaining second-restart positives have lower observed rates; they remain in the complete inventory. Width 32 is an informed engineering proposal after inspecting this archive, not a validated optimum or exhaustive shortlist.

## What selection alone has not solved

No generated recipe in this calibration has a final composite adjusted lower bound ≥10%. The strongest lower bound is **9.84659%**, from the rank-five candidate at 36/192. Thus the complete-family Pass's **18 supported viable teams came from other saved sources**, not these 2,016 distinct calibration-generated recipes.

The original three primaries all failed **both** viability and supported improvement over the deeper-coverage comparator. All three passed the fixed-anchor recovery component; that anchor itself won zero of 512 trials at this setting. This is not an anchor-recovery bottleneck. The unchanged comparator also had zero wins, so a higher point estimate alone was insufficient for the declared adjusted improvement requirement. The [reconstructed findings](../TestResults/balance/tower-kharad-search-diagnosis-20260913/findings.json) retain each component and its original bounds.

All three deeper-coverage arms and the third v13 arm have no observed wins in either discovery or final full-family rows. Most candidates had only 24 later trials, with a very wide family-adjusted upper bound of about **49.20%** for 0/24. These observations do not prove zero true win probability, an exhaustive absence of viable candidates, or a specific construction bug. They do show that a reliable generator has not been established.

The successful observations also emerged late. The first v13 discovery win appeared at evaluation **319/384** in restart 1144935365 and **309/384** in restart 528558651; all 17 discovery-winning evaluations were in the final quarter. The earliest candidate with a later observed win appeared at evaluations 316 and 264 respectively. This motivates a separate search-depth hypothesis, but the fixed archive cannot show which candidates a longer run or different parent ranking would construct.

Rescreening is selected first because final ranking can be changed and tested while preserving the candidate population. Increasing depth, changing parent fitness or adding another construction heuristic at the same time would prevent that attribution. The proposed comparison can fail; no improvement or eventual reliability pass is promised.

## Concrete next implementation

Implement the [finalist-rescreen plan](Tower-Finalist-Rescreen-Plan.md) as an opt-in offline workflow. Keep all generation and game content fixed. Freeze 32 v13 candidates per restart, measure each on 64 independent shared seeds, and rank by fresh wins with original rank as the tie-breaker. Preserve original and rescreened finalists and confirm their union alongside unchanged comparators and controls.

The proposed comparison caps at **57,344 fights**, with zero retries, and requires fresh schedules and a separate protocol before execution. Its ordinary case has at most 38 confirmation recipes and 44,032 fights. It reports the original three-part reliability gate and a separate paired benefit against the original v13 primary. Adoption requires both gates to pass in at least two of three restarts. No fights or seed reservations have been allocated here.

## Verification and preserved scope

The standalone [analysis script](../TestResults/balance/tower-kharad-search-diagnosis-20260913/analyze.py) has no simulator or constructor entry point. It verified exact inventories and hashes for calibration, calibration work, complete family, complete-family work, precision, precision work and checked application. It then:

- Recomputed typed recipe identities for all 2,304 associations, matched their complete party recipes to the full family and checked saved source provenance.
- Reproduced all six discovery rankings and twelve nominations, checked same-arm parent chronology and empty reference ancestry, and reconciled discovery wins with saved outcomes.
- Recounted both full-family evidence stages and checked final retained sample counts, artifact identities and adjusted intervals. Reconstructed all 32 calibration confirmation rates and six original paired reliability comparisons.
- Kept 24-, 192- and 512-trial measurements separate, preserving the existing selection and uncertainty allocation. The independent fresh precision trial remains governed by its sealed source package and existing verification.
- Reconstructed the diagnostic outputs in read-only `verify` mode and checked they matched exactly. No backend source or runtime changed, so backend fixture tests were not rerun for this analysis/documentation change.

Reproduce from the repository root, with all referenced local archives retained:

```powershell
python -B TestResults/balance/tower-kharad-search-diagnosis-20260913/analyze.py verify
```

Any Python 3.11+ interpreter with its standard library can run this script; use its explicit path if `python` is not on PATH. Verification used the available bundled interpreter. The [input receipt](../TestResults/balance/tower-kharad-search-diagnosis-20260913/inputs.json) records source hashes, applied content and the unchanged **476,054-reservation ledger**. The [verification log](../TestResults/balance/tower-kharad-search-diagnosis-20260913/verify.log) and [final receipt](../TestResults/balance/tower-kharad-search-diagnosis-20260913/final-verification.json) retain reconstruction, documentation checks and preservation results. Do not rerun its write-once `run` mode or alter historical packages to satisfy older workspace guards.

Changed repository files are this review, the rescreen implementation/comparison proposal and six active guides. Source code, catalogs, tests, all Tower content, historical reviews and plans remain unchanged. No new fights, seeds, configuration changes, migrations, service restarts, deployments or shared-database actions occurred. Practical ownership, unsearched combinations and floors 6–11 remain separate work. No required command remains blocked.
