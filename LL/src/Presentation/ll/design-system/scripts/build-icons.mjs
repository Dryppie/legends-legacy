#!/usr/bin/env node
// Writes the icon set from icons.json, the one source of icon drawings (D-125, D-133):
//
//   design-system/icons.json → src/app/grimoire/core/grimoire-icons.ts   (LG_ICONS, LgIconName, LG_ICON_NAMES, LG_ICON_MARKERS)
//
// Never edit that file by hand: change icons.json and run this script. An entry in icons.json is
//
//   "name": { "viewBox": "0 0 24 24", "strokeWidth": 1.6, "body": "<path d=\"…\"/>", "marker": true, "note": "…" }
//
// where `body` is the shapes inside the svg, drawn in currentColor; `marker` (true or left out) marks a solid inline
// marker, the only icons drawn at 12px; and `note` (optional) becomes a comment in the generated code. The order of
// the entries is the order of `LG_ICON_NAMES`. Foundations · Iconography sets how an icon is drawn.
//
// The SVG files in assets/Icons/ are the game's own sidebar files, shown as assets; this script does not touch them.
//
// Run from the repository root (any working directory works; the paths are resolved from this file):
//   node LL/src/Presentation/ll/design-system/scripts/build-icons.mjs           write grimoire-icons.ts
//   node LL/src/Presentation/ll/design-system/scripts/build-icons.mjs --check   exit 1 if it is out of date
//
// No dependencies; Node 18 or later.

import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const ds = join(dirname(fileURLToPath(import.meta.url)), '..');
const check = process.argv.includes('--check');
const nl = (s) => s.replace(/\r\n/g, '\n');
const generated = 'Generated from design-system/icons.json by design-system/scripts/build-icons.mjs. Do not edit: change icons.json and run the script.';

// ---- icons.json ----------------------------------------------------------------------------------------------------
const icons = JSON.parse(readFileSync(join(ds, 'icons.json'), 'utf8')).icons;
if (!icons || typeof icons !== 'object' || Array.isArray(icons)) fail('icons.json: expected an "icons" object.');
const names = Object.keys(icons);
const problems = [];
for (const name of names) {
  const d = icons[name];
  if (!/^[a-z0-9]+(-[a-z0-9]+)*$/.test(name)) problems.push(`"${name}": a name is lowercase words joined by hyphens`);
  for (const key of Object.keys(d)) {
    if (!['viewBox', 'strokeWidth', 'body', 'marker', 'note'].includes(key)) problems.push(`"${name}": unknown key "${key}"`);
  }
  if (typeof d.viewBox !== 'string' || !/^-?\d+(\.\d+)? -?\d+(\.\d+)? \d+(\.\d+)? \d+(\.\d+)?$/.test(d.viewBox)) problems.push(`"${name}": viewBox is four numbers`);
  if (typeof d.strokeWidth !== 'number' || !(d.strokeWidth > 0)) problems.push(`"${name}": strokeWidth is a positive number`);
  if (typeof d.body !== 'string' || !d.body.trim()) problems.push(`"${name}": body is the shapes inside the svg`);
  else {
    if (/#[0-9a-fA-F]{3,8}\b|url\(/.test(d.body)) problems.push(`"${name}": draw in currentColor only; no colour value or gradient (Foundations · Iconography)`);
    if (/<\/?(svg|script|style)\b/i.test(d.body)) problems.push(`"${name}": body holds the shapes only; no <svg>, <script> or <style>`);
  }
  if ('marker' in d && d.marker !== true) problems.push(`"${name}": marker is true, or left out`);
  if ('note' in d && (typeof d.note !== 'string' || /\*\/|\n/.test(d.note))) problems.push(`"${name}": note is one line of text`);
}
if (!names.length) problems.push('the set is empty');
if (problems.length) fail(`icons.json:\n  ${problems.join('\n  ')}`);

// ---- the generated code ----------------------------------------------------------------------------------------------
const tsString = (s) => `'${s.replace(/\\/g, '\\\\').replace(/'/g, "\\'")}'`;

function angularFile() {
  const lines = [
    `/* ${generated} */`,
    '',
    '/**',
    ' * The Grimoire icon set (Foundations · Iconography), drawn in currentColor by `lg-icon`. The game\'s sidebar icons',
    ' * (src/assets/icons/sidebar) are in it without their baked gold gradient.',
    ' */',
    'export const LG_ICONS = {',
  ];
  for (const name of names) {
    const d = icons[name];
    if (d.note) lines.push(`  // ${d.note}`);
    lines.push(`  ${tsString(name)}: { viewBox: ${tsString(d.viewBox)}, strokeWidth: ${d.strokeWidth}, body: ${tsString(d.body)}${d.marker ? ', marker: true' : ''} },`);
  }
  const markers = names.filter((name) => icons[name].marker).map(tsString).join(', ');
  lines.push(
    '} as const;',
    '',
    'export type LgIconName = keyof typeof LG_ICONS;',
    '',
    'export const LG_ICON_NAMES = Object.keys(LG_ICONS) as LgIconName[];',
    '',
    '/** The solid inline markers: the only icons drawn at 12px. Every other icon is a line icon, 16px and up. */',
    `export const LG_ICON_MARKERS: readonly LgIconName[] = [${markers}];`,
    '',
  );
  return lines.join('\n');
}

const angularPath = join(ds, '..', 'src', 'app', 'grimoire', 'core', 'grimoire-icons.ts');
const raw = existsSync(angularPath) ? readFileSync(angularPath, 'utf8') : null;
const next = angularFile();
const current = raw !== null && nl(raw) === next;

if (check) {
  if (!current) fail('src/app/grimoire/core/grimoire-icons.ts is out of date with design-system/icons.json. Run: node LL/src/Presentation/ll/design-system/scripts/build-icons.mjs');
  console.log(`grimoire-icons.ts matches icons.json (${names.length} icons).`);
} else {
  // Keep the file's line endings.
  if (!current) writeFileSync(angularPath, raw !== null && raw.includes('\r\n') ? next.replace(/\n/g, '\r\n') : next);
  console.log(`Wrote the icon set (${names.length} icons) into src/app/grimoire/core/grimoire-icons.ts.`);
}

function fail(message) {
  console.error(message);
  process.exit(1);
}
