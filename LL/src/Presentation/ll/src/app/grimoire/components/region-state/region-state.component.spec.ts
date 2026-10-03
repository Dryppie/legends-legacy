import { Component, signal } from '@angular/core';
import { TestBed, fakeAsync, flush, tick } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LG_REGION_STATE, LgRegionStateKind } from './region-state.component';
import { LgSkeletonComponent } from '../../primitives/skeleton/skeleton.component';
import { LgButtonComponent } from '../../primitives/button/button.component';
import { LgRegionStateHarness } from '../../testing/region-state.harness';
import { LgButtonHarness } from '../../testing/button.harness';
import {
  lgAnnouncerIdle,
  lgQuietAnnouncer,
  lgWatchAnnouncements,
} from '../../testing/announcer';

@Component({
  imports: [...LG_REGION_STATE, LgSkeletonComponent, LgButtonComponent],
  template: `
    @switch (state()) {
      @case ('loading') {
        <lg-region-state state="loading" heading="Loading members…"
          ><lg-skeleton shape="rows" count="4"
        /></lg-region-state>
      }
      @case ('empty') {
        <lg-region-state state="empty" heading="No listings yet."
          >List an item to sell it.
          <lg-region-state-actions
            ><button lgButton (click)="listed = true">
              List an item
            </button></lg-region-state-actions
          >
        </lg-region-state>
      }
      @case ('error') {
        <lg-region-state state="error" heading="Couldn't load the roster.">
          <lg-region-state-actions
            ><button lgButton (click)="state.set('loading')">
              Try again
            </button></lg-region-state-actions
          >
        </lg-region-state>
      }
    }
  `,
})
class RosterHost {
  readonly state = signal<LgRegionStateKind>('loading');
  listed = false;
}

describe('LgRegionStateComponent', () => {
  beforeEach(lgAnnouncerIdle);

  function setup(state: LgRegionStateKind) {
    const fixture = TestBed.createComponent(RosterHost);
    fixture.componentInstance.state.set(state);
    fixture.detectChanges();
    const loader = TestbedHarnessEnvironment.loader(fixture);
    return {
      fixture,
      host: fixture.componentInstance,
      loader,
      region: () => loader.getHarness(LgRegionStateHarness),
    };
  }

  it('Empty says what would fill it, and offers the way', async () => {
    const { host, loader, region } = setup('empty');
    const empty = await region();

    expect(await empty.getState()).toBe('empty');
    expect(await empty.getRole()).toBe('status');
    expect(await empty.isBusy()).toBeFalse();
    expect(await empty.getHeading()).toBe('No listings yet.');
    expect(await empty.getDetail()).toBe('List an item to sell it.');

    await (
      await loader.getHarness(LgButtonHarness.with({ label: 'List an item' }))
    ).press();
    expect(host.listed).toBeTrue();
  });

  it('Error is the ✕ and its words, with a way to try again', async () => {
    const { fixture, loader, region } = setup('error');
    const error = await region();

    expect(await error.getHeading()).toBe("Couldn't load the roster.");
    const glyph: HTMLElement =
      fixture.nativeElement.querySelector('.lg-rstate__glyph');
    expect(glyph.textContent).toBe('✕');
    expect(glyph.getAttribute('aria-hidden')).toBe('true');

    await (
      await loader.getHarness(LgButtonHarness.with({ label: 'Try again' }))
    ).press();
    expect(await (await region()).getState()).toBe('loading');
  });

  // Harnesses flush every timer in fakeAsync, so the timed tests read the page directly.
  it('Loading is busy, shows still blocks, and says what is loading once the wait passes a second', fakeAsync(() => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();
    const { fixture } = setup('loading');
    const region: HTMLElement =
      fixture.nativeElement.querySelector('lg-region-state');

    expect(region.getAttribute('aria-busy')).toBe('true');
    expect(
      region.querySelectorAll('lg-skeleton .lg-skeleton__bar').length,
    ).toBe(4);
    expect(
      region.querySelector('.lg-rstate__heading')?.textContent?.trim(),
    ).toBe('Loading members…');
    tick(999);
    expect(watch.said).toEqual([]);
    tick(101);
    expect(watch.said).toEqual(['polite: Loading members']);

    watch.stop();
    flush();
  }));

  it('a load that ends within a second says nothing', fakeAsync(() => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();
    const { fixture, host } = setup('loading');

    tick(500);
    host.state.set('empty');
    fixture.detectChanges();
    tick(2000);

    expect(watch.said).toEqual([]);
    watch.stop();
    flush();
  }));
});
