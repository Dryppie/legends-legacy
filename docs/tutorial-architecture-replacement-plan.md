# Tutorial Architecture Replacement Plan

## Goal

Replace the current tutorial implementation with an event-driven tutorial architecture where:

- The backend owns tutorial truth and progression.
- Static tutorial definitions describe the tutorial flow.
- Frontend tutorial code only presents the current active step.
- Completed tutorials become cheap: no page-level tutorial state polling, and no DB lookup for unrelated actions.
- Backend-verified tutorial steps progress from real game events, not client claims.
- Client-only tutorial steps are explicitly reported only when the active step expects them.

This plan builds on the current first-party tour overlay. It replaces the tutorial progression and state architecture around it, not the visual tour system itself.

## Current State

The current implementation already has some useful pieces:

- Backend progress exists in `CharacterTutorialProgress`.
- A tutorial controller exposes state and tutorial-specific commands.
- A first-party Angular tour system displays guided steps.
- Several backend actions call tutorial progress methods directly or indirectly.
- Some recent cleanup moved equipment progress behind an `EquipmentChangedEvent`.

The main problems are:

- Tutorial state is fetched or refreshed from many frontend services and components.
- Tutorial progression logic is concentrated in `TutorialService` as many `Record...` methods.
- Some feature services know about tutorial-specific behavior.
- The frontend owns separate step presentation data in `assets/help/tours/tutorial-*.json`.
- Tutorial state DTOs include UI copy and routes, mixing progression state with presentation.
- Completed tutorial state is not represented as `null`/inactive in the frontend store, so components still ask about it.

## Target Architecture

### Backend Owns Truth

The backend stores active tutorial progress, current step, version, and completion.

For the first migration, keep the scope character-level because the existing tutorial is character-level. The model should still leave room for account-level tutorials later.

Recommended eventual shape:

```csharp
public sealed class TutorialProgress
{
    public Guid Id { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? CharacterId { get; set; }
    public TutorialScope Scope { get; set; }
    public string TutorialKey { get; set; } = default!;
    public int TutorialVersion { get; set; }
    public string CurrentStepKey { get; set; } = default!;
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum TutorialScope
{
    Account,
    Character
}
```

For the simple linear `First Steps` tutorial, we do not need a separate step progress table yet. A single `CurrentStepKey` is enough.

Keep tutorial-specific reward flags only if they are needed for idempotency. Long term, prefer idempotent reward records or event/outbox-style safeguards over expanding the progress row with many one-off columns.

### Static Tutorial Definitions

Tutorial definitions should be static data, not editable database rows.

Recommended location:

```text
LL/src/API/API.LL/Data/tutorials/first-steps.v1.json
```

or, if the data should be shared deeper than the API project:

```text
LL/src/Infrastructure/Service/Services.LL/Tutorials/Definitions/first-steps.v1.json
```

The definition should include both progression trigger data and presentation metadata.

Example:

```json
{
  "tutorialKey": "first-steps",
  "version": 1,
  "scope": "Character",
  "initialStepKey": "defeat-training-creature",
  "steps": [
    {
      "key": "defeat-training-creature",
      "title": "Training Area",
      "objective": "Defeat the creature in the Training Area.",
      "trigger": {
        "type": "CombatEncounterCompleted",
        "areaId": "tutorial_area_training_grounds",
        "requiresVictory": true
      },
      "presentation": {
        "route": "/game/world/regions/tutorial?area=tutorial_area_training_grounds",
        "tourPageId": "tutorial-training-area",
        "actionLabel": "Go to Training Area"
      },
      "nextStepKey": "absorb-essence"
    }
  ]
}
```

Definitions should replace duplicated constants in:

- `Domain.Models.Tutorials.TutorialConstants`
- `LL/src/Presentation/ll/src/app/shared/models/tutorial.ts`
- tutorial route/action labels hardcoded in `TutorialService`

The frontend can still use `assets/help/tours/tutorial-*.json` during migration, but the end state should derive the active tour/page id from the backend-provided tutorial definition state.

## Backend Design

### New Application Services

Introduce a progression service with a trigger-based API:

```csharp
public interface ITutorialProgressionService
{
    Task<TutorialProgressResult?> TryProgressAsync(
        Guid characterId,
        TutorialTrigger trigger,
        CancellationToken cancellationToken);

    Task<TutorialStateDto?> GetActiveStateAsync(
        Guid characterId,
        CancellationToken cancellationToken);

    Task<TutorialStateDto?> CompleteClientStepAsync(
        Guid characterId,
        CompleteClientTutorialStepRequest request,
        CancellationToken cancellationToken);

    Task SkipAsync(Guid characterId, CancellationToken cancellationToken);
}
```

