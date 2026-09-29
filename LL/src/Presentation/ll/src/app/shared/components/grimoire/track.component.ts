import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';

export type LgTrackTone = 'gilt' | 'hp' | 'arcana';

/** Milestones as diamonds along a line: tiers, tower floors, quest steps. */
@Component({
  selector: 'lg-track',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': "'lg-track lg-track--' + tone()" },
  template: `
    @if (startLabel()) {
      <span class="lg-track__end">{{ startLabel() }}</span>
    }
    <div
      class="lg-track__rail"
      role="progressbar"
      aria-valuemin="1"
      [attr.aria-valuemax]="stepCount()"
      [attr.aria-valuenow]="currentStep() + 1"
      [attr.aria-valuetext]="valueText()"
      [attr.aria-label]="label()"
    >
      <span class="lg-track__fill" [style.width.%]="fillPercent()"></span>
      @for (node of nodes(); track node.index) {
        <span
          class="lg-track__node"
          [class]="node.state"
          [style.left.%]="node.left"
          [attr.title]="node.title"
        ></span>
      }
    </div>
    @if (endLabel()) {
      <span class="lg-track__end">{{ endLabel() }}</span>
    }
  `,
})
export class LgTrackComponent {
  readonly steps = input.required<number>();
  /** Zero-based current step; earlier steps are done. */
  readonly current = input.required<number>();
  readonly startLabel = input<string>();
  readonly endLabel = input<string>();
  readonly labels = input<readonly string[]>([]);
  readonly tone = input<LgTrackTone>('gilt');
  readonly label = input('Progress');

  protected readonly stepCount = computed(() => Math.max(2, this.steps()));
  protected readonly currentStep = computed(() =>
    Math.max(0, Math.min(this.current(), this.stepCount() - 1)),
  );
  protected readonly fillPercent = computed(
    () => (this.currentStep() / (this.stepCount() - 1)) * 100,
  );
  protected readonly valueText = computed(
    () =>
      this.labels()[this.currentStep()] ??
      `Step ${this.currentStep() + 1} of ${this.stepCount()}`,
  );
  protected readonly nodes = computed(() => {
    const count = this.stepCount();
    const current = this.currentStep();
    return Array.from({ length: count }, (_, index) => ({
      index,
      left: (index / (count - 1)) * 100,
      state: index < current ? 'is-done' : index === current ? 'is-current' : 'is-todo',
      title: this.labels()[index] ?? null,
    }));
  });
}
