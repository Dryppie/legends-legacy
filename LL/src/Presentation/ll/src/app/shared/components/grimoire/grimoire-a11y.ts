/*
 * Announcements, the reason tip, the layer stack and roving focus (Foundations · Accessibility, Foundations ·
 * Surfaces & Layering, Standards · States). Mirrors LL.announce, LL.why and LL.layers in
 * design-system/components/bundle.js; keep the two in step.
 */
import { Directive, ElementRef, computed, effect, inject, input } from '@angular/core';

/* ---------- Announcer: throttled live regions ---------- */
// lgAnnounce(text, { key, assertive }). Polite messages go out one at a time, at most every 1.5s; a message with the
// same key replaces the one still waiting (so ten loot drops become one line); the same text is not repeated within
// 5s; at most three wait. `assertive` is for errors only and interrupts.

interface AnnouncerState {
  polite: HTMLElement;
  assertive: HTMLElement;
  queue: { key: string; text: string }[];
  last: Record<string, { text: string; at: number }>;
  timer: ReturnType<typeof setTimeout> | null;
}
let announcer: AnnouncerState | null = null;

function announcerRegion(politeness: 'polite' | 'assertive'): HTMLElement {
  const el = document.createElement('div');
  el.className = 'lg-sr lg-announcer';
  el.setAttribute('aria-live', politeness);
  el.setAttribute('aria-atomic', 'true');
  document.body.appendChild(el);
  return el;
}
function announcerWrite(el: HTMLElement, text: string): void {
  el.textContent = '';
  setTimeout(() => (el.textContent = text), 60);
}
function announcerFlush(): void {
  const a = announcer!;
  const q = a.queue.shift();
  if (!q) {
    a.timer = null;
    return;
  }
  announcerWrite(a.polite, q.text);
  a.last[q.key] = { text: q.text, at: Date.now() };
  a.timer = setTimeout(announcerFlush, 1500);
}

export function lgAnnounce(text: string, options: { key?: string; assertive?: boolean } = {}): void {
  if (!text || typeof document === 'undefined') return;
  if (!announcer) {
    announcer = {
      polite: announcerRegion('polite'),
      assertive: announcerRegion('assertive'),
      queue: [],
      last: {},
      timer: null,
    };
  }
  const a = announcer;
  const key = options.key || text;
  const seen = a.last[key];
  if (seen && seen.text === text && Date.now() - seen.at < 5000) return;
  if (options.assertive) {
    announcerWrite(a.assertive, text);
    a.last[key] = { text, at: Date.now() };
    return;
  }
  let i = -1;
  for (let k = 0; k < a.queue.length; k++) if (a.queue[k].key === key) i = k;
  if (i >= 0) a.queue[i].text = text;
  else a.queue.push({ key, text });
  if (a.queue.length > 3) a.queue.shift();
  if (!a.timer) announcerFlush();
}

/* ---------- Roving focus ---------- */
/** The next index for Arrow keys, Home and End, or null for any other key. */
export function lgMoveKey(key: string, i: number, n: number): number | null {
  if (key === 'ArrowDown' || key === 'ArrowRight') return Math.min(n - 1, i + 1);
  if (key === 'ArrowUp' || key === 'ArrowLeft') return Math.max(0, i - 1);
  if (key === 'Home') return 0;
  if (key === 'End') return n - 1;
  return null;
}

/* ---------- Layer stack ---------- */
// One stack for everything Escape closes. Escape closes the topmost layer — the highest z, then the latest opened —
// and focus returns to the element that opened it. A layer can trap Tab (a dialog, a confirmation, the tour). Toasts
// are not layers. Only one modal at a time; a confirmation overlays the dialog that opened it.

export type LgLayerKind = 'popover' | 'modal' | 'confirm' | 'popover-detached' | 'tour' | 'drag';
type LgElementSource = Element | ElementRef<Element> | null | undefined;
type LgElementsSource = LgElementSource | LgElementSource[] | (() => LgElementSource | LgElementSource[]);

export interface LgLayerOptions {
  kind: LgLayerKind;
  onClose?: () => void;
  opener?: LgElementsSource;
  trap?: LgElementsSource;
  /** Limits the layer to keys pressed inside this element (an embedded widget). The game has one stack and no scope. */
  scope?: LgElementsSource;
}

export interface LgLayerHandle {
  close(restoreFocus?: boolean): void;
  isTop(): boolean;
}

