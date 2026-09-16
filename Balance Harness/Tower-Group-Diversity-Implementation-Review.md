# Group-diversity implementation: preserved verification stop

15 September 2026. The opt-in policy and isolated builds completed, but the frozen backend run returned **78 passed / 1 failed** out of 79. The failing full-search case reached `TowerBossDiscovery.ValidateProvenance`: the new diversity policy/method was absent from the explicit loadout-operator allowlist. The required bounded parent-count check remained active and rejected the new policy's valid loadout operators.

All 63 preceding policy cases and the new captured old-variation hash case passed. The unchanged reference executable saved 128 synthetic old-variation evaluations and 38 constructor outputs. Candidate captured-content construction and independent audit **did not run**. No new fights, preparations, seeds, replays or retries.

The failed source, binaries, test log/TRX, reference outputs and diagnostic charges are sealed in [the implementation package](../TestResults/balance/tower-group-diversity-implementation-20260915/files.json). No run in that directory may be repeated. The narrow correction is to allow the exact new policy/method in the existing loadout-operator branch, retaining every existing parent and provenance constraint. A separately frozen corrected verification is required; this stop does not claim verified implementation or stronger combat results.

No gameplay, configuration, migration or deployment changes. All 482,506 reservations remain preserved, including v19's unused 512. Reliability and adoption remain unchanged: adoption Hold. Detailed failed/compilation/publication charges are in [completion.json](../TestResults/balance/tower-group-diversity-implementation-20260915/completion.json).
