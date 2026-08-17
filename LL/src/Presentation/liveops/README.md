# Legends Legacy Live Operations dashboard

This is the private staff interface for account moderation, Chat moderation, support compensation, and audit review. It talks only to `API.LiveOps`; it never connects directly to Game or Chat databases.

## Run it locally

Prerequisites:

- .NET SDK matching the repository target framework
- Node.js and npm
- the normal local database/configuration required by `API.LL`

From the repository root, start the private API:

```powershell
dotnet run --project LL/src/API/API.LiveOps/API.LiveOps.csproj --urls http://localhost:7085
```

In another terminal, install and start the dashboard:

```powershell
Set-Location LL/src/Presentation/liveops
$env:npm_config_cache = Join-Path $env:TEMP 'legends-legacy-liveops-npm-cache'
npm ci
npm start
```

Open `http://localhost:4400`. Select **Sign in as operator**. In Development, the loopback-only development operator is enabled by `API.LiveOps/appsettings.Development.json`; it cannot pass the production startup checks.

Chat status and mute actions require the Chat service plus matching `Chat:Moderation:Secret` configuration. Player account and item operations remain available when Chat is offline, while the dashboard shows Chat as unavailable.

## Production build

Build the dashboard before publishing the host:

```powershell
Set-Location LL/src/Presentation/liveops
$env:npm_config_cache = Join-Path $env:TEMP 'legends-legacy-liveops-npm-cache'
npm ci
npm run build
Set-Location ../../../../
dotnet publish LL/src/API/API.LiveOps/API.LiveOps.csproj -c Release
```

The ASP.NET publish target copies `dist/liveops/browser` into its private `wwwroot`. The dashboard and API are therefore same-origin and use an HTTP-only operator session cookie plus antiforgery tokens.

Before starting outside Development, supply these settings through the deployment secret/configuration system:

- `AllowedHosts` for the private dashboard hostname
- `StaffIdentity:Authority`
- `StaffIdentity:Audience` for bearer/API access
- `StaffIdentity:ClientId` and `StaffIdentity:ClientSecret` for browser OIDC login
- `StaffIdentity:CallbackPath` (defaults to `/signin-oidc`)
- `StaffIdentity:Scopes` and identity-provider permission claims
- `Chat:Moderation:BaseUrl` and `Chat:Moderation:Secret`

The LiveOps Chat secret must match the Chat service's `InternalModeration:Secret` value.

Do not expose `API.LiveOps` through the public game ingress. Put it behind private access/VPN or an identity-aware proxy, require staff MFA at the identity provider, and configure only the intended staff identities and permissions.

## Permissions

- `liveops.read`: player lookup and history
- `liveops.accounts.moderate`: ban and unban
- `liveops.chat.moderate`: mute and unmute
- `liveops.economy.compensate`: item search and grants
- `liveops.superadmin`: all operations; keep membership very small

Every mutation requires a reason and an operation ID. Failed requests retain their operation ID so a retry remains idempotent.