const LAYER_Z: Record<LgLayerKind, number> = {
  popover: 100,
  modal: 200,
  confirm: 250,
  'popover-detached': 300,
  tour: 500,
  drag: 600,
};

interface LayerEntry {
  kind: LgLayerKind;
  z: number;
  seq: number;
  onClose?: () => void;
  trap?: LgElementsSource;
  scope?: LgElementsSource;
  opener: Element | null;
}
const layers: LayerEntry[] = [];
let layerSeq = 0;

function resolveElements(x: LgElementsSource): Element[] {
  if (typeof x === 'function') x = x();
  if (!x) return [];
  return (Array.isArray(x) ? x : [x])
    .map((e) => (e instanceof ElementRef ? e.nativeElement : e))
    .filter((e): e is Element => !!e);
}
function inScope(l: LayerEntry, node: Node | null): boolean {
  const sc = resolveElements(l.scope)[0];
  return !sc || !node || sc.contains(node);
}
function layerTop(node: Node | null): LayerEntry | null {
  let t: LayerEntry | null = null;
  layers.forEach((l) => {
    if (inScope(l, node) && (!t || l.z > t.z || (l.z === t.z && l.seq > t.seq))) t = l;
  });
  return t;
}
const FOCUSABLE =
  'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';
function focusablesIn(els: Element[]): HTMLElement[] {
  const out: HTMLElement[] = [];
  els.forEach((el) => {
    if (el.matches(FOCUSABLE)) out.push(el as HTMLElement);
    el.querySelectorAll<HTMLElement>(FOCUSABLE).forEach((f) => out.push(f));
  });
  return out.filter((f) => !f.closest('[inert]') && f.getClientRects().length > 0);
}
function onLayerKey(ev: KeyboardEvent): void {
  const t = layerTop(ev.target as Node);
  if (!t) return;
  if (ev.key === 'Escape' && !ev.defaultPrevented) {
    ev.preventDefault();
    t.onClose?.();
    return;
  }
  if (ev.key !== 'Tab' || !t.trap) return;
  const f = focusablesIn(resolveElements(t.trap));
  ev.preventDefault();
  if (!f.length) return;
  // Tab moves through the trap's focusables in order and wraps, even when they sit apart in the page.
  const i = f.indexOf(document.activeElement as HTMLElement);
  if (i < 0) {
    (ev.shiftKey ? f[f.length - 1] : f[0]).focus();
    return;
  }
  f[(i + (ev.shiftKey ? f.length - 1 : 1)) % f.length].focus();
}

/** Registers a layer. Closing it returns focus to its opener unless `restoreFocus` is false. */
export function lgOpenLayer(options: LgLayerOptions): LgLayerHandle {
  const kind = LAYER_Z[options.kind] ? options.kind : 'popover';
  const sc = resolveElements(options.scope)[0];
  if (kind === 'modal' && layers.some((l) => l.kind === 'modal' && resolveElements(l.scope)[0] === sc)) {
    console.warn("LL.layers: one modal at a time. Change the open dialog's content, or close it first.");
  }
  const entry: LayerEntry = {
    kind,
    z: LAYER_Z[kind],
    seq: ++layerSeq,
    onClose: options.onClose,
    trap: options.trap,
    scope: options.scope,
    opener: resolveElements(options.opener)[0] || document.activeElement,
  };
  layers.push(entry);
  if (layers.length === 1) document.addEventListener('keydown', onLayerKey);
  return {
    close(restore = true) {
      const i = layers.indexOf(entry);
      if (i < 0) return;
      layers.splice(i, 1);
      if (!layers.length) document.removeEventListener('keydown', onLayerKey);
      const t = entry.opener as HTMLElement | null;
      if (restore && t && t.isConnected && typeof t.focus === 'function') t.focus();
    },
    isTop() {
      return layerTop(resolveElements(entry.scope)[0] ?? null) === entry;
    },
  };
}

/** The kind of the topmost layer, or null. */
export function lgTopLayer(): LgLayerKind | null {
  return layerTop(null)?.kind ?? null;
}

/* ---------- The reason tip (Standards · States · The reason tip) ---------- */
// A blocked control stays focusable and says why. One tip serves the page, drawn on the body so no scrolling region
// clips it: it shows beside the control on hover and keyboard focus; a click or tap pins it, so touch can read it;
// pressing again, Escape, leaving or tabbing on closes it, and it can be hovered without closing (WCAG 1.4.13). The same
// words are the control's description, so screen readers hear them on focus, and a press announces them.

