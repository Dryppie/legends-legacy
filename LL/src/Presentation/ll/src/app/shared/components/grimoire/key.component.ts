import { ChangeDetectionStrategy, Component } from '@angular/core';

/** A key cap: `<lg-key>Esc</lg-key>`. */
@Component({
  selector: 'lg-key',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `<kbd class="lg-key"><ng-content /></kbd>`,
})
export class LgKeyComponent {}
