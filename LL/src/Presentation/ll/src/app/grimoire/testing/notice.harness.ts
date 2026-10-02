import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
} from '@angular/cdk/testing';
import { LgNoticeTone } from '../components/notice/notice.component';

export interface LgNoticeHarnessFilters extends BaseHarnessFilters {
  /** What the notice says happened. */
  heading?: string | RegExp;
  tone?: LgNoticeTone;
}

const TONES: readonly LgNoticeTone[] = ['info', 'warning', 'danger'];

/** `lg-notice`, a persistent notice. Notices are found by their heading or tone. */
export class LgNoticeHarness extends ComponentHarness {
  static hostSelector = 'lg-notice';

  static with(
    options: LgNoticeHarnessFilters = {},
  ): HarnessPredicate<LgNoticeHarness> {
    return new HarnessPredicate(LgNoticeHarness, options)
      .addOption('heading', options.heading, (harness, heading) =>
        HarnessPredicate.stringMatches(harness.getHeading(), heading),
      )
      .addOption(
        'tone',
        options.tone,
        async (harness, tone) => (await harness.getTone()) === tone,
      );
  }

  private readonly heading = this.locatorFor('.lg-notice__title');
  private readonly text = this.locatorFor('.lg-notice__text');
  private readonly busy = this.locatorForOptional('.lg-notice__busy');

  async getHeading(): Promise<string> {
    return (await (await this.heading()).text()).trim();
  }

  /** The detail under the heading; empty when there is none. */
  async getText(): Promise<string> {
    return (await (await this.text()).text()).replace(/\s+/g, ' ').trim();
  }

  async getTone(): Promise<LgNoticeTone> {
    const host = await this.host();
    for (const tone of TONES) {
      if (await host.hasClass(`lg-notice--${tone}`)) return tone;
    }
    return 'info';
  }

  /** `alert` for danger, `status` otherwise. */
  async getRole(): Promise<string | null> {
    return (await this.host()).getAttribute('role');
  }

  /** Whether something is under way: the pulsing progressbar shows. */
  async isBusy(): Promise<boolean> {
    return !!(await this.busy());
  }

  /** What the pulse is called, or null when the notice isn't busy. */
  async getBusyLabel(): Promise<string | null> {
    const busy = await this.busy();
    return busy ? busy.getAttribute('aria-label') : null;
  }
}
