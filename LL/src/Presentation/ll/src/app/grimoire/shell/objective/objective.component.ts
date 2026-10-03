import { ChangeDetectionStrategy, Component, computed, contentChild, input, model } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LG_NBSP, lgFormatNumber } from '../../core/grimoire-format';
import { LgPopoverComponent, LgPopoverTriggerDirective } from '../../primitives/popover/popover.component';

/** The Objective's full tracker: the quest chain's objectives and a way to the quest log. */
@Component({
  selector: 'lg-objective-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content />',
  styles: ':host { display: block; }',
})
export class LgObjectivePanelComponent {}

/**
 * The pinned quest in the TopBar's centre (D-110): its heading and current objective with the count. With an
 * `lg-objective-panel` — the full tracker — it is a button that opens it in a Popover beneath it, on the CDK overlay
 * (D-146): focus moves into it; Escape, a press on the button, or Tab past its end close it and focus goes back to the
 * button; a press outside closes it where the player pressed. An Escape something above it took first (the tip)
 * leaves it open. `[(open)]` binds the state.
 */
@Component({
  selector: 'lg-objective',
  imports: [NgTemplateOutlet, LgPopoverComponent, LgPopoverTriggerDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': "isOpen() ? 'lg-objective is-open' : 'lg-objective'" },
  template: `
    <ng-template #summary
      >@if (kicker()) {<span class="lg-objective__kicker">{{ kicker() }}</span>}<span class="lg-objective__title">{{
        heading()
      }}</span
      >@if (objective()) {<span class="lg-objective__line"
          ><span class="lg-objective__text">{{ objective() }}</span
          >@if (count(); as c) {<span class="lg-objective__count">{{ c }}</span>}</span
        >}</ng-template
    >
    @if (hasPanel()) {
      <button type="button" class="lg-objective__summary" [lgPopoverTrigger]="tracker" [(lgPopoverOpen)]="open">
        <ng-container [ngTemplateOutlet]="summary" />
      </button>
    } @else {
      <div class="lg-objective__summary"><ng-container [ngTemplateOutlet]="summary" /></div>
    }
    <lg-popover #tracker [label]="heading()" width="26.25rem">
      <ng-content select="lg-objective-panel" />
    </lg-popover>
  `,
  styleUrl: './objective.component.css',
})
export class LgObjectiveComponent {
  /** "Pinned quest". */
  readonly kicker = input<string>();
  /** The quest's title. */
  readonly heading = input.required<string>();
  /** The current objective's text. */
  readonly objective = input<string>();
  readonly current = input<number>();
  readonly required = input<number>();
  readonly open = model(false);

  private readonly panel = contentChild(LgObjectivePanelComponent);
  protected readonly hasPanel = computed(() => !!this.panel());
  protected readonly isOpen = computed(() => this.open() && this.hasPanel());
  protected readonly count = computed(() => {
    const req = this.required();
    return req ? lgFormatNumber(this.current() || 0) + LG_NBSP + '/' + LG_NBSP + lgFormatNumber(req) : null;
  });
}
