import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
} from '@angular/cdk/testing';

export interface LgRegionStateHarnessFilters extends BaseHarnessFilters {
  /** 'loading', 'empty', 'no-results' or 'error'. */
  state?: string;
}

/** A Region state (`lg-region-state`): what a region shows in place of its content. */
export class LgRegionStateHarness extends ComponentHarness {
  static hostSelector = 'lg-region-state';

  static with(
    options: LgRegionStateHarnessFilters = {},
  ): HarnessPredicate<LgRegionStateHarness> {
    return new HarnessPredicate(LgRegionStateHarness, options).addOption(
      'state',
      options.state,
      async (h, state) => (await h.getState()) === state,
    );
  }

  private readonly heading = this.locatorFor('.lg-rstate__heading');
  private readonly detail = this.locatorFor('.lg-rstate__detail');

  async getState(): Promise<string> {
    const cls = (await (await this.host()).getAttribute('class')) ?? '';
    return /lg-rstate--([\w-]+)/.exec(cls)?.[1] ?? '';
  }

  /** Its sentence: "No listings yet.", "Loading members…". */
  async getHeading(): Promise<string> {
    return (await (await this.heading()).text()).trim();
  }

  /** The next step's words, after the sentence. */
  async getDetail(): Promise<string> {
    return (await (await this.detail()).text()).trim();
  }

  /** Loading: the region is aria-busy. */
  async isBusy(): Promise<boolean> {
    return (await (await this.host()).getAttribute('aria-busy')) === 'true';
  }

  async getRole(): Promise<string | null> {
    return (await this.host()).getAttribute('role');
  }
}
