import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
} from '@angular/cdk/testing';

export interface LgToggleHarnessFilters extends BaseHarnessFilters {
  /** Its words. */
  label?: string | RegExp;
}

/** A Checkbox (`lg-checkbox`): its words and whether it is checked. */
export class LgCheckboxHarness extends ComponentHarness {
  static hostSelector = 'lg-checkbox';

  static with(
    options: LgToggleHarnessFilters = {},
  ): HarnessPredicate<LgCheckboxHarness> {
    return new HarnessPredicate(LgCheckboxHarness, options).addOption(
      'label',
      options.label,
      (harness, label) =>
        HarnessPredicate.stringMatches(harness.getLabel(), label),
    );
  }

  private readonly input = this.locatorFor('input[type=checkbox]');

  async getLabel(): Promise<string> {
    return (
      await (await this.locatorFor('.lg-checkbox__text')()).text()
    ).trim();
  }
  async isChecked(): Promise<boolean> {
    return (await this.input()).getProperty<boolean>('checked');
  }
  async isIndeterminate(): Promise<boolean> {
    return (await this.input()).getProperty<boolean>('indeterminate');
  }
  async isDisabled(): Promise<boolean> {
    return (await this.input()).getProperty<boolean>('disabled');
  }
  /** A press on the box or its words. */
  async toggle(): Promise<void> {
    await (await this.input()).click();
  }
  async focus(): Promise<void> {
    await (await this.input()).focus();
  }
  async blur(): Promise<void> {
    await (await this.input()).blur();
  }
}
