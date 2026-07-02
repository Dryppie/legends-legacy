# Authentication and Authorization Review

Date: 2026-06-29

Last updated: 2026-06-30

Scope: `LL` ASP.NET Core API, Angular frontend, admin dashboard boundary, and `LL-Chat`.

Status: implementation in progress. First hardening pass completed on 2026-06-29; second hardening pass completed on 2026-06-30.

## 1. Executive Summary

Rating: **Risky and should be improved before production**.

The main game authentication flow has a workable foundation:

- JWT access tokens are issued server-side.
- Refresh tokens exist and are stored hashed in the database.
- Refresh tokens are rotated on use.
- Angular keeps the access token in memory and restores sessions on startup through refresh.
- Most main game controllers derive the current character from JWT claims instead of trusting client-supplied character IDs.

However, there are production-blocking authorization gaps:

- The admin dashboard API boundary is anonymous.
- Marketplace listing cancellation does not verify seller ownership.
- Dungeon action execution loads runs by `runId` without checking that the run belongs to the authenticated character.
- Chat guild join, send, and history flows trust client-supplied guild IDs.

The system should be hardened before production use, especially because this is a multiplayer game where account, inventory, and guild integrity matter.

### Implementation Progress

Completed in the first pass:

- Protected `API.AdminDashboard` with JWT bearer authentication and an `AdminDashboard` email allow-list policy.
- Removed backend access-token cookie authentication; access tokens are now accepted through Authorization headers, with SignalR query-token support retained for hubs.
- Restricted the refresh token cookie to `/api/v1/auth`.
- Added logout refresh-token revocation.
- Enabled JWT issuer and audience validation in main API, chat API, and admin dashboard.
- Added marketplace seller ownership validation before cancellation.
- Added dungeon run ownership validation before executing run actions.
- Added guild invite guild-ID matching.
- Added chat guild checks for join, send, and history based on a `GuildId` JWT claim.
- Included current guild membership in issued JWT access tokens.
- Removed the Angular chat `localStorage.DevAuth` fallback and switched game/chat SignalR connections to bearer tokens from `AuthService`.
- Fixed Google token validation to use the configured validation settings.
- Development-gated the global main API `.AllowAnonymous()` controller mapping.

Completed in the second pass:

- Updated registration to return tokens and set auth cookies, matching the Angular client contract.
- Removed committed DB passwords, JWT signing keys, Quartz connection strings, and system chat secrets from checked-in appsettings.
- Preserved `HttpErrorResponse` details in the Angular API service while keeping a normalized `errorMessage` for existing toast flows.
- Added focused guild invite authorization tests for cross-guild invite attempts.

Completed in the third pass:

- Added a custom `X-LL-Refresh-Request` header requirement for refresh/logout cookie endpoints.
- Added Angular API support for sending the refresh-cookie CSRF header on refresh/logout.
- Added refresh-token reuse detection for rotated tokens and revocation of active refresh tokens for the affected user.
- Added a focused refresh-token reuse test.

Still open:

- Add refresh session/device metadata and token-family IDs.
- Add broader direct tests for marketplace, dungeon, chat, and refresh/logout authz guards.

## 2. Current Flow Description

### Login Flow

- `POST /api/v1/auth/login`
- Implemented in `LL/src/API/API.LL/Controllers/V1/AuthController.cs`.
- Sends `LoginCommand`.
- On success:
  - returns `Tokens` in the response body;
  - sets the `RefreshToken` HttpOnly cookie.

### Access Token Flow

- Tokens are created by `Services.LL.Authorization.JwtGenerator`.
- Access token lifetime defaults to 30 minutes via `JwtOptions.AccessMinutes`.
- JWT claims include:
  - `sub`
  - `ClaimTypes.NameIdentifier`
  - `email`
  - `guest`
  - `ClaimTypes.UserData`
  - `CharacterId`
  - `GuildId` when the character is currently in a guild
  - `ClaimTypes.Name`
  - `CharacterTitleDisplayName`
- API request validation in `API.LL/Program.cs` validates signing key, lifetime, issuer, and audience.
- API accepts tokens from:
  - Authorization header;
  - `DevAuth` header in debug builds.

