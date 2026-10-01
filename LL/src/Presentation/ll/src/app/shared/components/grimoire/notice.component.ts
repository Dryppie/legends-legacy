import { ChangeDetectionStrategy, Component, booleanAttribute, computed, contentChildren, input } from '@angular/core';
import { LgSlotDirective, lgHasSlot } from './grimoire-core';

export type LgNoticeTone = 'info' | 'warning' | 'danger';

/**
 * A persistent notice (D-111) at the head of the stage or a region. The title says it in words; the tone's start bar
 * backs it. `danger` is an alert, the others a status. `busy` adds a pulse, an indeterminate progressbar.
 * The text is the default content; an action goes in `lgSlot="action"`.
 */
@Component({
  selector: 'lg-notice',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents', '[attr.title]': 'null' },
  template: `
    <div [class]="'lg-notice lg-notice--' + (tone() || 'info')" [attr.role]="tone() === 'danger' ? 'alert' : 'status'"
      [attr.aria-busy]="busy() ? 'true' : null"
      >@if (busy()) {<span class="lg-notice__busy" role="progressbar" [attr.aria-label]="busyLabel() || 'In progress'"></span
      >}<div class="lg-notice__body"
        >@if (title()) {<p class="lg-notice__title">{{ title() }}</p>}@if (hasText()) {<div class="lg-notice__text"><ng-content /></div>}</div
      >@if (has('action')) {<div class="lg-notice__action"><ng-content select="[lgSlot=action]" /></div>}</div
    >
  `,
})
export class LgNoticeComponent {
  readonly tone = input<LgNoticeTone>('info');
  readonly title = input.required<string>();
  /** The text below the title is the default content; say so with `text`, since projected content can't be counted. */
  readonly text = input(true, { transform: booleanAttribute });
  readonly busy = input(false, { transform: booleanAttribute });
  /** What the pulse is named; "In progress" by default. */
  readonly busyLabel = input<string>();

  private readonly slots = contentChildren(LgSlotDirective);
  protected readonly hasText = computed(() => this.text());
  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }
}
