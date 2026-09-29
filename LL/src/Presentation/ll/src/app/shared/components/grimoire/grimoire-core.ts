import { Directive, InjectionToken, Signal, input } from '@angular/core';

/**
 * Marks projected content for a named slot of a Grimoire component:
 * `<div lgSlot="aside">…</div>`. Import it next to the component so the
 * component can tell which slots are filled.
 */
@Directive({ selector: '[lgSlot]' })
export class LgSlotDirective {
  readonly lgSlot = input.required<string>();
}

export function lgHasSlot(
  slots: readonly LgSlotDirective[],
  name: string,
): boolean {
  return slots.some((slot) => slot.lgSlot() === name);
}

const NUMBER_FORMAT = new Intl.NumberFormat('en-US');

/** 12480 → "12,480". Strings pass through unchanged. */
export function lgFormatNumber(
  value: number | string | null | undefined,
): string {
  if (value === null || value === undefined) return '';
  return typeof value === 'number' ? NUMBER_FORMAT.format(value) : value;
}

/** 12480 → "12.5k", 3200000 → "3.2M". */
export function lgFormatShort(value: number): string {
  const abs = Math.abs(value);
  const trim = (n: number, whole: boolean) =>
    (whole ? n.toFixed(0) : n.toFixed(1)).replace(/\.0$/, '');
  if (abs >= 1e9) return `${trim(value / 1e9, abs >= 1e10)}B`;
  if (abs >= 1e6) return `${trim(value / 1e6, abs >= 1e7)}M`;
  if (abs >= 1e4) return `${trim(value / 1e3, abs >= 1e5)}k`;
  return NUMBER_FORMAT.format(value);
}

export type LgPoint = readonly [number, number];

export function lgPolygon(
  sides: number,
  radius: number,
  cx = 50,
  cy = 50,
  rotationDeg = -90,
): LgPoint[] {
  const points: LgPoint[] = [];
  for (let i = 0; i < sides; i++) {
    const angle = (rotationDeg * Math.PI) / 180 + (i * 2 * Math.PI) / sides;
    points.push([cx + radius * Math.cos(angle), cy + radius * Math.sin(angle)]);
  }
  return points;
}

export function lgPointsAttr(points: readonly LgPoint[]): string {
  return points.map(([x, y]) => `${x.toFixed(2)},${y.toFixed(2)}`).join(' ');
}

/** Pointy-top hexagon used by Sigils and the PageHeader icon badge. */
export const LG_HEX_OUTER = lgPointsAttr(lgPolygon(6, 47));
export const LG_HEX_INNER = lgPointsAttr(lgPolygon(6, 39));

export type LgRarity =
  | 'Common'
  | 'Uncommon'
  | 'Rare'
  | 'Epic'
  | 'Unique'
  | 'Legendary'
  | 'Legacy';

/** The game's rarity codes (see shared/models/enums/rarity.ts). */
export const LG_RARITY_CODES: Record<LgRarity, string> = {
  Common: 'C',
  Uncommon: 'UC',
  Rare: 'R',
  Epic: 'E',
  Unique: 'U',
  Legendary: 'L',
  Legacy: 'LG',
};

export type LgChatLayout = 'docked' | 'floating';

export interface LgChroniclePosition {
  /** px from the shell's left edge */
  left: number;
  /** px from the shell's bottom edge */
  bottom: number;
}

/** What a GameShell offers the Chronicle projected into it. */
export interface LgShellApi {
  readonly chatLayout: Signal<LgChatLayout>;
  setChatCollapsed(collapsed: boolean): void;
  startChronicleDrag(event: PointerEvent): void;
  nudgeChronicle(event: KeyboardEvent): void;
}

export const LG_SHELL = new InjectionToken<LgShellApi>('LG_SHELL');

let nextId = 0;
export function lgUniqueId(prefix: string): string {
  nextId += 1;
  return `${prefix}-${nextId}`;
}
