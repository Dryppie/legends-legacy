import { Component, signal } from '@angular/core';
import { TestBed, fakeAsync, flush } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import {
  Router,
  RouterLink,
  RouterLinkActive,
  provideRouter,
} from '@angular/router';
import {
  LgNavItemComponent,
  LgNavRailComponent,
  LgNavSectionComponent,
} from './nav-rail.component';
import { LgNavRailHarness } from '../../testing/nav-rail.harness';
import { LgTipHarness, lgCloseTip } from '../../testing/tip.harness';
import {
  lgAnnouncerIdle,
  lgQuietAnnouncer,
  lgWatchAnnouncements,
} from '../../testing/announcer';

@Component({ template: '' })
class EmptyPage {}

@Component({
  imports: [
    LgNavRailComponent,
    LgNavSectionComponent,
    LgNavItemComponent,
    RouterLink,
    RouterLinkActive,
  ],
  template: `
    <div (click)="linkFollowed = !$event.defaultPrevented">
      <lg-nav-rail [compact]="compact()">
        <lg-nav-section label="Character">
          <a
            lgNavItem
            routerLink="/character/overview"
            routerLinkActive
            ariaCurrentWhenActive="page"
            icon="overview"
            (click)="pressed.push('overview')"
            >Overview</a
          >
          <a
            lgNavItem
            routerLink="/character/inventory"
            routerLinkActive
            ariaCurrentWhenActive="page"
            icon="inventory"
            [badge]="3"
            badgeLabel="3 new items"
            (click)="pressed.push('inventory')"
            >Inventory</a
          >
          <a
            lgNavItem
            locked
            reason="Unlocks at level 20"
            icon="essences"
            (click)="pressed.push('essences')"
            >Essences</a
          >
        </lg-nav-section>
        <lg-nav-section label="City">
          <a lgNavItem href="/guild" icon="guild">Guild</a>
        </lg-nav-section>
      </lg-nav-rail>
    </div>
  `,
})
class NavRailHost {
  readonly compact = signal(false);
  /** Every item whose (click) ran. */
  readonly pressed: string[] = [];
  /** Whether the last click on the rail was left to follow its link. */
  linkFollowed: boolean | null = null;
}

describe('LgNavRailComponent', () => {
  async function setup() {
    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: '**', component: EmptyPage }])],
    });
    const fixture = TestBed.createComponent(NavRailHost);
    await TestBed.inject(Router).navigateByUrl('/character/overview');
    fixture.detectChanges();
    await fixture.whenStable();
    const rail = await TestbedHarnessEnvironment.loader(fixture).getHarness(
      LgNavRailHarness.with({ label: 'Game' }),
    );
    const page = TestbedHarnessEnvironment.documentRootLoader(fixture);
    /** The reason tip while it shows, or null. */
    const shownTip = () =>
      page.getHarnessOrNull(LgTipHarness.with({ shown: true }));
    return { fixture, host: fixture.componentInstance, rail, shownTip };
  }

  beforeEach(lgAnnouncerIdle);
  beforeEach(lgCloseTip);
  afterEach(lgCloseTip);

  it('is a navigation of named groups; routerLinkActive marks the current page, and the locked item is unavailable', async () => {
    const { fixture, rail } = await setup();

    expect(await rail.getSectionLabels()).toEqual(['Character', 'City']);
    const groups = fixture.nativeElement.querySelectorAll('lg-nav-section');
    expect(groups[0].getAttribute('role')).toBe('group');
    expect(
      document.getElementById(groups[0].getAttribute('aria-labelledby'))
        ?.textContent,
    ).toBe('Character');
    expect(await rail.getItemTitles()).toEqual([
      'Overview',
      'Inventory',
      'Essences',
      'Guild',
    ]);
    expect(await rail.currentTitle()).toBe('Overview');
    expect(await rail.isLocked('Essences')).toBeTrue();
    expect(await rail.isLocked('Inventory')).toBeFalse();
  });

  it('a click follows the link and your (click) hears it; the current page follows the route', async () => {
    const { fixture, host, rail, shownTip } = await setup();

    await rail.click('Inventory');
    await fixture.whenStable();

    expect(host.pressed).toEqual(['inventory']);
    expect(TestBed.inject(Router).url).toBe('/character/inventory');
    expect(await rail.focusedTitle()).toBe('Inventory');
    expect(await rail.currentTitle()).toBe('Inventory');
    expect(await shownTip()).toBeNull();
  });

  it('a locked item without a link stays a link in the Tab order', async () => {
    const { fixture } = await setup();
    const essences: HTMLElement =
      fixture.nativeElement.querySelectorAll('a.lg-rail__item')[2];

    expect(essences.hasAttribute('href')).toBeFalse();
    expect(essences.getAttribute('role')).toBe('link');
    expect(essences.tabIndex).toBe(0);
    expect(essences.getAttribute('aria-describedby')).toBeTruthy();
  });

  it('a click on a locked item pins and announces its reason, and never navigates', fakeAsync(async () => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();
    const { host, rail, shownTip } = await setup();

    await rail.click('Essences');

    // The press stops at the item: neither your (click) nor anything around the rail hears it.
    expect(host.pressed).toEqual([]);
    expect(host.linkFollowed).toBeNull();
    expect(TestBed.inject(Router).url).toBe('/character/overview');
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

  it('Enter on a locked item shows its condition; Escape closes it and focus stays on the item', fakeAsync(async () => {
    const { host, rail, shownTip } = await setup();
    await rail.click('Inventory');
    await rail.focusItem('Essences');

    await rail.pressKey('Enter');
    expect(await (await shownTip())?.getReason()).toBe('Unlocks at level 20');

    await rail.pressKey('Escape');
    expect(await shownTip()).toBeNull();
    expect(await rail.focusedTitle()).toBe('Essences');
    expect(host.pressed).toEqual(['inventory']);

    flush();
  }));

  it('compact keeps each title as the item’s name, and the locked one’s reason tip names it', fakeAsync(async () => {
    const { fixture, host, rail, shownTip } = await setup();
    host.compact.set(true);
    fixture.detectChanges();

    expect(await rail.getItemTitles()).toEqual([
      'Overview',
      'Inventory',
      'Essences',
      'Guild',
    ]);
    const [overview, , essences] = fixture.nativeElement.querySelectorAll(
      'a.lg-rail__item .lg-rail__title',
    );
    expect(overview.getBoundingClientRect().width).toBeLessThanOrEqual(1);
    expect(getComputedStyle(essences).display).not.toBe('none');

    await rail.click('Essences');
    const tip = await shownTip();
    expect(await tip?.getTitle()).toBe('Essences');

    flush();
  }));
});
