#!/usr/bin/env node
// Static checks for the Grimoire design system. Run it from the repository root before you finish any design-system
// change (with `npm run build:development` and `npm run grimoire:snapshots`, which check what this can't):
//
//   node LL/src/Presentation/ll/design-system/scripts/check.mjs
//
// It checks the generated code, the docs' bookkeeping, and the code in src/app/grimoire/ and the -grimoire screen
// folders: raw values, token names, query widths, import boundaries, and that every part has its README, spec and
// showcase entry (Governance · Code).
//
// Errors exit 1. Warnings are existing debt: do not add to them, and clear the ones in files you touch.
// No dependencies; Node 18 or later.

import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs';
import { basename, dirname, join, relative, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const ds = join(dirname(fileURLToPath(import.meta.url)), '..');
const app = join(ds, '..');
const grimoire = join(app, 'src', 'app', 'grimoire');
const errors = [];
const warnings = [];
const rel = (p) => relative(app, p).replace(/\\/g, '/');
const read = (p) => readFileSync(p, 'utf8');
const lineOf = (s, index) => s.slice(0, index).split('\n').length;

function walk(dir, out = []) {
  for (const name of readdirSync(dir)) {
    const p = join(dir, name);
    if (name === '.angular' || name === 'node_modules') continue;
    if (statSync(p).isDirectory()) walk(p, out);
    else out.push(p);
  }
  return out;
}
function walkDirs(dir, out = []) {
  for (const name of readdirSync(dir)) {
    const p = join(dir, name);
    if (name === 'node_modules' || !statSync(p).isDirectory()) continue;
    out.push(p);
    walkDirs(p, out);
  }
  return out;
}

// The tiers, in import order: a folder imports only the folders before it (ANGULAR_DESIGN_SYSTEM_PLAN.md, section 5).
const TIERS = ['tokens', 'styles', 'core', 'primitives', 'components', 'game', 'shell'];
const PART_TIERS = ['primitives', 'components', 'game', 'shell'];
const tierOf = (p) => relative(grimoire, p).split(sep)[0];

const grimoireFiles = walk(grimoire);
const screenDirs = walkDirs(join(app, 'src', 'app')).filter((d) => /-grimoire$/.test(d));
const screenFiles = screenDirs.flatMap((d) => readdirSync(d).map((f) => join(d, f)).filter((f) => statSync(f).isFile()));

// ---- Generated code ---------------------------------------------------------------------------------------------

// 1. src/app/grimoire/tokens/ (tokens.css, tokens.ts, fonts/) is compiled from tokens.json and fonts/ (D-131, D-132).
// 2. src/app/grimoire/core/grimoire-icons.ts is generated from icons.json (D-125).
for (const script of ['build-tokens.mjs', 'build-icons.mjs']) {
  const run = spawnSync(process.execPath, [join(ds, 'scripts', script), '--check'], { encoding: 'utf8' });
  if (run.status !== 0) errors.push((run.stderr || run.stdout).trim());
}

// ---- The design-system folder -----------------------------------------------------------------------------------

const dsText = walk(ds).filter((p) => /\.(md|json)$/.test(p));

// 3. No asset-store ids or /_blob/ urls: assets are files under assets/<Group>/.
for (const p of dsText) {
  if (/\/_blob\/|\b[0-9a-f]{32}\b/.test(read(p))) errors.push(`${rel(p)}: contains an asset-store id or /_blob/ url. Use the relative path to assets/<Group>/<file>.`);
}

// 4. design-system.json lists documentation files and assets that exist.
const index = JSON.parse(read(join(ds, 'design-system.json')));
for (const doc of [index.docs.readme, ...index.docs.sections]) {
  if (!existsSync(join(ds, doc))) errors.push(`design-system.json: docs entry ${doc} does not exist.`);
}
for (const group of Object.values(index.assetGroups || {})) {
  for (const f of Object.values(group.files || {})) {
    if (f.blob && !existsSync(join(ds, f.blob))) errors.push(`design-system.json: ${f.blob} does not exist.`);
  }
}

// 5. Decision log: one row per id, in order, six columns.
const log = read(join(ds, 'docs', '10-governance', '02-decision-log.md'));
const rows = log.split('\n').filter((l) => /^\| D-\d{3} \|/.test(l));
rows.forEach((row, i) => {
  const id = row.slice(2, 7), want = `D-${String(i + 1).padStart(3, '0')}`;
  if (id !== want) errors.push(`Decision log: expected ${want}, found ${id}.`);
  const cells = row.replace(/\\\|/g, '').split('|').length - 2;
  if (cells !== 6) errors.push(`Decision log: ${id} has ${cells} columns, not 6.`);
});

// ---- Values ---------------------------------------------------------------------------------------------------------

// 6. No hex colour, pixel font size or raw shadow outside tokens.json and the compiled tokens: not in Grimoire's code,
//    and not in a screen that has moved to Grimoire (its -grimoire folder).
function values(s) {
  const found = [];
  s.split('\n').forEach((line, i) => {
    if (/mask/.test(line)) return; // mask gradients use #000 as an alpha channel, not a colour
    const clean = line.replace(/url\([^)]*\)/g, '').replace(/data:[^"')]*/g, '');
    if (/(^|[^&\w])#[0-9a-fA-F]{3,8}\b/.test(clean)) found.push(`${i + 1}: hex colour`);
    if (/font(-size|Size)?\s*['"]?\s*:\s*['"]?[^;'"]*\b\d+(\.\d+)?px/.test(clean) || /fontSize:\s*\d/.test(clean)) found.push(`${i + 1}: pixel font size`);
    const sh = clean.match(/(box-shadow|text-shadow|boxShadow|textShadow)\s*['"]?\s*:\s*['"]?([^;'"]*)/);
    // A shadow may draw a ring or an inset edge in a token colour; a shadow with its own colour is a raw value.
    if (sh && /rgba?\(|hsla?\(|#[0-9a-fA-F]|\b(black|white)\b/.test(sh[2])) found.push(`${i + 1}: raw shadow`);
  });
  return found;
}
const styled = [
  ...grimoireFiles.filter((f) => /\.(ts|css|html)$/.test(f) && tierOf(f) !== 'tokens' && !f.endsWith('.spec.ts')),
  ...screenFiles.filter((f) => /\.(ts|css|scss|html)$/.test(f) && !f.endsWith('.spec.ts')),
];
for (const p of styled) {
  const found = values(read(p));
  if (found.length) errors.push(`${rel(p)}: ${found.length} raw value(s), line ${found.slice(0, 6).join('; line ')}${found.length > 6 ? '; …' : ''}. Use tokens.`);
}

// 7. Token properties carry the prefix (var(--lg-ground), D-131), and every width in a container or media query is a
//    breakpoint token or a content tier (a max-width query may sit 0.0625rem below one).
const tokenJson = JSON.parse(read(join(ds, 'tokens.json')));
const tokenNames = new Set(Object.values(tokenJson).flatMap((f) => (f && f.tokens) || []).map((t) => t.name));
for (const n of Object.keys(tokenJson.type.families)) tokenNames.add(`font-${n}`);
for (const g of tokenJson.type.groups) for (const st of g.styles) tokenNames.add(`text-${st.name}`);
const queryWidths = new Set(
  [...tokenJson.breakpoint.tokens, ...tokenJson.layout.tokens.filter((t) => t.name.startsWith('content-'))].map((t) => parseFloat(t.value)),
);
for (const p of styled.filter((f) => !f.endsWith('.html'))) {
  const s = read(p);
  const bare = [...new Set([...s.matchAll(/var\(--([\w-]+)/g)].map((m) => m[1]).filter((n) => tokenNames.has(n)))];
  if (bare.length) errors.push(`${rel(p)}: token properties without the --lg- prefix: ${bare.slice(0, 6).map((n) => `--${n}`).join(', ')}${bare.length > 6 ? ', …' : ''}.`);
  for (const q of s.matchAll(/@(?:container|media)\b([^{]*)\{/g)) {
    for (const w of q[1].matchAll(/(\d+(?:\.\d+)?)(rem|px|em)\b/g)) {
      const v = parseFloat(w[1]);
      const where = `${rel(p)}:${lineOf(s, q.index)}`;
      if (w[2] !== 'rem') errors.push(`${where}: a query width in ${w[2]} (${w[0]}). Breakpoints are rem tokens (tokens.json → breakpoint).`);
      else if (!queryWidths.has(v) && !queryWidths.has(+(v + 0.0625).toFixed(4))) errors.push(`${where}: ${w[0]} in a query is not a breakpoint token or content tier. Add one to tokens.json → breakpoint, or use one.`);
    }
  }
}

// ---- Boundaries ---------------------------------------------------------------------------------------------------

// 8. @grimoire imports only Angular (the CDK included), rxjs and itself; inside it, each tier imports only the tiers
//    before it, and only specs, testing/ and showcase/ may reach testing/. The rest of the app imports @grimoire and
//    @grimoire/testing, never a path inside it. Two exceptions: main.ts imports core/grimoire-styles, so the startup
//    bundle stays free of the components, and app.routes.ts lazy-loads the dev-only showcase's routes.
const IMPORT = /^[ \t]*(?:import|export)\s+(?:type\s+)?(?:[\w$]+\s*,?\s*)?(?:\{[^}]*\}|\*(?:\s+as\s+[\w$]+)?)?\s*from\s*['"]([^'"]+)['"]|^[ \t]*import\s*['"]([^'"]+)['"]|\bimport\(\s*['"]([^'"]+)['"]\s*\)/gm;
const importsOf = (s) => [...s.matchAll(IMPORT)].map((m) => ({ spec: m[1] ?? m[2] ?? m[3], index: m.index }));
const EXCEPTIONS = [
  [join(app, 'src', 'main.ts'), join(grimoire, 'core', 'grimoire-styles')],
  [join(app, 'src', 'app', 'app.routes.ts'), join(grimoire, 'showcase', 'showcase.routes')],
];
for (const p of grimoireFiles.filter((f) => f.endsWith('.ts'))) {
  const s = read(p), tier = tierOf(p), loose = tier === 'showcase' || tier === 'testing' || p.endsWith('.spec.ts');
  for (const { spec, index } of importsOf(s)) {
    const where = `${rel(p)}:${lineOf(s, index)}`;
    if (!spec.startsWith('.')) {
      const ok = spec.startsWith('@angular/') || spec === 'rxjs' || spec.startsWith('rxjs/') || (loose && (spec === '@grimoire' || spec === '@grimoire/testing'));
      if (!ok) errors.push(`${where}: imports ${spec}. Grimoire imports only Angular, the CDK, rxjs and itself.`);
      continue;
    }
    const target = resolve(dirname(p), spec);
    if (!target.startsWith(grimoire + sep)) {
      errors.push(`${where}: imports ${spec}, outside src/app/grimoire/. Grimoire never imports the app.`);
      continue;
    }
    const to = tierOf(target);
    if (to === 'testing' && !loose) errors.push(`${where}: imports testing/, which only specs, the harnesses and the showcase may use.`);
    else if (to === 'showcase' && tier !== 'showcase') errors.push(`${where}: imports the showcase.`);
    else if (!loose && TIERS.includes(tier) && TIERS.includes(to) && TIERS.indexOf(to) > TIERS.indexOf(tier)) {
      errors.push(`${where}: ${tier}/ imports ${to}/. A tier imports only the tiers before it: ${TIERS.join(' → ')}.`);
    }
  }
}
for (const p of walk(join(app, 'src')).filter((f) => f.endsWith('.ts') && !f.startsWith(grimoire + sep))) {
  const s = read(p);
  for (const { spec, index } of importsOf(s)) {
    if (!spec.startsWith('.')) continue;
    const target = resolve(dirname(p), spec);
    if (!target.startsWith(grimoire + sep)) continue;
    if (EXCEPTIONS.some(([from, to]) => p === from && target === to)) continue;
    errors.push(`${rel(p)}:${lineOf(s, index)}: imports ${spec}. Import from '@grimoire' (or '@grimoire/testing').`);
  }
}

// ---- Parts ------------------------------------------------------------------------------------------------------

// 9. Every part (a <tier>/<name>/<name>.component.ts, or <name>.directive.ts for one without markup of its own) has its
//    guidelines page beside it and is covered by a showcase entry; a part without a spec is debt (a warning).
const covered = new Set(
  walk(join(grimoire, 'showcase', 'entries'))
    .filter((f) => f.endsWith('.showcase.ts'))
    .flatMap((f) => [...read(f).matchAll(/covers:\s*\[([^\]]*)\]/g)].flatMap((m) => [...m[1].matchAll(/['"](\w+)['"]/g)].map((n) => n[1]))),
);
for (const tier of PART_TIERS) {
  for (const name of readdirSync(join(grimoire, tier))) {
    const dir = join(grimoire, tier, name);
    if (!statSync(dir).isDirectory()) continue;
    const kind = existsSync(join(dir, `${name}.component.ts`)) ? 'component' : 'directive';
    const ts = join(dir, `${name}.${kind}.ts`);
    if (!existsSync(ts)) {
      errors.push(`${rel(dir)}/: a part's folder holds ${name}.component.ts (or ${name}.directive.ts).`);
      continue;
    }
    const readme = join(dir, 'README.md');
    if (!existsSync(readme)) errors.push(`${rel(dir)}/README.md is missing: every part has its guidelines page beside it.`);
    else if (!/^# \S/.test(read(readme))) errors.push(`${rel(readme)}: starts with "# <Name>" (Governance · Templates).`);
    if (!existsSync(join(dir, `${name}.${kind}.spec.ts`))) warnings.push(`${rel(dir)}/${name}.${kind}.spec.ts is missing.`);
    for (const cls of read(ts).matchAll(/^export class (Lg\w+)/gm)) {
      if (!covered.has(cls[1])) errors.push(`${rel(ts)}: ${cls[1]} is in no showcase entry's covers (src/app/grimoire/showcase/entries/).`);
    }
  }
}

for (const w of warnings) console.log(`warning  ${w}`);
for (const e of errors) console.log(`ERROR    ${e}`);
console.log(`\n${errors.length} error(s), ${warnings.length} warning(s).`);
process.exit(errors.length ? 1 : 0);