`TryProgressAsync` should:

1. Read active tutorial state from cache.
2. Return immediately if cached inactive.
3. Load active progress from DB only on cache miss.
4. Resolve the current step from static definitions.
5. Compare the current step trigger with the incoming trigger.
6. Complete the current step if it matches.
7. Grant any step rewards idempotently.
8. Move to the next step or complete the tutorial.
9. Update cache.
10. Publish realtime tutorial update events.

### Active Tutorial Cache

Use a cache entry per character:

```text
tutorial:active:{characterId}
```

Use an explicit inactive marker instead of raw null:

```csharp
public sealed class CachedTutorialState
{
    public bool IsActive { get; init; }
    public string? TutorialKey { get; init; }
    public int? Version { get; init; }
    public string? CurrentStepKey { get; init; }
}
```

Completed tutorials should cache:

```json
{ "isActive": false }
```

This makes future tutorial-relevant events cheap after completion.

### Event-Based Backend Progression

Feature code should publish facts. Tutorial handlers should interpret those facts.

Examples:

- `EquipmentChangedEvent`
- `EssenceAbsorbedEvent`
- `EssenceLoadoutChangedEvent`
- `TutorialTrainingBattleWonEvent`
- `CraftingPageVisitedClientStep`
- `CraftedEquipmentEvent`

Good pattern:

```csharp
await _publisher.Publish(
    new EssenceAbsorbedEvent(characterId, essenceDefinitionId),
    cancellationToken);
```

Tutorial handler:

```csharp
public sealed class TutorialEssenceAbsorbedHandler
    : INotificationHandler<EssenceAbsorbedEvent>
{
    public Task Handle(EssenceAbsorbedEvent notification, CancellationToken ct) =>
        _tutorialProgression.TryProgressAsync(
            notification.CharacterId,
            TutorialTrigger.ServerEvent(
                TutorialTriggerType.EssenceAbsorbed,
                new { notification.EssenceDefinitionId }),
            ct);
}
```

Bad pattern to remove:

```csharp
await _tutorialService.RecordEssenceAbsorbedAsync(...);
```

The feature should not know which tutorial step cares about the event.

### Backend Endpoints

Keep the tutorial API small:

```text
GET  /api/v1/tutorial/state
POST /api/v1/tutorial/client-step
POST /api/v1/tutorial/skip
POST /api/v1/tutorial/reset    dev/admin only
```

`GET /tutorial/state` is for bootstrap fallback, debugging, and manual refresh. It should not be called by every page.

`POST /tutorial/client-step` is only for client-only steps such as:

- route visited
- guided element clicked
- panel opened
- popover closed

It must validate that the requested client trigger matches the active current step. The frontend must not be able to complete backend-verified actions by claiming they happened.

The current `POST /tutorial/visit-crafting` should be replaced by the generic `client-step` endpoint.

`POST /tutorial/start-training-battle` may remain as a tutorial-specific command if the Training Area is truly tutorial-only. If it becomes a normal combat area with special availability rules, move it behind the normal combat flow and progress via combat completion events.

### Realtime Updates

Use the existing game realtime system to push tutorial state changes.

Add messages:

```csharp
public sealed record TutorialProgressedMsg(TutorialStateDto Tutorial) : GameEventMsg;
public sealed record TutorialCompletedMsg(string TutorialKey) : GameEventMsg;
```

When `TryProgressAsync` advances a step:

- publish `TutorialProgressedMsg` to the character audience

When the tutorial completes:

- publish `TutorialCompletedMsg`
- cache inactive state

This avoids adding tutorial fields to every command response.

### Bootstrap State

The best end state is for the primary game bootstrap or selected-character state response to include tutorial state once:

```json
{
  "character": {},
  "tutorial": {
    "tutorialKey": "first-steps",
    "version": 1,
    "currentStepKey": "absorb-essence"
  }
}
```

When completed:

```json
{
  "character": {},
  "tutorial": null
}
```

If there is no single bootstrap endpoint yet, keep `GET /tutorial/state` for now and treat bootstrap integration as a later phase.

## Frontend Design

### Tutorial Store

Replace `TutorialStateService` with a store that is initialized once from bootstrap or `GET /tutorial/state`.

Recommended shape:

```ts
export interface TutorialState {
  tutorialKey: string;
  version: number;
  currentStepKey: string;
  presentation: TutorialStepPresentation;
}

@Injectable({ providedIn: 'root' })
export class TutorialStore {
  private readonly _state = signal<TutorialState | null>(null);

  readonly state = this._state.asReadonly();
  readonly isActive = computed(() => this._state() !== null);

  initialize(state: TutorialState | null): void {
    this._state.set(state);
  }

  complete(): void {
    this._state.set(null);
  }
}
```

