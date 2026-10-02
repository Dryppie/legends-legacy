/*
 * The tip: the one small float on the page that explains the thing under the pointer or focus — a tooltip, a Ledger
 * row's explanation, a blocked control's reason (Standards · States · The reason tip). It lives on the CDK overlay
 * (D-134), so no scrolling region clips it and it stacks above whatever opened before it.
 *
 * One tip serves the page: showing it for one element moves it from any other. It shows beside its element on hover and
 * keyboard focus; a press can pin it, so touch can read it; it can be hovered without closing (WCAG 1.4.13); pressing
 * elsewhere, Escape, leaving and tabbing on close it. Escape reaches it before anything else on the page, so a drawer
 * or popover under it closes on the next Escape. The tip is for the eye: what it says is also its element's
 * description, set by whoever shows it.
 */
import {
  ApplicationRef,
  EmbeddedViewRef,
  Injectable,
  Injector,
  OnDestroy,
  TemplateRef,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { ConnectedPosition, Overlay, OverlayRef } from '@angular/cdk/overlay';
import { DomPortal } from '@angular/cdk/portal';

/** What a tip says, set as text. */
export interface LgTipContent {
  /** A heading: the control's name when its own label isn't visible (the compact rail), or what a row explains. */
  title?: string | null;
  /** The state's word ("Locked"), set in capitals above the text. */
  word?: string | null;
  /** The text; a line break starts a new line. */
  text: string;
  /** A footnote under the text. */
  meta?: string | null;
  /** 'warning' sets the text in the warning colour: a shortfall. */
  tone?: 'warning' | null;
  /** 'explanation' sets the title in the display face and the text muted: what a Ledger row means. */
  kind?: 'explanation' | null;
}

/** What a tip shows: text, or a template rendered with the given injector. */
export type LgTipBody = LgTipContent | { template: TemplateRef<unknown>; injector: Injector };

/** 'below' (the default) places the tip under its element, or above it when there is no room; 'end' beside it. */
export type LgTipPlace = 'below' | 'end';

const GAP = 6;
const BELOW: ConnectedPosition[] = [
  { originX: 'start', originY: 'bottom', overlayX: 'start', overlayY: 'top', offsetY: GAP },
  { originX: 'start', originY: 'top', overlayX: 'start', overlayY: 'bottom', offsetY: -GAP },
];
const END: ConnectedPosition[] = [
  {
    originX: 'end',
    originY: 'center',
    overlayX: 'start',
    overlayY: 'center',
    offsetX: GAP,
    panelClass: 'lg-tip-pane--end',
  },
  ...BELOW,
];
/** How long the tip waits after the pointer leaves, so it can be reached and hovered. */
const GRACE = 150;
/** How long the tip takes to fade out (duration-fast) before it leaves the overlay. */
const FADE = 160;

/**
 * The page's tip. The `lgTooltip` and `lgBlocked` directives drive it; a part needs it directly only when it is the
 * blocked control itself (Button, through `LgBlockedController`).
 */
@Injectable({ providedIn: 'root' })
export class LgTip implements OnDestroy {
  private readonly overlay = inject(Overlay);
  private readonly doc = inject(DOCUMENT);
  private readonly appRef = inject(ApplicationRef);

  private el: HTMLElement | null = null;
  private home: HTMLElement | null = null;
  private portal: DomPortal<HTMLElement> | null = null;
  private ref: OverlayRef | null = null;
  private view: EmbeddedViewRef<unknown> | null = null;
  /** Whom the tip shows for, and whether a press pinned it: signals, so a part can reflect them (a pinned Ledger row). */
  private readonly state = signal<{ owner: HTMLElement | null; pinned: boolean }>({ owner: null, pinned: false });
  private body: LgTipBody | null = null;
  private graceTimer: ReturnType<typeof setTimeout> | undefined;
  private fadeTimer: ReturnType<typeof setTimeout> | undefined;
  private readonly cleanup: (() => void)[] = [];

  /** Whether the tip is showing for `owner`. Read in a template or `computed`, it updates as the tip moves. */
  isShownFor(owner: HTMLElement): boolean {
    return this.state().owner === owner;
  }

  /** Whether a press pinned the tip open (for `owner`, when given). Reactive, like `isShownFor`. */
  isPinned(owner?: HTMLElement): boolean {
    const { owner: shownFor, pinned } = this.state();
    return !!shownFor && pinned && (!owner || shownFor === owner);
  }

  private get owner(): HTMLElement | null {
    return untracked(this.state).owner;
  }

  private get pinned(): boolean {
    return untracked(this.state).pinned;
  }

  /** Shows the tip beside `owner`, moving it from any other element. */
  show(owner: HTMLElement, body: LgTipBody, options: { pinned?: boolean; place?: LgTipPlace } = {}): void {
    const el = this.ensure();
    if (!el || !owner.isConnected) return;
    clearTimeout(this.graceTimer);
    clearTimeout(this.fadeTimer);
    const moving = this.owner !== owner || !this.ref?.hasAttached();
    if (moving || this.body !== body) this.fill(body);
    this.state.set({ owner, pinned: !!options.pinned });
    this.look(body);
    if (owner.closest('[data-motion="reduced"]')) el.setAttribute('data-motion', 'reduced');
    else el.removeAttribute('data-motion');

    const strategy = this.overlay
      .position()
      .flexibleConnectedTo(owner)
      .withPositions(options.place === 'end' ? END : BELOW)
      .withFlexibleDimensions(false)
      .withPush(true)
      .withViewportMargin(8);
    const ref = (this.ref ??= this.createRef());
    ref.updatePositionStrategy(strategy);
    if (moving) {
      // Attaching again puts the tip's pane last in the overlay container, above whatever opened since.
      if (ref.hasAttached()) ref.detach();
      ref.attach(this.portal!);
    }
    ref.updatePosition();
    if (!this.inView(owner)) {
      this.hide();
      return;
    }
    // From hidden to shown in a later style pass, so it fades in.
    void el.offsetWidth;
    el.classList.add('is-shown');
  }

  /** Changes what the tip says while it shows for `owner` (a cooldown ticking down). */
  update(owner: HTMLElement, body: LgTipBody): void {
    if (this.owner !== owner || !this.el) return;
    this.fill(body);
    this.look(body);
    this.ref?.updatePosition();
  }

  /** Hides the tip now. */
  hide(): void {
    clearTimeout(this.graceTimer);
    if (this.owner || this.pinned) this.state.set({ owner: null, pinned: false });
    if (!this.el) return;
    this.el.classList.remove('is-shown');
    clearTimeout(this.fadeTimer);
    // Off the overlay once it has faded, so it never covers the page.
    this.fadeTimer = setTimeout(() => {
      if (!this.owner && this.ref?.hasAttached()) this.ref.detach();
    }, FADE);
  }

  /** Hides the tip after a short grace, unless the pointer reaches it or something shows it again. */
  hideSoon(): void {
    clearTimeout(this.graceTimer);
    this.graceTimer = setTimeout(() => {
      if (!this.pinned) this.hide();
    }, GRACE);
  }

  ngOnDestroy(): void {
    clearTimeout(this.graceTimer);
    clearTimeout(this.fadeTimer);
    this.cleanup.forEach((fn) => fn());
    this.view?.destroy();
    this.ref?.dispose();
    this.home?.remove();
  }

  private ensure(): HTMLElement | null {
    if (this.el) return this.el;
    const doc = this.doc;
    if (!doc?.body) return null;
    // The tip waits in a hidden holder on the body while it isn't shown.
    const home = doc.createElement('div');
    home.hidden = true;
    const el = doc.createElement('div');
    el.className = 'lg-tip';
    el.setAttribute('aria-hidden', 'true');
    home.appendChild(el);
    doc.body.appendChild(home);
    this.home = home;
    this.el = el;
    this.portal = new DomPortal(el);

    const listen = <K extends keyof DocumentEventMap>(
      target: Document | Window | HTMLElement,
      type: K,
      fn: (e: DocumentEventMap[K]) => void,
      capture = false,
    ) => {
      target.addEventListener(type, fn as EventListener, capture);
      this.cleanup.push(() => target.removeEventListener(type, fn as EventListener, capture));
    };
    listen(el, 'mouseenter', () => clearTimeout(this.graceTimer));
    listen(el, 'mouseleave', () => {
      if (this.owner && !this.pinned) this.hideSoon();
    });
    // A press anywhere but the tip and its element closes it.
    listen(
      doc,
      'pointerdown',
      (e) => {
        const t = e.target as Node;
        if (this.owner && !this.owner.contains(t) && !el.contains(t)) this.hide();
      },
      true,
    );
    // Escape closes the tip before anything else on the page hears it.
    listen(
      doc,
      'keydown',
      (e) => {
        if (e.key === 'Escape' && this.owner) {
          this.hide();
          e.stopPropagation();
          e.preventDefault();
        }
      },
      true,
    );
    // Any region scrolling moves the tip with its element, and an element scrolled out of view loses its tip.
    listen(
      doc,
      'scroll',
      () => {
        if (!this.owner) return;
        if (this.inView(this.owner)) this.ref?.updatePosition();
        else this.hide();
      },
      true,
    );
    return el;
  }

  private createRef(): OverlayRef {
    const ref = this.overlay.create({
      panelClass: 'lg-tip-pane',
      scrollStrategy: this.overlay.scrollStrategies.noop(),
    });
    return ref;
  }

  private look(body: LgTipBody): void {
    const el = this.el!;
    el.classList.toggle('lg-tip--warning', 'text' in body && body.tone === 'warning');
    el.classList.toggle('lg-tip--explain', 'text' in body && body.kind === 'explanation');
  }

  private inView(owner: HTMLElement): boolean {
    if (!owner.isConnected) return false;
    const r = owner.getBoundingClientRect();
    const vh = this.doc.defaultView?.innerHeight ?? 0;
    return !(r.bottom < 0 || r.top > vh || (r.width === 0 && r.height === 0));
  }

  private fill(body: LgTipBody): void {
    const el = this.el!;
    this.body = body;
    this.view?.destroy();
    this.view = null;
    el.textContent = '';
    if ('template' in body) {
      const view = body.template.createEmbeddedView({}, body.injector);
      this.appRef.attachView(view);
      view.detectChanges();
      view.rootNodes.forEach((n) => el.appendChild(n));
      this.view = view;
      return;
    }
    const add = (cls: string, text: string | null | undefined) => {
      if (!text) return;
      const s = this.doc.createElement('span');
      s.className = cls;
      s.textContent = text;
      el.appendChild(s);
    };
    add('lg-tip__title', body.title);
    add('lg-tip__word', body.word);
    String(body.text || '')
      .split('\n')
      .forEach((line) => add('lg-tip__line', line));
    add('lg-tip__meta', body.meta);
  }
}
