# Automatic Return to Combat After Tempering

> Historical Alpha plan, superseded 3 September 2026. Crafting/gathering progression, queued tempering and their obsolete quest content have been removed. Conversion, refund and compatibility/backfill proposals below are not current implementation work. Shared numerical helpers with active consumers may remain. See the [post-Alpha cleanup](design/equipment-post-alpha-cleanup.md) and [current quest flow](../LEGENDSLEGACY_QUEST_FLOW.md) for supported behavior.

## Summary

Whenever a player enters a standard combat area, the game should remember that area as the eligible return destination. Once a later tempering queue finishes naturally, the character should automatically resume combat in that area.

This must be implemented as a server-side action transition rather than only as a frontend redirect. Queue resolution is lazy, so a client-only implementation would still cause idle time whenever the game is closed or backgrounded.

## Current Behavior

- Starting tempering replaces the active `CombatActionDetails`, causing the previous combat area to be lost.
- When the tempering queue naturally becomes empty, the crafting service marks the character action as deleted and clears its next resolution time.
- Frontend action polling stops after receiving that deleted action.
- Navigation to `/game/combat` currently happens only after a player manually starts combat.

As a result, finishing a tempering queue leaves the character idle until the player manually chooses another combat area.

## Functional Requirements

### Remembering the return destination

1. Whenever standard combat begins, including an automatic return from tempering, the server must store that combat area's identifier as the eligible return destination.
2. Directly replacing or explicitly quitting standard combat must retain the stored destination for the next tempering queue.
3. "Previously fought area" means the most recently entered standard combat area, rather than any area merely visited without starting combat.
4. Starting tempering while already idle must preserve the remembered destination; if the player has never entered eligible combat, it must not invent one.
5. Pausing and later resuming the same tempering queue must retain its return destination.
6. If a paused tempering queue is resumed while the character is fighting in another standard combat area, that newer area must become the return destination.

### Completing the queue

7. When the final queued item finishes tempering naturally, the server must automatically transition the character back to combat in the remembered area.
8. Combat must begin at the exact final tempering resolution boundary, not at the time of the next client request.
9. If the queue is resolved late, the server must calculate combat progress from that completion boundary through the current server time, subject to the existing offline-combat limits.
10. The final tempering results and the resumed combat result must be returned by the same action-resolution response.
11. The transition must be atomic and idempotent so concurrent resolution requests cannot create duplicate combat schedules or award duplicate rewards.

### Actions that must not trigger automatic combat

Automatic combat must not start when the queue becomes empty because the player:

- Cancels the complete queue.
- Manually removes its final item.
- Explicitly stops or abandons the queue's return behavior.

Natural queue exhaustion and player-initiated queue removal must remain distinguishable.

### Access and failure handling

12. The server must revalidate access to the remembered area before restarting combat.
13. If the area no longer exists or the player can no longer access it, tempering must still finish successfully and the character must remain idle.
14. A permanently invalid destination must not cause repeated automatic retries.
15. Transient failures must preserve transactional consistency: either tempering completion and the combat transition both commit, or the resolution can be retried without duplicate rewards.

## Recommended Technical Design

### Persisted return destination

Add a nullable field such as `ReturnToCombatAreaId` to the `CharacterAction` aggregate.

Keeping this field on the action root is preferable to storing it on `CraftingActionDetails` because the existing pause behavior removes the crafting details while retaining the queued items. The root field can survive a paused queue and be cleared when the return intent is no longer applicable.

The field should be managed as follows:

- Set or replace it whenever standard combat begins, including automatic combat resumption.
- Retain it when active combat is directly replaced by tempering or explicitly stopped.
- Preserve it when tempering is paused.
- Replace it with the current combat area when tempering is resumed from another active combat action.
- Preserve it when tempering begins from idle; leave it null only when no eligible combat area has been entered.
- Keep it set to the resumed area after a successful automatic combat transition.
- Clear it when the queue is explicitly cancelled or its final item is manually removed.

This requires an EF Core migration.

### Exact completion boundary

The crafting resolution flow must expose the timestamp at which the final tempering action completed. `TemperingSession.To` currently represents the request time and should not be treated as the queue completion time.

The completion timestamp should be captured explicitly by the crafting result or as transient action metadata. This timestamp becomes the first eligible combat boundary.

### Atomic crafting-to-combat transition

When natural queue completion is detected:

1. Read the persisted return area.
2. Revalidate access to the area.
3. Recreate `CombatActionDetails` using existing combat-detail creation logic.
4. Replace the empty crafting details with combat details in the current resolution transaction.
5. Increment the action's schedule generation.
6. Set `IsDeleted` to `false`.
7. Set the first combat boundary to the tempering completion timestamp.
8. Apply the normal combat switch lock from that timestamp.
9. Resolve combat through the current server time using the existing idle-combat service.
10. Keep the persisted destination set to the resumed combat area after the transition succeeds.

