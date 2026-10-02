import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { LgNumeric, lgPlainValue, lgUnitSplit } from '../../core/grimoire-format';

/**
 * A value with its unit set smaller and muted right after it: 84 HP/5s, 12s, 24.8%. Used inside other components;
 * `<lg-num [value]="'84 HP/5s'" />`.
 */
@Component({
  selector: 'lg-num',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `@if (split(); as s) {{{ s.number }}<span [class]="unitClass()">{{ s.unit }}</span>} @else {{{ plain() }}}`,
})
export class LgNumComponent {
  readonly value = input.required<LgNumeric>();
  readonly unitClass = input('lg-unit');
  protected readonly split = computed(() => lgUnitSplit(this.value()));
  protected readonly plain = computed(() => lgPlainValue(this.value()));
}
