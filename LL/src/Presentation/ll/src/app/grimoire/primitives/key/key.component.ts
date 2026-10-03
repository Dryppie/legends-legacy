import { ChangeDetectionStrategy, Component } from '@angular/core';

/** A key cap: `<kbd lgKey>Esc</kbd>`. The host is the key. */
@Component({
  selector: 'kbd[lgKey]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-key' },
  template: `<ng-content />`,
  styleUrl: './key.component.css',
})
export class LgKeyComponent {}
