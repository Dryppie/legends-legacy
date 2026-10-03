import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
} from '@angular/cdk/testing';

export interface LgInputHarnessFilters extends BaseHarnessFilters {
  /** The value typed in it. */
  value?: string | RegExp;
}

/** A Grimoire text input or textarea (`input[lgInput]`, `textarea[lgInput]`). */
export class LgInputHarness extends ComponentHarness {
  static hostSelector = 'input[lgInput], textarea[lgInput]';

  static with(
    options: LgInputHarnessFilters = {},
  ): HarnessPredicate<LgInputHarness> {
    return new HarnessPredicate(LgInputHarness, options).addOption(
      'value',
      options.value,
      (harness, value) =>
        HarnessPredicate.stringMatches(harness.getValue(), value),
    );
  }

  async getValue(): Promise<string> {
    return (await this.host()).getProperty<string>('value');
  }

  /** The player types: the old value is cleared and the new one entered, as keys. */
  async setValue(value: string): Promise<void> {
    const host = await this.host();
    await host.clear();
    if (value) await host.sendKeys(value);
    await host.dispatchEvent('change');
  }

  async focus(): Promise<void> {
    await (await this.host()).focus();
  }

  /** Focus leaves it: Reactive Forms marks the control touched. */
  async blur(): Promise<void> {
    await (await this.host()).blur();
  }

  async isInvalid(): Promise<boolean> {
    return (await (await this.host()).getAttribute('aria-invalid')) === 'true';
  }

  async isRequired(): Promise<boolean> {
    return (await (await this.host()).getAttribute('aria-required')) === 'true';
  }

  async isDisabled(): Promise<boolean> {
    return (await this.host()).getProperty<boolean>('disabled');
  }

  /** What describes it (`aria-describedby`): its hint, or its error. */
  async getDescription(): Promise<string | null> {
    const host = await this.host();
    const ids = await host.getAttribute('aria-describedby');
    if (!ids) return null;
    const words = await Promise.all(
      ids.split(/\s+/).map(async (id) => {
        const el = await this.documentRootLocatorFactory().locatorForOptional(
          `[id="${id}"]`,
        )();
        return el ? (await el.text()).trim() : '';
      }),
    );
    return words.filter(Boolean).join(' ') || null;
  }

  /** The label that names it (its `id`'s `<label for>`). */
  async getLabel(): Promise<string | null> {
    const id = await (await this.host()).getAttribute('id');
    if (!id) return null;
    const label = await this.documentRootLocatorFactory().locatorForOptional(
      `label[for="${id}"]`,
    )();
    return label ? (await label.text()).replace(/\s*\*$/, '').trim() : null;
  }
}
