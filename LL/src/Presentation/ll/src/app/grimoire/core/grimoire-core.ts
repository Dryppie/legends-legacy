import { InjectionToken, Signal } from '@angular/core';

/** Joins class names, skipping empty ones: lgCx('lg-btn', blocked && 'is-blocked'). */
export function lgCx(...names: (string | false | null | undefined)[]): string {
  return names.filter(Boolean).join(' ');
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

export type LgDensity = 'comfortable' | 'standard' | 'compact';

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
  /** Opens the rail drawer (narrow screens). */
  openRail(): void;
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
