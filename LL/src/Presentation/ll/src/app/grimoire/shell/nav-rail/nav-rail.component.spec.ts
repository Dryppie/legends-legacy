import { Component } from '@angular/core';
import { TestBed, fakeAsync, flush } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgNavRailComponent, LgNavSection } from './nav-rail.component';
import { LgNavRailHarness } from '../../testing/nav-rail.harness';
import { LgTipHarness, lgCloseTip } from '../../testing/tip.harness';
import {
  lgAnnouncerIdle,
  lgQuietAnnouncer,
  lgWatchAnnouncements,
} from '../../testing/announcer';

@Component({
  imports: [LgNavRailComponent],
  template: `
    <div (click)="linkFollowed = !$event.defaultPrevented">
      <lg-nav-rail
        [sections]="sections"
        activeId="overview"
        (navigate)="navigations.push($event)"
      />
    </div>
  `,
})
class NavRailHost {
  readonly sections: LgNavSection[] = [
    {
      label: 'Character',
      items: [
        { id: 'overview', title: 'Overview', icon: 'overview' },
        {
          id: 'inventory',
          title: 'Inventory',
          icon: 'inventory',
          badge: 3,
          badgeLabel: '3 new items',
        },
        {
          id: 'essences',
          title: 'Essences',
          icon: 'essences',
          locked: true,
          reason: 'Unlocks at level 20',
        },
      ],
    },
    {
      label: 'City',
      items: [{ id: 'guild', title: 'Guild', icon: 'guild', href: '/guild' }],
    },
  ];
  /** Every `navigate` the rail emitted. */
  readonly navigations: string[] = [];
  /** Whether the last click on the rail was left to follow its link. */
  linkFollowed: boolean | null = null;
}

describe('LgNavRailComponent', () => {
  async function setup() {
    TestBed.configureTestingModule({ imports: [NavRailHost] });
    const fixture = TestBed.createComponent(NavRailHost);
    const rail = await TestbedHarnessEnvironment.loader(fixture).getHarness(
      LgNavRailHarness.with({ label: 'Game' }),
    );
    const page = TestbedHarnessEnvironment.documentRootLoader(fixture);
    /** The reason tip while it shows, or null. */
    const shownTip = () =>
      page.getHarnessOrNull(LgTipHarness.with({ shown: true }));
    return { host: fixture.componentInstance, rail, shownTip };
  }

  /** The parity scenario's opening: a click on Inventory. */
  async function clickInventory() {
    const ctx = await setup();
    await ctx.rail.click('Inventory');
    return ctx;
  }

  beforeEach(lgAnnouncerIdle);
  beforeEach(lgCloseTip);
  afterEach(lgCloseTip);

  it('marks the active item as the current page and the locked one as unavailable', fakeAsync(async () => {
    const { rail } = await setup();

    expect(await rail.getItemTitles()).toEqual([
      'Overview',
      'Inventory',
      'Essences',
      'Guild',
    ]);
    expect(await rail.currentTitle()).toBe('Overview');
    expect(await rail.isLocked('Essences')).toBeTrue();
    expect(await rail.isLocked('Inventory')).toBeFalse();
  }));

  it('a click on an item emits navigate; the current page is the host’s to change', fakeAsync(async () => {
    const { host, rail, shownTip } = await clickInventory();

    expect(host.navigations).toEqual(['inventory']);
    expect(await rail.focusedTitle()).toBe('Inventory');
    expect(await rail.currentTitle()).toBe('Overview');
    expect(await shownTip()).toBeNull();
  }));

  it('a click on a locked item pins and announces its reason, and never navigates', fakeAsync(async () => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();
    const { host, rail, shownTip } = await clickInventory();

    await rail.click('Essences');

    expect(host.navigations).toEqual(['inventory']);
    expect(host.linkFollowed).toBeFalse();
    expect(await rail.focusedTitle()).toBe('Essences');
    expect(await rail.currentTitle()).toBe('Overview');
    const tip = await shownTip();
    expect(await tip?.getWord()).toBe('Locked');
    expect(await tip?.getReason()).toBe('Unlocks at level 20');
    const essences = {
      hover: () => rail.hover('Essences'),
      mouseAway: () => rail.mouseAway('Essences'),
    };
    expect(await tip?.isPinned(essences)).toBeTrue();
    expect(watch.said).toEqual(['polite: Locked. Unlocks at level 20']);

    flush();
    watch.stop();
  }));

  it('Escape closes the pinned reason and focus stays on the item', fakeAsync(async () => {
    const { host, rail, shownTip } = await clickInventory();
    await rail.click('Essences');

    await rail.pressKey('Escape');

    expect(await shownTip()).toBeNull();
    expect(await rail.focusedTitle()).toBe('Essences');
    expect(host.navigations).toEqual(['inventory']);

    flush();
  }));
});
