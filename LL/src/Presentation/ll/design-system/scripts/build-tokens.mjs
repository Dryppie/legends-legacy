#!/usr/bin/env node
// Compiles design-system/tokens.json into design-system/tokens.css.
//
// The Claude Design artifact used to do this on every save; since the move to the
// repository (D-090) this script does it. Output is byte-identical to the artifact's.
//
// Run from the repository root (any working directory works; the paths are resolved from this file):
//   node LL/src/Presentation/ll/design-system/scripts/build-tokens.mjs           write tokens.css
//   node LL/src/Presentation/ll/design-system/scripts/build-tokens.mjs --check   exit 1 if tokens.css is out of date
//   node LL/src/Presentation/ll/design-system/scripts/build-tokens.mjs --sync    then run sync-styles.mjs, which copies the styles into the app (D-092)
//
// No dependencies; Node 18 or later.

import { readFileSync, writeFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const tokens = JSON.parse(readFileSync(join(root, 'tokens.json'), 'utf8'));

// Token families in the order tokens.css lists them. The first block is the themed one.
const THEMED = ['color', 'shadow'];
const GLOBAL = ['spacing', 'radius', 'fontSize', 'lineHeight', 'letterSpacing', 'density',
  'iconSize', 'borderWidth', 'opacity', 'layout', 'zIndex', 'motion'];

const known = new Set([...THEMED, ...GLOBAL, 'type', 'name', 'version']);
for (const key of Object.keys(tokens)) {
  if (!known.has(key)) fail(`tokens.json has a family this script does not place: "${key}". Add it to THEMED or GLOBAL.`);
}

const value = (v) => String(v).replace(/\{([\w-]+)\}/g, 'var(--$1)');
const comment = (usage) => (usage ? ` /* ${usage} */` : '');
const decl = (t) => `  --${t.name}: ${value(t.value)};${comment(t.usage)}`;
const family = (key) => (tokens[key] && tokens[key].tokens) || [];

const out = [`/* ${tokens.name} — generated from tokens.json */`];

out.push(`:root, [data-theme="${tokens.color.themes[0].id}"] {`);
for (const key of THEMED) out.push(...family(key).map(decl));
out.push('}');

out.push(':root {');
for (const key of GLOBAL) out.push(...family(key).map(decl));

const type = tokens.type;
for (const [name, stack] of Object.entries(type.families)) out.push(`  --font-${name}: ${stack};`);

// A style without its own size token (it shares one, like body-strong with body) gets a
// font shorthand token instead: --text-<style>: <weight> <size>/<line-height> var(--font-<family>).
const sizeTokens = new Set(family('fontSize').map((t) => t.name));
const styles = type.groups.flatMap((g) => g.styles.map((s) => ({ ...s, family: s.family || g.family })));
for (const s of styles) {
  if (sizeTokens.has(`text-${s.name}`)) continue;
  out.push(`  --text-${s.name}: ${s.fontWeight} ${s.fontSize}/${s.lineHeight} var(--font-${s.family});${comment(s.usage)}`);
}
out.push('}');

for (const s of styles) {
  out.push(`.${s.name} {`,
    `  font-family: var(--font-${s.family});`,
    `  font-size: ${s.fontSize};`,
    `  line-height: ${s.lineHeight};`,
    `  font-weight: ${s.fontWeight};`,
    `  letter-spacing: ${s.letterSpacing ?? 0};`);
  if (s.fontStyle) out.push(`  font-style: ${s.fontStyle};`);
  out.push('}');
}

for (const f of type.fonts) {
  out.push('@font-face {',
    `  font-family: "${f.family}";`,
    `  src: url("${f.file}") format("woff2");`,
    `  font-weight: ${f.weight};`,
    `  font-style: ${f.style};`,
    '  font-display: swap;',
    '}');
}

const css = out.join('\n') + '\n';
const target = join(root, 'tokens.css');
const args = new Set(process.argv.slice(2));

if (args.has('--check')) {
  const current = readFileSync(target, 'utf8').replace(/\r\n/g, '\n');
  if (current !== css) fail('tokens.css is out of date with tokens.json. Run: node LL/src/Presentation/ll/design-system/scripts/build-tokens.mjs');
  console.log('tokens.css matches tokens.json.');
} else {
  writeFileSync(target, css);
  console.log(`Wrote tokens.css (${css.split('\n').length - 1} lines).`);
}

if (args.has('--sync')) {
  const sync = spawnSync(process.execPath, [join(root, 'scripts', 'sync-styles.mjs')], { stdio: 'inherit' });
  if (sync.status !== 0) process.exit(1);
}

function fail(message) {
  console.error(message);
  process.exit(1);
}