Completed tutorial state should be `null`.

Frontend code should generally ask:

```ts
if (!this.tutorialStore.isActive()) return;
```

It should not call `GET /tutorial/state` from each page.

### Tour Presentation

The current first-party tour overlay can remain. Its input should become the current tutorial step presentation instead of page components manually starting fixed tour ids.

Current pattern to remove:

```ts
this.tutorialState.load();
this.tour.start(TUTORIAL_GUIDANCE_BY_STEP[step].tourPageId);
```

Target pattern:

```ts
const step = this.tutorialStore.state();
if (!step) return;

this.tutorialPresenter.start(step.presentation);
```

The presenter should:

- navigate to `presentation.route` when the quest action is clicked
- start the associated `tourPageId`
- use action-aware first-party tour steps for click/wait behavior
- stop itself when the store becomes `null`

### Client Step Completion

Frontend can complete only client-authorized step types.

Example route watcher:

```ts
onRouteChanged(url: string): void {
  const tutorial = this.tutorialStore.state();
  if (!tutorial) return;

  const trigger = tutorial.presentation.clientTrigger;
  if (trigger?.type !== 'ClientRouteVisited') return;
  if (trigger.route !== url) return;

  this.tutorialApi.completeClientStep({
    tutorialKey: tutorial.tutorialKey,
    version: tutorial.version,
    stepKey: tutorial.currentStepKey,
    triggerType: 'ClientRouteVisited',
    route: url,
  }).subscribe((state) => this.tutorialStore.initialize(state));
}
```

### Realtime Tutorial Updates

Extend the existing game event map with:

- `TutorialProgressedMsg`
- `TutorialCompletedMsg`

Frontend handlers:

```ts
effect(() => {
  const event = this.gameEvents.event.TutorialProgressedMsg();
  if (!event) return;
  this.tutorialStore.initialize(event.tutorial);
});

effect(() => {
  const event = this.gameEvents.event.TutorialCompletedMsg();
  if (!event) return;
  this.tutorialStore.complete();
});
```

After this, command handlers such as equipment, essence, and crafting do not need to refresh tutorial state manually.

## Migration Plan

### Phase 1: Stabilize Current Boundaries

1. Keep `CharacterTutorialProgress` and existing tutorial DB table.
2. Add `TutorialTrigger`, `TutorialTriggerType`, and `ITutorialProgressionService`.
3. Move current `Record...` methods behind `TryProgressAsync`.
4. Keep `ITutorialService` as a facade temporarily if that reduces churn.
5. Convert direct tutorial calls in feature services to application events.
6. Keep existing frontend behavior while backend internals change.

Phase 1 is complete when all backend tutorial progression is trigger/event based.

### Phase 2: Static Definitions

1. Add static `first-steps.v1.json`.
2. Add a `TutorialDefinitionProvider`.
3. Move step titles, objectives, action labels, routes, tour page ids, and trigger rules into the definition.
4. Replace `TutorialConstants` step ordering with definition-based next step resolution.
5. Change `TutorialStateDto` to return:
   - `tutorialKey`
   - `version`
   - `currentStepKey`
   - current step presentation
6. Keep legacy DTO fields only as a temporary compatibility layer if needed.

Phase 2 is complete when changing tutorial text/route/trigger no longer requires code changes.

### Phase 3: Realtime Progress Updates

1. Add backend realtime contracts:
   - `TutorialProgressedMsg`
   - `TutorialCompletedMsg`
2. Publish these from `ITutorialProgressionService`.
3. Add frontend game event models and handlers.
4. Update `TutorialStore` from realtime events.
5. Remove tutorial refresh calls from:
   - equipment state service
   - essence state service
   - crafting service
   - character action state service

Phase 3 is complete when backend-verified tutorial progress appears on the frontend without manual refetches.

### Phase 4: Bootstrap and Inactive State

1. Include tutorial state in the selected-character/bootstrap response.
2. Initialize `TutorialStore` once during app/game bootstrap.
3. Return `tutorial: null` when completed.
4. Ensure first-party tour and tutorial quest UI do nothing when tutorial state is `null`.
5. Keep `GET /tutorial/state` only for debugging/manual refresh.

Phase 4 is complete when normal navigation does not call tutorial state endpoints.

### Phase 5: Client Step Endpoint

1. Add `POST /tutorial/client-step`.
2. Replace `POST /tutorial/visit-crafting`.
3. Add frontend route/element watchers for client-only steps.
4. Ensure the backend validates that the reported client step matches the active current step.

