# Adaptive racing pilot 02

**Subsequent diagnostic:** The [frozen-pool recognition study](Tower-Frozen-Pool-Recognition-Execution.md) completed 27,648 additional fresh fights and both audits. Its measured candidates were broadly below the fixed benchmark, making proposal quality the next development priority. It does not revise or pool the pilot results below.

**Completed and audited on 2026-09-23. The prespecified decision is `AbandonThisConfiguration`.** Across twelve paired roots, adaptive racing (N) averaged **0.944 percentage points below the baseline search (B)** and **4.199 points below the strongest independently confirmed reference (R*, `96b94357…`)**. Six roots selected novel recipes; none exceeded R*. Retain the baseline and benchmark. This pilot does not support a larger evaluation of this unchanged configuration, policy promotion or team adoption.

This is a separately declared second pilot after the [first pilot's technical failure](Tower-Adaptive-Racing-Comparison-Execution.md). Its [prospective declaration](Tower-Adaptive-Racing-Pilot-02-Declaration.json) was frozen before any new seed allocation. It uses the same `tower-adaptive-racing-comparison-v1` comparison design and native harness. The operational change is the repaired Python storage monitor. The first attempt remains a failed study with 2,640 search fights and zero held-out fights; it is not erased, resumed or pooled into this study.

## Held-out results

All 24 search outputs were frozen before held-out evaluation. Each method and R* has 3,072 logical evaluation positions, arranged as twelve common 256-seed panels. Identical recipes share physical battle rows; those rows are not counted as independent repetitions. The three roots where B and N selected the same recipe remain in every aggregate.

| Policy or reference | Wins / 3,072 | Held-out win rate |
| --- | ---: | ---: |
| Baseline search B | 2,258 | 73.503% |
| Adaptive racing N | 2,229 | 72.559% |
| Strongest reference R* | 2,358 | 76.758% |

| Contrast | Mean difference | Approximate two-sided 95% root interval | Conditional one-sided 95% lower bound |
| --- | ---: | ---: | ---: |
| N−B | −0.944 pp | [−3.420, +1.532] pp | −5.360 pp |
| N−R* | −4.199 pp | [−7.612, −0.786] pp | −8.616 pp |

The root intervals use a paired t calculation with eleven degrees of freedom and are small-sample approximations. The separate conditional Hoeffding bounds concern these frozen outputs and include the prespecified depletion correction; they do not bound performance on future search roots and are not simultaneous 95% bounds. The N−B interval includes zero, so this pilot does not establish a precise relative disadvantage against the baseline search. Its development decision instead follows the prespecified absolute benchmark rule.

All roots are shown below. Wins are out of 256; differences are percentage points. Party identifiers are abbreviated only for display. “Novel” means N selected a recipe outside the three retained references, not a statistically qualified improvement.

| Root | B party | N party | B wins | N wins | R* wins | N−B pp | N−R* pp | Novel |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | `399bc776` | `f0c72d4a` | 183 | 172 | 207 | -4.297 | -13.672 | Yes |
| 2 | `8287f779` | `9716c643` | 174 | 187 | 202 | +5.078 | -5.859 | Yes |
| 3 | `96b94357` | `9efc6b06` | 196 | 178 | 196 | -7.031 | -7.031 | Yes |
| 4 | `96b94357` | `96b94357` | 204 | 204 | 204 | +0.000 | +0.000 | No |
| 5 | `399bc776` | `96b94357` | 186 | 181 | 181 | -1.953 | +0.000 | No |
| 6 | `96b94357` | `96b94357` | 186 | 186 | 186 | +0.000 | +0.000 | No |
| 7 | `d0ce9845` | `96b94357` | 191 | 192 | 192 | +0.391 | +0.000 | No |
| 8 | `399bc776` | `96b94357` | 176 | 192 | 192 | +6.250 | +0.000 | No |
| 9 | `399bc776` | `0e43a708` | 178 | 170 | 200 | -3.125 | -11.719 | Yes |
| 10 | `96b94357` | `96b94357` | 207 | 207 | 207 | +0.000 | +0.000 | No |
| 11 | `8287f779` | `a1b50da6` | 178 | 164 | 192 | -5.469 | -10.938 | Yes |
| 12 | `96b94357` | `5448dc4e` | 199 | 196 | 199 | -1.172 | -1.172 | Yes |

N beat B on three roots, lost on six and matched on three. It selected R* on six roots and novel recipes on the other six; every novel selection scored below R*. Its median difference from R* was −0.586 points and its worst was −13.672 points. Five roots were at least three points below R*, and the same five were at least five points below. No root was three or five points above R*. These are descriptive counts, not estimates of qualification probabilities or evidence for adopting any individual team.

| Prespecified development condition | Observed | Outcome |
| --- | --- | --- |
| Larger evaluation: N−B ≥ +2 pp, N−R* ≥ 0, and ≥3 promising novel roots | −0.944 pp; −4.199 pp; 0 roots | Not met |
| Abandon: N−B ≤ −2 pp | −0.944 pp | Not met |
| Abandon: N−R* ≤ −2 pp and <3 promising novel roots | −4.199 pp and 0 roots | Met |

“Promising” was fixed as a novel selected recipe at least three observed points above R*. The comparison evaluates the complete proposal, racing and selection policy together; it cannot identify which operator or selection stage caused the outcome. Saved search traces remain available for diagnosis, but any revised configuration would need its own prospective study and fresh values. No further campaign was launched.

The [published result](../TestResults/balance/tower-adaptive-racing-pilot-02-20260923/result.json) retains full identities, paired gains/losses, per-root seed uncertainty, covariance and reference comparisons. Both native reconstruction and the independent Python saved-row audit reproduced it.

## Admission and scope

The [admission receipt](../TestResults/adaptive-racing-pilot-02-admission-20260923/admission.json) binds the repaired launcher, original captured runtime/content, all three reference recipes, and the complete updated registry. `399bc776…` remains the positive-tie primary; `96b94357…` remains the independent development benchmark. The selection rules, proposal operators, twelve paired roots, 528-fight search budgets per policy/root, held-out panels, development gates and uncertainty calculations are unchanged from the [original protocol](Tower-Adaptive-Racing-Comparison-Plan.json).

The before-and-after history scans and native check agree on **649,696 reserved values across 242 ledger files**, including all 16,383 fresh values exposed by the first failed attempt. The entire first failed archive was authenticated against its external inventory before and after this admission. Only synthetic engineering measurements were replayed for compatibility; previous scientific observations did not tune this new study.

The captured-runtime check again authenticated 211 producing source documents, resolved 895 methods and reconstructed the stored 528-observation adaptive fixture. The native admission prepared six reference inputs with a combat guard and allocated no seeds. All four retained harness files match the preceding tested binary output. No C# or gameplay changes were made.

The admission package sealed in **116.219 seconds**, retaining **48,262,705 bytes** plus its **506-byte** external pin. The separate read-only verification of every package member and current live history took **45.844 seconds**; both stages finished **180.391 seconds from admission start**, inside this admission's original **600-second / 512-MiB** allowance. Its owned verification process exited with an empty job and made no additional native preparations.

Bindings:

- Study declaration SHA-256: `55487a065798d94c2d62054ce6563014cf8a4df0a33f711b6038ec94b2b0b830`.
- Admission manifest SHA-256: `e931606aad5690e12774f5f33e2f50487d68ac802d6f21def93546973317d6a4`.
- Request SHA-256: `c6a75f07d66619d75f775d6c95d180667fd5b66a6a2acd890d87b4c86d3ea2b4`.
- Repaired launcher SHA-256: `4972e277baeb60afcd91532d9863e114f73fd567d07aa3d1568d1212484a9d83`.

See the [request](../TestResults/adaptive-racing-pilot-02-admission-20260923/request.json), [external admission pin](../TestResults/adaptive-racing-pilot-02-admission-20260923-pin.json), [native check](../TestResults/adaptive-racing-pilot-02-admission-20260923/native-check.json) and [bounded verification receipt](../TestResults/adaptive-racing-pilot-02-execution-20260923/admission-verification.json).

## Authorization and accounting

After the failed-run report and monitoring repair, the user asked to proceed. This follow-up records one new full pilot under the existing limits. The scientific protocol identifier describes the unchanged design; `tower-adaptive-racing-pilot-02`, the distinct output directory and the newly bound request identify this experiment. Its declaration and preserved predecessor receipts are included in its sealed admission.

The first study stays closed under its no-retry rule. The second study draws its own single entropy batch, excludes all previous values and may not replace failed roots, resume, refill, extend its panel or launch another attempt automatically. It uses none of the first study's unused allowance. Both operational outcomes must be reported; any efficacy result concerns this complete second pilot alone and cannot promote a policy or adopt a team.

This study retains a separate **10,800-second / 6-GiB** scientific allowance, including both audits and publication, with **10,680 seconds / 5.75 GiB** for native execution. Its own preparatory admission is separately charged at 600 seconds / 512 MiB. Earlier admission, the failed first launch and its closeout retain their original accounting. These are limits rather than runtime/storage forecasts; complete historical engineering totals remain unknown.

## Implementation and verification

The admission helper adds an explicit `--pilot-02` profile. Default behavior for the earlier profile and all sealed copies remain unchanged. The new profile binds the prospective declaration, repaired launcher and repair-test receipts; independently authenticates the failed predecessor; carries its two reservation ledgers into the complete exclusion history; and rejects reuse of the prior package/output.

**23 admission tests passed**, including seven new cases covering separate study identity, unchanged native protocol, repair provenance, preservation of failed-run reservations and receipts, and prohibition of pooling or repeated profile selection. The [test log](../TestResults/adaptive-racing-pilot-02-admission-tests-20260923.log) is retained in the admission. The 42 tests for the unchanged storage repair and launcher contracts remain pinned as preceding evidence, as do the unchanged harness's 180 backend and 48 Python implementation tests. Those prior suites are not represented as newly executed tests in this step.

The native no-combat admission and the sealed-package/live-history verification passed. No C# changes required a new backend run in this step; the retained binary and its producing evidence were authenticated instead.

## Execution, publication and permanent reservations

The one scientific launch completed **23,680 fights**: **12,672 search** and **11,008 held-out**. The held-out total corresponds to 43 physical root/recipe panels; deduplication accounts for the difference from the 28,032-fight ceiling. There were no retries, replacement roots, seed refills or additional audit fights.

The [completion receipt](../TestResults/balance/tower-adaptive-racing-pilot-02-20260923/completion.json) reports **1,129.922 seconds** for the scientific launch, both audits and publication, and **1,667,510,750 observed/retained bytes**, within 10,800 seconds / 6 GiB. Native execution took **988.672 seconds** by the owned-process receipt; its internal measurement was **988.196 seconds**. The separate native audit took **95.922 seconds** and the independent Python audit **41.953 seconds**. Every completed owned process reports exit 0, no timeout and zero active processes. These measurements do not estimate combat-only throughput or future run costs.

Evidence: [native receipt](../TestResults/balance/tower-adaptive-racing-pilot-02-20260923/native-receipt.json), [native audit process](../TestResults/balance/tower-adaptive-racing-pilot-02-20260923/native-audit-process.json), [independent saved-row audit](../TestResults/balance/tower-adaptive-racing-pilot-02-20260923/independent-audit.json), [independent audit process](../TestResults/balance/tower-adaptive-racing-pilot-02-20260923/independent-audit-process.json) and [external scientific pin](../TestResults/adaptive-racing-pilot-02-publication-verification-20260923/scientific-pin.json).

A supplementary read-only closeout initially exited 1 after **2.797 seconds** because its aggregate helper returns an empty `descriptiveViews` list, while the script compared that aggregate with the full result containing 24 descriptive views. The full scientific auditor had already reconstructed those views and passed. A targeted comparison established that `descriptiveViews` was the only mismatch. The original [script](../TestResults/adaptive-racing-pilot-02-execution-20260923/closeout.py), [failure log](../TestResults/adaptive-racing-pilot-02-execution-20260923/closeout.log) and [exit/empty-job receipt](../TestResults/adaptive-racing-pilot-02-execution-20260923/closeout-process.json) are preserved unchanged. This engineering failure did not rerun combat or modify the published scientific archive.

A separate corrected publication verifier compares the aggregate with the corresponding aggregate fields, while still requiring exact full-result agreement with the successful independent audit. It authenticated the complete admission and scientific archive, reclassified all entropy words, checked both complete reservation ledgers, scanned the entire live history, and reauthenticated the first failed study and original execution evidence. This read-only verification had its own **300-second / 64-MiB** engineering allowance, completed in **52.203 seconds** with **129,715 retained bytes**, and added zero fights or values. The original failed check retains its separate 300-second / 64-MiB allowance and failure receipt; neither engineering allowance was transferred to scientific execution. See the [closeout receipt](../TestResults/adaptive-racing-pilot-02-publication-verification-20260923/closeout.json), [process receipt](../TestResults/adaptive-racing-pilot-02-publication-verification-20260923/closeout-process.json) and [completion](../TestResults/adaptive-racing-pilot-02-publication-verification-20260923/closeout-completion.json).

The single 16,384-word batch contained **16,380 fresh values**, four historical collisions and zero duplicate words. Exactly **4,428** fresh values were assigned; the **11,952-value unused tail remains permanently reserved**. The complete history now contains **666,076 values across 244 ledger files**. All 242 prior files are unchanged; the two new ledgers are exactly those of this pilot. The [history inventory](../TestResults/adaptive-racing-pilot-02-publication-verification-20260923/live-history-files.json) binds this union.

Final SHA-256 bindings:

- Scientific archive manifest: `f2327de7f9382f3a3ac3213a25cdb590e81e676ddd6e622fb91e3615ea8dd95a`.
- Publication verification package manifest: `3e11948b3892f4d813a6b992d12a242c8c10342df34c6ee91996604045d2d798`.
- Preserved original execution inventory, including the failed extra check: `248a77ae0be78b979d8d58b06560181879183326d2d88014f9576369bfa66d9a`.
- Complete live-history inventory: `bd3ac54d122759d35408ae43686e411e456cb87451ac89cd54f9ba3608a35e39`.

Across both scientific studies there was **one technical failure and one completed pilot**, consuming **26,320 physical fights** in total. Only the complete second pilot contributes to the efficacy result. The first attempt's 2,640 search fights and all its reserved values remain recorded; its observations were not reused. Complete historical engineering time/storage totals remain unknown.

## Changed files and commands

- [Prospective declaration](Tower-Adaptive-Racing-Pilot-02-Declaration.json) and `.gitattributes`: distinct study identity, unchanged scientific rules and exact declaration bytes.
- [Admission helper](analysis/prepare-adaptive-racing-admission.py) and [tests](analysis/test-adaptive-racing-admission.py): explicit second-pilot profile, failed-predecessor preservation, repair provenance and updated full-history requirements; seven new tests bring the suite to 23.
- This report and the earlier admission, implementation and failed-execution status links: verified outcome, uncertainty, failure accounting and evidence locations.
- Local `TestResults` packages: sealed admission, one completed scientific archive, preserved failed supplementary check and corrected publication/history verification. These are machine-specific evidence artifacts rather than application configuration.

Commands executed in this step, with `python` denoting the bundled Python interpreter's absolute path:

```powershell
python -B -X utf8 'Balance Harness/analysis/test-adaptive-racing-admission.py'
python -B -X utf8 'Balance Harness/analysis/prepare-adaptive-racing-admission.py' prepare --pilot-02
python -B -X utf8 'TestResults/adaptive-racing-pilot-02-execution-20260923/verify-admission.py'
python -B -X utf8 'TestResults/adaptive-racing-pilot-02-admission-20260923/run-reference-exploration-comparison.py' --request 'TestResults/adaptive-racing-pilot-02-admission-20260923/request.json' --harness 'TestResults/adaptive-racing-pilot-02-admission-20260923/runtime/BalanceHarness.dll'
python -B -X utf8 'TestResults/adaptive-racing-pilot-02-execution-20260923/closeout.py'
python -B -X utf8 'TestResults/adaptive-racing-pilot-02-publication-verification-20260923/closeout.py'
python -B -X utf8 'TestResults/adaptive-racing-pilot-02-report-check.py'
```

The original supplementary closeout command failed for the comparison-shape error described above; the separate corrected verification passed. Both scientific audits, all 23 admission tests, Python syntax, scoped whitespace, report arithmetic and document links passed. No required verification remains blocked. The admission, launch and evidence wrappers above are single-use historical commands and must not be rerun against their existing output directories.

The [static report check](../TestResults/adaptive-racing-pilot-02-report-check.py) matches all twelve result rows and both interval rows to the published result, checks the displayed win rates and reservation counts, verifies eight hash bindings, and resolves the local evidence links. It executes no combat or audit subprocesses.

There is no database migration, application configuration change or deployment. The separate pending 52,000-fight confirmation remains untouched.

The subsequent [saved-stage diagnosis](Tower-Adaptive-Racing-Stage-Review.md) reconstructs all twelve pairs without new combat. It identifies zero initial wins for every fully fresh adaptive proposal, two losing outputs selected on nonprimary-reference ties, four losing strict winners with narrow selection leads, and 198 generated candidate instances without held-out measurements. It retains the abandonment decision and recommends a frozen-pool recognition diagnostic before another search revision.
