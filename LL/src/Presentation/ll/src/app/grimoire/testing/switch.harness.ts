import { ComponentHarness, HarnessPredicate } from '@angular/cdk/testing';
import { LgToggleHarnessFilters } from './checkbox.harness';

/** A Switch (`lg-switch`): a `role="switch"` button with its words. */
export class LgSwitchHarness extends ComponentHarness {
  static hostSelector = 'lg-switch';

  static with(
    options: LgToggleHarnessFilters = {},
  ): HarnessPredicate<LgSwitchHarness> {
    return new HarnessPredicate(LgSwitchHarness, options).addOption(
      'label',
      options.label,
      (harness, label) =>
        HarnessPredicate.stringMatches(harness.getLabel(), label),
    );
  }

  private readonly control = this.locatorFor('[role=switch]');

  async getLabel(): Promise<string> {
    return (await (await this.locatorFor('.lg-switch__label')()).text()).trim();
  }
  async isChecked(): Promise<boolean> {
    return (
      (await (await this.control()).getAttribute('aria-checked')) === 'true'
    );
  }
  async isDisabled(): Promise<boolean> {
    return (await this.control()).getProperty<boolean>('disabled');
  }
  async toggle(): Promise<void> {
    await (await this.control()).click();
  }
  async blur(): Promise<void> {
    await (await this.control()).blur();
  }
}
