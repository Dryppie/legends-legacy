import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { lgUniqueId } from '../../core/grimoire-core';
import { LgButtonComponent } from '../button/button.component';
import { LgIconButtonComponent } from '../icon-button/icon-button.component';

/** A toast's tone: its start bar. The words carry the meaning; the bar backs them. */
export type LgToastTone = 'info' | 'success' | 'warning' | 'danger';

/**
 * One toast: a brief outcome on a Level 2 float (D-145). `LgToaster.show()` makes and places them; the element is
 * exported for stories and tests. The heading says what happened ("Listed for 1,200 Cinders"), the text adds a detail,
 * one action may follow ("Undo", "View"), and it can always be dismissed. It is never the only way to act, and it never
 * takes focus: the toaster announces it.
 */
@Component({
  selector: 'lg-toast',
  imports: [LgButtonComponent, LgIconButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': "'lg-toast lg-toast--' + (tone() || 'info')",
    role: 'group',
    '[attr.aria-labelledby]': 'headingId',
  },
  template: `
    <div class="lg-toast__body">
      <p class="lg-toast__heading" [id]="headingId">{{ heading() }}</p>
      @if (text()) {
        <p class="lg-toast__text">{{ text() }}</p>
      }
    </div>
    @if (action()) {
      <button lgButton="link" class="lg-toast__action" (click)="acted.emit()">{{ action() }}</button>
    }
    <button lgIconButton="close" label="Dismiss" size="sm" class="lg-toast__dismiss" (click)="dismissed.emit()"></button>
  `,
  styleUrl: './toast.component.css',
})
export class LgToastComponent {
  /** What happened, in words: "Saved", "Listed for 1,200 Cinders", "Couldn't save. Your change was undone." */
  readonly heading = input.required<string>();
  /** A detail under it, muted. */
  readonly text = input<string>();
  readonly tone = input<LgToastTone>('info');
  /** One action's label: "Undo", "View". */
  readonly action = input<string>();
  /** The action was pressed. */
  readonly acted = output<void>();
  /** Dismiss was pressed. */
  readonly dismissed = output<void>();

  protected readonly headingId = lgUniqueId('lgtoast');
}
