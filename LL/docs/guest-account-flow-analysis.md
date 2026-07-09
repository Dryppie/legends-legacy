# Guest Account Flow Analysis

## Scope

This document analyzes the Legends Legacy guest account flow and the flow for turning a guest account into a bound account.

Target service:

- `LL`
- `LL/src/API/API.LL`
- `LL/src/Core`
- `LL/src/Infrastructure`
- `LL/src/Presentation/ll`

This document started as a flow analysis. The highest-priority recommendations have now been implemented in the codebase; the implementation notes and verification results are recorded near the end.

## Original Flow

### Guest Login

Guest login starts in `AuthController.LoginAsGuest` at:

- `LL/src/API/API.LL/Controllers/V1/AuthController.cs`

The endpoint sends `GuestLoginCommand`.

The command handler:

1. Calls `IUserService.RegisterGuestAsync`.
2. Creates an `AppUser` with `IsGuest = true`.
3. Generates a random guest username.
4. Publishes `UserCreatedEvent`.
5. The `UserCreatedEventHandler` creates the initial character.
6. The handler fetches the character for the new user.
7. `JwtGenerator.IssueTokens` returns an access token and refresh token.
8. The refresh token is stored as an HTTP-only cookie.
9. The access token is returned in the response body and kept client-side by Angular.

Important files:

- `LL/src/Core/Application/UseCases/Users/Commands/GuestLogin/GuestLoginCommand.cs`
- `LL/src/Infrastructure/Service/Services.LL/Users/UserService.cs`
- `LL/src/Core/Domain/Models/Users/AppUser.cs`
- `LL/src/Core/Application/UseCases/Characters/EventHandlers/UserCreatedEventHandler.cs`
- `LL/src/Infrastructure/Service/Services.LL/Authorization/JwtGenerator.cs`
- `LL/src/Presentation/ll/src/app/core/services/api/auth/auth.service.ts`
- `LL/src/Presentation/ll/src/app/features/public/landing/login/login.component.ts`

### Guest To Email Account

Guest-to-email conversion starts from Settings:

- `LL/src/Presentation/ll/src/app/features/game/settings/settings.component.html`
- `LL/src/Presentation/ll/src/app/features/game/settings/settings.component.ts`

The UI opens the existing signup component with `convertAccount = true`. The form is prefilled with the current character name.

On submit, Angular calls:

- `AuthService.convertGuestToUser`
- `POST auth/convertGuestToUser`

The backend:

1. Reads the current user id from the authenticated JWT.
2. Calls `ConvertGuestToUserCommand`.
3. Checks only username length in the command handler.
4. Calls `UserService.ConvertGuestToUser`.
5. Finds the existing guest user row.
6. Rejects the operation if another user already has the same email.
7. Mutates the existing user with username, email, password hash, and `IsGuest = false`.
8. Fetches the existing character.
9. Publishes `ConvertedGuestToUserEvent`.
10. The event handler renames the character to the new username.
11. Issues fresh tokens.
12. Updates the refresh token cookie.

This preserves progress because the same `AppUser.Id` and character are retained.

Important files:

- `LL/src/API/API.LL/Controllers/V1/AuthController.cs`
- `LL/src/Core/Application/UseCases/Users/Commands/ConvertGuestToUser/ConvertGuestToUserCommand.cs`
- `LL/src/Infrastructure/Service/Services.LL/Users/UserService.cs`
- `LL/src/Core/Application/UseCases/Characters/EventHandlers/ConvertedGuestToUserEventHandler.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Entities/Characters/CharacterRepository.cs`
- `LL/src/Presentation/ll/src/app/features/public/landing/signup/signup.component.ts`

### Google Login And Binding

Public Google login calls:

- `POST auth/google`
- `GoogleLoginCommand`
- `GoogleAuthService.LoginOrCreateAsync`

That flow validates the Google token, finds an existing external login, finds or creates a user by Google email, adds an `ExternalLogin`, creates a character for new accounts, and issues tokens.

