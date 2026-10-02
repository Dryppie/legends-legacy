import {
  DestroyRef,
  Directive,
  ElementRef,
  Injector,
  TemplateRef,
  booleanAttribute,
  computed,
  effect,
  inject,
  input,
  untracked,
} from '@angular/core';
import { AriaDescriber } from '@angular/cdk/a11y';
import { LgTip, LgTipBody, LgTipContent, LgTipPlace } from '../../core/grimoire-tip';

/** Parts read as one description, each a sentence: "Power. Raises every damage roll. From Strength". */
export function lgSentences(parts: readonly (string | null | undefined)[]): string {
  return parts
    .map((p) => (p ?? '').trim())
    .filter(Boolean)
    .reduce((all, p) => (!all ? p : /[.!?…:]$/.test(all) ? all + ' ' + p : all + '. ' + p), '');
}

/** What a tooltip says: plain words, words with a heading and a footnote, or a template. */
export type LgTooltipContent = string | LgTipContent | TemplateRef<unknown>;

/** How an element's tooltip behaves. Each is read when it is needed, so a signal keeps it current. */
export interface LgTooltipOptions {
  /** Where the tip sits; 'below' by default. */
  place?: () => LgTipPlace;
  /** Whether a press pins the tip open, and a second press closes it. */
  pin?: () => boolean;
  /** What screen readers hear, when it isn't the content's own words (a template's, or an explanation's without its heading). */
  description?: () => string | null;
}

/**
 * The tooltip's behaviour on an element: `lgTooltip` uses it, and so does a part that is its own tooltip's element
 * (a Ledger row). Create it in an injection context, and call `enter`, `leave`, `focus`, `blur` and `press` from the
 * element's own events. It keeps the element's description (aria-describedby) to the tooltip's words.
 */
export class LgTooltipController {
  private readonly tip = inject(LgTip);
  private readonly injector = inject(Injector);
  private readonly body = computed<LgTipBody | null>(() => {
    const c = this.content();
    if (!c) return null;
    if (c instanceof TemplateRef) return { template: c, injector: this.injector };
    return typeof c === 'string' ? { text: c } : c;
  });
  private readonly message = computed(() => {
    const own = this.options.description?.();
    if (own != null) return own;
    const c = this.content();
    if (!c || c instanceof TemplateRef) return '';
    if (typeof c === 'string') return c;
    return lgSentences([c.title, c.word, c.text, c.meta]);
  });

  constructor(
    private readonly el: HTMLElement,
    private readonly content: () => LgTooltipContent | null | undefined,
    private readonly options: LgTooltipOptions = {},
  ) {
    const describer = inject(AriaDescriber);
    let described = '';
    effect(() => {
      const message = this.message();
      untracked(() => {
        if (described) describer.removeDescription(this.el, described);
        if (message) describer.describe(this.el, message);
        described = message;
        const body = this.body();
        if (!this.tip.isShownFor(this.el)) return;
        if (body) this.tip.update(this.el, body);
        else this.tip.hide();
      });
    });
    inject(DestroyRef).onDestroy(() => {
      if (described) describer.removeDescription(this.el, described);
      if (untracked(() => this.tip.isShownFor(this.el))) this.tip.hide();
    });
  }

  /** Whether the tooltip shows for this element. Reactive. */
  isShown(): boolean {
    return this.tip.isShownFor(this.el);
  }

  /** Whether a press pinned it open. Reactive. */
  isPinned(): boolean {
    return this.tip.isPinned(this.el);
  }

  /** Shows the tooltip, as hover does. */
  show(): void {
    const body = this.body();
    if (body) this.tip.show(this.el, body, { place: this.place() });
  }

  /** Hides the tooltip. */
  hide(): void {
    if (untracked(() => this.tip.isShownFor(this.el))) this.tip.hide();
  }

  enter(): void {
    if (!untracked(() => this.tip.isPinned())) this.show();
  }

  leave(): void {
    if (untracked(() => this.tip.isShownFor(this.el) && !this.tip.isPinned())) this.tip.hideSoon();
  }

  focus(): void {
    if (this.el.matches(':focus-visible')) this.show();
  }

  blur(): void {
    this.hide();
  }

  press(): void {
    if (!this.options.pin?.()) return;
    const body = this.body();
    if (!body) return;
    if (untracked(() => this.tip.isPinned(this.el))) this.tip.hide();
    else this.tip.show(this.el, body, { pinned: true, place: this.place() });
  }

  private place(): LgTipPlace {
    return this.options.place?.() ?? 'below';
  }
}

/**
 * A tooltip: `<span lgTooltip="Rare: R">R</span>`. It shows beside its element on hover and keyboard focus, can be
 * hovered, and closes on leaving, tabbing on, a press elsewhere or Escape (WCAG 1.4.13). Its words are also the
 * element's description (aria-describedby), so screen readers hear them; give a template `lgTooltipDescription` for
 * that. Never the only home of something that matters: say it on the page too. `lgTooltipPin` lets a press pin it
 * open, so touch can read it. The element must be focusable to reach keyboard users.
 */
@Directive({
  selector: '[lgTooltip]',
  host: {
    '(mouseenter)': 'controller.enter()',
    '(mouseleave)': 'controller.leave()',
    '(focus)': 'controller.focus()',
    '(blur)': 'controller.blur()',
    '(click)': 'controller.press()',
  },
})
export class LgTooltipDirective {
  readonly content = input<LgTooltipContent | null | undefined>(null, { alias: 'lgTooltip' });
  readonly place = input<LgTipPlace>('below', { alias: 'lgTooltipPlace' });
  /** A press pins the tooltip open, and a second press closes it. */
  readonly pin = input(false, { alias: 'lgTooltipPin', transform: booleanAttribute });
  /** What screen readers hear when the content is a template. */
  readonly description = input<string | null>(null, { alias: 'lgTooltipDescription' });

  protected readonly controller = new LgTooltipController(
    inject<ElementRef<HTMLElement>>(ElementRef).nativeElement,
    () => this.content(),
    {
      place: () => this.place(),
      pin: () => this.pin(),
      description: () => (this.content() instanceof TemplateRef ? (this.description() ?? '') : null),
    },
  );

  /** Shows the tooltip, as hover does. */
  show(): void {
    this.controller.show();
  }

  /** Hides the tooltip. */
  hide(): void {
    this.controller.hide();
  }
}
