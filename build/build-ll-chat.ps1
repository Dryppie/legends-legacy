param(
    [switch]$SkipDockerBuild
)

$ErrorActionPreference = "Stop"

$root = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($root)) {
    throw "Unable to resolve the repository root."
}

$root = $root.Trim()
$artifactPath = "$root/artifacts"
$buildpath = "$root/build"
$chatArtifactPath = "$artifactPath/api.chat"

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

if (Test-Path -LiteralPath $chatArtifactPath) {
    Remove-Item -LiteralPath $chatArtifactPath -Recurse -Force
}

New-Item -ItemType Directory -Path $chatArtifactPath -Force | Out-Null

Write-Debug "--- dotnet publish ---"
dotnet publish "$root/LL-Chat/API/API.Chat/API.Chat.csproj" -c Release -o $chatArtifactPath /p:UseAppHost=false --no-self-contained
if ($LASTEXITCODE -ne 0) {
    throw "Chat API publish failed with exit code $LASTEXITCODE."
}

if ($SkipDockerBuild) {
    Write-Debug "Docker image build skipped."
    return
}

Write-Debug "--- docker build ---"
docker build --push -f "$buildpath/ll-chat.dockerfile" "$chatArtifactPath/." --tag "$($DOCKER_REGISTRY)ll-chat:$IMAGE_TAG"
if ($LASTEXITCODE -ne 0) {
    throw "Chat API image build failed with exit code $LASTEXITCODE."
}
