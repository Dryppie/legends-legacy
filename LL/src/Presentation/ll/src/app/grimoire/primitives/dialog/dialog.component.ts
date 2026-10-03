import {
  ChangeDetectionStrategy,
  Component,
  Directive,
  OnDestroy,
  OnInit,
  booleanAttribute,
  inject,
  input,
} from '@angular/core';
import { CdkDialogContainer, DialogRef } from '@angular/cdk/dialog';
import { lgUniqueId } from '../../core/grimoire-core';
import { LgIconButtonComponent } from '../icon-button/icon-button.component';

export type LgDialogSize = 'sm' | 'md' | 'lg';

/** The container a dialog's content was opened into, when it was (inline, in a story or a test, there is none). */
function openedIn(ref: DialogRef | null): CdkDialogContainer | null {
  return (ref?.containerInstance as CdkDialogContainer | undefined) ?? null;
}

/**
 * The dialog's title, at the head of the sheet, and its Close button: `<lg-dialog-header>Rename character</lg-dialog-header>`.
 * The words are an `h2` in the section title style, and name the dialog (`aria-labelledby`). `closable="false"` leaves
 * the Close button out, for a dialog that must be answered with its own buttons.
 */
@Component({
  selector: 'lg-dialog-header',
  imports: [LgIconButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-dialog__header' },
  template: `<h2 class="lg-heading lg-heading--section lg-dialog__title" [id]="titleId"><ng-content /></h2
    >@if (closable()) {<button lgIconButton="close" label="Close" size="sm" class="lg-dialog__close" (click)="close()"></button>}`,
  styles: `
    :host { display: flex; align-items: flex-start; gap: var(--lg-space-3); flex: none; padding: var(--lg-inset) var(--lg-inset) var(--lg-space-4); }
    .lg-dialog__title { flex: 1 1 auto; min-width: 0; overflow-wrap: anywhere; }
    /* The Close button sits on the title's first line, its icon on the inset's edge. */
    .lg-dialog__close { margin: calc((var(--lg-leading-title-md) - var(--lg-control)) / 2) calc(-1 * var(--lg-space-2)) 0 0; }
  `,
})
export class LgDialogHeaderComponent implements OnInit, OnDestroy {
  readonly closable = input(true, { transform: booleanAttribute });
  protected readonly titleId = lgUniqueId('lgdlg') + '-title';
  private readonly ref = inject(DialogRef, { optional: true });
  private readonly container = openedIn(this.ref);

  ngOnInit(): void {
    // After this check: the container has already drawn its aria-labelledby for this pass.
    if (this.container) void Promise.resolve().then(() => this.container?._addAriaLabelledBy(this.titleId));
  }

  ngOnDestroy(): void {
    this.container?._removeAriaLabelledBy(this.titleId);
  }

  protected close(): void {
    this.ref?.close(undefined, { focusOrigin: 'mouse' });
  }
}

/** The dialog's body: what it asks or shows. It scrolls when the sheet is taller than the window allows. */
@Component({
  selector: 'lg-dialog-content',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-dialog__content' },
  template: '<ng-content />',
  styles: `
    :host { display: block; flex: 1 1 auto; min-height: 0; overflow-y: auto; padding: 0 var(--lg-inset) var(--lg-inset); }
    :host(:first-child) { padding-top: var(--lg-inset); }
  `,
})
export class LgDialogContentComponent {}

/**
 * The dialog's buttons, at its foot and end: the committing one last, the screen's one `solid` Button (Foundations ·
 * Surfaces & Layering · The modal stack), with Cancel or Back before it.
 */
@Component({
  selector: 'lg-dialog-actions',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-dialog__actions' },
  template: '<ng-content />',
  styles: `
    :host { display: flex; flex-wrap: wrap; justify-content: flex-end; align-items: center; gap: var(--lg-space-2); flex: none; padding: 0 var(--lg-inset) var(--lg-inset); }
    :host(:first-child) { padding-top: var(--lg-inset); }
  `,
})
export class LgDialogActionsComponent {}

/**
 * Closes the dialog it is in, with a result: `<button lgButton="quiet" lgDialogClose>Cancel</button>`,
 * `<button lgButton="solid" [lgDialogClose]="name.value">Save</button>`. Outside a dialog it does nothing.
 */
@Directive({
  selector: '[lgDialogClose]',
  host: { '(click)': 'close($event)' },
})
export class LgDialogCloseDirective {
  /** What the dialog closes with: `closed` emits it. */
  readonly result = input<unknown>(undefined, { alias: 'lgDialogClose' });
  private readonly ref = inject(DialogRef, { optional: true });

  protected close(event: MouseEvent): void {
    // A blocked or pending Button stops its own press before this hears it.
    this.ref?.close(this.result(), { focusOrigin: event.detail === 0 ? 'keyboard' : 'mouse' });
  }
}

/**
 * A dialog's sheet: Level 3 (`folio`, `shadow-panel`, a `line-strong` hairline, `radius-container`), Comfortable,
 * holding its regions. `LgDialog.open()` puts it over the scrim on the CDK overlay; your dialog component's template is
 *
 *   <lg-dialog>
 *     <lg-dialog-header>Rename character</lg-dialog-header>
 *     <lg-dialog-content>…</lg-dialog-content>
 *     <lg-dialog-actions>
 *       <button lgButton="quiet" lgDialogClose>Cancel</button>
 *       <button lgButton="solid" (click)="save()">Save</button>
 *     </lg-dialog-actions>
 *   </lg-dialog>
 *
 * A dialog holds tiles, slots, inputs and washes, never a Panel; it groups its content with headings and SectionRules
 * (Foundations · Surfaces & Layering · Nesting).
 */
@Component({
  selector: 'lg-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': "'lg-dialog lg-dialog--' + (size() || 'md')",
    'data-density': 'comfortable',
  },
  template: '<ng-content />',
  styleUrl: './dialog.component.css',
})
export class LgDialogComponent {
  /** The sheet's width: `sm` 24rem (a confirmation), `md` 32rem (the default), `lg` 44rem (a list or a form in two columns). */
  readonly size = input<LgDialogSize>('md');
}

/** The dialog's sheet, regions and close directive, for a standalone `imports` array. */
export const LG_DIALOG = [
  LgDialogComponent,
  LgDialogHeaderComponent,
  LgDialogContentComponent,
  LgDialogActionsComponent,
  LgDialogCloseDirective,
] as const;
