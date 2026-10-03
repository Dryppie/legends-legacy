import { Component, signal } from '@angular/core';
import { TestBed, fakeAsync, flush, tick } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgIconButtonComponent } from './icon-button.component';
import { LgIconButtonHarness } from '../../testing/icon-button.harness';
import { LgTipHarness, lgCloseTip } from '../../testing/tip.harness';
import {
  lgAnnouncerIdle,
  lgQuietAnnouncer,
  lgWatchAnnouncements,
} from '../../testing/announcer';

@Component({
  imports: [LgIconButtonComponent],
  // Kept in view: the tip closes when its element is off screen.
  host: { style: 'position: fixed; top: 0; left: 0' },
  template: `
    <button
      lgIconButton="close"
      label="Close"
      (click)="closes = closes + 1"
    ></button>
    <button
      lgIconButton="expand"
      [label]="tall() ? 'Make chat shorter' : 'Make chat taller'"
      [pressed]="tall()"
      (click)="tall.set(!tall())"
    ></button>
    <button
      lgIconButton="expand"
      label="Sort by level"
      state="unavailable"
      reason="Not while the list loads"
      (click)="sorts = sorts + 1"
    ></button>
    <button lgIconButton="close" label="Remove filter" disabled></button>
  `,
})
class IconButtonHost {
  closes = 0;
  sorts = 0;
  readonly tall = signal(false);
}

describe('LgIconButtonComponent', () => {
  beforeEach(lgAnnouncerIdle);
  afterEach(lgCloseTip);

  function setup() {
    const fixture = TestBed.createComponent(IconButtonHost);
    fixture.detectChanges();
    const loader = TestbedHarnessEnvironment.loader(fixture);
    const page = TestbedHarnessEnvironment.documentRootLoader(fixture);
    const shownTip = () =>
      page.getHarnessOrNull(LgTipHarness.with({ shown: true }));
    return { fixture, host: fixture.componentInstance, loader, shownTip };
  }

  it('is named by its label, which its tooltip shows on hover without repeating it as a description', fakeAsync(async () => {
    const { loader, shownTip } = setup();
    const close = await loader.getHarness(
      LgIconButtonHarness.with({ label: 'Close' }),
    );

    expect(await close.isPressed()).toBeNull();
    expect(await close.getDescription()).toBeNull();
    await close.hover();
    expect(await (await shownTip())?.getReason()).toBe('Close');

    await close.mouseAway();
    tick(200);
    expect(await shownTip()).toBeNull();
    flush();
  }));

  it('acts on a press, and its tooltip goes', fakeAsync(async () => {
    const { host, loader, shownTip } = setup();
    const close = await loader.getHarness(
      LgIconButtonHarness.with({ label: 'Close' }),
    );

    await close.hover();
    await close.press();

    expect(host.closes).toBe(1);
    tick(200);
    expect(await shownTip()).toBeNull();
    flush();
  }));

  it('as a toggle, reports pressed and takes its new name', async () => {
    const { loader } = setup();
    const toggle = await loader.getHarness(
      LgIconButtonHarness.with({ label: /chat/ }),
    );

    expect(await toggle.isPressed()).toBeFalse();
    await toggle.press();

    expect(await toggle.isPressed()).toBeTrue();
    expect(await toggle.getLabel()).toBe('Make chat shorter');
  });

  it('blocked, it stays focusable, says why, and answers a press with its reason instead of acting', fakeAsync(async () => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();
    const { host, loader, shownTip } = setup();
    const sort = await loader.getHarness(
      LgIconButtonHarness.with({ label: 'Sort by level' }),
    );

    expect(await sort.isBlocked()).toBeTrue();
    expect(await sort.isDisabled()).toBeFalse();
    expect(await sort.getDescription()).toBe('Not while the list loads');

    await sort.press();
    tick(100);

    expect(host.sorts).toBe(0);
    const tip = await shownTip();
    // The icon shows no words, so the tip names the action.
    expect(await tip?.getTitle()).toBe('Sort by level');
    expect(await tip?.getReason()).toBe('Not while the list loads');
    expect(watch.said).toEqual(['polite: Not while the list loads']);
    watch.stop();
    flush();
  }));

  it('plain disabled leaves the Tab order', async () => {
    const { loader } = setup();
    const remove = await loader.getHarness(
      LgIconButtonHarness.with({ label: 'Remove filter' }),
    );
    expect(await remove.isDisabled()).toBeTrue();
    expect(await remove.isBlocked()).toBeFalse();
  });
});
