param(
    [string]$BaseUrl = $env:LL_SMOKE_BASE_URL
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
    throw "A smoke API base URL is required through -BaseUrl or LL_SMOKE_BASE_URL."
}

$apiRoot = "$($BaseUrl.TrimEnd('/'))/api/v1"
$runId = [Guid]::NewGuid().ToString("N")
$email = "alpha-smoke-$runId@example.invalid"
$password = "AlphaSmoke-$runId"
$characterName = "Smoke$($runId.Substring(0, 12))"

function Invoke-SmokeJson {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("Get", "Post")]
        [string]$Method,

        [Parameter(Mandatory)]
        [string]$Path,

        [hashtable]$Headers,

        [object]$Body
    )

    $parameters = @{
        Method = $Method
        Uri = "$apiRoot/$Path"
    }

    if ($Headers) {
        $parameters.Headers = $Headers
    }

    if ($null -ne $Body) {
        $parameters.ContentType = "application/json"
        $parameters.Body = $Body | ConvertTo-Json -Depth 10 -Compress
    }

    try {
        return Invoke-RestMethod @parameters
    }
    catch {
        $statusCode = $_.Exception.Response?.StatusCode
        throw "Smoke request $Method $Path failed with HTTP $statusCode. $($_.Exception.Message)"
    }
}

function Get-AccessToken {
    param(
        [Parameter(Mandatory)]
        [object]$Response,

        [Parameter(Mandatory)]
        [string]$Step
    )

    $isEnvelope = $null -ne $Response.PSObject.Properties["isSuccess"]
    if ($isEnvelope -and -not $Response.isSuccess) {
        throw "$Step returned an unsuccessful response. $($Response.errorMessage)"
    }

    $payload = if ($isEnvelope) { $Response.data } else { $Response }
    if ($null -eq $payload -or [string]::IsNullOrWhiteSpace($payload.accessToken)) {
        $json = $Response | ConvertTo-Json -Depth 5 -Compress
        throw "$Step did not return an access token. Response: $json"
    }

    return $payload.accessToken
}

function Assert-Bootstrap {
    param(
        [Parameter(Mandatory)]
        [string]$AccessToken,

        [Parameter(Mandatory)]
        [string]$Step
    )

    $bootstrap = Invoke-SmokeJson `
        -Method Get `
        -Path "GameBootstrap" `
        -Headers @{ Authorization = "Bearer $AccessToken" }

    $isEnvelope = $null -ne $bootstrap.PSObject.Properties["isSuccess"]
    if ($isEnvelope -and -not $bootstrap.isSuccess) {
        throw "$Step bootstrap returned an unsuccessful response. $($bootstrap.errorMessage)"
    }

    $payload = if ($isEnvelope) { $bootstrap.data } else { $bootstrap }
    if ($null -eq $payload -or $null -eq $payload.character) {
        throw "$Step bootstrap did not include a character."
    }

    if ([string]::IsNullOrWhiteSpace($payload.serverTimeUtc)) {
        throw "$Step bootstrap did not include server time."
    }

    return $payload
}

Write-Output "Smoke: creating a clean guest account."
$guestResponse = Invoke-SmokeJson -Method Post -Path "auth/loginAsGuest"
$guestToken = Get-AccessToken -Response $guestResponse -Step "Guest login"
$guestBootstrap = Assert-Bootstrap -AccessToken $guestToken -Step "Guest"
Write-Output "Smoke: guest bootstrap loaded character $($guestBootstrap.character.id)."

Write-Output "Smoke: registering a generated non-production account."
$registerResponse = Invoke-SmokeJson `
    -Method Post `
    -Path "auth/register" `
    -Body @{
        CharacterName = $characterName
        Email = $email
        Password = $password
    }
[void](Get-AccessToken -Response $registerResponse -Step "Registration")

Write-Output "Smoke: logging in with the generated account."
$loginResponse = Invoke-SmokeJson `
    -Method Post `
    -Path "auth/login" `
    -Body @{
        Email = $email
        Password = $password
    }
$loginToken = Get-AccessToken -Response $loginResponse -Step "Login"
$accountBootstrap = Assert-Bootstrap -AccessToken $loginToken -Step "Registered account"

if ($accountBootstrap.character.name -ne $characterName) {
    throw "Login bootstrap returned character '$($accountBootstrap.character.name)' instead of '$characterName'."
}

Write-Output "Smoke: loading the first region and locating the Training Area."
$region = Invoke-SmokeJson `
    -Method Get `
    -Path "Region/1" `
    -Headers @{ Authorization = "Bearer $loginToken" }
$trainingArea = @($region.areas) |
    Where-Object {
        $_.id -eq "tutorial_area_training_grounds" -and
        $_.name -eq "Training Area"
    } |
    Select-Object -First 1

if ($null -eq $trainingArea) {
    throw "The first region did not expose the Training Area."
}

Write-Output "Smoke passed: guest login, registration/login, bootstrap, and Training Area are reachable."
