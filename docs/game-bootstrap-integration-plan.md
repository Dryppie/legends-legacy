# Game Bootstrap Integration Plan

## Goal

Introduce a single game bootstrap boundary that hydrates the authenticated game shell once, then lets page-specific state and realtime updates take over.

For this game, "bootstrap" should mean:

- the player is authenticated,
- the backend has resolved the current character from claims,
- the frontend receives the minimal shared state needed by the whole `/game` shell,
- feature pages no longer need to independently ask for tutorial state, current character, current action, and other global state during first render.

This is especially useful for the tutorial system because a completed tutorial can be represented as `tutorial: null` once at startup, and unrelated pages do not need to poll tutorial endpoints just to discover there is nothing active.

## Current State

The game currently has no dedicated bootstrap endpoint.

Current startup flow is roughly:

1. Angular `APP_INITIALIZER` calls `AuthService.checkAuth()`.
2. `checkAuth()` refreshes tokens through `Auth/createNewTokens`.
3. If refresh succeeds, `AuthService` calls `CharacterController.Get`.
4. `AppComponent` watches `AuthService.isAuthenticated()`.
5. Once authenticated, `AppComponent` starts `CharacterActionsStateService.init()`.
6. Individual pages/components load their own state:
   - tutorial pages call `tutorialState.load()`,
   - character overview calls `Character/Overview`,
   - inventory calls `Inventory`,
   - essences calls essence endpoints,
   - region/world pages call region endpoints,
   - etc.

This works, but it creates scattered ownership. Some state belongs to the whole game shell, but it is still loaded by individual pages.

## What Should Be Bootstrapped

Bootstrap should only include state that is global to the authenticated game shell or needed before route-specific pages can behave correctly.

Recommended first payload:

```json
{
  "character": {
    "id": "guid",
    "name": "StormyDragonMystic_5660",
    "level": 1,
    "experience": 0,
    "experienceUntilNextLevel": 100,
    "cinders": 0,
    "soulstones": 0,
    "arenaRating": 1000,
    "equippedTitle": null
  },
  "tutorial": {
    "tutorialId": "tutorial.first_steps",
    "title": "First Steps",
    "currentStep": "defeat_training_creature",
    "objective": "Defeat the creature in the Training Area.",
    "currentAmount": 0,
    "requiredAmount": 1,
    "actionLabel": "Go to Training Area",
    "destinationRoute": "/game/world/shenic?area=tutorial_area_training_grounds",
    "guidePageId": "tutorial-training",
    "tourPageId": "tutorial-training-area",
    "isCompleted": false
  },
  "currentAction": null,
  "serverTimeUtc": "2026-07-07T12:00:00Z"
}
```

When the tutorial is complete:

```json
{
  "character": {},
  "tutorial": null,
  "currentAction": null,
  "serverTimeUtc": "2026-07-07T12:00:00Z"
}
```

Do not include heavy page data in the first version:

- full inventory,
- full soul archive,
- crafting recipe lists,
- guild overview,
- prophecy pages,
- combat area lists,
- market data,
- achievement pages.

Those should remain page-owned because they are only needed when the player visits that page.

## Backend Shape

Add a new controller:

```text
GET /api/v1/GameBootstrap
```

Suggested files:

```text
LL/src/API/API.LL/Controllers/V1/GameBootstrapController.cs
LL/src/Core/Application/UseCases/GameBootstrap/Queries/GetGameBootstrap/GetGameBootstrapQuery.cs
LL/src/Core/Application/UseCases/GameBootstrap/Dtos/GameBootstrapDto.cs
```

Suggested DTO - Ensure it uses IMapFrom and has a mapping profile method:

```csharp
public sealed record GameBootstrapDto(
    CharacterDto Character,
    TutorialStateDto? Tutorial,
    CharacterActionDto? CurrentAction,
    DateTimeOffset ServerTimeUtc);
```

The query handler should compose existing application services or queries:

- current character from the same logic used by `GetCharacterQuery`,
- active tutorial state from `ITutorialService.GetStateAsync`,
- current action from the same logic used by `GetCharacterActionQuery`,
- server time from `DateTimeOffset.UtcNow` or `TimeProvider`.

The handler should not duplicate query logic. Prefer shared application services or small internal helpers if the existing query handlers cannot be composed cleanly.

## Backend Ownership Rules

Bootstrap is a read model, not a gameplay command.

It should:

- read the current character from `CurrentUserId` / `CurrentCharacterGuid`,
- return only data for the authenticated selected character,
- never mutate gameplay state except for safe idempotent creation that existing getters already perform,
- represent completed tutorial as `null`,
- be safe to call on app startup, browser refresh, and reconnect recovery.

