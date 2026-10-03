import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { LgDensity, lgCx } from '../../core/grimoire-core';

/**
 * A data table: the native `table`, `caption`, `thead`, `tbody`, `th` and `td`, in Grimoire's type, density and row
 * rhythm (D-146). `<table lgTable density="compact"><caption>Your listings</caption><thead>…</thead><tbody>…</tbody>
 * </table>`. Give a column of numbers `class="lg-table__num"` on its cells and header: tabular figures at the end of
 * the cell. A wide table goes in an `lg-tablewrap`, which holds its first column and header and hides columns by their
 * `data-priority` as its region narrows (Foundations · Layout).
 *
 * Its styles are global, as the Button's are: its cells are the screen's own markup, which an encapsulated part
 * couldn't reach (table.component.css, in grimoire.css).
 */
@Component({
  selector: 'table[lgTable]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'hostClass()',
    '[attr.data-density]': 'density() ?? null',
  },
  template: '<ng-content />',
})
export class LgTableComponent {
  /** The row rhythm (Foundations · Lines): a hairline between rows (the default), or zebra rows, never both. */
  readonly rhythm = input<'separators' | 'zebra'>('separators');
  /** Pins the table to one density; without it, it follows its region. Tables are usually Compact. */
  readonly density = input<LgDensity>();

  protected readonly hostClass = computed(() => lgCx('lg-table', this.rhythm() === 'zebra' && 'lg-table--zebra'));
}
