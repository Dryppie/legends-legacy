import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
} from '@angular/cdk/testing';
import { LgInputHarness } from './input.harness';

export interface LgFieldHarnessFilters extends BaseHarnessFilters {
  /** The Field's label. */
  label?: string | RegExp;
}

/** A labelled form field (`lg-field`): its label, hint and error, and the input inside it. */
export class LgFieldHarness extends ComponentHarness {
  static hostSelector = 'lg-field';

  static with(
    options: LgFieldHarnessFilters = {},
  ): HarnessPredicate<LgFieldHarness> {
    return new HarnessPredicate(LgFieldHarness, options).addOption(
      'label',
      options.label,
      (harness, label) =>
        HarnessPredicate.stringMatches(harness.getLabel(), label),
    );
  }

  private readonly labelEl = this.locatorFor('.lg-field__label');
  private readonly hintEl = this.locatorForOptional('.lg-field__hint');
  private readonly errorEl = this.locatorFor('.lg-field__error');

  /** The label's words, without the required mark. */
  async getLabel(): Promise<string> {
    return (await (await this.labelEl()).text()).replace(/\s*\*$/, '').trim();
  }

  /** The hint, while it shows (an error takes its place). */
  async getHint(): Promise<string | null> {
    const hint = await this.hintEl();
    return hint ? (await hint.text()).trim() : null;
  }

  /** The error's words, without its ✕; null when there is none. */
  async getError(): Promise<string | null> {
    const words = (await (await this.errorEl()).text()).replace('✕', '').trim();
    return words || null;
  }

  async isRequired(): Promise<boolean> {
    return !!(await this.locatorForOptional('.lg-field__required')());
  }

  /** The input or textarea inside it. */
  async getInput(): Promise<LgInputHarness> {
    return this.locatorFor(LgInputHarness)();
  }
}