### Refresh Flow

- `POST /api/v1/auth/createNewTokens`
- Implemented in `AuthController.CreateNewTokens`.
- Marked `[AllowAnonymous]`.
- Requires `X-LL-Refresh-Request: 1`.
- Reads the refresh token from the `RefreshToken` cookie.
- `JwtGenerator.RefreshAsync`:
  - hashes the presented refresh token;
  - looks up the DB record;
  - checks `IsActive`;
  - sets `RevokedUtc`;
  - issues a new access/refresh pair;
  - stores the hash of the new refresh token in `ReplacedBy`.

### Logout Flow

- `POST /api/v1/auth/logout`
- Requires `X-LL-Refresh-Request: 1`.
- Revokes the matching refresh token in the database when present.
- Deletes the current `RefreshToken` cookie and legacy auth cookies.

### Frontend Token Handling

- `APP_INITIALIZER` calls `AuthService.checkAuth()` on startup.
- `checkAuth()` attempts refresh and then fetches the current character.
- Access token is stored in memory as `_accessToken`.
- `AuthInterceptor` attaches `Authorization: Bearer <token>` to non-auth API requests.
- API service uses `withCredentials: true` for requests, so cookies are sent as well.

### Backend Authorization Flow

- Main API controllers inherit `[Authorize]` from `BaseController`.
- Many controllers pass `CurrentCharacterGuid` from JWT claims into commands.
- Some endpoints are explicitly anonymous:
  - registration;
  - login;
  - guest login;
  - Google login;
  - token refresh;
  - health checks;
  - time sync.
- Main API SignalR hub is mapped with `.RequireAuthorization()` and `GameHub` is also `[Authorize]`.
- Chat service has its own JWT setup.
- Admin dashboard base controller requires the `AdminDashboard` authorization policy.

## 3. Authentication Findings

### A1. Cookie-Based Access Token Auth Creates CSRF Exposure

Severity: **High**

Files:

- `LL/src/API/API.LL/Program.cs`
- `LL/src/API/API.LL/Controllers/V1/AuthController.cs`
- `LL/src/Presentation/ll/src/app/core/services/api/api.service.ts`

Original behavior:

- The API accepts access tokens from an HttpOnly `AccessToken` cookie.
- Cookies are configured with `SameSite=None`, `Secure=true`, `HttpOnly=true`.
- Angular sends credentials on every API call.

Why it matters:

- If a browser automatically sends cookies, mutating endpoints can become CSRF-sensitive.
- CORS is not a full CSRF defense.
- The frontend mostly uses Authorization headers, but the backend also trusts cookies.

Recommended improvement:

- Prefer Authorization headers for access tokens.
- Restrict refresh cookies to refresh/logout paths.
- Add CSRF protection for any cookie-authenticated mutating endpoint.
- Consider removing access-token cookie support entirely.

### A2. Logout Does Not Revoke Refresh Tokens

Severity: **High**

Files:

- `LL/src/API/API.LL/Controllers/V1/AuthController.cs`
- `LL/src/Core/Domain/Models/Users/RefreshToken.cs`

Original behavior:

- Logout deletes browser cookies only.
- The refresh token record remains active until expiration.

Why it matters:

- If a refresh token was stolen before logout, it remains usable.
- Logout does not reliably end the server-side session.

Recommended improvement:

- On logout, hash the presented refresh token and revoke the DB record.
- Add "logout all sessions" separately if needed.

### A3. Request JWT Validation Disables Issuer and Audience

Severity: **Medium**

Files:

- `LL/src/API/API.LL/Program.cs`
- `LL-Chat/API/API.Chat/Program.cs`

Original behavior:

- `ValidateIssuer = false`
- `ValidateAudience = false`
- Comments say these need to be true.

Why it matters:

- Tokens signed with the same key but intended for another service/environment could be accepted.
- This weakens environment and service isolation.

Recommended improvement:

- Configure real issuer and audience values.
- Enable issuer and audience validation in both main API and chat API.
- Keep validation settings consistent across token issuer and consumers.

### A4. Refresh Token Rotation Exists but Reuse Detection Is Missing

