# Starter baseline package — 9 September 2026

This increment targets the primary LL game's offline balancing harness. It packages the two accepted starter baselines and their retained evidence so they can be recovered independently of the original checkout. The accepted recipes, 50–90% win policies, trial schedules and baseline manifests remain unchanged.

## Package contract

[The versioned input contract](../build/starter-balance-package-v1.json) pins eighteen reviewed manifest, policy and result hashes before export. It includes the complete Blood Grove acceptance and preceding Inconclusive runs, Crystal Creek fine-grid and ownership-compatibility evidence, both combined-regression attempts, four retained build/provenance directories, the original two baseline manifests, relevant scripts and review documents. The full fine-grid history is retained alongside the selected candidate.

The [export command](../build/export-starter-balance.ps1) writes a ZIP directly from these selected inputs. It preserves their repository-relative layout and every file's bytes, records per-file lengths and SHA-256 checksums, and rechecks the entire source file set before sealing the archive. It also validates the original executable retention manifests and permits only the selected combat settings in captured appsettings files. Existing outputs are rejected; incomplete attempts do not receive a success receipt.

The [restore command](../build/restore-starter-balance.ps1) requires the expected ZIP hash, validates paths and the complete archive inventory before extraction, and checks every extracted file. The [independent verifier](../build/verify-starter-balance-package.ps1) uses only the restored package for all input paths. Historical absolute paths inside old reports are preserved as provenance and are not used to locate inputs.

## Fixed recovery check

Restore into a new directory outside the original checkout. Check every payload file, reevaluate the two accepted suites and their two combined-regression candidates against the unchanged policies, and compare each with its original accepted manifest. Replay trial index 0 in each suite's two primary cells using that suite's exact executable: **eight fixed replays**. No complete simulation suite is rerun, no independent samples are added, and no baseline is promoted.

Full replay retains the original .NET 10.0.11 / Microsoft Windows 10.0.26200 / X64 identity requirement. File integrity can be checked on another supported PowerShell platform with `-IntegrityOnly`; that mode does not certify replay. The archive contains executables and dependencies, but does not install or bundle the .NET shared runtime or Windows.

