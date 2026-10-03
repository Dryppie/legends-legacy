import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestKey,
} from '@angular/cdk/testing';
import { LgButtonHarness } from './button.harness';

export interface LgDialogHarnessFilters extends BaseHarnessFilters {
  /** Its title (its name), as text or a pattern. */
  title?: string | RegExp;
}

/**
 * An open dialog or confirmation (`LgDialog.open()`, `LgDialog.confirm()`). It lives on the CDK overlay, so load it from
 * `TestbedHarnessEnvironment.documentRootLoader(fixture)`.
 */
export class LgDialogHarness extends ComponentHarness {
  static hostSelector = '.lg-dialog-pane .cdk-dialog-container';

  static with(
    options: LgDialogHarnessFilters = {},
  ): HarnessPredicate<LgDialogHarness> {
    return new HarnessPredicate(LgDialogHarness, options).addOption(
      'title',
      options.title,
      (h, title) => HarnessPredicate.stringMatches(h.getTitle(), title),
    );
  }

  private readonly title = this.locatorForOptional('.lg-dialog__title');
  private readonly content = this.locatorForOptional('.lg-dialog__content');
  private readonly close = this.locatorForOptional('.lg-dialog__close');

  /** 'dialog', or 'alertdialog' for a confirmation. */
  async getRole(): Promise<string | null> {
    return (await this.host()).getAttribute('role');
  }

  async isModal(): Promise<boolean> {
    return (await (await this.host()).getAttribute('aria-modal')) === 'true';
  }

  async getTitle(): Promise<string> {
    return (await (await this.title())?.text())?.trim() ?? '';
  }

  /** What screen readers hear as its name: the element its aria-labelledby names, or its aria-label. */
  async getName(): Promise<string> {
    const host = await this.host();
    const label = await host.getAttribute('aria-label');
    if (label) return label;
    const id = await host.getAttribute('aria-labelledby');
    if (!id) return '';
    const named = await this.documentRootLocatorFactory().locatorForOptional(
      `[id="${id}"]`,
    )();
    return (await named?.text())?.trim() ?? '';
  }

  /** What its aria-describedby names, or null. */
  async getDescription(): Promise<string | null> {
    const id = await (await this.host()).getAttribute('aria-describedby');
    if (!id) return null;
    const described =
      await this.documentRootLocatorFactory().locatorForOptional(
        `[id="${id}"]`,
      )();
    return (await described?.text())?.trim() ?? null;
  }

  async getContentText(): Promise<string> {
    return (await (await this.content())?.text())?.trim() ?? '';
  }

  /** Its buttons: the Close button's aside, the Buttons in its content and actions. */
  async getButtons(
    filter: { label?: string | RegExp } = {},
  ): Promise<LgButtonHarness[]> {
    return this.locatorForAll(LgButtonHarness.with(filter))();
  }

  async hasCloseButton(): Promise<boolean> {
    return !!(await this.close());
  }

  /** Presses its Close button. */
  async pressClose(): Promise<void> {
    const close = await this.close();
    if (!close) throw Error('This dialog has no Close button.');
    await close.click();
  }

  /** Escape, from where focus is in it. */
  async pressEscape(): Promise<void> {
    const focused = await this.locatorForOptional(':focus')();
    await (focused ?? (await this.host())).sendKeys(TestKey.ESCAPE);
  }

  /** The text of the element focus is on inside it, or null when focus is elsewhere. */
  async getFocusedText(): Promise<string | null> {
    const focused = await this.locatorForOptional(':focus')();
    return focused ? (await focused.text()).trim() : null;
  }

  /** Whether the dialog is inert: a confirmation is open over it. */
  async isInert(): Promise<boolean> {
    return (await this.host()).matchesSelector('[inert] *');
  }
}
