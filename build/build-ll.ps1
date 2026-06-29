$root = git rev-parse --show-toplevel
$artifactPath = "$root/artifacts"
$buildpath = "$root/build"

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

Write-Debug "--- dotnet publish ---"
dotnet publish "$root/LL/src/API/API.LL/API.LL.csproj" -c Release -o $artifactPath/api.ll /p:UseAppHost=false --no-self-contained
dotnet publish "$root/LL/src/Worker/Worker.LL/Worker.LL.csproj" -c Release -o $artifactPath/worker.ll /p:UseAppHost=false --no-self-contained

Write-Debug "--- docker build ---"
docker build --push -f "$buildpath/ll-backend.dockerfile" $artifactPath/api.ll/. --tag "$($DOCKER_REGISTRY)ll-backend:$IMAGE_TAG"
docker build --push -f "$buildpath/ll-worker.dockerfile" $artifactPath/worker.ll/. --tag "$($DOCKER_REGISTRY)ll-worker:$IMAGE_TAG"
