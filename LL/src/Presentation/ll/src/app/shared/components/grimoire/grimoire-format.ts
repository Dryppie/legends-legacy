/*
 * Number, unit and duration formatting (Foundations · Numerals). Mirrors LL.format in
 * design-system/components/bundle.js; keep the two in step.
 */

export const LG_MINUS = '−';
export const LG_NDASH = '–';
export const LG_TIMES = '×';
/** Shown for an unknown or not-applicable value. */
export const LG_NONE = '—';
export const LG_NBSP = ' ';

export type LgNumeric = number | string | null | undefined;

export function lgMissing(n: unknown): boolean {
  return n == null || n === '' || (typeof n === 'number' && !isFinite(n));
}

/** 12,480 · −12 (a true minus) · — for unknown · a fixed number of decimals when given. */
export function lgFormatNumber(n: LgNumeric, digits?: number): string {
  if (lgMissing(n)) return LG_NONE;
  if (typeof n !== 'number') return String(n).replace(/^-(?=[\d.])/, LG_MINUS);
  const options =
    digits != null ? { minimumFractionDigits: digits, maximumFractionDigits: digits } : undefined;
  const s = Math.abs(n).toLocaleString('en-US', options);
  return n < 0 && /[1-9]/.test(s) ? LG_MINUS + s : s;
}

/** 12480 → "12.5k", 3200000 → "3.2M". */
export function lgFormatShort(n: LgNumeric): string {
  if (typeof n !== 'number' || lgMissing(n)) return lgFormatNumber(n);
  const a = Math.abs(n);
  const sign = n < 0 ? LG_MINUS : '';
  const trim = (x: number, whole: boolean) => x.toFixed(whole ? 0 : 1).replace(/\.0$/, '');
  if (a >= 1e9) return sign + trim(a / 1e9, a >= 1e10) + 'B';
  if (a >= 1e6) return sign + trim(a / 1e6, a >= 1e7) + 'M';
  if (a >= 1e4) return sign + trim(a / 1e3, a >= 1e5) + 'k';
  return lgFormatNumber(n);
}

/** 12–18 */
export function lgFormatRange(a: number, b: number, digits?: number): string {
  return lgFormatNumber(a, digits) + LG_NDASH + lgFormatNumber(b, digits);
}

/** ×1.5 */
export function lgFormatTimes(x: LgNumeric, digits?: number): string {
  return lgMissing(x) ? LG_NONE : LG_TIMES + lgFormatNumber(x, digits);
}

/** 3,120 / 4,150 */
export function lgFormatFraction(value: LgNumeric, max: LgNumeric): string {
  return lgFormatNumber(value) + LG_NBSP + '/' + LG_NBSP + lgFormatNumber(max);
}

/** 24.8% */
export function lgFormatPercent(x: LgNumeric, digits?: number): string {
  return lgMissing(x) ? LG_NONE : lgFormatNumber(x, digits) + '%';
}

/** A symbol unit attaches (24.8%, 12s); a word unit takes a non-breaking space (84 HP/5s). */
export function lgFormatUnit(x: LgNumeric, unit: string, digits?: number): string {
  if (lgMissing(x)) return LG_NONE;
  return lgFormatNumber(x, digits) + (/^(%|ms|[smhd])$/.test(unit) ? '' : LG_NBSP) + unit;
}

export interface LgNumberParts {
  int: string;
  frac: string;
  unit: string;
  /** A space separated the number from its unit. */
  sp: boolean;
}

/** Splits "184.6 threat/s" into sign-and-integer, fraction and unit, so a column can align on the decimal point. */
export function lgNumberParts(v: LgNumeric): LgNumberParts | null {
  if (typeof v === 'number') v = lgFormatNumber(v);
  if (typeof v !== 'string') return null;
  const t = v.trim();
  if (t === LG_NONE) return { int: LG_NONE, frac: '', unit: '', sp: false };
  // A range (18–24) or a fraction (7 / 20) stays whole in the integer column; only a unit after it is split off.
  const g =
    /^([+−-]?[\d,]+(?:\.\d+)?(?:–[\d,]+(?:\.\d+)?| \/ [\d,]+(?:\.\d+)?))([\s ]*)(.*)$/.exec(t);
  if (g) return { int: g[1].replace(/^-/, LG_MINUS), frac: '', unit: g[3] || '', sp: !!g[2] && !!g[3] };
  const m = /^([+−-]?×?[\d,]+)(\.\d+)?([\s ]*)(.*)$/.exec(t);
  if (!m) return null;
  return { int: m[1].replace(/^-/, LG_MINUS), frac: m[2] || '', unit: m[4] || '', sp: !!m[3] && !!m[4] };
}

/** A value split for display as number + smaller muted unit, or null when there is no unit. */
export function lgUnitSplit(v: LgNumeric): { number: string; unit: string } | null {
  const q = lgNumberParts(v);
  if (!q || !q.unit) return null;
  return { number: q.int + q.frac, unit: (q.sp ? LG_NBSP : '') + q.unit };
}

/** What numNode shows when there is no unit to split off. */
export function lgPlainValue(v: LgNumeric): string {
  return typeof v === 'number' || lgMissing(v) ? lgFormatNumber(v) : String(v);
}

const DURATION_UNITS: readonly (readonly [string, number, string])[] = [
  ['d', 86400, 'day'],
  ['h', 3600, 'hour'],
  ['m', 60, 'minute'],
  ['s', 1, 'second'],
];

function durationParts(seconds: number): [number, readonly [string, number, string]][] {
  let s = Math.max(0, Math.round(seconds));
  const out: [number, readonly [string, number, string]][] = [];
  for (let i = 0; i < DURATION_UNITS.length; i++) {
    const n = Math.floor(s / DURATION_UNITS[i][1]);
    if (!out.length && n === 0 && i < DURATION_UNITS.length - 1) continue;
    if (out.length && n === 0) break;
    out.push([n, DURATION_UNITS[i]]);
    s -= n * DURATION_UNITS[i][1];
    if (out.length === 2) break;
  }
  return out;
}

/** Two units at most: 4m 12s · 2h 14m · 3d 4h · 12s */
export function lgFormatDuration(seconds: LgNumeric): string {
  if (lgMissing(seconds)) return LG_NONE;
  return durationParts(Number(seconds))
    .map(([n, u]) => n + u[0])
    .join(' ');
}

/** "4 minutes 12 seconds", for screen readers. */
export function lgSpokenDuration(seconds: LgNumeric): string {
  if (lgMissing(seconds)) return 'unknown';
  return durationParts(Number(seconds))
    .map(([n, u]) => `${n} ${u[2]}${n === 1 ? '' : 's'}`)
    .join(' ');
}

/** LL.format, under the same names. */
export const LG_FORMAT = {
  number: lgFormatNumber,
  short: lgFormatShort,
  range: lgFormatRange,
  times: lgFormatTimes,
  fraction: lgFormatFraction,
  percent: lgFormatPercent,
  unit: lgFormatUnit,
  parts: lgNumberParts,
  none: LG_NONE,
  duration: lgFormatDuration,
  spokenDuration: lgSpokenDuration,
} as const;
