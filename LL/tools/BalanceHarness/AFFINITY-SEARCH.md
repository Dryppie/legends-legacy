# Supported affinity search

Use `affinity-creation-with-benchmark-validation-v1` as the supported offline search profile. It combines the original `tower-proposal-policy-v3` affinity proposer with `tower-proposal-racing-v5` and the existing benchmark validation gate. This choice establishes a maintainable baseline; it does not establish superiority over the confirmed benchmark.

`TowerAffinitySearch.CreatePlan` takes an admitted racing plan, the frozen affinity inventory and the selected affinity IDs. It copies the inputs and fixes the proposal and validation policies. `RunAsync` executes through the existing native archive boundary and returns a concise result:

| Status | Meaning |
| --- | --- |
| `BenchmarkRetained` | The challenger failed the fresh paired validation gate. Keep the benchmark. |
| `ChallengerNeedsConfirmation` | The challenger passed this search's provisional gate. Independently confirm the exact team before treating it as a replacement. |
| Any incomplete status | No selected team is returned. Retain the failure evidence. |

Each complete search evaluates 17 generated proposals in two waves and spends 528 fights. The five-member nomination panel contains two generated finalists and three references. The supported profile chooses one of the four nonbenchmark nominees, then compares it with the benchmark on 60 fresh paired seeds. The exact win/loss gate, primary-reference tie rule, zero-win health rule and benchmark fallback are unchanged.

## Commands

With the qualified harness DLL, inspect a bound plan without starting fights:

```text
dotnet <BalanceHarness.dll> tower-affinity-search-check <racing-plan.json>
```

Reconstruct and summarize an already sealed supported search:

```text
dotnet <BalanceHarness.dll> tower-affinity-search-verify <search-archive> <files.json-sha256>
```

The archive must be an individual search root containing `racing/plan.json`; a comparison's `search/root-01/control` is an example. The supplied SHA-256 must come from the retained trusted manifest pin. Verification authenticates the native evidence before returning the summary.

These commands do not allocate seeds or start unowned work. Execution remains inside the existing admitted owner, which handles history exclusions, process and storage limits, evidence and independent audits. There is no new standalone public launch command in this change.

## One explicit experiment

`tower-affinity-nomination-comparison-v1` compares the supported profile with experimental `tower-proposal-racing-v9`. The experiment permits only the two generated finalists to challenge the benchmark. Its proposer, all first 408 observations and the final validation gate remain identical to the supported arm. Native and independent Python audits enforce those constraints.

The experiment is governed by [the frozen design and stop rule](../../../Balance%20Harness/Tower-Affinity-Search-Consolidation.md). A result below that rule ends this tuning cycle. It does not trigger automatic parameter changes, another seed draw or a new variant.

Historical search commands retain their versioned behaviour. Gameplay, APIs and deployment configuration are outside this offline harness change.

See the [implementation and verification record](../../../Balance%20Harness/Tower-Affinity-Search-Implementation.md) for changed components and engineering evidence.
