/*
 * Blocked, not disabled (Standards · States, D-087, D-134). A control the player can't use right now stays focusable
 * (aria-disabled), is described by its reason, shows the reason in the tip on hover and keyboard focus, and answers a
 * press with the reason: the tip pins, so touch can read it, and the reason is said again.
 */
import {
  DestroyRef,
  Directive,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  untracked,
} from '@angular/core';
import { LgAnnouncer } from './grimoire-announcer';
import { LgTip, LgTipContent, LgTipPlace } from './grimoire-tip';

/** Why a control is blocked, and how to say it. */
export interface LgBlockedTip {
  reason: string;
  /** "Locked": set in capitals above the reason. */
  word?: string | null;
  tone?: 'warning' | null;
  /** 'end' places the tip beside the control rather than below it. */
  place?: LgTipPlace | null;
  /** The reason is already printed in the element whose id is the description id: no tip, no description span. */
  printed?: boolean;
  /** Names the control when its own label isn't visible (the compact rail). */
  title?: string | null;
  /** What a press announces and the description reads, when it differs from the reason. */
  spoken?: string | null;
  /** false leaves aria-describedby off (the reason is already in the control's name). */
  describe?: boolean;
}

/** What screen readers hear: the word, then the reason. */
export function lgBlockedSpoken(o: LgBlockedTip): string {
  return (o.word ? o.word + '. ' : '') + (o.spoken || o.reason || '');
}

function tipOf(o: LgBlockedTip): LgTipContent {
  return { title: o.title, word: o.word, text: o.reason, tone: o.tone };
}

/**
 * The blocked behaviour for one element. A part that is itself the blocked control (Button) creates one in a field
 * initializer and forwards its host events to it; everything else uses the `lgBlocked` directive. Render the
 * description yourself, unless printed: `<span [id]="id" class="lg-sr lg-blocked__desc" aria-hidden="true">{{
 * blocked.spoken() }}</span>`.
 */
export class LgBlockedController {
  /** The aria-describedby value, or null. */
  readonly describedBy = computed(() => {
    const o = this.options();
    return o && o.describe !== false && this.id() ? this.id() : null;
  });
  /** What the description reads and a press announces. */
  readonly spoken = computed(() => {
    const o = this.options();
    return o ? lgBlockedSpoken(o) : '';
  });

  private readonly tip = inject(LgTip);
  private readonly announcer = inject(LgAnnouncer);
  private readonly content = computed(() => {
    const o = this.options();
    return o ? tipOf(o) : null;
  });

  constructor(
    private readonly el: HTMLElement,
    readonly options: () => LgBlockedTip | null | undefined,
    private readonly id: () => string,
  ) {
    // A change while the tip shows (a cooldown ticking) changes its words; no longer blocked closes it.
    effect(() => {
      const c = this.content();
      untracked(() => {
        if (!this.tip.isShownFor(this.el)) return;
        if (!c || this.options()?.printed) this.tip.hide();
        else this.tip.update(this.el, c);
      });
    });
    inject(DestroyRef).onDestroy(() => {
      if (this.tip.isShownFor(this.el)) this.tip.hide();
    });
  }

  /** A press on the blocked control: pin its reason (or close it if pinned) and say it again. */
  press(event?: Event): void {
    const o = this.options();
    if (!o) return;
    event?.preventDefault();
    if (this.tip.isPinned(this.el)) {
      this.tip.hide();
      return;
    }
    if (!o.printed) this.tip.show(this.el, this.content()!, { pinned: true, place: o.place ?? 'below' });
    this.announcer.announce(lgBlockedSpoken(o), { key: 'why' });
  }

  enter(): void {
    const o = this.options();
    if (o && !o.printed && !this.tip.isPinned()) this.tip.show(this.el, this.content()!, { place: o.place ?? 'below' });
  }

  leave(): void {
    const o = this.options();
    if (o && !o.printed && this.tip.isShownFor(this.el) && !this.tip.isPinned()) this.tip.hideSoon();
  }

  focus(): void {
    const o = this.options();
    if (o && !o.printed && this.el.matches(':focus-visible'))
      this.tip.show(this.el, this.content()!, { place: o.place ?? 'below' });
  }

  blur(): void {
    const o = this.options();
    if (o && !o.printed && this.tip.isShownFor(this.el)) this.tip.hide();
  }
}

/**
 * Makes a control blocked: `[lgBlocked]="{ reason: 'Unlocks at level 20', word: 'Locked' }"` with `[lgBlockedId]`
 * naming the element that holds the description. With a reason it sets aria-disabled and aria-describedby, shows the
 * reason tip on hover and keyboard focus, and turns a press into the reason (the part must not act on that press).
 * With null it does nothing.
 */
@Directive({
  selector: '[lgBlocked]',
  host: {
    '[attr.aria-disabled]': "options() ? 'true' : null",
    '[attr.aria-describedby]': 'blocked.describedBy()',
    '(click)': 'blocked.press($event)',
    '(mouseenter)': 'blocked.enter()',
    '(mouseleave)': 'blocked.leave()',
    '(focus)': 'blocked.focus()',
    '(blur)': 'blocked.blur()',
  },
})
export class LgBlockedDirective {
  readonly options = input<LgBlockedTip | null | undefined>(null, { alias: 'lgBlocked' });
  readonly descriptionId = input<string>('', { alias: 'lgBlockedId' });
  protected readonly blocked = new LgBlockedController(
    inject<ElementRef<HTMLElement>>(ElementRef).nativeElement,
    this.options,
    this.descriptionId,
  );
}
