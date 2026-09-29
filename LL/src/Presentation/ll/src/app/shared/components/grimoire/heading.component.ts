import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';

export type LgHeadingLevel = 'folio' | 'screen' | 'section';

/** Engraved Marcellus title: `<h1 lgHeading="screen" sub="Ember">Wolf</h1>`. */
@Component({
  selector: 'h1[lgHeading], h2[lgHeading], h3[lgHeading], h4[lgHeading]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': 'hostClass()' },
  template: `
    @if (sub()) {
      <span class="lg-heading__sub">{{ sub() }} </span>
    }
    <span class="lg-heading__main"><ng-content /></span>
  `,
})
export class LgHeadingComponent {
  readonly level = input<LgHeadingLevel, LgHeadingLevel | ''>('screen', {
    alias: 'lgHeading',
    transform: (value: LgHeadingLevel | '') => value || 'screen',
  });
  /** Lighter leading words, set in ink-muted. */
  readonly sub = input<string>();
  protected readonly hostClass = computed(
    () => `lg-heading lg-heading--${this.level()}`,
  );
}
