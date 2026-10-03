import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  Directive,
  ElementRef,
  Injector,
  TemplateRef,
  ViewContainerRef,
  afterNextRender,
  effect,
  inject,
  input,
  model,
  untracked,
  viewChild,
} from '@angular/core';
import { InteractivityChecker } from '@angular/cdk/a11y';
import { ConnectedPosition, Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import { lgUniqueId } from '../../core/grimoire-core';
import { LgTip } from '../../core/grimoire-tip';

export type LgPopoverPlace = 'below' | 'below-start' | 'end';

const GAP = 4;
const PLACES: Record<LgPopoverPlace, ConnectedPosition[]> = {
  below: [
    { originX: 'center', originY: 'bottom', overlayX: 'center', overlayY: 'top', offsetY: GAP },
    { originX: 'center', originY: 'top', overlayX: 'center', overlayY: 'bottom', offsetY: -GAP },
  ],
  'below-start': [
    { originX: 'start', originY: 'bottom', overlayX: 'start', overlayY: 'top', offsetY: GAP },
    { originX: 'start', originY: 'top', overlayX: 'start', overlayY: 'bottom', offsetY: -GAP },
  ],
  end: [
    { originX: 'end', originY: 'top', overlayX: 'start', overlayY: 'top', offsetX: GAP },
    { originX: 'start', originY: 'top', overlayX: 'end', overlayY: 'top', offsetX: -GAP },
  ],
};

/**
 * What a popover holds: `<lg-popover label="Tracked quests">…</lg-popover>`, opened by a `[lgPopoverTrigger]`. Its
 * content is drawn on the CDK overlay while it is open, in a Level 2 float, and is a non-modal dialog named by `label`.
 * The element itself takes no room where it is written.
 */
@Component({
  selector: 'lg-popover',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-popover-source' },
  template: `<ng-template #body
    ><div
      class="lg-popover"
      role="dialog"
      tabindex="-1"
      [id]="id"
      [attr.aria-label]="label() || null"
      [style.--lg-popover-width]="width() || null"
      ><ng-content /></div
  ></ng-template>`,
  styleUrl: './popover.component.css',
})
export class LgPopoverComponent {
  /** Its name, as a dialog: "Tracked quests". */
  readonly label = input<string>();
  /** Its width (any CSS length); 20rem by default, and never wider than the window allows. */
  readonly width = input<string>();
  readonly id = lgUniqueId('lgpop');
  /** @internal What the trigger draws on the overlay. */
  readonly body = viewChild.required<TemplateRef<unknown>>('body');
}

/**
 * Opens a popover beside its element on a press: `<button lgButton [lgPopoverTrigger]="tracker">Quests</button>`
 * (D-146). The element says so (`aria-haspopup`, `aria-expanded`, `aria-controls`); a press opens and closes it, and
 * focus moves into it, so Tab reaches what it holds. Escape, or Tab past either end of it, closes it and focus goes
 * back to the element; a press outside, or focus leaving it for the page, closes it where the player went. Escape reaches the tip first, then the topmost
 * overlay, so a popover opened from a dialog closes before the dialog does. `[(lgPopoverOpen)]` binds the state.
 */
@Directive({
  selector: '[lgPopoverTrigger]',
  exportAs: 'lgPopoverTrigger',
  host: {
    'aria-haspopup': 'dialog',
    '[attr.aria-expanded]': 'open()',
    '[attr.aria-controls]': 'open() ? popover().id : null',
    '(click)': 'toggle()',
  },
})
export class LgPopoverTriggerDirective {
  readonly popover = input.required<LgPopoverComponent>({ alias: 'lgPopoverTrigger' });
  readonly open = model(false, { alias: 'lgPopoverOpen' });
  /** Where it opens: `below` (centred, the default), `below-start`, or `end` (beside the element). */
  readonly place = input<LgPopoverPlace>('below', { alias: 'lgPopoverPlace' });

  private readonly overlay = inject(Overlay);
  private readonly vcr = inject(ViewContainerRef);
  private readonly injector = inject(Injector);
  private readonly tip = inject(LgTip);
  private readonly checker = inject(InteractivityChecker);
  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private ref: OverlayRef | null = null;
  /** Where focus goes when it closes: back to the element (Escape, a press on it), or nowhere (the player went on). */
  private returnFocus = true;

  constructor() {
    effect(() => {
      const open = this.open();
      untracked(() => (open ? this.attach() : this.detach()));
    });
    inject(DestroyRef).onDestroy(() => this.ref?.dispose());
  }

  toggle(): void {
    this.returnFocus = true;
    this.open.set(!this.open());
  }

  close(returnFocus = true): void {
    this.returnFocus = returnFocus;
    this.open.set(false);
  }

  private attach(): void {
    if (this.ref?.hasAttached()) return;
    this.tip.hide();
    const ref = (this.ref ??= this.create());
    ref.updatePositionStrategy(
      this.overlay
        .position()
        .flexibleConnectedTo(this.el)
        .withPositions(PLACES[this.place()] ?? PLACES.below)
        .withFlexibleDimensions(false)
        .withPush(true)
        .withViewportMargin(8),
    );
    ref.attach(new TemplatePortal(this.popover().body(), this.vcr));
    if (this.el.closest('[data-motion="reduced"]')) ref.overlayElement.setAttribute('data-motion', 'reduced');
    else ref.overlayElement.removeAttribute('data-motion');
    // Focus into it, so Tab reaches what it holds.
    afterNextRender(() => (ref.overlayElement.querySelector('.lg-popover') as HTMLElement | null)?.focus(), {
      injector: this.injector,
    });
  }

  private detach(): void {
    const ref = this.ref;
    if (!ref?.hasAttached()) return;
    const hadFocus = ref.overlayElement.contains(document.activeElement);
    ref.detach();
    if (this.returnFocus && (hadFocus || document.activeElement === document.body)) this.el.focus();
  }

  private create(): OverlayRef {
    const ref = this.overlay.create({
      panelClass: ['lg-root', 'lg-popover-pane'],
      scrollStrategy: this.overlay.scrollStrategies.reposition(),
    });
    ref.keydownEvents().subscribe((event) => {
      if (event.defaultPrevented) return;
      // Escape: the popover is the topmost layer that heard it.
      if (event.key === 'Escape') {
        event.preventDefault();
        this.close(true);
      }
      // Tab past either end of what it holds goes back to its element, from where the page goes on.
      if (event.key === 'Tab' && ref.overlayElement.contains(document.activeElement)) {
        const stops = Array.from(
          ref.overlayElement.querySelectorAll<HTMLElement>('button, [href], input, select, textarea, [tabindex]'),
        ).filter((el) => this.checker.isTabbable(el));
        const at = stops.indexOf(document.activeElement as HTMLElement);
        if ((event.shiftKey && at <= 0) || (!event.shiftKey && at === stops.length - 1)) {
          event.preventDefault();
          this.close(true);
        }
      }
    });
    // A press outside closes it where the player pressed; a press on its element is the element's own toggle.
    ref.outsidePointerEvents().subscribe((event) => {
      if (this.el.contains(event.target as Node)) return;
      this.close(false);
    });
    // Focus leaving it for somewhere else on the page closes it, and leaves focus there.
    ref.overlayElement.addEventListener('focusout', (event: FocusEvent) => {
      const to = event.relatedTarget as Node | null;
      if (!to || ref.overlayElement.contains(to) || this.el.contains(to)) return;
      this.close(false);
    });
    return ref;
  }
}

/** The Popover and its trigger, for a standalone `imports` array. */
export const LG_POPOVER = [LgPopoverComponent, LgPopoverTriggerDirective] as const;
