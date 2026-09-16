# Group-variation verification: publication accounting correction

15 September 2026. **The 63 passing tests and independently verified 1,956 schedule positions / 19 legal constructions remain valid.** The primary publication receipt has an invalid negative duration. Use this correction's [completion.json](../TestResults/balance/tower-group-variation-verification-accounting-20260915/completion.json) for cumulative accounting; preserve the primary receipt unchanged as evidence of the reporting defect. No tests, construction requests, replays, preparation or combat were rerun.

The report writer reused the name `start` for a Markdown character offset inside `publish()`, shadowing the outer monotonic start time. Its final subtraction therefore produced **-22280.86 seconds**, and the original completion total became negative. The diagnostic result files themselves are valid. This is a publication timing defect, not a negative workload, completed-combat failure or basis for discarding earlier charges.

| Accounting component | Seconds | Basis |
|---|---:|---|
| Five valid diagnostic phases | 7.828 | Frozen bootstrap/tests/content/audit/preservation result receipts |
| Original publication | 1.000 | Conservative charge rounded up from the tool's 0.5894476-second whole-command wall time |
| This accounting correction | Recorded in corrected completion | Separately measured monotonic duration |
| Previous cumulative workload | 263.066 | Failed implementation completion; its 8.625 seconds remain charged |
| Corrected cumulative before this correction's own work | 271.894 | Sum of the preceding valid charges |

The exact original monotonic publication duration cannot be recovered. Its one-second entry is an explicit conservative charge, not an exact timing estimate. [accounting-basis.json](../TestResults/balance/tower-group-variation-verification-accounting-20260915/accounting-basis.json) retains the invalid receipt hash, individual phase values and the tool receipt identifier/wall time. Construction timing **106.7258 ms**, test-build timing **2.703 seconds**, test outcomes and all recipe/preservation results are unchanged.

The [unapplied producer patch](../TestResults/balance/tower-group-variation-verification-accounting-20260915/producer-timing-fix.patch) uses a dedicated timing argument and rejects negative accumulated time. The sealed producer was not edited, patched or executed again. This small correction package freezes its own script and inputs, checks the primary seal, publishes corrected accounting and verifies its separate inventory. Active review and coverage-plan links now point to the corrected receipt; the primary package's captured documents remain unchanged.

Commands executed once for this correction:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-group-variation-verification-accounting-20260915'
& $py -B "$w/freeze.py"
& $py -B "$w/correct.py"
```

All **482,461 reservations** and the unused original 512 values remain preserved. The corrected positive totals remain within the 120-second follow-up, combined 300-second implementation and cumulative 1,800-second limits. Primary plus correction output remains within the 128-MiB follow-up, combined 512-MiB implementation and cumulative 4-GiB envelopes. No old cap increase, diagnostic retry, new balance seed, gameplay/configuration change, migration or deployment. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption Hold remain unchanged.
