# Portfolio confirmation audit: validation amendment

Frozen before the corrected zero-combat audit on 14 September 2026. The [implementation protocol](Tower-Portfolio-Confirmation-Implementation-Protocol.md) and its original frozen evidence remain unchanged.

The initial audit returned exit 2 in **13.873 seconds**, after inventory checks but before creating its audit bundle. It executed **zero fights and allocated zero seeds**. The source validator's attempt to register all 253 recipes as discovery references hit the unchanged v19 limit of 112. This was an engineering import-validation error, not evidence of gameplay incompatibility or a confirmation outcome.

Keep that discovery limit. Production input validation now visits all recipes in exactly **112 + 112 + 29** reference groups, with cancellation checks between groups. The complete confirmation definition and all ordinary/joint statistics still use one **253-recipe family**. A new regression test verifies exact membership/order, original source immutability and unchanged discovery limits. Failed audits now retain exception and trace data.

Preserve the original source/runtime snapshot, frozen comparison binaries, audit definition, protocol, failed log and command receipt. Use `captured-source-v2`, `execution-v2`, isolated `build/tests-v2`, `audit-definition-v2.json` and a new `audit-v2` destination for the corrected verification. Neither original evidence nor the comparison binaries it binds may be overwritten. Record the amended definition/input hashes before execution.

The corrected audit has the same zero-combat source, content, recipe order, comparison scope, 900-second and 512-MiB bounds. The total engineering package remains bounded at 1 GiB, and all diagnostic/audit execution remains within the original cumulative 1,800 seconds / 4 GiB / 512 fights. Record the failed 13.873 seconds in that accounting. No new balance seed schedule, retry of combat, confirmation preparation or confirmation execution is authorized by this amendment.
