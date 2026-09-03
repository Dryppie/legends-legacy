/* AUTO-GENERATED - DO NOT EDIT.
 * Source: Core/Application/WebSockets/Contracts/StateSyncScopes.cs
 */
export const stateSyncScopes = [
  'character',
  'character-overview',
  'inventory',
  'loot-history',
  'equipment',
  'quests',
  'area-access',
  'event-quests',
  'achievements',
  'essences',
  'soulstones',
  'dungeons',
  'prophecies',
  'marketplace',
  'guild',
  'guild-buildings',
  'guild-missions',
  'guild-shop',
  'guild-membership',
  'guild-invites',
  'guild-directory',
  'colosseum',
  'tournament',
  'raid-directory',
  'world-tower',
] as const;

export type StateSyncScope = (typeof stateSyncScopes)[number];
export type StateVersionMap = Readonly<
  Partial<Record<StateSyncScope, number>>
>;

const stateSyncScopeSet: ReadonlySet<string> = new Set(stateSyncScopes);

export function isStateSyncScope(value: string): value is StateSyncScope {
  return stateSyncScopeSet.has(value);
}
