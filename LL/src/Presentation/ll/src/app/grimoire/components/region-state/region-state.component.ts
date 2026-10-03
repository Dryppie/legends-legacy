import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  untracked,
} from '@angular/core';
import { LgAnnouncer } from '../../core/grimoire-announcer';
import { LG_STATES } from '../../core/grimoire-states';

/** The Data states a whole region can be in instead of its content (Standards · States · Data). */
export type LgRegionStateKind = 'loading' | 'empty' | 'no-results' | 'error';

/** How long a load waits before it says in words what is loading, and announces it (Standards · States · Loading). */
const SAY_AFTER = 1000;

/** The Region state's way forward: one or two Buttons ("Try again", "List an item", "Clear filters"). */
@Component({
  selector: 'lg-region-state-actions',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-rstate__actions' },
  template: '<ng-content />',
  styles: ':host { display: flex; flex-wrap: wrap; align-items: center; gap: var(--lg-space-2); }',
})
export class LgRegionStateActionsComponent {}

/**
 * What a region shows in place of its content while it has none to show (Standards · States · Data):
 *
 *   <lg-region-state state="loading" heading="Loading members…"><lg-skeleton shape="rows" count="5" /></lg-region-state>
 *   <lg-region-state state="empty" heading="No listings yet.">List an item to sell it.
 *     <lg-region-state-actions><button lgButton (click)="list()">List an item</button></lg-region-state-actions>
 *   </lg-region-state>
 *   <lg-region-state state="no-results" heading="No matching players">…Clear filters…</lg-region-state>
 *   <lg-region-state state="error" heading="Couldn't load the roster.">…Try again…</lg-region-state>
 *
 * `heading` is the sentence, in the words of Standards · States; the content is the next step, or, while loading, the
 * still blocks in the shape of what is coming. Empty offers a way to make something; no results a way to undo the
 * filter. Loading is `aria-busy`: its words show, and are announced, once the wait passes a second. Error is the ✕ and
 * its words in `danger`, with a way to try again. The host is the region's own box: it sits where the content would.
 */
@Component({
  selector: 'lg-region-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': "'lg-rstate lg-rstate--' + state()",
    role: 'status',
    '[attr.aria-busy]': "state() === 'loading' ? 'true' : null",
  },
  template: `
    <div class="lg-rstate__line">
      @if (state() === 'error') {<span class="lg-rstate__glyph" aria-hidden="true">{{ errorGlyph }}</span>}
      <span class="lg-rstate__heading">{{ words() }}</span>
      <span class="lg-rstate__detail"><ng-content /></span>
    </div>
    <ng-content select="lg-region-state-actions" />
  `,
  styleUrl: './region-state.component.css',
})
export class LgRegionStateComponent {
  readonly state = input.required<LgRegionStateKind>();
  /** The sentence: "Loading members…", "No listings yet.", "No matching players", "Couldn't load the roster." */
  readonly heading = input<string>();

  private readonly announcer = inject(LgAnnouncer);
  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  protected readonly loadingWord = LG_STATES.loading.word;
  protected readonly errorGlyph = LG_STATES.error.glyph;
  protected readonly words = computed(() => this.heading() || (this.state() === 'loading' ? this.loadingWord : ''));
  private readonly loadingFor = computed(() => (this.state() === 'loading' ? this.words() : null));

  constructor() {
    let timer: ReturnType<typeof setTimeout> | undefined;
    effect(() => {
      const words = this.loadingFor();
      untracked(() => {
        clearTimeout(timer);
        if (!words) return;
        // The words show after a second (the stylesheet's delay), and are said once then, without their ellipsis:
        // "Loading members".
        timer = setTimeout(() => {
          if (this.el.isConnected) this.announcer.announce((words || '').replace(/…$/, ''), { key: 'loading' });
        }, SAY_AFTER);
      });
    });
    inject(DestroyRef).onDestroy(() => clearTimeout(timer));
  }
}

/** The Region state and its region, for a standalone `imports` array. */
export const LG_REGION_STATE = [LgRegionStateComponent, LgRegionStateActionsComponent] as const;
