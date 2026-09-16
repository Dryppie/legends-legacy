# Discovery comparison gate: failure closure

16 September 2026. The first `workflow.py freeze` attempt reached its frozen **20-second preservation deadline** while checking older packages. Its [failure receipt](../TestResults/balance/tower-discovery-comparison-gate-20260916/control/freeze-failure.json) charges **20.000 seconds**. No `freeze.json` or successful prerequisite receipt was produced. Build, test-build, tests, saved-fixture processing and independent audit did not run. The new gate and ten tests remain unverified source.

The [original protocol](Tower-Discovery-Comparison-Gate-Protocol.md), scripts and failure receipt stay unchanged. Do not retry the preservation scan, raise its deadline, launch dependent phases or invoke the success publisher. A bounded failure closure is needed because that publisher requires a successful freeze and repeats the preservation scan.

Run `close_failure.py` once, with at most **15 seconds** charged inside the original 80-second / 128 MiB scope and cumulative 3,000-second / 4 GiB limits. This closure only copies the unverified source/protocol, records unexecuted commands, verifies checkout preservation against the pre-edit hashes and the current ledger hash against the preceding freeze, updates active Markdown, checks changed-file whitespace/links, records the measured remaining allowance and seals the failed package. It does not scan predecessor package contents or claim their verification completed. Include one closure second and the conservative 1 MiB shared-artifact allowance.

No compilation, tests, generation, combat, runtime preparation, seed allocation, retry, resume or replay. Preserve all 482,821 reservations and adoption Hold. A future verification scope must explicitly address the preservation deadline and carry the remaining budget; this closure authorizes no replacement diagnostic.

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B 'TestResults/balance/tower-discovery-comparison-gate-20260916/close_failure.py'
```
