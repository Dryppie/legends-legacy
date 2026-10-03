import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestKey,
} from '@angular/cdk/testing';

export interface LgSelectHarnessFilters extends BaseHarnessFilters {
  /** What it shows: the chosen option's words, or the placeholder. */
  value?: string | RegExp;
}

/**
 * A Select (`lg-select`): its button, and the options it opens on the CDK overlay (read through the document root).
 */
export class LgSelectHarness extends ComponentHarness {
  static hostSelector = 'lg-select';

  static with(
    options: LgSelectHarnessFilters = {},
  ): HarnessPredicate<LgSelectHarness> {
    return new HarnessPredicate(LgSelectHarness, options).addOption(
      'value',
      options.value,
      (h, value) => HarnessPredicate.stringMatches(h.getValueText(), value),
    );
  }

  private readonly trigger = this.locatorFor('.lg-select__trigger');

  /** The chosen option's words, or the placeholder while nothing is chosen. */
  async getValueText(): Promise<string> {
    return (await (await this.locatorFor('.lg-select__value')()).text()).trim();
  }

  async hasValue(): Promise<boolean> {
    return !(await (
      await this.locatorFor('.lg-select__value')()
    ).hasClass('is-placeholder'));
  }

  async isOpen(): Promise<boolean> {
    return (
      (await (await this.trigger()).getAttribute('aria-expanded')) === 'true'
    );
  }

  async isDisabled(): Promise<boolean> {
    return (
      (await (await this.trigger()).getProperty<boolean>('disabled')) === true
    );
  }

  /** Its accessible name's source: the element its aria-labelledby names, or its aria-label. */
  async getLabel(): Promise<string | null> {
    const trigger = await this.trigger();
    const label = await trigger.getAttribute('aria-label');
    if (label) return label;
    const id = await trigger.getAttribute('aria-labelledby');
    if (!id) return null;
    const named = await this.documentRootLocatorFactory().locatorForOptional(
      `[id="${id}"]`,
    )();
    return (await named?.text())?.trim() ?? null;
  }

  async isFocused(): Promise<boolean> {
    return (await this.trigger()).isFocused();
  }

  /** A press on the button (it takes focus, as a click does). */
  async press(): Promise<void> {
    const trigger = await this.trigger();
    await trigger.focus();
    await trigger.click();
  }

  async pressKey(key: TestKey | string): Promise<void> {
    await (await this.trigger()).sendKeys(key);
  }

  async blur(): Promise<void> {
    await (await this.trigger()).blur();
  }

  /** The options' words, while it is open. */
  async getOptionTexts(): Promise<string[]> {
    const list = await this.list();
    if (!list) return [];
    const options = await this.documentRootLocatorFactory().locatorForAll(
      `[id="${await this.listId()}"] lg-option`,
    )();
    return Promise.all(options.map(async (o) => (await o.text()).trim()));
  }

  /** The highlighted option's words (aria-activedescendant), or null. */
  async getHighlightedText(): Promise<string | null> {
    const id = await (
      await this.trigger()
    ).getAttribute('aria-activedescendant');
    if (!id) return null;
    const option = await this.documentRootLocatorFactory().locatorForOptional(
      `[id="${id}"]`,
    )();
    return (await option?.text())?.trim() ?? null;
  }

  /** The chosen option's words, as the open list marks it, or null. */
  async getChosenOptionText(): Promise<string | null> {
    const option = await this.documentRootLocatorFactory().locatorForOptional(
      `[id="${await this.listId()}"] lg-option.is-chosen`,
    )();
    return (await option?.text())?.trim() ?? null;
  }

  /** Opens it if needed and presses the option with these words. */
  async choose(text: string): Promise<void> {
    if (!(await this.isOpen())) await this.press();
    const options = await this.documentRootLocatorFactory().locatorForAll(
      `[id="${await this.listId()}"] lg-option`,
    )();
    for (const option of options) {
      if ((await option.text()).trim() === text) {
        await option.click();
        return;
      }
    }
    throw Error(`No option "${text}".`);
  }

  private async listId(): Promise<string | null> {
    return (await this.trigger()).getAttribute('aria-controls');
  }

  private async list() {
    const id = await this.listId();
    return id
      ? this.documentRootLocatorFactory().locatorForOptional(`[id="${id}"]`)()
      : null;
  }
}
