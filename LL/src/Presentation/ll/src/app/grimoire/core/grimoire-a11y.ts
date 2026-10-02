/*
 * Roving focus (Foundations · Accessibility · Keyboard). The announcer is `LgAnnouncer` (grimoire-announcer.ts), the
 * tip `LgTip` (grimoire-tip.ts) and blocked controls `lgBlocked` (grimoire-blocked.ts); overlays are the CDK's (D-134).
 * Composite widgets move to the CDK's FocusKeyManager as each is re-shaped (ANGULAR_DESIGN_SYSTEM_PLAN.md, phase 3).
 */

/** The next index for Arrow keys, Home and End, or null for any other key. */
export function lgMoveKey(key: string, i: number, n: number): number | null {
  if (key === 'ArrowDown' || key === 'ArrowRight') return Math.min(n - 1, i + 1);
  if (key === 'ArrowUp' || key === 'ArrowLeft') return Math.max(0, i - 1);
  if (key === 'Home') return 0;
  if (key === 'End') return n - 1;
  return null;
}
