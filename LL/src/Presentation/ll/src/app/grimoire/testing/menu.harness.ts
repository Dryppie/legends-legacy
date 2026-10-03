import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestKey,
} from '@angular/cdk/testing';

export interface LgMenuHarnessFilters extends BaseHarnessFilters {
  /** Its name, as text or a pattern. */
  label?: string | RegExp;
}

/**
 * A Menu (`lg-menu`). One a trigger opened is on the CDK overlay: load it from
 * `TestbedHarnessEnvironment.documentRootLoader(fixture)`.
 */
export class LgMenuHarness extends ComponentHarness {
  static hostSelector = 'lg-menu';

  static with(
    options: LgMenuHarnessFilters = {},
  ): HarnessPredicate<LgMenuHarness> {
    return new HarnessPredicate(LgMenuHarness, options).addOption(
      'label',
      options.label,
      async (h, label) =>
        HarnessPredicate.stringMatches(
          (await (await h.host()).getAttribute('aria-label')) ?? '',
          label,
        ),
    );
  }

  private readonly items = this.locatorForAll('.lg-menu__item');

  /** Every item's words, in order. */
  async getItemTexts(): Promise<string[]> {
    return Promise.all(
      (await this.items()).map(async (i) => (await i.text()).trim()),
    );
  }

  /** The words of the item focus is on, or null. */
  async getFocusedItemText(): Promise<string | null> {
    const item = await this.locatorForOptional('.lg-menu__item:focus')();
    return (await item?.text())?.trim() ?? null;
  }

  /** Whether the item with these words is checked: true, false, or null when it isn't a checkbox item. */
  async isChecked(text: string): Promise<boolean | null> {
    const checked = await (await this.item(text)).getAttribute('aria-checked');
    return checked == null ? null : checked === 'true';
  }

  async isItemDisabled(text: string): Promise<boolean> {
    return (
      (await (await this.item(text)).getAttribute('aria-disabled')) === 'true'
    );
  }

  /** A press on the item with these words. */
  async pressItem(text: string): Promise<void> {
    await (await this.item(text)).click();
  }

  /** A key pressed where focus is in the menu. */
  async pressKey(key: TestKey | string): Promise<void> {
    const focused = await this.locatorForOptional('.lg-menu__item:focus')();
    await (focused ?? (await this.host())).sendKeys(key);
  }

  private async item(text: string) {
    for (const item of await this.items()) {
      if ((await item.text()).trim() === text) return item;
    }
    throw Error(`No menu item "${text}".`);
  }
}
