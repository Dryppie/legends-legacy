#!/usr/bin/env node
// Copies the design system's styles into the Angular app (D-092):
//
//   design-system/tokens.css            → src/styles/grimoire/tokens.css
//   design-system/components/bundle.css → src/styles/grimoire/components.css
//   design-system/fonts/*.woff2         → src/styles/grimoire/fonts/
//
// The lg-* Angular components render the same markup and classes as the React reference components, so they share
// one stylesheet. Never edit the copies: change design-system/ and run this script.
//
// Three rules in bundle.css are for the catalog page only and are left out of the app copy, because the game's own
// stylesheet owns them and the legacy screens depend on them:
//   - the Google Fonts @import (src/index.html loads the same families);
//   - the body rule (the game's body keeps its legacy font and colours until the shell moves to Grimoire);
//   - the :root font-size rules (the game sets the root size from the reading-size setting: 14, 16 or 18px).
//
// Run from the repository root (any working directory works; the paths are resolved from this file):
//   node LL/src/Presentation/ll/design-system/scripts/sync-styles.mjs           write the app copies
//   node LL/src/Presentation/ll/design-system/scripts/sync-styles.mjs --check   exit 1 if a copy is out of date
//
// No dependencies; Node 18 or later.

import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const ds = join(dirname(fileURLToPath(import.meta.url)), '..');
const out = join(ds, '..', 'src', 'styles', 'grimoire');
const check = process.argv.includes('--check');
const nl = (s) => s.replace(/\r\n/g, '\n');

const header = (from) =>
  `/* Generated from design-system/${from} by design-system/scripts/sync-styles.mjs. Do not edit: change the design system and run the script. */\n`;

function strip(css, pattern, what) {
  const next = css.replace(pattern, '');
  if (next === css) fail(`sync-styles: could not find ${what} in components/bundle.css. Update the script's rules to match it.`);
  return next;
}

let components = nl(readFileSync(join(ds, 'components', 'bundle.css'), 'utf8'));
components = strip(components, /^@import url\([^)]*\);\n\n?/, 'the Google Fonts @import');
components = strip(components, /^body \{[^}]*\}\n/m, 'the body rule');
components = strip(components, /^:root \{ font-size: [^}]*\}\n:root\[data-reading-font-size="large"\] \{ font-size: [^}]*\}\n:root\[data-reading-font-size="extra-large"\] \{ font-size: [^}]*\}\n/m, 'the :root font-size rules');

const files = [
  ['tokens.css', header('tokens.css') + nl(readFileSync(join(ds, 'tokens.css'), 'utf8'))],
  ['components.css', header('components/bundle.css') + components],
];
const fonts = readdirSync(join(ds, 'fonts')).filter((f) => f.endsWith('.woff2'));

const stale = [];
for (const [name, text] of files) {
  const target = join(out, name);
  const current = existsSync(target) ? nl(readFileSync(target, 'utf8')) : null;
  if (current === text) continue;
  if (check) stale.push(`src/styles/grimoire/${name}`);
  else writeFileSync(target, text);
}
for (const f of fonts) {
  const target = join(out, 'fonts', f), source = readFileSync(join(ds, 'fonts', f));
  if (existsSync(target) && readFileSync(target).equals(source)) continue;
  if (check) { stale.push(`src/styles/grimoire/fonts/${f}`); continue; }
  mkdirSync(join(out, 'fonts'), { recursive: true });
  writeFileSync(target, source);
}

if (check) {
  if (stale.length) fail(`Out of date with design-system/: ${stale.join(', ')}. Run: node LL/src/Presentation/ll/design-system/scripts/sync-styles.mjs`);
  console.log('The app\'s Grimoire styles match the design system.');
} else {
  console.log('Synced tokens.css, components.css and fonts into src/styles/grimoire/.');
}

function fail(message) {
  console.error(message);
  process.exit(1);
}
