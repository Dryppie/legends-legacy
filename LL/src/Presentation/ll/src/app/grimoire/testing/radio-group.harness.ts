import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  parallel,
} from '@angular/cdk/testing';

export interface LgRadioGroupHarnessFilters extends BaseHarnessFilters {
  /** The group's legend. */
  label?: string | RegExp;
}

/** A radio group (`lg-radio-group`): its legend, its choices by their words, and the chosen one. */
export class LgRadioGroupHarness extends ComponentHarness {
  static hostSelector = 'lg-radio-group';

  static with(
    options: LgRadioGroupHarnessFilters = {},
  ): HarnessPredicate<LgRadioGroupHarness> {
    return new HarnessPredicate(LgRadioGroupHarness, options).addOption(
      'label',
      options.label,
      (harness, label) =>
        HarnessPredicate.stringMatches(harness.getLabel(), label),
    );
  }

  private readonly texts = this.locatorForAll('lg-radio .lg-radio__text');
  private readonly inputs = this.locatorForAll('lg-radio input[type=radio]');

  async getLabel(): Promise<string> {
    return (await (await this.locatorFor('legend')()).text()).trim();
  }
  async getOptions(): Promise<string[]> {
    const texts = await this.texts();
    return parallel(() => texts.map(async (t) => (await t.text()).trim()));
  }
  /** The chosen choice's words, or null. */
  async getSelected(): Promise<string | null> {
    const [texts, inputs] = await parallel(() => [this.texts(), this.inputs()]);
    const checked = await parallel(() =>
      inputs.map((i) => i.getProperty<boolean>('checked')),
    );
    const i = checked.indexOf(true);
    return i < 0 ? null : (await texts[i].text()).trim();
  }
  /** A press on the choice with these words. */
  async select(label: string): Promise<void> {
    const options = await this.getOptions();
    const i = options.indexOf(label);
    if (i < 0)
      throw Error(`No choice "${label}"; the group has ${options.join(', ')}.`);
    await (await this.inputs())[i].click();
  }
  async isDisabled(): Promise<boolean> {
    return (await this.locatorFor('fieldset')()).getProperty<boolean>(
      'disabled',
    );
  }
}
