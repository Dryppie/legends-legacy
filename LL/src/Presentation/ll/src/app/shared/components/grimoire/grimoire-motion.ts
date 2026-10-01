/*
 * Motion (Foundations · Motion): durations and easings, reduced motion, live values and live lists. Mirrors
 * LL.motion in design-system/components/bundle.js; keep the two in step. The catalog-only audits
 * (LL.motion.audit, LL.ornament.audit) are not ported: they check previews, not the game.
 */
import {
  DestroyRef,
  Directive,
  ElementRef,
  Signal,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';

/** The motion tokens, mirrored for script. lgMotionMs reads the live token when tokens.css is loaded. */
export const LG_MOTION = {
  duration: { instant: 0, fast: 140, base: 220, slow: 400, reveal: 600 },
  easing: {
    standard: 'cubic-bezier(0.4, 0, 0.2, 1)',
    enter: 'cubic-bezier(0, 0, 0.2, 1)',
    exit: 'cubic-bezier(0.4, 0, 1, 1)',
  },
} as const;

export type LgMotionDuration = keyof typeof LG_MOTION.duration;

/** The only loops allowed: an indeterminate progress indicator and live combat playback. */
export const LG_LOOPS_ALLOWED = '[role="progressbar"]:not([aria-valuenow]), [data-motion="playback"]';

export function lgMotionMs(name: LgMotionDuration): number {
  try {
    const v = getComputedStyle(document.documentElement).getPropertyValue('--duration-' + name).trim();
    const m = /^([\d.]+)(ms|s)$/.exec(v);
    if (m) return +m[1] * (m[2] === 's' ? 1000 : 1);
  } catch {
    /* no tokens.css: use the mirror */
  }
  return LG_MOTION.duration[name];
}

/** Reduced motion: the operating system's setting, or data-motion="reduced" on an ancestor or the root. */
export function lgReducedMotion(el?: Element | null): boolean {
  if (typeof window === 'undefined') return true;
  if (window.matchMedia?.('(prefers-reduced-motion: reduce)').matches) return true;
  const t = el && el.nodeType === 1 ? el : document.documentElement;
  return !!t.closest('[data-motion="reduced"]');
}

function bezier(x1: number, y1: number, x2: number, y2: number): (x: number) => number {
  const at = (a: number, b: number, t: number) => 3 * a * t * (1 - t) * (1 - t) + 3 * b * t * t * (1 - t) + t * t * t;
  return (x) => {
    if (x <= 0) return 0;
    if (x >= 1) return 1;
    let lo = 0;
    let hi = 1;
    let t = x;
    for (let i = 0; i < 24; i++) {
      t = (lo + hi) / 2;
      if (at(x1, x2, t) < x) lo = t;
      else hi = t;
    }
    return at(y1, y2, t);
  };
}
const EASE_STANDARD = bezier(0.4, 0, 0.2, 1);
function decimals(n: number): number {
  const s = String(n);
  const i = s.indexOf('.');
  return i < 0 ? 0 : s.length - i - 1;
}

export interface LgLiveOptions {
  /** 'player': a change the player caused counts to the new value. Anything else shows at once and is marked. */
  cause?: 'player' | 'world';
  /** false: no mark for a change that is not the player's. */
  mark?: boolean;
  reduced?: boolean;
}

export interface LgLiveValue<T> {
  value: Signal<T>;
  changed: Signal<boolean>;
  /** 'lg-live', plus 'is-changed' while the mark shows. */
  className: Signal<string>;
}

/**
 * One live value, shown by the rules of Foundations · Motion: a change the player caused counts to the new value over
 * duration-slow; any other change shows at once and is marked (lg-live is-changed) for duration-reveal, then the mark
 * fades. Never both. Under reduced motion nothing counts; the mark still shows and goes at once.
 * Call it in an injection context (a field initializer).
 */
export function lgLive<T>(source: () => T, options: () => LgLiveOptions = () => ({})): LgLiveValue<T> {
  const host = inject(ElementRef, { optional: true })?.nativeElement as Element | undefined;
  const shown = signal<T>(untracked(source));
  const changed = signal(false);
  // Inputs are not set yet when this runs in a field initializer: the first value the effect sees is the start.
  let started = false;
  let prev: T;
  let raf = 0;
  let timer: ReturnType<typeof setTimeout> | undefined;

  effect(() => {
    const value = source();
    if (!started) {
      started = true;
      prev = value;
      shown.set(value);
      return;
    }
    if (Object.is(value, prev)) return;
    prev = value;
    const o = untracked(options);
    const from = untracked(shown);
    const reduced = o.reduced ?? lgReducedMotion(host);
    const numeric =
      typeof value === 'number' && typeof from === 'number' && isFinite(value) && isFinite(from);
    cancelAnimationFrame(raf);
    if (o.cause === 'player' && !reduced && numeric) {
      const a = from as number;
      const b = value as number;
      const d = lgMotionMs('slow');
      const dec = Math.max(decimals(a), decimals(b));
      let t0: number | null = null;
      const step = (t: number) => {
        if (t0 === null) t0 = t;
        const k = Math.min(1, (t - t0) / d);
        shown.set((k >= 1 ? b : +(a + (b - a) * EASE_STANDARD(k)).toFixed(dec)) as T);
        if (k < 1) raf = requestAnimationFrame(step);
      };
      raf = requestAnimationFrame(step);
      return;
    }
    shown.set(value);
    if (o.cause !== 'player' && o.mark !== false) {
      changed.set(true);
      clearTimeout(timer);
      timer = setTimeout(() => changed.set(false), lgMotionMs('reveal'));
    }
  });
  inject(DestroyRef).onDestroy(() => {
    cancelAnimationFrame(raf);
    clearTimeout(timer);
  });
  return {
    value: shown.asReadonly(),
    changed: changed.asReadonly(),
    className: computed(() => (changed() ? 'lg-live is-changed' : 'lg-live')),
  };
}

export interface LgLiveRow<T> {
  key: string;
  item: T;
  /** The item has left the data; the row stays in place, muted, until the list is released. */
  gone: boolean;
}

/**
 * A refreshed list that never moves what the player is about to click. While the pointer is over the list or focus is
 * inside it, rows keep their order: values update in place, a row whose item has gone stays where it was (gone), and
 * new items wait, counted in `pending`, until the player leaves the list or calls release(). Keys in `keep` (the
 * selection) stay in place, gone, even after release.
 *
 * `<ul [lgLiveList]="orders()" key="id" #live="lgLiveList">@for (row of live.rows(); track row.key) {…}</ul>`
 */
@Directive({
  selector: '[lgLiveList]',
  exportAs: 'lgLiveList',
  host: {
    '(pointerenter)': 'held.set(true)',
    '(pointerleave)': 'leaveSoon()',
    '(focusin)': 'held.set(true)',
    '(focusout)': 'leaveSoon()',
  },
})
export class LgLiveListDirective<T> {
  // Not required: a template may read rows() or pending() before the element that carries the directive is bound.
  readonly items = input<readonly T[] | null | undefined>([], { alias: 'lgLiveList' });
  readonly key = input<string | ((item: T) => string)>('id');
  readonly keep = input<string | readonly string[] | null>(null);

  readonly held = signal(false);
  private readonly released = signal(0);
  private order: string[] | null = null;
  private last: Record<string, T> = {};
  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;

  private readonly state = computed(() => {
    this.released();
    const items = this.items() || [];
    const k = this.key();
    const keyOf = typeof k === 'function' ? k : (x: T) => String((x as Record<string, unknown>)[k]);
    const held = this.held();
    const byKey: Record<string, T> = {};
    items.forEach((x) => (byKey[keyOf(x)] = x));
    let rows: LgLiveRow<T>[];
    let pending = 0;
    const order = this.order;
    if (held && order) {
      rows = order.map((key) =>
        byKey[key] !== undefined ? { key, item: byKey[key], gone: false } : { key, item: this.last[key], gone: true },
      );
      items.forEach((x) => {
        if (order.indexOf(keyOf(x)) < 0) pending++;
      });
    } else {
      rows = items.map((x) => ({ key: keyOf(x), item: x, gone: false }));
      const keep = this.keep();
      ([] as (string | null)[]).concat(keep == null ? [] : (keep as string | string[])).forEach((key) => {
        if (key == null || byKey[key] !== undefined || this.last[key] === undefined || !order) return;
        const i = order.indexOf(key);
        rows.splice(i < 0 ? rows.length : Math.min(i, rows.length), 0, { key, item: this.last[key], gone: true });
      });
    }
    const last: Record<string, T> = {};
    rows.forEach((row) => (last[row.key] = row.item));
    this.order = rows.map((row) => row.key);
    this.last = last;
    return { rows, pending };
  });

  readonly rows = computed(() => this.state().rows);
  readonly pending = computed(() => this.state().pending);

  release(): void {
    this.order = null;
    this.released.update((n) => n + 1);
  }

  protected leaveSoon(): void {
    setTimeout(() => this.held.set(this.el.matches(':hover') || this.el.contains(document.activeElement)), 0);
  }
}
