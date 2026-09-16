# Refinement launcher: real registry check review

16 September 2026. **VerifiedLiveRefinementRegistry**. The unchanged launcher and independent full registry reader agree on complete membership, ledger hashes and all 482,821 reservations. No values were allocated and no combat ran.

Native Check: **56.8624s**, 53.5938 CPU seconds, 4,671,713,856 allocated bytes, 590,094,336 peak working-set bytes. Error: `None`.

Independent reader: **38.7810s** enumeration and **6.5160s** hashing/union; 174 ledger paths, 131 distinct ledger hashes, 704,601 directories and 5,357,545 entries.

These timings do not establish a new performance improvement or stronger builds. Native time includes preflight, enumeration, repeated hashing and union; the prior 57.467-second figure measures enumeration alone. The prior independent enumeration was 85.484 seconds, followed by 8.281 seconds for hashing/union. Cache state, machine load and registry growth are uncontrolled; each new scan ran once. Native internal trace stages are unavailable through the existing Check API.

The thin host called only the already tested public Check operation. Launcher/gameplay binaries, root harness source, content, request and consumed evidence were pinned. Existing ten passing launcher tests through `build/run-tests.ps1`, six reader fixtures and earlier preflight/driver tests are reused evidence; no new backend tests were run. The full backend suite, actual compact runtime, allocation and combat were not scheduled. The independent scan did not use the old path list as a complete whitelist. Hidden files and reparse-point rejection remain enforced; no archive pruning was added.

| Completed phase before publication | Charged seconds |
| --- | ---: |
| audit | 0.250000 |
| build | 1.672000 |
| freeze | 0.265000 |
| independent | 45.516000 |
| native | 57.109000 |
| readiness | 0.340000 |

Uncompleted phases: **none**. Failure detail:

```
None.
```

The [frozen protocol](Tower-Refinement-Live-Registry-Protocol.md) and [completion receipt](../TestResults/balance/tower-refinement-live-registry-20260916/completion.json) record the exact approved allowance and measured cumulative time/output. Incoming usage was 3,023.714469456 seconds. The separately approved 210-second extension sets the cumulative ceiling to 3,240 seconds; this scope is capped at 210 seconds and 16 MiB within the unchanged cumulative 4 GiB output cap. No temporary fixtures, seed values, fights, preparations, replays or retries were created.

The live registry input gate is closed for this captured check. A real comparison still needs an exact production request, fresh-value/combat authorization and a sufficient resource envelope; this check grants none. Equality between two traversals is not an atomic snapshot against arbitrary concurrent writers. Later allocation must refresh history again. The nested preflight's requires-live-refresh flag describes its snapshot stage; the outer check and independent comparison supply this run's live evidence only.

Commands executed at most once; missing phase receipts denote unrun commands:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-live-registry-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" native
& $python -B "$work/workflow.py" independent
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

Only the new check package, protocol/review and six active Markdown handoffs changed. No launcher, gameplay, configuration or migration changes; no deployment. Preserve all 482,821 reservations, v19's 512 unused confirmation values and its 253 recipes. V19 remains Unresolved; later reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged.
