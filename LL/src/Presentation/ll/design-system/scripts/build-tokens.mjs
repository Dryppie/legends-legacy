#!/usr/bin/env node
// Compiles design-system/tokens.json into src/app/grimoire/tokens/:
//   tokens.css    every token as a --lg-* custom property, the font stacks and type shorthands, one .lg-type-<style>
//                 class per type style, and the @font-face rules (the first entry of styles/grimoire.css);
//   tokens.ts     the values script needs: durations, easings, breakpoints, layers;
//   fonts/        the bundled font files, copied from design-system/fonts/.
//
// Every property takes Grimoire's prefix (--lg-ground, --lg-space-4; D-128, D-131). The docs name tokens without it.
//
// Run from the repository root (any working directory works; the paths are resolved from this file):
//   node LL/src/Presentation/ll/design-system/scripts/build-tokens.mjs           write them
//   node LL/src/Presentation/ll/design-system/scripts/build-tokens.mjs --check   exit 1 if any is out of date
//
// No dependencies; Node 18 or later.

import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const tokens = JSON.parse(readFileSync(join(root, 'tokens.json'), 'utf8'));

// Token families in the order tokens.css lists them. The first block is the themed one.
const THEMED = ['color', 'shadow'];
const GLOBAL = ['spacing', 'radius', 'fontSize', 'lineHeight', 'letterSpacing', 'density',
  'iconSize', 'borderWidth', 'opacity', 'layout', 'zIndex', 'motion'];
// Families that are not custom properties: CSS can't read one inside a query.
const SCRIPT_ONLY = ['breakpoint'];

const known = new Set([...THEMED, ...GLOBAL, ...SCRIPT_ONLY, 'type', 'name', 'version']);
for (const key of Object.keys(tokens)) {
  if (!known.has(key)) fail(`tokens.json has a family this script does not place: "${key}". Add it to THEMED or GLOBAL.`);
}

const P = '--lg-';
const value = (v) => String(v).replace(/\{([\w-]+)\}/g, `var(${P}$1)`);
const comment = (usage) => (usage ? ` /* ${usage} */` : '');
const decl = (t) => `  ${P}${t.name}: ${value(t.value)};${comment(t.usage)}`;
const family = (key) => (tokens[key] && tokens[key].tokens) || [];

const out = [`/* ${tokens.name} — generated from design-system/tokens.json by design-system/scripts/build-tokens.mjs. Do not edit. */`];

out.push(`:root, [data-theme="${tokens.color.themes[0].id}"] {`);
for (const key of THEMED) out.push(...family(key).map(decl));
out.push('}');

out.push(':root {');
for (const key of GLOBAL) out.push(...family(key).map(decl));

const type = tokens.type;
for (const [name, stack] of Object.entries(type.families)) out.push(`  ${P}font-${name}: ${stack};`);

// A style without its own size token (it shares one, like body-strong with body) gets a
// font shorthand token instead: --lg-text-<style>: <weight> <size>/<line-height> var(--lg-font-<family>).
const sizeTokens = new Set(family('fontSize').map((t) => t.name));
const styles = type.groups.flatMap((g) => g.styles.map((s) => ({ ...s, family: s.family || g.family })));
for (const s of styles) {
  if (sizeTokens.has(`text-${s.name}`)) continue;
  out.push(`  ${P}text-${s.name}: ${s.fontWeight} ${s.fontSize}/${s.lineHeight} var(${P}font-${s.family});${comment(s.usage)}`);
}
out.push('}');

