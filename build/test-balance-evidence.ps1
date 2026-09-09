#requires -Version 7.4
[CmdletBinding()]
param([string]$OutputDirectory = (Join-Path ([IO.Path]::GetTempPath()) ('balance-evidence-tests-' + [guid]::NewGuid().ToString('N'))))
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not ('LegendsLegacy.Tools.BalanceEvidenceArchive' -as [type])) {
    Add-Type -Path (Join-Path $PSScriptRoot 'BalanceEvidenceArchive.cs')
}
$testRoot = [IO.Path]::GetFullPath($OutputDirectory, $PWD.Path)
if (Test-Path -LiteralPath $testRoot) { throw 'Test output already exists.' }
$sourceRoot = Join-Path $testRoot 'source'
$null = New-Item -ItemType Directory -Path (Join-Path $sourceRoot 'payload/nested')
$data = '{"startsAt":"2000-01-01T00:00:00+00:00","value":0.4400}'
[IO.File]::WriteAllText((Join-Path $sourceRoot 'payload/nested/fixture.json'), $data)
[IO.File]::WriteAllBytes((Join-Path $sourceRoot 'payload/binary.dat'), [byte[]](0..255))
$script:passed = 0
function Reject([string]$name, [scriptblock]$action, [string]$expected) {
    $caught = $null
    try { & $action } catch { $caught = $_.Exception.ToString() }
    if (-not $caught -or $caught -notmatch $expected) { throw "Expected rejection for $name; got: $caught" }
    $script:passed++
}
$archive = Join-Path $testRoot 'valid.zip'
$index = [LegendsLegacy.Tools.BalanceEvidenceArchive]::Create($sourceRoot, $archive, @('payload'), '{"id":"test"}')
$hash = [LegendsLegacy.Tools.BalanceEvidenceArchive]::Hash($archive)
$restored = Join-Path $testRoot 'restored'
$null = [LegendsLegacy.Tools.BalanceEvidenceArchive]::Restore($archive, $hash, $restored)
$verified = [LegendsLegacy.Tools.BalanceEvidenceArchive]::Verify($restored)
if ($index.Files.Count -ne 2 -or $verified.Files.Count -ne 2 -or
    [IO.File]::ReadAllText((Join-Path $restored 'payload/nested/fixture.json')) -cne $data) { throw 'Round trip changed bytes.' }
$script:passed++
Push-Location $testRoot
try {
    & (Join-Path $PSScriptRoot 'restore-starter-balance.ps1') -ArchivePath 'valid.zip' -ExpectedSha256 $hash -OutputDirectory 'relative-restored'
    if (-not (Test-Path -LiteralPath (Join-Path $testRoot 'relative-restored/package.json'))) { throw 'Relative paths ignored the PowerShell location.' }
} finally { Pop-Location }
$script:passed++
Reject 'existing archive' { [LegendsLegacy.Tools.BalanceEvidenceArchive]::Create($sourceRoot, $archive, @('payload'), '{}') } 'already exists'
Reject 'archive inside input' { [LegendsLegacy.Tools.BalanceEvidenceArchive]::Create($sourceRoot, (Join-Path $sourceRoot 'payload/inside.zip'), @('payload'), '{}') } 'outside its selected inputs'
Reject 'existing restore' { [LegendsLegacy.Tools.BalanceEvidenceArchive]::Restore($archive, $hash, $restored) } 'already exists'
$badHashRoot = Join-Path $testRoot 'bad-hash'
Reject 'wrong archive hash' { [LegendsLegacy.Tools.BalanceEvidenceArchive]::Restore($archive, ('0' * 64), $badHashRoot) } 'SHA-256'
if (Test-Path $badHashRoot) { throw 'Wrong hash created output.' }
$fixture = Join-Path $restored 'payload/nested/fixture.json'
[IO.File]::WriteAllText($fixture, $data.Replace('0.4400','0.4500'))
Reject 'same-length corruption' { [LegendsLegacy.Tools.BalanceEvidenceArchive]::Verify($restored) } 'checksum mismatch'
[IO.File]::WriteAllText($fixture, $data)
$extra = Join-Path $restored 'unexpected.txt'
[IO.File]::WriteAllText($extra, 'extra')
Reject 'unexpected file' { [LegendsLegacy.Tools.BalanceEvidenceArchive]::Verify($restored) } 'Unexpected file'
Remove-Item -LiteralPath $extra
Remove-Item -LiteralPath $fixture
Reject 'missing file' { [LegendsLegacy.Tools.BalanceEvidenceArchive]::Verify($restored) } 'files are missing'
[IO.File]::WriteAllText($fixture, $data)

# Even on a case-sensitive host, an extra differently cased index must not be ignored.
$indexPath = Join-Path $restored 'package.json'
Rename-Item -LiteralPath $indexPath -NewName 'PACKAGE.JSON'
Reject 'index filename case' { [LegendsLegacy.Tools.BalanceEvidenceArchive]::Verify($restored) } 'Unexpected file|Could not find|not find'
Rename-Item -LiteralPath (Join-Path $restored 'PACKAGE.JSON') -NewName 'package.json'

foreach ($case in @(
    @{ Name='traversal'; Entries=@('../escape.txt'); Expected='Unsafe package path' },
    @{ Name='absolute'; Entries=@('C:/escape.txt'); Expected='Unsafe package path' },
    @{ Name='alternate-stream'; Entries=@('payload/file:stream'); Expected='Unsafe package path' },
    @{ Name='case-collision'; Entries=@('payload/file','payload/FILE'); Expected='Duplicate' },
    @{ Name='symlink'; Entries=@('payload/link'); Expected='linked archive' }
)) {
    $badArchive = Join-Path $testRoot "$($case.Name).zip"
    $zip = [IO.Compression.ZipFile]::Open($badArchive, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($name in $case.Entries) {
            $entry = $zip.CreateEntry($name)
            if ($case.Name -eq 'symlink') { $entry.ExternalAttributes = -1577058304 }
            $stream = $entry.Open()
            try { $stream.WriteByte(65) } finally { $stream.Dispose() }
        }
    } finally { $zip.Dispose() }
    $badOutput = Join-Path $testRoot "$($case.Name)-output"
    Reject $case.Name { [LegendsLegacy.Tools.BalanceEvidenceArchive]::Restore($badArchive, [LegendsLegacy.Tools.BalanceEvidenceArchive]::Hash($badArchive), $badOutput) } $case.Expected
    if (Test-Path -LiteralPath $badOutput) { throw 'Invalid archive created output.' }
}
if ([LegendsLegacy.Tools.BalanceEvidenceArchive]::Hash($archive) -ne $hash) { throw 'Guard checks changed original archive.' }
@{ Status='Pass'; Checks=$script:passed; OutputDirectory=$testRoot } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $testRoot 'results.json') -Encoding utf8
Write-Host "$script:passed archive integrity/restore checks passed. Evidence: $testRoot"