Severity: **Medium**

Files:

- `LL/src/Infrastructure/Service/Services.LL/Authorization/JwtGenerator.cs`
- `LL/src/Core/Domain/Models/Users/RefreshToken.cs`

Original behavior:

- Refresh tokens are rotated.
- Old tokens are revoked.
- `ReplacedBy` stores the hash of the new token.
- Reuse of an old token simply fails.

Why it matters:

- Reuse of a rotated refresh token is often evidence of theft.
- The system does not detect or respond to that compromise.

Recommended improvement:

- Add refresh session metadata.
- Detect reuse of revoked tokens.
- Revoke the token family/session on reuse.
- Log a security event.

### A5. Refresh Tokens Are Not Bound to Session or Device Metadata

Severity: **Medium**

Files:

- `LL/src/Core/Domain/Models/Users/RefreshToken.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Users/RefreshTokenConfiguration.cs`

Original behavior:

- Refresh token rows store user ID, token hash, expiration, created time, revoked time, and replacement hash.
- No user agent, IP, session ID, device label, or token family exists.

Why it matters:

- Users cannot see or revoke individual sessions.
- Suspicious session behavior is hard to investigate.

Recommended improvement:

- Add session/device metadata.
- Add cleanup for expired tokens.
- Add per-session revocation.

### A6. Google ID Token Audience Setting Is Built but Not Used

Severity: **Medium**

File:

- `LL/src/Infrastructure/Service/Services.LL/Authorization/GoogleTokenValidator.cs`

Original behavior:

- `ValidationSettings` is created with `Audience = new[] { _clientId }`.
- The call uses `GoogleJsonWebSignature.ValidateAsync(idToken, _clock, false)` instead of passing the settings.

Why it matters:

- The intended client ID validation may not be applied.

Recommended improvement:

- Pass the configured `ValidationSettings` into Google token validation.
- Add tests proving tokens for another client ID are rejected.

### A7. Register Flow Does Not Match Frontend Expectations

Severity: **Low**

Files:

- `LL/src/Core/Application/UseCases/Users/Commands/Register/RegisterCommand.cs`
- `LL/src/API/API.LL/Controllers/V1/AuthController.cs`
- `LL/src/Presentation/ll/src/app/core/services/api/auth/auth.service.ts`

Original behavior:

- Backend registration returns `Response<Unit>`.
- Frontend `register()` expects `accessToken` and `accessExpiresAt`.

Why it matters:

- Registration success may fail client-side or leave auth state incorrect.

Recommended improvement:

- Either make registration return tokens and set cookies, or update the frontend to redirect to login after registration.

## 4. Authorization Findings

### Z1. Admin Dashboard API Is Anonymous

Severity: **Critical**

Files:

- `LL/src/API/API.AdminDashboard/Controllers/BaseController.cs`
- `LL/src/API/API.AdminDashboard/Program.cs`

Endpoint/service involved:

- Admin dashboard controllers for items, creatures, diagnostics, and essence catalog.

Original behavior:

- Admin dashboard `BaseController` is `[AllowAnonymous]`.
- Admin dashboard app calls `UseAuthorization()` but does not configure authentication.

Possible exploit or failure case:

- Anyone who can reach the dashboard API can update item/creature/admin data or run diagnostics endpoints.

Recommended improvement:

- Add authentication to `API.AdminDashboard`.
- Require an explicit admin policy or role.
- Remove `[AllowAnonymous]` from the dashboard base controller.

### Z2. Marketplace Listing Cancellation Does Not Verify Seller Ownership

Severity: **High**

Files:

- `LL/src/Infrastructure/Service/Services.LL/MarketPlaces/MarketPlaceService.cs`
- `LL/src/Core/Application/UseCases/MarketPlaces/Commands/CancelMarketPlaceListing/CancelMarketPlaceListingCommand.cs`

Endpoint/service involved:

- `POST /api/v1/MarketPlace/CancelMarketPlaceListing/{listingId}` or equivalent marketplace cancel flow.

Original behavior:

- `CancelMarketPlaceListingAsync(characterId, listingId)` loads the listing by ID.
- It creates an inventory item for the caller.
- It removes the listing.
- It does not check `listing.SellerId == characterId`.