export interface LgWhyOptions {
  reason: string;
  /** "Locked": set in capitals above the reason. */
  word?: string | null;
  tone?: 'warning' | null;
  /** 'end' places the tip beside the control rather than below it. */
  place?: 'end' | null;
  /** The reason is already printed in the element whose id is the describedby id: no tip and no description span. */
  printed?: boolean;
  /** Names the control when its own label isn't visible (the compact rail). */
  title?: string | null;
  /** What a press announces and the description reads, when it differs from the reason. */
  spoken?: string | null;
  /** false leaves aria-describedby off (the reason is already in the control's name). */
  describe?: boolean;
}

/** What screen readers hear: the word, then the reason. */
export function lgWhySpoken(o: LgWhyOptions): string {
  return (o.word ? o.word + '. ' : '') + (o.spoken || o.reason || '');
}

interface WhyState {
  el: HTMLElement;
  owner: HTMLElement | null;
  pinned: boolean;
  timer: ReturnType<typeof setTimeout> | undefined;
  opts: LgWhyOptions | null;
}
let why: WhyState | null = null;

function whyLayer(): WhyState | null {
  if (why) return why;
  if (typeof document === 'undefined' || !document.body) return null;
  const el = document.createElement('div');
  // The control's description is what is read; the float is for the eye.
  el.className = 'lg-why';
  el.setAttribute('aria-hidden', 'true');
  document.body.appendChild(el);
  const w: WhyState = { el, owner: null, pinned: false, timer: undefined, opts: null };
  why = w;
  el.addEventListener('mouseenter', () => clearTimeout(w.timer));
  el.addEventListener('mouseleave', () => {
    if (!w.pinned) whyLater();
  });
  document.addEventListener(
    'pointerdown',
    (e) => {
      if (w.owner && !w.owner.contains(e.target as Node) && !el.contains(e.target as Node)) whyHide();
    },
    true,
  );
  // Escape closes the tip first, before anything else hears it — a drawer or dialog closes on the next Escape.
  document.addEventListener(
    'keydown',
    (e) => {
      if (e.key === 'Escape' && w.owner) {
        whyHide();
        e.stopPropagation();
        e.preventDefault();
      }
    },
    true,
  );
  window.addEventListener('scroll', () => w.owner && whyPlace(), true);
  window.addEventListener('resize', () => w.owner && whyPlace());
  return w;
}
function whyFill(o: LgWhyOptions): void {
  const w = why!;
  w.el.textContent = '';
  const add = (cls: string, text: string) => {
    const s = document.createElement('span');
    s.className = cls;
    s.textContent = text;
    w.el.appendChild(s);
  };
  if (o.title) add('lg-why__title', o.title);
  if (o.word) add('lg-why__word', o.word);
  String(o.reason || '')
    .split('\n')
    .forEach((line) => add('lg-why__reason', line));
  w.opts = o;
}
function whyPlace(): void {
  const w = why!;
  const o = w.owner;
  if (!o || !o.isConnected) {
    whyHide();
    return;
  }
  const r = o.getBoundingClientRect();
  const vw = window.innerWidth;
  const vh = window.innerHeight;
  if (r.bottom < 0 || r.top > vh || (r.width === 0 && r.height === 0)) {
    whyHide();
    return;
  }
  const tw = w.el.offsetWidth;
  const th = w.el.offsetHeight;
  const gap = 6;
  let end = w.opts?.place === 'end';
  let x: number;
  let y: number;
  if (end && r.right + gap + tw <= vw - 8) {
    x = r.right + gap;
    y = r.top + r.height / 2 - th / 2;
  } else {
    x = r.left;
    y = r.bottom + gap;
    if (y + th > vh - 8 && r.top - gap - th >= 8) y = r.top - gap - th;
    end = false;
  }
  w.el.classList.toggle('lg-why--end', !!end);
  w.el.style.left = Math.round(Math.max(8, Math.min(x, vw - tw - 8))) + 'px';
  w.el.style.top = Math.round(Math.max(8, Math.min(y, vh - th - 8))) + 'px';
}
function whyShow(owner: HTMLElement, o: LgWhyOptions, pinned: boolean): void {
  const w = whyLayer();
  if (!w) return;
  clearTimeout(w.timer);
  if (w.owner !== owner || w.opts !== o) whyFill(o);
  w.owner = owner;
  w.pinned = pinned;
  w.el.classList.toggle('lg-why--warning', o.tone === 'warning');
  w.el.classList.toggle('is-detached', !!owner.closest('[role="dialog"], [role="alertdialog"], [aria-modal="true"]'));
  if (owner.closest('[data-motion="reduced"]')) w.el.setAttribute('data-motion', 'reduced');
  else w.el.removeAttribute('data-motion');
  whyPlace();
  if (w.owner) w.el.classList.add('is-shown');
}
function whyHide(): void {
  const w = why;
  if (!w) return;
  clearTimeout(w.timer);
  w.owner = null;
  w.pinned = false;
  w.el.classList.remove('is-shown');
}
function whyLater(): void {
  const w = why;
  if (!w) return;
  clearTimeout(w.timer);
  w.timer = setTimeout(() => {
    if (!w.pinned) whyHide();
  }, 150);
}

