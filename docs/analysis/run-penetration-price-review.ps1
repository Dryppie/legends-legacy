param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [double[]]$Prices = @(1.5, 3, 4, 6, 8),
    [ValidateSet('screen','confirm')][string]$Phase = 'screen'
)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$reviewRoot = (Resolve-Path $OutputDirectory).Path
$source = Join-Path $repoRoot 'LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentStatBudgetCatalog.cs'
$sourceHash = (Get-FileHash -LiteralPath $source).Hash
$protocol = Get-Content -LiteralPath (Join-Path $reviewRoot 'protocol.json') -Raw | ConvertFrom-Json
if ($sourceHash -ne $protocol.sourcePriceHash) { throw 'Production price source changed after review preparation.' }
$original = [IO.File]::ReadAllText($source)
$needle = 'AttributeType.ArmorPenetration or AttributeType.MagicPenetration => 1.5d,'
if (-not $original.Contains($needle)) { throw 'Unexpected production price; review must be prepared again.' }

foreach ($price in $Prices) {
    if ($price -notin @(1.5,3,4,6,8)) { throw "Price $price was not declared by the protocol." }
    $number = $price.ToString([Globalization.CultureInfo]::InvariantCulture)
    $label = $number.Replace('.','_')
    $buildRoot = Join-Path $reviewRoot "build-price-$label"
    $executable = Join-Path $buildRoot 'bin/BalanceHarness/release/BalanceHarness.dll'
    if (-not (Test-Path -LiteralPath $executable)) {
        $overlay = Join-Path $reviewRoot "EquipmentStatBudgetCatalog.price-$label.cs"
        [IO.File]::WriteAllText($overlay, $original.Replace($needle,
            "AttributeType.ArmorPenetration or AttributeType.MagicPenetration => ${number}d,"))
        $targets = Join-Path $reviewRoot "price-$label.targets"
        $xml = @'
<Project>
  <Target Name="ReviewPenetrationPrice" BeforeTargets="CoreCompile" Condition="'$(MSBuildProjectName)' == 'Domain'">
    <ItemGroup>
      <Compile Remove="@(Compile)" Condition="'%(Compile.Filename)%(Compile.Extension)' == 'EquipmentStatBudgetCatalog.cs'" />
      <Compile Include="OVERLAY_SOURCE" />
    </ItemGroup>
    <Message Importance="high" Text="Compiling isolated penetration price overlay" />
  </Target>
</Project>
'@
        [IO.File]::WriteAllText($targets, $xml.Replace('OVERLAY_SOURCE', [Security.SecurityElement]::Escape($overlay)))
        Write-Output "Building isolated penetration price $number."
        & dotnet build (Join-Path $repoRoot 'LL/tools/BalanceHarness/BalanceHarness.csproj') -c Release `
            --artifacts-path $buildRoot "-p:CustomBeforeMicrosoftCommonTargets=$targets" `
            *> (Join-Path $reviewRoot "build-price-$label.log")
        if ($LASTEXITCODE -ne 0) { throw "Price $number build failed; see its build log." }
        [ordered]@{price=$price; productionSourceHash=$sourceHash; overlaySourceHash=(Get-FileHash $overlay).Hash;
            domainAssemblyHash=(Get-FileHash (Join-Path $buildRoot 'bin/Domain/release/Domain.dll')).Hash} |
            ConvertTo-Json | Set-Content -LiteralPath (Join-Path $reviewRoot "build-price-$label.json")
    }
    Write-Output "Running $Phase for penetration price $number."
    & dotnet $executable attribute-allocation-study (Join-Path $reviewRoot "$Phase-price-$label.json") `
        *> (Join-Path $reviewRoot "run-$Phase-price-$label.log")
    if ($LASTEXITCODE -ne 0) { throw "Price $number $Phase failed; see its run log." }
    Write-Output "Completed $Phase for penetration price $number."
}
if ((Get-FileHash -LiteralPath $source).Hash -ne $sourceHash) { throw 'Production price source changed during review.' }
