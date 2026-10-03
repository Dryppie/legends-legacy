import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injectable,
  Injector,
  OnDestroy,
  afterNextRender,
  inject,
  signal,
} from '@angular/core';
import { Observable, ReplaySubject } from 'rxjs';
import { Overlay, OverlayRef, PositionStrategy } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { LgAnnouncer } from '../../core/grimoire-announcer';
import { lgSentences } from '../tooltip/tooltip.directive';
import { LgToastComponent, LgToastTone } from './toast.component';

/** At most three show at once; the rest wait their turn (Foundations · Surfaces & Layering). */
const SHOWN_AT_ONCE = 3;
/** Each stays at least six seconds. */
const LEAST = 6000;
/** How long a toast takes to fade out (duration-fast) before it leaves the stack. */
const FADE = 160;

/** Why a toast went: its time ran out, the player dismissed it, or the player took its action. */
export type LgToastEnd = 'timeout' | 'dismissed' | 'action';

/** What a toast says and offers. */
export interface LgToastOptions {
  /** What happened, in the words of Standards · States: "Saved", "Claimed 120 Cinders", "Couldn't save. Your change was undone." */
  heading: string;
  /** A detail, muted. */
  text?: string;
  /** Its start bar: info (the default), success, warning or danger. A danger toast is announced at once (assertive). */
  tone?: LgToastTone;
  /** One action's label ("Undo", "View"); `closed` emits 'action' when it is pressed. Never the only way to do it. */
  action?: string;
  /** How long it stays, in ms: at least 6000, the default. null keeps it until it is dismissed. */
  duration?: number | null;
}

/** A toast the toaster showed, or is waiting to show. */
export class LgToastRef {
  private readonly ended = new ReplaySubject<LgToastEnd>(1);
  /** Emits once, why it went, and completes. */
  readonly closed: Observable<LgToastEnd> = this.ended.asObservable();

  constructor(private readonly close: (why: LgToastEnd) => void) {}

  /** Takes it away now (or out of the queue, if it is still waiting). */
  dismiss(): void {
    this.close('dismissed');
  }

  /** @internal */
  _end(why: LgToastEnd): void {
    this.ended.next(why);
    this.ended.complete();
  }
}

/** A toast in the stack, as the stack draws it. */
export interface LgToastView {
  readonly id: number;
  readonly heading: string;
  readonly text?: string;
  readonly tone: LgToastTone;
  readonly action?: string;
  readonly leaving: boolean;
}

interface Entry {
  view: LgToastView;
  ref: LgToastRef;
  /** ms left to stay, or null to stay until dismissed. */
  left: number | null;
  started: number;
  timer?: ReturnType<typeof setTimeout>;
  /** Whether the pointer or focus is in it. */
  held: { pointer: boolean; focus: boolean };
  /** Where focus came from when it entered the toast, to go back to if the toast goes while it holds focus. */
  cameFrom: HTMLElement | null;
}

/**
 * The game's toasts (D-145): brief outcomes on the `z-toast` layer, at the top centre of the stage, where
 * `lg-toast-outlet` marks it (the GameShell places one), or of the window. At most three show, newest on top; the
 * rest wait. Each stays at least six seconds, longer while hovered or focused, and can be dismissed. A toast never takes
 * focus: it is announced through `LgAnnouncer` (Foundations · Surfaces & Layering, Foundations · Accessibility).
 *
 *   this.toaster.show({ heading: 'Listed for 1,200 Cinders', tone: 'success', action: 'Undo' })
 *     .closed.subscribe((why) => why === 'action' && this.withdraw());
 */
@Injectable({ providedIn: 'root' })
export class LgToaster implements OnDestroy {
  private readonly overlay = inject(Overlay);
  private readonly injector = inject(Injector);
  private readonly announcer = inject(LgAnnouncer);

