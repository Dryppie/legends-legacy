# Loadout placement: diagnostic scratch ownership

Current status: a source-bound, opt-in storage contract now enforces declared file names, roles and byte caps for a private diagnostic directory. Verification passes 378 Python tests. Unchanged native sources/runtime retain the authenticated prior 212-test backend verification. External runtime/cache paths and the remaining observer lifetime remain unresolved; both compressed guards and the 1,806/1,800-second resource gate remain closed.

The target is the offline Balance Harness diagnostic owner. The new `proposal_diagnostic_storage.py` implements `DiagnosticStorage`; `RetainedOwner(..., storage_contract=..., protected_roots=...)` opts into it. Existing callers continue to use the unchanged `ManagedStorage` implementation. No native code, scientific launcher, supervisor, monitor, codec, game service or admission rule changes.

A contract declares a finite mapping of portable leaf names to `retained` or `scratch` roles and a maximum logical byte length. It also declares caps on simultaneous live bytes, simultaneous scratch bytes and total accepted write bytes across the scope. The root is created exclusively. Every name may be created once, including names subsequently deleted. Only closed scratch files can be deleted. The binding receipt retains the canonical declaration, its SHA-256 and the storage implementation's SHA-256.

```python
contract = {
    'version': 'tower-proposal-diagnostic-storage-v1',
    'files': {
        'result.json': {'kind': 'retained', 'maxBytes': 1024},
        'temporary.bin': {'kind': 'scratch', 'maxBytes': 4096},
    },
    'maxLiveBytes': 5120,
    'maxScratchBytes': 4096,
    'maxTotalWrittenBytes': 5120,
}
```

These numbers illustrate the API only. They are not a scientific resource envelope. A retained owner must additionally declare every binding, worker/process receipt, terminal receipt and manifest it will publish; missing declarations or insufficient publication headroom prevent a sealed success. The retained integration fixture contains that complete literal declaration.

Before each mutation, directory and file identities, lengths, link counts and exact membership are checked. Closed content is also hashed before create/delete/snapshot and on close. Names reject paths, alternate streams, case aliases, trailing-dot aliases and reserved Windows device names. Linked or reparse directories and files, hard links, changed contents, replacement roots/files, undeclared members and overlapping protected roots are rejected. Protected paths are supplied by the caller; they are not an automatic repository-wide discovery mechanism.

The append request must fit every applicable cap **before writing**. A short successful return contributes only its accepted bytes. Deleted scratch still contributes to historical peaks and total written bytes. A thrown write may have consumed an invisible prefix, so its progress becomes unknown and the scope cannot produce a valid snapshot. Failed create/write/flush/sync/close/delete/validation operations also prevent subsequent success. Cleanup still attempts to close handles, and a close error does not replace an earlier write or caller error. Ordinary caller exceptions can leave a known partial file; the owner's existing error path still reports failure.

The contract is for a trusted, single owner using this API. It does not restrict what the operating system permits another thread or process to write. Check/use races and transient external mutations between checks remain possible. An external cache file can grow and disappear without changing the managed inventory. The snapshot therefore reports `DeclaredCooperativeDirectory`, `filesystemConfinement=false`, `externalWritesBounded=false`, `wholeProcessCoverage=false` and `usableForAdmission=false`. It is never promoted to a machine-wide storage high-water mark.

Repeated full content validation is deliberate for this diagnostic boundary and can be expensive. Validation reads through supplied owner counters are counted; selected logical operations are counted separately. This change makes no performance or physical/durable I/O claim. The snapshot ends at its own API boundary; its persistence and the remaining observer lifetime stay excluded.

The [resource specification v2](Tower-Loadout-Placement-Resource-Boundaries-v2.json) retains all 11 domains and adds the implemented cooperative contract. The [updated boundary inventory](Tower-Loadout-Placement-Scratch-Boundaries.json) pins current source references. Earlier specifications and inventories are preserved unchanged. The finite external-observer closure requirement remains in force.

Verification uses fresh export directories and the authenticated retained native fixture/runtime:

- **38 new storage tests** cover declaration validation, path aliases, caps, scratch deletion, partial writes, hidden progress, failures, changed contents/identities, hard links, reparse rejection, external-cache exclusions and owner publication.
- **340 existing tests** pass: 280 diagnostic regressions, 24 monitor tests, 20 lease tests, 13 read tests and three actual native audit command cases.
- The retained owner fixture creates and deletes 131,072 scratch bytes and preserves that peak. Its four process observations are synthetic literals; it launches no process. A terminal-publication cap failure cannot create a success manifest.
- Native audit succeeds for the authenticated fabricated-outcome fixture and rejects changed content and a resealed false input identity. Audit reconstructs retained trial input bindings through production `CreateInput`; there is no new fixture construction, encounter preparation or combat.

The [verification package](../TestResults/loadout-placement-scratch-ownership-verification-20260925) retains commands, logs, declarations, source snapshots and fresh fixtures. The first two focused runs exposed fixture setup errors (non-fresh counters, then omitted process-receipt declarations); those logs are preserved. The final 38-test run passes. Backend tests were not rerun because native sources/runtime are unchanged; their prior required-runner 212-test result and hashes were authenticated. No required command is blocked.

```text
python -B -X utf8 build/test-proposal-scratch-ownership.py -v
python -B -X utf8 TestResults/loadout-placement-scratch-ownership-verification-20260925/run-verification.py
python -B -X utf8 TestResults/loadout-placement-scratch-ownership-verification-20260925/run-supplemental.py
git diff --check -- <changed files>
```

Changed files are the new storage module, its retained-owner integration, one new test, this report, two versioned specifications/inventories, scoped LF attributes and only line 3 of nine status documents. Historical document bodies, evidence and runtimes remain unchanged. Cumulative charges remain **79,339.66095319996 seconds** and **51,980,910,910 bytes**; declared maxima remain **168,240 seconds** and **107,122,524,160 bytes**. The first failed pair stays failed.

Next extend explicit ownership or defensible bounds to pending native publication, leases, worker sidecars/logs and external runtime/cache paths. Then bound the finite external observer, including terminal persistence and exit, before proposing measurement. Unknown paths remain disqualifying. No scientific launch, qualification, cost experiment, timing pair, live-history rescan, production entropy, scientific reservation, encounter preparation or combat occurred. There are no migrations, application configuration changes or deployments. Future opt-in packages must bind both updated Python modules; retained historical runtimes must not be replaced. No current forecast, compression speedup or search improvement is established.