Settings Google binding calls:

- `POST auth/bind-google`
- `BindGoogleCommand`
- `GoogleAuthService.BindAsync`

That flow validates the Google token and adds an `ExternalLogin` row for the current user. It does not convert `IsGuest`, does not issue fresh tokens, and does not refresh client user info.

After implementation, Google binding now converts a guest into a bound account, returns fresh tokens, refreshes the auth cookie, and updates settings user info on the client.

Important files:

- `LL/src/Core/Application/UseCases/Users/Commands/GoogleLogin/GoogleLoginCommand.cs`
- `LL/src/Core/Application/UseCases/Users/Commands/BindGoogle/BindGoogleCommand.cs`
- `LL/src/Infrastructure/Service/Services.LL/Authorization/GoogleAuthService.cs`
- `LL/src/Presentation/ll/src/app/core/services/api/auth/google-auth.service.ts`
- `LL/src/Presentation/ll/src/app/core/services/api/auth/auth.service.ts`

## Strengths

- Guest accounts are real accounts, not client-only temporary identities. This makes progress preservation straightforward.
- Email conversion mutates the same user row instead of migrating progress between accounts.
- Refresh tokens are HTTP-only cookies, reducing direct JavaScript exposure.
- Refresh token rotation exists, including reuse detection.
- The game bootstrap flow hydrates authenticated game state after login.
- The same settings page exposes both email and Google binding, which is a good place for account recovery actions.

## Pain Points

### Google Binding Does Not Convert Guest Status

The biggest conceptual issue is that binding Google to a guest account does not make the account non-guest.

After Google binding, the user can be:

- `IsGuest = true`
- `IsGmailBound = true`
- JWT `guest` claim still set to true until a later token refresh

This creates confusing product semantics. The UI can show the account as both Guest and Gmail Bound.

Relevant files:

- `LL/src/Infrastructure/Service/Services.LL/Authorization/GoogleAuthService.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Users/UserRepository.cs`
- `LL/src/Infrastructure/Service/Services.LL/Authorization/JwtGenerator.cs`

Recommendation:

Decide whether a Google-bound guest is considered a bound account. If yes, `bind-google` should convert the account out of guest state, return fresh tokens, and refresh settings state. If no, rename the UI labels so “Registered” means email/password specifically and “Gmail Bound” means recovery/sign-in method only.

### Weak Database Uniqueness Guarantees

Email, username, and character name uniqueness are checked mostly in application code. The EF configuration has a unique index for external login provider/provider id, but no equivalent hard uniqueness for users email, users username, or character name.

Relevant files:

- `LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Users/AppUserConfiguration.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Users/ExternalLoginConfiguration.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Users/UserRepository.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Entities/Characters/CharacterRepository.cs`

Risk:

Concurrent requests can bypass application-level checks. Different casing can also produce inconsistent behavior because not all comparisons are normalized the same way.

Recommendation:

Add normalized email, normalized username, and normalized character name fields or consistent expression indexes, then enforce uniqueness at the database level.

### Backend Validation Is Thin

The backend currently checks username length in register and convert flows, but it does not consistently validate:

- Required username
- Required email
- Email format
- Password strength
- Whitespace-only values
- Allowed username characters
- Case-normalized uniqueness

Relevant files:

- `LL/src/Core/Application/UseCases/Users/Commands/Register/RegisterCommand.cs`
- `LL/src/Core/Application/UseCases/Users/Commands/ConvertGuestToUser/ConvertGuestToUserCommand.cs`
- `LL/src/Infrastructure/Service/Services.LL/Users/UserService.cs`

Recommendation:

Add server-side validation for auth commands. The frontend validators are useful, but the API should enforce the same rules independently.

### Conversion Silently Renames The Character

The email conversion form uses a `username` field, prefilled with the character name. If the player changes it, conversion renames the active character through `ConvertedGuestToUserEvent`.

Relevant files:

