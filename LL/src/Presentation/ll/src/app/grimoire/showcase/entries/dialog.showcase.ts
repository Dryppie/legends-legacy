import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { DialogRef } from '@angular/cdk/dialog';
import {
  LG_DIALOG,
  LgButtonComponent,
  LgDialog,
  LgFieldComponent,
  LgInputComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

/** The Redeem Nobility dialog, opened by the interactive story (Foundations · Surfaces & Layering · In the game). */
@Component({
  selector: 'sc-redeem-dialog',
  imports: [...LG_DIALOG, LgButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <lg-dialog>
      <lg-dialog-header>Redeem Nobility</lg-dialog-header>
      <lg-dialog-content>
        <p class="sc-cap">
          Nobility doubles your offline time and opens the Noble market.
        </p>
      </lg-dialog-content>
      <lg-dialog-actions>
        <button lgButton="quiet" lgDialogClose>Cancel</button>
        <button lgButton="solid" (click)="redeem()">Redeem</button>
      </lg-dialog-actions>
    </lg-dialog>
  `,
})
class RedeemDialogComponent {
  private readonly dialog = inject(LgDialog);
  private readonly ref = inject(DialogRef);

  redeem(): void {
    this.dialog
      .confirm({
        heading: 'Spend 400 Soulstones?',
        text: "30 days of Nobility start now. Soulstones aren't refunded.",
        confirm: 'Redeem for 400',
      })
      .closed.subscribe((yes) => yes && this.ref.close(true));
  }
}

@Component({
  selector: 'sc-dialog-showcase',
  imports: [
    ShowcaseStoryDirective,
    ReactiveFormsModule,
    ...LG_DIALOG,
    LgButtonComponent,
    LgFieldComponent,
    LgInputComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Sheet"
      notes="Level 3: folio, shadow-panel, a line-strong hairline, radius-container; Comfortable. The title names it and Close sits beside it; the committing Button is last, the screen's one solid Button. On the overlay it sits over the scrim (try the last story)."
    >
      <lg-dialog>
        <lg-dialog-header>Rename character</lg-dialog-header>
        <lg-dialog-content>
          <lg-field label="Character name" hint="3 to 16 letters">
            <input lgInput [formControl]="name" />
          </lg-field>
        </lg-dialog-content>
        <lg-dialog-actions>
          <button lgButton="quiet">Cancel</button>
          <button lgButton="solid">Save</button>
        </lg-dialog-actions>
      </lg-dialog>
    </ng-template>

    <ng-template
      scStory="Confirmation"
      notes="An alertdialog at sm, its question the title, no Close: it is answered with Back or the action, which names its cost. Focus starts on Back when the action spends or can't be undone."
    >
      <lg-dialog size="sm">
        <lg-dialog-header [closable]="false"
          >Spend 400 Soulstones?</lg-dialog-header
        >
        <lg-dialog-content
          >30 days of Nobility start now. Soulstones aren't
          refunded.</lg-dialog-content
        >
        <lg-dialog-actions>
          <button lgButton="quiet">Back</button>
          <button lgButton="solid">Redeem for 400</button>
        </lg-dialog-actions>
      </lg-dialog>
    </ng-template>

    <ng-template
      scStory="Loss"
      notes="A loss the player can't undo takes the danger Button as its committing action."
    >
      <lg-dialog size="sm">
        <lg-dialog-header [closable]="false"
          >Leave the Ashen Order?</lg-dialog-header
        >
        <lg-dialog-content
          >Your borrowed items go back to the Vault.</lg-dialog-content
        >
        <lg-dialog-actions>
          <button lgButton="quiet">Back</button>
          <button lgButton="danger">Leave guild</button>
        </lg-dialog-actions>
      </lg-dialog>
    </ng-template>

    <ng-template
      scStory="Large"
      notes="lg is 44rem, for a list or a form in two columns. A long title wraps beside Close."
    >
      <lg-dialog size="lg">
        <lg-dialog-header
          >Transfer items between your characters on this
          account</lg-dialog-header
        >
        <lg-dialog-content
          >Choose the items to send. They arrive in the other character's
          inventory at once.</lg-dialog-content
        >
        <lg-dialog-actions>
          <button lgButton="quiet">Cancel</button>
          <button lgButton="solid">Transfer</button>
        </lg-dialog-actions>
      </lg-dialog>
    </ng-template>

    <ng-template
      scStory="Open a dialog"
      notes="Interactive: the dialog over the scrim, focus inside and trapped; Redeem opens the confirmation over it, which dims and freezes it; Escape closes the topmost layer only, and focus goes back to what opened it."
    >
      <button lgButton (click)="open()">Redeem Nobility</button>
    </ng-template>
  `,
})
export class DialogShowcaseComponent extends ShowcaseEntryComponent {
  private readonly dialog = inject(LgDialog);
  protected readonly name = new FormControl('Aldric Vane', {
    nonNullable: true,
    validators: [Validators.required],
  });

  protected open(): void {
    this.dialog.open(RedeemDialogComponent);
  }
}

export const DIALOG_SHOWCASE: ShowcaseEntry = {
  slug: 'dialog',
  name: 'Dialog',
  tier: 'primitives',
  summary: 'A modal sheet over the scrim, and the confirmation over it.',
  covers: [
    'LgDialogComponent',
    'LgDialogHeaderComponent',
    'LgDialogContentComponent',
    'LgDialogActionsComponent',
    'LgDialogCloseDirective',
    'LgConfirmComponent',
  ],
  readme: 'src/app/grimoire/primitives/dialog/README.md',
  component: DialogShowcaseComponent,
};
