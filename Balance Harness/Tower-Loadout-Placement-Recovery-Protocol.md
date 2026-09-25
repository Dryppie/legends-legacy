# Placement recovery protocol and necessary resource gate

**Status: the recovery route is closed under the unchanged resource limits.** The registered conservative recovery rule has a necessary audit/publication floor of **1,806 seconds**, above its **1,800-second** cap. No new matched pair, timing sample, native preparation or runtime qualification was started.

The [recovery protocol](Tower-Loadout-Placement-Recovery-Protocol.json) applies prospectively to a possible corrected-auditor pair. It does not amend the original frozen experiment or convert its failed baseline into a completed study. The [sealed registration](../TestResults/loadout-placement-recovery-protocol-20260924/files.json) has SHA-256 `6a19708c73e6c25de5040f97ed7dd4f9baeb9767d60537b35143a587f1130b1e`.

## Recovery rule and why another timing pair cannot help

The first experiment remains failed, its prepared candidate remains unexecuted, and its full 24,420-second /13,019,119,616-byte declared allowance remains charged. The corrected auditor is pinned to `4769d793e1d5eb14fc70ac21efe10b6720b9618a10b52d15636fa88888a110be`; the production runtime, physical context, literal rules, ownership and audit requirements remain unchanged.

The failed baseline completed its native worker and native reconstruction. Its authenticated native receipt records 146.6040994 measured seconds and 1,078,739,935 observed bytes. These are not completed-study phase costs. The recovery protocol conservatively caps any future baseline **native denominator** at these retained values. Taking the minimum of that ceiling and a future completed native cost prevents a slower replacement baseline from making the native cost ratio look better.

A future placement numerator would still be at least the original retained placement cost, using final closeout costs for audit/publication. Both new studies would have to complete using the corrected auditor. The failed original audit has no completed audit/publication denominator; its partial process receipts remain retained and charged. For the necessary gate, the two unknown future audit ratios receive only their mathematical lower bound of one. No audit measurement or equality is imputed.

Therefore every permitted native ratio is at least the original placement cost divided by its retained native ceiling, and every audit ratio is at least one. The inherited forecast uses only positive scaling, maxima and upward rounding, including storage-driven audit scaling. It cannot decrease when any of those ratios increases. This establishes a necessary bound before another measurement is allowed.

| Partition | Necessary floor | Frozen cap | Gate |
| --- | ---: | ---: | --- |
| Native time | 8,020 seconds | 9,000 seconds | Fits |
| Audit/publication time | **1,806 seconds** | **1,800 seconds** | **Fails** |
| Native storage | 5,347,633,246 bytes | 5,905,580,032 bytes | Fits |
| Audit/publication storage | 34,989,702 bytes | 536,870,912 bytes | Fits |

The minimum native time and storage ratios are respectively 3.5112660704 and 1.0464475082. After the completed 16,256-fight pilot is scaled to 21,888 fights with margin two and inherited floors, minimum projected retention is 5,382,622,947.78 bytes. Applying that storage growth to the frozen audit probe, retaining both independent audit timings and the 120-second publication reserve, produces **1,805.080522 seconds before upward rounding**. The audit partition must fit separately; spare native time cannot cover this excess.

This is a necessary bound for the registered recovery methodology. It is not a measured current-runtime forecast, a causal estimate of placement overhead, an efficacy result, or proof that every possible future implementation would fail. The assessment explicitly keeps `qualifiedCurrentForecast` null, `recoveryPairMayBeRegistered` false, and admission/qualification false.

## Implementation and verification

- `analysis/loadout-placement-recovery-protocol.py` authenticates the 228 inherited pins and current source hashes, retains the cost inputs and failed native/audit receipts, validates the common-v5 evidence, registers the necessary gate and independently reproduces it from a pinned retained package. It has no fixture or native execution path.
- `analysis/test-loadout-placement-recovery-protocol.py` adds **20 passing tests**, covering retained cost floors, final closeouts, unknown audit bounds, invalid receipts, unchanged limits, strict upward rounding, replacement-output rejection and manifest tampering. One test checks 100 deterministic synthetic cost combinations against the monotonic lower bound.
- The frozen JSON protocol records the recovery ceilings, completion requirements, unchanged limits and failure-before-execution rule. This report and nine current-status lines document the result; historical document bodies are unchanged. `.gitattributes` preserves the new protocol and Python files with LF endings.

Verification commands completed successfully:

```text
python -B -X utf8 "Balance Harness/analysis/test-loadout-placement-recovery-protocol.py" -v
python -B -X utf8 "Balance Harness/analysis/loadout-placement-recovery-protocol.py" register --output TestResults/loadout-placement-recovery-protocol-20260924
python -B -X utf8 "TestResults/loadout-placement-recovery-protocol-20260924/implementation/Balance Harness/analysis/loadout-placement-recovery-protocol.py" verify --output TestResults/loadout-placement-recovery-protocol-20260924 --expected-manifest-sha256 6a19708c73e6c25de5040f97ed7dd4f9baeb9767d60537b35143a587f1130b1e
```

The retained implementation and retained inputs reproduced the assessment exactly. No required command was blocked. Backend tests were not rerun because no backend, test-host, owner, auditor or runtime code changed; their previously recorded verification remains pinned. The [verification package](../TestResults/loadout-placement-recovery-protocol-verification-20260924) retains test logs and before/after snapshots.

## Accounting and next work

This read-only gate charged its declared **180 seconds and 67,108,864 bytes** at the start. It completed its assessment in approximately 2.47 seconds before sealing. The earlier failed pair's full allowance remains charged; no replacement pair or qualification allowance was started. Production entropy, scientific reservations, combat and live-history rescans remain zero for this stage.

Another timing run under this protocol cannot reopen the gate. A subsequent implementation or scope change needs a separate prospective design and a justified resource model, with all historical evidence and costs retained. In particular, faster reruns, rounding down, borrowing native headroom, or silently dropping the old placement cost floor cannot produce an admission. Any proposal to change evidence storage must explain its new comparability and cost model before measurement; it cannot claim that an optimization alone removes this protocol's retained floors.

The next concrete engineering step is a read-only byte inventory of the retained baseline and placement archives, followed by a prospective design for a lossless, versioned evidence-storage change if the inventory supports one. That design must preserve complete catalogue and trajectory reconstruction and both audits, and justify its own comparable resource model before any new timing. No storage change or new measurement is authorized by the failed gate itself.

No migrations, application configuration changes, deployments or external-environment operations occurred.