/**
 * The reason-tip behaviour for one element. Components that are themselves the blocked control (Button) create one in
 * a field initializer and forward their host events to it; everything else uses the lgWhy directive.
 */
export class LgWhyController {
  /** The aria-describedby value, or null. */
  readonly describedBy = computed(() => {
    const o = this.options();
    return o && o.describe !== false && this.id() ? this.id() : null;
  });
  /** What the description span reads and a press announces. */
  readonly spoken = computed(() => {
    const o = this.options();
    return o ? lgWhySpoken(o) : '';
  });

  constructor(
    private readonly el: HTMLElement,
    readonly options: () => LgWhyOptions | null | undefined,
    private readonly id: () => string,
  ) {
    // A change while the tip is open (a cooldown ticking) refreshes its words. Needs an injection context.
    effect(() => {
      const o = this.options();
      const w = why;
      if (!w || w.owner !== this.el) return;
      if (!o) {
        whyHide();
        return;
      }
      if (w.opts && (w.opts.reason !== o.reason || w.opts.word !== o.word || w.opts.title !== o.title)) {
        whyFill(o);
        whyPlace();
      }
    });
  }

  /** A press on the blocked control: pin its reason (or close it if pinned) and say it again. */
  press(event?: Event): void {
    const o = this.options();
    if (!o) return;
    event?.preventDefault();
    const w = why;
    if (w && w.owner === this.el && w.pinned) {
      whyHide();
      return;
    }
    if (!o.printed) whyShow(this.el, o, true);
    lgAnnounce(lgWhySpoken(o), { key: 'why' });
  }
  enter(): void {
    const o = this.options();
    if (o && !o.printed && (!why || !why.pinned)) whyShow(this.el, o, false);
  }
  leave(): void {
    const o = this.options();
    if (o && !o.printed && why && why.owner === this.el && !why.pinned) whyLater();
  }
  focus(): void {
    const o = this.options();
    if (o && !o.printed && this.el.matches(':focus-visible')) whyShow(this.el, o, false);
  }
  blur(): void {
    const o = this.options();
    if (o && !o.printed && why && why.owner === this.el) whyHide();
  }
}

/**
 * Makes a blocked control explain itself: `[lgWhy]="{ reason: 'Unlocks at level 20', word: 'Locked' }"` with
 * `[lgWhyId]` naming the element that holds the description. With options it sets aria-disabled and
 * aria-describedby, shows the reason tip on hover and keyboard focus, and turns a press into the reason (the
 * component must not act on that press). With null it does nothing. Render the description yourself, unless printed:
 * `<span [id]="id" class="lg-sr lg-why__desc" aria-hidden="true">{{ spoken }}</span>`.
 */
@Directive({
  selector: '[lgWhy]',
  host: {
    '[attr.aria-disabled]': "options() ? 'true' : null",
    '[attr.aria-describedby]': 'why.describedBy()',
    '(click)': 'why.press($event)',
    '(mouseenter)': 'why.enter()',
    '(mouseleave)': 'why.leave()',
    '(focus)': 'why.focus()',
    '(blur)': 'why.blur()',
  },
})
export class LgWhyDirective {
  readonly options = input<LgWhyOptions | null | undefined>(null, { alias: 'lgWhy' });
  readonly whyId = input<string>('', { alias: 'lgWhyId' });
  protected readonly why = new LgWhyController(
    inject<ElementRef<HTMLElement>>(ElementRef).nativeElement,
    this.options,
    this.whyId,
  );
}
