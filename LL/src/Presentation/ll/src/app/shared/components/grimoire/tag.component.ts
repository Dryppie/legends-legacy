import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';

export type LgTagTone =
  | 'neutral'
  | 'new'
  | 'gilt'
  | 'danger'
  | 'warning'
  | 'common'
  | 'uncommon'
  | 'rare'
  | 'epic'
  | 'unique'
  | 'legendary'
  | 'legacy';

/** One to three words of status beside a name: "+ New quest", "Equipped", a rarity. */
@Component({
  selector: 'lg-tag',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': 'hostClass()' },
  template: `<ng-content />`,
})
export class LgTagComponent {
  readonly tone = input<LgTagTone>('neutral');
  protected readonly hostClass = computed(
    () => `lg-tag lg-tag--${this.tone()}`,
  );
}
