# Starter baseline evidence package v1

This package preserves the accepted level-5 Blood Grove and level-10 Crystal Creek starter references, their exact executables, full supporting fine-tuning/compatibility evidence, the earlier Blood Grove Inconclusive run, and both attempts at the combined regression. Accepted manifests and archived files are copied byte-for-byte. No baseline is promoted and no independent samples are added.

The two selected starter recipes use Goblin Warrior, a Shortsword and Heavy Breastplate; level 10 adds an Amulet and Goblin in slot 2. Neither primary recipe equips a Combat Style. Both fixed pairs at each checkpoint enforce 50–90% wins, with no duration target. The remaining fourteen Creek cells are diagnostics.

## Verified local package — 9 September 2026

Use `TestResults/balance/exports/starter-baselines-v1-corrected.zip`. Its SHA-256 is `09ad2306d2cf9fb2ac525258323ce2a82c86ca8a12c804f2327534694a7564ea`. Independent recovery verified all 428,158 payload files, four saved suites and eight fixed replays. The [package review](../Balance%20Harness/Starter-Baseline-Package.md) records the results, exact runtime and correction history.

The original `starter-baselines-v1.zip` is retained as engineering history. The corrected ZIP fixes relative-path handling in four packaging scripts; accepted evidence and gameplay files are unchanged. Use the existing corrected package for recovery. A new export is only needed for a separately reviewed package, with a new output name. Later documentation updates do not change either sealed archive or its historical Markdown snapshots.

## Files to retain

Keep the ZIP, its `.sha256` and `.receipt.json` sidecars, and the trusted restore tools together. The outer SHA-256 identifies the complete archive. `package.json` inventories the length and SHA-256 of every payload file and embeds the versioned input/verification contract. The archive retains the repository-relative layout, including `TestResults/balance/baselines`, so the original relative baseline links continue to resolve.

Historical reports contain absolute paths from their original machine. Those strings are retained as provenance; restoration and verification use paths relative to the package root. Do not run old experiment orchestration scripts to verify an archive. The supported verifier below only reads saved suites and replays fixed battles, using each suite's matching retained executable.

## Export from the original checkout

PowerShell 7.4 or later is required. The archive helper uses the .NET libraries bundled with PowerShell; no new package manager or NuGet dependency is required. Run from the repository root:

```powershell
./build/test-balance-evidence.ps1
./build/export-starter-balance.ps1 -ArchivePath TestResults/balance/exports/starter-baselines-v1-new-export.zip
```

The versioned contract in `build/starter-balance-package-v1.json` names all required inputs and pins reviewed manifest/policy/result hashes. Export checks the original retention manifests, copies every file, and rechecks the source file set and hashes before sealing the ZIP. Source drift, missing evidence or existing output causes failure. A failed partial archive is retained for inspection and receives no success receipt; retry with a new path.

## Restore and verify

The concrete commands below use the completed local recovery kit. Start in `TestResults/balance/exports` with PowerShell 7.4 or later. The restore, runtime and verification directories must be new; verification output must be outside the immutable restored package. The expected hashes are recorded in the package review and retained receipts:

```powershell
./starter-baselines-v1-corrected-recovery-tools/restore-starter-balance.ps1 -ArchivePath ./starter-baselines-v1-corrected.zip -ExpectedSha256 09ad2306d2cf9fb2ac525258323ce2a82c86ca8a12c804f2327534694a7564ea -OutputDirectory ./restored-starter-baselines

$runtimeZip = Join-Path $PWD.Path 'dotnet-runtime-10.0.11-win-x64.zip'
if ((Get-FileHash -LiteralPath $runtimeZip -Algorithm SHA256).Hash -ne '04da57a13b191005730dfead6eb0a530616f53ed7011b36885a291e1d94e84b6') { throw 'Runtime archive checksum differs.' }
[IO.Compression.ZipFile]::ExtractToDirectory($runtimeZip, (Join-Path $PWD.Path 'replay-runtime'))

./restored-starter-baselines/build/verify-starter-balance-package.ps1 -PackageDirectory ./restored-starter-baselines -OutputDirectory ./starter-restore-verification -DotnetPath (Join-Path $PWD.Path 'replay-runtime/dotnet.exe')
```

Restore checks the ZIP hash before extraction, validates the complete entry list, rejects unsafe/duplicate/link paths, and checks each extracted file against the inventory. It executes no packaged code. The verifier checks the complete restored file inventory before using any retained executable. Use only a trusted archive and trusted tools.

Full verification requires the original **.NET 10.0.11 / Microsoft Windows 10.0.26200 / X64** execution identity. It evaluates all four saved accepted/regression suites, compares them with the two accepted manifests, and replays trial index 0 in their primary cells: eight replays total. These are integrity checks, not new sampling. Replay intentionally rejects different runtimes/platforms; do not weaken that check after an OS/runtime upgrade.

If the system runtime has moved forward, obtain the Windows X64 .NET 10.0.11 runtime ZIP from [Microsoft's release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json), verify its published SHA-512, and extract it into a separate local directory. Pass its `dotnet.exe` explicitly with `-DotnetPath`; installing or downgrading the system runtime is unnecessary. The recorded runtime ZIP SHA-512 is `d9ab9c0d9916b8fa3585b5f403057f594ffffb8364dac09e0007dd8ac671c86754935b980d8fb5da83cb1b82ac3cd57cc407c969e6d837aaa2fae21047cb7448`.

On another platform, or before installing the matching runtime, verify file integrity without executing the harness:

```powershell
./restored-starter-baselines/build/verify-starter-balance-package.ps1 -PackageDirectory ./restored-starter-baselines -OutputDirectory ./starter-integrity-verification -IntegrityOnly
```

`IntegrityOnly` is not gameplay replay certification. The same Windows/platform constraints also apply to comparisons intended to control for execution-environment differences. The archived Win32 relative baseline paths are preserved, rather than silently rewritten for Linux.

## Retention and future integration

The user chose **local storage for now**. Keep the corrected ZIP, checksum/receipt, recovery tools and separate verified runtime ZIP together under `TestResults/balance/exports`. Off-device storage is deferred; it is not a blocker for the completed local recovery milestone. If that choice changes, copy the kit to the chosen destination, verify its checksums there, and record the destination/version before relying on that copy.

No upload, GitHub release, hosted gameplay gate, deployment or migration is performed by these tools. The existing seven-day CI smoke artifacts are separate from this accepted evidence package. Hosted integration still requires publication of the implementation and a deliberate retention/workflow decision.
