# Legends Legacy Live Operations dashboard

This is the private staff interface for account moderation, Chat moderation, support compensation, and audit review. The browser talks only to `API.LiveOps`; it never connects directly to Game or Chat databases.

## Run it locally

Prerequisites:

- .NET SDK matching the repository target framework
- Node.js and npm
- the normal local PostgreSQL databases used by `API.LL` and `API.Chat`

The Development configuration uses the repository's existing local Game database
(`legends_legacy` on `localhost:5432`) and a development-only Chat moderation secret.
Start the local services with their `http` launch profiles so LiveOps can use the
preconfigured addresses.

From the repository root, start Game so its migrations and local data are ready:

```powershell
dotnet run --project LL/src/API/API.LL/API.LL.csproj --launch-profile http
```

Start Chat in another terminal:

```powershell
dotnet run --project LL-Chat/API/API.Chat/API.Chat.csproj --launch-profile http
```

Then start the private API:

```powershell
dotnet run --project LL/src/API/API.LiveOps/API.LiveOps.csproj --launch-profile http
```

In another terminal, install and start the dashboard:

```powershell
Set-Location LL/src/Presentation/liveops
$env:npm_config_cache = Join-Path $env:TEMP 'legends-legacy-liveops-npm-cache'
npm ci
npm start
```

Open `http://localhost:4400`. Select **Sign in as operator**. In Development, the loopback-only development operator is enabled by `API.LiveOps/appsettings.Development.json`; it cannot pass the production startup checks.

Chat status and mute actions require the Chat service. The checked-in Development
settings give Chat's `InternalModeration:Secret` launch-profile variable and LiveOps'
`Chat:Moderation:Secret` the same development-only value. Player account and item
operations remain available when Chat is offline, while the dashboard shows Chat
as unavailable.

If player search reports that the connection string is not initialized, restart
`API.LiveOps` with `ASPNETCORE_ENVIRONMENT=Development` (the `http` launch profile
sets this automatically). If your local PostgreSQL credentials differ from the
repository defaults, override `ConnectionStrings__LegendsLegacyDB` with a user
secret or environment variable rather than editing committed production settings.

## Production build

Create the complete dashboard/API artifact from the repository root:

```powershell
./build/build-ll-liveops.ps1 -SkipDockerBuild
```

The artifact is written to `artifacts/api.liveops`. Without `-SkipDockerBuild`, the script also builds and pushes `ll-liveops:$IMAGE_TAG` using `DOCKER_REGISTRY`, following the repository's existing image-build convention. The ASP.NET publish target copies the Angular release into `wwwroot`, so the dashboard and API are same-origin and use an HTTP-only operator session cookie plus antiforgery tokens.

## One-owner Google login

Google authenticates the account; `API.LiveOps` performs the final owner allowlist check. For the first production login only, configure your exact Google email as `StaffIdentity:BootstrapOwnerEmail`. The email must be returned by Google as verified.

After signing in, open `/auth/session` and copy its `subject` value. Set that value as `StaffIdentity:OwnerSubject`, remove `StaffIdentity:BootstrapOwnerEmail`, and restart LiveOps. When `OwnerSubject` is present it always takes precedence, so another account cannot gain access through the bootstrap email setting.

The Google OAuth web client requires this redirect URI:

```text
https://liveops.legends-legacy.com/signin-oidc
```

Only the `openid`, `profile`, and `email` scopes are required.

Before starting outside Development, supply these settings through the deployment secret/configuration system:

- `AllowedHosts` for the private dashboard hostname
- `StaffIdentity:Authority`
- `StaffIdentity:Audience` for bearer/API access
- `StaffIdentity:ClientId` and `StaffIdentity:ClientSecret` for browser OIDC login
- `StaffIdentity:CallbackPath` (defaults to `/signin-oidc`)
- `StaffIdentity:OwnerSubject`, or `StaffIdentity:BootstrapOwnerEmail` for the first login only
- `StaffIdentity:Scopes` (`openid`, `profile`, and `email` for Google)
- `ConnectionStrings:LegendsLegacyDB`
- `ReverseProxy:Enabled=true` and either exact trusted proxy IPs in `ReverseProxy:KnownProxies` or trusted proxy CIDRs in `ReverseProxy:KnownNetworks`
- `Chat:Moderation:BaseUrl` and `Chat:Moderation:Secret`

The LiveOps Chat secret must match the Chat service's `InternalModeration:Secret` value.

A production environment-variable configuration for the first login looks like:

```text
ASPNETCORE_ENVIRONMENT=Production
AllowedHosts=liveops.legends-legacy.com
ConnectionStrings__LegendsLegacyDB=<secret connection string>
StaffIdentity__Authority=https://accounts.google.com
StaffIdentity__Audience=legends-legacy-liveops
StaffIdentity__ClientId=<secret-managed Google client ID>
StaffIdentity__ClientSecret=<secret-managed Google client secret>
StaffIdentity__CallbackPath=/signin-oidc
StaffIdentity__BootstrapOwnerEmail=<your exact Google email>
ReverseProxy__Enabled=true
ReverseProxy__ForwardLimit=1
ReverseProxy__KnownProxies__0=<private ingress proxy IP>
Chat__Moderation__BaseUrl=<internal Chat base URL ending in /chat/>
Chat__Moderation__Secret=<secret shared with Chat>
```

The release must include the existing `AddLiveOpsAdministration` Game migration and `AddChatModeration` Chat migration through the normal reviewed migration process. Do not run production migrations from the dashboard container.

Do not expose `API.LiveOps` through the public game ingress. Put it behind private access/VPN or an identity-aware proxy, require staff MFA at the identity provider, and configure only the intended staff identities and permissions.

## Permissions

- `liveops.read`: player lookup and history
- `liveops.accounts.moderate`: ban and unban
- `liveops.chat.moderate`: mute and unmute
- `liveops.economy.compensate`: item search and grants
- `liveops.superadmin`: all operations; keep membership very small

Every mutation requires a reason and an operation ID. Failed requests retain their operation ID so a retry remains idempotent.
