import {
  ChangeDetectionStrategy,
  Component,
  Injectable,
  EnvironmentInjector,
  Injector,
  TemplateRef,
  ViewContainerRef,
  afterNextRender,
  inject,
  isDevMode,
} from '@angular/core';
import { InteractivityChecker } from '@angular/cdk/a11y';
import { DOCUMENT } from '@angular/common';
import { ComponentType } from '@angular/cdk/portal';
import { AutoFocusTarget, DIALOG_DATA, Dialog, DialogConfig, DialogRef } from '@angular/cdk/dialog';
import { LgTip } from '../../core/grimoire-tip';
import { lgUniqueId } from '../../core/grimoire-core';
import { LgButtonComponent } from '../button/button.component';
import {
  LgDialogActionsComponent,
  LgDialogComponent,
  LgDialogContentComponent,
  LgDialogHeaderComponent,
} from './dialog.component';

/** A dialog the `LgDialog` service opened: `closed` emits its result once it has closed. The CDK's `DialogRef`. */
export type LgDialogRef<R = unknown, C = unknown> = DialogRef<R, C>;

/** How a dialog opens. Everything else — the scrim, the focus trap and its return, Escape, `aria-modal` — is fixed. */
export interface LgDialogConfig<D = unknown> {
  /** What the dialog's component reads with `inject(DIALOG_DATA)`. */
  data?: D;
  /** The dialog's name when it has no `lg-dialog-header` to name it. */
  ariaLabel?: string;
  /** The id of what describes it, read after its name. */
  ariaDescribedBy?: string;
  /** Escape and a press on the scrim don't close it: it must be answered with its own buttons. */
  disableClose?: boolean;
  /** Asked before it closes: return false to keep it open (unsaved changes, say). */
  closePredicate?: (result: unknown) => boolean;
  /** Where focus starts. By default the element marked `cdkFocusInitial`, or else the first control in its content,
   *  then in its actions, and the Close button last; or the dialog itself, its first heading, or a selector. */
  autoFocus?: AutoFocusTarget | string;
  /** Where focus goes when it closes: the element focused when it opened (the default), another element, or none. */
  restoreFocus?: boolean | string | HTMLElement;
  /** Where the component sits in Angular's tree, for what it injects. */
  viewContainerRef?: ViewContainerRef;
  injector?: Injector;
}

/** A confirmation (Foundations · Surfaces & Layering · The modal stack): the question, and the action with its cost. */
export interface LgConfirmOptions {
  /** The question: "Spend 400 Soulstones?" */
  heading: string;
  /** What follows: "30 days of Nobility start now. Soulstones aren't refunded." */
  text?: string;
  /** The committing button names the action and its cost: "Redeem for 400". */
  confirm: string;
  /** The way back: "Back" by default. */
  back?: string;
  /** The action spends currency or can't be undone, so focus starts on Back (the default). false starts it on the
   *  committing button. */
  costly?: boolean;
  /** 'danger' for a loss: the committing button is the danger Button ("Leave guild", "Abandon quest"). */
  tone?: 'danger';
}

interface LgConfirmData extends LgConfirmOptions {
  textId: string;
}