The PowerShell entry points use a small [C# archive helper](../build/BalanceEvidenceArchive.cs) through `Add-Type`, using PowerShell's existing .NET libraries without an additional runtime or package manager. [Tool checks](../build/test-balance-evidence.ps1) exercise byte-exact recovery, corruption, missing/unexpected files, overwrite protection, path traversal, alternate streams, duplicate paths and linked ZIP entries.

## Runtime recovery

The installed runtime had advanced to **10.0.12** since the 8 September evidence. Full replay still requires **10.0.11**. The official Windows X64 runtime ZIP was downloaded from [Microsoft's release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json), checked against its published SHA-512, and extracted under ignored `TestResults/balance/replay-runtime-10.0.11`. The ZIP and source/hash receipt are retained beside the evidence export. No system runtime, persistent environment variable or archived runtime configuration was changed.

The verifier's `-DotnetPath` selects that isolated executable explicitly. This preserves the original replay requirement after the system update. The runtime is a separate recovery dependency, not a change to an accepted build or a new gameplay measurement. The first download attempt could not access the network inside the sandbox; the approved download succeeded and its published checksum matched.

## Result

**Pass: the corrected package restores and verifies independently of the original checkout.** Both ZIP versions restored all 428,158 payload files with matching checksums. The corrected verifier ran from the restored tools, after changing the PowerShell working directory, with the evidence and original runtime in separate temporary folders outside the repository. All four saved suites pass their unchanged policies and comparisons; all **eight fixed detailed replays match**.

| Export property | Recorded value |
| --- | --- |
| Payload files | 428,158 |
| Payload bytes | 11,559,154,650 |
| ZIP bytes | 1,594,857,661 (1.59 GB) |
| Reviewed anchors | 18, all matched |
| Captured settings files | 49, selected combat fields only |
| Source verification | Complete file set and all hashes rechecked before sealing |
| Corrected ZIP SHA-256 | `09ad2306d2cf9fb2ac525258323ce2a82c86ca8a12c804f2327534694a7564ea` |
| Separate runtime ZIP SHA-256 | `04da57a13b191005730dfead6eb0a530616f53ed7011b36885a291e1d94e84b6` |

The local artifact to use is `TestResults/balance/exports/starter-baselines-v1-corrected.zip`, with checksum and correction-receipt sidecars. The same folder includes a concrete recovery README, a matching bootstrap tool folder extracted from the corrected ZIP, the verified runtime ZIP and source/hash receipt, and the predeclared external restore paths. The ZIP preserves the accepted manifests unchanged; it does not promote a replacement baseline.

The original ZIP remains retained with SHA-256 `68ac2a41426674a24ecd721534341cbf9bf9af4ec8ff79df27d719d73552c64f`. PowerShell can change its working location without changing the process directory used by one-argument `.NET GetFullPath`; a direct changed-directory check exposed that mismatch. The four command/test scripts now resolve paths against `$PWD.Path`. A new ZIP copy replaces only those four script entries and updates their index records; every other payload hash is preserved. The bounded correction script and before/after hashes are retained beside the archives. All fifteen archive/command checks pass, including the changed-directory case. No game-evidence file, policy or sample changed.

| Restored suite | Saved battles verified | Compared cells | Existing policy | Matched replays |
| --- | ---: | ---: | --- | ---: |
| Blood Grove accepted reference | 20,000 | 2 | Pass | 2 |
| Crystal Creek accepted reference | 32,000 | 16 | Pass | 2 |
| Blood Grove combined-regression copy | 20,000 | 2 | Pass | 2 |
| Crystal Creek combined-regression copy | 32,000 | 16 | Pass | 2 |

These inspect the existing records, including repeated schedules. They add **zero independent samples**. The accepted estimates and their confidence intervals are unchanged; no baseline was promoted. Recovery on the original runtime does not certify gameplay under the system's newer 10.0.12 runtime.

The final report and its 44 supporting evaluation/comparison/replay files were copied back with matching checksums to `TestResults/balance/exports/verification-corrected`. Its `results.json` SHA-256 is `91be13b77cb660bc417467565a1d22b679fafe028f8dd699bb519739d11da86d`. A separate index audit confirms exactly four changed packaging scripts, **428,154 unchanged payload files**, and an unchanged package contract. The original export receipt, correction receipt and both versions of the archive remain available for inspection.

## Verification and changed files

- `./build/test-balance-evidence.ps1 -OutputDirectory TestResults/balance/package-tool-tests-path-corrected`: **15 checks passed**. All four command entry points parse successfully.
- `./build/export-starter-balance.ps1 -ArchivePath TestResults/balance/exports/starter-baselines-v1.zip`: completed; eighteen reviewed anchors, original retention manifests, all source files and 49 captured settings files verified.
- The retained correction script produced `starter-baselines-v1-corrected.zip`; an independent comparison verified the exact four-file correction boundary and unchanged contract.
- `restore-starter-balance.ps1` with the corrected ZIP and its expected SHA-256: **428,158 files restored and verified** in a new folder outside the checkout. The original ZIP's full restore also passed.
- The restored `verify-starter-balance-package.ps1`, with relative paths after changing directory and an explicit isolated `-DotnetPath`: completed with exit 0, four passing saved-suite checks and eight matching replays.
- Scoped whitespace, local documentation targets, archive/receipt hashes and preserved acceptance anchors were checked after documentation updates.

New repository files are the four PowerShell entry points, `BalanceEvidenceArchive.cs`, the versioned `starter-balance-package-v1.json` contract, the portable usage guide and this review. The main harness plan, harness README, combined-regression follow-up and post-alpha roadmap now record verified local recovery. No game code, combat content, accepted fixture/policy, game/environment configuration, migration, deployment, API restart or test-character change was made. Backend/frontend suites and hosted CI were not rerun for this packaging-only increment; the 2,119-test backend result retained inside the archive remains the 8 September verification. No required command remains blocked.

## Storage and workflow boundary

The user chose **leave it local for now**. The archive and checksum/receipt sidecars will remain under ignored `TestResults/balance/exports`; off-device storage is deliberately deferred. A locally verified ZIP is not an off-device backup. Existing evidence remains in place.

The [portable usage guide](../build/STARTER_BALANCE_PACKAGE.md) documents export, restore, verification and platform requirements. This work does not upload artifacts, create a release, publish code, enable a hosted gameplay gate, deploy services, apply migrations, restart the API or modify the test character. Current hosted verification remains pending publication and a separate workflow/retention decision.
