<#
.SYNOPSIS
Runs the real PostgreSQL equipment integration rehearsal in a new local TEMP cluster.
.EXAMPLE
./build/run-postgres-equipment-rehearsal.ps1 -PostgresBin C:/tools/pgsql/bin
#>
param(
    [Parameter(Mandatory = $true)][string]$PostgresBin,
    [int]$Port = 55439
)
$ErrorActionPreference = "Stop"
if ($Port -lt 1024 -or $Port -gt 65535) { throw "Choose an unprivileged local port." }
$PostgresBin = (Resolve-Path -LiteralPath $PostgresBin).Path
foreach ($tool in @("initdb.exe", "pg_ctl.exe")) {
    if (-not (Test-Path -LiteralPath (Join-Path $PostgresBin $tool))) { throw "Missing PostgreSQL binary: $tool" }
}
$repository = (git rev-parse --show-toplevel).Trim()
$rehearsalRoot = Join-Path $env:TEMP ("ll-equipment-pg-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $rehearsalRoot | Out-Null
$pgData = Join-Path $rehearsalRoot "data"
$pwFile = Join-Path $rehearsalRoot "password.txt"
$password = [Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$password | Set-Content -LiteralPath $pwFile
$oldConnection = $env:LL_REHEARSAL_POSTGRES_CONNECTION
$oldApiRoot = $env:LL_TEST_API_ROOT
$started = $false
function Invoke-PgCtl([string[]]$Arguments) {
    $process = Start-Process -FilePath (Join-Path $PostgresBin "pg_ctl.exe") -ArgumentList $Arguments -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $rehearsalRoot "pgctl.log") -RedirectStandardError (Join-Path $rehearsalRoot "pgctl-error.log")
    if (-not $process.WaitForExit(60000)) { throw "PostgreSQL control timed out; inspect $rehearsalRoot." }
    if ($process.ExitCode -ne 0) { throw "PostgreSQL control failed; inspect $rehearsalRoot." }
}
try {
    & (Join-Path $PostgresBin "initdb.exe") -D $pgData -U postgres --encoding=UTF8 --locale=C --auth=scram-sha-256 "--pwfile=$pwFile"
    if ($LASTEXITCODE -ne 0) { throw "initdb failed." }
    Invoke-PgCtl @("start", "-D", "`"$pgData`"", "-l", "`"$(Join-Path $rehearsalRoot 'server.log')`"", "-o", "`"-h 127.0.0.1 -p $Port`"")
    $started = $true
    $env:LL_REHEARSAL_POSTGRES_CONNECTION = "Host=127.0.0.1;Port=$Port;Database=postgres;Username=postgres;Password=$password"
    $env:LL_TEST_API_ROOT = Join-Path $repository "LL/src/API/API.LL"
    & (Join-Path $repository "build/run-tests.ps1") -Filter "FullyQualifiedName~EquipmentPostgresRehearsalTests" `
        -ArtifactsPath (Join-Path $rehearsalRoot "build")
}
finally {
    $env:LL_REHEARSAL_POSTGRES_CONNECTION = $oldConnection
    $env:LL_TEST_API_ROOT = $oldApiRoot
    if ($started) { Invoke-PgCtl @("stop", "-D", "`"$pgData`"", "-m", "fast") }
    Remove-Item -LiteralPath $pwFile -ErrorAction SilentlyContinue
    Write-Host "Rehearsal files retained at $rehearsalRoot"
}