// One class per type style, for text that no component sets: .lg-type-body, .lg-type-label.
for (const s of styles) {
  out.push(`.lg-type-${s.name} {`,
    `  font-family: var(${P}font-${s.family});`,
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

// tokens.ts: what script needs, so no value is copied by hand into TypeScript.
const camel = (s) => s.replace(/-(\w)/g, (_, c) => c.toUpperCase());
// One line per token of `list`, keyed by its name without `prefix`, in camelCase: duration-fast → fast: 140.
const entries = (list, prefix, map) => list.map((t) => {
  if (!t.name.startsWith(prefix)) fail(`tokens.json: "${t.name}" does not start with "${prefix}".`);
  return `  ${camel(t.name.slice(prefix.length))}: ${map(t.value)},`;
});
const starting = (key, prefix) => family(key).filter((t) => t.name.startsWith(prefix));
const ms = (v) => {
  const m = /^([\d.]+)(ms|s)$/.exec(v);
  if (!m) fail(`tokens.json: "${v}" is not a duration in ms or s.`);
  return String(+m[1] * (m[2] === 's' ? 1000 : 1));
};
const str = (v) => `'${String(v).replace(/'/g, "\\'")}'`;
const num = (v) => {
  if (!/^-?\d+$/.test(String(v))) fail(`tokens.json: "${v}" is not a whole number.`);
  return String(v);
};
const ts = [
  `// ${tokens.name} — generated from design-system/tokens.json by design-system/scripts/build-tokens.mjs. Do not edit:`,
  '// change tokens.json and run the script. The same values are --lg-* custom properties in tokens.css.',
  '',
  '/** Motion durations in milliseconds (Foundations · Motion). Under reduced motion every transition ends at once. */',
  'export const LG_DURATION = {',
  ...entries(starting('motion', 'duration-'), 'duration-', ms),
  '} as const;',
  '',
  '/** Easing curves, as CSS timing functions (Foundations · Motion). */',
  'export const LG_EASING = {',
  ...entries(starting('motion', 'ease-'), 'ease-', str),
  '} as const;',
  '',
  '/**',
  ' * The shell\'s container breakpoints (Foundations · Layout · Shell breakpoints). rem, so they move with the reading-size',
  ' * setting: compare them with a container\'s width in rem, not with the window in px.',
  ' */',
  'export const LG_BREAKPOINT = {',
  ...entries(family('breakpoint'), 'breakpoint-', str),
  '} as const;',
  '',
  '/** Where a region\'s Narrow, Medium and Wide content tiers start (Foundations · Layout, D-050). */',
  'export const LG_CONTENT_TIER = {',
  ...entries(starting('layout', 'content-'), 'content-', str),
  '} as const;',
  '',
  '/** The layer stack, bottom to top (Foundations · Surfaces & Layering). A scrim sits one below the layer it serves. */',
  'export const LG_LAYER = {',
  ...entries(family('zIndex'), 'z-', num),
  '} as const;',
  '',
].join('\n');

const outDir = join(root, '..', 'src', 'app', 'grimoire', 'tokens');
const targets = [
  { file: join(outDir, 'tokens.css'), name: 'tokens.css', text: css },
  { file: join(outDir, 'tokens.ts'), name: 'tokens.ts', text: ts },
  ...readdirSync(join(root, 'fonts')).filter((f) => f.endsWith('.woff2'))
    .map((f) => ({ file: join(outDir, 'fonts', f), name: `fonts/${f}`, bytes: readFileSync(join(root, 'fonts', f)) })),
];
const same = (t) => existsSync(t.file) && (t.bytes ? readFileSync(t.file).equals(t.bytes) : readFileSync(t.file, 'utf8').replace(/\r\n/g, '\n') === t.text);
const args = new Set(process.argv.slice(2));

if (args.has('--check')) {
  const stale = targets.filter((t) => !same(t));
  if (stale.length) fail(`src/app/grimoire/tokens/ is out of date with design-system/ (${stale.map((t) => t.name).join(', ')}). Run: node LL/src/Presentation/ll/design-system/scripts/build-tokens.mjs`);
  console.log('src/app/grimoire/tokens/ matches tokens.json and fonts/.');
} else {
  mkdirSync(join(outDir, 'fonts'), { recursive: true });
  for (const t of targets) {
    if (same(t)) continue;
    writeFileSync(t.file, t.bytes ?? t.text);
    console.log(`Wrote src/app/grimoire/tokens/${t.name}.`);
  }
  console.log('src/app/grimoire/tokens/ is up to date.');
}

function fail(message) {
  console.error(message);
  process.exit(1);
}