/** A confirmation's sheet, opened by `LgDialog.confirm()`: an `alertdialog`, its question as its name. */
@Component({
  selector: 'lg-confirm',
  imports: [LgDialogComponent, LgDialogHeaderComponent, LgDialogContentComponent, LgDialogActionsComponent, LgButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <lg-dialog size="sm">
      <lg-dialog-header [closable]="false">{{ data.heading }}</lg-dialog-header>
      @if (data.text) {
        <lg-dialog-content><p class="lg-confirm__text" [id]="data.textId">{{ data.text }}</p></lg-dialog-content>
      }
      <lg-dialog-actions>
        <button lgButton="quiet" class="lg-confirm__back" (click)="ref.close(false)">{{ data.back || 'Back' }}</button>
        <button [lgButton]="data.tone === 'danger' ? 'danger' : 'solid'" class="lg-confirm__go" (click)="ref.close(true)">
          {{ data.confirm }}
        </button>
      </lg-dialog-actions>
    </lg-dialog>
  `,
  styles: `
    .lg-confirm__text { margin: 0; color: var(--lg-ink-muted); }
  `,
})
export class LgConfirmComponent {
  protected readonly data = inject<LgConfirmData>(DIALOG_DATA);
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
}

/**
 * Opens Grimoire's dialogs and confirmations on the CDK's `Dialog` (D-145): the sheet over the scrim, focus moved into it
 * and trapped there, the rest of the page hidden from screen readers, Escape and a press on the scrim closing it, focus
 * back to the element that opened it (Foundations · Surfaces & Layering · The modal stack).
 *
 *   const ref = this.dialog.open(RenameDialogComponent, { data: { name } });
 *   ref.closed.subscribe((name) => …);
 *
 *   this.dialog.confirm({ heading: 'Spend 400 Soulstones?', text: '…', confirm: 'Redeem for 400' })
 *     .closed.subscribe((yes) => yes && this.redeem());
 *
 * One modal at a time: opening a dialog closes the one open first, so a dialog that leads to another hands over to it.
 * A confirmation overlays the dialog that opened it, on its own scrim, with the dialog behind kept and inert; there is
 * only ever one. Opening either closes the page's tip.
 */
@Injectable({ providedIn: 'root' })
export class LgDialog {
  private readonly dialog = inject(Dialog);
  private readonly tip = inject(LgTip);
  private readonly doc = inject(DOCUMENT);
  private readonly environment = inject(EnvironmentInjector);
  private readonly checker = inject(InteractivityChecker);
  private modal: DialogRef<unknown, unknown> | null = null;
  private confirmation: DialogRef<boolean, LgConfirmComponent> | null = null;

  /** The dialog open now, if any (not a confirmation). */
  get openDialog(): LgDialogRef | null {
    return this.modal;
  }

  /** Opens a dialog whose component's template is an `lg-dialog`. */
  open<R = unknown, D = unknown, C = unknown>(
    content: ComponentType<C> | TemplateRef<C>,
    config: LgDialogConfig<D> = {},
  ): LgDialogRef<R, C> {
    this.confirmation?.close();
    this.modal?.close();
    if (this.modal && isDevMode()) {
      console.warn('LgDialog: the dialog open would not close, so two are open; one modal at a time (Foundations · Surfaces & Layering).');
    }
    const ref = this.openLayer<R, D, C>(content, {
      ...config,
      role: 'dialog',
      panelClass: ['lg-root', 'lg-dialog-pane'],
    });
    this.modal = ref as DialogRef<unknown, unknown>;
    ref.closed.subscribe(() => {
      if (this.modal === (ref as DialogRef<unknown, unknown>)) this.modal = null;
    });
    return ref;
  }

  /**
   * Asks before an action that spends or can't be undone. `closed` emits true for the action, false for Back, and
   * undefined for Escape or a press on the scrim.
   */
  confirm(options: LgConfirmOptions): LgDialogRef<boolean, LgConfirmComponent> {
    this.confirmation?.close();
    const textId = lgUniqueId('lgcf') + '-text';
    const behind = this.modal;
    const opener = this.doc.activeElement as HTMLElement | null;
    const ref = this.openLayer<boolean, LgConfirmData, LgConfirmComponent>(LgConfirmComponent, {
      data: { ...options, textId },
      role: 'alertdialog',
      ariaDescribedBy: options.text ? textId : undefined,
      autoFocus: options.costly === false ? '.lg-confirm__go' : '.lg-confirm__back',
      panelClass: ['lg-root', 'lg-dialog-pane', 'lg-dialog-pane--confirm'],
      // Focus goes back below, once the dialog behind is no longer inert.
      restoreFocus: false,
    });
    // The dialog behind stays, with its state, but nothing in it can be reached until the confirmation closes.
    const host = behind?.overlayRef.hostElement;
    host?.setAttribute('inert', '');
    this.confirmation = ref;
    ref.closed.subscribe(() => {
      host?.removeAttribute('inert');
      if (this.confirmation === ref) this.confirmation = null;
      const active = this.doc.activeElement;
      if (opener?.isConnected && (!active || active === this.doc.body)) opener.focus();
    });
    return ref;
  }

  /** Closes every dialog and confirmation open. */
  closeAll(): void {
    this.confirmation?.close();
    this.modal?.close();
  }

  private openLayer<R, D, C>(
    content: ComponentType<C> | TemplateRef<C>,
    config: LgDialogConfig<D> & Pick<DialogConfig, 'role' | 'panelClass'>,
  ): DialogRef<R, C> {
    // Opening a modal layer closes the page's popovers: they belong to a page that is now inert.
    this.tip.hide();
    const opener = this.doc.activeElement as HTMLElement | null;
    const ref = this.dialog.open<R, D, C>(content, {
      ...config,
      // Its first control is the first in its content, not the Close button at its head (below).
      autoFocus: config.autoFocus ?? 'dialog',
      ariaModal: true,
      hasBackdrop: true,
      backdropClass: 'lg-dialog-scrim',
      closePredicate: config.closePredicate ? (result) => config.closePredicate!(result) : undefined,
    });
    // Reduced motion set on the page (an in-game setting, the showcase's) reaches the layer, which lives outside it.
    if (opener?.closest?.('[data-motion="reduced"]')) {
      ref.overlayRef.overlayElement.setAttribute('data-motion', 'reduced');
      ref.overlayRef.backdropElement?.setAttribute('data-motion', 'reduced');
    }
    if (config.autoFocus == null) {
      const container = ref.overlayRef.overlayElement;
      afterNextRender(() => this.focusFirst(container), { injector: this.environment });
    }
    return ref;
  }

  /** Focus to the dialog's first control: what is marked `cdkFocusInitial`, the content's, the actions', then Close. */
  private focusFirst(container: HTMLElement): void {
    // Unless something in it already took focus (the CDK focuses the dialog itself, before or after this).
    const active = this.doc.activeElement;
    if (active && container.contains(active) && !active.matches('.cdk-dialog-container')) return;
    const controls = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';
    const marked = container.querySelector<HTMLElement>('[cdkFocusInitial]');
    const first = (scope: string) =>
      Array.from(container.querySelectorAll<HTMLElement>(`${scope} :is(${controls})`)).find((el) =>
        this.checker.isTabbable(el),
      );
    const target =
      marked ?? first('.lg-dialog__content') ?? first('.lg-dialog__actions') ?? first('.lg-dialog__header') ?? first('');
    target?.focus();
  }
}
