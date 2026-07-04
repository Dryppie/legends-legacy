import { ARENA_COLUMNS } from './rows/arenaRow';
import { COMBAT_COLUMNS } from './rows/combatRow';
import { PROFESSION_COLUMNS } from './rows/professionRow';
import { TOTAL_LEVEL_COLUMNS } from './rows/totalLevelRow';
import { WEALTH_COLUMNS } from './rows/wealthRow';

export const COLUMNS_BY_TAB = {
  'Total level': TOTAL_LEVEL_COLUMNS,
  Combat: COMBAT_COLUMNS,
  Wealth: WEALTH_COLUMNS,
  Mining: PROFESSION_COLUMNS,
  Woodcutting: PROFESSION_COLUMNS,
  Crafting: PROFESSION_COLUMNS,
  arena: ARENA_COLUMNS,
} as const;
