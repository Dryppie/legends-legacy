$ErrorActionPreference = 'Stop'
# Keep test invocations from writing step outputs used by the real classifier.
$savedOutput = $env:GITHUB_OUTPUT
$savedSummary = $env:GITHUB_STEP_SUMMARY
try {
    $env:GITHUB_OUTPUT = ''
    $env:GITHUB_STEP_SUMMARY = ''
    $cases = @(
        @{ Paths = @(); Harness = $false; Smoke = $false },
        @{ Paths = @('LL/src/Presentation/ll/src/app/app.ts'); Harness = $false; Smoke = $false },
        @{ Paths = @('LL/src/API/API.LL/Controllers/AdminController.cs'); Harness = $false; Smoke = $false },
        @{ Paths = @('LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/UserRepository.cs'); Harness = $false; Smoke = $false },
        @{ Paths = @('LL/src/Infrastructure/Service/Services.LL/Administration/ItemService.cs'); Harness = $false; Smoke = $false },
        @{ Paths = @('LL/src/Core/Domain/Models/Users/AppUser.cs'); Harness = $false; Smoke = $false },
        @{ Paths = @('LL/tools/BalanceHarness/README.md'); Harness = $false; Smoke = $false },
        @{ Paths = @('LL/tests/EssenceSystem.Tests/BalanceHarnessTests.cs'); Harness = $true; Smoke = $false },
        @{ Paths = @('LL/tests/EssenceSystem.Tests/CombatStyleHarnessTests.cs'); Harness = $true; Smoke = $false },
        @{ Paths = @('LL/tools/BalanceHarness/Dashboard/studies.js'); Harness = $true; Smoke = $false },
        @{ Paths = @('LL/tools/BalanceHarness/TowerBenchmark.cs'); Harness = $true; Smoke = $true },
        @{ Paths = @('LL/tools/BalanceHarness/Fixtures/idle-reference.json'); Harness = $true; Smoke = $true },
        @{ Paths = @('LL/src/Infrastructure/Service/Services.LL/Combat/Engine/Damage.cs'); Harness = $true; Smoke = $true },
        @{ Paths = @('LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentBalance.cs'); Harness = $true; Smoke = $true },
        @{ Paths = @('LL/src/Core/Application/UseCases/LL/WorldTower/Commands/Enter.cs'); Harness = $true; Smoke = $true },
        @{ Paths = @('LL/src/API/API.LL/Data/world/creatures.json'); Harness = $true; Smoke = $true },
        @{ Paths = @('LL/src/Core/Common/Randomness/StableRandom.cs'); Harness = $true; Smoke = $true },
        @{ Paths = @('Directory.Build.props'); Harness = $true; Smoke = $true },
        @{ Paths = @('LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj'); Harness = $true; Smoke = $true },
        @{ Paths = @('build/balance-paths.json'); Harness = $true; Smoke = $true }
    )
    foreach ($case in $cases) {
        $result = & "$PSScriptRoot/get-balance-impact.ps1" -ChangedPaths $case.Paths
        if ($result.harness -ne $case.Harness -or $result.smoke -ne $case.Smoke) {
            throw "Incorrect impact for $($case.Paths -join ', '): $($result | ConvertTo-Json -Compress)"
        }
    }
    foreach ($args in @(@{ Force = $true; ChangedPaths = @() }, @{ Base = ('0' * 40) })) {
        $result = & "$PSScriptRoot/get-balance-impact.ps1" @args
        if (-not $result.harness -or -not $result.smoke) { throw 'Override or missing base did not run both checks.' }
    }
    # Exercise real Git history: a PR must ignore changes made only on its base
    # branch, while a rename out of gameplay must still select balance checks.
    $fixture = Join-Path ([IO.Path]::GetTempPath()) ('balance-impact-' + [guid]::NewGuid().ToString('N'))
    $null = New-Item -ItemType Directory -Path $fixture
    Push-Location $fixture
    try {
        function GitFixture([string[]]$Arguments) {
            $output = & git @Arguments
            if ($LASTEXITCODE -ne 0) { throw "Git fixture failed: $Arguments" }
            return $output
        }
        $null = GitFixture @('init', '-q', '-b', 'main')
        $null = GitFixture @('config', 'user.email', 'balance-test@example.invalid')
        $null = GitFixture @('config', 'user.name', 'Balance policy test')
        Set-Content readme.txt 'initial'
        $null = GitFixture @('add', '.')
        $null = GitFixture @('-c', 'commit.gpgsign=false', 'commit', '-qm', 'initial')
        $initial = GitFixture @('rev-parse', 'HEAD')
        $null = GitFixture @('checkout', '-qb', 'feature')
        Set-Content readme.txt 'frontend-only equivalent'
        $null = GitFixture @('add', '.')
        $null = GitFixture @('-c', 'commit.gpgsign=false', 'commit', '-qm', 'unrelated')
        $feature = GitFixture @('rev-parse', 'HEAD')
        $null = GitFixture @('checkout', '-q', 'main')
        $null = New-Item -ItemType Directory -Path 'LL/src/API/API.LL/Data' -Force
        Set-Content 'LL/src/API/API.LL/Data/creatures.json' '{}'
        $null = GitFixture @('add', '.')
        $null = GitFixture @('-c', 'commit.gpgsign=false', 'commit', '-qm', 'gameplay')
        $base = GitFixture @('rev-parse', 'HEAD')
        $result = & "$PSScriptRoot/get-balance-impact.ps1" -Base $base -Head $feature -PullRequest
        if ($result.harness -or $result.smoke) { throw 'PR included base-only changes.' }
        $result = & "$PSScriptRoot/get-balance-impact.ps1" -Base $initial -Head $base
        if (-not $result.harness -or -not $result.smoke) { throw 'Push missed gameplay change.' }
        $null = GitFixture @('mv', 'LL/src/API/API.LL/Data/creatures.json', 'moved.json')
        $null = GitFixture @('-c', 'commit.gpgsign=false', 'commit', '-qm', 'rename out of gameplay')
        $result = & "$PSScriptRoot/get-balance-impact.ps1" -Base $base
        if (-not $result.harness -or -not $result.smoke) { throw 'Rename lost the original gameplay path.' }
    } finally {
        Pop-Location
    }
    # New harness classes must not silently leak back into the ordinary suite.
    $testRoot = Join-Path $PSScriptRoot '../LL/tests/EssenceSystem.Tests'
    $files = Get-ChildItem $testRoot -File | Where-Object Name -Match '^(BalanceHarness|CombatStyleHarness).*Tests.cs$'
    foreach ($file in $files) {
        if ([IO.File]::ReadAllText($file.FullName) -notmatch '\[Trait\("Category", "BalanceHarness"\)\]') {
            throw "Missing BalanceHarness category: $($file.Name)"
        }
    }
    Write-Host "Passed $($cases.Count) path cases, manual/missing-base cases, PR/push/rename history cases, and $($files.Count) harness category checks."
} finally {
    $env:GITHUB_OUTPUT = $savedOutput
    $env:GITHUB_STEP_SUMMARY = $savedSummary
}