It should not:

- start combat,
- claim rewards,
- grant tutorial items,
- select or change active loadouts,
- perform route/client tutorial completion,
- replace feature-specific endpoints.

## Frontend Shape

Add a game bootstrap API service:

```text
LL/src/Presentation/ll/src/app/core/services/api/game-bootstrap/game-bootstrap.service.ts
```

Add a game bootstrap state/store:

```text
LL/src/Presentation/ll/src/app/core/services/api/game-bootstrap/game-bootstrap-state.service.ts
```

Suggested store responsibilities:

- hold loading/error/loaded state,
- call `GameBootstrap` exactly once per authenticated game session,
- hydrate `AuthService` or `CharacterStateService` with `character`,
- hydrate `TutorialStateService` with `tutorial`,
- hydrate `CharacterActionsStateService` with `currentAction`,
- expose `loaded()` so the dashboard can delay rendering tutorial-dependent shell UI until initial state is known.

Suggested frontend model:

```ts
export interface GameBootstrapDto {
  character: CharacterDto;
  tutorial: TutorialState | null;
  currentAction: CharacterActionDto | null;
  serverTimeUtc: string;
}
```

## Startup Flow

Recommended target flow:

1. Angular `APP_INITIALIZER` still runs `AuthService.checkAuth()` to refresh tokens.
2. `AuthService.checkAuth()` should only establish authentication and token state.
3. When `/game` dashboard shell initializes, `GameBootstrapStateService.load()` runs.
4. `GameBootstrapStateService` calls `GET /GameBootstrap`.
5. It initializes:
   - current character,
   - tutorial state,
   - current action state,
   - optional server clock baseline.
6. Realtime events keep these stores fresh after bootstrap.
7. Page components load only their own page data.

This keeps public/landing routes light. A player who opens the landing page while logged out should not call game bootstrap.

## Where To Trigger Bootstrap

Best first trigger point: `DashboardComponent`.

Reason:

- `DashboardComponent` is the root of the authenticated `/game` shell.
- It already owns shell UI: sidebar, navbar, chat, current action, tutorial quest panel.
- It avoids loading game data from public routes.
- It avoids putting gameplay-specific bootstrap into `AppComponent`, which also wraps public/auth views.

Longer term, this can become a route resolver or guarded shell initializer, but a dashboard-level state service is easier to debug and safer for incremental migration.

## Relationship To Auth

Do not make auth responsible for the entire game bootstrap.

`AuthService` should answer:

- am I authenticated?
- what is my access token?
- who is my current character at a lightweight identity level?

`GameBootstrapStateService` should answer:

- has the authenticated game shell been hydrated?
- what shared game state did the backend return?
- have dependent stores been initialized?

During migration, `AuthService.checkAuth()` can continue fetching `Character` until bootstrap takes over that responsibility. The final shape should avoid fetching character twice.

## Relationship To Tutorial

Bootstrap should replace first-render tutorial polling.

Current pattern to remove over time:

```ts
ngOnInit(): void {
  this.tutorialState.load();
}
```

Target pattern:

```ts
const tutorial = this.tutorialState.state();
if (!tutorial) return;
```

Tutorial state should be initialized from bootstrap:

```ts
this.tutorialState.initialize(bootstrap.tutorial);
```

After bootstrap:

- `TutorialProgressedMsg` updates the tutorial store,
- `TutorialCompletedMsg` sets the tutorial store to `null`,
- explicit `GET /Tutorial` remains only for debugging/manual recovery.

## Relationship To Current Action

Current action is also shell-level state because it affects:

- current action banner,
- combat overlay visibility,
- "go to action" behavior,
- idle/combat session summary behavior.

Bootstrap should include the current action so `AppComponent` does not need to independently call `CharacterActionsStateService.init()` immediately after auth.

Target:

- bootstrap initializes current action once,
- realtime/action commands update it later,
- `CharacterActionsStateService.init()` becomes either a bootstrap hydration method or a reconnect/manual refresh method.

## Relationship To Realtime

Realtime should start after auth, as it does now.

Bootstrap and realtime must tolerate either order:

- if bootstrap arrives first, realtime events patch initialized stores,
- if realtime connects first, stores should still accept later bootstrap data,
- if reconnect happens, either call bootstrap again or call targeted refreshes for character/current action/tutorial.

Recommended first approach:

- keep current realtime initialization,
- call bootstrap on dashboard load,
- on realtime reconnect, call a lightweight `GameBootstrapStateService.reload()` only if the app was authenticated and already bootstrapped.