The existing `CharacterActions/Resolve` endpoint should be sufficient. A separate public endpoint is not required.

## Frontend Requirements

The action state returned after completion can be a Combat action containing both:

- The final `TemperingSession`.
- The newly resolved `CombatSession`.

The frontend crafting action handler currently processes a tempering session only when the resulting action type is Crafting. It must instead process any returned `TemperingSession`, regardless of the final action type. Otherwise the player can lose final outcome cards and client-side profession progression feedback during the automatic transition.

The frontend must also:

- Replace the displayed queue with the returned empty queue.
- Process final tempering outcomes exactly once.
- Process the returned combat session through the existing combat handler.
- Continue polling using the resumed combat action's next boundary.
- Update the persisted client-side action type to Combat.

### Navigation behavior

Recommended behavior:

- If the player is still on the Tempering screen when completion is observed, navigate to `/game/combat`.
- If the player has navigated elsewhere while waiting, resume combat silently and expose it through the existing active-action indicator rather than forcibly replacing the player's current page.
- If the application is opened after the transition has already occurred, hydrate the active combat normally without requiring a special redirect.

If the intended product behavior is to force the Combat screen from every page, that should be confirmed explicitly because it may interrupt unrelated player activity.

## Acceptance Criteria

### Standard live transition

Given a character is fighting in area A,
when the player starts a tempering queue and the queue finishes naturally,
then the character automatically becomes active in combat in area A without an idle scheduling gap.

### Offline transition

Given a character interrupts combat in area A with a tempering queue,
when the client is closed long enough for the queue to finish,
then the next resolution resumes combat from the queue's historical completion boundary and grants the combat progression allowed by the existing offline limits.

### No previous combat

Given a character starts tempering while idle,
when the queue finishes,
then the character remains idle.

### Explicit cancellation

Given a tempering queue has a remembered combat destination,
when the player cancels the queue or removes its final item,
then combat does not start automatically.

### Pause and resume

Given a queue interrupted combat in area A and is later paused,
when that same queue is resumed from idle and finishes,
then combat resumes in area A.

### Newer combat destination

Given a paused tempering queue originally interrupted area A,
when the player starts combat in area B and subsequently resumes the queue,
then completing the queue resumes combat in area B.

### Invalid destination

Given the remembered area is missing or inaccessible,
when the queue finishes,
then tempering rewards are committed, the character becomes idle, and automatic restart is not repeatedly attempted.

### Concurrent resolution

Given multiple clients resolve the final queue boundary concurrently,
when the requests complete,
then only one combat schedule is created and tempering and combat rewards are each awarded once.

## Expected Change Surface

### Domain and persistence

- `CharacterAction` persisted return-area field.
- Character-action persistence configuration.
- Character-action repository transition and cleanup behavior.
- EF Core migration and model snapshot.

### Application and services

- Character-action resolution orchestration.
- Crafting completion metadata.
- Combat-area access validation.
- Atomic crafting-to-combat transition.
- Combined final tempering and combat sessions.

### Frontend

- Character-action state transition handling.
- Crafting handler support for a final tempering session on a Combat action.
- Optional scoped navigation from Tempering to Combat.
- Tests for combined sessions and polling continuation.

## Required Tests

Backend coverage should include:

- Combat A to tempering to automatic Combat A.
- Correct combat schedule boundary after the final tempering action.
- Offline combat catch-up after historical queue completion.
- Queue started from idle remaining idle.
- Cancellation and manual removal not starting combat.
- Pause and resume retaining the destination.
- A newer combat area replacing the original destination.
- Invalid and inaccessible area fallback.
- Combined final tempering and combat sessions.
- Concurrent/idempotent final resolution.
- Persistence of the return destination across reloads.

Frontend coverage should include:

- Crafting outcomes being processed when the returned action type is Combat.
- Combat session handling after the transition.
- Continued combat polling.
- Navigation only under the agreed route conditions.
- No duplicate outcome processing after repeated snapshots.

## Migration and Deployment Implications

- An EF Core migration is required for the nullable return-area field.
- The migration must be deployed before or together with the backend version that reads and writes the field.
- Existing character actions must default to a null return destination and preserve current behavior.
- No infrastructure-as-code or external environment changes are required.
- No new public API endpoint is expected.

## Verification Baseline

At the time of this analysis, a fresh backend build completed with zero warnings and zero errors. All 1,499 existing backend tests passed.