- `LL/src/Presentation/ll/src/app/features/game/settings/settings.component.html`
- `LL/src/Presentation/ll/src/app/features/public/landing/signup/signup.component.ts`
- `LL/src/Core/Application/UseCases/Characters/EventHandlers/ConvertedGuestToUserEventHandler.cs`

Risk:

The player may think they are choosing an account username, but they are also renaming their character.

Recommendation:

Either split account username from character name, or make the UI explicit that the field controls the character name.

### Error Handling Is Inconsistent

The API has a response result filter that unwraps `Response<T>` into raw data for success and raw strings for failures. The Angular auth service still tries to handle both wrapped and unwrapped response shapes, and error rendering alternates between `e.message` and `e.errorMessage`.

Relevant files:

- `LL/src/API/API.LL/Filters/ResponseResultFilter.cs`
- `LL/src/Presentation/ll/src/app/core/services/api/api.service.ts`
- `LL/src/Presentation/ll/src/app/core/services/api/auth/auth.service.ts`

Risk:

User-facing auth errors can be generic or misleading. For example, guest conversion collapses several possible causes into “Username or email might already be in use.”

Recommendation:

Standardize API error shape and client unwrapping. Prefer field-specific errors or stable error codes for auth flows.

### User Info Is Not Refreshed After Binding

After Google binding, the UI shows a success toast but does not refresh `getUserInfo`. After email conversion, `afterSuccessfulAuth` navigates to `/game`, but the settings modal itself does not explicitly close or refresh local state.

Relevant files:

- `LL/src/Presentation/ll/src/app/core/services/api/auth/auth.service.ts`
- `LL/src/Presentation/ll/src/app/features/game/settings/settings.component.ts`

Recommendation:

Return enough data from bind and convert operations for the client to update immediately, or have settings refresh `getUserInfo` after successful binding.

### Guest Account Creation Has No Visible Abuse Or Cleanup Controls

Guest login creates durable backend state:

- User row
- Character row
- Refresh token row
- Initial character-related data

There is no visible rate limit, cleanup policy, guest expiry, or abuse protection in the inspected flow.

Recommendation:

Add a product decision for guest lifecycle:

- Keep forever
- Expire unbound guests after inactivity
- Soft-delete abandoned guest accounts
- Rate-limit guest creation by IP/device
- Add bot protection if public abuse becomes an issue

### Auth State Hydration Has Timing Edges

The Angular app initializer refreshes tokens on load, but does not directly hydrate the current character. Game bootstrap later does that when the authenticated dashboard loads.

Relevant files:

- `LL/src/Presentation/ll/src/app/app.config.ts`
- `LL/src/Presentation/ll/src/app/core/services/api/auth/auth.service.ts`
- `LL/src/Presentation/ll/src/app/core/services/api/game-bootstrap/game-bootstrap-state.service.ts`

Risk:

Screens that depend on `currentCharacter` can be sensitive to load order unless bootstrap has completed.

Recommendation:

Keep bootstrap as the source of game state, but make protected layouts explicitly show a loading state until bootstrap has hydrated the character.

## Suggested Priority

1. Define account semantics: decide whether Google binding converts a guest to a bound account.
2. Add DB uniqueness guarantees for email, username, and character name.
3. Add server-side validators for register and convert flows.
4. Improve conversion UI copy around character naming.
5. Standardize auth error response handling.
6. Refresh settings state after successful binding.
7. Decide guest lifecycle and cleanup policy.

## Cleaner Long-Term Identity Model

The current implementation preserves the existing shape of the application, where `AppUser.Username` and `Character.Name` are both meaningful and guest conversion uses the submitted `username` as the visible character name. That is workable, but it leaves some conceptual overlap between account identity and character identity.

A cleaner long-term model would separate login identity from public game identity:

