import { readFileSync, writeFileSync } from 'fs';
import path from 'path';

const source = path.resolve(__dirname, '../../../API/API.LL/Data/recipes.json');
const target = path.resolve(__dirname, '../src/app/data/game-content.ts');

const json = readFileSync(source, 'utf-8').trim();
writeFileSync(
  target,
  `/* AUTO-GENERATED — DO NOT EDIT */
export const GAME_CONTENT = ${json} as const;
`,
);
console.log('Static game-content.ts regenerated');
