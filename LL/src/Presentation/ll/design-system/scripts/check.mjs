#!/usr/bin/env node
// Static checks for the Grimoire design system. Run it from the repository root before you finish any design-system change:
//
//   node LL/src/Presentation/ll/design-system/scripts/check.mjs
//
// It checks the design system and the app's generated copy of its styles. The lg-* components themselves are checked
// against the React reference by the parity check (design-system/parity/README.md).
//
// Errors exit 1. Warnings are existing debt: do not add to them, and clear the ones in files you touch.
// No dependencies; Node 18 or later.

import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const ds = join(dirname(fileURLToPath(import.meta.url)), '..');
const app = join(ds, '..');
const errors = [];
const warnings = [];
const rel = (p) => relative(app, p).replace(/\\/g, '/');
const read = (p) => readFileSync(p, 'utf8');

function walk(dir, out = []) {
  for (const name of readdirSync(dir)) {
    const p = join(dir, name);
    if (name === '.angular' || name === 'node_modules') continue;
    if (statSync(p).isDirectory()) walk(p, out); else out.push(p);
  }
  return out;
}
const textFiles = walk(ds).filter((p) => /\.(md|json|html|css|js|ts)$/.test(p) && !/[\\/](lib|scripts)[\\/]/.test(p));

// 1. tokens.css is compiled from tokens.json.
const build = spawnSync(process.execPath, [join(ds, 'scripts', 'build-tokens.mjs'), '--check'], { encoding: 'utf8' });
if (build.status !== 0) errors.push((build.stderr || build.stdout).trim());

// 2. The app's copies of the styles (src/styles/grimoire/) are generated from this folder and up to date (D-092).
const sync = spawnSync(process.execPath, [join(ds, 'scripts', 'sync-styles.mjs'), '--check'], { encoding: 'utf8' });
if (sync.status !== 0) errors.push((sync.stderr || sync.stdout).trim());

// 3. No asset-store ids or /_blob/ urls: assets are files under assets/<Group>/.
for (const p of textFiles) {
  const s = read(p);
  if (/\/_blob\/|\b[0-9a-f]{32}\b/.test(s)) errors.push(`${rel(p)}: contains an asset-store id or /_blob/ url. Use the relative path to assets/<Group>/<file>.`);
}

// 4. Asset paths named in previews, design-system.json and manifest.json exist.
const assetRef = /(?:\.\.\/\.\.\/)?(assets\/[\w-]+\/[\w.-]+\.(?:svg|webp|png|jpe?g))/g;
for (const p of [...textFiles.filter((f) => f.endsWith('preview.html')), join(ds, 'design-system.json'), join(ds, 'manifest.json')]) {
  for (const m of read(p).matchAll(assetRef)) {
    if (!existsSync(join(ds, m[1]))) errors.push(`${rel(p)}: ${m[1]} does not exist.`);
  }
}

// 5. manifest.json and the component folders agree; every preview has an @dsCard height.
const manifest = JSON.parse(read(join(ds, 'manifest.json')));
const listed = new Set(manifest.components.map((c) => c.name));
const NOT_IN_MANIFEST = new Set(['Cover', 'lib']); // the cover card, and the React library folder
for (const c of manifest.components) {
  const dir = join(ds, 'components', c.name);
  if (!c.group) errors.push(`manifest.json: ${c.name} has no group.`);
  for (const f of ['README.md', 'preview.html']) if (!existsSync(join(dir, f))) errors.push(`components/${c.name}/${f} is missing.`);
  if (!existsSync(join(ds, 'api', 'components', `${c.name}.md`))) warnings.push(`api/components/${c.name}.md is missing.`);
}
for (const name of readdirSync(join(ds, 'components'))) {
  const dir = join(ds, 'components', name);
  if (!statSync(dir).isDirectory() || NOT_IN_MANIFEST.has(name)) continue;
  if (!listed.has(name)) errors.push(`components/${name}/ is not in manifest.json, so the catalog will not show it.`);
}
for (const p of textFiles.filter((f) => f.endsWith('preview.html'))) {
  const card = read(p).match(/<!--\s*@dsCard\b([\s\S]*?)-->/);
  if (!card || !/\bheight=\d+/.test(card[1])) errors.push(`${rel(p)}: needs a first-line <!-- @dsCard group="…" height=… --> comment.`);
}

// 6. design-system.json lists documentation files that exist.
const index = JSON.parse(read(join(ds, 'design-system.json')));
for (const doc of [index.docs.readme, ...index.docs.sections]) {
  if (!existsSync(join(ds, doc))) errors.push(`design-system.json: docs entry ${doc} does not exist.`);
}

// 7. Decision log: one row per id, in order, six columns.
const log = read(join(ds, 'docs', '10-governance', '02-decision-log.md'));
const rows = log.split('\n').filter((l) => /^\| D-\d{3} \|/.test(l));
rows.forEach((row, i) => {
  const id = row.slice(2, 7), want = `D-${String(i + 1).padStart(3, '0')}`;
  if (id !== want) errors.push(`Decision log: expected ${want}, found ${id}.`);
  const cells = row.replace(/\\\|/g, '').split('|').length - 2;
  if (cells !== 6) errors.push(`Decision log: ${id} has ${cells} columns, not 6.`);
});

// 8. Values: no hex colour, pixel font size or raw shadow outside tokens.json and tokens.css.
function values(p, s) {
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
const designFiles = [join(ds, 'components', 'bundle.css'), ...textFiles.filter((f) => f.endsWith('preview.html'))];
for (const p of designFiles) {
  const found = values(p, read(p));
  if (!found.length) continue;
  const msg = `${rel(p)}: ${found.length} raw value(s) — line ${found.slice(0, 6).join('; line ')}${found.length > 6 ? '; …' : ''}`;
  (p.endsWith('bundle.css') ? errors : warnings).push(msg);
}
const port = [join(app, 'src', 'styles', 'grimoire', 'components.css')];
const portDir = join(app, 'src', 'app', 'shared', 'components', 'grimoire');
if (existsSync(portDir)) port.push(...walk(portDir).filter((f) => f.endsWith('.ts')));
let portCount = 0;
for (const p of port.filter(existsSync)) portCount += values(p, read(p)).length;
if (portCount) errors.push(`lg-* port (src/styles/grimoire/components.css and the grimoire components): ${portCount} raw value(s). Use tokens.`);

for (const w of warnings) console.log(`warning  ${w}`);
for (const e of errors) console.log(`ERROR    ${e}`);
console.log(`\n${errors.length} error(s), ${warnings.length} warning(s).`);
process.exit(errors.length ? 1 : 0);