  /** The toasts showing, newest first. */
  readonly toasts = signal<readonly LgToastView[]>([]);
  private readonly shown = new Map<number, Entry>();
  private readonly waiting: Entry[] = [];
  private readonly anchors: HTMLElement[] = [];
  private ref: OverlayRef | null = null;
  private next = 1;

  show(options: LgToastOptions): LgToastRef {
    const id = this.next++;
    const ref = new LgToastRef((why) => this.close(id, why));
    const duration = options.duration === null ? null : Math.max(LEAST, options.duration ?? LEAST);
    const entry: Entry = {
      view: {
        id,
        heading: options.heading,
        text: options.text || undefined,
        tone: options.tone || 'info',
        action: options.action || undefined,
        leaving: false,
      },
      ref,
      left: duration,
      started: 0,
      held: { pointer: false, focus: false },
      cameFrom: null,
    };
    if (this.present() < SHOWN_AT_ONCE) this.enter(entry);
    else this.waiting.push(entry);
    return ref;
  }

  /** Takes every toast away, the waiting ones too. */
  dismissAll(): void {
    [...this.waiting, ...this.shown.values()].forEach((e) => this.close(e.view.id, 'dismissed'));
  }

  ngOnDestroy(): void {
    this.shown.forEach((e) => clearTimeout(e.timer));
    this.ref?.dispose();
  }

  /** @internal The pointer or focus came into a toast, or left it: it waits while either is in it. */
  _hold(id: number, by: 'pointer' | 'focus', held: boolean, from?: EventTarget | null): void {
    const e = this.shown.get(id);
    if (!e || e.view.leaving) return;
    const was = e.held.pointer || e.held.focus;
    e.held[by] = held;
    if (by === 'focus' && held && from instanceof HTMLElement && !from.closest('.lg-toast-stack')) e.cameFrom = from;
    const is = e.held.pointer || e.held.focus;
    if (is && !was) this.pause(e);
    else if (!is && was) this.run(e);
  }

  /** @internal Escape while focus is in a toast dismisses it; nothing else does (The modal stack). */
  _close(id: number, why: LgToastEnd): void {
    this.close(id, why);
  }

  /** @internal An `lg-toast-outlet` marks where the toasts show; the latest one placed wins. */
  _anchor(el: HTMLElement, placed: boolean): void {
    const at = this.anchors.indexOf(el);
    if (at >= 0) this.anchors.splice(at, 1);
    if (placed) this.anchors.push(el);
    if (this.ref) this.ref.updatePositionStrategy(this.position());
  }

  private present(): number {
    let n = 0;
    this.shown.forEach((e) => !e.view.leaving && n++);
    return n;
  }

  private enter(e: Entry): void {
    this.ensure();
    this.shown.set(e.view.id, e);
    this.toasts.update((list) => [e.view, ...list]);
    this.announcer.announce(lgSentences([e.view.heading, e.view.text]), {
      key: 'toast',
      assertive: e.view.tone === 'danger',
    });
    this.run(e);
  }

  private run(e: Entry): void {
    if (e.left == null) return;
    clearTimeout(e.timer);
    e.started = Date.now();
    e.timer = setTimeout(() => this.close(e.view.id, 'timeout'), e.left);
  }

  private pause(e: Entry): void {
    if (e.left == null) return;
    clearTimeout(e.timer);
    e.left = Math.max(0, e.left - (Date.now() - e.started));
  }

  private close(id: number, why: LgToastEnd): void {
    const queued = this.waiting.findIndex((e) => e.view.id === id);
    if (queued >= 0) {
      this.waiting.splice(queued, 1)[0].ref._end(why);
      return;
    }
    const e = this.shown.get(id);
    if (!e || e.view.leaving) return;
    clearTimeout(e.timer);
    const el = this.ref?.overlayElement.querySelector(`[data-toast="${id}"]`);
    const hadFocus = !!el && el.contains(el.ownerDocument.activeElement);
    e.view = { ...e.view, leaving: true };
    this.toasts.update((list) => list.map((v) => (v.id === id ? e.view : v)));
    e.ref._end(why);
    // Focus never stays on a toast that is going: back to where it came from, as the player left it.
    if (hadFocus) {
      if (e.cameFrom?.isConnected) e.cameFrom.focus();
      else (el!.ownerDocument.activeElement as HTMLElement | null)?.blur();
    }
    setTimeout(() => {
      this.shown.delete(id);
      this.toasts.update((list) => list.filter((v) => v.id !== id));
      const waiting = this.waiting.shift();
      if (waiting) this.enter(waiting);
    }, FADE);
  }

