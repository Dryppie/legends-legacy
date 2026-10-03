import { Component, inject } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { DialogRef } from '@angular/cdk/dialog';
import { LG_DIALOG } from './dialog.component';
import { LgDialog } from './dialog.service';
import { LgButtonComponent } from '../button/button.component';
import { LgDialogHarness } from '../../testing/dialog.harness';
import { LgButtonHarness } from '../../testing/button.harness';
import { lgCloseTip } from '../../testing/tip.harness';

/** The Redeem Nobility dialog (Foundations · Surfaces & Layering · In the game). */
@Component({
  imports: [...LG_DIALOG, LgButtonComponent],
  template: `
    <lg-dialog>
      <lg-dialog-header>Redeem Nobility</lg-dialog-header>
      <lg-dialog-content>
        <p>Choose how long.</p>
        <button lgButton class="days">30 days</button>
      </lg-dialog-content>
      <lg-dialog-actions>
        <button lgButton="quiet" lgDialogClose="cancelled">Cancel</button>
        <button lgButton="solid" (click)="redeem()">Redeem</button>
      </lg-dialog-actions>
    </lg-dialog>
  `,
})
class RedeemDialog {
  private readonly dialog = inject(LgDialog);
  private readonly ref = inject<DialogRef<string>>(DialogRef);

  redeem(): void {
    this.dialog
      .confirm({
        heading: 'Spend 400 Soulstones?',
        text: "30 days of Nobility start now. Soulstones aren't refunded.",
        confirm: 'Redeem for 400',
      })
      .closed.subscribe((yes) => {
        if (yes) this.ref.close('redeemed');
      });
  }
}

/** A second dialog the first can lead to. */
@Component({
  imports: [...LG_DIALOG],
  template: `<lg-dialog
    ><lg-dialog-header>Nobility perks</lg-dialog-header
    ><lg-dialog-content>Perks.</lg-dialog-content></lg-dialog
  >`,
})
class PerksDialog {}

@Component({
  imports: [LgButtonComponent],
  template: `<button lgButton (click)="open()">Redeem Nobility</button>`,
})
class OverviewPage {
  private readonly dialog = inject(LgDialog);
  result: unknown = 'open';

  open(): void {
    this.result = 'open';
    this.dialog
      .open<string>(RedeemDialog)
      .closed.subscribe((r) => (this.result = r));
  }
  perks(): void {
    this.dialog.open(PerksDialog);
  }
}

describe('LgDialog', () => {
  let page: HarnessLoader;
  let host: OverviewPage;

  beforeEach(() => {
    const fixture = TestBed.createComponent(OverviewPage);
    fixture.detectChanges();
    host = fixture.componentInstance;
    page = TestbedHarnessEnvironment.documentRootLoader(fixture);
  });

  afterEach(() => {
    TestBed.inject(LgDialog).closeAll();
    lgCloseTip();
  });

  const opener = () =>
    page.getHarness(LgButtonHarness.with({ label: 'Redeem Nobility' }));
  async function open(): Promise<LgDialogHarness> {
    await (await opener()).focus();
    await (await opener()).press();
    return page.getHarness(LgDialogHarness.with({ title: 'Redeem Nobility' }));
  }

  it('opens a modal dialog, named by its title, with focus on the first control in its content', async () => {
    const dialog = await open();

    expect(await dialog.getRole()).toBe('dialog');
    expect(await dialog.isModal()).toBeTrue();
    expect(await dialog.getName()).toBe('Redeem Nobility');
    expect(await dialog.hasCloseButton()).toBeTrue();
    expect(await dialog.getFocusedText()).toBe('30 days');
  });

  it('Escape closes it, and focus goes back to the button that opened it', async () => {
    const dialog = await open();

    await dialog.pressEscape();

    expect(await page.getHarnessOrNull(LgDialogHarness)).toBeNull();
    expect(await (await opener()).isFocused()).toBeTrue();
    expect(host.result).toBeUndefined();
  });

  it('its Close button closes it', async () => {
    const dialog = await open();

    await dialog.pressClose();

    expect(await page.getHarnessOrNull(LgDialogHarness)).toBeNull();
  });

  it('lgDialogClose closes it with its result', async () => {
    const dialog = await open();

    await (await dialog.getButtons({ label: 'Cancel' }))[0].press();

    expect(await page.getHarnessOrNull(LgDialogHarness)).toBeNull();
    expect(host.result).toBe('cancelled');
  });

  describe('a confirmation over it', () => {
    async function confirmation(): Promise<{
      dialog: LgDialogHarness;
      confirm: LgDialogHarness;
    }> {
      const dialog = await open();
      // A press focuses the button, as a click does in the browser.
      const redeem = (await dialog.getButtons({ label: 'Redeem' }))[0];
      await redeem.focus();
      await redeem.press();
      const confirm = await page.getHarness(
        LgDialogHarness.with({ title: 'Spend 400 Soulstones?' }),
      );
      return { dialog, confirm };
    }

    it('is an alertdialog named by its question and described by what follows, with focus on Back', async () => {
      const { dialog, confirm } = await confirmation();

      expect(await confirm.getRole()).toBe('alertdialog');
      expect(await confirm.getName()).toBe('Spend 400 Soulstones?');
      expect(await confirm.getDescription()).toBe(
        "30 days of Nobility start now. Soulstones aren't refunded.",
      );
      expect(await confirm.hasCloseButton()).toBeFalse();
      expect(await confirm.getFocusedText()).toBe('Back');
      // The dialog behind stays, with its state, but is inert.
      expect(await dialog.isInert()).toBeTrue();
    });

    it('Escape closes only the confirmation, and focus goes back to the button that opened it', async () => {
      const { dialog, confirm } = await confirmation();

      await confirm.pressEscape();

      expect(
        await page.getHarnessOrNull(
          LgDialogHarness.with({ title: 'Spend 400 Soulstones?' }),
        ),
      ).toBeNull();
      expect(await dialog.isInert()).toBeFalse();
      expect(await dialog.getFocusedText()).toBe('Redeem');
      expect(host.result).toBe('open');
    });

    it('Back closes it the same way', async () => {
      const { dialog, confirm } = await confirmation();

      await (await confirm.getButtons({ label: 'Back' }))[0].press();

      expect(await dialog.getFocusedText()).toBe('Redeem');
    });

    it('the committing action closes both when it finishes the dialog’s task, and focus goes back to the opener', async () => {
      const { confirm } = await confirmation();

      await (await confirm.getButtons({ label: 'Redeem for 400' }))[0].press();

      expect(await page.getHarnessOrNull(LgDialogHarness)).toBeNull();
      expect(host.result).toBe('redeemed');
      expect(await (await opener()).isFocused()).toBeTrue();
    });
  });

  it('one modal at a time: a dialog that leads to another closes first', async () => {
    await open();

    host.perks();

    const dialogs = await page.getAllHarnesses(LgDialogHarness);
    expect(dialogs.length).toBe(1);
    expect(await dialogs[0].getTitle()).toBe('Nobility perks');
    expect(host.result).toBeUndefined();
  });
});
