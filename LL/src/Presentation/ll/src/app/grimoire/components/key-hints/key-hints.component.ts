import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export interface LgKeyHint {
  label: string;
  key: string;
}

/** Keyboard shortcuts at the stage's foot: "Back (Esc) · Select (↵)". */
@Component({
  selector: 'lg-key-hints',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <div class="lg-keyhints" role="group" [attr.aria-label]="label()">
      @for (hint of hints(); track hint.label + hint.key) {
        <span class="lg-keyhints__item"><span>{{ hint.label }}</span><kbd class="lg-key">{{ hint.key }}</kbd></span>
      }
    </div>
  `,
})
export class LgKeyHintsComponent {
  readonly hints = input.required<readonly LgKeyHint[]>();
  readonly label = input('Keyboard shortcuts');
}
