import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LgKeyComponent } from '../../primitives/key/key.component';

export interface LgKeyHint {
  label: string;
  key: string;
}

/** Keyboard shortcuts at the stage's foot: "Back (Esc) · Select (↵)". */
@Component({
  selector: 'lg-key-hints',
  imports: [LgKeyComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-keyhints', role: 'group', '[attr.aria-label]': 'label()' },
  template: `
    @for (hint of hints(); track hint.label + hint.key) {
      <span class="lg-keyhints__item"><span>{{ hint.label }}</span><kbd lgKey>{{ hint.key }}</kbd></span>
    }
  `,
  styleUrl: './key-hints.component.css',
})
export class LgKeyHintsComponent {
  readonly hints = input.required<readonly LgKeyHint[]>();
  readonly label = input('Keyboard shortcuts');
}