Possible exploit or failure case:

- A player can cancel another player's marketplace listing and receive the listed item.

Recommended improvement:

- Check `listing.SellerId == characterId` before returning inventory or removing the listing.
- Return 403 or a generic failure on mismatch.
- Add integration tests.

### Z3. Dungeon Action Execution Does Not Check Run Ownership

Severity: **High**

Files:

- `LL/src/API/API.LL/Controllers/V1/DungeonController.cs`
- `LL/src/Core/Application/UseCases/Dungeons/Commands/ExecuteDungeonAction/ExecuteDungeonActionCommand.cs`
- `LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonRunService.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Dungeons/DungeonRunRepository.cs`

Endpoint/service involved:

- `POST /api/v1/Dungeon/ExecuteAction/{runId}`

Original behavior:

- Controller passes `CurrentCharacterGuid`.
- Command receives `CharacterId`.
- Command calls `_dungeonRunService.ExecuteActionAsync(request.RunId, ...)`.
- Service loads run only by `runId`.
- No comparison is made between `run.CharacterId` and the authenticated character ID.

Possible exploit or failure case:

- A player who learns another player's dungeon run ID can execute actions in that run.

Recommended improvement:

- Change service signature to include `characterId`.
- Load by `(runId, characterId)` or compare after loading.
- Add ownership tests for execute action.

### Z4. Chat Guild Join, Send, and History Trust Client-Supplied Guild IDs

Severity: **High**

Files:

- `LL-Chat/API/API.Chat/Hubs/ChatHub.cs`
- `LL-Chat/API/API.Chat/Controllers/V1/ChatController.cs`
- `LL-Chat/Infrastructure/Persistence/Persistence.Chat/Repositories/ChatMessageRepository.cs`

Endpoint/service involved:

- Chat hub `JoinGuild`
- Chat hub `Send` with `ChatChannelType.Guild`
- `GET /chat/api/v1/Chat/GetChatHistory`

Original behavior:

- `JoinGuild(string guildId)` adds the connection to `guild:<guildId>`.
- Guild sends publish to `GuildPrefix + contextKey`.
- Chat history returns guild messages where `ContextKey == guildChannel`.
- Chat service does not verify that the current character is a member of the requested guild.

Possible exploit or failure case:

- A user can join, read, or post in another guild's chat by guessing or obtaining the guild ID.

Recommended improvement:

- The chat service must validate guild membership before joining, sending, or reading guild history.
- Either replicate membership data into chat or call a trusted main-game service.
- Add tests for non-member access.

### Z5. Guild Invite Commands Can Target Arbitrary Guild IDs

Severity: **Medium**

Files:

- `LL/src/Core/Application/UseCases/Guilds/Dtos/Requests/InviteToGuildDto.cs`
- `LL/src/Core/Application/UseCases/Guilds/Commands/Invite/InviteCommand.cs`
- `LL/src/Core/Application/UseCases/Guilds/Commands/InviteCharacterByName/InviteCharacterByNameCommand.cs`
- `LL/src/Infrastructure/Service/Services.LL/Guilds/GuildService.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Guilds/GuildRepository.cs`

Endpoint/service involved:

- Guild invite flows.

Original behavior:

- Caller invite permission is checked using the caller's guild membership.
- The target guild ID comes from the request.
- The repository loads and mutates the requested guild ID.
- The requested guild ID is not compared to `requestingMember.GuildId`.

Possible exploit or failure case:

- An officer in one guild may be able to create invites for another guild if they pass another guild ID.

Recommended improvement:

- Do not accept guild ID for officer actions where the caller's own guild is implied.
- Or require `guildId == requestingMember.GuildId`.

### Z6. Feature Flag Can Globally Bypass Controller Authorization

Severity: **High**

File:

- `LL/src/API/API.LL/Program.cs`

Endpoint/service involved:

- All mapped controllers when `FeatureManagement:AllowAnonymous` is true.

Original behavior:

- If `FeatureManagement:AllowAnonymous` is true, controllers are mapped with `.AllowAnonymous()`.

