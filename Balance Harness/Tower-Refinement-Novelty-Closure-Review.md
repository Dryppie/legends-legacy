# Refinement novelty closure â€” VerifiedZeroCombat

16 September 2026. Captured verification is complete: v1 and v2 exactly match their saved results, and v3 gives identical results with reversed metadata. V3 evaluates 16/16 distinct synthetic teams ({'evaluated': 16}). The earlier 28 backend tests remain passing evidence; no tests were rerun.

The captured duplicate-distribution blocker is closed. Next, explicitly integrate v3 into the comparison launcher and verify its binding/gates in a new zero-combat scope. The current launcher still selects v1; no combat comparison or fresh-seed allocation is authorized.

This scope reused the compiled candidate and the existing compact JSON serializer through a .NET 10 PowerShell host. No rebuild, code change, fight, preparation, combat replay, fresh value or retry occurred. Compact serialization changes whitespace; both saved-policy hashes and independently compared parsed traces establish parity. The prior missing-runtime failure remains sealed.

| Check | Distinct evaluated teams | Proposals | Status |
| --- | ---: | ---: | --- |
| v1 | 14 | 16 | Incomplete |
| v2 | 15 | 16 | Incomplete |
| v3 | 16 | 16 | Complete |
| v3-reordered | 16 | 16 | Complete |

V1 used saved discovery observations; v2/v3 used deterministic synthetic scores. These counts measure construction, not combat strength.

V3 distribution construction checked 1 of 240, 1 of 360, 1 of 400 bounded possibilities. The attempt cap remains 16. Exhaustion still charges a duplicate; this fixture does not guarantee completion for every input.

The [frozen protocol](Tower-Refinement-Novelty-Closure-Protocol.md), [completion receipt](../TestResults/balance/tower-refinement-novelty-closure-20260916/completion.json) and full compact traces retain the evidence. The independent audit checks exact saved outputs, party identities, roles, family/inventory legality, ancestry, construction limits and sealed-file preservation. The 28 backend tests were previously run through `build/run-tests.ps1`; no additional suite was necessary because the candidate DLL is unchanged.

Reproducible invocation, from the repository root: bundled Python with `-B TestResults/balance/tower-refinement-novelty-closure-20260916/close.py <phase>`, using `freeze`, `captured`, `audit`, `publish` once each. The native PowerShell command is in `command.json`; runtime, binary/source/input hashes and serializer setting are pinned in `launch.json`, `setup-pins.json` and `freeze.json`. This sealed package must not be rerun; reproduce in a new explicitly bounded directory.

All 482,866 reservations remain, including the failed combat comparison's 40 unused values and v19's separate 512 unused values and 253 recipes. V19 reliability Unresolved, later reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged. No balance-strength or whole-run performance claim is made.

Changed files: this protocol/review, six active Markdown handoffs and the isolated verification evidence/scripts. No harness/gameplay source, configuration, migration or deployment changes. Previous performance engineering measurements remain 622.54Ã— for incremental accounting and 4.50Ã— for the measured sixteen-write lifecycle; this verification adds no new throughput measurement.
