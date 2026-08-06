export const GUIDE_PAGE_IDS = {
  characterOverview: 'character-overview',
  inventory: 'inventory',
  essences: 'essences',
  achievements: 'achievements',
  soulstones: 'soulstones',
  combat: 'combat',
  world: 'world',
  dungeons: 'dungeons',
  prophecies: 'prophecies',
  crafting: 'crafting',
  guild: 'guild',
  colosseum: 'colosseum',
  tournamentReplay: 'tournament-replay',
  marketplace: 'marketplace',
  leaderboard: 'leaderboard',
  settings: 'settings',
} as const;

export type GuidePageId = (typeof GUIDE_PAGE_IDS)[keyof typeof GUIDE_PAGE_IDS];

export const ALL_GUIDE_PAGE_IDS: readonly GuidePageId[] =
  Object.values(GUIDE_PAGE_IDS);
