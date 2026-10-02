/*
 * The ornament budget's runtime warnings (Foundations · Ornament): one ornamented framed surface per screen, two
 * ornament rules, and no ornament in the forbidden zones.
 */

export const LG_ORNAMENT_BUDGET = { framed: 1, rules: 2, halos: 0, loops: 0 } as const;

const ZONES: readonly (readonly [string, string])[] = [
  ['table', 'a table'],
  ['[role="menu"], [role="menubar"]', 'a menu'],
  ['[role="status"], .lg-toast', 'a toast'],
  ['input, select, textarea', 'an input'],
  ['ul, ol, [role="list"], [role="listbox"], [role="log"], .lg-list, .lg-entrylist', 'a list'],
  [
    '[role="dialog"]:not([data-commitment="major"]), [role="alertdialog"]:not([data-commitment="major"])',
    'a dialog that is not a major commitment',
  ],
];

/** The forbidden zone an ornament sits in, or null. */
export function lgForbiddenZone(el: Element | null): string | null {
  if (!el) return null;
  for (const [sel, name] of ZONES) if (el.parentElement && el.parentElement.closest(sel)) return name;
  const panel = el.closest('.lg-panel');
  if (panel && (panel.matches('[data-density="compact"], .lg-panel--flush') || panel.querySelector('.lg-list, table, .lg-ledger')))
    return 'a dense Panel';
  return null;
}

/** Warns when a Folio and a Banner meet on one screen. */
export function lgCheckFramed(el: Element | null): void {
  const shell = el?.closest('.lg-shell');
  if (!shell) return;
  const framed = shell.querySelectorAll('.lg-folio, .lg-banner');
  if (framed.length > LG_ORNAMENT_BUDGET.framed && framed[framed.length - 1] === el)
    console.warn('LL: a Folio and a Banner on one screen. One ornamented framed surface per screen (Foundations · Ornament).');
}

const SURFACE_SEL =
  '.lg-folio, .lg-panel, .lg-banner, .lg-journey, .lg-page, [role="dialog"], .lg-level-1, .lg-level-2, .lg-level-3, .lg-level-1--float, .lg-level-2--float';

/** The ornament rule's checks: one per surface, none in a forbidden zone, two per screen. */
export function lgCheckOrnamentRule(el: Element | null): void {
  if (!el) return;
  const surf = el.closest(SURFACE_SEL) || el.ownerDocument.body;
  const mine = Array.prototype.filter.call(
    surf.querySelectorAll('.lg-rule--ornament'),
    (o: Element) => (o.closest(SURFACE_SEL) || o.ownerDocument.body) === surf,
  ) as Element[];
  if (mine.length > 1 && mine.indexOf(el) > 0)
    console.warn('lg-section-rule: one ornament per surface. Use a hairline or a band for the other groups (Foundations · Lines).');
  const zone = lgForbiddenZone(el);
  if (zone) console.warn(`lg-section-rule: no ornament in ${zone} (Foundations · Ornament · Forbidden zones).`);
  const shell = el.closest('.lg-shell');
  if (shell) {
    const all = shell.querySelectorAll('.lg-rule--ornament');
    if (all.length > LG_ORNAMENT_BUDGET.rules && all[all.length - 1] === el)
      console.warn(
        `lg-section-rule: ${all.length} ornament rules on one screen; the budget is ${LG_ORNAMENT_BUDGET.rules} (Foundations · Ornament).`,
      );
  }
}