This gives one recovery call instead of several independent feature refreshes.

## Migration Plan

### Phase 1: Add Backend Bootstrap Read Model

1. Add `GameBootstrapDto`.
2. Add `GetGameBootstrapQuery`.
3. Add `GameBootstrapController`.
4. Compose current character, tutorial, current action, and server time.
5. Return `tutorial: null` when tutorial is completed.

Done when `GET /GameBootstrap` can replace the first `Character`, `Tutorial`, and `CharacterActions` startup calls.

### Phase 2: Add Frontend Bootstrap Store

1. Add `GameBootstrapService`.
2. Add `GameBootstrapStateService`.
3. Add `initializeFromBootstrap` methods to:
   - `TutorialStateService`,
   - `CharacterActionsStateService`,
   - possibly `CharacterStateService` or `AuthService`.
4. Trigger bootstrap from `DashboardComponent`.
5. Display a minimal shell loading state if bootstrap is still pending.

Done when `/game` calls bootstrap once and dependent shell state initializes from it.

### Phase 3: Remove Duplicate Initial Loads

Remove or guard:

- `tutorialState.load()` from region, inventory, essences, combat area card, and tutorial quest components.
- immediate `CharacterActionsStateService.init()` from `AppComponent`.
- duplicate current character fetches from auth once bootstrap owns character hydration.

Done when normal `/game` entry does not call separate tutorial/current-action endpoints.

### Phase 4: Reconnect Recovery

1. Decide whether reconnect recovery uses full bootstrap reload or targeted store refreshes.
2. Prefer full bootstrap reload for now because payload is small.
3. Ensure realtime events and bootstrap responses do not fight each other.
4. Add request de-duping so multiple shell components cannot launch parallel bootstrap calls.

Done when websocket reconnect can rehydrate shell state without page-specific reload hacks.

### Phase 5: Optional Expansion

Only after the core flow is stable, consider adding:

- lightweight unread chat/channel metadata,
- online player count,
- daily/weekly reset timestamps,
- global feature flags,
- selected character account permissions.

Avoid adding large page-owned collections.

## Implementation Details To Watch

### Avoid Response Bloat

Bootstrap should stay small. It should answer "what does the shell need to know?" not "what does every page need?"

Bad additions:

- all inventory items,
- all crafting recipes,
- all prophecy cards,
- full guild details.

Good additions:

- active tutorial state,
- current action,
- current character identity/currencies,
- server time.

### Avoid Double Fetching Character

During migration, `AuthService.checkAuth()` and bootstrap may both fetch character data.

Short-term acceptable:

- keep both while wiring bootstrap.

Final target:

- `checkAuth()` refreshes auth only,
- bootstrap hydrates character.

### Keep Completed Tutorial Cheap

Backend tutorial cache already supports inactive completed state. Bootstrap should preserve that:

- `tutorial: null` for completed or inactive tutorial,
- no page-level tutorial fetches after startup,
- realtime completion also sets frontend tutorial state to `null`.

### Handle No Character Edge Cases

The current game appears to treat the character as claim-backed and always present after auth. Still, the bootstrap plan should define behavior:

- if the character claim is missing or invalid, return `401`,
- if the character no longer exists, return `404` or force logout,
- if multiple-character support is added later, bootstrap should accept a selected-character id or use a selected-character cookie/claim.

## Acceptance Criteria

- Opening `/game` after login calls `GET /GameBootstrap` once.
- Tutorial quest panel is initialized from bootstrap.
- Completed tutorial appears as `tutorial: null`.
- First render of region, inventory, essences, crafting, and combat card does not call `GET /Tutorial`.
- Current action shell UI initializes from bootstrap.
- Realtime tutorial events still update tutorial state after bootstrap.
- Browser refresh resumes the current tutorial step without navigating to the right page first.
- Public routes do not call bootstrap.
- Backend build and Angular build pass.

## Suggested Verification

Backend:

```powershell
dotnet build LL/src/API/API.LL/API.LL.csproj
```

Frontend:

```powershell
cd LL/src/Presentation/ll
node ./node_modules/@angular/cli/bin/ng.js build --configuration development
```

Manual browser checks:

1. Log in with an incomplete tutorial.
2. Confirm `/GameBootstrap` returns tutorial state.
3. Confirm no immediate `GET /Tutorial` call follows.
4. Refresh the browser on a non-tutorial page.
5. Confirm tutorial state resumes and quest action navigates correctly.
6. Complete the tutorial.
7. Refresh again and confirm `tutorial: null`.

