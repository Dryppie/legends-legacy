param(
    [Parameter(Mandatory = $true)]
    [string]$ResultsDirectory,

    [Parameter(Mandatory = $true)]
    [string]$Title,

    [int]$Top = 20
)

$ErrorActionPreference = "Stop"

function Format-Duration {
    param([double]$Seconds)

    $duration = [TimeSpan]::FromSeconds($Seconds)
    if ($duration.TotalHours -ge 1) {
        return $duration.ToString("h\:mm\:ss")
    }

    return $duration.ToString("m\:ss\.fff")
}

function Escape-MarkdownCell {
    param([string]$Value)

    return $Value.Replace("|", "\|").Replace("`r", " ").Replace("`n", " ")
}

$trxFiles = Get-ChildItem -LiteralPath $ResultsDirectory -Filter "*.trx" -File -ErrorAction SilentlyContinue
if ($null -eq $trxFiles -or $trxFiles.Count -eq 0) {
    $message = "### $Title`n`nNo TRX results were produced."
    Write-Host $message
    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) {
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $message
    }
    return
}

$results = foreach ($trxFile in $trxFiles) {
    [xml]$trx = Get-Content -Raw -LiteralPath $trxFile.FullName
    $namespace = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
    $namespace.AddNamespace("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

    foreach ($result in $trx.SelectNodes("//t:UnitTestResult", $namespace)) {
        $testName = [string]$result.testName
        $qualifiedName = ($testName -split "\(", 2)[0]
        $segments = $qualifiedName -split "\."
        $className = if ($segments.Count -ge 2) {
            $segments[0..($segments.Count - 2)] -join "."
        }
        else {
            $qualifiedName
        }

        [pscustomobject]@{
            Test = $testName
            Class = $className
            Seconds = [TimeSpan]::Parse(
                [string]$result.duration,
                [System.Globalization.CultureInfo]::InvariantCulture).TotalSeconds
            Outcome = [string]$result.outcome
        }
    }
}

$orderedTests = $results | Sort-Object Seconds -Descending
$orderedClasses = $results |
    Group-Object Class |
    ForEach-Object {
        [pscustomobject]@{
            Class = $_.Name
            Tests = $_.Count
            Seconds = ($_.Group | Measure-Object Seconds -Sum).Sum
        }
    } |
    Sort-Object Seconds -Descending

$passed = @($results | Where-Object Outcome -eq "Passed").Count
$failed = @($results | Where-Object Outcome -eq "Failed").Count
$other = $results.Count - $passed - $failed
$summary = [System.Collections.Generic.List[string]]::new()
$summary.Add("### $Title")
$summary.Add("")
$summary.Add("Tests: **$($results.Count)** · Passed: **$passed** · Failed: **$failed** · Other: **$other**")
$summary.Add("")
$summary.Add("#### Slowest test classes")
$summary.Add("")
$summary.Add("| Class | Tests | Cumulative duration |")
$summary.Add("|---|---:|---:|")
foreach ($class in $orderedClasses | Select-Object -First $Top) {
    $summary.Add("| $(Escape-MarkdownCell $class.Class) | $($class.Tests) | $(Format-Duration $class.Seconds) |")
}

$summary.Add("")
$summary.Add("#### Slowest tests")
$summary.Add("")
$summary.Add("| Test | Duration | Outcome |")
$summary.Add("|---|---:|---|")
foreach ($test in $orderedTests | Select-Object -First $Top) {
    $summary.Add("| $(Escape-MarkdownCell $test.Test) | $(Format-Duration $test.Seconds) | $($test.Outcome) |")
}

$rendered = $summary -join "`n"
Write-Host $rendered
if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) {
    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $rendered
}