- `AppUser` owns authentication and account recovery identity.
- `AppUser.Email` and external login providers are the primary ways to sign in.
- `AppUser.Username` is removed, made internal, or treated only as a legacy/non-public account field.
- `Character.Name` owns the public player-facing name.
- `Character.NormalizedName` owns case-insensitive uniqueness for public character names.
- Signup, guest conversion, rename, guild invite, profile lookup, chat display, and character search all speak in terms of character name.

In that model, `Character.NormalizedName` is not an extra copy of user identity. It is the normalized form of the visible game identity. `User.NormalizedUsername` is unnecessary unless the product still wants a separate public account handle.

The practical migration path would be:

1. Stop presenting account conversion fields as `username` in the API and UI.
2. Introduce request DTOs that use `CharacterName` where the value renames or creates the character.
3. Keep accepting the old `Username` field temporarily for compatibility if needed.
4. Move public-name uniqueness and lookup logic fully to `Character`.
5. Remove or de-emphasize `AppUser.Username` after all auth and game flows no longer depend on it.

This keeps authentication boring and private, while making character names the single source of truth for the identity other players see.

## Implemented Changes

### Account Semantics

Google binding now treats a Google-bound guest as a proper bound account.

Implemented behavior:

- `bind-google` converts the current guest out of `IsGuest`.
- The Google email is copied onto the account and marked confirmed.
- Fresh tokens are issued so the `guest` JWT claim updates immediately.
- The frontend applies the returned tokens and refreshes settings user info.

### Cleaner Identity Boundary

The public naming contract has been moved toward character identity:

- Register and guest-conversion commands now accept `CharacterName`.
- The API still accepts legacy `Username` payloads for compatibility.
- `UserCreatedEvent` and `ConvertedGuestToUserEvent` now carry `CharacterName`.
- Account `Username` remains only as a legacy/internal account label.

### Identity Normalization And Uniqueness

Added normalized identity fields for durable case-insensitive lookup and database uniqueness:

- `AppUser.NormalizedEmail`
- `Character.NormalizedName`

The EF migration backfills these values from existing data before creating unique indexes. Character names, not account usernames, own public-name uniqueness.

### Backend Validation

Registration, guest conversion, and character rename now share server-side validation for:

- Required names and email
- Whitespace trimming
- Email format
- Minimum password length
- Name length

### Character Name Conflict Checks

Guest conversion now checks whether the requested visible character name is already taken before converting the account. Character repository lookups now use normalized names.

### Settings UX

The settings copy now makes the conversion semantics clearer:

- Google binding is presented as securing the account.
- Email/password conversion labels the name field as the character name.
- The shared signup component accepts contextual label/help text.

### Remaining Recommendations

Not implemented in this pass:

- Guest expiry or cleanup policy.
- Rate limiting or abuse controls for guest creation.
- Full auth error-shape standardization with stable field-level error codes.
- Explicit protected-layout loading states for game bootstrap hydration.

## Verification

The following commands were run after implementation:

```powershell
dotnet build LL\LegendsLegacy.sln
dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj
git diff --check
```

Result:

- Build succeeded.
- Tests passed: 307 total.
- `git diff --check` passed with only line-ending warnings.
- 0 errors.

Frontend build was not run because this workspace has no `node_modules`, and the system `npm` entrypoint is broken in this environment (`npm-cli.js` could not be found under the user profile). The bundled runtime includes Node and pnpm, but the project uses `package-lock.json`, so no package-manager switch was made during verification.

## Migration, Configuration, And Deployment Notes

An EF Core migration was added:

- `20260708185907_AddAccountBindingIdentityConstraints`
- `20260708203000_DropLegacyUserNormalizedUsername`

Deployment implications:

- The migration adds normalized identity columns and unique indexes.
- The migration trims and backfills existing username, email, and character name data before creating indexes.
- The follow-up migration removes the legacy `IX_Users_NormalizedUsername` index and `NormalizedUsername` column from databases where an earlier version of the account-binding migration was already applied.
- Deployment can fail if existing production data contains duplicate normalized non-null emails or duplicate normalized character names.
- No external configuration changes are required.
