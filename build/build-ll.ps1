param(
    [switch]$SkipDockerBuild,
    [switch]$SkipPublish,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$root = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($root)) {
    throw "Unable to resolve the repository root."
}

$root = $root.Trim()
$artifactPath = "$root/artifacts"
$buildpath = "$root/build"
$apiArtifactPath = "$artifactPath/api.ll"
$workerArtifactPath = "$artifactPath/worker.ll"

$DOCKER_REGISTRY = $env:DOCKER_REGISTRY
$IMAGE_TAG = $env:IMAGE_TAG
if($null -eq $IMAGE_TAG) {
    $IMAGE_TAG = "dev1"
}

$BUILD_VERSION = $env:BUILD_VERSION
if($null -eq $BUILD_VERSION) {
    $BUILD_VERSION = "0.0.0"
}

Write-Debug "root: $root"
Write-Debug "artifactPath: $artifactPath"
Write-Debug "buildpath: $buildpath"
Write-Debug "DOCKER_REGISTRY: $DOCKER_REGISTRY"
Write-Debug "IMAGE_TAG: $IMAGE_TAG"
Write-Debug "BUILD_VERSION: $BUILD_VERSION"

if (-not $SkipPublish) {
    foreach ($publishPath in @($apiArtifactPath, $workerArtifactPath)) {
        if (Test-Path -LiteralPath $publishPath) {
            Remove-Item -LiteralPath $publishPath -Recurse -Force
        }

        New-Item -ItemType Directory -Path $publishPath -Force | Out-Null
    }

    Write-Debug "--- dotnet publish ---"
    $commonPublishArguments = @(
        "-c", "Release",
        "/p:UseAppHost=false",
        "--no-self-contained"
    )
    if ($NoBuild) {
        $commonPublishArguments += @("--no-build", "--no-restore")
    }

    & dotnet publish "$root/LL/src/API/API.LL/API.LL.csproj" -o $apiArtifactPath @commonPublishArguments
    if ($LASTEXITCODE -ne 0) {
        throw "API publish failed with exit code $LASTEXITCODE."
    }

    & dotnet publish "$root/LL/src/Worker/Worker.LL/Worker.LL.csproj" -o $workerArtifactPath @commonPublishArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Worker publish failed with exit code $LASTEXITCODE."
    }
}

if ($SkipDockerBuild) {
    Write-Debug "Docker image builds skipped."
    return
}

Write-Debug "--- docker build ---"
foreach ($publishPath in @($apiArtifactPath, $workerArtifactPath)) {
    if (-not (Test-Path -LiteralPath $publishPath -PathType Container)) {
        throw "Published artifact directory '$publishPath' was not found."
    }
}

docker build --push -f "$buildpath/ll-backend.dockerfile" "$apiArtifactPath/." --tag "$($DOCKER_REGISTRY)ll-backend:$IMAGE_TAG"
if ($LASTEXITCODE -ne 0) {
    throw "API image build failed with exit code $LASTEXITCODE."
}

docker build --push -f "$buildpath/ll-worker.dockerfile" "$workerArtifactPath/." --tag "$($DOCKER_REGISTRY)ll-worker:$IMAGE_TAG"
if ($LASTEXITCODE -ne 0) {
    throw "Worker image build failed with exit code $LASTEXITCODE."
}