Phase 5 is complete when client-only tutorial steps use one generic endpoint.

### Phase 6: Cache Active/Inactive Tutorial State

1. Add a cache abstraction for active tutorial state.
2. Cache active progress by character id.
3. Cache inactive completed state explicitly.
4. Invalidate/update cache whenever tutorial progresses, skips, resets, or completes.
5. Make event handlers exit immediately when cached inactive.

Phase 6 is complete when completed tutorials do not cause DB lookups from tutorial-relevant events.

### Phase 7: Cleanup Legacy Code

Remove or collapse:

- `TUTORIAL_GUIDANCE_BY_STEP` from Angular.
- hardcoded tutorial step labels/routes in backend service code.
- per-feature `tutorialState.refresh()` calls.
- specific `Record...` methods on `ITutorialService`.
- `POST /tutorial/visit-crafting`.
- tutorial state polling in page components.

Keep:

- first-party tour overlay
- tutorial quest panel
- tutorial-specific game content, such as Training Area and tutorial rewards
- `data-tour`/future `data-tutorial` anchors

## Current Code Areas To Touch

Backend:

- `LL/src/Core/Domain/Models/Tutorials/CharacterTutorialProgress.cs`
- `LL/src/Core/Domain/Models/Tutorials/TutorialConstants.cs`
- `LL/src/Core/Application/Interfaces/Services/LL/Tutorials/ITutorialService.cs`
- `LL/src/Infrastructure/Service/Services.LL/Tutorials/TutorialService.cs`
- `LL/src/Infrastructure/Service/Services.LL/Tutorials/TutorialBattleService.cs`
- `LL/src/API/API.LL/Controllers/V1/TutorialController.cs`
- feature events under `LL/src/Core/Application/UseCases/**/Events/`
- game realtime contracts under `LL/src/Core/Application/WebSockets/Contracts/`

Frontend:

- `LL/src/Presentation/ll/src/app/core/services/api/tutorial/tutorial-state.service.ts`
- `LL/src/Presentation/ll/src/app/core/services/api/tutorial/tutorial.service.ts`
- `LL/src/Presentation/ll/src/app/core/services/client-side/first-party-tour/`
- `LL/src/Presentation/ll/src/app/shared/components/first-party-tour-overlay/`
- `LL/src/Presentation/ll/src/app/layout/dashboard/tutorial-quest/`
- tutorial refresh calls in equipment, essence, crafting, character action, region, inventory, and combat components
- `LL/src/Presentation/ll/src/assets/help/tours/tutorial-*.json`
- `LL/src/Presentation/ll/src/assets/help/guides/tutorial-*.json`

## Important Decisions

### Character-Level First, Account-Level Later

Keep the first replacement character-level to avoid derailing the migration. Add `TutorialScope` to definitions and model shape so account-level tutorials can be introduced later without redesigning the whole system.

### SignalR Over Response Pollution

Use realtime tutorial events for backend-verified tutorial progress. Do not add tutorial payloads to every command response.

### Static Definitions Over Database Editing

Use JSON or seeded static data. Avoid making tutorial steps editable in the database until there is a real live-editing need.

### Definitions Own Presentation Metadata

The backend should return enough presentation metadata for the frontend to know where to guide the player. The frontend should not maintain a second copy of step ordering and destination routes.

### Frontend Owns Rendering

The frontend still owns DOM targeting and overlay behavior. The backend should not know about exact CSS layout details beyond stable presentation keys such as route, tour id, and target id.

## Acceptance Criteria

- A completed tutorial is represented as `tutorial: null` in frontend state.
- Opening unrelated pages does not call tutorial endpoints.
- Equip, essence, combat, and crafting systems publish events; they do not call tutorial progression methods directly.
- Tutorial progression service can ignore inactive/completed tutorials from cache.
- Backend-verified steps cannot be completed by a client claim.
- Client-only steps use one generic endpoint and are validated against the active step.
- Tutorial progression is pushed to the frontend through realtime events.
- The first-party tour overlay starts from active tutorial state, not scattered component calls.
- Step copy, route, trigger, and tour id live in static tutorial definitions.

## Suggested Implementation Order

1. Add definitions and provider while keeping current service behavior.
2. Introduce trigger-based progression behind existing `ITutorialService`.
3. Convert direct backend tutorial calls to event handlers.
4. Add realtime tutorial progress messages.
5. Replace frontend refresh calls with realtime store updates.
6. Add bootstrap initialization and `tutorial: null`.
7. Replace `visit-crafting` with `client-step`.
8. Add active/inactive tutorial cache.
9. Delete legacy constants, duplicate frontend step maps, and polling code.

