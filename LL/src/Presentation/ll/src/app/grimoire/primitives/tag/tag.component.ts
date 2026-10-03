import { ChangeDetectionStrategy, Component, booleanAttribute, computed, input } from '@angular/core';
import { lgCx } from '../../core/grimoire-core';
import { lgFormatNumber } from '../../core/grimoire-format';
import { LG_STATES, LgStateName, LgTagTone, lgStateWarn } from '../../core/grimoire-states';

export type { LgTagTone } from '../../core/grimoire-states';

/**
 * One to three words of status beside a name. `state` takes the word, tone and glyph from the state model, so
 * "Locked", "Claimable" or "Expires in 2h" read the same everywhere: `<lg-tag state="expiring" value="2h" />`.
 * A Tag without a state shows its content: `<lg-tag tone="new">New quest</lg-tag>`. With a state, `label` replaces
 * the state's word ("In Preset 2" for assigned). `value` follows the word, out of capitals.
 */
@Component({
  selector: 'lg-tag',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': 'classes()', '[attr.aria-hidden]': "ariaHidden() ? 'true' : null" },
  template: `@if (glyph()) {<span class="lg-tag__glyph" aria-hidden="true">{{ glyph() }}</span>}<ng-content
    />{{ word() }}@if (value() != null) {<span class="lg-tag__value">{{ formatted() }}</span
      >}@if (srText()) {<span class="lg-sr">{{ srText() }}</span>}`,
  styleUrl: './tag.component.css',
})
export class LgTagComponent {
  readonly tone = input<LgTagTone>();
  readonly state = input<LgStateName>();
  readonly value = input<number | string | null>();
  /** Hide the Tag from screen readers when its words are already in the row's name. */
  readonly ariaHidden = input(false, { transform: booleanAttribute });
  /** With a state: the words to show instead of the state's word. */
  readonly label = input<string>();

  private readonly info = computed(() => {
    const s = this.state();
    if (!s) return null;
    const info = LG_STATES[s];
    if (!info) lgStateWarn('tag:' + s, `"${s}" is not a state`);
    return info ?? null;
  });
  protected readonly toneName = computed(() => this.tone() || this.info()?.tone || 'neutral');
  protected readonly word = computed(() => this.label() ?? this.info()?.word ?? '');
  protected readonly glyph = computed(() =>
    this.toneName() === 'success' ? '✓' : this.info()?.glyph ?? null,
  );
  protected readonly srText = computed(() =>
    this.toneName() === 'beneficial' ? ', beneficial' : this.toneName() === 'harmful' ? ', harmful' : null,
  );
  protected readonly formatted = computed(() => lgFormatNumber(this.value()));
  protected readonly classes = computed(() =>
    lgCx('lg-tag', 'lg-tag--' + this.toneName(), this.state() && 'lg-tag--is-' + this.state()),
  );
}