  private ensure(): void {
    if (this.ref) return;
    const ref = this.overlay.create({
      panelClass: ['lg-root', 'lg-toast-pane'],
      positionStrategy: this.position(),
      scrollStrategy: this.overlay.scrollStrategies.noop(),
    });
    // The layer: above every page layer and dialog (overlay.css).
    ref.hostElement.classList.add('lg-toast-host');
    ref.attach(new ComponentPortal(LgToastStackComponent, null, this.injector));
    this.ref = ref;
  }

  private position(): PositionStrategy {
    const anchor = [...this.anchors].reverse().find((a) => a.isConnected);
    const motion = anchor?.closest('[data-motion="reduced"]');
    if (this.ref) {
      if (motion) this.ref.overlayElement.setAttribute('data-motion', 'reduced');
      else this.ref.overlayElement.removeAttribute('data-motion');
    }
    if (!anchor) return this.overlay.position().global().top('1.5rem').centerHorizontally();
    return this.overlay
      .position()
      .flexibleConnectedTo(anchor)
      .withPositions([{ originX: 'center', originY: 'top', overlayX: 'center', overlayY: 'top', offsetY: 12 }])
      .withFlexibleDimensions(false)
      .withPush(true)
      .withViewportMargin(8);
  }
}

/** The toasts on the overlay, newest on top. The toaster creates it; you place an `lg-toast-outlet`, or nothing. */
@Component({
  selector: 'lg-toast-stack',
  imports: [LgToastComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-toast-stack' },
  template: `
    @for (t of toaster.toasts(); track t.id) {
      <lg-toast
        [attr.data-toast]="t.id"
        [class.is-leaving]="t.leaving"
        [heading]="t.heading"
        [text]="t.text"
        [tone]="t.tone"
        [action]="t.action"
        (acted)="toaster._close(t.id, 'action')"
        (dismissed)="toaster._close(t.id, 'dismissed')"
        (mouseenter)="toaster._hold(t.id, 'pointer', true)"
        (mouseleave)="toaster._hold(t.id, 'pointer', false)"
        (focusin)="toaster._hold(t.id, 'focus', true, $event.relatedTarget)"
        (focusout)="left(t.id, $event)"
        (keydown.escape)="escape(t.id, $event)"
      />
    }
  `,
  styleUrl: './toast-stack.component.css',
})
export class LgToastStackComponent {
  protected readonly toaster = inject(LgToaster);

  protected left(id: number, event: FocusEvent): void {
    const into = event.relatedTarget as Node | null;
    if (into && (event.currentTarget as HTMLElement).contains(into)) return;
    this.toaster._hold(id, 'focus', false);
  }

  protected escape(id: number, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.toaster._close(id, 'dismissed');
  }
}

/**
 * Marks where the toasts show: at the top centre of this element. The GameShell places one under its TopBar, so a
 * screen in the shell needs none; a page without the shell places one at the top of its main region. Without one, they
 * show at the top centre of the window.
 */
@Component({
  selector: 'lg-toast-outlet',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-toast-outlet', 'aria-hidden': 'true' },
  template: '',
  styles: ':host { display: block; height: 0; pointer-events: none; }',
})
export class LgToastOutletComponent implements OnDestroy {
  private readonly toaster = inject(LgToaster);
  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;

  constructor() {
    afterNextRender(() => this.toaster._anchor(this.el, true));
  }

  ngOnDestroy(): void {
    this.toaster._anchor(this.el, false);
  }
}
