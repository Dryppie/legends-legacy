<#
.SYNOPSIS
Preview or apply/retry/rollback equipment conversion against an isolated LOCAL database copy.
.DESCRIPTION
Start a local LiveOps host against the isolated copy with live rules 17 and all workers stopped.
The script never applies an EF migration or activates version 18. Authentication uses
LL_REHEARSAL_TOKEN, or the existing loopback Development operator with -UseDevelopmentOperator.
Authentication material is never written to artifacts. See docs/analysis/attribute-redesign-rollout.md.
#>
param(
    [Parameter(Mandatory)][Uri]$BaseUri,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateRange(1, 10000)][int]$MaximumItems = 30,
    [switch]$ApplyAndRollback,
    [switch]$UseDevelopmentOperator,
    [ValidateRange(1, 2147483647)][int]$SourceBalanceVersion = 1,
    [ValidateRange(2, 2147483647)][int]$TargetBalanceVersion = 2
)
$ErrorActionPreference = 'Stop'
if (!$BaseUri.IsLoopback -or $BaseUri.Scheme -notin @('http', 'https') -or $BaseUri.UserInfo) {
    throw 'Only a loopback LiveOps host for an isolated database copy is supported.'
}
if (!$UseDevelopmentOperator -and [string]::IsNullOrWhiteSpace($env:LL_REHEARSAL_TOKEN)) {
    throw 'Set LL_REHEARSAL_TOKEN for a local SuperAdmin, or use -UseDevelopmentOperator on an isolated Development host.'
}
$artifactRoot = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $artifactRoot) { throw 'Choose a new output directory; evidence is never overwritten.' }
$null = New-Item -ItemType Directory -Path $artifactRoot
$origin = $BaseUri.AbsoluteUri.TrimEnd('/')
$headers = @{}
$session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
if ($UseDevelopmentOperator) {
    # Do not follow a redirect to an external identity provider or persist the cookie/token.
    $login = Invoke-WebRequest -Uri "$origin/auth/login?returnUrl=/auth/session" -WebSession $session `
        -MaximumRedirection 0 -SkipHttpErrorCheck -ErrorAction SilentlyContinue
    if ($login.StatusCode -ne 302 -or [string]$login.Headers.Location -ne '/auth/session') {
        throw 'The host did not accept the loopback Development operator login.'
    }
    $identity = Invoke-RestMethod -Uri "$origin/auth/session" -WebSession $session -MaximumRedirection 0
    if (!$identity.isDevelopmentOperator -or $identity.environment -ne 'Development') {
        throw 'Expected the existing Development operator session.'
    }
    $antiforgery = Invoke-RestMethod -Uri "$origin/auth/antiforgery" -WebSession $session -MaximumRedirection 0
    if ([string]::IsNullOrWhiteSpace($antiforgery.requestToken)) { throw 'Missing operator antiforgery token.' }
    $headers['X-XSRF-TOKEN'] = $antiforgery.requestToken
}
else {
    $headers.Authorization = "Bearer $env:LL_REHEARSAL_TOKEN"
}
$api = $origin + '/api/liveops/equipment-migration'
function Invoke-Migration([string]$Method, [string]$Route, $Body = $null, [switch]$AllowRejection) {
    $arguments = @{ Method = $Method; Uri = "$api/$Route"; Headers = $headers; WebSession = $session; MaximumRedirection = 0 }
    if ($null -ne $Body) { $arguments.Body = $Body | ConvertTo-Json -Depth 100 -Compress; $arguments.ContentType = 'application/json' }
    $response = Invoke-RestMethod @arguments
    if ($AllowRejection) { return $response }
    if (!$response.IsSuccess) { throw "Migration request failed: $($response.ErrorMessage)" }
    return $response.Data
}
function Save-Evidence([string]$Name, $Value) {
    $Value | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $artifactRoot $Name) -Encoding utf8
}
$targets = [Collections.Generic.List[object]]::new()
$page = 0
do {
    $audit = Invoke-Migration GET "audit?page=$page&pageSize=100&sourceBalanceVersion=$SourceBalanceVersion"
    Save-Evidence "audit-$page.json" $audit
    foreach ($target in $audit.Targets) {
        if ($targets.Count -lt $MaximumItems) { $targets.Add($target) }
    }
    $page++
} while ($audit.Targets.Count -eq 100 -and $targets.Count -lt $MaximumItems)
$blocked = [Collections.Generic.List[object]]::new()
$previewed = 0
$roundTripped = 0
$unversionedPreviewed = 0
$emptyLegacyPreviewed = 0
foreach ($target in $targets) {
    $response = Invoke-Migration POST preview @{ target = $target; targetBalanceVersion = $TargetBalanceVersion } -AllowRejection
    if (!$response.IsSuccess) {
        if ($response.ErrorCode -ne 'equipment_migration_preview_blocked') { throw "Preview failed: $($response.ErrorMessage)" }
        $blocked.Add(@{ Target = $target; ErrorCode = $response.ErrorCode; ErrorMessage = $response.ErrorMessage })
        Save-Evidence 'blocked-previews.json' @($blocked.ToArray())
        continue
    }
    $preview = $response.Data
    $previewed++
    if ($null -ne $preview.LegacyBefore) {
        $unversionedPreviewed++
        if ($null -eq $preview.Before) { $emptyLegacyPreviewed++ }
    }
    Save-Evidence "$($target.ItemId)-preview.json" $preview
    if (!$ApplyAndRollback) { continue }
    $operation = [Guid]::NewGuid()
    $request = @{ operationId = $operation; target = $target; sourceHash = $preview.SourceHash; definitionId = $preview.After.State.DefinitionId; targetBalanceVersion = $TargetBalanceVersion; expectedResultHash = $preview.ResultHash }
    # Persist the idempotency key before the first request, including uncertain network outcomes.
    Save-Evidence "$operation-request.json" $request
    $receipt = Invoke-Migration POST apply $request
    Save-Evidence "$operation-receipt.json" $receipt
    $retry = Invoke-Migration POST apply $request
    if ($retry.OperationId -ne $receipt.OperationId -or $retry.ResultHash -ne $receipt.ResultHash) { throw 'Apply retry changed its receipt.' }
    $rollback = Invoke-Migration POST "$operation/rollback"
    Save-Evidence "$operation-rollback.json" $rollback
    $rollbackRetry = Invoke-Migration POST "$operation/rollback"
    if ($null -eq $rollbackRetry.RolledBackAtUtc -or $rollbackRetry.RolledBackAtUtc -ne $rollback.RolledBackAtUtc) { throw 'Rollback retry changed its receipt.' }
    $restored = Invoke-Migration POST preview @{ target = $target; targetBalanceVersion = $TargetBalanceVersion }
    if ($restored.SourceHash -ne $preview.SourceHash) { throw 'Rollback did not restore the canonical original equipment hash.' }
    $roundTripped++
}
$finalAudit = Invoke-Migration GET "audit?page=0&pageSize=100&sourceBalanceVersion=$SourceBalanceVersion"
Save-Evidence 'final-audit.json' $finalAudit
Save-Evidence 'summary.json' @{ Targets = $targets.Count; Previewed = $previewed; RoundTripped = $roundTripped;
    BlockedPreviews = $blocked.Count; UnversionedInstances = $finalAudit.UnversionedInstances;
    UnversionedPreviewed = $unversionedPreviewed; EmptyLegacyPreviewed = $emptyLegacyPreviewed;
    UnreferencedUnversionedInstances = $finalAudit.UnreferencedUnversionedInstances;
    UnversionedPendingRewards = $finalAudit.UnversionedPendingRewards }
Write-Output "Previewed $previewed items; round-tripped $roundTripped; blocked previews $($blocked.Count); unversioned instances $($finalAudit.UnversionedInstances). Evidence: $artifactRoot. Rules were not activated."