Possible exploit or failure case:

- A misconfigured production deployment could disable all controller auth.

Recommended improvement:

- Remove the global bypass.
- If needed locally, guard it with `app.Environment.IsDevelopment()`.

## 5. Frontend Findings

### F1. Auth Guard Trusts In-Memory Auth State

Severity: **Medium**

Files:

- `LL/src/Presentation/ll/src/app/core/guards/auth/auth.guard.ts`
- `LL/src/Presentation/ll/src/app/app.config.ts`

Original behavior:

- Startup initializer calls `checkAuth()`.
- Route guard checks only `authService.isAuthenticated()`.

Risk or weakness:

- Guards can be fragile during reload or startup races.

Recommended improvement:

- Make guards rely on `checkAuth()` or `ensureValidToken()` when auth state is unknown.

### F2. Chat Uses Local Storage Dev Token Fallback

Severity: **Medium**

File:

- `LL/src/Presentation/ll/src/app/core/services/ll-chat/chat-service/chat.service.ts`

Current behavior:

- SignalR `accessTokenFactory` returns `this.auth.getAccessToken() || localStorage.getItem('DevAuth') || ''`.

Risk or weakness:

- A localStorage dev token fallback increases accidental production exposure.

Recommended improvement:

- Remove `localStorage.DevAuth` fallback from production builds.
- Gate debug-only auth behavior behind explicit development checks.

### F3. Error Normalization Loses HTTP Detail

Severity: **Low**

File:

- `LL/src/Presentation/ll/src/app/core/services/api/api.service.ts`

Current behavior:

- `formatErrors` wraps `error.error` in a plain `Error`.

Risk or weakness:

- The app loses status codes, response headers, and structured error details.
- Auth errors become harder to handle consistently.

Recommended improvement:

- Preserve `HttpErrorResponse`.
- Add typed error handling for 401, 403, and refresh failures.

## 6. Configuration and Deployment Findings

### C1. Secrets and Local Credentials Are Committed in Appsettings

Severity: **High**

Files:

- `LL/src/API/API.LL/appsettings.json`
- `LL-Chat/API/API.Chat/appsettings.json`

Current behavior:

- JWT signing key is in appsettings.
- DB connection strings include usernames and passwords.
- System chat secret is in appsettings.

Risk:

- Secrets can leak through source control or deployment artifacts.

Recommended improvement:

- Move secrets to environment variables or secret manager.
- Keep appsettings values as empty placeholders or development-only examples.

### C2. Same JWT Signing Key Is Shared Across Main API and Chat

Severity: **Medium**

Files:

- `LL/src/API/API.LL/appsettings.json`
- `LL-Chat/API/API.Chat/appsettings.json`

Current behavior:

- Main API and chat service use the same signing key and both disable issuer/audience validation.

Risk:

- Service boundaries are weak.

Recommended improvement:

- Enable issuer/audience validation.
- Consider distinct audiences per service.

### C3. EF Migrations Run at Application Startup

Severity: **Medium**

Files:

- `LL/src/API/API.LL/Program.cs`
- `LL-Chat/API/API.Chat/Program.cs`

Current behavior:

- APIs call `Database.MigrateAsync()` at startup.

Risk:

- Production deployment can apply schema changes implicitly.
- Multiple replicas can race.

Recommended improvement:

- Use controlled migration jobs or deployment steps.

### C4. CORS Allows Credentials

Severity: **Medium**

Files:

- `LL/src/API/API.LL/Program.cs`
- `LL-Chat/API/API.Chat/Program.cs`

Current behavior:

- CORS allows credentials for local and dev origins.

Risk:

- Credentialed CORS plus cookie auth requires careful CSRF and origin management.

Recommended improvement:

- Keep allowed origins explicit per environment.
- Do not combine broad credentialed CORS with cookie-authenticated mutations.

## 7. Missing Tests

### Backend Unit Tests

