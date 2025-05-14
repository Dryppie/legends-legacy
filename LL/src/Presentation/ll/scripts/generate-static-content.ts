import { existsSync, readFileSync, writeFileSync } from 'fs';
import path from 'path';

function findRecipesJson(): string {
  const guesses = [
    // dev: backend cloned *next to* frontend
    path.resolve(__dirname, '../backend/API.LL/Data/recipes.json'),
    // dev: monorepo (old path you had)
    path.resolve(__dirname, '../../../API/API.LL/Data/recipes.json'),
    // CI: file copied into build context under /ng-app/backend/…
    path.resolve(__dirname, '../backend/API.LL/Data/recipes.json'),
  ];

  for (const p of guesses) if (existsSync(p)) return p;

  throw new Error(
    `recipes.json not found.\nChecked:\n  ${[...guesses].join('\n  ')}\n` +
      'Set RECIPES_JSON env var or copy the file into one of the above locations.',
  );
}

const source = findRecipesJson();
const target = path.resolve(__dirname, '../src/app/data/recipes-content.ts');

const json = readFileSync(source, 'utf-8').trim();

const tsLiteral = JSON.stringify(JSON.parse(json), null, 2)
  // itemType → ItemType.Foo
  .replace(/"itemType":\s*"([^"]+)"/g, (_, v) => `"itemType": ItemType.${v}`)
  // craftType → CraftType.Bar
  .replace(/"craftType":\s*"([^"]+)"/g, (_, v) => `"craftType": CraftType.${v}`)
  .replace(/"rarity":\s*"([^"]+)"/g, (_, v) => `"rarity": Rarity.${v}`);
writeFileSync(
  target,
  `/* AUTO-GENERATED — DO NOT EDIT */
import { Recipe, CraftType } from '../shared/models/profession';
import { ItemType } from '../shared/models/enums/itemType';
import { Rarity } from '../shared/models/enums/rarity';

export const RECIPES_CONTENT = ${tsLiteral} satisfies Recipe[];
`,
);
console.log('Static recipes-content.ts regenerated');
