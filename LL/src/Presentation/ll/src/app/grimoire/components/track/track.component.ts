import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type LgTrackTone = 'gilt' | 'hp' | 'arcana';

/** Milestones as diamonds along a line: tiers, tower floors, quest steps. */
@Component({
  selector: 'lg-track',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': "'lg-track lg-track--' + (tone() || 'gilt')" },
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
      [attr.aria-label]="label() || 'Progress'"
    >
      <span class="lg-track__fill" [style.width.%]="fillPercent()"></span>
      @for (node of nodes(); track node.index) {
        <span [class]="'lg-track__node ' + node.state" [style.left.%]="node.left" [attr.title]="node.title"></span>
      }
    </div>
    @if (endLabel()) {
      <span class="lg-track__end">{{ endLabel() }}</span>
    }
  `,
  styleUrl: './track.component.css',
})
export class LgTrackComponent {
  readonly steps = input<number>(5);
  /** Zero-based current step; earlier steps are done. */
  readonly current = input<number>(0);
  readonly startLabel = input<string>();
  readonly endLabel = input<string>();
  readonly labels = input<readonly string[]>();
  readonly tone = input<LgTrackTone>('gilt');
  readonly label = input<string>('Progress');

  protected readonly stepCount = computed(() => Math.max(2, this.steps() || 5));
  protected readonly currentStep = computed(() => Math.max(0, Math.min(this.current() || 0, this.stepCount() - 1)));
  protected readonly fillPercent = computed(() => (this.currentStep() / (this.stepCount() - 1)) * 100);
  protected readonly valueText = computed(
    () => this.labels()?.[this.currentStep()] || `Step ${this.currentStep() + 1} of ${this.stepCount()}`,
  );
  protected readonly nodes = computed(() => {
    const n = this.stepCount();
    const cur = this.currentStep();
    return Array.from({ length: n }, (_, index) => ({
      index,
      left: (index / (n - 1)) * 100,
      state: index < cur ? 'is-done' : index === cur ? 'is-current' : 'is-todo',
      title: this.labels()?.[index] || null,
    }));
  });
}
