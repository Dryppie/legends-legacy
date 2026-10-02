import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  computed,
  contentChildren,
  effect,
  inject,
  input,
  model,
  untracked,
  viewChild,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LgSlotDirective, lgHasSlot, lgUniqueId } from '../../core/grimoire-core';
import { LG_NBSP, lgFormatNumber } from '../../core/grimoire-format';

/**
 * The pinned quest in the TopBar's centre (D-110): its title and current objective with the count. With a
 * `lgSlot="panel"` child — the full tracker — it is a disclosure that opens it in a Level 2 popover beneath it; Escape,
 * a click outside or the button close it, and Escape returns focus to the button. An Escape something above the tracker
 * took first (the tip, an overlay) leaves it open. `[(open)]` binds the state.
 */
@Component({
  selector: 'lg-objective',
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents', '[attr.title]': 'null' },
  template: `
    <ng-template #summary
      >@if (kicker()) {<span class="lg-objective__kicker">{{ kicker() }}</span>}<span class="lg-objective__title">{{
        title()
      }}</span
      >@if (objective()) {<span class="lg-objective__line"
          ><span class="lg-objective__text">{{ objective() }}</span
          >@if (count(); as c) {<span class="lg-objective__count">{{ c }}</span>}</span
        >}</ng-template
    >
    <div [class]="isOpen() ? 'lg-objective is-open' : 'lg-objective'">
      @if (hasPanel()) {
        <button
          #button
          type="button"
          class="lg-objective__summary"
          [attr.aria-expanded]="open()"
          [attr.aria-controls]="panelId"
          (click)="open.set(!open())"
        >
          <ng-container [ngTemplateOutlet]="summary" />
        </button>
      } @else {
        <div class="lg-objective__summary"><ng-container [ngTemplateOutlet]="summary" /></div>
      }
      @if (isOpen()) {
        <div #panel [id]="panelId" class="lg-objective__panel" role="region" [attr.aria-label]="title()">
          <ng-content select="[lgSlot=panel]" />
        </div>
      }
    </div>
  `,
})
export class LgObjectiveComponent {
  /** "Pinned quest". */
  readonly kicker = input<string>();
  readonly title = input.required<string>();
  /** The current objective's text. */
  readonly objective = input<string>();
  readonly current = input<number>();
  readonly required = input<number>();
  readonly open = model(false);

  protected readonly panelId = lgUniqueId('lgobj');
  private readonly slots = contentChildren(LgSlotDirective);
  private readonly buttonRef = viewChild<ElementRef<HTMLElement>>('button');
  private readonly panelRef = viewChild<ElementRef<HTMLElement>>('panel');
  protected readonly hasPanel = computed(() => lgHasSlot(this.slots(), 'panel'));
  protected readonly isOpen = computed(() => this.open() && this.hasPanel());
  protected readonly count = computed(() => {
    const req = this.required();
    return req ? lgFormatNumber(this.current() || 0) + LG_NBSP + '/' + LG_NBSP + lgFormatNumber(req) : null;
  });

  private listening = false;
  private byPointer = false;

  constructor() {
    const onDown = (e: PointerEvent) => {
      const b = this.buttonRef()?.nativeElement;
      const p = this.panelRef()?.nativeElement;
      const t = e.target as Node;
      if ((b && b.contains(t)) || (p && p.contains(t))) return;
      this.byPointer = true;
      this.open.set(false);
    };
    // Escape closes the tracker unless something above it took the key first: the tip and the CDK overlays mark the
    // keys they use (defaultPrevented), and the overlays hear them before the document does.
    const onKey = (e: KeyboardEvent) => {
      if (e.key !== 'Escape' || e.defaultPrevented) return;
      e.preventDefault();
      this.open.set(false);
    };
    const stop = () => {
      document.removeEventListener('pointerdown', onDown);
      document.removeEventListener('keydown', onKey);
      this.listening = false;
    };
    effect(() => {
      const open = this.isOpen();
      untracked(() => {
        if (open && !this.listening) {
          this.byPointer = false;
          document.addEventListener('pointerdown', onDown);
          document.addEventListener('keydown', onKey);
          this.listening = true;
        } else if (!open && this.listening) {
          stop();
          // Focus goes back to the button, unless a press elsewhere closed the tracker.
          const b = this.buttonRef()?.nativeElement;
          if (!this.byPointer && b?.isConnected) b.focus();
        }
      });
    });
    inject(DestroyRef).onDestroy(stop);
  }
}
