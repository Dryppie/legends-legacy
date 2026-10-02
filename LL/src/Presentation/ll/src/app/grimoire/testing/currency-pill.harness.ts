import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
} from '@angular/cdk/testing';
import { LgTipHarness } from './tip.harness';

export interface LgCurrencyPillHarnessFilters extends BaseHarnessFilters {
  /** The currency ("Cinders"), as text or a pattern. */
  name?: string | RegExp;
}

/** A Grimoire CurrencyPill (`[lgCurrencyPill]`): a button or a span; a short pill with `toggle` switches 12.5k and 12,480. */
export class LgCurrencyPillHarness extends ComponentHarness {
  static hostSelector = '[lgCurrencyPill]';

  static with(
    options: LgCurrencyPillHarnessFilters = {},
  ): HarnessPredicate<LgCurrencyPillHarness> {
    return new HarnessPredicate(LgCurrencyPillHarness, options).addOption(
      'name',
      options.name,
      (h, name) => HarnessPredicate.stringMatches(h.getName(), name),
    );
  }

  // The pill is its host.
  private readonly control = () => this.host();
  private readonly tip = this.documentRootLocatorFactory().locatorForOptional(
    LgTipHarness.with({ shown: true }),
  );
  private readonly amount = this.locatorFor('.lg-currency__amount');
  private readonly name = this.locatorFor('.lg-currency__name');

  /** The amount as shown: "12.5k" or "12,480". */
  async getShownAmount(): Promise<string> {
    return (await this.amount()).text();
  }

  async getName(): Promise<string> {
    return (await this.name()).text();
  }

  /** What screen readers hear: the full amount and name ("12,480 Cinders"), whatever is shown. */
  async getAccessibleName(): Promise<string> {
    return (await this.control()).text({ exclude: '[aria-hidden="true"]' });
  }

  /** The tip on hover: the full amount and name, or the `tooltip` given; null when none shows. */
  async getTooltip(): Promise<string | null> {
    const control = await this.control();
    await control.hover();
    const tip = await this.tip();
    const text = tip ? await tip.getReason() : null;
    await control.mouseAway();
    return text;
  }

  /** The width, in characters, kept for the amount, so the TopBar does not reflow as it changes. */
  async getReservedWidth(): Promise<number> {
    const width = (
      await (await this.amount()).getProperty<CSSStyleDeclaration>('style')
    ).minWidth;
    return parseFloat(width) || 0;
  }

  /** A click, tap or key press: switches a toggling pill's format, or runs your (click). */
  async press(): Promise<void> {
    return (await this.control()).click();
  }
}