- [ ] JWT claim generation includes required claims and excludes sensitive data.
- [ ] JWT issuer/audience validation rejects mismatched tokens.
- [ ] Google token validation rejects wrong audience.
- [ ] Refresh token rotation revokes old token.
- [x] Refresh token reuse detection revokes active user refresh tokens.
- [ ] Logout revokes current refresh token.
- [ ] Marketplace cancel fails when caller is not seller.
- [ ] Dungeon execute action fails when run belongs to another character.
- [x] Guild invite cannot target another guild.
- [ ] Chat guild membership checks reject non-members.

### Backend Integration Tests

- [ ] Protected endpoints return 401 without token.
- [ ] Ownership failures return 403 or a consistent failure.
- [ ] Admin dashboard endpoints require admin auth.
- [ ] Cookie-authenticated mutation is CSRF-protected or rejected.
- [ ] Refresh endpoint handles expired, revoked, reused, and malformed tokens.
- [ ] Logout prevents future refresh with the same token.
- [ ] Chat history cannot read another guild's messages.
- [ ] Chat hub cannot join another guild's group.

### Frontend Unit Tests

- [ ] `AuthService.checkAuth()` restores session after reload.
- [ ] `AuthService.checkAuth()` marks unauthenticated after failed refresh.
- [ ] `AuthInterceptor` attaches Authorization header.
- [ ] `AuthInterceptor` refreshes only once for concurrent failures.
- [ ] Auth guard handles unknown startup state.
- [ ] Register flow matches backend contract.
- [ ] Failed refresh logs out cleanly.

### Frontend Integration/E2E Tests

- [ ] Reload while logged in restores session.
- [ ] Expired access token refreshes automatically.
- [ ] Invalid refresh token sends user to login.
- [ ] User cannot cancel another player's market listing.
- [ ] User cannot execute another player's dungeon action.
- [ ] User cannot join another guild's chat.

## 8. Recommended Improved Design

### Access Tokens

- Lifetime: 10 to 15 minutes.
- Storage: memory only in the browser.
- Transport: Authorization header only.
- Claims:
  - `sub`
  - `character_id`
  - `jti`
  - `session_id`
  - optional `guest`
- Avoid mutable display claims where possible, such as character title display name.

### Refresh Tokens

- Lifetime: 14 to 30 days.
- Storage: HttpOnly, Secure cookie.
- Cookie path: restrict to refresh/logout endpoints where practical.
- SameSite: `Lax` or `Strict` where deployment allows; otherwise use CSRF tokens.
- Database storage:
  - hash only;
  - session ID;
  - token family;
  - created/revoked/replaced timestamps;
  - IP/user-agent metadata;
  - reuse detection state.

### Logout

- Logout current session revokes current refresh token.
- Logout all sessions revokes all active refresh tokens for user.
- Frontend clears memory token and auth state.

### Authorization

- Centralize ownership checks.
- Add resource authorization helpers for:
  - character ownership;
  - dungeon run ownership;
  - marketplace listing seller ownership;
  - guild member/officer/leader permissions;
  - chat guild membership.
- Prefer policies or services over scattered ad hoc checks.
- Return 403 for authenticated users who are not allowed to perform the action.

### Frontend Auth Flow

- On startup:
  - call refresh;
  - fetch current character;
  - set authenticated state only after server confirmation.
- On each request:
  - ensure token is valid;
  - refresh once for concurrent requests;
  - retry after refresh;
  - logout on refresh failure.
- Do not use localStorage for production auth tokens.

### SignalR and Chat

- Main game SignalR already verifies guild subscription membership; keep that pattern.
- Chat service must enforce the same authorization rules:
  - guild join requires membership;
  - guild send requires membership;
  - guild history requires membership;
  - whisper target should be server-resolved and validated.

## 9. Prioritized Action Plan

### Must Fix Before Production

- [x] Protect AdminDashboard with real admin authentication and authorization.
- [x] Add marketplace listing seller ownership check before cancellation.
- [x] Add dungeon run ownership check before executing actions.
- [x] Add chat guild membership checks for join, send, and history.
- [x] Enable JWT issuer and audience validation in main API and chat API.
- [x] Move signing keys, DB credentials, and system secrets out of appsettings.
- [x] Remove or development-gate global `AllowAnonymous` controller mapping.
- [x] Add CSRF-safe cookie strategy or stop accepting access tokens from cookies.

