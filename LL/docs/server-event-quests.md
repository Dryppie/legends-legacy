# Server-wide event quests

> Content update, 3 September 2026: only the expired Defense of Lumo example remains in Data/event-quests. The former crafting/tempering and gathering events were deleted; no schedule was extended. Descriptions of those events below are historical. Further LiveOps work is deferred. See the [current quest flow](../../LEGENDSLEGACY_QUEST_FLOW.md#4-scheduled-server-wide-event-quests).

Server-wide event quests are scheduled, content-driven quests where every character in the current Legends Legacy database contributes toward shared objectives. The first implementation deliberately treats one deployed database as one game server. If realms are introduced later, add a `RealmId` to each event quest persistence key and audience.

## What is implemented

- Event definitions are JSON files under `Data/event-quests`.
- Definitions can be disabled, scheduled, ended, and given a separate reward-claim deadline.
- Existing durable gameplay outbox events feed both personal quests and event quests.
- Each outbox message is counted at most once per event objective.
- Global objective totals and each character's contribution are persisted.
- Definitions can contain cumulative personal contribution milestones with their own rewards.
- Rewards are claimed by eligible players instead of being fanned out to every account at completion time.
- Event progress changes are broadcast to the whole server through the existing game SignalR hub.
- World chat announces an event when it opens and again when the server finishes it. The lines are generated from the event title, link to the quest journal, and use a deterministic message id so a retry cannot post the same announcement twice.
- The quest journal shows active, upcoming, completed, and expired events in a focused Event tab, including the live top-three contributors, the current player's rank, shared progress, and personal milestones.

## Authoring an event

Create a uniquely named JSON file in `LL/src/API/API.LL/Data/event-quests`. Do not reuse an event ID for a later occurrence. Reusing an ID would make the new schedule refer to the old persisted instance and claims.

```json
{
  "id": "event.lumo_defense.2026_09",
  "version": 1,
  "enabled": false,
  "title": "The Defense of Lumo",
  "summary": "Drive the creatures back from Lumo Ruins before the wards fail.",
  "startsAtUtc": "2026-09-04T18:00:00Z",
  "endsAtUtc": "2026-09-07T18:00:00Z",
  "claimEndsAtUtc": "2026-09-14T18:00:00Z",
  "minimumContribution": 5,
  "sortOrder": 100,
  "objectives": [
    {
      "key": "lumo-victories",
      "description": "Win 250,000 encounters in Lumo Ruins.",
      "type": "CombatEncounterCompleted",
      "requiredAmount": 250000,
      "filters": {
        "areaId": "region_01_area_01",
        "requiresVictory": true
      }
    }
  ],
  "rewards": [
    {
      "key": "ore",
      "type": "Item",
      "itemBaseId": "ore",
      "quantity": 25
    }
  ],
  "personalMilestones": [
    {
      "key": "first-response",
      "requiredContribution": 10,
      "rewards": [
        {
          "key": "first-response-ore",
          "type": "Item",
          "itemBaseId": "ore",
          "quantity": 5
        }
      ]
    },
    {
      "key": "steadfast-defender",
      "requiredContribution": 50,
      "rewards": [
        {
          "key": "steadfast-wood",
          "type": "Item",
          "itemBaseId": "wood",
          "quantity": 10
        }
      ]
    }
  ]
}
```

The supported objective types are the same durable event-backed types used by normal quests: combat completion, area actions with a gathering tool, gathered resource quantities (`ResourceGathered`), Essence absorption/focus/ascension, crafting, tempering, character level, Colosseum and tournament battles, dungeon starts/completions, and daily prophecies. Equipment-state objectives are intentionally excluded from event definitions because they require querying mutable state after the original action.

`minimumContribution` is the total qualifying contribution a character must make across the event's objectives before claiming the community-completion reward. Global progress is capped at each objective's required amount, while personal contribution records the player's actual qualifying contribution through `endsAtUtc`, including after the community target completes.

Personal milestones must use unique keys and strictly increasing, positive `requiredContribution` values. They are cumulative: reaching 50 contribution unlocks both the 10 and 50 milestones. Each milestone is claimed once, either individually or through **Claim all available**, and remains claimable until `claimEndsAtUtc`. Milestone rewards do not depend on the server completing the global objective.

## Making an event go live

1. Choose a new event ID and author the JSON with `enabled: false`.
2. Use UTC timestamps. Leave enough time between deployment and `startsAtUtc` for all instances to load the definition.
3. Validate item IDs, area IDs, objective filters, dates, and the minimum contribution in a local or staging build.
4. Estimate the target from production activity. For example, expected participants multiplied by typical daily qualifying actions multiplied by event days, then apply the desired completion rate.
5. Change `enabled` to `true` in a reviewed release change.
6. Deploy the API/worker application normally. No manual database insert is required. The lifecycle service materializes the instance from content.
7. Confirm the event appears as Upcoming or Active in `GET /api/v1/EventQuest` and in the Event tab of the quest journal.
8. Watch objective progress, outbox failures, and the ratio of unique contributors to claims during the event.

Changing ordinary display text before the event starts is safe when the version is incremented. Do not change objective keys, targets, community rewards, milestone keys, milestone thresholds, milestone rewards, or schedule after contributions begin. For a material correction, disable the definition and publish a replacement with a new event ID.

## Lifecycle and rewards

| State | Meaning |
| --- | --- |
| Upcoming | Enabled and before `startsAtUtc`; no contribution is accepted. |
| Active | Inside the contribution window and not yet globally complete. |
| Completed | Every global objective reached its target. Personal contribution continues until `endsAtUtc`; rewards remain claimable until `claimEndsAtUtc`. |
| Ended | The contribution window elapsed before global completion. |
| Expired | The claim deadline elapsed. |

An event that completes early stops increasing its capped global totals, but qualifying actions continue increasing personal contribution until the scheduled end. A character can claim the community reward once when the event is completed, the claim window is open, and their total contribution meets the minimum. Every personal milestone has its own unique claim record, so retries cannot duplicate milestone rewards.

Reward endpoints:

- `POST /api/v1/EventQuest/{eventQuestId}/claim` claims the community-completion reward.
- `POST /api/v1/EventQuest/{eventQuestId}/milestones/{milestoneKey}/claim` claims one unlocked milestone.
- `POST /api/v1/EventQuest/{eventQuestId}/milestones/claim-all` claims every unlocked, unclaimed milestone.

## Operational changes

- **Delay before start:** update the schedule, increment the definition version, and deploy before the original start.
- **Cancel before start:** set `enabled` to `false` and deploy.
- **Cancel after start:** set `enabled` to `false`, increment the version, deploy, and communicate the cancellation. Existing rows remain as an audit trail.
- **Correct a live event:** prefer a replacement event ID. Editing targets or objective keys after contribution has started is unsupported.
- **Repeat an event:** copy the definition, use a new ID containing the occurrence, and set new dates.

## Deployment implications

The `AddServerEventQuests` and `AddEventQuestPersonalMilestones` EF Core migrations must be applied before deploying code that enables an event with milestones. Event JSON is application content and therefore goes live through the same reviewed build/deploy process as other game definitions. No infrastructure repository changes are required.
