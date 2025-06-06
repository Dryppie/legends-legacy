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
dotnet publish "$root/LL-Chat/API/API.Chat/API.Chat.csproj" -c Release -o $artifactPath/api.ll /p:UseAppHost=false --no-self-contained

Write-Debug "--- docker build ---"
docker buildx build --push -f "$buildpath/ll-chat.dockerfile" $artifactPath/api.ll/. --platform linux/amd64,linux/arm64 --tag "$($DOCKER_REGISTRY)ll-chat:$IMAGE_TAG"