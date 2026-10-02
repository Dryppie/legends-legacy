import { ChangeDetectionStrategy, Component, booleanAttribute, computed, input } from '@angular/core';

export type LgNoticeTone = 'info' | 'warning' | 'danger';

/** The Notice's way out, at its end: usually one small Button ("Retry"). */
@Component({
  selector: 'lg-notice-actions',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-notice__actions' },
  template: '<ng-content />',
  styles: ':host { flex: none; display: flex; gap: var(--lg-space-2); }',
})
export class LgNoticeActionsComponent {}

/**
 * A persistent notice (D-111) at the head of the stage or a region. The heading says it in words; the tone's start bar
 * backs it. `danger` is an alert, the others a status. `busy` adds a pulse, an indeterminate progressbar.
 * The detail is the content; the way out goes in `<lg-notice-actions>`.
 */
@Component({
  selector: 'lg-notice',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
    '[attr.role]': "tone() === 'danger' ? 'alert' : 'status'",
    '[attr.aria-busy]': "busy() ? 'true' : null",
  },
  template: `
    @if (busy()) {
      <span class="lg-notice__busy" role="progressbar" [attr.aria-label]="busyLabel() || 'In progress'"></span>
    }
    <div class="lg-notice__body">
      <p class="lg-notice__title">{{ heading() }}</p>
      <div class="lg-notice__text"><ng-content /></div>
    </div>
    <ng-content select="lg-notice-actions" />
  `,
  styleUrl: './notice.component.css',
})
export class LgNoticeComponent {
  readonly tone = input<LgNoticeTone>('info');
  /** What happened, in words. */
  readonly heading = input.required<string>();
  readonly busy = input(false, { transform: booleanAttribute });
  /** What the pulse is named; "In progress" by default. */
  readonly busyLabel = input<string>();

  protected readonly classes = computed(() => 'lg-notice lg-notice--' + (this.tone() || 'info'));
}

/** The Notice and its region, for a standalone `imports` array. */
export const LG_NOTICE = [LgNoticeComponent, LgNoticeActionsComponent] as const;
