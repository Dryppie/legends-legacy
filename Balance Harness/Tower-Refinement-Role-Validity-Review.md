# Refinement role validity — VerifiedZeroCombat

16 September 2026. All 20 backend tests passed. V1 exactly reconstructs the saved 16-proposal/14-evaluation search. Both saved missing-role edits produce valid role-preserving alternatives. The two metadata-order variants of v2 agree exactly: 15/16 synthetic evaluations, with results {'evaluated': 15, 'duplicate': 1}.

The new opt-in `independent-discovery-refinement-roles-v2` preserves the last provider of each required team role during construction. Distribution records the actual safe subset of requested slots; essence refinement scans the sorted provider pool once from its sampled offset; whole-character replacement reserves otherwise missing role providers. V1 retains its original behavior. Ability order remains canonical. Shared inventory, family validity, final role validation, cancellation, provenance and the 16-proposal cap remain enforced. Duplicate or other invalid proposals remain charged; complete discovery is not guaranteed.

The sealed combat comparison remains incomplete. A future comparison must explicitly adopt and verify v2 in the launcher, freeze new resources and receive a fresh-seed exception before execution; current launcher contracts still select v1. No strength improvement or reliability change is established.

Zero fights, preparations, combat replays, new seeds or retries ran. All 482,866 reservations remain, including the failed comparison's 40 unused values and v19's separate 512 values and 253 recipes. V19 reliability remains Unresolved; later reliability Fail 1/3 and deep recovery 0/3 remain unchanged. Adoption Hold.

The [frozen protocol](Tower-Refinement-Role-Validity-Protocol.md), [captured results](../TestResults/balance/tower-refinement-role-validity-20260916/captured-result.json), [completion receipt](../TestResults/balance/tower-refinement-role-validity-20260916/completion.json) and command/log files in `control` describe the exact scope. Existing performance measurements remain separate; this work measures validity, not combat strength or throughput.

Reproduction: from the repository root, invoke the bundled Python with `-B TestResults/balance/tower-refinement-role-validity-20260916/workflow.py <phase>` for `freeze`, `build`, `test-build`, `tests`, `captured`, `audit`, `publish`, in that order. This sealed package is once-only; reproduce from the pinned sources/assets in a new separately bounded directory. Backend tests used `build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/balance/tower-refinement-role-validity-20260916/tests -Filter 'FullyQualifiedName~BalanceHarnessDiscoveryRefinementTests|FullyQualifiedName~BalanceHarnessRefinementRoleTests'`.

Changed files: the refinement helper and seven policy/dispatch integrations, one eight-test backend file, this protocol/review and six active handoffs. No gameplay assemblies/content, migrations, configuration, deployment or comparison-cap changes.

Captured construction cases (zero combat):

[
  {
    "index": 4,
    "operation": "loadout-distribute",
    "beforeHash": "6a4617fefe7af7f4f626e2a38b5f7a7a19c84e91ee1d76427f0c6c9988bfd4d9",
    "afterHash": "4e2d02b56b77d49ce1e5d2ec14924d2c57dc11f06a7d6c0ebeaba925992bdac3",
    "missingBefore": [
      "attack-enabler",
      "recovery"
    ],
    "missingAfter": [],
    "requestedTargets": [
      9,
      2,
      5,
      10,
      6,
      4,
      3,
      1,
      8
    ],
    "actualTargets": [
      9,
      2,
      5,
      10,
      6,
      4,
      3,
      8
    ],
    "replacement": null
  },
  {
    "index": 13,
    "operation": "loadout-refine",
    "beforeHash": "342cf7d1d7372141359b703e9863affd9f55178419d80c62b3f2512cf74b4342",
    "afterHash": "8f7ee0e2d104aad98b5e85bd1a7793b78cfa744f67a1774afb00e619edd1a52f",
    "missingBefore": [
      "protection",
      "recovery"
    ],
    "missingAfter": [],
    "requestedTargets": [
      1
    ],
    "actualTargets": [
      1
    ],
    "replacement": "essence.transparent_slime"
  }
]
