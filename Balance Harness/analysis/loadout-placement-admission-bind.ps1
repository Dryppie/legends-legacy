param([Parameter(Mandatory)][string]$Package)
$ErrorActionPreference = 'Stop'
$placementAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $Package 'runtime/BalanceHarness.dll'))
$placementFlags = [Reflection.BindingFlags]'Static,Instance,Public,NonPublic,DeclaredOnly'
$placementTraceType = $placementAssembly.GetType('BalanceHarness.TowerPerformanceTrace', $true)
$placementTrace = $placementTraceType.GetConstructor($placementFlags, $null, [Type[]]@([Action[bool]]), $null).Invoke(
    [object[]]@([Action[bool]]{ param($completed) throw 'Admission binding forbids combat.' }))
$placementGuard = $placementTrace.Activate()
try {
    $placementContext = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalContext]((Join-Path $Package 'context-draft.json'))
    $placementOldPlan = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalComparisonPlan]((Join-Path $Package 'qualified-plan.json'))
    $placementRecreate = $placementAssembly.GetType('BalanceHarness.TowerProposalComparison', $true).GetMethod('Recreate', $placementFlags)
    $placementArguments = [object[]]::new(2)
    $placementArguments.SetValue($placementOldPlan, 0)
    $placementArguments.SetValue($placementContext, 1)
    $placementPlan = $placementRecreate.Invoke($null, $placementArguments)
    [BalanceHarness.HarnessJson]::WriteNew[object]((Join-Path $Package 'context.json'), $placementContext)
    [BalanceHarness.HarnessJson]::WriteNew[object]((Join-Path $Package 'plan.json'), $placementPlan)
    [BalanceHarness.HarnessJson]::WriteNew[object]((Join-Path $Package 'binding.json'), @{
        status = 'QualifiedPlacementPlanReboundNoReservation'
        contextHash = [BalanceHarness.HarnessJson]::Hash[object]($placementContext)
        planHash = [BalanceHarness.HarnessJson]::Hash[object]($placementPlan)
        executionHash = $placementContext.Scope.ExecutionHash
        fights = 0; newValues = 0
    })
} finally { $placementGuard.Dispose() }