### Should Fix Soon

- [x] Revoke refresh tokens on logout.
- [x] Add refresh token reuse detection.
- [ ] Add refresh session model, device metadata, and token-family IDs.
- [x] Fix Google ID token audience validation.
- [x] Fix backend/frontend registration contract.
- [x] Preserve structured HTTP errors in Angular API service.
- [ ] Add login rate limiting and account lockout or throttling.
- [ ] Add security event logging for login, refresh, logout, reuse, and suspicious failures.

### Nice To Have

- [ ] Add user-visible session management.
- [ ] Add scheduled cleanup for expired refresh tokens.
- [ ] Replace mutable display claims with DB-backed user/character data.
- [ ] Add policy-based authorization for guild and ownership actions.
- [ ] Add standardized auth error envelopes.

## 10. Implementation Notes for Later

### Likely Code Changes

- `LL/src/API/API.LL/Controllers/V1/AuthController.cs`
- `LL/src/API/API.LL/Program.cs`
- `LL/src/Core/Domain/Models/Users/RefreshToken.cs`
- `LL/src/Infrastructure/Service/Services.LL/Authorization/JwtGenerator.cs`
- `LL/src/Infrastructure/Service/Services.LL/Authorization/GoogleTokenValidator.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Users/RefreshTokenRepository .cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Users/RefreshTokenConfiguration.cs`
- `LL/src/Infrastructure/Service/Services.LL/MarketPlaces/MarketPlaceService.cs`
- `LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonRunService.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Dungeons/DungeonRunRepository.cs`
- `LL/src/Infrastructure/Service/Services.LL/Guilds/GuildService.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Guilds/GuildRepository.cs`
- `LL-Chat/API/API.Chat/Hubs/ChatHub.cs`
- `LL-Chat/API/API.Chat/Controllers/V1/ChatController.cs`
- `LL-Chat/Infrastructure/Persistence/Persistence.Chat/Repositories/ChatMessageRepository.cs`
- `LL/src/API/API.AdminDashboard/Program.cs`
- `LL/src/API/API.AdminDashboard/Controllers/BaseController.cs`
- `LL/src/Presentation/ll/src/app/core/services/api/auth/auth.service.ts`
- `LL/src/Presentation/ll/src/app/core/guards/auth/auth.guard.ts`
- `LL/src/Presentation/ll/src/app/core/services/api/api.service.ts`
- `LL/src/Presentation/ll/src/app/core/services/ll-chat/chat-service/chat.service.ts`

### New Services or Policies

- `IRefreshSessionService`
- `ICurrentUserService` or `ICurrentCharacterAccessor`
- `ICharacterOwnershipService`
- `IDungeonRunAuthorizationService`
- `IMarketplaceAuthorizationService`
- `IGuildAuthorizationService`
- `IChatGuildMembershipService`
- Admin authorization policy

### Migration Needs

- Add refresh session metadata:
  - session ID;
  - token family ID;
  - `JwtId`;
  - created IP/user agent;
  - revoked reason;
  - replaced token relationship;
  - reuse detection fields.
- Add uniqueness constraints where missing:
  - refresh token hash;
  - external login provider/user ID if not already unique.
- Optional:
  - security audit event table.

### Test Files to Add

- Backend unit tests for auth token generation and refresh behavior.
- Backend integration tests for protected endpoints and ownership failures.
- Marketplace service tests for cancel ownership.
- Dungeon run service tests for execute ownership.
- Guild service tests for invite guild matching.
- Chat integration tests for guild membership enforcement.
- Angular tests for auth service, guard, and interceptor behavior.

## Verification Performed

- Inspected source with `rg` and `Get-Content`.
- Ran `dotnet build LL\LegendsLegacy.sln`: passed with existing warnings.
- Ran `dotnet build LL-Chat\LL-Chat.sln`: passed with warnings, including an existing `AutoMapper` high-severity advisory warning.
- Ran `dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj`: passed, 265 tests.
- Attempted `npm run build` in `LL\src\Presentation\ll`: blocked before Angular build because the system `npm` shim points to a missing global `npm-cli.js`, and this worktree has no local `node_modules`.
